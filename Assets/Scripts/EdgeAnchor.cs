using UnityEngine;

public class EdgeAnchor : MonoBehaviour
{
    public enum Side { Left, Right }
    public Side side = Side.Left;

    [Tooltip("Gap from the screen edge (world units).")]
    public float xMargin = 0.5f;

    [Tooltip("Top/bottom padding (world units) for clamping movement.")]
    public float yMargin = 0.5f;

    Camera cam;
    Rigidbody2D rb;

    void Awake()
    {
        cam = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        RepositionToEdge();
        ClampY();
    }

    void LateUpdate()
    {
        // If resolution/aspect changes at runtime, keep it anchored.
        RepositionToEdge();
        ClampY();
    }

    void RepositionToEdge()
    {
        if (!cam) return;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        float x = (side == Side.Left) ? -halfW + xMargin : halfW - xMargin;
        var p = transform.position;
        p.x = x;

        if (rb) rb.position = new Vector2(x, rb.position.y);
        else transform.position = p;
    }

    void ClampY()
    {
        if (!cam) return;

        float halfH = cam.orthographicSize;
        float minY = -halfH + yMargin;
        float maxY = halfH - yMargin;

        if (rb)
            rb.position = new Vector2(rb.position.x, Mathf.Clamp(rb.position.y, minY, maxY));
        else
            transform.position = new Vector3(transform.position.x, Mathf.Clamp(transform.position.y, minY, maxY), transform.position.z);
    }

    // Call this from your paddle movement code each FixedUpdate:
    public Vector2 ClampMove(Vector2 desiredPos)
    {
        float halfH = cam.orthographicSize;
        float minY = -halfH + yMargin;
        float maxY = halfH - yMargin;
        desiredPos.y = Mathf.Clamp(desiredPos.y, minY, maxY);
        // lock X to edge
        float halfW = halfH * cam.aspect;
        desiredPos.x = (side == Side.Left) ? -halfW + xMargin : halfW - xMargin;
        return desiredPos;
    }
}
