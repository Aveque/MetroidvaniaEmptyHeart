using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DashAbilityPickup : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField] private float bobHeight = 0.08f;
    private Vector3 visualOrigin;
    private SpriteRenderer visualRenderer;
    private bool collected;
    private float messageUntil;

    public bool Collected => collected;

    public void Configure(Transform componentVisual) => visual = componentVisual;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        if (visual == null) return;
        visualOrigin = visual.localPosition;
        visualRenderer = visual.GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (collected)
        {
            if (Time.time >= messageUntil) Destroy(gameObject);
            return;
        }
        if (visual == null) return;
        visual.localPosition = visualOrigin + Vector3.up * (Mathf.Sin(Time.time * 2.8f) * bobHeight);
        if (visualRenderer != null)
        {
            float pulse = 0.85f + Mathf.Sin(Time.time * 3.5f) * 0.15f;
            visualRenderer.color = new Color(pulse, 1f, 1f, 1f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) => TryCollect(other.GetComponentInParent<PlayerMove>());
    private void OnTriggerStay2D(Collider2D other) => TryCollect(other.GetComponentInParent<PlayerMove>());

    public bool TryCollect(PlayerMove player)
    {
        if (collected || player == null || !player.isActiveAndEnabled) return false;
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null && health.CurrentHealth <= 0) return false;

        bool newlyUnlocked = player.UnlockDash();
        collected = true;
        GetComponent<BoxCollider2D>().enabled = false;
        if (visual != null) visual.gameObject.SetActive(false);
        // Keep only the notification alive; the component cannot be collected twice.
        messageUntil = Time.time + (newlyUnlocked ? 5f : 0f);
        return newlyUnlocked;
    }

    private void OnGUI()
    {
        if (!collected || Time.time >= messageUntil) return;
        EmptyHeartUI.Announcement("КОМПОНЕНТ ИНТЕГРИРОВАН", "ИМПУЛЬСНОЕ СУХОЖИЛИЕ",
            "Shift — рывок в сторону взгляда", Mathf.Clamp01(messageUntil - Time.time), 0.62f);
    }
}
