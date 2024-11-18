using UnityEngine;

public static class ColorUtils
{
    /// <summary>
    /// color 转换hex
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public static string ColorToHex(Color color)
    {
        int r = Mathf.RoundToInt(color.r * 255.0f);
        int g = Mathf.RoundToInt(color.g * 255.0f);
        int b = Mathf.RoundToInt(color.b * 255.0f);
        int a = Mathf.RoundToInt(color.a * 255.0f);
        string hex = string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", r, g, b, a);
        return hex;
    }

    /// <summary>
    /// hex转换到color
    /// </summary>
    /// <param name="hex"></param>
    /// <returns></returns>
    public static Color HexToColor(string hex, float a = 1f)
    {
        hex = hex.TrimStart('#');
        byte br = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte bg = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte bb = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        float r = br / 255f;
        float g = bg / 255f;
        float b = bb / 255f;
        //float a = 1f;
        if (hex.Length >= 8)
            a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber) / 255f;

        return new Color(r, g, b, a);
    }

    public static Color Color(int R, int G, int B, int A = 255)
    {
        return Color(R * 1.0f, G * 1.0f, B * 1.0f, A * 1.0f);
    }

    public static Color Color(float R, float G, float B, float A = 255f)
    {
        return new Color(R / 255f, G / 255f, B / 255f, A / 255f);
    }
}
