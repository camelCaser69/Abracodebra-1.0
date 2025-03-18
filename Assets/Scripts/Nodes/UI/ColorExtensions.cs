using UnityEngine;

public static class ColorExtensions
{
    public static Color WithAlpha(this Color c, float alpha)
    {
        return new Color(c.r, c.g, c.b, alpha);
    }
}