using UnityEngine;

/// <summary>
/// Play-mode note for the Volume study scenes. Uses an OS font so Chinese renders in IMGUI.
/// </summary>
public class VolumeStudyNote : MonoBehaviour
{
    public string title = "Volume";

    [TextArea(8, 24)]
    public string body = "";

    public float panelHeight = 360f;

    static Font sharedFont;
    GUIStyle boxStyle;
    GUIStyle titleStyle;
    GUIStyle bodyStyle;
    Vector2 scroll;

    void OnGUI()
    {
        EnsureStyles();
        float height = Mathf.Min(panelHeight, Screen.height - 24f);
        GUILayout.BeginArea(new Rect(12f, 12f, 700f, height), boxStyle);
        GUILayout.Label(title, titleStyle);
        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.Label(body, bodyStyle);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void EnsureStyles()
    {
        if (sharedFont == null)
        {
            sharedFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" },
                18);
        }

        if (boxStyle != null)
            return;

        boxStyle = new GUIStyle(GUI.skin.box)
        {
            font = sharedFont,
            fontSize = 16,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true,
            padding = new RectOffset(14, 14, 12, 12)
        };
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            font = sharedFont,
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };
        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            font = sharedFont,
            fontSize = 16,
            wordWrap = true
        };
    }
}
