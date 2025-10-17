using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class RainbowColor : MonoBehaviour
{
    public float speed = 1f;
    public bool vertical = true; // false = horizontal gradient

    private TMP_Text text;

    // Your 4-color palette
    private readonly Color[] palette =
    {
        new Color(0.75f, 1.00f, 0.75f), // greenish
        new Color(1.00f, 0.97f, 0.70f), // yellowish
        new Color(1.00f, 0.82f, 0.65f), // peach
        new Color(1.00f, 0.70f, 0.70f)  // pink
    };

    void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (!text) return;

        // Find current and next palette colors
        float t = Mathf.PingPong(Time.time * speed, palette.Length - 1);
        int idx = Mathf.FloorToInt(t);
        int next = (idx + 1) % palette.Length;
        float lerp = t - idx;

        Color c1 = Color.Lerp(palette[idx], palette[next], lerp);
        Color c2 = Color.Lerp(palette[next], palette[(next + 1) % palette.Length], lerp);

        // Apply TMP gradient
        VertexGradient gradient;
        if (vertical)
        {
            gradient = new VertexGradient(c1, c1, c2, c2);
        }
        else
        {
            gradient = new VertexGradient(c1, c2, c1, c2);
        }

        text.colorGradient = gradient;
    }
}
