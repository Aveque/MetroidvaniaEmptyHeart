#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Runs against real physics callbacks in Play mode. Copies each authored gate
// into empty space so terrain/enemies cannot interfere with boundary samples.
public static class TransitionGatePlayCheck
{
    private static readonly float[] Samples = { -2f, -.2f, -2f, -.2f, .2f, -.2f, .2f, 2f, .2f, 2f, .2f, -.2f, -2f, -.2f, -20f };
    private static CameraTransitionGate[] gates;
    private static GameObject probe;
    private static PlayerMove player;
    private static Rigidbody2D body;
    private static CameraFollow camera;
    private static CameraRoom negative, positive;
    private static Vector2 axis, origin;
    private static Vector2 savedPosition;
    private static RigidbodyType2D savedType;
    private static float savedGravity;
    private static bool savedEnabled;
    private static int index, sample, checks;
    private static float next;

    [MenuItem("Tools/Empty Heart/Проверки/Мир/Переходы камеры")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) return;
        try
        {
            gates = Object.FindObjectsByType<CameraTransitionGate>(FindObjectsSortMode.None);
            Require(gates.Length > 0, "No authored gates.");
            player = Object.FindFirstObjectByType<PlayerMove>();
            body = player.GetComponent<Rigidbody2D>();
            camera = Camera.main.GetComponent<CameraFollow>();
            savedPosition = body.position; savedType = body.bodyType;
            savedGravity = body.gravityScale; savedEnabled = player.enabled;
            player.enabled = false; body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
            index = checks = 0;
            BeginGate();
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
        }
        catch (Exception e) { Finish(e); }
    }

    private static void BeginGate()
    {
        var data = new SerializedObject(gates[index]);
        axis = data.FindProperty("travelAxis").enumValueIndex == 0 ? Vector2.right : Vector2.up;
        negative = (CameraRoom)data.FindProperty("negativeSideRoom").objectReferenceValue;
        positive = (CameraRoom)data.FindProperty("positiveSideRoom").objectReferenceValue;
        Require(negative && positive, "Missing room on " + gates[index].name);
        origin = new Vector2(0, 200 + index * 30);
        probe = new GameObject("Transition boundary physics check"); probe.transform.position = origin;
        probe.AddComponent<CameraTransitionGate>().Configure(
            axis == Vector2.right ? CameraTransitionGate.TravelAxis.Horizontal : CameraTransitionGate.TravelAxis.Vertical,
            negative, positive, gates[index].GetComponent<BoxCollider2D>().size);
        camera.SetRoom(negative, true); sample = 0; Place();
    }

    private static void Place()
    {
        Vector2 point = origin + axis * Samples[sample];
        body.position = point; player.transform.position = point;
        // Deliberately point velocity at the opposite room while only touching
        // the near half. Position, not intended travel, must determine the room.
        body.linearVelocity = axis * (Samples[sample] < 0 ? .1f : -.1f);
        Physics2D.SyncTransforms();
        if (sample == Samples.Length - 1) camera.SetRoom(positive, true);
        next = Time.time + .065f;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
        if (Time.time < next) return;
        try
        {
            CameraRoom expected = sample == Samples.Length - 1 ? positive : Samples[sample] < 0 ? negative : positive;
            Require(camera.ActiveRoom == expected, gates[index].name + " sample " + sample + ": camera chose wrong side.");
            checks++;
            if (++sample < Samples.Length) Place();
            else
            {
                Object.DestroyImmediate(probe); probe = null;
                if (++index < gates.Length) BeginGate(); else Finish(null);
            }
        }
        catch (Exception e) { Finish(e); }
    }

    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Finish(Exception error)
    {
        EditorApplication.update -= Tick;
        if (probe) Object.DestroyImmediate(probe);
        if (body)
        {
            body.position = savedPosition; player.transform.position = savedPosition;
            body.linearVelocity = Vector2.zero; body.bodyType = savedType; body.gravityScale = savedGravity;
            player.enabled = savedEnabled; Physics2D.SyncTransforms(); camera.SnapToPlayerRoom();
        }
        string result = error == null
            ? "TRANSITION GATES PASSED: " + gates.Length + " authored gates, " + checks + " physics checks; touch and retreat from both sides; cross and immediately reverse inside trigger; full crossing both ways; velocity-independent room selection; teleport exit does not override respawn camera."
            : "TRANSITION GATES FAILED: " + error;
        File.WriteAllText("TransitionGatesCheck.txt", result);
        if (error == null) Debug.Log(result); else Debug.LogError(result);
    }
}
#endif
