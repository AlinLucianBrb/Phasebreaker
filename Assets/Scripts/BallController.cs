using UnityEngine;

public enum State { Flying, Attached, Aiming }

public class BallController : MonoBehaviour
{
    public static BallController Instance { get; private set; }

    public State state = State.Flying;

    Rigidbody2D rigidBody2D;
    public float initialSpeed;
    public float speedIncrement;
    float speed;

    float noStickUntil;
    float reattachCooldown;

    public Transform aimArrow;

    [HideInInspector]
    public Vector2 ballDir;

    Vector2 accumNormal = Vector2.zero;
    bool pendingResolve = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        rigidBody2D = GetComponent<Rigidbody2D>();
        aimArrow = transform.GetChild(0);

    }

    void Start()
    {
        RestartBall();
        reattachCooldown = PlayerController.Instance.reattachCooldown;
    }

    void OnEnable()
    {
        GameEvents.OnBallLaunch += Launch;
    }

    void OnDisable()
    {
        GameEvents.OnBallLaunch -= Launch;
    }

    void RestartBall()
    {
        state = State.Attached;
        PlayerController.Instance.collisionOffest = new Vector2(0f, 0.4f);
    }

    bool IsTopContact(Collision2D c)
    {
        // paddle collider we hit
        var paddleCol = c.collider;
        var b = paddleCol.bounds;

        // how close to the top edge counts as "top" (world units or fraction)
        float topMargin = Mathf.Max(0.01f, b.size.y * 0.15f); // top 15% of height

        // require a mostly-upward normal and a contact point in the top band
        for (int i = 0; i < c.contactCount; i++)
        {
            var ct = c.GetContact(i);
            // upward-ish normal (reject sides/bottom)
            if (ct.normal.y > 0.6f)
            {
                if (ct.point.y >= b.max.y - topMargin)
                    return true;
            }
        }

        return false;
    }

    void FixedUpdate()
    {
        if (pendingResolve)
        {
            pendingResolve = false;

            Vector3 reflected = Vector3.Reflect(ballDir, accumNormal);
            if (Vector2.Dot(reflected, accumNormal) > -0.98f) reflected = Vector2.Lerp(reflected, accumNormal, 0.1125f); // nudge away
            ballDir = reflected.normalized;

            accumNormal = Vector2.zero;
        }

        rigidBody2D.linearVelocity = ballDir * speed;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (state != State.Flying) return;

        if (collision.transform.CompareTag("BallLost"))
        {
            RestartBall();
            GameEvents.BallLost(-1);
            return;
        }

        if (collision.transform.CompareTag("Player"))
        {
            if (Time.time >= noStickUntil && IsTopContact(collision) && PlayerController.Instance.ballCatching)
            {
                EnterAttached();
                GameEvents.BallStick();
                return;
            }

            if (Time.time < noStickUntil)
            {
                return;
            }
        }

        speed += speedIncrement;
        foreach (var contact in collision.contacts)
        {
            accumNormal += contact.normal;
        }
        accumNormal.Normalize();
        pendingResolve = true;

        // SFX etc...
        // ManagerGame.I.PlaySFX("Hit");
    }

    void EnterAttached()
    {
        rigidBody2D.linearVelocity = Vector2.zero;
        rigidBody2D.angularVelocity = 0f;
        rigidBody2D.simulated = false;   
        state = State.Attached;
    }

    void Launch(float angleDeg)
    {
        // compute direction from angle relative to up
        Vector2 dir = Quaternion.Euler(0f, 0f, angleDeg) * Vector2.up;

        // ensure we never launch downward
        if (dir.y <= 0f) dir.y = Mathf.Abs(dir.y) + 0.0001f;

        state = State.Flying;
        rigidBody2D.simulated = true;
        //rigidBody2D.linearVelocity = dir.normalized * launchSpeed;
        ballDir = dir.normalized;
        speed = initialSpeed;

        noStickUntil = Time.time + reattachCooldown;

        if (aimArrow) aimArrow.gameObject.SetActive(false);
    }
}
