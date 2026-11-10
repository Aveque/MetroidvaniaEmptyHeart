#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Integration check: real terrain, physics, attack overlap, enemy damage and Animator.
public static class PlayerTraversalPlayCheck
{
    private static PlayerMove player;
    private static Rigidbody2D body;
    private static EnemyPatrol enemy;
    private static Vector2 start;
    private static int stage;
    private static double nextStep;
    private static float launchX;
    private static float bounceBaseY;
    private static float bouncePeakY;
    private static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static void PreviewWallPose()
    {
        if (!EditorApplication.isPlaying) { Debug.LogWarning("Start Play mode first."); return; }
        player = UnityEngine.Object.FindFirstObjectByType<PlayerMove>();
        body = player.GetComponent<Rigidbody2D>();
        player.transform.position = new Vector3(7.70f, 1.4f, 0f);
        body.position = player.transform.position;
        body.linearVelocity = Vector2.zero;
        body.gravityScale = 0f;
        Physics2D.SyncTransforms();
        Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();
        Debug.Log("Wall pose preview: Play-mode-only zero gravity for visual inspection; stop Play to restore normal physics.");
    }

    [MenuItem("Tools/Empty Heart/Проверки/Персонаж/Прыжки от стен и отскок")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) { Debug.LogWarning("Start Play mode first."); return; }
        player = UnityEngine.Object.FindFirstObjectByType<PlayerMove>();
        body = player.GetComponent<Rigidbody2D>();
        enemy = UnityEngine.Object.FindObjectsByType<EnemyPatrol>(FindObjectsSortMode.None)
            .First(e => e.name == "Sentinel - Central Sector");
        start = body.position;
        stage = 0;
        PlaceAtWall(false, 0.5f);
        nextStep = Time.timeAsDouble + 0.8;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
        if (stage == 6) bouncePeakY = Mathf.Max(bouncePeakY, body.position.y);
        if (Time.timeAsDouble < nextStep) return;
        try
        {
            switch (stage)
            {
                case 0:
                    CheckWallJump(false);
                    launchX = body.position.x;
                    Wait(1, 0.12); break;
                case 1:
                    Require(body.position.x > launchX + 0.5f, "Left-wall impulse was cancelled.");
                    PlaceAtWall(false, 1.3f);
                    Wait(2, 0.25); break;
                case 2:
                    CheckWallJump(false);
                    PlaceAtWall(true, 1.4f);
                    Wait(3, 0.25); break;
                case 3:
                    CheckWallJump(true);
                    launchX = body.position.x;
                    Wait(4, 0.12); break;
                case 4:
                    Require(body.position.x < launchX - 0.5f, "Right-wall impulse was cancelled.");
                    PlaceAtWall(true, 2.1f);
                    Wait(5, 0.25); break;
                case 5:
                    CheckWallJump(true);
                    Debug.Log("TRAVERSAL CHECK: both walls slide without sticking, correct facing and custom WallSlide animation; repeated jumps work above original takeoff height; impulse preserved.");
                    body.position = new Vector2(-6, 0);
                    body.linearVelocity = Vector2.down;
                    Physics2D.SyncTransforms();
                    Set("isGrounded", false); Set("attackDirection", 2);
                    Invoke("PerformAttack");
                    Require(body.linearVelocity.y < 0, "A missed downward attack produced a bounce.");
                    body.position = (Vector2)enemy.transform.position + new Vector2(-1.05f, 0.1f);
                    body.linearVelocity = Vector2.down;
                    Physics2D.SyncTransforms();
                    Set("attackDirection", 0); Set("attackFacingDirection", 1f);
                    int before = enemy.CurrentHealth;
                    Invoke("PerformAttack");
                    Require(enemy.CurrentHealth == before - 1, "Side hit did not damage the enemy.");
                    Require(body.linearVelocity.y < 0, "Side hit incorrectly bounced.");
                    Pogo(false);
                    bounceBaseY = body.position.y;
                    bouncePeakY = bounceBaseY;
                    Wait(6, 0.65); break;
                case 6:
                    float bounceHeight = bouncePeakY - bounceBaseY;
                    Require(bounceHeight > 0.6f && bounceHeight < 1.6f,
                        $"Reduced bounce height out of range: {bounceHeight:F2} units.");
                    Debug.Log($"BOUNCE HEIGHT CHECK PASSED: speed=5 units/s, measured rise={bounceHeight:F2} world units (about one tile).");
                    Pogo(true);
                    player.transform.position = start;
                    body.position = start;
                    body.linearVelocity = Vector2.zero;
                    Physics2D.SyncTransforms();
                    Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();
                    Debug.Log("TRAVERSAL CHECK PASSED: left/right wall jumps and re-grabs; custom four-frame wall pose; downward hits and killing blows bounce; misses and side hits do not bounce.");
                    EditorApplication.update -= Tick;
                    break;
            }
        }
        catch (Exception exception)
        {
            EditorApplication.update -= Tick;
            Debug.LogException(exception);
        }
    }

    private static void Pogo(bool lethal)
    {
        body.position = (Vector2)enemy.transform.position + Vector2.up * 1.15f;
        body.linearVelocity = Vector2.down * 3;
        Physics2D.SyncTransforms();
        Set("isGrounded", false); Set("attackDirection", 2);
        int before = enemy.CurrentHealth;
        Invoke("PerformAttack");
        Require(enemy.CurrentHealth == before - 1, "Downward hit did not damage enemy.");
        Require(Mathf.Abs(body.linearVelocity.y - 5f) < 0.05f, "Downward bounce speed should be the reduced 5 units/s.");
        if (lethal) Require(enemy.CurrentHealth == 0, "Killing-blow bounce was not exercised.");
    }

    private static void PlaceAtWall(bool right, float y)
    {
        bool expanded = UnityEngine.Object.FindFirstObjectByType<StationMapInfo>() != null;
        body.position = new Vector2(expanded ? (right ? -16.30f : -51.70f) : (right ? 7.70f : -7.70f), y);
        body.linearVelocity = Vector2.down * 0.1f;
        Set("jumpsLeft", 0);
        Physics2D.SyncTransforms();
        Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();
    }

    private static void CheckWallJump(bool right)
    {
        Require(player.IsWallSliding, "Player is not sliding at " + (right ? "right" : "left") + " wall.");
        Require(player.GetComponent<SpriteRenderer>().flipX == !right, "Wall pose faces away from wall.");
        Animator animator = player.GetComponent<Animator>();
        Require(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.WallSlide"),
            $"Wall state is stuck in a fall animation: stage={stage}, position={body.position}, velocity={body.linearVelocity}, clip={player.GetComponent<SpriteRenderer>().sprite.name}, transition={animator.IsInTransition(0)}.");
        Require(player.GetComponent<SpriteRenderer>().sprite.name.StartsWith("WallCling_"), "Custom wall art not playing.");
        Set("jumpsLeft", 0);
        Require((bool)Invoke("Jump"), "Wall jump was rejected with no air jumps left.");
        Require(body.linearVelocity.y > 10 && (right ? body.linearVelocity.x < -10 : body.linearVelocity.x > 10), "Incorrect wall-jump impulse.");
    }

    private static object Invoke(string method) => typeof(PlayerMove).GetMethod(method, Flags).Invoke(player, null);
    private static void Set(string field, object value) => typeof(PlayerMove).GetField(field, Flags).SetValue(player, value);
    private static void Wait(int next, double delay) { stage = next; nextStep = Time.timeAsDouble + delay; }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("TRAVERSAL CHECK FAILED: " + message);
    }
}
#endif
