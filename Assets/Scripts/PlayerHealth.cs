using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    public event System.Action Died;
    [SerializeField, Min(1)] private int maxHealth = 5;
    [SerializeField, Min(0.05f)] private float invulnerabilityTime = 0.9f;
    [SerializeField, Min(0f)] private float knockbackForce = 7f;
    [SerializeField, Min(0f)] private float respawnDelay = 0.8f;

    private int currentHealth;
    private bool invulnerable;
    private bool respawning;
    private Vector3 spawnPoint;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;
    private PlayerMove movement;
    private Color normalColor;
    private Coroutine hurtRoutine;
    private float contactGraceUntil;

    public void GrantContactGrace(float duration)
    {
        contactGraceUntil = Mathf.Max(contactGraceUntil, Time.time + duration);
    }

    public int DamageVersion {get;private set;}
    public int Heal(int amount){if(amount<=0 || respawning || currentHealth<=0)return 0;int before=currentHealth;currentHealth=Mathf.Min(maxHealth,currentHealth+amount);return currentHealth-before;}
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsInvulnerable => invulnerable;
    public bool IsRespawning => respawning;
    public Vector3 RespawnPoint => spawnPoint;
    public RestCheckpoint ActiveCheckpoint { get; private set; }
    public void SetTravelSpawn(Vector2 position) { spawnPoint = position; }
    public void RestoreTravelHealth(int amount, RestCheckpoint checkpoint)
    {
        currentHealth=Mathf.Clamp(amount,1,maxHealth);
        if(checkpoint){ActiveCheckpoint=checkpoint;spawnPoint=checkpoint.SpawnPoint;}
    }

    public bool RestAt(RestCheckpoint checkpoint)
    {
        if (!checkpoint || respawning || currentHealth <= 0) return false;
        spawnPoint = checkpoint.SpawnPoint;
        ActiveCheckpoint = checkpoint;
        if (hurtRoutine != null) StopCoroutine(hurtRoutine);
        hurtRoutine = null;
        currentHealth = maxHealth;
        invulnerable = false;
        spriteRenderer.color = normalColor;
        GrantContactGrace(.75f);
        return true;
    }

    public void Configure(int healthAmount)
    {
        maxHealth = Mathf.Max(1, healthAmount);
        currentHealth = maxHealth;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMove>();
        normalColor = spriteRenderer.color;
        spawnPoint = transform.position;
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        TakeContactDamage(damage, transform.position - Vector3.right);
    }

    public void TakeContactDamage(int damage, Vector3 damageSource)
    {
        if (damage <= 0 || invulnerable || respawning || Time.time < contactGraceUntil)
            return;

        DamageVersion++;
        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (currentHealth <= 0)
        {
            if (hurtRoutine != null)
                StopCoroutine(hurtRoutine);
            StartCoroutine(RespawnRoutine());
            return;
        }

        float direction = transform.position.x >= damageSource.x ? 1f : -1f;
        if (body != null)
            body.linearVelocity = new Vector2(direction * knockbackForce, Mathf.Max(body.linearVelocity.y, knockbackForce * 0.55f));

        if (hurtRoutine != null)
            StopCoroutine(hurtRoutine);
        hurtRoutine = StartCoroutine(HurtRoutine());
    }

    private IEnumerator HurtRoutine()
    {
        invulnerable = true;
        float elapsed = 0f;

        while (elapsed < invulnerabilityTime)
        {
            elapsed += 0.09f;
            spriteRenderer.color = spriteRenderer.color.a > 0.5f
                ? new Color(1f, 0.3f, 0.35f, 0.28f)
                : new Color(1f, 0.55f, 0.6f, 1f);
            yield return new WaitForSeconds(0.09f);
        }

        spriteRenderer.color = normalColor;
        invulnerable = false;
        hurtRoutine = null;
    }

    private IEnumerator RespawnRoutine()
    {
        respawning = true;
        invulnerable = true;
        Died?.Invoke();

        if (movement != null)
            movement.enabled = false;
        if (body != null)
            body.linearVelocity = Vector2.zero;

        spriteRenderer.color = new Color(1f, 0.1f, 0.15f, 0.25f);
        yield return new WaitForSeconds(respawnDelay);

        transform.position = spawnPoint;
        if (body != null)
            body.linearVelocity = Vector2.zero;

        Physics2D.SyncTransforms();
        CameraFollow cameraFollow = Camera.main != null
            ? Camera.main.GetComponent<CameraFollow>()
            : FindFirstObjectByType<CameraFollow>();
        if (cameraFollow != null)
            cameraFollow.SnapToPlayerRoom();

        currentHealth = maxHealth;
        spriteRenderer.color = normalColor;
        if (movement != null)
            movement.enabled = true;

        yield return new WaitForSeconds(0.65f);
        invulnerable = false;
        respawning = false;
    }

    private void OnGUI()
    {
        if(StationAtlas.MapVisible)return;
        EmptyHeartUI.PlayerHUD(currentHealth, maxHealth, movement != null && movement.DashUnlocked,
            movement != null ? movement.DashRecharge : 0f);
    }
}


