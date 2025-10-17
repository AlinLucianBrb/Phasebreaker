using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public class LevelPatternGenerator : MonoBehaviour
{
    public static LevelPatternGenerator Instance { get; private set; }
    public enum FillMode { RowsCols, TotalCount }
    public enum Pattern
    {
        Solid, Checker, Diamond, Triangle, ZigZag, Waves, Rings, Meander, RandomHoles
    }

    [Header("Basics")]
    public Camera cam;
    public GameObject brickPrefab;
    public bool generateOnStart = true;

    [Header("Sizing & Layout")]
    public FillMode fillMode = FillMode.RowsCols;
    [Min(1)] public int rows = 6;
    [Min(1)] public int cols = 10;
    [Min(1)] public int totalBricks = 60;        // used when FillMode = TotalCount
    [Range(0.2f, 1f)] public float screenFill = 0.85f; // % of screen width used
    [Range(0f, 0.2f)] public float margin = 0.05f;     // world-units-ish fraction of screen height
    [Range(0f, 1f)] public float spacing = 0.02f;    // space between bricks (world units)

    [Header("Pattern")]
    public Pattern pattern = Pattern.Solid;
    [Range(0f, 1f)] public float patternDensity = 0.75f; // used for RandomHoles / Waves amplitude etc.
    public int seed = 12345;
    public bool centerVertically = true;

    [Header("Per-Brick Options (optional)")]
    public bool assignHP = true;
    public int hpMin = 1;
    public int hpMax = 5;

    public List<GameObject> spawned;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (!cam) cam = Camera.main;

        spawned = new List<GameObject>();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        if (!brickPrefab || !cam) { Debug.LogWarning("Assign Camera + Brick Prefab"); return; }

        // clear old
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            DestroyImmediate(c.gameObject);
        }
        spawned.Clear();

        // Determine rows/cols
        int R = rows, C = cols;
        if (fillMode == FillMode.TotalCount)
        {
            // make a near-square grid that matches aspect
            float aspect = cam.aspect;
            C = Mathf.CeilToInt(Mathf.Sqrt(totalBricks * aspect));
            C = Mathf.Max(1, C);
            R = Mathf.CeilToInt((float)totalBricks / C);
        }

        // Camera world-space rect
        float worldH = cam.orthographicSize * 2f;
        float worldW = worldH * cam.aspect;

        // usable area (apply margins and screenFill)
        float usableW = worldW * screenFill;
        float usableH = worldH * (centerVertically ? screenFill : 1f) - (worldH * margin);

        // cell size
        float totalSpacingX = spacing * Mathf.Max(0, C - 1);
        float totalSpacingY = spacing * Mathf.Max(0, R - 1);
        float cellW = (usableW - totalSpacingX) / C;
        float cellH = (usableH - totalSpacingY) / R;
        float cellSize = Mathf.Min(cellW, cellH); // bricks are square-ish

        // top-left anchor
        Vector3 center = cam.transform.position; center.z = 0;
        float gridW = C * cellSize + totalSpacingX;
        float gridH = R * cellSize + totalSpacingY;
        Vector3 origin = new(
            center.x - gridW * 0.5f + cellSize * 0.5f,
            (centerVertically ? center.y : (center.y + worldH * 0.5f - gridH * 0.5f - worldH * margin * 0.5f)) + gridH * 0.5f - cellSize * 0.5f,
            0f
        );

        // prefab sprite size (world units, scale=1)
        var srPrefab = brickPrefab.GetComponentInChildren<SpriteRenderer>();
        if (!srPrefab) { Debug.LogWarning("Brick prefab needs a SpriteRenderer"); return; }
        Vector2 spriteSize = srPrefab.sprite.bounds.size;
        float scaleFactor = (spriteSize.x > 1e-5f) ? (cellSize / spriteSize.x) : 1f;

        // RNG for patterns
        System.Random rng = new System.Random(seed);
        int placed = 0, toPlace = (fillMode == FillMode.TotalCount) ? totalBricks : (R * C);

        // Build
        for (int r = 0; r < R; r++)
            for (int c = 0; c < C; c++)
            {
                // stop if we’ve placed enough (TotalCount mode)
                if (fillMode == FillMode.TotalCount && placed >= totalBricks) break;

                bool place = ShouldPlace(pattern, r, c, R, C, patternDensity, rng);

                if (!place) continue;

                // position
                float x = origin.x + c * (cellSize + spacing);
                float y = origin.y - r * (cellSize + spacing);
                var go = Instantiate(brickPrefab, new Vector3(x, y, 0f), Quaternion.identity, transform);

                // scale uniformly so sprite fits cell
                go.transform.localScale = Vector3.one * scaleFactor;

                var col = go.GetComponent<BoxCollider2D>();
                if (col)
                {
                    // reset collider so it fits sprite bounds at current scale
                    var sr = go.GetComponent<SpriteRenderer>();
                    if (sr && sr.sprite)
                    {
                        Vector2 s = sr.sprite.bounds.size;
                        col.size = s;
                        col.offset = Vector2.zero;
                    }
                }

                // optional HP
                if (assignHP)
                {
                    int hp = Random.Range(hpMin, hpMax + 1);
                    go.GetComponent<BrickHealth>().SetHP(hp, hp);
                }

                spawned.Add(go);
                placed++;
            }
    }

    bool ShouldPlace(Pattern p, int r, int c, int R, int C, float density, System.Random rng)
    {
        switch (p)
        {
            case Pattern.Solid:
                return true;

            case Pattern.Checker:
                return ((r + c) % 2 == 0);

            case Pattern.Diamond:
                {
                    // diamond centered in grid; thinner with lower density
                    float cr = (R - 1) * 0.5f;
                    float cc = (C - 1) * 0.5f;
                    float manhattan = Mathf.Abs(r - cr) + Mathf.Abs(c - cc);
                    float maxRadius = Mathf.Lerp(0.35f, 0.95f, density) * (Mathf.Min(R, C) * 0.5f);
                    return manhattan <= maxRadius;
                }

            case Pattern.Triangle:
                {
                    // pyramid up
                    int baseWidth = Mathf.RoundToInt(Mathf.Lerp(1, C, density));
                    int widthAtRow = Mathf.RoundToInt(Mathf.Lerp(baseWidth, C, (float)r / Mathf.Max(1, R - 1)));
                    int left = (C - widthAtRow) / 2;
                    return (c >= left && c < left + widthAtRow);
                }

            case Pattern.ZigZag:
                {
                    // staggered rows
                    int shift = (int)Mathf.Floor((r % 2 == 0 ? 0f : 0.5f) + 0.001f);
                    int effectiveC = C - 1;
                    float cutoff = Mathf.Lerp(0.6f, 1f, density);
                    return (c + shift) % 2 == 0 && (float)r / Mathf.Max(1, R - 1) <= cutoff;
                }

            case Pattern.Waves:
                {
                    // sine wave band; density controls band thickness
                    float t = (float)r / Mathf.Max(1, R - 1);
                    float mid = (Mathf.Sin((c / (float)C) * Mathf.PI * 2f) * 0.5f + 0.5f);
                    float band = Mathf.Lerp(0.15f, 0.45f, density);
                    return Mathf.Abs(t - mid) <= band;
                }

            case Pattern.Rings:
                {
                    // concentric rings from center
                    float cr = (R - 1) * 0.5f;
                    float cc = (C - 1) * 0.5f;
                    float dist = Mathf.Sqrt((r - cr) * (r - cr) + (c - cc) * (c - cc));
                    float ringWidth = Mathf.Lerp(0.6f, 1.2f, density);
                    int band = Mathf.FloorToInt(dist / ringWidth);
                    return (band % 2 == 0);
                }

            case Pattern.Meander:
                {
                    // Greek key–like horizontal runs with periodic turns
                    int period = Mathf.Max(2, Mathf.RoundToInt(Mathf.Lerp(3f, 6f, 1f - density)));
                    bool horizontal = (r / period) % 2 == 0;
                    if (horizontal) return true;
                    // vertical connectors
                    int col = (r % period == period - 1) ? (c / period) * period : -1;
                    return (col >= 0 && c == col);
                }

            case Pattern.RandomHoles:
            default:
                {
                    // keep roughly "density" fill
                    double chance = density;
                    return rng.NextDouble() < chance;
                }
        }
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(LevelPatternGenerator))]
public class LevelPatternGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var gen = (LevelPatternGenerator)target;

        if (GUILayout.Button("Rebuild Now"))
        {
            // Call the method directly (no SendMessage)
            gen.Generate();
        }
    }
}
#endif