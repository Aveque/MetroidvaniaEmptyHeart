#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BossCombatVisualsPlayCheck
{
    private static BiomechBoss boss;
    private static PaintedBossRig rig;
    private static float started;
    private static bool pauseOnRoot;
    private static readonly Queue<BiomechBoss.State> previewStages=new Queue<BiomechBoss.State>();
    private static readonly Dictionary<BiomechBoss.Attack, HashSet<Vector3Int>> poses = new Dictionary<BiomechBoss.Attack, HashSet<Vector3Int>>();
    private static float previousAngle, previousSample, maximumStep;
    private static readonly HashSet<BiomechBoss.Attack> warnings = new HashSet<BiomechBoss.Attack>();
    [MenuItem("Tools/Empty Heart/Проверки/Босс/Анимации и эффекты")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) return;
        pauseOnRoot = false;
        previewStages.Clear();
        try
        {
            boss = Object.FindFirstObjectByType<BiomechBoss>();
            Require(!Object.FindFirstObjectByType<BossArena>().InCombat, "Use a fresh Play session.");
            Require(boss.HasDistinctAttacks, "Missing distinct attack sets.");
            rig = boss.GetComponent<PaintedBossRig>();
            Require(rig && rig.Ready && !boss.GetComponent<SpriteRenderer>().enabled, "Continuous puppet is not visible.");
            Require(rig.parts.All(s => s && AssetDatabase.GetAssetPath(s).StartsWith("Assets/Art/Painted/GardenerRigParts/")), "Missing isolated puppet parts.");
            string[] paths = Directory.GetFiles("Assets/Art/Painted/GardenerFrames/", "*.png");
            Require(paths.Length == 28, "Expected sixteen base poses and twelve combat poses.");
            foreach (string path in paths)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Require(sprite != null && !sprite.packed && importer.spriteImportMode == SpriteImportMode.Single
                    && AssetDatabase.GetAssetPath(sprite.texture) == path, "Sprite still shares an atlas.");
            }
            poses.Clear(); warnings.Clear();
            previousSample = -1; maximumStep = 0;
            PlayerMove player = Object.FindFirstObjectByType<PlayerMove>(); player.UnlockDash();
            player.GetComponent<PlayerHealth>().GrantContactGrace(40f);
            player.transform.position = new Vector3(70, -2);
            player.GetComponent<Rigidbody2D>().position = player.transform.position;
            Physics2D.SyncTransforms(); Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();
            started = Time.time; EditorApplication.update -= Tick; EditorApplication.update += Tick;
        }
        catch (Exception e) { Finish(e); }
    }
    public static void Preview() { Run(); pauseOnRoot = true; }
    public static void PreviewEffects()
    {
        Run();previewStages.Enqueue(BiomechBoss.State.Pulse);previewStages.Enqueue(BiomechBoss.State.Charge);previewStages.Enqueue(BiomechBoss.State.Slam);
    }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
        try
        {
            if (Time.time - started > 25) throw new Exception("Timed out waiting for all attack animations.");
            var attack = boss.ActiveAttack;
            if (attack == BiomechBoss.Attack.None) return;
            if(previewStages.Count>0 && boss.CurrentState==previewStages.Peek() && boss.StateAge>.10f)
            {previewStages.Dequeue();EditorApplication.isPaused=true;}
            if (!poses.ContainsKey(attack)) poses[attack] = new HashSet<Vector3Int>();
            float angle = rig.FrontBlade.localEulerAngles.z;
            poses[attack].Add(new Vector3Int(Mathf.RoundToInt(angle * 4),
                Mathf.RoundToInt(rig.FrontBlade.parent.localEulerAngles.z * 4), Mathf.RoundToInt(rig.Body.localPosition.y * 200)));
            float dt = Time.time - previousSample;
            if (previousSample >= 0 && dt > .001f && dt < .04f)
            {
                float step = Mathf.Abs(Mathf.DeltaAngle(previousAngle, angle));
                maximumStep = Mathf.Max(maximumStep, step);
                Require(step < 50, "A sickle joint visibly jumps between animation poses.");
            }
            previousSample = Time.time; previousAngle = angle;
            Require(!boss.GetComponent<SpriteRenderer>().enabled, "Four-frame fallback is still drawn over the continuous puppet.");
            if (boss.CurrentState == BiomechBoss.State.Telegraph)
            {
                var indicators = Object.FindObjectsByType<BossAttackIndicator>(FindObjectsSortMode.None).Where(i => i.IsWarning).ToArray();
                foreach (var indicator in indicators)
                {
                    SpriteRenderer[] painted=indicator.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled).ToArray();
                    Require(indicator.Painted && painted.Length>0 && painted.All(r=>r.sortingOrder>=9), "Painted warning missing or hidden behind environment.");
                    Require(indicator.GetComponentsInChildren<LineRenderer>().Length==0,"Geometric markers remain.");
                    Require(painted.All(r=>AssetDatabase.GetAssetPath(r.sprite).StartsWith("Assets/Art/Painted/GardenerEffects/")),"Warning uses a placeholder sprite.");
                    if (indicator.Progress > .3f) warnings.Add(attack);
                    if (pauseOnRoot && attack == BiomechBoss.Attack.Roots && indicator.Progress > .55f)
                    { pauseOnRoot = false; EditorApplication.isPaused = true; }
                }
            }
            if (warnings.Count == 3 && poses.Count == 3 && poses.Values.All(p => p.Count >= 24)
                && boss.CurrentState == BiomechBoss.State.Stunned)
            {
                Finish(null);
            }
        }
        catch (Exception e) { Finish(e); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Finish(Exception error)
    {
        EditorApplication.update -= Tick;
        string result = error == null ? "BOSS PRESENTATION PASSED: continuous articulated painted puppet visible; isolated textures; all 3 painted animated warnings above environment; no geometric markers; distinct intermediate joint poses observed: "
            + string.Join(", ", poses.Select(p => p.Key + "=" + p.Value.Count)) + "; maximum sampled sickle step=" + maximumStep.ToString("F2")
            + " degrees at sample intervals below 40ms; original four-frame renderer hidden; full windup/strike/recovery cycle completed." : error.ToString();
        File.WriteAllText("BossCombatVisualsCheck.txt", result);
        if (error == null) Debug.Log(result); else Debug.LogException(error);
    }
}
#endif
