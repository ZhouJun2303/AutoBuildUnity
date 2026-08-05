using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 将 Unity 日志输出到屏幕，便于移动端调试。
///
/// 「一段」定义（对齐 Unity → Android Logcat）：
/// 一次 Debug.Log / LogWarning / LogError / Exception 回调 = 一段，
/// 对应 adb logcat 中 tag 为 Unity 的一次日志事件（消息 + 可选堆栈）。
/// 复制内容按 Logcat 风格：P/Unity: 每行正文。
/// </summary>
public class ConsoleToScreen : MonoBehaviour
{
    [Header("显示")]
    [Tooltip("默认是否展开日志面板")]
    public bool visible = true;

    [Tooltip("基础字号（会按屏幕密度缩放）")]
    [Range(18, 48)]
    public int fontSize = 28;

    [Tooltip("最多保留的日志段数（一段 = 一次 Unity Log / 一次 Logcat Unity 事件）")]
    [Range(50, 500)]
    public int maxLines = 200;

    [Tooltip("面板占屏幕高度的比例（越大可滑动区域越大）")]
    [Range(0.4f, 0.95f)]
    public float panelHeightRatio = 0.78f;

    [Header("行为")]
    [Tooltip("有新日志时自动滚到底部（用户上滑阅读后暂停，点「底」再恢复）")]
    public bool autoScrollToBottom = true;

    [Tooltip("是否附带堆栈（与 Player Settings 里 Stack Trace Logging 类似，拼进同一段 Logcat 文本）")]
    public bool showStackTrace = true;

    [Tooltip("仅 Error/Exception 附带堆栈；关闭则所有级别在 showStackTrace 开启时都带堆栈")]
    public bool stackTraceOnlyForErrors = true;

    [Tooltip("复制成功提示显示秒数")]
    public float copyToastSeconds = 1.5f;

    const float DesignWidth = 1080f;
    const float DesignHeight = 1920f;
    const float EdgePadding = 12f;
    const float ButtonHeight = 56f;
    const float ButtonWidth = 120f;
    const float ButtonGap = 10f;
    const float CopyBtnWidth = 88f;

    readonly List<LogEntry> _entries = new List<LogEntry>(256);

    Vector2 _scrollPosition;
    bool _autoScroll = true;
    bool _dirty;

    // 整区拖拽滑动（移动端不能只依赖右侧滚动条）
    bool _scrollPointerDown;
    bool _scrollDragMoved;
    Vector2 _scrollDragLastGui;
    const float ScrollDragThreshold = 12f;

    string _toastText;
    float _toastUntil;
    int _highlightIndex = -1;
    float _highlightUntil;

    GUIStyle _labelStyle;
    GUIStyle _buttonStyle;
    GUIStyle _copyBtnStyle;
    GUIStyle _titleStyle;
    GUIStyle _toastStyle;
    Texture2D _panelBg;
    Texture2D _buttonBg;
    Texture2D _buttonBgOn;
    Texture2D _rowBg;
    Texture2D _rowBgAlt;
    Texture2D _rowBgHighlight;
    Texture2D _toastBg;

    struct LogEntry
    {
        /// <summary>展示与复制用的完整 Logcat 风格文本（一段）</summary>
        public string text;
        public LogType type;
        public Color color;
    }

    void OnEnable()
    {
        Application.logMessageReceived += OnLog;
        _autoScroll = autoScrollToBottom;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= OnLog;
    }

    void OnDestroy()
    {
        DestroyTexture(ref _panelBg);
        DestroyTexture(ref _buttonBg);
        DestroyTexture(ref _buttonBgOn);
        DestroyTexture(ref _rowBg);
        DestroyTexture(ref _rowBgAlt);
        DestroyTexture(ref _rowBgHighlight);
        DestroyTexture(ref _toastBg);
    }

    static void DestroyTexture(ref Texture2D tex)
    {
        if (tex == null) return;
        if (Application.isPlaying)
            Destroy(tex);
        else
            DestroyImmediate(tex);
        tex = null;
    }

    void OnLog(string logString, string stackTrace, LogType type)
    {
        if (string.IsNullOrEmpty(logString))
            return;

        // 一段 = 一次 Unity 日志事件（与写入 Logcat tag=Unity 的那次对应）
        bool attachStack = ShouldAttachStack(type) && !string.IsNullOrEmpty(stackTrace);
        string text = FormatUnityLogcatSegment(logString, attachStack ? stackTrace : null, type);

        _entries.Add(new LogEntry
        {
            text = text,
            type = type,
            color = ColorFor(type)
        });

        int overflow = _entries.Count - maxLines;
        if (overflow > 0)
            _entries.RemoveRange(0, overflow);

        _dirty = true;
    }

    bool ShouldAttachStack(LogType type)
    {
        if (!showStackTrace)
            return false;
        if (!stackTraceOnlyForErrors)
            return true;
        return type == LogType.Error || type == LogType.Exception || type == LogType.Assert;
    }

    /// <summary>
    /// 格式化成接近 adb logcat / Unity Android Logcat 中 tag=Unity 的一段文本。
    /// 多行消息与堆栈的每一行都带上 P/Unity: 前缀，复制后可直接对照 Logcat。
    /// </summary>
    static string FormatUnityLogcatSegment(string message, string stackTrace, LogType type)
    {
        char priority = PriorityChar(type);
        // 例: I/Unity: hello
        string prefix = priority + "/Unity: ";

        var sb = new System.Text.StringBuilder(256);
        AppendPrefixedLines(sb, prefix, message);
        if (!string.IsNullOrEmpty(stackTrace))
        {
            if (sb.Length > 0)
                sb.Append('\n');
            AppendPrefixedLines(sb, prefix, stackTrace);
        }

        return sb.ToString();
    }

    static void AppendPrefixedLines(System.Text.StringBuilder sb, string prefix, string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return;

        string text = raw.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd();
        int start = 0;
        bool first = sb.Length == 0;
        for (int i = 0; i <= text.Length; i++)
        {
            if (i < text.Length && text[i] != '\n')
                continue;

            int len = i - start;
            // 跳过纯空行（与 logcat 里空行通常不单独成条一致）
            if (len > 0)
            {
                if (!first)
                    sb.Append('\n');
                first = false;
                sb.Append(prefix);
                sb.Append(text, start, len);
            }
            else if (len == 0 && i < text.Length)
            {
                // 连续 \n：忽略
            }

            start = i + 1;
        }
    }

    static char PriorityChar(LogType type)
    {
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                return 'E';
            case LogType.Warning:
                return 'W';
            case LogType.Assert:
                return 'E';
            default:
                return 'I';
        }
    }

    static Color ColorFor(LogType type)
    {
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                return new Color(1f, 0.45f, 0.45f, 1f);
            case LogType.Warning:
                return new Color(1f, 0.9f, 0.35f, 1f);
            case LogType.Assert:
                return new Color(1f, 0.6f, 0.2f, 1f);
            default:
                return new Color(0.95f, 0.95f, 0.95f, 1f);
        }
    }

    void EnsureStyles(float scale)
    {
        int fs = Mathf.Max(18, Mathf.RoundToInt(fontSize * scale));
        int btnFs = Mathf.Max(20, Mathf.RoundToInt(22 * scale));
        int copyFs = Mathf.Max(16, Mathf.RoundToInt(18 * scale));

        if (_panelBg == null)
        {
            _panelBg = MakeTex(new Color(0f, 0f, 0f, 0.82f));
            _buttonBg = MakeTex(new Color(0.2f, 0.2f, 0.22f, 0.92f));
            _buttonBgOn = MakeTex(new Color(0.15f, 0.45f, 0.75f, 0.95f));
            _rowBg = MakeTex(new Color(1f, 1f, 1f, 0.04f));
            _rowBgAlt = MakeTex(new Color(1f, 1f, 1f, 0.08f));
            _rowBgHighlight = MakeTex(new Color(0.2f, 0.55f, 0.3f, 0.55f));
            _toastBg = MakeTex(new Color(0.1f, 0.55f, 0.25f, 0.92f));
        }

        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fs,
                wordWrap = true,
                richText = false,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(8, 8, 6, 6)
            };
            _labelStyle.normal.textColor = Color.white;
        }
        else
        {
            _labelStyle.fontSize = fs;
        }

        if (_buttonStyle == null)
        {
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = btnFs,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 8, 8)
            };
            _buttonStyle.normal.textColor = Color.white;
            _buttonStyle.hover.textColor = Color.white;
            _buttonStyle.active.textColor = Color.white;
            _buttonStyle.normal.background = _buttonBg;
            _buttonStyle.hover.background = _buttonBgOn;
            _buttonStyle.active.background = _buttonBgOn;
        }
        else
        {
            _buttonStyle.fontSize = btnFs;
        }

        if (_copyBtnStyle == null)
        {
            _copyBtnStyle = new GUIStyle(_buttonStyle)
            {
                fontSize = copyFs,
                fontStyle = FontStyle.Normal,
                padding = new RectOffset(4, 4, 4, 4)
            };
            _copyBtnStyle.normal.background = _buttonBg;
            _copyBtnStyle.hover.background = _buttonBgOn;
            _copyBtnStyle.active.background = _buttonBgOn;
        }
        else
        {
            _copyBtnStyle.fontSize = copyFs;
        }

        if (_titleStyle == null)
        {
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = btnFs,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _titleStyle.normal.textColor = new Color(0.85f, 0.9f, 1f, 1f);
        }
        else
        {
            _titleStyle.fontSize = btnFs;
        }

        if (_toastStyle == null)
        {
            _toastStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = btnFs,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            _toastStyle.normal.textColor = Color.white;
            _toastStyle.normal.background = _toastBg;
        }
        else
        {
            _toastStyle.fontSize = btnFs;
        }
    }

    static Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        t.SetPixel(0, 0, c);
        t.Apply(false, true);
        t.hideFlags = HideFlags.HideAndDontSave;
        return t;
    }

    void OnGUI()
    {
        // 设计分辨率缩放，保证各机型控件尺寸一致
        float scaleX = Screen.width / DesignWidth;
        float scaleY = Screen.height / DesignHeight;
        float scale = Mathf.Clamp(Mathf.Min(scaleX, scaleY), 0.45f, 2.5f);

        // 用屏幕实际像素绘制按钮，避免矩阵缩放导致触摸热区偏差
        float pad = EdgePadding * scale;
        float btnH = ButtonHeight * scale;
        float btnW = ButtonWidth * scale;
        float gap = ButtonGap * scale;

        EnsureStyles(scale);

        // 右上角：显示/隐藏
        Rect toggleRect = new Rect(Screen.width - pad - btnW, pad, btnW, btnH);
        string toggleLabel = visible ? "隐藏" : "日志";
        if (GUI.Button(toggleRect, toggleLabel, _buttonStyle))
        {
            visible = !visible;
        }

        if (!visible)
            return;

        // 面板：尽量占满下方区域，仅顶部留出「隐藏」按钮
        float topReserve = pad + btnH + gap; // 给右上角切换按钮留空
        float maxPanelH = Screen.height - topReserve - pad;
        float panelH = Mathf.Min(Screen.height * panelHeightRatio, maxPanelH);
        float panelY = Screen.height - panelH - pad * 0.5f;
        float panelW = Screen.width - pad; // 左右更贴边，滑动区更宽
        float panelX = pad * 0.5f;
        float innerPad = pad * 0.5f;

        // 顶栏：标题 + 全复制 / 清空 / 底 / 自动（尽量矮，把高度留给滚动区）
        float barH = btnH;
        Rect panelRect = new Rect(panelX, panelY, panelW, panelH);
        GUI.DrawTexture(panelRect, _panelBg);

        Rect barRect = new Rect(panelX + innerPad, panelY + innerPad, panelW - innerPad * 2f, barH);
        GUI.Label(new Rect(barRect.x, barRect.y, barRect.width * 0.28f, barH),
            $"Log ({_entries.Count})", _titleStyle);

        float right = barRect.xMax;
        Rect clearRect = new Rect(right - btnW, barRect.y, btnW, btnH);
        Rect copyAllRect = new Rect(clearRect.x - gap - btnW, barRect.y, btnW, btnH);
        Rect bottomRect = new Rect(copyAllRect.x - gap - btnW, barRect.y, btnW, btnH);
        Rect autoRect = new Rect(bottomRect.x - gap - btnW, barRect.y, btnW, btnH);

        // 自动滚动开关（按下高亮）
        GUIStyle autoStyle = new GUIStyle(_buttonStyle);
        if (_autoScroll)
            autoStyle.normal.background = _buttonBgOn;

        if (GUI.Button(autoRect, _autoScroll ? "自动·开" : "自动·关", autoStyle))
        {
            _autoScroll = !_autoScroll;
            if (_autoScroll)
                ScrollToBottom();
        }

        if (GUI.Button(bottomRect, "到底", _buttonStyle))
        {
            _autoScroll = true;
            ScrollToBottom();
        }

        if (GUI.Button(copyAllRect, "全复制", _buttonStyle))
            CopyAllLogs();

        if (GUI.Button(clearRect, "清空", _buttonStyle))
        {
            _entries.Clear();
            _dirty = true;
            _scrollPosition = Vector2.zero;
            _highlightIndex = -1;
        }

        // 日志滚动区：占满顶栏以下的全部面板空间
        float scrollTop = barRect.yMax + gap * 0.5f;
        float scrollBottom = panelY + panelH - innerPad;
        float scrollH = Mathf.Max(80f * scale, scrollBottom - scrollTop);
        Rect scrollView = new Rect(panelX + innerPad, scrollTop, panelW - innerPad * 2f, scrollH);

        float lineH = _labelStyle.fontSize + 12f * scale;
        float blockPad = 6f * scale;
        float copyBtnW = CopyBtnWidth * scale;
        float copyBtnH = Mathf.Max(lineH, 40f * scale);
        // 右侧留出细指示条与复制按钮宽度
        float indicatorW = 10f * scale;
        float textW = scrollView.width - indicatorW - copyBtnW - gap;
        float contentW = scrollView.width - indicatorW;

        // 每段高度 = 文本高度 + 段间距
        float contentH = 0f;
        for (int i = 0; i < _entries.Count; i++)
            contentH += EstimateHeight(_entries[i].text, textW, lineH) + blockPad;
        contentH = Mathf.Max(contentH, scrollView.height);

        // 内容比视口宽一点即可，竖向滚动条可关：整区拖动才是主交互
        Rect contentRect = new Rect(0, 0, contentW, contentH);
        float maxScrollY = Mathf.Max(0f, contentH - scrollView.height);

        // 整区手指/鼠标拖拽滑动（必须在 BeginScrollView 之前处理）
        HandleScrollAreaInput(scrollView, maxScrollY, scale);

        // 新日志且开启自动滚底：贴到底部
        if (_autoScroll && _dirty && Event.current.type == EventType.Layout)
            _scrollPosition.y = maxScrollY;

        _scrollPosition.y = Mathf.Clamp(_scrollPosition.y, 0f, maxScrollY);

        // 不强制显示右侧滚动条；仍可用滚轮，主交互靠整区拖拽
        _scrollPosition = GUI.BeginScrollView(
            scrollView,
            _scrollPosition,
            contentRect,
            GUIStyle.none,
            GUIStyle.none);

        float y = 0f;
        float now = Time.realtimeSinceStartup;
        if (_highlightIndex >= 0 && now > _highlightUntil)
            _highlightIndex = -1;

        // 本帧是否因拖动而禁止「复制」点击
        bool blockCopyClick = _scrollDragMoved;

        for (int i = 0; i < _entries.Count; i++)
        {
            LogEntry e = _entries[i];
            float textH = EstimateHeight(e.text, textW, lineH);
            // 段高至少能放下复制按钮
            float h = Mathf.Max(textH, copyBtnH);
            Rect blockRect = new Rect(0, y, contentW, h);

            // 斑马纹 / 刚复制高亮（整段一块）
            Texture2D rowBg = i == _highlightIndex
                ? _rowBgHighlight
                : (i % 2 == 0 ? _rowBg : _rowBgAlt);
            GUI.DrawTexture(blockRect, rowBg);

            _labelStyle.normal.textColor = e.color;
            GUI.Label(new Rect(0, y, textW, h), e.text, _labelStyle);

            // 每段右上角一个「复制」，复制整段内容
            Rect copyRect = new Rect(textW + gap * 0.5f, y + 2f * scale, copyBtnW, copyBtnH);
            if (GUI.Button(copyRect, "复制", _copyBtnStyle) && !blockCopyClick)
                CopyLog(i);

            y += h + blockPad;
        }

        GUI.EndScrollView();

        // 拖动手势结束后复位，避免一直挡住「复制」
        if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.Ignore)
            _scrollDragMoved = false;

        // 可选：右侧细滚动指示条（仅位置反馈，不必拖它）
        if (maxScrollY > 1f)
        {
            float trackW = 6f * scale;
            float trackX = scrollView.xMax - trackW - 2f * scale;
            float trackH = scrollView.height;
            float thumbH = Mathf.Max(28f * scale, trackH * (scrollView.height / contentH));
            float thumbY = scrollView.y + (trackH - thumbH) * (_scrollPosition.y / maxScrollY);
            GUI.DrawTexture(new Rect(trackX, scrollView.y, trackW, trackH), _rowBgAlt);
            GUI.DrawTexture(new Rect(trackX, thumbY, trackW, thumbH), _buttonBgOn);
        }

        if (_dirty && Event.current.type == EventType.Repaint)
            _dirty = false;

        // 复制成功 Toast（屏幕中上部）
        if (!string.IsNullOrEmpty(_toastText) && Time.realtimeSinceStartup < _toastUntil)
        {
            float tw = Mathf.Min(Screen.width * 0.7f, 520f * scale);
            float th = 56f * scale;
            Rect toastRect = new Rect((Screen.width - tw) * 0.5f, Screen.height * 0.18f, tw, th);
            GUI.Label(toastRect, _toastText, _toastStyle);
        }
        else if (Time.realtimeSinceStartup >= _toastUntil)
        {
            _toastText = null;
        }
    }

    void CopyLog(int index)
    {
        if (index < 0 || index >= _entries.Count)
            return;

        string text = _entries[index].text ?? string.Empty;
        GUIUtility.systemCopyBuffer = text;
        _highlightIndex = index;
        _highlightUntil = Time.realtimeSinceStartup + copyToastSeconds;
        ShowToast("已复制本段(Logcat)");
    }

    void CopyAllLogs()
    {
        if (_entries.Count == 0)
        {
            ShowToast("暂无日志");
            return;
        }

        var sb = new System.Text.StringBuilder(_entries.Count * 64);
        for (int i = 0; i < _entries.Count; i++)
        {
            if (i > 0) sb.Append("\n----------\n");
            sb.Append(_entries[i].text);
        }

        GUIUtility.systemCopyBuffer = sb.ToString();
        _highlightIndex = -1;
        ShowToast($"已复制全部 {_entries.Count} 段(Logcat)");
    }

    void ShowToast(string msg)
    {
        _toastText = msg;
        _toastUntil = Time.realtimeSinceStartup + copyToastSeconds;
    }

    void ScrollToBottom()
    {
        // 内容高度在下一帧 OnGUI 里算准后再贴底；这里标脏即可
        _dirty = true;
        _autoScroll = true;
    }

    /// <summary>
    /// 在滚动视口任意位置拖拽即可滑动；滚轮同样支持。
    /// 拖动超过阈值后抑制本手势的「复制」点击，避免误触。
    /// </summary>
    void HandleScrollAreaInput(Rect scrollView, float maxScrollY, float scale)
    {
        Event e = Event.current;
        if (e == null)
            return;

        Vector2 mouse = e.mousePosition;
        float threshold = ScrollDragThreshold * Mathf.Max(1f, scale);

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0 && scrollView.Contains(mouse))
                {
                    _scrollPointerDown = true;
                    _scrollDragMoved = false;
                    _scrollDragLastGui = mouse;
                }
                break;

            case EventType.MouseDrag:
                if (!_scrollPointerDown || e.button != 0)
                    break;

                Vector2 delta = mouse - _scrollDragLastGui;
                if (!_scrollDragMoved && delta.sqrMagnitude >= threshold * threshold)
                    _scrollDragMoved = true;

                if (_scrollDragMoved)
                {
                    // 手指上滑（mouse.y 减小）→ 看更下方内容 → scrollPosition.y 增大
                    // 故：scrollY -= delta.y（delta.y 为 GUI 坐标增量，向下为正）
                    _scrollPosition.y = Mathf.Clamp(_scrollPosition.y - delta.y, 0f, maxScrollY);
                    _scrollDragLastGui = mouse;

                    if (_scrollPosition.y < maxScrollY - 48f * scale)
                        _autoScroll = false;

                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (e.button == 0 && _scrollPointerDown)
                {
                    // 拖过则吃掉 Up，降低按钮误点；阈值内抬起仍可点「复制」
                    if (_scrollDragMoved)
                        e.Use();
                    _scrollPointerDown = false;
                    // _scrollDragMoved 保留到本帧按钮判定后再清
                }
                break;

            case EventType.ScrollWheel:
                if (scrollView.Contains(mouse))
                {
                    _scrollPosition.y = Mathf.Clamp(
                        _scrollPosition.y + e.delta.y * 24f * scale,
                        0f,
                        maxScrollY);
                    if (_scrollPosition.y < maxScrollY - 48f * scale)
                        _autoScroll = false;
                    e.Use();
                }
                break;
        }
    }

    float EstimateHeight(string text, float width, float lineH)
    {
        if (string.IsNullOrEmpty(text))
            return lineH;

        // 按硬换行分段，再估算每段因 wordWrap 产生的视觉行数
        float avgCharW = _labelStyle.fontSize * 0.55f;
        int charsPerLine = Mathf.Max(8, Mathf.FloorToInt(width / avgCharW));
        int totalLines = 0;
        string[] hardLines = text.Split('\n');
        for (int i = 0; i < hardLines.Length; i++)
        {
            int len = hardLines[i].Length;
            if (len == 0)
                totalLines += 1;
            else
                totalLines += Mathf.Max(1, Mathf.CeilToInt(len / (float)charsPerLine));
        }

        return Mathf.Max(1, totalLines) * lineH;
    }

    /// <summary>外部可调用：显示面板</summary>
    public void Show() => visible = true;

    /// <summary>外部可调用：隐藏面板</summary>
    public void Hide() => visible = false;

    /// <summary>外部可调用：切换显示</summary>
    public void Toggle() => visible = !visible;
}
