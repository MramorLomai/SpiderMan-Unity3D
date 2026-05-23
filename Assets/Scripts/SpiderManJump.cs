using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderManJump : SpiderMan
{
    [Header("Jump Parameters")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float timeToJumpApex = 0.5f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private float groundCheckOffset = 0.1f;
    [SerializeField] private LayerMask groundLayer = 1 << 9;
    [SerializeField] private AudioClip LandingAudioClip;

    [Header("Coyote Time & Jump Buffer")]
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.2f;

    [Header("Double Jump")]
    [SerializeField] private bool enableDoubleJump = true;
    [SerializeField] private float doubleJumpHeight = 1.8f;
    [SerializeField] private int maxDoubleJumps = 1;
    [SerializeField] private AudioClip JumpAudioClip;

    [Header("Animation Debug")]
    [SerializeField] private bool debugAnimations = true;
    [SerializeField] private bool showDebugVisuals = true;

    private float gravity;
    private float jumpSpeed;
    private float distToGround;

    private bool wasGroundedLastFrame;
    public bool isJumping;
    private bool isFalling;

    private int doubleJumpsRemaining;
    private bool hasDoubleJumped;

    private float lastGroundedTime;
    private float lastJumpPressTime;
    private float lastAirborneTime;

    private float lastLandingTime;
    [SerializeField] private float landingSoundCooldown = 0.3f;

    private int animIDJump;
    private int animIDDoubleJump;
    private int animIDFreeFall;
    private int animIDGrounded;

    private float lastJumpAnimationTime;
    private bool jumpAnimationTriggered;

    private void Start()
    {
        if (collider != null)
        {
            distToGround = collider.bounds.extents.y;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (LandingAudioClip != null || JumpAudioClip != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        animIDJump = Animator.StringToHash("Jump");
        animIDDoubleJump = Animator.StringToHash("DoubleJump");
        animIDFreeFall = Animator.StringToHash("FreeFall");
        animIDGrounded = Animator.StringToHash("Grounded");

        if (_animator == null)
        {
            Debug.LogError("Animator не найден на объекте " + gameObject.name);
        }

        CalculateGravity();
        ResetDoubleJumps();

        lastGroundedTime = -1f;
        lastJumpPressTime = -1f;
        lastJumpAnimationTime = -1f;
        jumpAnimationTriggered = false;
        lastLandingTime = -1f;
    }

    private void CalculateGravity()
    {
        gravity = -(2f * jumpHeight) / (timeToJumpApex * timeToJumpApex);
        jumpSpeed = Mathf.Abs(gravity) * timeToJumpApex;
    }

    private void Update()
    {
        if (transform.position.y < -20)
        {
            transform.position = startingPosition;
            jumpVelocity = Vector3.zero;
            ResetDoubleJumps();
        }

        CheckGrounded();

        if (Input.GetKeyDown(KeyCode.Space))
            lastJumpPressTime = Time.time;

        if (!isSwinging)
        {
            jumpPhysics();
        }
        else
        {
            if (GetComponent<SpiderManJump>() != null)
            {
                GetComponent<SpiderManJump>().ResetJumpState();
            }

            jumpVelocity.y = 0;
        }

        UpdateAnimations();

        if (showDebugVisuals)
            DebugDraw();
    }

    private void CheckGrounded()
    {
        Vector3 groundCheckPos = transform.position + Vector3.down * groundCheckOffset;

        RaycastHit hit;
        bool grounded = Physics.SphereCast(
            transform.position,
            groundCheckRadius * 0.8f,
            Vector3.down,
            out hit,
            distToGround + groundCheckOffset + 0.1f,
            groundLayer
        );

        Collider[] groundColliders = Physics.OverlapSphere(
            groundCheckPos,
            groundCheckRadius,
            groundLayer
        );

        bool groundedOverlap = groundColliders.Length > 0;
        bool wasGrounded = isGrounded;
        isGrounded = grounded || groundedOverlap;

        // Landing detection with cooldown
        if (isGrounded && !wasGrounded && Time.time - lastLandingTime > landingSoundCooldown)
        {
            PlayLandingSound();
            lastLandingTime = Time.time;

            // Reset double jumps when landing
            ResetDoubleJumps();
        }

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            lastAirborneTime = 0f;
        }
        else
        {
            if (lastAirborneTime == 0f)
            {
                lastAirborneTime = Time.time;
            }
        }

        wasGroundedLastFrame = isGrounded;
    }

    private void PlayLandingSound()
    {
        if (LandingAudioClip != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(LandingAudioClip);
            }
            else
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.position, 0.8f);
            }
        }
    }

    private void PlayJumpSound()
    {
        if (JumpAudioClip != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(JumpAudioClip);
            }
            else
            {
                AudioSource.PlayClipAtPoint(JumpAudioClip, transform.position, 0.8f);
            }
        }
    }

    public void jumpPhysics()
    {
        if (!isGrounded && !IsNearGround())
        {
            jumpVelocity.y += gravity * Time.deltaTime;
        }
        else if (!isJumping && !hasDoubleJumped)
        {
            jumpVelocity.y = Mathf.Max(0, jumpVelocity.y);
        }

        bool coyoteTimeActive = (Time.time - lastGroundedTime) < coyoteTime;
        bool jumpBuffered = (Time.time - lastJumpPressTime) < jumpBufferTime;

        bool canJump = jumpBuffered && (isGrounded || coyoteTimeActive) && !isJumping;

        bool canDoubleJump = enableDoubleJump &&
                             !isGrounded &&
                             !coyoteTimeActive &&
                             jumpBuffered &&
                             doubleJumpsRemaining > 0 &&
                             !isJumping;

        if (canJump)
        {
            PerformJump();
            lastJumpPressTime = -1f;
        }
        else if (canDoubleJump)
        {
            PerformDoubleJump();
            lastJumpPressTime = -1f;
        }

        isFalling = !isGrounded && jumpVelocity.y < 0 && !isJumping && !hasDoubleJumped;

        if (jumpVelocity.y < -25f)
        {
            jumpVelocity.y = -25f;
        }

        if (isGrounded && jumpVelocity.y <= 0)
        {
            isJumping = false;
            hasDoubleJumped = false;
        }
    }

    private bool IsNearGround()
    {
        return Physics.Raycast(transform.position, Vector3.down, distToGround + 0.5f, groundLayer);
    }

    private void PerformJump()
    {
        jumpVelocity.y = jumpSpeed;
        isJumping = true;
        jumpAnimationTriggered = true;
        lastJumpAnimationTime = Time.time;
        hasDoubleJumped = false;

        lastGroundedTime = -1f;
        lastJumpPressTime = -1f;

        PlayJumpSound();
        SetJumpAnimation(true);
    }

    public void PerformDoubleJump()
    {
        float doubleJumpSpeed = enableDoubleJump ?
            Mathf.Abs(gravity) * Mathf.Sqrt(2 * doubleJumpHeight / Mathf.Abs(gravity)) :
            jumpSpeed * 0.9f;

        jumpVelocity.y = doubleJumpSpeed;
        isJumping = true;
        hasDoubleJumped = true;
        doubleJumpsRemaining--;
        jumpAnimationTriggered = true;
        lastJumpAnimationTime = Time.time;

        lastGroundedTime = -1f;
        lastJumpPressTime = -1f;

        PlayJumpSound();
        SetDoubleJumpAnimation(true);
    }

    private void SetJumpAnimation(bool value)
    {
        if (_animator == null) return;

        _animator.SetBool(animIDGrounded, false);
        _animator.SetBool(animIDFreeFall, false);
        _animator.SetBool(animIDDoubleJump, false);
        _animator.SetBool(animIDJump, value);

        _animator.Update(0);
    }

    private void SetDoubleJumpAnimation(bool value)
    {
        if (_animator == null) return;

        _animator.SetBool(animIDJump, false);
        _animator.SetBool(animIDDoubleJump, value);
        _animator.SetBool(animIDFreeFall, false);
    }

    private void UpdateAnimations()
    {
        if (_animator == null) return;

        _animator.SetBool(animIDGrounded, isGrounded);
        _animator.SetBool(animIDFreeFall, isFalling && !hasDoubleJumped);

        if (isJumping)
        {
            if (jumpVelocity.y <= 0 || isGrounded)
            {
                _animator.SetBool(animIDJump, false);
                _animator.SetBool(animIDDoubleJump, false);

                if (isGrounded)
                {
                    isJumping = false;
                    hasDoubleJumped = false;
                }
            }
        }

        if (isJumping && jumpVelocity.y < 0 && !isGrounded && !hasDoubleJumped)
        {
            _animator.SetBool(animIDJump, false);
            _animator.SetBool(animIDDoubleJump, false);
            _animator.SetBool(animIDFreeFall, true);
        }
    }

    private void ResetDoubleJumps()
    {
        doubleJumpsRemaining = maxDoubleJumps;
        hasDoubleJumped = false;
    }

    private void DebugDraw()
    {
        Vector3 groundCheckPos = transform.position + Vector3.down * groundCheckOffset;
        Color groundColor = isGrounded ? Color.green : Color.red;
        Debug.DrawRay(transform.position, Vector3.down * (distToGround + 0.1f), groundColor);
    }

    private void OnDrawGizmos()
    {
        if (!showDebugVisuals) return;

        Vector3 groundCheckPos = transform.position + Vector3.down * groundCheckOffset;

        if (Application.isPlaying)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
        }
        else
        {
            Gizmos.color = Color.yellow;
        }

        Gizmos.DrawWireSphere(groundCheckPos, groundCheckRadius);
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }

    public void UpdateJumpParameters(float newJumpHeight, float newTimeToJumpApex)
    {
        jumpHeight = newJumpHeight;
        timeToJumpApex = newTimeToJumpApex;
        CalculateGravity();
    }

    public void ResetJumpState()
    {
        isJumping = false;
        isFalling = false;
        hasDoubleJumped = false;
        ResetDoubleJumps();

        if (_animator != null)
        {
            _animator.SetBool(animIDJump, false);
            _animator.SetBool(animIDDoubleJump, false);
            _animator.SetBool(animIDFreeFall, false);
            _animator.SetBool(animIDGrounded, isGrounded);
        }
    }
}