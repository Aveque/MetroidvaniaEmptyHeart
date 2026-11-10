using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerMove : MonoBehaviour
{
    private const int AttackSide = 0;
    private const int AttackUp = 1;
    private const int AttackDown = 2;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 25f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private int maxJumps = 2;

    [Header("Wall Jump")]
    [SerializeField] private float wallJumpForce = 12f;
    [SerializeField] private float wallJumpUpForce = 12f;
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private float wallCheckDistance = 0.15f;
    [SerializeField] private float wallJumpControlLock = 0.18f;
    [SerializeField] private float wallJumpGraceTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Dash")]
    [SerializeField] private bool dashUnlocked;
    [SerializeField] private float dashDistance = 4f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.5f;

    [Header("Attack")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackDuration = 0.35f;
    [SerializeField] private float attackHitDelay = 0.12f;
    [SerializeField] private float downwardBounceSpeed = 5f;

    [SerializeField] private LayerMask attackableLayers = ~0;

    [SerializeField]
    private Vector2 horizontalAttackSize =
        new Vector2(1.0f, 0.8f);

    [SerializeField]
    private Vector2 verticalAttackSize =
        new Vector2(0.8f, 1.0f);

    [SerializeField] private float horizontalAttackOffset = 0.7f;
    [SerializeField] private float verticalAttackOffset = 0.7f;

    [Header("Layers")]
    [SerializeField] private LayerMask wallLayer;

    private Rigidbody2D rb;
    private Collider2D col;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float moveInput;
    private float facingDirection = 1f;

    private int jumpsLeft;
    public bool DoubleJumpUnlocked=>StationJourney.Collected.Contains("return-drive");
    private int AvailableJumps=>DoubleJumpUnlocked?Mathf.Max(2,maxJumps):1;
    public void UnlockDoubleJump(){StationJourney.Collected.Add("return-drive");jumpsLeft=Mathf.Max(jumpsLeft,1);}

    private bool isGrounded;
    private bool isTouchingWall;
    private bool wallOnRight;
    private bool isDashing;
    private bool isAttacking;

    private float wallJumpLockedUntil;
    private float lastWallTouchTime = -999f;
    private bool lastWallOnRight;
    private float jumpRequestedUntil = -999f;
    private float ignoreGroundUntil;
    private float protectBounceUntil;
    private PhysicsMaterial2D movementMaterial;

    public bool IsWallSliding => isTouchingWall && !isGrounded && !isDashing
        && Time.time >= wallJumpLockedUntil && rb.linearVelocity.y <= 0.05f;

    // 0 = боковая атака
    // 1 = вверх
    // 2 = вниз
    private int attackDirection = AttackSide;

    // Направление именно текущего удара.
    // Не меняется, если игрок нажмёт другую кнопку
    // во время уже выполняющейся атаки.
    private float attackFacingDirection = 1f;

    private float lastDashTime = -999f;

    private Coroutine attackCoroutine;
    private Coroutine dashCoroutine;
    private float dashGravityScale;
    private float attackStarted, lastAttackEnded = -999f, lastComboFacing;
    private bool lastAttackWasSide, queuedAttack;
    private int queuedDirection;
    private float queuedFacing, queuedUntil;
    private SwordHitFeedback hitFeedback;
    public int ComboStep { get; private set; }
    public bool IsAttacking => isAttacking;
    public float AttackProgress => isAttacking ? Mathf.Clamp01((Time.time - attackStarted) / Mathf.Max(.05f, attackDuration)) : 0;
    public bool ComboWindowActive => !isAttacking && lastAttackWasSide && Time.time - lastAttackEnded < .55f;
    public float ComboRestBlend => Mathf.InverseLerp(.35f, .55f, Time.time - lastAttackEnded);

    public bool DashUnlocked => dashUnlocked;
    public bool IsDashing => isDashing;
    public float EffectiveDashCooldown => dashCooldown * PlayerOrgans.DashCooldownMultiplier;
    public float DashRecharge => Mathf.Clamp01((lastDashTime + EffectiveDashCooldown - Time.time) / Mathf.Max(0.01f, EffectiveDashCooldown));

    public bool UnlockDash()
    {
        if (dashUnlocked) return false;
        dashUnlocked = true;
        lastDashTime = -999f;
        return true;
    }

    public bool TryDash()
    {
        if (PlayerOrgans.BlocksInput || PlayerOrgans.IsChanneling || !isActiveAndEnabled || !dashUnlocked || isDashing || isAttacking ||
            Time.timeScale <= 0f || Time.time < lastDashTime + EffectiveDashCooldown)
            return false;

        dashCoroutine = StartCoroutine(Dash());
        return true;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Wall friction must not cancel gravity while the player pushes into a wall.
        movementMaterial = new PhysicsMaterial2D("Player Movement - No Wall Friction")
        {
            friction = 0f,
            bounciness = 0f
        };
        col.sharedMaterial = movementMaterial;

        jumpsLeft = AvailableJumps;

        facingDirection = spriteRenderer.flipX ? -1f : 1f;
        UpdateFacing();
        CheckAnimator();
        hitFeedback = GetComponent<SwordHitFeedback>();
        if (!hitFeedback) hitFeedback = gameObject.AddComponent<SwordHitFeedback>();
    }

    private void Update()
    {
        if(StationLoreNode.BlocksInput || PlayerOrgans.BlocksInput || PlayerOrgans.IsChanneling)return;
        ReadInput();

        CheckGround();
        CheckWall();

        // Прыжок.
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            jumpRequestedUntil = Time.time + jumpBufferTime;
        }

        if (Time.time <= jumpRequestedUntil && Jump())
            jumpRequestedUntil = -999f;

        // Управление высотой прыжка.
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            CutJump();
        }

        // Dash.
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame)
            TryDash();

        // Атака на ЛКМ.
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            !isDashing)
        {
            RequestAttack();
        }

        UpdateAnimator();
    }

    private void OnDisable()
    {
        if (dashCoroutine != null)
            StopCoroutine(dashCoroutine);
        dashCoroutine = null;
        if (isDashing && rb != null)
            rb.gravityScale = dashGravityScale;
        isDashing = false;
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);
        attackCoroutine = null;
        isAttacking = false;
        queuedAttack = false; lastAttackWasSide = false; lastAttackEnded = -999f; ComboStep = 0;
        jumpRequestedUntil = -999f;
        wallJumpLockedUntil = 0f;
        lastWallTouchTime = -999f;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool("IsAttacking", false);
            animator.SetBool("IsDashing", false);
        }
    }

    private void OnDestroy()
    {
        if (movementMaterial != null)
            Destroy(movementMaterial);
    }

    private void OnValidate()
    {
        attackDuration = Mathf.Max(0.05f, attackDuration);
        attackHitDelay = Mathf.Clamp(attackHitDelay, 0f, attackDuration);
        attackDamage = Mathf.Max(0, attackDamage);
    }

    private void FixedUpdate()
    {
        if(PlayerOrgans.IsChanneling){rb.linearVelocity=Vector2.zero;return;}
        if (isDashing)
            return;

        CheckGround();
        CheckWall();
        Move();

        // Скольжение по стене.
        if (IsWallSliding)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                Mathf.Max(
                    rb.linearVelocity.y,
                    -wallSlideSpeed
                )
            );
        }

    }

    // =========================================
    // INPUT И НАПРАВЛЕНИЕ
    // =========================================

    private void ReadInput()
    {
        moveInput = 0f;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.aKey.isPressed)
            moveInput = -1f;

        if (Keyboard.current.dKey.isPressed)
            moveInput = 1f;

        if (moveInput != 0f && !isAttacking && !isDashing && Time.time >= wallJumpLockedUntil)
        {
            facingDirection = moveInput;
            UpdateFacing();
        }
    }

    private void UpdateFacing()
    {
        if (spriteRenderer == null)
            return;

        // Предполагается, что исходный спрайт
        // смотрит вправо.
        spriteRenderer.flipX = facingDirection < 0f;
    }

    // =========================================
    // MOVEMENT
    // =========================================

    private void Move()
    {
        if (Time.time < wallJumpLockedUntil)
            return;

        float targetSpeed = moveInput * moveSpeed * (PlayerOrgans.Has(4)?1.15f:1f);

        float speedDifference =
            targetSpeed - rb.linearVelocity.x;

        float accelerationRate =
            Mathf.Abs(targetSpeed) > 0.01f
                ? acceleration
                : deceleration;

        float movement = speedDifference * accelerationRate;

        rb.AddForce(Vector2.right * movement);
    }

    // =========================================
    // JUMP
    // =========================================

    private bool Jump()
    {
        // Во время атаки и Dash новый прыжок не запускаем.
        if (isAttacking || isDashing)
            return false;

        // Прыжок от стены.
        if (!isGrounded && Time.time >= wallJumpLockedUntil &&
            (isTouchingWall || Time.time - lastWallTouchTime <= wallJumpGraceTime))
        {
            float horizontalDirection =
                (isTouchingWall ? wallOnRight : lastWallOnRight) ? -1f : 1f;

            wallJumpLockedUntil = Time.time + wallJumpControlLock;
            lastWallTouchTime = -999f;
            ignoreGroundUntil = Time.time + 0.1f;
            isGrounded = false;

            rb.linearVelocity = new Vector2(
                horizontalDirection * wallJumpForce,
                wallJumpUpForce
            );

            jumpsLeft = AvailableJumps - 1;

            facingDirection = horizontalDirection;
            UpdateFacing();

            TriggerJumpAnimation();

            return true;
        }

        // Обычный или двойной прыжок.
        if (jumpsLeft > 0)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );

            jumpsLeft--;
            ignoreGroundUntil = Time.time + 0.1f;
            isGrounded = false;

            TriggerJumpAnimation();
            return true;
        }
        return false;
    }

    private void TriggerJumpAnimation()
    {
        if (animator == null)
            return;

        animator.ResetTrigger("Jump");
        animator.SetTrigger("Jump");
    }

    private void CutJump()
    {
        if (rb.linearVelocity.y > 0 && Time.time >= protectBounceUntil)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * jumpCutMultiplier
            );
        }
    }

    // =========================================
    // ATTACK
    // =========================================

    private void StartAttack()
    {
        if (isAttacking || isDashing)
            return;

        DetermineAttackDirection();

        BeginAttack();
    }

    // One buffered click, consumed only near the end of the current swing.
    // Holding LMB never generates extra swings.
    public bool RequestAttack()
    {
        if (PlayerOrgans.BlocksInput || PlayerOrgans.IsChanneling || !isActiveAndEnabled || isDashing || Time.timeScale <= 0f) return false;
        if (!isAttacking) { StartAttack(); return true; }
        if (queuedAttack || Time.time - attackStarted < Mathf.Max(0, attackDuration - .2f)) return false;
        ReadAttackIntent(out queuedDirection, out queuedFacing);
        queuedAttack = true; queuedUntil = Time.time + .23f;
        return true;
    }

    private void BeginAttack()
    {

        if (Time.timeScale <= 0f)
            return;

        isAttacking = true;
        ComboStep = attackDirection == AttackSide && lastAttackWasSide &&
            Time.time - lastAttackEnded < .55f && attackFacingDirection == lastComboFacing
            ? (ComboStep + 1) % 3 : 0;
        lastAttackWasSide = attackDirection == AttackSide;
        lastComboFacing = attackFacingDirection;
        if (attackDirection == AttackSide) facingDirection = attackFacingDirection;
        attackStarted = Time.time;
        UpdateFacing();

        if (animator != null)
        {
            // Не оставляем старый Trigger прыжка
            // активным на время атаки.
            animator.ResetTrigger("Jump");

            animator.SetFloat(
                "AttackDirection",
                attackDirection
            );

            animator.SetFloat("AttackSpeed", 0.35f / Mathf.Max(0.05f, attackDuration));
            animator.SetBool("IsAttacking", true);
            // Start the actual sword animation on the click, from its first frame.
            string attackState = attackDirection == AttackUp ? "Attack_Up"
                : attackDirection == AttackDown ? "Attack_Down" : "Attack_Side";
            animator.Play("Base Layer." + attackState, 0, 0f);
        }

        attackCoroutine = StartCoroutine(AttackRoutine());
    }

    private void DetermineAttackDirection()
    {
        ReadAttackIntent(out attackDirection, out attackFacingDirection);
    }

    private void ReadAttackIntent(out int direction, out float facing)
    {
        direction = AttackSide; facing = facingDirection;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.wKey.isPressed) direction = AttackUp;
        else if (keyboard.sKey.isPressed) direction = AttackDown;
        else if (keyboard.aKey.isPressed) facing = -1;
        else if (keyboard.dKey.isPressed) facing = 1;
    }

    private IEnumerator AttackRoutine()
    {
        // Ждём момента удара в анимации.
        float duration = Mathf.Max(0.05f, attackDuration);
        float hitDelay = Mathf.Clamp(attackHitDelay, 0f, duration);
        if (hitDelay > 0f)
        {
            yield return new WaitForSeconds(hitDelay);
        }

        PerformAttack();

        // Держим состояние атаки до конца заданного времени.
        float remainingDuration =
            duration - hitDelay;

        if (remainingDuration > 0f)
        {
            yield return new WaitForSeconds(remainingDuration);
        }

        isAttacking = false;
        lastAttackEnded = Time.time;

        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
        }

        attackCoroutine = null;
        if (queuedAttack && Time.time <= queuedUntil && isActiveAndEnabled)
        {
            queuedAttack = false;
            attackDirection = queuedDirection; attackFacingDirection = queuedFacing;
            BeginAttack();
        }
        else queuedAttack = false;
    }

    private void PerformAttack()
    {
        Vector2 attackCenter;
        Vector2 attackSize;

        Vector2 playerCenter = col.bounds.center;

        // Определяем область удара.
        switch (attackDirection)
        {
            case AttackUp:
                attackCenter =
                    playerCenter +
                    Vector2.up * verticalAttackOffset;

                attackSize = verticalAttackSize;
                break;

            case AttackDown:
                attackCenter =
                    playerCenter +
                    Vector2.down * verticalAttackOffset;

                attackSize = verticalAttackSize;
                break;

            default:
                attackCenter =
                    playerCenter +
                    Vector2.right *
                    attackFacingDirection *
                    horizontalAttackOffset;

                attackSize = horizontalAttackSize;
                break;
        }

        // Получаем все коллайдеры в области удара.
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            attackCenter,
            attackSize,
            0f,
            attackableLayers
        );

        // Один враг может иметь несколько коллайдеров.
        // Не наносим ему урон несколько раз за один удар.
        HashSet<IDamageable> damagedTargets =
            new HashSet<IDamageable>();
        bool bounce = false;

        foreach (Collider2D hit in hits)
        {
            if (hit.attachedRigidbody == rb || hit.transform.IsChildOf(transform))
                continue;

            IDamageable target =
                hit.GetComponentInParent<IDamageable>();

            if (target != null && damagedTargets.Add(target))
            {
                EnemyPatrol enemy = hit.GetComponentInParent<EnemyPatrol>();
                SynapticSentry sentry = hit.GetComponentInParent<SynapticSentry>();
                BiomechBoss boss = hit.GetComponentInParent<BiomechBoss>();
                MatriarchBoss flower = hit.GetComponentInParent<MatriarchBoss>();
                bool alive = enemy != null ? enemy.CurrentHealth > 0 : sentry != null ? sentry.CurrentHealth > 0 : boss != null ? boss.ActiveBody : flower != null ? flower.CanTakeDamage : true;
                bool armored = (boss != null && !boss.CanTakeDamage) || (flower != null && !flower.CanTakeDamage);
                if (attackDamage > 0 && attackDirection == AttackDown && !isGrounded &&
                    ((enemy != null && enemy.CurrentHealth > 0) || (sentry != null && sentry.CurrentHealth > 0) || (boss != null && boss.CanTakeDamage) || (flower != null && flower.CanTakeDamage)) &&
                    col.bounds.center.y > hit.bounds.center.y + 0.1f)
                    bounce = true;
                target.TakeDamage(attackDamage+(PlayerOrgans.Has(3)?1:0));
                if (alive && attackDamage > 0 && hitFeedback)
                {
                    Vector2 direction = attackDirection == AttackUp ? Vector2.up : attackDirection == AttackDown
                        ? Vector2.down : Vector2.right * attackFacingDirection;
                    hitFeedback.Emit(hit.ClosestPoint(playerCenter), direction, armored);
                }
            }
        }

        if (bounce)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, downwardBounceSpeed);
            jumpsLeft = Mathf.Max(0, AvailableJumps - 1);
            lastDashTime = -999f;
            ignoreGroundUntil = Time.time + 0.12f;
            protectBounceUntil = Time.time + 0.15f;
            isGrounded = false;
            PlayerHealth health = GetComponent<PlayerHealth>();
            if (health != null)
                health.GrantContactGrace(0.18f);
        }
    }

    // =========================================
    // GROUND CHECK
    // =========================================

    private void CheckGround()
    {
        if (Time.time < ignoreGroundUntil)
        {
            isGrounded = false;
            return;
        }
        Bounds bounds = col.bounds;

        Vector2 origin = new Vector2(
            bounds.center.x,
            bounds.min.y + 0.04f
        );

        RaycastHit2D hit = Physics2D.BoxCast(
            origin,
            new Vector2(bounds.size.x * 0.75f, 0.04f),
            0f,
            Vector2.down,
            0.1f,
            wallLayer
        );

        isGrounded = hit.collider != null && hit.normal.y > 0.6f && rb.linearVelocity.y <= 0.1f;

        if (isGrounded)
        {
            jumpsLeft = AvailableJumps;
        }
    }

    // =========================================
    // WALL CHECK
    // =========================================

    private void CheckWall()
    {
        Bounds bounds = col.bounds;
        Vector2 size = new Vector2(Mathf.Max(0.02f, bounds.size.x - 0.06f), bounds.size.y * 0.65f);
        RaycastHit2D leftHit = Physics2D.BoxCast(bounds.center, size, 0f,
            Vector2.left, wallCheckDistance + 0.03f, wallLayer);
        RaycastHit2D rightHit = Physics2D.BoxCast(bounds.center, size, 0f,
            Vector2.right, wallCheckDistance + 0.03f, wallLayer);
        bool left = leftHit.collider != null && leftHit.normal.x > 0.6f;
        bool right = rightHit.collider != null && rightHit.normal.x < -0.6f;
        isTouchingWall = left || right;
        if (!isTouchingWall) return;

        wallOnRight = right && (!left || moveInput >= 0f);
        if (!isGrounded && Time.time >= wallJumpLockedUntil)
        {
            lastWallTouchTime = Time.time;
            lastWallOnRight = wallOnRight;
        }
    }

    // =========================================
    // ANIMATOR
    // =========================================

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        animator.SetFloat(
            "Speed",
            Mathf.Abs(moveInput)
        );

        animator.SetBool(
            "IsGrounded",
            isGrounded
        );

        bool wallSliding = IsWallSliding;

        if (wallSliding && !isAttacking && !isDashing)
        {
            animator.ResetTrigger("Jump");
            facingDirection = wallOnRight ? 1f : -1f;
            UpdateFacing();
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.WallSlide"))
                animator.Play("Base Layer.WallSlide", 0, 0f);
        }

        animator.SetBool(
            "IsWallSliding",
            wallSliding
        );

        animator.SetBool(
            "IsDashing",
            isDashing
        );

        animator.SetFloat(
            "VerticalVelocity",
            rb.linearVelocity.y
        );

        animator.SetBool(
            "IsRising",
            rb.linearVelocity.y > 0.01f
        );

        animator.SetBool(
            "IsAttacking",
            isAttacking
        );

        animator.SetFloat(
            "AttackDirection",
            attackDirection
        );
    }

    // =========================================
    // ANIMATOR CHECK
    // =========================================

    private void CheckAnimator()
    {
        if (animator == null)
        {
            Debug.LogError("PlayerMove: Animator не найден!");
            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError(
                "PlayerMove: у Animator не назначен Controller!"
            );

            return;
        }

        CheckParameter("Speed");
        CheckParameter("IsGrounded");
        CheckParameter("IsWallSliding");
        CheckParameter("IsDashing");
        CheckParameter("VerticalVelocity");
        CheckParameter("Jump");
        CheckParameter("IsAttacking");
        CheckParameter("AttackDirection");
        CheckParameter("IsRising");
        CheckParameter("AttackSpeed");
    }

    private void CheckParameter(string parameterName)
    {
        foreach (AnimatorControllerParameter parameter
                 in animator.parameters)
        {
            if (parameter.name == parameterName)
                return;
        }

        Debug.LogError(
            "PlayerMove: параметр Animator '" +
            parameterName +
            "' не найден!"
        );
    }

    // =========================================
    // DASH
    // =========================================

    private IEnumerator Dash()
    {
        if (!dashUnlocked) yield break;
        isDashing = true;
        lastDashTime = Time.time;

        float direction = moveInput;

        if (Mathf.Abs(direction) < 0.01f)
        {
            direction = facingDirection;
        }

        facingDirection = direction;
        UpdateFacing();

        Vector2 startPosition = rb.position;

        Vector2 targetPosition =
            startPosition +
            Vector2.right * direction * dashDistance;

        // Resolve the whole Dash path before moving. This prevents successive
        // MovePosition targets from carrying the player through a thin arena seal.
        float clearDistance = dashDistance;
        foreach (RaycastHit2D obstruction in Physics2D.BoxCastAll(col.bounds.center,
            col.bounds.size * 0.95f, 0f, Vector2.right * direction, dashDistance, wallLayer))
        {
            if (!obstruction.collider || obstruction.collider.GetComponentInParent<DashMembrane>() != null) continue;
            clearDistance = Mathf.Min(clearDistance, Mathf.Max(0f, obstruction.distance - 0.04f));
        }
        targetPosition = startPosition + Vector2.right * direction * clearDistance;

        float elapsed = 0f;

        dashGravityScale = rb.gravityScale;

        rb.gravityScale = 0f;

        while (elapsed < dashDuration)
        {
            elapsed += Time.fixedDeltaTime;

            float t = elapsed / dashDuration;

            rb.MovePosition(
                Vector2.Lerp(
                    startPosition,
                    targetPosition,
                    t
                )
            );

            yield return new WaitForFixedUpdate();
        }

        rb.gravityScale = dashGravityScale;

        isDashing = false;
        dashCoroutine = null;
    }

    // =========================================
    // ATTACK HITBOX PREVIEW
    // =========================================

    private void OnDrawGizmosSelected()
    {
        Collider2D playerCollider = GetComponent<Collider2D>();

        Vector2 playerCenter = playerCollider != null
            ? (Vector2)playerCollider.bounds.center
            : (Vector2)transform.position;

        Vector2 center;
        Vector2 size;

        switch (attackDirection)
        {
            case AttackUp:
                center = playerCenter +
                         Vector2.up * verticalAttackOffset;

                size = verticalAttackSize;
                break;

            case AttackDown:
                center = playerCenter +
                         Vector2.down * verticalAttackOffset;

                size = verticalAttackSize;
                break;

            default:
                center = playerCenter +
                         Vector2.right *
                         attackFacingDirection *
                         horizontalAttackOffset;

                size = horizontalAttackSize;
                break;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, size);
    }
}


