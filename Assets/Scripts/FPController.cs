using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class FPController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraPivot; // Genelde Main Camera
    private CharacterController controller;

    [Header("Look")]
    public float mouseSensitivity = 1.2f;     // Genel hassasiyet çarpaný
    public float lookXMultiplier = 1f;        // Yaw
    public float lookYMultiplier = 1f;        // Pitch
    public float pitchMin = -85f;
    public float pitchMax = 85f;
    public bool lockCursorOnStart = true;
    public bool invertY = false;

    [Header("Move Speeds")]
    public float walkSpeed = 4.5f;
    public float sprintSpeed = 7.5f;
    public float crouchSpeed = 2.5f;

    [Header("Move Feel")]
    public float acceleration = 18f;
    public float deceleration = 24f;
    public float airControl = 0.5f;           // Havada yatay kontrol
    public float gravity = -20f;
    public float jumpHeight = 1.2f;
    public float coyoteTime = 0.12f;          // Yerden kesildikten sonra kýsa süre zýplamaya izin
    public float jumpBuffer = 0.12f;          // Zýplama tuþunu erken basmayý tamponlar

    [Header("Grounding")]
    public LayerMask groundMask;
    public float groundCheckRadius = 0.28f;   // Çizimde kullanýlýyor ama aþaðýda bounds’tan dinamik hesaplýyoruz
    public float groundCheckOffset = 0.1f;    // (Gizmos için kaldý)
    public float slopeSlideGravity = -35f;    // çok dik yamaçta hafif kayma

    [Header("Crouch")]
    public KeyCode crouchToggleFallback = KeyCode.LeftControl;
    public float standHeight = 1.8f;
    public float crouchHeight = 1.1f;
    public float crouchSmooth = 12f;

    [Header("Head Bob (opsiyonel)")]
    public bool enableHeadBob = true;
    public float bobFrequency = 1.8f;
    public float bobAmplitude = 0.04f;

    [Header("Controller Tuning")]
    public bool autoCenter = true;      // CC center’ý otomatik ayarlansýn mý?
    public bool bottomAnchored = true;  // true: tabaný sabit tut (height deðiþse de ayak yerde kalsýn)

    // Internal
    private float _pitch;
    private Vector3 _velocity;                // world-space
    private bool _isGrounded;
    private bool _isSprinting;
    private bool _isCrouching;
    private float _lastGroundedTime = -999f;
    private float _lastJumpPressedTime = -999f;
    private bool _jumpQueued = false;
    private float _bobTimer;
    private Vector3 _camLocalDefault;
    private float _baseBottomY = 0f;          // center.y - height/2 (taban referansý)
    private float yVelocity = 0f;
    private bool _headBobJustStarted = true;
    bool firstmove = true;
    
#if ENABLE_INPUT_SYSTEM
    private Keyboard kb => Keyboard.current;
    private Mouse ms => Mouse.current;
#endif

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraPivot == null && Camera.main != null) cameraPivot = Camera.main.transform;
        if (cameraPivot != null) _camLocalDefault = cameraPivot.localPosition;

        // taban referansýný al (Inspector’daki mevcut center/height üzerinden)
        _baseBottomY = controller.center.y - controller.height * 0.5f;

        if (lockCursorOnStart) LockCursor(true);
    }

    void Update()
    {
        HandleLook();
        HandleCrouch();

        // Jump inputu kuyrukla (auto zýplamayý keser)
        if (JumpPressedThisFrame())
        {
            _jumpQueued = true;
            _lastJumpPressedTime = Time.time;
        }

        HandleMovement();
        HandleHeadBob();
        HandleCursorToggle();
    }

    #region Look
    void HandleLook()
    {
        Vector2 lookDelta = GetLookDelta();
        float invert = invertY ? 1f : -1f;

        // Yaw
        transform.Rotate(Vector3.up, lookDelta.x * mouseSensitivity * lookXMultiplier);

        // Pitch
        _pitch += lookDelta.y * mouseSensitivity * lookYMultiplier * invert;
        _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);

        if (cameraPivot != null)
        {
            Vector3 e = cameraPivot.localEulerAngles;
            e.x = _pitch;
            cameraPivot.localEulerAngles = e;
        }
    }

    Vector2 GetLookDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (ms != null)
        {
            Vector2 md = ms.delta.ReadValue() * (ms.rightButton.isPressed ? 0f : 1f);
            return md * 0.1f;
        }
#endif
        float x = Input.GetAxis("Mouse X");
        float y = Input.GetAxis("Mouse Y");
        return new Vector2(x, y);
    }
    #endregion

    #region Input
    Vector2 GetMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (kb != null)
        {
            float x = 0f, y = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
            Vector2 v = new Vector2(x, y);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }
#endif
        float h = Input.GetAxisRaw("Horizontal");
        float v2 = Input.GetAxisRaw("Vertical");
        Vector2 vv = new Vector2(h, v2);
        return vv.sqrMagnitude > 1f ? vv.normalized : vv;
    }

    bool JumpPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (kb != null) return kb.spaceKey.wasPressedThisFrame;
#endif
        return Input.GetKeyDown(KeyCode.Space);
    }

    bool SprintHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (kb != null) return kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
#endif
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    bool CrouchTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (kb != null) return kb.leftCtrlKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame;
#endif
        return Input.GetKeyDown(crouchToggleFallback) || Input.GetKeyDown(KeyCode.C);
    }
    #endregion

    #region Movement
    void HandleMovement()
    {
        // Ground check: bounds tabanýndan dinamik yarýçapla
        Bounds b = controller.bounds;
        Vector3 feet = b.center + Vector3.down * (b.extents.y - 0.02f);
        float dynRadius = Mathf.Max(0.18f, controller.radius * 0.6f);
        _isGrounded = Physics.CheckSphere(feet, dynRadius, groundMask, QueryTriggerInteraction.Ignore) || controller.isGrounded;

        if (_isGrounded) _lastGroundedTime = Time.time;

        // Hedef hýz (state’e göre)
        _isSprinting = SprintHeld() && !_isCrouching;
        float desiredSpeed = _isCrouching ? crouchSpeed : (_isSprinting ? sprintSpeed : walkSpeed);

        // Yatay hedef vektör
        Vector2 input = GetMoveInput();
        Vector3 inputWorld = transform.TransformDirection(new Vector3(input.x, 0f, input.y));
        Vector3 desiredHorizontal = inputWorld.normalized * desiredSpeed;

        // Ývmelenme / Frenleme
        float accel = _isGrounded ? acceleration : acceleration * airControl;
        float decel = _isGrounded ? deceleration : deceleration * 0.5f;

        Vector3 currentHorizontal = new Vector3(_velocity.x, 0f, _velocity.z);

        Vector3 diff = desiredHorizontal - currentHorizontal;
        Vector3 change;
        if (desiredHorizontal.sqrMagnitude > 0.01f)
            change = Vector3.ClampMagnitude(diff, accel * Time.deltaTime);
        else
            change = Vector3.ClampMagnitude(-currentHorizontal, decel * Time.deltaTime);

        currentHorizontal += change;

        // Slope’a uydur
        currentHorizontal = ProjectOnGround(currentHorizontal);

        // Y ekseni/yer çekimi
        if (_isGrounded && _velocity.y < 0f) _velocity.y = -2f;
        else _velocity.y += gravity * Time.deltaTime;

        // Jump buffer & coyote time (kuyruklu)
        bool canJump =
            _jumpQueued &&
            Time.timeSinceLevelLoad >= 0.1f &&                                    // açýlýþta yanlýþ tetiklemeyi engelle
            (Time.time - _lastGroundedTime) <= coyoteTime &&
            (Time.time - _lastJumpPressedTime) <= jumpBuffer;

        if (canJump)
        {
            _jumpQueued = false;
            _lastJumpPressedTime = -999f;
            _lastGroundedTime = -999f;
            _velocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight);
        }

        // Çok dik slope’ta hafif kayma
        if (!_isGrounded && IsOnSteepSlope(out Vector3 steepNormal))
        {
            Vector3 steepDown = Vector3.ProjectOnPlane(Vector3.down, steepNormal).normalized;
            currentHorizontal += steepDown * (Mathf.Abs(slopeSlideGravity) * 0.2f * Time.deltaTime);
        }

        // Birleþtir ve uygula
        _velocity.x = currentHorizontal.x;
        _velocity.z = currentHorizontal.z;

        controller.Move(_velocity * Time.deltaTime);
    }

    Vector3 ProjectOnGround(Vector3 vec)
    {
        if (GetGround(out RaycastHit hit))
            return Vector3.ProjectOnPlane(vec, hit.normal);
        return vec;
    }

    bool GetGround(out RaycastHit hit)
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float rayLen = (controller.height * 0.5f) + 0.5f;
        return Physics.SphereCast(origin, controller.radius * 0.95f, Vector3.down, out hit, rayLen, groundMask, QueryTriggerInteraction.Ignore);
    }

    bool IsOnSteepSlope(out Vector3 normal)
    {
        normal = Vector3.up;
        if (GetGround(out RaycastHit hit))
        {
            normal = hit.normal;
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            return angle > controller.slopeLimit + 1f;
        }
        return false;
    }
    #endregion

    #region Crouch
    void HandleCrouch()
    {
        if (CrouchTogglePressed())
        {
            if (_isCrouching)
            {
                if (CanStandUp()) _isCrouching = false;
            }
            else _isCrouching = true;
        }

        float targetH = _isCrouching ? crouchHeight : standHeight;
        float newH = Mathf.Lerp(controller.height, targetH, Time.deltaTime * crouchSmooth);
        controller.height = newH;

        if (autoCenter)
        {
            float newCenterY = bottomAnchored
                ? _baseBottomY + controller.height * 0.5f   // tabaný sabit tut
                : controller.height * 0.5f;                 // klasik: merkeze sabitle
            controller.center = new Vector3(0f, newCenterY, 0f);
        }

        // Kamera yüksekliði
        if (cameraPivot != null)
        {
            float camTargetY = (_isCrouching ? 0.85f : 1.2f);
            Vector3 lp = cameraPivot.localPosition;
            lp.y = Mathf.Lerp(lp.y, camTargetY, Time.deltaTime * crouchSmooth);
            cameraPivot.localPosition = lp;
        }
    }

    bool CanStandUp()
    {
        float extra = (standHeight - controller.height) + 0.05f;
        Vector3 start = transform.position + Vector3.up * controller.height;
        return !Physics.SphereCast(start, controller.radius * 0.95f, Vector3.up, out _, extra, ~0, QueryTriggerInteraction.Ignore);
    }
    #endregion

    #region HeadBob
    void HandleHeadBob()
    {
        if (!enableHeadBob || cameraPivot == null) return;

        Vector3 horiz = new Vector3(_velocity.x, 0f, _velocity.z);
        bool moving = _isGrounded && horiz.magnitude > 0.2f;

        if (moving)
        {
            _bobTimer += Time.deltaTime * bobFrequency * (_isSprinting ? 1.35f : 1f);
            float offset = Mathf.Sin(_bobTimer * Mathf.PI * 2f) * bobAmplitude;
            float targetY = _camLocalDefault.y + offset;

            if (firstmove)
            {
                //  Ýlk hareket fazý: birkaç frame boyunca SmoothDamp çalýþsýn
                float smoothY = Mathf.SmoothDamp(
                    cameraPivot.localPosition.y,
                    targetY,
                    ref yVelocity,
                    0.08f // smooth süresi
                );

                cameraPivot.localPosition = new Vector3(
                    _camLocalDefault.x,
                    smoothY,
                    _camLocalDefault.z
                );

                // Hedefe yeterince yaklaþtý mý? Artýk normal moda geç
                if (Mathf.Abs(smoothY - targetY) < 0.001f)
                {
                    firstmove = false;
                    // Ýstersen hizaya tam oturt:
                    cameraPivot.localPosition = new Vector3(
                        _camLocalDefault.x,
                        targetY,
                        _camLocalDefault.z
                    );
                }
            }
            else
            {
                //  Sonraki tüm framelerde anýnda bob
                cameraPivot.localPosition = new Vector3(
                    _camLocalDefault.x,
                    targetY,
                    _camLocalDefault.z
                );
            }
        }
        else
        {
            //  Durunca resetle
            firstmove = true;
            yVelocity = 0f;
            _bobTimer = 0f;

            cameraPivot.localPosition = Vector3.Lerp(
                cameraPivot.localPosition,
                _camLocalDefault,
                Time.deltaTime * 10f
            );
        }
    }
    #endregion

    #region Cursor
    void HandleCursorToggle()
    {
#if ENABLE_INPUT_SYSTEM
    // ESC ile kilidi aç
    if (kb != null && kb.escapeKey.wasPressedThisFrame)
        LockCursor(false);

    //  Sol týkla tekrar kilitleyen satýrý SÝLDÝK
    // if (kb != null && ms != null && ms.leftButton.wasPressedThisFrame && !Cursor.lockState.Equals(CursorLockMode.Locked))
    //     LockCursor(true);
#else
        // ESC ile kilidi aç
        if (Input.GetKeyDown(KeyCode.Escape))
            LockCursor(false);

        //  Eski sol týk lock satýrý:
        // if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) 
        //     LockCursor(true);
#endif
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
    #endregion

    void OnDrawGizmosSelected()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        Bounds b = controller.bounds;
        Vector3 feet = b.center + Vector3.down * (b.extents.y - 0.02f);
        float dynRadius = Mathf.Max(0.18f, controller.radius * 0.6f);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(feet, dynRadius);
    }

}
