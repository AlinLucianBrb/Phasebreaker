using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.ContentSizeFitter;

[ExecuteAlways]
public class BrickStyle : MonoBehaviour
{
    [Range(0f, 1f)] public float maxDarkenAmount = 0.6f;

    SpriteRenderer sr;
    Image uiImage;
    MaterialPropertyBlock mpb;

    Color currentTint;
    public enum TintMode { UseBaseColor, PaletteByMaxHP }
    public TintMode tintMode = TintMode.PaletteByMaxHP;
    [Tooltip("Palette by maxHP (1..5). Index 0 unused.")]
    public Color[] paletteByMaxHP = new Color[6];

    void Awake()
    {
        if (!uiImage) uiImage = GetComponent<Image>();
        if (!sr) sr = GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
        EnsurePalette();
    }
    void EnsurePalette()
    {
        if (paletteByMaxHP == null || paletteByMaxHP.Length < 6) paletteByMaxHP = new Color[6];
        if (paletteByMaxHP[1].a == 0f) paletteByMaxHP[1] = new Color(0.75f, 0.85f, 1.00f);
        if (paletteByMaxHP[2].a == 0f) paletteByMaxHP[2] = new Color(0.75f, 1.00f, 0.75f);
        if (paletteByMaxHP[3].a == 0f) paletteByMaxHP[3] = new Color(1.00f, 0.97f, 0.70f);
        if (paletteByMaxHP[4].a == 0f) paletteByMaxHP[4] = new Color(1.00f, 0.82f, 0.65f);
        if (paletteByMaxHP[5].a == 0f) paletteByMaxHP[5] = new Color(1.00f, 0.70f, 0.70f);
    }

    Color GetPaletteColorForMaxHP(int maxHP)
    {
        int idx = Mathf.Clamp(maxHP, 1, 5);
        EnsurePalette();
        return paletteByMaxHP[idx];
    }

    public void ApplyHP(int hp, int maxHP)
    {
        sr.GetPropertyBlock(mpb);
        Color baseHue = (tintMode == TintMode.PaletteByMaxHP) ? GetPaletteColorForMaxHP(maxHP) : sr.color;
        float amt = (float)hp/(float)maxHP;

        Color darkest = baseHue * maxDarkenAmount;
        currentTint = Color.Lerp(darkest, baseHue, amt);
        mpb.SetColor("_Color", currentTint);
        sr.SetPropertyBlock(mpb);
    }
}
    
