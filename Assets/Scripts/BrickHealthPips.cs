using UnityEngine;

[DisallowMultipleComponent]
public class BrickHealthPips : MonoBehaviour
{
    [Header("Appearance")]
    [Range(0.5f, 2f)] public float pipScale = 1f; // make dots bigger/smaller
    public Color activeColor = new(0.15f, 0.15f, 0.18f, 0.95f);
    public Color inactiveColor = new(0.15f, 0.15f, 0.18f, 0.25f);
    public bool showInactiveSlots = true;

    [Header("Visibility")]
    public bool showAfterDamage = true;   // NEW: start hidden, reveal on first hit
    public float revealFadeTime = 0.15f;  // quick fade-in

    static readonly float bandHeightFrac = 0.22f; // vertical size fraction
    static readonly float pipSizeFrac = 0.22f;    // diameter vs band height
    static readonly float spacingFrac = 0.18f;    // spacing vs pip diameter
    static readonly int orderOffset = 1;          // render above brick

    static Sprite dotSprite; // generated once
    SpriteRenderer brickSR;
    Transform rowRoot;
    bool revealed;           // have we shown the pips yet?

    void Awake()
    {
        brickSR = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (!brickSR) { enabled = false; return; }

        if (!rowRoot)
        {
            var go = new GameObject("HP_Pips");
            go.transform.SetParent(transform, false);
            rowRoot = go.transform;
        }

        EnsureDotSprite();

        // start hidden if we only show after damage
        SetVisible(!showAfterDamage);
        revealed = !showAfterDamage;
    }

    /// Call this every time HP changes (even if hidden).
    public void Refresh(int hp, int maxHP)
    {
        if (!brickSR) return;

        // rebuild children
        for (int i = rowRoot.childCount - 1; i >= 0; i--)
        {
            var c = rowRoot.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
        }

        // brick local size
        Vector2 world = brickSR.bounds.size;
        Vector3 lossy = transform.lossyScale;
        Vector2 local = new(world.x / Mathf.Max(1e-4f, lossy.x),
                            world.y / Mathf.Max(1e-4f, lossy.y));

        float bandH = Mathf.Clamp(local.y * bandHeightFrac, 0.01f, local.y);
        float pipDia = Mathf.Clamp(bandH * pipSizeFrac * pipScale, 0.005f, bandH);
        float spacing = pipDia * spacingFrac;

        int totalSlots = Mathf.Max(1, maxHP);
        float totalWidth = totalSlots * pipDia + (totalSlots - 1) * spacing;
        float startX = -totalWidth * 0.5f;
        float y = 0f; // centered vertically

        for (int i = 0; i < totalSlots; i++)
        {
            bool filled = i < hp;
            if (!showInactiveSlots && !filled) continue;

            var go = new GameObject(filled ? $"Pip_{i + 1}" : $"Ghost_{i + 1}");
            go.transform.SetParent(rowRoot, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = dotSprite;
            sr.sortingLayerID = brickSR.sortingLayerID;
            sr.sortingOrder = brickSR.sortingOrder + orderOffset;
            sr.color = filled ? activeColor : inactiveColor;

            go.transform.localScale = Vector3.one * (pipDia / dotSprite.bounds.size.x);
            go.transform.localPosition = new Vector3(startX + i * (pipDia + spacing), y, 0f);
        }
    }

    /// Call this from your health script the first time the brick takes damage.
    public void Reveal()
    {
        if (revealed) return;
        revealed = true;
        if (!gameObject.activeInHierarchy) { SetVisible(true); return; }
        // fade in
        SetVisible(true);
        if (revealFadeTime > 0f) StartCoroutine(FadeIn());
    }

    System.Collections.IEnumerator FadeIn()
    {
        // multiply alpha of each child from 0 -> 1
        var srs = rowRoot.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs) { var c = sr.color; c.a = 0f; sr.color = c; }

        float t = 0f;
        while (t < revealFadeTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / revealFadeTime);
            foreach (var sr in srs)
            {
                var baseCol = sr.name.StartsWith("Pip_") ? activeColor : inactiveColor;
                var c = baseCol; c.a *= a;
                sr.color = c;
            }
            yield return null;
        }
        // ensure final
        foreach (var sr in srs)
        {
            var baseCol = sr.name.StartsWith("Pip_") ? activeColor : inactiveColor;
            sr.color = baseCol;
        }
    }

    void SetVisible(bool on)
    {
        if (!rowRoot) return;
        rowRoot.gameObject.SetActive(on);
    }

    static void EnsureDotSprite()
    {
        if (dotSprite) return;
        int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var px = new Color[s * s];
        Vector2 c = new(s * 0.5f, s * 0.5f);
        float r = s * 0.45f, r2 = r * r;

        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = x - c.x, dy = y - c.y, d2 = dx * dx + dy * dy;
                float a = Mathf.Clamp01(Mathf.InverseLerp(r2 * 1.08f, r2, d2));
                px[y * s + x] = new Color(1, 1, 1, a);
            }
        tex.SetPixels(px);
        tex.Apply(false);
        dotSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }
}
