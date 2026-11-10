#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

public static class CheckpointPlayCheck
{
    private static PlayerRest rest;
    private static PlayerHealth health;
    private static PlayerMove move;
    private static Rigidbody2D body;
    private static RestCheckpoint[] stations;
    private static int stage,index;
    private static float next,started;
    [MenuItem("Tools/Empty Heart/Проверки/Персонаж/Чекпоинты")]
    public static void Run()
    {
        if(!EditorApplication.isPlaying)return;
        try
        {
            rest=Object.FindFirstObjectByType<PlayerRest>();health=rest.GetComponent<PlayerHealth>();
            move=rest.GetComponent<PlayerMove>();body=rest.GetComponent<Rigidbody2D>();
            stations=Object.FindObjectsByType<RestCheckpoint>(FindObjectsSortMode.None).OrderBy(s=>s.transform.position.x).ToArray();
            Require(stations.Length==5 && !health.ActiveCheckpoint,"Use a fresh session with five stations.");
            Require(!rest.TryRest(stations[2]),"Distant station accepted input.");
            index=0;stage=0;started=Time.time;Warp(stations[0].SpawnPoint+Vector3.up*2);Wait(.15f);
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        catch(Exception e){Finish(e);}
    }
    [MenuItem("Tools/Empty Heart/Просмотр/Чекпоинт")]
    public static void Preview()
    {
        if(!EditorApplication.isPlaying)return;
        var p=Object.FindFirstObjectByType<PlayerMove>();var s=Object.FindObjectsByType<RestCheckpoint>(FindObjectsSortMode.None).OrderBy(c=>c.transform.position.x).First();
        p.transform.position=s.SpawnPoint;p.GetComponent<Rigidbody2D>().position=s.SpawnPoint;
        p.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
        Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();
    }
    private static void Tick()
    {
        if(!EditorApplication.isPlaying){EditorApplication.update-=Tick;return;}
        if(Time.time<next)return;
        try
        {
            Require(Time.time-started<90,"Checkpoint test timed out.");
            switch(stage)
            {
                case 0:
                    Require(!rest.TryRest(stations[0]),"Airborne player can rest.");
                    Warp(stations[0].SpawnPoint);stage=1;Wait(.4f);break;
                case 1:
                    Require(rest.Nearby==stations[0] && !health.ActiveCheckpoint,"Approaching auto-activated checkpoint or prompt missing.");
                    health.TakeDamage(1);Require(health.CurrentHealth<health.MaxHealth,"Damage setup failed.");
                    stage=2;Wait(1.1f);break;
                case 2: Warp(stations[index].SpawnPoint);stage=3;Wait(.4f);break;
                case 3:
                    Require(rest.Nearby==stations[index],"Prompt missing at "+stations[index].name);
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.E));stage=4;Wait(.08f);break;
                case 4:
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                    Require(rest.IsResting && !move.enabled,"E did not start rest or input remains enabled.");
                    Require(health.ActiveCheckpoint!=stations[index],"Checkpoint activated before animation completed.");
                    stage=5;Wait(.6f);break;
                case 5:
                    Require(rest.IsResting && rest.Progress>.2f,"Rest animation did not play.");
                    var skin=rest.transform.Find("Painted Hero — animated skeleton");
                    Require(skin && skin.localPosition.y<-.04f,"Hero did not kneel.");
                    stage=6;Wait(1.3f);break;
                case 6:
                    Require(!rest.IsResting && move.enabled && health.ActiveCheckpoint==stations[index] && stations.Count(s=>s.IsActive)==1,"Last station did not become sole active checkpoint.");
                    Require(health.CurrentHealth==health.MaxHealth && Vector3.Distance(health.RespawnPoint,stations[index].SpawnPoint)<.01f,"Rest failed to heal or save spawn.");
                    Warp(new Vector3(70,-2.45f));stage=7;Wait(1.1f);break;
                case 7:
                    Require(Object.FindFirstObjectByType<BossArena>().InCombat,"Arena did not start.");
                    health.TakeDamage(999);Require(health.IsRespawning,"Death did not trigger.");stage=8;Wait(1.6f);break;
                case 8:
                    Require(Vector2.Distance(body.position,stations[index].SpawnPoint)<.25f,"Respawn did not use active checkpoint.");
                    var follow=Camera.main.GetComponent<CameraFollow>();
                    Require(follow.ActiveRoom && follow.ActiveRoom.Contains(body.position),"Camera stayed in death room.");
                    var arena=Object.FindFirstObjectByType<BossArena>();
                    Require(!arena.InCombat && !arena.DoorsLocked && arena.Boss.CurrentState==BiomechBoss.State.Dormant,"Boss failed to reset after checkpoint respawn.");
                    if(++index<stations.Length){Warp(stations[index].SpawnPoint);stage=2;Wait(.3f);}
                    else Finish(null);
                    break;
            }
        }
        catch(Exception e){Finish(e);}
    }
    private static void Warp(Vector3 point){body.position=point;rest.transform.position=point;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();}
    private static void Wait(float seconds){next=Time.time+seconds;}
    private static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    private static void Finish(Exception error)
    {
        EditorApplication.update-=Tick;
        if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        string message=error==null?"CHECKPOINTS PASSED: all five E interactions; no proximity activation; distance/airborne rejection; animated kneeling and control lock; full healing; exactly one active station; five deaths inside boss arena respawn at last station with correct camera and reset boss.":"CHECKPOINTS FAILED stage "+stage+" index "+index+": "+error;
        File.WriteAllText("RestCheckpointsCheck.txt",message);
        if(error==null)Debug.Log(message);else Debug.LogError(message);
    }
}
#endif
