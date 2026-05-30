/*
 * Unity Character Controller 2 (UCC2)
 * Physics (Rigidbody) based character controller
 * 
 * FEATURES
 * Base: Camera rotation, Player locomotion, Jumping, Crouching, Slope handling
 * Extentions: Wallrunning, Crouch boost, Double jumping
 *
 * Very inspired by Source engine, Karlson (by Danidev) and Titanfall 2 (by Respawn) movement
 * Titanfall 2 is peak
 *
 * By NVcoder
 * Github: https://github.com/NVcoder24
 * Version: 0.1
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// called PlayerCon for backwards compatibility with UCC legacy
public class PlayerCon : MonoBehaviour
{
    public enum State
    {
        GROUND,
        AIR,
        CROUCH,
        WALLRUN
    }

    [Header("UCC2")]
    public UCC2_Input input;

    [Header("Features")]
    public bool wallrunEnabled = true;
    public bool crouchBoostEnabled = true;
    public bool doubleJumpEnabled = true;


    [Header("Objects")]
    public Transform ori;
    public Transform player;
    public Camera camera;

    public Rigidbody rb;
    public LayerMask detectMask = ~0;
    public Vector3 baseScale = new Vector3(0.7f, 1.7f, 0.7f);

    //[Header("Camera")]
    //public float sensitivity = 1f;
    //public bool isLocked = true;

    public float fov = 70f;
    public float maxFov = 100f;
    public float maxSpeed = 15f;
    public float fovSpeed = 100f;

    [Header("Jumping")]
    public float jumpImpulse = 9f;
    public float doubleJumpImpulse = 8f;
    [Range(0f, 1f)] public float doubleJumpDirAssist = 0.4f;
    [Range(0f, 1f)] public float slopeJumpNormalBlend = 0.5f;

    [Header("Ground Check")]
    public float groundCheckDist = 0.2f;
    [Range(0f, 0.5f)] public float groundCheckCornerInset = 0.1f;
    [Range(0f, 90f)] public float maxSlopeAngle = 55f;

    [Header("Ground State")]
    public float speed = 7f;
    public float accel = 40f;
    public float drag = 20f;
    public float runMul = 1.5f;

    [Header("Air State")]
    public float airSpeed = 10f;
    public float airAccel = 10f;
    public float airDrag = 10f;
    public float airRunMul = 1.5f;

    [Header("Crouch State")]
    public float crouchPlayerHeight = 0.8f;
    public float crouchLerpSpeed = 12f;
    public float crouchBoost = 10f;
    public float slideForce = 20f;
    public float slideMinAngle = 10f;
    public AnimationCurve crouchBoostSpeedMul;
    public float crouchMaxSpeed = 10f;
    public float crouchSpeed = 5f;
    public float crouchAccel = 10f;
    public float crouchDrag = 5f;
    public float crouchRunMul = 1.2f;

    [Header("Wallrun State")]
    public float wallrunMaxWallAngle = 30f;
    public float wallrunDetectDist = 1f;
    public float wallrunSpeed = 25f;
    public float wallrunAccel = 10f;
    public float wallrunGravityScale = 0.5f;
    public float wallrunMaxTime = 999f;
    public float wallrunCooldown = 0.1f;
    public float wallCameraTilt = -15f;
    public float wallCameraTiltSpeed = 10f;
    public float wallJumpImpulse = 15f;
    public float wallrunMinSpeed = 0f;
    public float wallrunStickyForce = 100f;

    [Header("======= DEBUG =======")]
    public State state;
    public float debugMaxWishSpeed;
    public float debugHorizontalSpeed;

    bool prevJump;
    bool prevCrouch;
    bool jumpDown;
    bool crouchDown;

    bool isGrounded;
    bool hasDoubleJump;
    bool isWallLeft, isWallRight;
    RaycastHit wallHitLeft, wallHitRight;
    Vector3 wallNormal;
    bool wallrunOnLeft;
    float wallrunTimer;
    float wallrunCooldownTimer;
    bool wallJumpedThisFrame;
    bool justJumped;
    float justJumpedTimer;
    bool velocityLocked;

    Vector3 previousPosition;
    Vector3 measuredVelocity;
    float prevYVelocity;

    Collider col;

    void Start()
    {
        previousPosition = rb.position;
        col = GetComponent<Collider>();
    }

    void FixedUpdate()
    {
        measuredVelocity = (rb.position - previousPosition) / Time.fixedDeltaTime;
        previousPosition = rb.position;
        prevYVelocity = rb.velocity.y;
    }

    float tilt = 0f;
    float targetTilt = 0f;
    //float yRotation = 0;
    //float xRotation = 0;
    void CameraRotation()
    {
        //float camX = Input.GetAxis("Mouse X");
        //float camY = Input.GetAxis("Mouse Y");

        //yRotation -= camY * sensitivity;
        //yRotation = Mathf.Clamp(yRotation, -90f, 90f);
        //xRotation += camX * sensitivity;

        tilt = Mathf.Lerp(tilt, targetTilt, wallCameraTiltSpeed * Time.deltaTime);

        Quaternion q = input.GetOrientation();
        Vector3 euler = q.eulerAngles;
        ori.rotation = Quaternion.Euler(0f, euler.y, 0f);
        camera.transform.rotation = Quaternion.Euler(euler.x, euler.y, tilt);
    }

    //void CursorState()
    //{
        //Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        //Cursor.visible = !isLocked;
    //}

    void CameraPosition()
    {
        camera.transform.position = player.position + new Vector3(0, player.localScale.y / 2, 0);
    }

    Vector3 MyVec3Lerp(Vector3 from, Vector3 to, float a)
    {
        float maxDelta = (to - from).magnitude;
        return from + (to - from).normalized * Mathf.Abs(Mathf.Clamp(a, -maxDelta, maxDelta));
    }

    Vector3 rbVel;
    void Movement()
    {
        Vector2 movVec = new Vector2(input.HorizontalInput(), input.VerticalInput()).normalized;
        bool isMoving = movVec.magnitude > 0;

        float thisSpeed = speed;
        float thisAccel = accel;
        float thisDrag  = drag;
        float thisRunMul = runMul;

        if (state == State.AIR)     { thisSpeed = airSpeed;    thisAccel = airAccel;    thisDrag = airDrag;    thisRunMul = airRunMul; }
        if (state == State.CROUCH)  { thisSpeed = crouchSpeed; thisAccel = crouchAccel; thisDrag = crouchDrag; thisRunMul = crouchRunMul; }
        if (state == State.WALLRUN) { thisSpeed = wallrunSpeed; thisAccel = wallrunAccel; thisRunMul = 1f; }

        thisSpeed *= input.RunInput() ? thisRunMul : 1f;

        debugMaxWishSpeed = thisSpeed;
        debugHorizontalSpeed = new Vector2(measuredVelocity.x, measuredVelocity.z).magnitude;

        Vector3 wishVel;
        float a;
        float groundAngle = Vector3.Angle(groundHit.normal, Vector3.up);
        bool onSlope = isGrounded && (state == State.GROUND || state == State.CROUCH)
                       && groundAngle > 0.5f && groundAngle < maxSlopeAngle;

        if (state == State.WALLRUN)
        {
            rbVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            // Flatten wall normal to ground plane so tilted walls dont produce vertical forces
            Vector3 wallNormalH = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
            Vector3 wallForward = Vector3.Cross(wallNormalH, Vector3.up);
            if (Vector3.Dot(wallForward, ori.forward) < 0f) wallForward = -wallForward;

            // Only change speed if player explicitly pushes along the wall; otherwise maintain current speed
            float currentWallSpeed = Vector3.Dot(rbVel, wallForward);
            Vector3 inputDir = isMoving ? (ori.forward * movVec.y + ori.right * movVec.x).normalized : Vector3.zero;
            float inputAlongWall = Vector3.Dot(inputDir, wallForward);

            float targetSpeed = currentWallSpeed;
            if      (inputAlongWall >  0.1f) targetSpeed = thisSpeed;
            else if (inputAlongWall < -0.1f) targetSpeed = 0f;

            wishVel = wallForward * targetSpeed;
            a = thisAccel;
        }
        else
        {
            wishVel = thisSpeed * ori.forward * movVec.y + thisSpeed * ori.right * movVec.x;
            if (onSlope && !justJumped)
            {
                rbVel = Vector3.ProjectOnPlane(rb.velocity, groundHit.normal);
                if (wishVel.sqrMagnitude > 0f)
                    wishVel = Vector3.ProjectOnPlane(wishVel, groundHit.normal).normalized * wishVel.magnitude;
            }
            else if (justJumped)
            {
                rbVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            }
            else
            {
                rbVel = new Vector3(rbVel.x, 0f, rbVel.z);
            }
            a = isMoving ? thisAccel : thisDrag;
        }

        if (velocityLocked)
        {
            if (state != State.AIR) velocityLocked = false;
            else rbVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        }
        else
        {
            rbVel = MyVec3Lerp(rbVel, wishVel, a * Time.deltaTime);
        }

        // Crouch boost applied to actual velocity so wall contact can't inflate it
        if (crouchBoostEnabled && crouchDown)
        {
            Vector3 horizVel = new Vector3(measuredVelocity.x, 0f, measuredVelocity.z);
            Vector3 inputDir = isMoving ? (ori.forward * movVec.y + ori.right * movVec.x).normalized : ori.forward;
            Vector3 boostDir = horizVel.magnitude > 0.5f ? horizVel.normalized : inputDir;
            rb.velocity += boostDir * crouchBoost * crouchBoostSpeedMul.Evaluate(horizVel.magnitude / 2f / crouchMaxSpeed);
            rbVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        }

        float yVel = rb.velocity.y;
        if (state == State.WALLRUN)
        {
            yVel += Physics.gravity.y * wallrunGravityScale * Time.deltaTime;
            Vector3 sticky = wallNormal * wallrunStickyForce * Time.deltaTime;
            rbVel -= new Vector3(sticky.x, 0f, sticky.z);
            yVel  -= sticky.y;
        }

        if (state == State.CROUCH && isGrounded)
        {
            float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);
            if (slopeAngle > slideMinAngle)
            {
                Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, groundHit.normal).normalized;
                rbVel += slideDir * slideForce * (slopeAngle / 90f) * Time.deltaTime;
            }
        }

        if (onSlope && !justJumped)
            rb.velocity = rbVel;
        else
            rb.velocity = rbVel + new Vector3(0, yVel, 0);
    }

    RaycastHit groundHit;
    void GroundCheck()
    {
        float checkDist = col.bounds.extents.y + groundCheckDist;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, out groundHit, checkDist, detectMask);

        if (!isGrounded)
        {
            float ox = col.bounds.extents.x - groundCheckCornerInset;
            float oz = col.bounds.extents.z - groundCheckCornerInset;
            Vector3[] corners = {
                new Vector3( ox, 0,  oz),
                new Vector3(-ox, 0,  oz),
                new Vector3( ox, 0, -oz),
                new Vector3(-ox, 0, -oz),
            };
            foreach (var offset in corners)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position + offset, Vector3.down, out hit, checkDist, detectMask))
                {
                    isGrounded = true;
                    groundHit = hit;
                    break;
                }
            }
        }
    }

    void WallCheck()
    {
        isWallLeft  = Physics.Raycast(transform.position, -ori.right, out wallHitLeft,  wallrunDetectDist, detectMask);
        isWallRight = Physics.Raycast(transform.position,  ori.right, out wallHitRight, wallrunDetectDist, detectMask);

        // During wallrun, also cast toward the last known wall normal so curved surfaces stay tracked
        if (state == State.WALLRUN)
        {
            RaycastHit trackedHit;
            if (Physics.Raycast(transform.position, -wallNormal, out trackedHit, wallrunDetectDist + 0.3f, detectMask))
            {
                if (wallrunOnLeft) { isWallLeft = true; wallHitLeft = trackedHit; }
                else               { isWallRight = true; wallHitRight = trackedHit; }
            }
        }
    }

    void ExitWallrun()
    {
        rb.useGravity = true;
        state = State.AIR;
        targetTilt = 0f;
        wallrunCooldownTimer = wallrunCooldown;
        hasDoubleJump = true;
    }

    void WallrunHandler()
    {
        if (isGrounded)
        {
            if (state == State.WALLRUN) { rb.useGravity = true; state = State.GROUND; targetTilt = 0f; }
            return;
        }

        if (state == State.WALLRUN)
        {
            wallrunTimer -= Time.deltaTime;
            bool wallStillThere = wallrunOnLeft ? isWallLeft : isWallRight;

            if (!wallStillThere || wallrunTimer <= 0)
            {
                ExitWallrun();
                return;
            }

            wallNormal = wallrunOnLeft ? wallHitLeft.normal : wallHitRight.normal;
            targetTilt = wallrunOnLeft ? wallCameraTilt : -wallCameraTilt;

            if (jumpDown)
            {
                Vector3 jumpDir = (wallNormal + Vector3.up).normalized;
                rbVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z) + new Vector3(jumpDir.x, 0f, jumpDir.z) * wallJumpImpulse;
                rb.velocity = rbVel + new Vector3(0, jumpDir.y * wallJumpImpulse, 0);
                justJumpedTimer = 0.15f;
                justJumped = true;
                wallJumpedThisFrame = true;
                ExitWallrun();
            }
        }
        else if (state == State.AIR)
        {
            if (wallrunCooldownTimer > 0f) { wallrunCooldownTimer -= Time.deltaTime; return; }
            if (!isWallLeft && !isWallRight) return;

            float horizontalSpeed = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;
            if (horizontalSpeed < wallrunMinSpeed) return;

            if (isWallRight) { wallrunOnLeft = false; wallNormal = wallHitRight.normal; }
            else             { wallrunOnLeft = true;  wallNormal = wallHitLeft.normal; }

            if (Mathf.Abs(Vector3.Angle(wallNormal, Vector3.up) - 90f) > wallrunMaxWallAngle) return;

            // don't re-enter if player is moving away from the wall (e.g. just wall-jumped off a cylinder)
            Vector3 horizMeasured = new Vector3(measuredVelocity.x, 0f, measuredVelocity.z);
            if (Vector3.Dot(horizMeasured, -wallNormal) < -0.1f) return;

            wallrunTimer = wallrunMaxTime;
            rb.useGravity = false;
            state = State.WALLRUN;

            if (rb.velocity.y < 0f)
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        }
    }

    void Jumping()
    {
        if (justJumpedTimer > 0f) justJumpedTimer -= Time.deltaTime;
        justJumped = justJumpedTimer > 0f;

        if (isGrounded)
        {
            hasDoubleJump = true;
            if (jumpDown)
            {
                float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);
                Vector3 jumpDir = (slopeAngle > 0.5f && slopeAngle < maxSlopeAngle)
                    ? Vector3.Slerp(Vector3.up, groundHit.normal, slopeJumpNormalBlend).normalized
                    : Vector3.up;
                rb.AddForce(jumpDir * jumpImpulse, ForceMode.VelocityChange);
                justJumpedTimer = 0.15f;
                justJumped = true;
            }
        }
        else if (doubleJumpEnabled && state == State.AIR && jumpDown && hasDoubleJump && !wallJumpedThisFrame)
        {
            hasDoubleJump = false;

            Vector3 horizVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            Vector2 movVec = new Vector2(input.HorizontalInput(), input.VerticalInput()).normalized;
            if (doubleJumpDirAssist > 0f && movVec.magnitude > 0.1f && horizVel.magnitude > 0.1f)
            {
                Vector3 inputDir = (ori.forward * movVec.y + ori.right * movVec.x).normalized;
                Vector3 assistedDir = Vector3.Slerp(horizVel.normalized, inputDir, doubleJumpDirAssist);
                horizVel = assistedDir * horizVel.magnitude;
            }

            rbVel = new Vector3(horizVel.x, 0f, horizVel.z);
            rb.velocity = rbVel;
            rb.AddForce(Vector3.up * doubleJumpImpulse, ForceMode.VelocityChange);
        }

        wallJumpedThisFrame = false;
    }

    void CameraFov()
    {
        /*float spd = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;
        float targetFov = fov + (maxFov - fov) * Mathf.Clamp(spd / maxSpeed, 0, 1);
        Debug.Log(targetFov);
        camera.fieldOfView = targetFov;*/
    }

    void OnCollisionStay(Collision collision)
    {
        if (justJumped) return;
        foreach (ContactPoint contact in collision.contacts)
        {
            if (Vector3.Angle(contact.normal, Vector3.up) > maxSlopeAngle && rb.velocity.y > prevYVelocity)
            {
                rb.velocity = new Vector3(rb.velocity.x, Mathf.Max(prevYVelocity, 0f), rb.velocity.z);
                break;
            }
        }
    }

    void CrouchStateHandler()
    {
        float targetHeight = input.CrouchInput() ? crouchPlayerHeight : baseScale.y;
        float newHeight = Mathf.Lerp(rb.transform.localScale.y, targetHeight, crouchLerpSpeed * Time.deltaTime);
        rb.transform.localScale = new Vector3(baseScale.x, newHeight, baseScale.z);
    }

    public void SetInternalVelocity(Vector3 vel)
    {
        rbVel = vel;
    }

    void UpdateInputEdges()
    {
        jumpDown   = input.JumpInput()   && !prevJump;
        crouchDown = input.CrouchInput() && !prevCrouch;
        prevJump   = input.JumpInput();
        prevCrouch = input.CrouchInput();
    }

    void Update()
    {
        UpdateInputEdges();
        GroundCheck();
        WallCheck();

        if (state != State.WALLRUN)
        {
            bool crouchActive = input.CrouchInput() || rb.transform.localScale.y < baseScale.y - 0.05f;
            state = crouchActive ? State.CROUCH : (isGrounded ? State.GROUND : State.AIR);
        }

        if (wallrunEnabled) WallrunHandler();
        else if (state == State.WALLRUN) ExitWallrun();

        CrouchStateHandler();
        Jumping();
        CameraRotation();
        CameraPosition();
        //CursorState();
        Movement();
        CameraFov();
    }

}
