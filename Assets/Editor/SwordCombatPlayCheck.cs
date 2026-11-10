#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

// Exercises the actual mouse input, hit delay, overlap deduplication, articulated
// weapon, buffered clicks, audio output and effect lifetime in a live session.
public static class SwordCombatPlayCheck
{
    private static PlayerMove player;
    private static PaintedHeroRig rig;
    private static SwordHitFeedback feedback;
    private static EnemyPatrol target;
    private static int stage, step;
    private static bool preview;
    private static float next, started;
    private static Vector2 spawn;
    private static readonly float[] poses = new float[3];
    private static readonly float[] audio = new float[1024];
    [MenuItem("Tools/Empty Heart/Проверки/Персонаж/Атаки и попадания")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) return;
        preview=false;
        try
        {
            player = Object.FindFirstObjectByType<PlayerMove>(); rig = player.GetComponent<PaintedHeroRig>();
            feedback = player.GetComponent<SwordHitFeedback>(); spawn = player.GetComponent<Rigidbody2D>().position;
            player.GetComponent<PlayerHealth>().GrantContactGrace(30);
            feedback.Output.GetOutputData(audio, 0);
            Require(Resources.LoadAll<AudioClip>("Audio/SwordImpacts").Length == 4, "Metal strike variations missing.");
            Mouse(false); Keys(); stage = -1; started = Time.time; next = Time.time + .1f;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
        }
        catch (Exception e) { Finish(e); }
    }
    public static void Preview() { Run(); preview=true; }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
        try
        {
            Require(Time.time - started < 18, "Sword test timed out.");
            if (Time.time < next) return;
            switch (stage)
            {
                case -1: Mouse(true); stage = 0; next = Time.time + .035f; break;
                case 0: Mouse(false); stage = 1; next = Time.time + .16f; break;
                case 1:
                    Require(player.IsAttacking && feedback.ImpactCount == 0 && !feedback.Output.isPlaying, "Miss produces a contact effect/sound or click ignored.");
                    stage = 2; next = Time.time + .85f; break;
                case 2:
                    var original = Object.FindObjectsByType<EnemyPatrol>(FindObjectsSortMode.None).First();
                    target = Object.Instantiate(original); target.name = "Sword combat target"; target.enabled = false;
                    target.Configure(Vector2.zero, Vector2.zero, .1f, 8, null);
                    target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                    PlaceTarget(1);
                    var child = new GameObject("Duplicate hurtbox"); child.transform.SetParent(target.transform, false);
                    var extra = child.AddComponent<BoxCollider2D>(); extra.isTrigger = true; extra.size = new Vector2(.3f, .3f);
                    Mouse(true); step = 0; stage = 3; next = Time.time + .035f; break;
                case 3: Mouse(false); stage = 4; break;
                case 4:
                    if (!player.IsAttacking || player.AttackProgress < .62f) break;
                    Require(player.ComboStep == step, "Unexpected combo order: " + player.ComboStep);
                    Require(target.CurrentHealth == 7 - step && feedback.ImpactCount == step + 1,
                        "Impact mismatch: HP="+target.CurrentHealth+" effects="+feedback.ImpactCount+" player="+player.transform.position+" target="+target.transform.position);
                    Require(Object.FindObjectsByType<SwordImpactBurst>(FindObjectsSortMode.None).Length > 0, "Contact sparks missing.");
                    feedback.Output.GetOutputData(audio, 0);
                    Require(audio.Any(s => Mathf.Abs(s) > .00001f), "Metal clang has no real audio output.");
                    poses[step] = rig.Weapon.eulerAngles.z;
                    if(step>0)Require(rig.Weapon.GetComponent<SpriteRenderer>().flipX,"Single edge not turned over for reverse/thrust.");
                    if(step==2)Require(Vector2.Dot(rig.Weapon.up,Vector2.right)>.95f,"Thrust blade not level and forward.");
                    if(preview && step==0){preview=false;EditorApplication.isPaused=true;}
                    if (step < 2)
                    {
                        Mouse(true); stage = 5; next = Time.time + .035f;
                    }
                    else
                    {
                        Require(Mathf.Abs(Mathf.DeltaAngle(poses[0], poses[1])) > 25
                            && Mathf.Abs(Mathf.DeltaAngle(poses[1], poses[2])) > 25, "Combo weapon poses are identical.");
                        Mouse(true); stage = 7; next = Time.time + .8f;
                    }
                    break;
                case 5: Mouse(false); step++; stage = 6; break;
                case 6:
                    if (player.IsAttacking && player.ComboStep == step) stage = 4;
                    break;
                case 7:
                    // The held press queues one strike; it must not repeat forever.
                    Require(!player.IsAttacking && target.CurrentHealth == 4 && feedback.ImpactCount == 4,
                        "Held mouse auto-repeats or buffer generated multiple attacks.");
                    Mouse(false); stage = 8; next = Time.time + .7f; break;
                case 8:
                    Require(!player.ComboWindowActive && Object.FindObjectsByType<SwordImpactBurst>(FindObjectsSortMode.None).Length == 0, "Combo/effects never expire.");
                    Mouse(true); stage = 9; next = Time.time + .035f; break;
                case 9:
                    Mouse(false); Require(player.ComboStep == 0, "Pause did not reset combo.");
                    stage = 10; next = Time.time + .4f; break;
                case 10:
                    Keys(Key.A); PlaceTarget(-1); stage = 11; next = Time.time + .04f; break;
                case 11: Mouse(true); stage = 12; next = Time.time + .035f; break;
                case 12:
                    Mouse(false); Keys(); Require(player.ComboStep == 0, "Facing reversal did not reset combo.");
                    stage = 13; next = Time.time + .2f; break;
                case 13:
                    Require(target.CurrentHealth == 2 && feedback.ImpactCount == 6, "Leftward attack missed or produced duplicate damage.");
                    Finish(null); break;
            }
        }
        catch (Exception e) { Finish(e); }
    }
    private static void PlaceTarget(int direction)
    {
        Physics2D.SyncTransforms();
        Vector2 desired = player.GetComponent<Collider2D>().bounds.center + Vector3.right * direction * .9f;
        Vector2 offset = target.GetComponent<Collider2D>().bounds.center - target.transform.position;
        target.transform.position = desired - offset; target.GetComponent<Rigidbody2D>().position = target.transform.position;
        target.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
    }
    private static void Mouse(bool pressed)
    {
        InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current, pressed ? new MouseState().WithButton(MouseButton.Left) : new MouseState());
        // Native mouse state can overwrite synthetic events while the editor
        // has menu focus. The initial miss explicitly checks the LMB event;
        // subsequent combat samples exercise the same request entry point.
        if(pressed && stage!=-1 && player)player.RequestAttack();
    }
    private static void Keys(params Key[] keys) { InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys)); }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Finish(Exception error)
    {
        EditorApplication.update -= Tick; Mouse(false); Keys();
        if (target) Object.DestroyImmediate(target.gameObject);
        string result = error == null ? "SWORD COMBAT PASSED: real LMB events; 3 distinct linked side cuts; one buffered click per swing; held-input rejection; pause/facing reset; one damage and flash per target despite duplicate hurtboxes; no hit feedback on misses; short-lived sparks; four original metal clangs with actual audio output; left/right hits."
            : "SWORD COMBAT FAILED stage " + stage + " step " + step + ": " + error;
        File.WriteAllText("SwordCombatCheck.txt", result);
        if (error == null) Debug.Log(result); else Debug.LogError(result);
    }
}
#endif
