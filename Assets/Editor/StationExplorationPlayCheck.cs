#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

// Isolates each mechanic at its entrance; the separate station route check covers traversal.
public static class StationExplorationPlayCheck
{
    static PlayerMove player;static PlayerHealth health;static Rigidbody2D body;static Keyboard keyboard;static KeyboardState keys;
    static InputSettings.EditorInputBehaviorInPlayMode oldInput;static int stage,originalHp,shots,travelHp;static double next,began;static string checkpointName;
    static SynapticSentry sentry;static StationLift lift;static bool passedMembrane,sawCharge;static int hpBeforeShot;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Place(Vector2 p){body.position=p;player.transform.position=p;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();}
    static void Acquire(){player=Object.FindFirstObjectByType<PlayerMove>();health=player.GetComponent<PlayerHealth>();body=player.GetComponent<Rigidbody2D>();}
    static void Wait(int step,float delay){stage=step;next=EditorApplication.timeSinceStartup+delay;}
    [MenuItem("Tools/Empty Heart/Проверки/Мир/Исследование (после проверки босса)")]
    public static void Run()
    {
        if(!EditorApplication.isPlaying)return;
        try
        {
            Acquire();Require(Object.FindFirstObjectByType<BossArena>().Defeated && player.DashUnlocked,"Run the boss fight check first, then this exploration check.");
            Require(Object.FindObjectsByType<CameraRoom>(FindObjectsSortMode.None).Length==15,"Secret room areas missing.");
            oldInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.onBeforeUpdate+=Feed;
            health.GrantContactGrace(120);originalHp=health.MaxHealth;Place(new Vector2(-90,22.5f));keys=new KeyboardState(Key.A);passedMembrane=false;
            began=EditorApplication.timeSinceStartup;Wait(0,1.2f);EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }catch(Exception e){Finish(e);}
    }
    static void Feed(){if(keyboard!=null && keyboard.added)InputSystem.QueueStateEvent(keyboard,keys);}
    static void Tick()
    {
        if(!EditorApplication.isPlaying){Finish(new Exception("Play stopped"));return;}
        try
        {
            Require(EditorApplication.timeSinceStartup-began<90,"Exploration test timeout at "+stage);
            if(stage==1 && Object.FindObjectsByType<DashMembrane>(FindObjectsSortMode.None).Any(m=>m.Passing))passedMembrane=true;
            if(stage==11 && sentry && sentry.Charging)sawCharge=true;
            if(EditorApplication.timeSinceStartup<next)return;
            switch(stage)
            {
                case 0:
                    Require(body.position.x>-93,"Membrane did not block ordinary walking: "+body.position);
                    keys=new KeyboardState(Key.A,Key.LeftShift);Wait(1,.18f);break;
                case 1:
                    Require(passedMembrane && body.position.x<-93.5f,"Real Dash did not cross membrane: "+body.position);
                    keys=new KeyboardState();Wait(2,.5f);break;
                case 2:
                    var atlas=Object.FindFirstObjectByType<StationAtlas>();var annex=Object.FindObjectsByType<CameraRoom>(FindObjectsSortMode.None).First(r=>r.name.StartsWith("14 "));
                    Require(atlas.Discovered(annex),"Secret room was not recorded by exploration.");
                    keys=new KeyboardState(Key.M);Wait(18,.12f);break;
                case 18:
                    Require(Object.FindFirstObjectByType<StationAtlas>().IsOpen && Time.timeScale==0,"M key did not open and pause map.");keys=new KeyboardState();Wait(19,.12f);break;
                case 19:keys=new KeyboardState(Key.M);Wait(20,.12f);break;
                case 20:
                    Require(!Object.FindFirstObjectByType<StationAtlas>().IsOpen && Time.timeScale==1,"M key did not close and resume map.");
                    keys=new KeyboardState();Place(new Vector2(-104,22.7f));Wait(3,.3f);break;
                case 3:Require(StationJourney.Fragments==1,"First secret fragment trigger missed.");Place(new Vector2(67,-32.3f));Wait(4,.3f);break;
                case 4:Require(StationJourney.Fragments==2,"Second secret fragment trigger missed.");Place(new Vector2(43,50.7f));Wait(5,.3f);break;
                case 5:
                    Require(StationJourney.Fragments==3 && health.MaxHealth==originalHp+1,"Three fragments did not grant one health segment.");
                    var duplicate=Object.FindObjectsByType<ShellFragment>(FindObjectsSortMode.None).FirstOrDefault();if(duplicate)Require(!duplicate.TryCollect(health),"Fragment duplicated reward.");
                    lift=Object.FindObjectsByType<StationLift>(FindObjectsSortMode.None).First(l=>l.Id=="pump-relay" && !l.UnlockEnd);Place(lift.transform.position);Wait(6,.2f);break;
                case 6:
                    Require(!lift.Available && !lift.TryUse(player),"Shortcut opened from locked end.");lift=lift.Other;Place(lift.transform.position);Wait(7,.2f);break;
                case 7:Require(lift.TryUse(player),"Far-side actuator refused activation.");Wait(8,2);break;
                case 8:
                    Require(lift.Available && Vector2.Distance(body.position,lift.Other.transform.position)<.5f && player.enabled,"Lift did not arrive or restore movement.");
                    lift=Object.FindObjectsByType<StationLift>(FindObjectsSortMode.None).First(l=>l.Id=="observatory-gallery" && l.UnlockEnd);Place(lift.transform.position);Wait(9,.2f);break;
                case 9:Require(lift.TryUse(player),"Observatory lift refused.");Wait(10,2);break;
                case 10:
                    Require(lift.Available && Vector2.Distance(body.position,lift.Other.transform.position)<.5f,"Observatory shortcut did not arrive.");
                    sentry=Object.FindObjectsByType<SynapticSentry>(FindObjectsSortMode.None).First(s=>s.name.StartsWith("Relay"));shots=sentry.ShotsFired;sawCharge=false;hpBeforeShot=health.CurrentHealth;
                    typeof(PlayerHealth).GetField("contactGraceUntil",Private).SetValue(health,0f);Place(new Vector2(-5,-2.5f));Wait(11,4.2f);break;
                case 11:
                    Require(sentry.ShotsFired>shots,"Sentry failed line of sight / aimed firing.");
                    Require(sawCharge && health.CurrentHealth<hpBeforeShot,"Sentry charge indicator or real projectile damage failed.");health.GrantContactGrace(120);
                    body.gravityScale=0;Place((Vector2)sentry.transform.position+Vector2.right*.95f);keys=new KeyboardState(Key.A);Wait(12,.05f);break;
                case 12:keys=new KeyboardState();player.RequestAttack();Wait(13,.45f);break;
                case 13:Require(sentry.CurrentHealth<3,"Player sword did not damage ranged enemy.");player.RequestAttack();Wait(14,.45f);break;
                case 14:player.RequestAttack();Wait(15,.6f);break;
                case 15:
                    Require(!sentry || sentry.CurrentHealth==0,"Ranged enemy could not be killed.");body.gravityScale=3;Place(new Vector2(93,-2.5f));
                    var checkpoint=Object.FindObjectsByType<RestCheckpoint>(FindObjectsSortMode.None).First();health.RestAt(checkpoint);checkpointName=checkpoint.name;
                    typeof(PlayerHealth).GetField("contactGraceUntil",Private).SetValue(health,0f);health.TakeContactDamage(1,player.transform.position+Vector3.left);travelHp=health.CurrentHealth;
                    var passage=Object.FindFirstObjectByType<StationPassage>();Require(passage.TryTravel(player),"Post-boss passage did not open.");Wait(16,2);break;
                case 16:
                    if(SceneManager.GetActiveScene().name!="VascularThreshold" || StationJourney.Travelling){Wait(16,.2f);break;}
                    Require(SceneManager.GetActiveScene().name=="VascularThreshold","Next biome scene not loaded.");Acquire();
                    Require(player.DashUnlocked && health.MaxHealth==originalHp+1 && health.CurrentHealth==travelHp && !StationJourney.Travelling,"Abilities/health/movement lost during travel.");
                    Require(Object.FindObjectsByType<RestCheckpoint>(FindObjectsSortMode.None).Length==1,"Next biome arrival cradle missing.");
                    Place(new Vector2(-3,-2.5f));Require(Object.FindFirstObjectByType<StationPassage>().TryTravel(player),"Return passage refused.");Wait(17,2);break;
                case 17:
                    if(SceneManager.GetActiveScene().name!="StationEcho_Organic" || StationJourney.Travelling){Wait(17,.2f);break;}
                    Require(SceneManager.GetActiveScene().name=="StationEcho_Organic","Return scene not loaded.");Acquire();
                    Require(Object.FindFirstObjectByType<BossArena>().Defeated && !Object.FindFirstObjectByType<BossArena>().DoorsLocked,"Boss revived or locked doors on return.");
                    Require(StationJourney.Collected.Count==3 && Object.FindObjectsByType<ShellFragment>(FindObjectsSortMode.None).Length==0,"Collected secrets respawned across travel.");
                    Require(player.DashUnlocked && health.MaxHealth==originalHp+1 && StationJourney.Opened.Count==2,"Travel lost progression.");
                    Require(health.CurrentHealth==travelHp && health.ActiveCheckpoint && health.ActiveCheckpoint.name==checkpointName,"Travel lost current health or station checkpoint.");
                    Finish(null);break;
            }
        }catch(Exception e){Finish(e);}
    }
    static void Finish(Exception e)
    {
        EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=Feed;if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);keyboard=null;
        InputSystem.settings.editorInputBehaviorInPlayMode=oldInput;Time.timeScale=1;
        string result=e==null?"EXPLORATION PASSED: ordinary walking blocked by membrane; real keyboard Dash crosses; camera records secret room; map pauses and resumes; three physical fragment pickups grant one HP segment without duplicates; far-side shortcut locks and both lift arrivals; sentry aimed firing and death from player sword; next-biome scene loads and returns with Dash, health, fragments, lifts and defeated boss retained.":"EXPLORATION FAILED at "+stage+": "+e;
        File.WriteAllText("StationExplorationCheck.txt",result);if(e==null)Debug.Log(result);else Debug.LogError(result);
    }
    [MenuItem("Tools/Empty Heart/Просмотр/Карта изученных комнат")]
    static void PreviewAtlas()
    {
        if(!EditorApplication.isPlaying)return;var atlas=Object.FindFirstObjectByType<StationAtlas>();
        Debug.Log("ATLAS PREVIEW: toggled="+atlas.Toggle()+" open="+atlas.IsOpen+" keyboard="+(Keyboard.current!=null?Keyboard.current.displayName:"none")+" timescale="+Time.timeScale);
    }
    [MenuItem("Tools/Empty Heart/Просмотр/Полная карта")]
    static void PreviewCompleteAtlas()
    {
        if(!EditorApplication.isPlaying)return;
        foreach(var room in Object.FindObjectsByType<CameraRoom>(FindObjectsSortMode.None))StationJourney.Visited.Add(room.gameObject.scene.name+"/"+room.name);
        PreviewAtlas();
    }
}
#endif
