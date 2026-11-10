using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public sealed class SwordHitFeedback : MonoBehaviour
{
    private AudioSource audioSource;
    private AudioClip[] clangs;
    private float lastSound = -999;
    private int variation;
    public int ImpactCount { get; private set; }
    public AudioSource Output => audioSource;
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false; audioSource.spatialBlend = 0; audioSource.volume = .52f;
        clangs = Resources.LoadAll<AudioClip>("Audio/SwordImpacts");
    }
    public void Emit(Vector2 contact, Vector2 direction, bool armored)
    {
        ImpactCount++;
        var burst = new GameObject(armored ? "Sword clang — armour sparks" : "Sword hit — metal sparks");
        burst.transform.position = contact;
        burst.AddComponent<SwordImpactBurst>().Configure(direction, armored);
        // Multiple hurtboxes/targets still get their own contact flashes, but
        // share one sound per swing rather than stacking deafening duplicates.
        if (Time.time - lastSound < .04f || clangs.Length == 0) return;
        lastSound = Time.time;
        audioSource.pitch = new[] { .98f, 1.035f, .955f, 1.015f }[variation % 4];
        audioSource.PlayOneShot(clangs[variation++ % clangs.Length], armored ? 1 : .85f);
    }
}
