#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Integration check against the scene, real trigger, keyboard input and respawn.
public static class DashUpgradePlayCheck
{
    private static PlayerMove player;
    private static PlayerHealth health;
    private static Rigidbody2D body;
    private static DashAbilityPickup pickup;
    private static Keyboard keyboard;
    private static int stage;
    private static double nextStep;
    private static Vector2 initialSpawn;
    private static float dashStartX;
    private static float normalGravity;
    private static KeyboardState pendingKeyboardState;
    private static bool pendingKeyboardEvent;
    private static InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
    private static bool inputConfigured;
    private static readonly FieldInfo Dashing = typeof(PlayerMove).GetField("isDashing", BindingFlags.NonPublic | BindingFlags.Instance);

    [MenuItem("Tools/Empty Heart/Проверки/Персонаж/Получение Dash (новый запуск игры)")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) { Debug.LogWarning("Start Play mode first."); return; }
        try
        {
            player = UnityEngine.Object.FindFirstObjectByType<PlayerMove>();
            health = player.GetComponent<PlayerHealth>();
            body = player.GetComponent<Rigidbody2D>();
            pickup = UnityEngine.Object.FindFirstObjectByType<DashAbilityPickup>();
            Require(pickup != null, "Gallery pickup missing.");
            Require(!player.DashUnlocked, "Dash already unlocked in a fresh game.");
            Require(!player.TryDash(), "Dash accepted before acquiring component.");
            Require(UnityEngine.Object.FindObjectsByType<DashAbilityPickup>(FindObjectsSortMode.None).Length == 1,
                "Duplicate pickups in scene.");
            CameraRoom upper = UnityEngine.Object.FindObjectsByType<CameraRoom>(FindObjectsSortMode.None)
                .First(room => room.name == "03 Upper Complex");
            Require(upper.Contains(pickup.transform.position), "Pickup not in Control Gallery.");
            Require(Physics2D.Raycast(pickup.transform.position, Vector2.down, 2f, 1 << 7).collider != null,
                "Pickup has no reachable platform underneath.");
            initialSpawn = body.position;
            normalGravity = body.gravityScale;
            previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            inputConfigured = true;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.onBeforeUpdate += FeedKeyboard;
            Place(new Vector2(11f, -2.5f));
            dashStartX = body.position.x;
            QueueKeys(Key.LeftShift);
            Wait(0, 0.12);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }
        catch (Exception error) { Fail(error); }
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) { Cleanup(); return; }
        if (Time.timeAsDouble < nextStep) return;
        try
        {
            switch (stage)
            {
                case 0:
                    Require(!IsDashing && Mathf.Abs(body.position.x - dashStartX) < 0.2f,
                        "Shift moved the player before unlock.");
                    QueueKeys();
                    Place(pickup.transform.position);
                    Wait(1, 0.12);
                    break;
                case 1:
                    Require(player.DashUnlocked && pickup.Collected, "Real pickup trigger did not unlock Dash.");
                    Require(!pickup.GetComponent<BoxCollider2D>().enabled, "Pickup trigger remained enabled.");
                    Require(pickup.GetComponentInChildren<SpriteRenderer>() == null, "Pickup visual did not disappear.");
                    Require(!pickup.TryCollect(player), "Pickup can be collected twice.");
                    Place(new Vector2(11f, -2.5f));
                    dashStartX = body.position.x;
                    QueueKeys(Key.LeftShift);
                    Wait(2, 0.06);
                    break;
                case 2:
                    Require(IsDashing, $"Shift did not start unlocked Dash: enabled={player.enabled}, unlocked={player.DashUnlocked}, key={keyboard.leftShiftKey.isPressed}, current={Keyboard.current == keyboard}, pending={pendingKeyboardEvent}, x={body.position.x}, frameTime={Time.deltaTime}.");
                    Require(player.GetComponent<Animator>().GetBool("IsDashing"), "Dash animation not activated.");
                    Require(!player.TryDash(), "Concurrent Dash accepted.");
                    QueueKeys();
                    Wait(3, 0.18);
                    break;
                case 3:
                    Require(!IsDashing && Mathf.Abs(body.gravityScale - normalGravity) < 0.01f, "Dash failed to restore gravity.");
                    Require(body.position.x - dashStartX > 3.5f && body.position.x - dashStartX < 4.5f,
                        "Unlocked Dash travelled incorrect distance.");
                    Require(!player.TryDash(), "Cooldown ignored.");
                    Wait(4, 0.6);
                    break;
                case 4:
                    Place(new Vector2(36f, 23f));
                    Require(player.TryDash(), "Dash unavailable after cooldown.");
                    health.TakeDamage(health.MaxHealth);
                    Require(!IsDashing && Mathf.Abs(body.gravityScale - normalGravity) < 0.01f,
                        "Death during Dash leaves player dashing or weightless.");
                    Wait(5, 1.7);
                    break;
                case 5:
                    Require(player.DashUnlocked, "Death removed acquired ability.");
                    Require(health.CurrentHealth == health.MaxHealth && player.enabled, "Respawn did not complete.");
                    Require(Vector2.Distance(body.position, initialSpawn) < 1f, "Respawn location incorrect.");
                    Require(player.TryDash(), "Dash unavailable after respawn.");
                    Wait(6, 0.25);
                    break;
                case 6:
                    Require(!IsDashing && body.gravityScale == normalGravity, "Post-respawn Dash did not finish.");
                    Cleanup();
                    Debug.Log("DASH UPGRADE CHECK PASSED: fresh-game Shift blocked; real Gallery trigger unlocks once; Shift starts animated 4-unit Dash; cooldown enforced; death during Dash restores gravity; ability survives respawn.");
                    break;
            }
        }
        catch (Exception error) { Fail(error); }
    }

    private static bool IsDashing => (bool)Dashing.GetValue(player);
    private static void Place(Vector2 position)
    {
        player.transform.position = position;
        body.position = position;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();
    }
    private static void Wait(int next, double seconds) { stage = next; nextStep = Time.timeAsDouble + seconds; }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("DASH UPGRADE CHECK FAILED: " + message);
    }
    private static void Fail(Exception error) { Cleanup(); Debug.LogException(error); }
    private static void QueueKeys(params Key[] keys)
    {
        pendingKeyboardState = new KeyboardState(keys);
        pendingKeyboardEvent = true;
    }
    private static void FeedKeyboard()
    {
        // Editor input updates must not consume game-key presses before Update.
        if (!pendingKeyboardEvent || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        InputSystem.QueueStateEvent(keyboard, pendingKeyboardState);
        pendingKeyboardEvent = false;
    }
    private static void Cleanup()
    {
        EditorApplication.update -= Tick;
        InputSystem.onBeforeUpdate -= FeedKeyboard;
        pendingKeyboardEvent = false;
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        keyboard = null;
        if (inputConfigured) InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
        inputConfigured = false;
    }
}
#endif
