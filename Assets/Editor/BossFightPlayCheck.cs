#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BossFightPlayCheck
{
    private static BossArena arena;
    private static BiomechBoss boss;
    private static PlayerMove player;
    private static PlayerHealth health;
    private static Rigidbody2D body;
    private static int stage, hp, slams;
    private static BiomechBoss.State previousState;
    private static double next;
    private static readonly HashSet<BiomechBoss.State> seen = new HashSet<BiomechBoss.State>();
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    [MenuItem("Tools/Empty Heart/Проверки/Босс/Полный бой (новый запуск игры)")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) { Debug.LogWarning("Start Play first."); return; }
        try
        {
            arena = Object.FindFirstObjectByType<BossArena>(); boss = arena.Boss;
            player = Object.FindFirstObjectByType<PlayerMove>(); health = player.GetComponent<PlayerHealth>();
            body = player.GetComponent<Rigidbody2D>();
            Require(!arena.InCombat && !arena.Defeated && !player.DashUnlocked, "Start a fresh Play session.");
            Require(EmptyHeartUI.FontsLoaded && EmptyHeartUI.HeadingFont.HasCharacter('Я') && EmptyHeartUI.BodyFont.HasCharacter('ё'),
                "Bundled Cyrillic UI fonts missing.");
            seen.Clear(); slams = 0; previousState = BiomechBoss.State.Dormant;
            // Unity creates the output-data tap on its first read. Let the audio thread
            // fill it before asserting that the actual stream contains nonzero samples.
            AtmospherePlayCheck.HasSignal(StationMusic.Instance.ExplorationSource);
            Wait(-1, .3f); EditorApplication.update -= Tick; EditorApplication.update += Tick;
        }
        catch (Exception e) { Fail(e); }
    }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
        seen.Add(boss.CurrentState);
        if (boss.CurrentState == BiomechBoss.State.Slam && previousState != boss.CurrentState) slams++;
        previousState = boss.CurrentState;
        if (Time.timeAsDouble < next) return;
        try
        {
            switch (stage)
            {
                case -1:
                    AtmospherePlayCheck.Validate();
                    Place(new Vector2(59, -2)); player.enabled = false; body.linearVelocity = Vector2.right * 12;
                    Wait(0, .3f); break;
                case 0:
                    Require(body.position.x > 62f, "Old access seal still blocks a player without Dash.");
                    player.enabled = true; Place(new Vector2(66, -2)); Wait(1, 0.15f); break;
                case 1:
                    Require(arena.InCombat && arena.DoorsLocked && !player.DashUnlocked, "No-Dash entry did not begin and seal the fight.");
                    Require(boss.CurrentState == BiomechBoss.State.Intro && Camera.main.orthographicSize >= 6.5f, "Intro or camera failed.");
                    Require(!player.TryDash(), "Dash usable before acquisition.");
                    AtmospherePlayCheck.HasSignal(StationMusic.Instance.BossSource);
                    Wait(2, 2f); break;
                case 2:
                    float shortest=100,longest=0;
                    foreach(float sampleX in new[]{65f,69f,74f,79f,83f})
                    {
                        var sample=boss.SpawnHazard(new Vector2(sampleX,2),new Vector2(.55f,10),Vector2.right,.01f,false,true,Color.cyan);
                        shortest=Mathf.Min(shortest,sample.HitSize.y);longest=Mathf.Max(longest,sample.HitSize.y);
                        Require(Mathf.Abs(sample.GetComponent<BossAttackIndicator>().VisualExtent.y-sample.HitSize.y)<.01f,"Beam visual and damage contours disagree.");
                        Object.Destroy(sample.gameObject);
                    }
                    Require(longest-shortest>2,"Beam does not follow the vaulted roof.");
                    Require(StationMusic.Instance.InBossMode && StationMusic.Instance.Blend > .99f &&
                        AtmospherePlayCheck.HasSignal(StationMusic.Instance.BossSource), "Boss music crossfade or audio output failed.");
                    Require(boss.ActiveBody && !boss.CanTakeDamage, "Boss bioarmor is not active.");
                    Place((Vector2)boss.transform.position + new Vector2(-2, 0.85f));
                    Sword(0); boss.TakeDamage(boss.MaxHealth);
                    Require(boss.CurrentHealth == boss.MaxHealth, "Boss can be killed by trading damage without Dash.");
                    Place(new Vector2(70, -2)); hp = health.CurrentHealth;
                    boss.SpawnHazard(new Vector2(70, 2), new Vector2(0.55f, 10), new Vector2(-0.1f, 0), 0.15f, true, true, Color.cyan);
                    Wait(3, 0.12f); break;
                case 3:
                    Require(health.CurrentHealth == hp - 1 && !boss.CanTakeDamage, "Non-Dash beam contact failed or opened the core.");
                    Wait(16, 1.1f); break;
                case 16:
                    health.TakeDamage(health.MaxHealth); Wait(4, 1.7f); break;
                case 4:
                    Require(!StationMusic.Instance.InBossMode && StationMusic.Instance.Blend < .15f, "No-Dash death did not restore exploration music.");
                    Require(!arena.InCombat && !arena.DoorsLocked && boss.CurrentHealth == boss.MaxHealth && !player.DashUnlocked,
                        "No-Dash death did not reset encounter.");
                    player.UnlockDash(); Place(new Vector2(66, -2)); Wait(5, 2.2f); break;
                case 5:
                    Require(arena.InCombat && !boss.CanTakeDamage, "Retry failed or armor opened without a circuit break.");
                    Place(new Vector2(65, -2)); Set(player, "facingDirection", -1f); Set(player, "moveInput", 0f);
                    Require(player.TryDash(), "Door test Dash rejected."); Wait(6, 0.3f); break;
                case 6:
                    Require(body.position.x > 64f && arena.DoorsLocked, "Dash escaped through the locked entrance.");
                    health.GrantContactGrace(25f); Wait(7, 0.3f); break;
                case 7:
                    Place(new Vector2(70, -2)); Set(player, "facingDirection", 1f); Set(player, "moveInput", 0f);
                    hp = health.CurrentHealth;
                    boss.SpawnHazard(new Vector2(72, 2), new Vector2(0.55f, 10), new Vector2(-0.1f, 0), 0.3f, true, true, Color.cyan);
                    Require(player.TryDash(), "Circuit test Dash rejected.");
                    StationMusic.Instance.BossSource.time = StationMusic.Instance.BossTheme.length - .12f;
                    Wait(8, 0.35f); break;
                case 8:
                    Require(StationMusic.Instance.BossSource.isPlaying && StationMusic.Instance.BossSource.time < 1f,
                        "Boss music did not wrap at the loop boundary.");
                    Require(body.position.x > 73.5f && health.CurrentHealth == hp && boss.CanTakeDamage,
                        "Dash did not safely break the real energy-wall circuit.");
                    Place((Vector2)boss.transform.position + new Vector2(-2, 0.85f)); Sword(0);
                    Require(boss.CurrentHealth == boss.MaxHealth - 1, "Sword did not damage exposed core.");
                    Place((Vector2)boss.transform.position + new Vector2(0, 3.35f)); Set(player, "isGrounded", false); Sword(2);
                    Require(boss.CurrentHealth == boss.MaxHealth - 2 && Mathf.Abs(body.linearVelocity.y - 5f) < 0.05f, "Exposed-core pogo failed.");
                    boss.TakeDamage(14); Place(new Vector2(70, -2)); Wait(9, 17f); break;
                case 9:
                    Require(boss.Enraged && slams >= 2 && seen.Contains(BiomechBoss.State.Pulse) && seen.Contains(BiomechBoss.State.Charge),
                        "Second phase or attack routines failed.");
                    Require(!boss.CanTakeDamage, "Core exposure did not expire.");
                    int before = boss.CurrentHealth; boss.TakeDamage(100); Require(boss.CurrentHealth == before, "Expired armor no longer blocks hits.");
                    Set(health, "contactGraceUntil", 0f); health.TakeDamage(health.MaxHealth); Wait(10, 1.7f); break;
                case 10:
                    Require(!StationMusic.Instance.InBossMode && StationMusic.Instance.Blend < .15f, "Death did not restore exploration music.");
                    Require(!arena.InCombat && !arena.DoorsLocked && boss.CurrentHealth == boss.MaxHealth && player.DashUnlocked, "Death reset failed.");
                    Require(Camera.main.transform.position.x < 10 && Mathf.Abs(Camera.main.orthographicSize - 5.6f) < 0.01f, "Respawn camera failed.");
                    Require(Object.FindObjectsByType<BossHazard>(FindObjectsSortMode.None).Length == 0, "Hazards survived death.");
                    Place(new Vector2(66, -2)); Wait(11, 2.2f); break;
                case 11:
                    Place(new Vector2(70, -2)); Set(player, "facingDirection", 1f); Set(player, "moveInput", 0f);
                    boss.SpawnHazard(new Vector2(72, 2), new Vector2(0.55f, 10), new Vector2(-0.1f, 0), 0.3f, true, true, Color.cyan);
                    Require(player.TryDash(), "Victory circuit test Dash rejected."); Wait(12, 0.35f); break;
                case 12:
                    Require(boss.CanTakeDamage, "Core did not open on repeat attempt.");
                    boss.TakeDamage(boss.MaxHealth);
                    Require(boss.CurrentState == BiomechBoss.State.Dying && arena.DoorsLocked, "Death animation skipped.");
                    Wait(13, 1.9f); break;
                case 13:
                    Require(arena.Defeated && !arena.InCombat && !arena.DoorsLocked && boss.CurrentState == BiomechBoss.State.Dead, "Victory exits failed.");
                    Require(!GameObject.Find("Arena Exit Seal"), "Exit remains sealed.");
                    hp = health.MaxHealth; Place(Object.FindFirstObjectByType<BossCoreReward>().transform.position); Wait(14, 0.3f); break;
                case 14:
                    Require(health.MaxHealth == hp + 1 && health.CurrentHealth == health.MaxHealth, "Core reward failed.");
                    Place(new Vector2(70, -2)); Wait(15, 2f); break;
                case 15:
                    Require(!arena.InCombat && arena.Defeated, "Boss restarted after victory.");
                    Require(!StationMusic.Instance.InBossMode && StationMusic.Instance.Blend < .001f &&
                        !StationMusic.Instance.BossSource.isPlaying && AtmospherePlayCheck.HasSignal(StationMusic.Instance.ExplorationSource),
                        "Victory music did not return to the exploration cue.");
                    StationMusic.Instance.ExplorationSource.time = StationMusic.Instance.ExplorationTheme.length - .12f;
                    Wait(17, .4f); break;
                case 17:
                    Require(StationMusic.Instance.ExplorationSource.isPlaying && StationMusic.Instance.ExplorationSource.time < 1f,
                        "Exploration music did not wrap continuously at the loop boundary.");
                    string result = "BOSS FIGHT CHECK PASSED: free entry without Dash; fight seals; bioarmor rejects no-Dash hits; beam damage; no-Dash death/reset; safe Dash crossing breaks circuit; sword/pogo damage exposed core; timed armor restoration; second phase; locked-door Dash; death camera; victory and health reward; bundled Cyrillic fonts; three parallax depths and camera-snap coverage; real exploration/boss audio output; combat/death/victory music crossfades; streaming loop wrap.";
                    File.WriteAllText("BossFightCheck.txt", result); Debug.Log(result); EditorApplication.update -= Tick; break;
            }
        }
        catch (Exception e) { Fail(e); }
    }
    [MenuItem("Tools/Empty Heart/Просмотр/Арена босса")]
    public static void Preview()
    {
        if (!EditorApplication.isPlaying) return;
        PlayerMove p = Object.FindFirstObjectByType<PlayerMove>(); p.UnlockDash();
        p.transform.position = new Vector3(66, -2); p.GetComponent<Rigidbody2D>().position = p.transform.position;
        Physics2D.SyncTransforms(); Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();
    }
    private static void Sword(int direction)
    {
        Set(player, "attackDirection", direction); Set(player, "attackFacingDirection", 1f); Invoke(player, "PerformAttack");
    }
    private static void Place(Vector2 position)
    {
        player.transform.position = position; body.position = position; body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms(); Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();
    }
    private static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Private).SetValue(obj, value);
    private static object Invoke(object obj, string method) => obj.GetType().GetMethod(method, Private).Invoke(obj, null);
    private static void Wait(int nextStage, float seconds) { stage = nextStage; next = Time.timeAsDouble + seconds; }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception("BOSS FIGHT CHECK FAILED: " + message); }
    private static void Fail(Exception e)
    {
        EditorApplication.update -= Tick; if (player != null) player.enabled = true;
        File.WriteAllText("BossFightCheck.txt", e.ToString()); Debug.LogException(e);
    }
}
#endif
