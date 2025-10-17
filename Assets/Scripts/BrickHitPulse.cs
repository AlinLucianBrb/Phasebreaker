using UnityEngine;

[DisallowMultipleComponent]
public class BrickHitPulse : MonoBehaviour
{
    [Header("Scale Pulse")]
    public float punchScale = 1.15f;      // how big it gets on impact
    public float duration = 0.12f;      // total up+down time
    [Range(0f, 1f)] public float easeOutPortion = 0.35f; // 0..1, how much of time is the "expand" phase

    [Header("Flash (optional)")]
    public bool flash = true;
    public Color flashColor = Color.white;
    [Range(0f, 1f)] public float flashStrength = 0.6f;   // how much to blend toward flashColor
    public float flashFadeTime = 0.12f;

    Vector3 baseScale;
    SpriteRenderer sr;
    Color baseColor;

    void Awake()
    {
        baseScale = transform.localScale;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    public void Ping()
    {
        // restart animation if already running
        StopAllCoroutines();
        StartCoroutine(PulseRoutine());
        if (flash && sr) StartCoroutine(FlashRoutine());
    }

    System.Collections.IEnumerator PulseRoutine()
    {
        float t = 0f;
        float upTime = Mathf.Clamp01(easeOutPortion) * duration;
        float downTime = Mathf.Max(0.0001f, duration - upTime);

        // expand (ease-out)
        while (t < upTime)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / upTime);
            float eased = 1f - Mathf.Pow(1f - u, 3f); // cubic ease-out
            float s = Mathf.Lerp(1f, punchScale, eased);
            transform.localScale = baseScale * s;
            yield return null;
        }

        // contract (ease-in)
        t = 0f;
        while (t < downTime)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / downTime);
            float eased = u * u * u; // cubic ease-in
            float s = Mathf.Lerp(punchScale, 1f, eased);
            transform.localScale = baseScale * s;
            yield return null;
        }

        transform.localScale = baseScale;
    }

    System.Collections.IEnumerator FlashRoutine()
    {
        // quick brighten then fade back
        float t = 0f;
        float half = flashFadeTime * 0.4f;

        // snap toward flash
        while (t < half)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / half);
            sr.color = Color.Lerp(baseColor, Color.Lerp(baseColor, flashColor, flashStrength), 1f - (1f - u) * (1f - u));
            yield return null;
        }
        // fade back
        t = 0f;
        while (t < flashFadeTime)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / flashFadeTime);
            sr.color = Color.Lerp(sr.color, baseColor, u * u);
            yield return null;
        }
        sr.color = baseColor;
    }
}
