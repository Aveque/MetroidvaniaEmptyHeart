#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

public static class OrganSystemPlayCheck
{
    static readonly Queue<(Action action,float delay)> steps=new();static double next,until;static int stage;
    static PlayerMove player;static PlayerOrgans organs;static PlayerHealth health;static Rigidbody2D body;static ImpulseResidue residue;
    static Keyboard keyboard;static Mouse mouse;static KeyboardState keys;static MouseState buttons;static InputSettings.EditorInputBehaviorInPlayMode previous;
    static Collider2D[] contacts;static bool[] contactStates;static AsyncOperation loading;
    const string Menu="Tools/Empty Heart/Проверки/Персонаж/Импульс и сменные органы";
    [MenuItem(Menu,true)]static bool Ready()=>EditorApplication.isPlaying && SceneManager.GetActiveScene().name=="StationEcho_Organic";
    [MenuItem(Menu)]public static void Run()
    {
        if(!Ready())return;
        try
        {
            Need(PlayerOrgans.Owned==0,"Use a fresh Play session");Acquire();Need(Object.FindObjectsByType<OrganPickup>(FindObjectsSortMode.None).Length==3,"Three organ pickups missing");
            foreach(int id in Enumerable.Range(0,3))Need(OrganPickup.Art(id),"Generated organ art missing");Need(Resources.Load<Sprite>("Organs/impulse"),"Impulse artwork missing");
            contacts=Object.FindObjectsByType<EnemyPatrol>(FindObjectsSortMode.None).SelectMany(e=>e.GetComponentsInChildren<Collider2D>()).ToArray();contactStates=contacts.Select(c=>c.enabled).ToArray();foreach(var c in contacts)c.enabled=false;
            previous=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();keys=new KeyboardState();buttons=new MouseState();InputSystem.onBeforeUpdate+=Feed;InputSystem.onAfterUpdate+=Current;
            steps.Clear();stage=0;Directory.CreateDirectory("OrganPreviews");
            foreach(int value in Enumerable.Range(0,3))
            {
                int id=value;Add(()=>{var pickup=Object.FindObjectsByType<OrganPickup>(FindObjectsSortMode.None).First(p=>p.Id==id);Warp(pickup.transform.position);keys=new KeyboardState();},.35f);
                if(id==0)Add(()=>ScreenCapture.CaptureScreenshot("OrganPreviews/FirstOrgan.png"),.15f);
                Add(()=>keys=new KeyboardState(Key.E),.2f);Add(()=>{Need((PlayerOrgans.Owned&(1<<id))!=0,"Actual E pickup failed: "+id);keys=new KeyboardState();},.12f);
            }
            Add(()=>{Need(!PlayerOrgans.Acquire(0),"Duplicate organ accepted");Warp(new Vector2(-26,-2.5f));Need(!organs.TryOpenMenu(),"Organ menu opened away from cradle");Rest();},.4f);
            Add(()=>keys=new KeyboardState(Key.G),.15f);
            Add(()=>keys=new KeyboardState(Key.Digit1),.15f);
            Add(()=>{Need(PlayerOrgans.Has(0),"Keyboard 1 did not install organ");keys=new KeyboardState();},.1f);
            Add(()=>keys=new KeyboardState(Key.Digit1),.15f);
            Add(()=>{Need(PlayerOrgans.Equipped==0,"Keyboard 1 did not remove organ");keys=new KeyboardState();},.1f);
            Add(()=>{Need(organs.MenuOpen && Time.timeScale==0,"G did not open paused organ menu");Need(!player.RequestAttack() && !Object.FindFirstObjectByType<StationAtlas>().Toggle(),"Gameplay leaks into menu");Need(organs.ToggleOrgan(0) && organs.ToggleOrgan(1),"Two sockets failed");Need(!organs.ToggleOrgan(2) && PlayerOrgans.Equipped==3,"Third organ bypassed limit");Need(organs.ToggleOrgan(0) && organs.ToggleOrgan(2) && PlayerOrgans.Equipped==6,"Module replacement failed");Need(Mathf.Abs(PlayerOrgans.DashCooldownMultiplier-.7f)<.001f,"Tendon modifier missing");Need(!player.DashUnlocked && !player.TryDash(),"Tendon bypassed Dash progression");keys=new KeyboardState();ScreenCapture.CaptureScreenshot("OrganPreviews/OrganMenu.png");},.25f);
            Add(()=>keys=new KeyboardState(Key.Escape),.2f);
            Add(()=>{keys=new KeyboardState();player.UnlockDash();Need(player.TryDash(),"Initial Dash failed");},.4f);
            Add(()=>Need(player.TryDash(),"Tendon did not reduce actual Dash cooldown below 0.5 seconds"),.25f);
            Add(()=>{Need(!organs.MenuOpen && Time.timeScale==1,"Escape did not resume world");keys=new KeyboardState();health.Configure(5);Damage(2);Warp(new Vector2(-26,-2.5f));var enemy=Object.Instantiate(Object.FindFirstObjectByType<EnemyPatrol>());enemy.transform.position=new Vector2(-25,-2.5f);enemy.TakeDamage(999);enemy.TakeDamage(999);},1.25f);
            Add(()=>{Need(Object.FindObjectsByType<ImpulseResidue>(FindObjectsSortMode.None).Length==1,"Enemy death did not drop exactly one impulse");residue=Object.FindFirstObjectByType<ImpulseResidue>();Warp(new Vector2(residue.transform.position.x-.7f,-2.5f));},.35f);
            Add(()=>keys=new KeyboardState(Key.F),.4f);
            Add(()=>{Need(organs.Channeling && organs.Progress>0,"F did not channel");ScreenCapture.CaptureScreenshot("OrganPreviews/Absorption.png");},.1f);
            Add(()=>keys=new KeyboardState(),.2f);
            Add(()=>{Need(!organs.Channeling && residue && health.CurrentHealth==3,"Release consumed impulse or healed");keys=new KeyboardState(Key.F);},.3f);
            Add(()=>{buttons=new MouseState().WithButton(MouseButton.Left);},.2f);
            Add(()=>{Need(!organs.Channeling && residue,"Attack did not interrupt");keys=new KeyboardState();buttons=new MouseState();},.6f);
            Add(()=>keys=new KeyboardState(Key.F),.3f);
            Add(()=>Damage(1),.2f);
            Add(()=>{Need(!organs.Channeling && residue && health.CurrentHealth==2,"Damage did not cancel vulnerable channel");keys=new KeyboardState();Warp(new Vector2(residue.transform.position.x-.7f,-2.5f));},1.1f);
            Add(()=>keys=new KeyboardState(Key.F),1.65f);
            Add(()=>{Need(!residue && health.CurrentHealth==4,"Reservoir did not consume one impulse and heal two HP");keys=new KeyboardState();Rest();},.4f);
            Add(()=>{Need(organs.TryOpenMenu(),"Cannot reopen organ menu");Need(organs.ToggleOrgan(2) && organs.ToggleOrgan(0) && PlayerOrgans.Equipped==3,"Fast absorption build failed");organs.CloseMenu();Warp(new Vector2(-26,-2.5f));residue=ImpulseResidue.Spawn(new Vector2(-25,-2.5f));},.35f);
            Add(()=>keys=new KeyboardState(Key.F),.9f);
            Add(()=>{Need(!residue && health.CurrentHealth==5,"Fast siphon timing or healing clamp failed");keys=new KeyboardState();residue=ImpulseResidue.Spawn(new Vector2(-25,-2.5f));},.1f);
            Add(()=>keys=new KeyboardState(Key.F),.3f);
            Add(()=>{Need(!organs.Channeling && residue,"Full HP consumed a residue");keys=new KeyboardState();int count=Object.FindObjectsByType<ImpulseResidue>(FindObjectsSortMode.None).Length;var sentry=Object.Instantiate(Object.FindFirstObjectByType<SynapticSentry>());sentry.transform.position=new Vector2(-24,-2);sentry.TakeDamage(999);sentry.TakeDamage(999);Need(Object.FindObjectsByType<ImpulseResidue>(FindObjectsSortMode.None).Length==count+1,"Sentry death duplicated/lost residue");Damage(999);},1.8f);
            Add(()=>{Need(!health.IsRespawning && PlayerOrgans.Owned==7 && PlayerOrgans.Equipped==3,"Death lost module loadout");RestoreContacts();loading=SceneManager.LoadSceneAsync("VascularThreshold");},.2f);
            Add(()=>WaitScene("VascularThreshold",()=>{Acquire();Need(PlayerOrgans.Owned==7 && PlayerOrgans.Equipped==3,"Biome travel lost organs");loading=SceneManager.LoadSceneAsync("StationEcho_Organic");}),.2f);
            Add(()=>WaitScene("StationEcho_Organic",()=>{Acquire();Need(PlayerOrgans.Owned==7 && PlayerOrgans.Equipped==3 && Object.FindObjectsByType<OrganPickup>(FindObjectsSortMode.None).Length==0,"Return duplicated pickups or lost loadout");}),.3f);
            Add(()=>Finish(null),0);next=0;until=EditorApplication.timeSinceStartup+100;EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }catch(Exception e){Finish(e);}
    }
    static void WaitScene(string scene,Action success)
    {
        if(loading!=null && loading.isDone && SceneManager.GetActiveScene().name==scene){success();return;}
        var rest=steps.ToArray();steps.Clear();Add(()=>WaitScene(scene,success),.2f);foreach(var step in rest)steps.Enqueue(step);
    }
    static void Add(Action action,float delay)=>steps.Enqueue((action,delay));
    static void Acquire(){player=Object.FindFirstObjectByType<PlayerMove>();organs=player.GetComponent<PlayerOrgans>();health=player.GetComponent<PlayerHealth>();body=player.GetComponent<Rigidbody2D>();Need(organs,"Player organ component not bootstrapped");}
    static void Rest(){var c=RestCheckpoint.Instances.First();Warp(c.SpawnPoint);}
    static void Warp(Vector2 p){body.position=p;player.transform.position=p;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();}
    static void Damage(int amount){typeof(PlayerHealth).GetField("contactGraceUntil",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(health,0f);health.TakeDamage(amount);}
    static void Need(bool b,string message){if(!b)throw new Exception(message);}
    static void Feed(){if(keyboard!=null && keyboard.added){InputSystem.QueueStateEvent(keyboard,keys);InputSystem.QueueStateEvent(mouse,buttons);}}
    static void Current(){if(keyboard!=null && keyboard.added && InputState.currentUpdateType==InputUpdateType.Dynamic){keyboard.MakeCurrent();mouse.MakeCurrent();}}
    static void Tick(){if(!EditorApplication.isPlaying){Finish(new Exception("Play stopped"));return;}if(EditorApplication.timeSinceStartup>until){Finish(new Exception("Timeout"));return;}if(Time.unscaledTimeAsDouble<next)return;try{if(steps.Count==0)return;var s=steps.Dequeue();stage++;s.action();next=Time.unscaledTimeAsDouble+s.delay;}catch(Exception e){Finish(e);}}
    static void RestoreContacts(){if(contacts!=null)for(int i=0;i<contacts.Length;i++)if(contacts[i])contacts[i].enabled=contactStates[i];contacts=null;}
    static void Finish(Exception e)
    {
        EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=Feed;InputSystem.onAfterUpdate-=Current;RestoreContacts();if(keyboard!=null && keyboard.added){InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);InputSystem.settings.editorInputBehaviorInPlayMode=previous;}keyboard=null;if(organs){organs.Cancel();organs.CloseMenu();}
        string result=e==null?"ORGANS PASSED: three generated pickups through real E; keyboard 1 installs/removes; actual tendon Dash cooldown; sentry residue; G/ESC paused checkpoint menu; two-slot limit and swapping; tendon does not grant locked Dash; exactly one impulse from enemy death; F channel release/attack/damage cancellation; one-time healing, reservoir +2, fast siphon and HP cap; full health cannot consume; death and biome round trip preserve organs without duplicate pickups.":"ORGANS FAILED at step "+stage+": "+e;File.WriteAllText("OrganSystemCheck.txt",result);if(e==null)Debug.Log(result);else Debug.LogError(result);
    }
}
#endif


