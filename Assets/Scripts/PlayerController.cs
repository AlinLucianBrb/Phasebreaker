using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    private Rigidbody2D m_rigidBody2D;

    public InputActionAsset inputActions;
    InputAction shootAction;
    InputAction m_moveAction;
    InputAction pauseActionPlayer;
    InputAction pauseActionUI;
    private float m_moveAmt;

    public float m_movementSpeed = 10;
    public float attachOffsetY = 0.35f;     // how high above paddle the ball sits
    public float maxAimAngle = 90f;         // half-arc; 90 => full 180� sweep
    public float aimSweepSpeed = 2.2f;      // how fast arrow sweeps
    public float reattachCooldown = 0.25f;  // prevent instant regrab after launch

    float aimAngle;

    public Vector3 collisionOffest;
    Transform aimArrow;

    Vector3 ballPosition;
    State ballState;

    public bool ballCatching;

    private void OnEnable()
    {
        inputActions.FindActionMap("Player").Enable();
        GameEvents.OnBallStick += EnterAttached;
    }

    private void OnDisable()
    {
        inputActions.FindActionMap("Player").Disable();
        GameEvents.OnBallStick -= EnterAttached;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        m_moveAction = InputSystem.actions.FindAction("Move");
        shootAction = InputSystem.actions.FindAction("Shoot");
        pauseActionPlayer = InputSystem.actions.FindAction("Player/Pause");
        pauseActionUI = InputSystem.actions.FindAction("UI/Pause");
        m_rigidBody2D = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        aimArrow = BallController.Instance.aimArrow;
    }

    void Update()
    {
        Debug.Log(LevelPatternGenerator.Instance.spawned.Count);

        ballPosition = BallController.Instance.transform.position;
        ballState = BallController.Instance.state;
        m_moveAmt = m_moveAction.ReadValue<float>();

        if(ballState == State.Flying && shootAction.IsPressed())
        {
            ballCatching = true;
        }
        else
        {
            ballCatching = false;
        }

        if (ballState == State.Attached || ballState == State.Aiming)
        {
            BallController.Instance.transform.position = new Vector3(transform.position.x + collisionOffest.x, transform.position.y + collisionOffest.y, 0f);
        }
        if (ballState == State.Attached && (shootAction.IsPressed() || shootAction.WasPressedThisFrame()))
        {
            EnterAiming();
        }
        if (ballState == State.Aiming)
        {
            if (shootAction.WasReleasedThisFrame())
            {
                GameEvents.BallLaunch(aimAngle);
                return;
            }

            m_moveAmt = 0;
            UpdateAimArrow();       
        }

        //Pause
        if (pauseActionPlayer.WasPressedThisFrame())
        {
            inputActions.FindActionMap("Player").Disable();
            inputActions.FindActionMap("UI").Enable();
            ManagerGame.SetPause(true);
        }
        else if (pauseActionUI.WasPressedThisFrame() || !ManagerGame.IsPaused)
        {
            inputActions.FindActionMap("UI").Disable();
            inputActions.FindActionMap("Player").Enable();
            ManagerGame.SetPause(false);
        }
    }

    private void FixedUpdate()
    {
        m_rigidBody2D.MovePosition(m_rigidBody2D.position + new Vector2(m_movementSpeed * m_moveAmt * Time.fixedDeltaTime, 0));
    }

    void EnterAttached()
    {
        collisionOffest = -transform.position + BallController.Instance.transform.position;
    }

    void EnterAiming()
    {
        BallController.Instance.state = State.Aiming;
        if (aimArrow)
        {
            aimArrow.gameObject.SetActive(true);
            aimArrow.localPosition = Vector3.zero; // base at ball center
            aimArrow.localRotation = Quaternion.identity;
        }
    }

    void UpdateAimArrow()
    {
        // sweep -maxAimAngle..+maxAimAngle using a sine ping-pong
        // sin(t) ? [-1,1] -> map to [0,1] -> Lerp(-A, +A, x)
        float t = 0.5f * (Mathf.Sin(Time.time * aimSweepSpeed) + 1f);
        aimAngle = Mathf.Lerp(-maxAimAngle, maxAimAngle, t);

        // rotate arrow so its local up points at the chosen angle (relative to +Y)
        if (aimArrow)
        {
            aimArrow.up = Quaternion.Euler(0f, 0f, aimAngle) * Vector2.up;
        }
    }

    
}
