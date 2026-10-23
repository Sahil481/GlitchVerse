using UnityEngine;

/// <summary>
/// 2D side-scrolling controller (Rigidbody2D).
/// Mechanics: run, sprint, double jump, air control, dash, phase dash,
/// size shift, glitch-zone hooks, touch-based death layers, and configurable controls.
///
/// Unity 6 uses rb.linearVelocity. On older Unity versions, replace every
/// "linearVelocity" with "velocity".
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    public enum PlayerAction
    {
        MoveLeft,
        MoveRight,
        Sprint,
        Jump,
        Dash,
        PhaseDash,
        ToggleSize
    }

    [System.Serializable]
    public class ControlBindings
    {
        public KeyCode moveLeft = KeyCode.A;
        public KeyCode moveRight = KeyCode.D;
        public KeyCode sprint = KeyCode.LeftShift;
        public KeyCode jump = KeyCode.Space;
        public KeyCode dash = KeyCode.Q;
        public KeyCode phaseDash = KeyCode.E;
        public KeyCode toggleSize = KeyCode.F;
    }

    [Header("Run")]
    public float walkSpeed = 7f;
    public float sprintSpeed = 12f;
    public float groundAccel = 75f;
    public float groundDecel = 85f;

    [Header("Air Control (the 'hack')")]
    public float airAccel = 55f;
    public float airTurnBoost = 3.5f;

    [Header("Jump / Gravity")]
    public float jumpVelocity = 13f;
    public int extraJumps = 1;
    public float baseGravityScale = 3.5f;
    public float fallMultiplier = 1.7f;
    public float lowJumpMultiplier = 2f;
    public float maxFallSpeed = 40f;
    public float coyoteTime = 0.1f;
    public float jumpBuffer = 0.1f;

    [Header("Dash")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Phase Dash")]
    public string phaseLayerName = "PhaseWall";
    public float phaseDashSpeed = 16f;
    public float phaseDashDuration = 0.3f;
    public float phaseDashCooldown = 2f;

    [Header("Size Shift")]
    public float smallSize = 0.5f;
    public float sizeChangeSpeed = 5f;

    [Header("Small Form Physics")]
    [Tooltip("Small form is nimbler on the ground, but weaker in the air.")]
    [Min(0f)] public float smallWalkSpeedMultiplier = 1.1f;
    [Min(0f)] public float smallSprintSpeedMultiplier = 1.1f;
    [Min(0f)] public float smallGroundAccelMultiplier = 1.15f;
    [Min(0f)] public float smallAirAccelMultiplier = 1f;
    [Tooltip("Lower value means a shorter jump. 0.65 = 65% of normal jump strength.")]
    [Min(0f)] public float smallJumpVelocityMultiplier = 0.65f;
    [Min(0)] public int smallExtraJumps = 1;
    [Min(0f)] public float smallGravityMultiplier = 1.15f;
    [Tooltip("Small form has a shorter dash and phase dash.")]
    [Min(0f)] public float smallDashSpeedMultiplier = 0.75f;
    [Min(0f)] public float smallPhaseDashSpeedMultiplier = 0.75f;

    [Header("Ground Check")]
    public LayerMask groundLayers;

    [Header("Death")]
    [Tooltip("Touching any object on these layers opens the death screen. Supports trigger and non-trigger colliders.")]
    public LayerMask deathLayers;
    [Tooltip("Assign the DeathScreenUI in the scene. If empty, the controller will try to find one at runtime.")]
    [SerializeField] private DeathScreenUI deathScreenUI;

    [Header("Controls")]
    [Tooltip("These bindings are defaults. Saved changes from the settings menu are loaded automatically.")]
    [SerializeField] private ControlBindings controls = new ControlBindings();

    // Glitch modifiers set by other scripts.
    [HideInInspector] public bool controlsInverted;
    [HideInInspector] public float surfaceGrip = 1f;

    // Public read-only state for UI and other scripts.
    public bool IsGrounded => grounded;
    public bool IsDashReady => dashCooldownTimer <= 0f && !isDashing;
    public bool IsSmall => isSmall;
    public bool IsDead => isDead;
    public ControlBindings Controls => controls;
    public bool GameplayInputEnabled => gameplayInputEnabled;

    /// <summary>Raised after the player has been restored by Respawn().
    /// Other systems (for example DimensionShift and Manipulable) use this to reset their state.</summary>
    public static event System.Action PlayerRespawned;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticEvents()
    {
        PlayerRespawned = null;
    }

    private const string ControlPrefsPrefix = "GlitchVerse.Control.";

    Rigidbody2D rb;
    Collider2D col;

    float gravityScale;
    float UpDir => gravityScale >= 0f ? 1f : -1f;

    // Input
    float moveInput;
    float facing = 1f;
    bool sprinting, jumpHeld;
    float jumpBufferTimer;
    bool gameplayInputEnabled = true;

    // State
    bool grounded;
    bool isDead;
    float coyoteTimer;
    int extraJumpsLeft;
    Vector2 spawnPoint;

    // Dash
    bool isDashing, isPhasing, airDashUsed;
    float dashTimer, dashDir, dashCooldownTimer;
    int phaseLayer;

    // Size
    Vector3 baseScale;
    float baseHalfHeight;
    float currentSize = 1f, targetSize = 1f;
    bool isSmall;

    // Unity calls Reset when this component is first added or when Reset is selected
    // from its Inspector context menu. These match the project's configured layers.
    void Reset()
    {
        groundLayers = LayerMask.GetMask("Ground", "PhaseWall");
        deathLayers = LayerMask.GetMask("Death");
        phaseLayerName = "PhaseWall";

        if (controls == null)
            controls = new ControlBindings();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        baseScale = transform.localScale;
        gravityScale = baseGravityScale;

        LoadControls();

        phaseLayer = LayerMask.NameToLayer(phaseLayerName);
        if (phaseLayer < 0)
            Debug.LogWarning($"Layer '{phaseLayerName}' not found. Phase dash won't work until you create it.");
    }

    void Start()
    {
        baseHalfHeight = col.bounds.extents.y;
        spawnPoint = rb.position;
    }

    void OnDisable()
    {
        if (isPhasing) SetPhase(false);
    }

    // ------------------------------------------------------------ INPUT
    void Update()
    {
        if (!gameplayInputEnabled)
        {
            moveInput = 0f;
            sprinting = false;
            jumpHeld = false;
            jumpBufferTimer = 0f;
            return;
        }

        moveInput = 0f;
        if (Input.GetKey(controls.moveLeft)) moveInput -= 1f;
        if (Input.GetKey(controls.moveRight)) moveInput += 1f;
        if (controlsInverted) moveInput *= -1f;

        if (Mathf.Abs(moveInput) > 0.01f) facing = Mathf.Sign(moveInput);

        sprinting = Input.GetKey(controls.sprint);
        jumpHeld = Input.GetKey(controls.jump);

        if (Input.GetKeyDown(controls.jump)) jumpBufferTimer = jumpBuffer;
        if (Input.GetKeyDown(controls.dash)) TryDash(false);
        if (Input.GetKeyDown(controls.phaseDash)) TryDash(true);
        if (Input.GetKeyDown(controls.toggleSize)) ToggleSize();
    }

    // ------------------------------------------------------------ PHYSICS
    void FixedUpdate()
    {
        if (isDead) return;

        float dt = Time.fixedDeltaTime;

        jumpBufferTimer -= dt;
        dashCooldownTimer -= dt;

        UpdateSize(dt);

        Vector2 v = rb.linearVelocity;
        float upVel = v.y * UpDir;

        grounded = CheckGrounded();
        if (grounded && upVel <= 0.1f)
        {
            coyoteTimer = coyoteTime;
            extraJumpsLeft = isSmall ? smallExtraJumps : extraJumps;
            airDashUsed = false;
        }
        else coyoteTimer -= dt;

        if (isDashing) { DashStep(dt); return; }

        // --- Horizontal movement ---
        float speedMultiplier = isSmall
            ? (sprinting ? smallSprintSpeedMultiplier : smallWalkSpeedMultiplier)
            : 1f;
        float target = moveInput * (sprinting ? sprintSpeed : walkSpeed) * speedMultiplier;
        float accel;
        if (grounded)
        {
            float groundAccelMultiplier = isSmall ? smallGroundAccelMultiplier : 1f;
            accel = (Mathf.Abs(moveInput) > 0.01f ? groundAccel : groundDecel)
                    * groundAccelMultiplier * surfaceGrip;
        }
        else
        {
            accel = airAccel * (isSmall ? smallAirAccelMultiplier : 1f);
            bool reversing = Mathf.Abs(moveInput) > 0.01f
                             && Mathf.Abs(v.x) > 0.1f
                             && Mathf.Sign(moveInput) != Mathf.Sign(v.x);
            if (reversing) accel *= airTurnBoost;
        }
        v.x = Mathf.MoveTowards(v.x, target, accel * dt);

        // --- Jump (buffered + coyote + size-dependent extra jumps) ---
        if (jumpBufferTimer > 0f)
        {
            if (coyoteTimer > 0f) DoJump(ref v);
            else if (extraJumpsLeft > 0) { extraJumpsLeft--; DoJump(ref v); }
        }

        // --- Gravity (variable jump height + snappy fall) ---
        float g = gravityScale * (isSmall ? smallGravityMultiplier : 1f);
        upVel = v.y * UpDir;
        if (upVel < 0f) g *= fallMultiplier;
        else if (upVel > 0f && !jumpHeld) g *= lowJumpMultiplier;
        rb.gravityScale = g;
        v.y = Mathf.Clamp(v.y, -maxFallSpeed, maxFallSpeed);

        rb.linearVelocity = v;
    }

    void DoJump(ref Vector2 v)
    {
        float jumpMultiplier = isSmall ? smallJumpVelocityMultiplier : 1f;
        v.y = jumpVelocity * jumpMultiplier * UpDir;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }

    bool CheckGrounded()
    {
        Bounds b = col.bounds;
        Vector2 center = new Vector2(b.center.x, b.center.y - UpDir * b.extents.y);
        Vector2 size = new Vector2(b.size.x * 0.9f, 0.12f);
        return Physics2D.OverlapBox(center, size, 0f, groundLayers) != null;
    }

    // ------------------------------------------------------------ DASH
    void TryDash(bool phase)
    {
        if (isDashing || dashCooldownTimer > 0f) return;
        if (!grounded && airDashUsed) return;
        if (phase && phaseLayer < 0) return;

        isDashing = true;
        isPhasing = phase;
        dashDir = facing;
        dashTimer = phase ? phaseDashDuration : dashDuration;
        if (!grounded) airDashUsed = true;
        if (phase) SetPhase(true);
    }

    void DashStep(float dt)
    {
        float speed = isPhasing ? phaseDashSpeed : dashSpeed;
        if (isSmall)
        {
            speed *= isPhasing ? smallPhaseDashSpeedMultiplier : smallDashSpeedMultiplier;
        }

        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dashDir * speed, 0f);
        dashTimer -= dt;

        if (dashTimer <= 0f)
        {
            if (isPhasing && InsidePhaseGeometry()) return;
            EndDash();
        }
    }

    void EndDash()
    {
        if (isPhasing) SetPhase(false);
        dashCooldownTimer = isPhasing ? phaseDashCooldown : dashCooldown;
        isDashing = false;
        isPhasing = false;
        float endDashSpeed = walkSpeed * (isSmall ? smallWalkSpeedMultiplier : 1f);
        rb.linearVelocity = new Vector2(dashDir * endDashSpeed, 0f);
    }

    void SetPhase(bool on)
    {
        if (phaseLayer >= 0)
            Physics2D.IgnoreLayerCollision(gameObject.layer, phaseLayer, on);
    }

    bool InsidePhaseGeometry()
    {
        return Physics2D.OverlapBox(col.bounds.center, col.bounds.size * 0.9f, 0f, 1 << phaseLayer) != null;
    }

    // ------------------------------------------------------------ SIZE SHIFT
    void ToggleSize()
    {
        if (isSmall)
        {
            if (!HasRoomToGrow()) return;
            isSmall = false;
            targetSize = 1f;
        }
        else
        {
            isSmall = true;
            targetSize = smallSize;
            extraJumpsLeft = Mathf.Min(extraJumpsLeft, smallExtraJumps);
        }
    }

    bool HasRoomToGrow()
    {
        Vector2 full = col.bounds.size / currentSize;
        float feetY = col.bounds.center.y - UpDir * col.bounds.extents.y;
        Vector2 center = new Vector2(col.bounds.center.x, feetY + UpDir * full.y * 0.5f);
        return Physics2D.OverlapBox(center, full * 0.95f, 0f, groundLayers) == null;
    }

    void UpdateSize(float dt)
    {
        if (Mathf.Approximately(currentSize, targetSize)) return;

        float newSize = Mathf.MoveTowards(currentSize, targetSize, sizeChangeSpeed * dt);
        Vector2 p = rb.position;
        p.y += (newSize - currentSize) * baseHalfHeight * UpDir;
        rb.position = p;
        currentSize = newSize;
        transform.localScale = baseScale * currentSize;
    }

    // ------------------------------------------------------------ DEATH LAYER
    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsDeathLayer(other.gameObject.layer)) Die();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsDeathLayer(collision.gameObject.layer)) Die();
    }

    /// <summary>Stops the player and opens the death screen. Can also be called by other hazards.</summary>
    public void Die()
    {
        if (isDead) return;

        if (deathScreenUI == null)
            deathScreenUI = FindAnyObjectByType<DeathScreenUI>();

        // Keep the old safe behavior if no death UI has been added to the scene.
        if (deathScreenUI == null)
        {
            Debug.LogWarning("No DeathScreenUI found. Respawning immediately. Add a DeathScreenUI to show the death menu.");
            Respawn();
            return;
        }

        isDead = true;
        SetGameplayInputEnabled(false);
        if (isPhasing) SetPhase(false);
        isDashing = false;
        isPhasing = false;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
        deathScreenUI.Show();
    }

    bool IsDeathLayer(int layer)
    {
        return (deathLayers.value & (1 << layer)) != 0;
    }

    // ------------------------------------------------------------ CONTROL BINDINGS API
    /// <summary>Returns the currently assigned key for the requested action.</summary>
    public KeyCode GetBinding(PlayerAction action)
    {
        switch (action)
        {
            case PlayerAction.MoveLeft: return controls.moveLeft;
            case PlayerAction.MoveRight: return controls.moveRight;
            case PlayerAction.Sprint: return controls.sprint;
            case PlayerAction.Jump: return controls.jump;
            case PlayerAction.Dash: return controls.dash;
            case PlayerAction.PhaseDash: return controls.phaseDash;
            case PlayerAction.ToggleSize: return controls.toggleSize;
            default: return KeyCode.None;
        }
    }

    /// <summary>
    /// Assigns and saves a key. Returns false if the key is None or already used
    /// by a different action. A settings UI can call this method when rebinding.
    /// </summary>
    public bool TrySetBinding(PlayerAction action, KeyCode key)
    {
        if (key == KeyCode.None) return false;

        foreach (PlayerAction otherAction in System.Enum.GetValues(typeof(PlayerAction)))
        {
            if (otherAction != action && GetBinding(otherAction) == key)
                return false;
        }

        switch (action)
        {
            case PlayerAction.MoveLeft: controls.moveLeft = key; break;
            case PlayerAction.MoveRight: controls.moveRight = key; break;
            case PlayerAction.Sprint: controls.sprint = key; break;
            case PlayerAction.Jump: controls.jump = key; break;
            case PlayerAction.Dash: controls.dash = key; break;
            case PlayerAction.PhaseDash: controls.phaseDash = key; break;
            case PlayerAction.ToggleSize: controls.toggleSize = key; break;
            default: return false;
        }

        SaveControls();
        return true;
    }

    /// <summary>Allows a settings menu to stop gameplay responding while waiting for a key.</summary>
    public void SetGameplayInputEnabled(bool enabled)
    {
        gameplayInputEnabled = enabled;
        if (!enabled)
        {
            moveInput = 0f;
            sprinting = false;
            jumpHeld = false;
            jumpBufferTimer = 0f;
        }
    }

    /// <summary>Saves the current key assignments between game sessions.</summary>
    public void SaveControls()
    {
        PlayerPrefs.SetInt(ControlPrefsPrefix + nameof(PlayerAction.MoveLeft), (int)controls.moveLeft);
        PlayerPrefs.SetInt(ControlPrefsPrefix + nameof(PlayerAction.MoveRight), (int)controls.moveRight);
        PlayerPrefs.SetInt(ControlPrefsPrefix + nameof(PlayerAction.Sprint), (int)controls.sprint);
        PlayerPrefs.SetInt(ControlPrefsPrefix + nameof(PlayerAction.Jump), (int)controls.jump);
        PlayerPrefs.SetInt(ControlPrefsPrefix + nameof(PlayerAction.Dash), (int)controls.dash);
        PlayerPrefs.SetInt(ControlPrefsPrefix + nameof(PlayerAction.PhaseDash), (int)controls.phaseDash);
        PlayerPrefs.SetInt(ControlPrefsPrefix + nameof(PlayerAction.ToggleSize), (int)controls.toggleSize);
        PlayerPrefs.Save();
    }

    /// <summary>Restores the default keys and saves them.</summary>
    public void ResetControlsToDefaults()
    {
        controls.moveLeft = KeyCode.A;
        controls.moveRight = KeyCode.D;
        controls.sprint = KeyCode.LeftShift;
        controls.jump = KeyCode.Space;
        controls.dash = KeyCode.Q;
        controls.phaseDash = KeyCode.E;
        controls.toggleSize = KeyCode.F;
        SaveControls();
    }

    void LoadControls()
    {
        controls.moveLeft = LoadKey(PlayerAction.MoveLeft, controls.moveLeft);
        controls.moveRight = LoadKey(PlayerAction.MoveRight, controls.moveRight);
        controls.sprint = LoadKey(PlayerAction.Sprint, controls.sprint);
        controls.jump = LoadKey(PlayerAction.Jump, controls.jump);
        controls.dash = LoadKey(PlayerAction.Dash, controls.dash);
        controls.phaseDash = LoadKey(PlayerAction.PhaseDash, controls.phaseDash);
        controls.toggleSize = LoadKey(PlayerAction.ToggleSize, controls.toggleSize);
    }

    KeyCode LoadKey(PlayerAction action, KeyCode fallback)
    {
        string prefKey = ControlPrefsPrefix + action;
        if (!PlayerPrefs.HasKey(prefKey)) return fallback;

        int storedValue = PlayerPrefs.GetInt(prefKey);
        if (System.Enum.IsDefined(typeof(KeyCode), storedValue))
            return (KeyCode)storedValue;

        return fallback;
    }

    // ------------------------------------------------------------ GLITCH HOOKS
    public void SetGravityMultiplier(float multiplier) { gravityScale = baseGravityScale * multiplier; }
    public void ResetGravity() { gravityScale = baseGravityScale; }

    // ------------------------------------------------------------ RESPAWN
    public void Respawn()
    {
        // Reactivate physics first in case the death screen froze the Rigidbody2D.
        rb.simulated = true;
        isDead = false;
        SetGameplayInputEnabled(true);

        if (isPhasing) SetPhase(false);
        isDashing = false;
        isPhasing = false;

        ResetGravity();
        controlsInverted = false;
        surfaceGrip = 1f;

        isSmall = false;
        currentSize = targetSize = 1f;
        transform.localScale = baseScale;
        currentSize = targetSize = 1f;

        rb.position = spawnPoint;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = baseGravityScale;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        extraJumpsLeft = extraJumps;
        airDashUsed = false;
        dashCooldownTimer = 0f;

        // Notify other systems so dimensions, manipulated platforms, etc. reset too.
        PlayerRespawned?.Invoke();
    }
}
