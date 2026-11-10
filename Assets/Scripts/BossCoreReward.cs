using UnityEngine;

public class BossCoreReward : MonoBehaviour
{
    private bool collected;
    private float showUntil;
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (collected || player == null || player.CurrentHealth <= 0) return;
        collected = true; showUntil = Time.time + 4f;
        StationJourney.CoreCollected = true;
        player.Configure(player.MaxHealth + 1);
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;
    }
    private void OnGUI()
    {
        if (!collected || Time.time >= showUntil) return;
        EmptyHeartUI.Announcement("КОНТУР УСИЛЕН", "ЯДРО САДОВНИКА",
            "Целостность +1. Здоровье восстановлено.", Mathf.Clamp01(showUntil - Time.time), 0.62f);
    }
}
