using UnityEngine;

/// Add to an empty GameObject in your scene.
[RequireComponent(typeof(SpriteRenderer))]
public class SkyGenerator : MonoBehaviour
{
    [Header("Canvas / Fit")]
    public Camera cam;
    public int texWidth = 512;     // reduce if perf needed
    public int texHeight = 256;

    [Header("Gradient (top -> bottom)")]
    public Color topColor = new Color(0.55f, 0.78f, 1.0f);   // light sky blue
    public Color bottomColor = new Color(0.90f, 0.97f, 1.0f); // near white

    [Header("Clouds")]
    [Range(0f, 1f)] public float cloudDensity = 0.55f; // higher = more clouds
    public float cloudSharpness = 3.0f;               // edge contrast
    public float noiseScale = 2.0f;                   // size of clouds
    public float noiseOctaves = 3;                    // 1–5 is fine
    public float octaveFalloff = 0.5f;                // persistence
    public float driftSpeedX = 0.02f;                 // units / sec
    public float driftSpeedY = 0.0f;

    [Header("Cloud Color/Alpha")]
    public Color cloudColor = Color.white;
    [Range(0f, 1f)] public float maxCloudAlpha = 0.85f;

    Texture2D tex;
    SpriteRenderer sr;
    Vector2 noiseOffset;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        sr = GetComponent<SpriteRenderer>();

        tex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, texWidth, texHeight), new Vector2(0.5f, 0.5f), 100f);

        // Fit to camera width/height
        FitToCamera();

        // randomize starting cloud position so every run looks a bit different
        noiseOffset = new Vector2(Random.value * 1000f, Random.value * 1000f);

        // initial draw
        Redraw();
    }

    void Update()
    {
        // drift the noise sample to animate clouds
        noiseOffset += new Vector2(driftSpeedX, driftSpeedY) * Time.deltaTime;
        Redraw();
    }

    

    void FitToCamera()
    {
        float worldHeight = cam.orthographicSize * 2f;
        float worldWidth = worldHeight * cam.aspect;
        // scale sprite so it covers the full view
        var spriteSize = new Vector2(tex.width / 100f, tex.height / 100f); // pixelsPerUnit=100
        transform.localScale = new Vector3(worldWidth / spriteSize.x, worldHeight / spriteSize.y, 1f);
        transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        sr.sortingOrder = -100; // keep behind gameplay
    }

    void Redraw()
    {
        // write pixels
        Color[] pixels = new Color[texWidth * texHeight];
        for (int y = 0; y < texHeight; y++)
        {
            float v = (float)y / (texHeight - 1);

            // vertical gradient (top -> bottom)
            Color baseCol = Color.Lerp(bottomColor, topColor, v);

            for (int x = 0; x < texWidth; x++)
            {
                // fbm perlin for clouds
                float nx = (x / (float)texWidth) * noiseScale + noiseOffset.x;
                float ny = (y / (float)texHeight) * noiseScale + noiseOffset.y;

                float n = FBM(nx, ny, (int)noiseOctaves, octaveFalloff); // 0..1

                // push noise into "puffy" range:
                // - bias by density, then increase contrast by sharpness
                float cloudMask = Mathf.Clamp01((n + (1f - cloudDensity) - 0.5f) * cloudSharpness + 0.5f);

                // alpha controls how strong clouds sit over sky
                float a = cloudMask * maxCloudAlpha;

                // premix cloud color over gradient
                Color c = Color.Lerp(baseCol, cloudColor, a);
                pixels[y * texWidth + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false);
    }

    // Fractal Brownian Motion using Unity Perlin
    float FBM(float x, float y, int octaves, float falloff)
    {
        float amp = 0.5f;
        float freq = 1f;
        float sum = 0f;
        float norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += Mathf.PerlinNoise(x * freq, y * freq) * amp;
            norm += amp;
            amp *= falloff;
            freq *= 2f;
        }
        return sum / Mathf.Max(norm, 1e-5f);
    }
}
