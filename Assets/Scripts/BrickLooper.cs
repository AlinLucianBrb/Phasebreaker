using UnityEngine;

/// Add to a Brick (with Rigidbody2D). The brick will drift, wrap at screen edges,
/// and optionally reverse direction every `reverseInterval` seconds.
[RequireComponent(typeof(Rigidbody2D))]
public class BrickLooper : MonoBehaviour
{
    [Header("Motion")]
    public float speed = 1.5f;         // world units per second
    public bool horizontal = true;     // move on X; if false, move on Y
    public int startDirection = 1;     // 1 or -1

    [Header("Auto Reverse")]
    public bool autoReverse = true;    // flip direction every interval?
    public float reverseInterval = 5f; // seconds between flips

    [Header("Camera / Wrap")]
    public Camera cam;                 // leave empty -> Camera.main
    public float wrapPadding = 0.0f;   // extra padding beyond bounds before wrapping

    Rigidbody2D rb;
    Vector2 dir;               // normalized (±1 on chosen axis)
    float nextFlipTime;
    float halfW, halfH;        // camera half extents (world units)
    float halfX, halfY;        // this object's half extents

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!cam) cam = Camera.main;
        if (rb.bodyType == RigidbodyType2D.Dynamic)
        {
            // Recommended: make the brick kinematic so it isn't pushed by the ball.
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        dir = horizontal ? new Vector2(Mathf.Sign(startDirection), 0f)
                         : new Vector2(0f, Mathf.Sign(startDirection));
        dir = dir.normalized;

        CacheExtents();
        nextFlipTime = Time.time + reverseInterval;
    }

    void OnEnable()
    {
        CacheExtents(); // in case camera/scale changed while disabled
    }

    void LateUpdate()
    {
        // If resolution or camera size changed at runtime, recalc.
        CacheExtents();
    }

    void FixedUpdate()
    {
        if (autoReverse && Time.time >= nextFlipTime)
        {
            dir = -dir;
            nextFlipTime = Time.time + reverseInterval;
        }

        Vector2 pos = rb.position + dir * speed * Time.fixedDeltaTime;

        Vector3 camPos = cam.transform.position;
        float left = camPos.x - halfW;
        float right = camPos.x + halfW;
        float bottom = camPos.y - halfH;
        float top = camPos.y + halfH;

        float padX = wrapPadding + halfX;
        float padY = wrapPadding + halfY;

        bool wrapped = false;

        if (horizontal)
        {
            if (pos.x - halfX > right + wrapPadding) { pos.x = left - padX; wrapped = true; }
            else if (pos.x + halfX < left - wrapPadding) { pos.x = right + padX; wrapped = true; }
        }
        else
        {
            if (pos.y - halfY > top + wrapPadding) { pos.y = bottom - padY; wrapped = true; }
            else if (pos.y + halfY < bottom - wrapPadding) { pos.y = top + padY; wrapped = true; }
        }

        if (wrapped)
        {
            rb.position = pos;
            rb.angularVelocity = rb.angularVelocity;
        }
        else
        {
            rb.MovePosition(pos);
        }
    }

    void CacheExtents()
    {
        if (!cam) return;

        // Camera half-extents
        halfH = cam.orthographicSize;                   // half height
        halfW = halfH * cam.aspect;                     // half width

        // Object half-extents (prefer renderer, else collider)
        var rend = GetComponent<Renderer>();
        if (rend) { halfX = rend.bounds.extents.x; halfY = rend.bounds.extents.y; }
        else
        {
            var col = GetComponent<Collider2D>();
            if (col) { halfX = col.bounds.extents.x; halfY = col.bounds.extents.y; }
            else { halfX = halfY = 0.5f; } // fallback
        }
    }

    // Optional helpers:
    public void SetDirection(int sign) { dir = horizontal ? new Vector2(Mathf.Sign(sign), 0) : new Vector2(0, Mathf.Sign(sign)); }
    public void ToggleDirection() { dir = -dir; }
}
