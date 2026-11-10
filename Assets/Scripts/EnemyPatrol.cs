using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyPatrol : MonoBehaviour, IDamageable
{
    [SerializeField] private Vector2 patrolPointA;
    [SerializeField] private Vector2 patrolPointB;
    [SerializeField, Min(0.1f)] private float moveSpeed = 2.1f;
    [SerializeField, Min(1)] private int maxHealth = 3;
    [SerializeField, Min(1)] private int contactDamage = 1;
    [SerializeField, Min(0.1f)] private float contactCooldown = 0.75f;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField, Min(0.04f)] private float walkFrameTime = 0.11f;

    private Rigidbody2D body;
    private BoxCollider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private int currentHealth;
    private bool movingToB = true;
    private bool dying;
    private float nextContactDamageTime;
    private float stunnedUntil;
    private Color normalColor;
    private Coroutine flashRoutine;
    private int walkFrameIndex;
    private float nextWalkFrameTime;

    public int CurrentHealth => currentHealth;

    public void Configure(Vector2 pointA, Vector2 pointB, float speed, int health, Sprite[] animationFrames)
    {
        patrolPointA = pointA;
        patrolPointB = pointB;
        moveSpeed = Mathf.Max(0.1f, speed);
        maxHealth = Mathf.Max(1, health);
        currentHealth = maxHealth;
        SetWalkFrames(animationFrames);
    }

    public void SetWalkFrames(Sprite[] animationFrames)
    {
        walkFrames = animationFrames;
        walkFrameIndex = 0;

        if (walkFrames != null && walkFrames.Length > 0)
            GetComponent<SpriteRenderer>().sprite = walkFrames[0];
    }

    public void SetPaintedFrames(Sprite[] walk,Sprite[] death)
    {
        SetWalkFrames(walk); deathFrames=death;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        normalColor = spriteRenderer.color;
        currentHealth = maxHealth;

        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
    }

    private void Update()
    {
        if (dying || Time.time < stunnedUntil || walkFrames == null || walkFrames.Length < 2 || Time.time < nextWalkFrameTime)
            return;

        nextWalkFrameTime = Time.time + walkFrameTime;
        walkFrameIndex = (walkFrameIndex + 1) % walkFrames.Length;
        spriteRenderer.sprite = walkFrames[walkFrameIndex];
    }

    private float unsupportedFallSpeed;
    private void FixedUpdate()
    {
        if (dying || Time.time < stunnedUntil)
            return;

        Vector2 target = movingToB ? patrolPointB : patrolPointA;
        target.y=body.position.y;
        Vector2 nextPosition = Vector2.MoveTowards(body.position, target, moveSpeed * Time.fixedDeltaTime);
                float footOffset=body.position.y-bodyCollider.bounds.min.y;
        int groundMask=1<<LayerMask.NameToLayer("Wall");
        var below=Physics2D.Raycast(body.position,Vector2.down,footOffset+.45f,groundMask);
        if(!below.collider || below.normal.y<.6f)
        {
            unsupportedFallSpeed+=Mathf.Abs(Physics2D.gravity.y)*Time.fixedDeltaTime;
            float drop=unsupportedFallSpeed*Time.fixedDeltaTime;
            var landing=Physics2D.Raycast(body.position,Vector2.down,footOffset+drop+.02f,groundMask);
            Vector2 fall=body.position;
            if(landing.collider&&landing.normal.y>.6f){fall.y=landing.point.y+footOffset;unsupportedFallSpeed=0;}
            else fall.y-=drop;
            body.MovePosition(fall);return;
        }
        unsupportedFallSpeed=0;
        float heading=Mathf.Sign(target.x-body.position.x);
        var edge=Physics2D.Raycast(new Vector2(nextPosition.x+heading*(bodyCollider.bounds.extents.x+.08f),body.position.y+.1f),Vector2.down,footOffset+.55f,groundMask);
        if(!edge.collider || edge.normal.y<.6f){movingToB=!movingToB;return;}
        nextPosition.y=below.point.y+footOffset;
        body.MovePosition(nextPosition);

        float direction = target.x - body.position.x;
        if (Mathf.Abs(direction) > 0.02f)
            spriteRenderer.flipX = direction < 0f;

        if (Vector2.SqrMagnitude(nextPosition - target) < 0.0025f)
            movingToB = !movingToB;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.collider);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (dying || Time.time < nextContactDamageTime)
            return;

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
            return;

        playerHealth.TakeContactDamage(contactDamage, transform.position);
        nextContactDamageTime = Time.time + contactCooldown;
    }

    public void TakeDamage(int damage)
    {
        if (dying || damage <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        stunnedUntil = Time.time + 0.16f;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HitFlashRoutine());

        if (currentHealth <= 0)
            StartCoroutine(DeathRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        spriteRenderer.color = new Color(1f, 0.24f, 0.32f, 1f);
        yield return new WaitForSeconds(0.1f);
        if (!dying)
            spriteRenderer.color = normalColor;
        flashRoutine = null;
    }

    private IEnumerator DeathRoutine()
    {
        dying = true;
        ImpulseResidue.Spawn(transform.position);
        bodyCollider.enabled = false;
        body.simulated = false;

        if(deathFrames!=null && deathFrames.Length>0)
        {
            if(flashRoutine!=null) StopCoroutine(flashRoutine);
            spriteRenderer.color=normalColor;
            foreach(Sprite frame in deathFrames)
            {
                spriteRenderer.sprite=frame;
                yield return new WaitForSeconds(.075f);
            }
            Destroy(gameObject);
            yield break;
        }

        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        const float duration = 0.24f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.Lerp(startScale, new Vector3(startScale.x * 1.3f, 0.05f, startScale.z), t);
            spriteRenderer.color = Color.Lerp(new Color(1f, 0.25f, 0.3f, 1f), new Color(0.1f, 0.9f, 1f, 0f), t);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnGUI()
    {
        if (dying || currentHealth >= maxHealth || Camera.main == null)
            return;

        Vector3 screenPoint = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.25f);
        if (screenPoint.z <= 0f)
            return;

        float x = screenPoint.x - 26f;
        float y = Screen.height - screenPoint.y;
        Rect background = new Rect(x, y, 52f, 7f);
        Rect fill = new Rect(x + 1f, y + 1f, 50f * currentHealth / maxHealth, 5f);

        Color oldColor = GUI.color;
        GUI.color = new Color(0.03f, 0.04f, 0.06f, 0.9f);
        GUI.DrawTexture(background, Texture2D.whiteTexture);
        GUI.color = new Color(0.95f, 0.16f, 0.23f, 0.95f);
        GUI.DrawTexture(fill, Texture2D.whiteTexture);
        GUI.color = oldColor;
    }
}


