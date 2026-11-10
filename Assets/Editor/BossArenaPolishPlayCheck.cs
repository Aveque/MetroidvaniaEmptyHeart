#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BossArenaPolishPlayCheck
{
    private static BossArena arena;
    private static BiomechBoss boss;
    private static BossArmorVisual armor;
    private static PlayerMove player;
    private static LivingEnergyRibbon beam;
    private static Vector3 sampled;
    private static float next, started;
    private static int stage, boundedSamples;
    private static bool preview, deformed;
    private static BossHazard left, right;

    [MenuItem("Tools/Empty Heart/Проверки/Босс/Щит, ворота и луч")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) return;
        preview = false;
        try
        {
            arena = Object.FindFirstObjectByType<BossArena>(); boss = arena.Boss;
            Require(!arena.InCombat && !arena.Defeated, "Use a fresh Play session.");
            armor = boss.GetComponent<BossArmorVisual>();
            var seals = Object.FindObjectsByType<PaintedArenaSeal>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Require(seals.Length == 2 && seals.All(s => s.Ready && !s.GetComponent<SpriteRenderer>().enabled
                && s.GetComponent<BoxCollider2D>().enabled && s.gameObject.layer == 7), "Painted solid seals missing.");
            player = Object.FindFirstObjectByType<PlayerMove>(); player.UnlockDash();
            player.GetComponent<PlayerHealth>().GrantContactGrace(40);
            player.transform.position = new Vector3(70, -2);
            player.GetComponent<Rigidbody2D>().position = player.transform.position;
            Physics2D.SyncTransforms(); Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();
            started = Time.time; boundedSamples = stage = 0; deformed = false; beam = null;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
        }
        catch (Exception e) { Finish(e); }
    }
    public static void Preview() { Run(); preview = true; }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
        try
        {
            Require(Time.time - started < 25, "Polish check timed out.");
            Bounds room = arena.CombatBounds;
            foreach (var hazard in Object.FindObjectsByType<BossHazard>(FindObjectsSortMode.None))
            {
                if (!hazard.CoreCircuitOwner) continue;
                float half = hazard.HitSize.x / 2;
                Require(hazard.transform.position.x - half >= room.min.x - .001f
                    && hazard.transform.position.x + half <= room.max.x + .001f, "Beam crossed an arena wall.");
                boundedSamples++;
            }
            if (stage == 0 && boss.CurrentState == BiomechBoss.State.Pulse && boss.StateAge > .12f)
            {
                Require(armor && armor.Strength > .99f, "Armour circuit not visibly active.");
                beam = Object.FindObjectsByType<LivingEnergyRibbon>(FindObjectsSortMode.None).FirstOrDefault(r => !r.Curved);
                Require(beam && beam.AnimatedMesh.vertexCount == 98, "Beam has no deforming painted surface.");
                sampled = beam.AnimatedMesh.vertices[30]; next = Time.time + .13f; stage = 1;
            }
            if (stage == 1 && Time.time >= next)
            {
                deformed = (beam.AnimatedMesh.vertices[30] - sampled).sqrMagnitude > .000001f;
                Require(deformed, "Plasma is translating as a rigid PNG.");
                stage = 2;
                if (preview) { preview = false; EditorApplication.isPaused = true; }
            }
            if (stage == 2 && boss.CurrentState == BiomechBoss.State.Stunned)
            {
                Require(boundedSamples > 20, "Insufficient real beam samples.");
                left = boss.SpawnHazard(new Vector2(room.min.x + .6f, 2), new Vector2(.55f, 10),
                    Vector2.left * 8.5f, 5, true, true, Color.white);
                right = boss.SpawnHazard(new Vector2(room.max.x - .6f, 2), new Vector2(.55f, 10),
                    Vector2.right * 8.5f, 5, true, true, Color.white);
                boss.BreakCoreCircuit(); next = Time.time + .35f; stage = 3;
            }
            if (stage == 3 && Time.time >= next)
            {
                Require(!left && !right, "A beam survives after reaching a side wall.");
                Require(boss.CanTakeDamage && armor.Strength < .01f, "Shield did not visibly break.");
                next = Time.time + 5.1f; stage = 4;
            }
            if (stage == 4 && Time.time >= next)
            {
                Require(!boss.CanTakeDamage && armor.Strength > .99f, "Shield did not visibly reform.");
                Finish(null);
            }
        }
        catch (Exception e) { Finish(e); }
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Finish(Exception error)
    {
        EditorApplication.update -= Tick;
        string result = error == null
            ? "BOSS ARENA POLISH PASSED: two painted articulated root seals with original solid collision; live 98-vertex plasma deformation; "
                + boundedSamples + " bounded beam samples; both wall exits destroy beams; visible shield break and timed reformation."
            : "BOSS ARENA POLISH FAILED stage " + stage + ": " + error;
        File.WriteAllText("BossArenaPolishCheck.txt", result);
        if (error == null) Debug.Log(result); else Debug.LogError(result);
    }
}
#endif
