using UnityEngine;

public class BossAccessGate : MonoBehaviour
{
    private void Awake()
    {
        // Compatibility for older saved scenes: access is now unrestricted.
        Collider2D seal = GetComponent<Collider2D>();
        if (seal != null) seal.enabled = false;
        SpriteRenderer visual = GetComponent<SpriteRenderer>();
        if (visual != null) visual.enabled = false;
    }
}
