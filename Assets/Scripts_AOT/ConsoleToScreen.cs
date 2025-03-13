using UnityEngine;
using System.Collections.Generic;
using System;

public class ConsoleToScreen : MonoBehaviour
{
    const int maxLines = 50;
    private readonly List<string> _lines = new List<string>();
    private string _logStr = "";
    private Vector2 _scrollPosition;
    private bool _needsScrollToBottom;
    public int fontSize = 30;

    void OnEnable() => Application.logMessageReceived += Log;
    void OnDisable() => Application.logMessageReceived -= Log;

    public void Log(string logString, string stackTrace, LogType type)
    {
        // 添加新日志时标记需要滚动
        //_needsScrollToBottom = true;

        foreach (var line in logString.Split('\n'))
        {
            _lines.AddRange(SplitLine(line, 100));
        }

        // 保持最多maxLines行
        if (_lines.Count > maxLines)
            _lines.RemoveRange(0, _lines.Count - maxLines);

        _logStr = string.Join("\n", _lines);
    }

    private IEnumerable<string> SplitLine(string line, int maxChars)
    {
        for (int i = 0; i < line.Length; i += maxChars)
            yield return line.Substring(i, Math.Min(maxChars, line.Length - i));
    }

    void OnGUI()
    {
        // 分辨率适配
        float scaleX = Screen.width / 1080f;
        float scaleY = Screen.height / 1920f;
        GUI.matrix = Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.identity,
            new Vector3(scaleX, scaleY, 1)
        );

        // 字体设置
        int scaledFontSize = (int)(fontSize * Mathf.Min(scaleX, scaleY));
        GUIStyle style = new GUIStyle()
        {
            fontSize = Mathf.Max(14, scaledFontSize),
            normal = new GUIStyleState() { textColor = Color.white },
            wordWrap = true,
            clipping = TextClipping.Overflow
        };

        // 半透明背景
        Texture2D bg = new Texture2D(1, 1);
        bg.SetPixel(0, 0, new Color(0, 0, 0, 0.7f));
        bg.Apply();
        style.normal.background = bg;

        // 滚动视图参数
        Rect viewRect = new Rect(10, 1200, 1060, 600); // 可视区域
        float contentHeight = _lines.Count * (scaledFontSize + 2); // 内容高度

        // 自动滚动逻辑
        if (_needsScrollToBottom && Event.current.type == EventType.Repaint)
        {
            _scrollPosition.y = Mathf.Max(0, contentHeight - viewRect.height);
            _needsScrollToBottom = false;
        }

        // 绘制滚动视图
        _scrollPosition = GUI.BeginScrollView(
            viewRect,
            _scrollPosition,
            new Rect(0, 0, viewRect.width - 20, contentHeight)
        );

        GUI.Label(
            new Rect(0, 0, viewRect.width - 20, contentHeight),
            _logStr,
            style
        );

        GUI.EndScrollView();
    }
}