using UnityEngine;

// Three irregular painted arcs make the active circuit readable around the
// carapace. A broken circuit visibly separates and dissipates in a quarter sec.
[DefaultExecutionOrder(700)]
public sealed class BossArmorVisual : MonoBehaviour
{
    private BiomechBoss boss;
    private readonly LivingEnergyRibbon[] arcs = new LivingEnergyRibbon[3];
    private float strength;
    public float Strength => strength;
    private void Start()
    {
        boss = GetComponent<BiomechBoss>();
        if (boss.AttackEffects == null || boss.AttackEffects.Length != 4) return;
        Transform body = GetComponent<PaintedBossRig>()?.Body;
        if (!body) body = transform;
        var root = new GameObject("Living armour circuit").transform; root.SetParent(body, false);
        root.localPosition = new Vector3(.05f, .36f);
        for (int i = 0; i < arcs.Length; i++)
        {
            var part = new GameObject("Painted shield arc " + i); part.transform.SetParent(root, false);
            arcs[i] = part.AddComponent<LivingEnergyRibbon>();
            arcs[i].Configure(boss.AttackEffects[0], GetComponent<SpriteRenderer>().sharedMaterial,
                new Vector2(3.3f, 2.8f), 9, true, i * 120 + 10, i * 120 + 108);
            arcs[i].Opacity = 0;
        }
    }
    private void LateUpdate()
    {
        if (!boss) return;
        float target = boss.ActiveBody && !boss.CanTakeDamage ? 1 : 0;
        strength = Mathf.MoveTowards(strength, target, Time.deltaTime * 5);
        foreach (var arc in arcs)
        {
            if (!arc) continue;
            arc.Opacity = strength * (boss.ArmorHitFlash ? 1 : .9f);
            arc.Spread = 1 + (1 - strength) * .28f;
            arc.Tint = boss.ArmorHitFlash ? new Color(1, .7f, .3f) : Color.white;
        }
    }
}
