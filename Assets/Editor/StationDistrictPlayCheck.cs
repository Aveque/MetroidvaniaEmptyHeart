#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

// Isolate each new branch at its entrance, then traverse with real keyboard input
// and ordinary player physics. No Dash, altered gravity or injected velocity.
public static class StationDistrictPlayCheck
{
    struct Leg {public Vector2 p;public bool jump;public Leg(float x,float y,bool j=false){p=new Vector2(x,y);jump=j;}}
    static readonly Leg[][] routes={
        new[]{new Leg(-54,19.5f,true),new Leg(-60,18.5f),new Leg(-88,18.5f),new Leg(-59,18.5f),new Leg(-54,19.5f,true),new Leg(-48,18.5f)},
        new[]{new Leg(49.5f,30.5f,true),new Leg(52.5f,33.5f,true),new Leg(49.5f,36.5f,true),new Leg(52.5f,39.5f,true),new Leg(49.5f,42.5f,true),new Leg(52.5f,45.5f,true),new Leg(55.5f,43.5f)},
        new[]{new Leg(26.5f,-22.5f),new Leg(23.5f,-25.5f),new Leg(26.5f,-28.5f),new Leg(23.5f,-31.5f),new Leg(29,-35.5f),new Leg(23.5f,-31.5f,true),new Leg(26.5f,-28.5f,true),new Leg(23.5f,-25.5f,true),new Leg(26.5f,-22.5f,true),new Leg(23.5f,-19.5f,true)}
    };
    static readonly Vector2[] starts={new Vector2(-48,18.5f),new Vector2(53,26.5f),new Vector2(23.5f,-19.5f)};
    static PlayerMove player;static Rigidbody2D body;static Keyboard keyboard;static KeyboardState keys;
    static InputSettings.EditorInputBehaviorInPlayMode oldInput;static bool setup,started;static int route,leg;static double until,began;
    static Collider2D[] patrols;static Collider2D playerCollider;
    static readonly FieldInfo groundedField=typeof(PlayerMove).GetField("isGrounded",BindingFlags.Instance|BindingFlags.NonPublic);
    [MenuItem("Tools/Empty Heart/Проверки/Мир/Проходимость комнат")]
    public static void Run()
    {
        if(!EditorApplication.isPlaying)return;
        player=Object.FindFirstObjectByType<PlayerMove>();body=player.GetComponent<Rigidbody2D>();
        playerCollider=player.GetComponent<Collider2D>();patrols=Object.FindObjectsByType<EnemyPatrol>(FindObjectsSortMode.None).Select(e=>e.GetComponent<Collider2D>()).ToArray();
        foreach(var c in patrols)Physics2D.IgnoreCollision(playerCollider,c,true);
        player.GetComponent<PlayerHealth>().GrantContactGrace(300);
        oldInput=InputSystem.settings.editorInputBehaviorInPlayMode;setup=true;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.onBeforeUpdate+=Feed;
        route=0;StartRoute();EditorApplication.update-=Tick;EditorApplication.update+=Tick;
    }
    static void StartRoute()
    {body.position=starts[route];player.transform.position=body.position;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();leg=0;started=false;keys=new KeyboardState();until=Time.timeAsDouble+.3;}
    static void Tick()
    {
        if(!EditorApplication.isPlaying){Finish("Play stopped");return;}
        try{
            if(Time.timeAsDouble<until)return;
            if(leg==routes[route].Length){Debug.Log("DISTRICT ROUTE PASSED: "+route);if(++route<routes.Length){StartRoute();return;}Finish(null);return;}
            var target=routes[route][leg];bool grounded=(bool)groundedField.GetValue(player);
            if(!started){if(!grounded){keys=new KeyboardState();return;}started=true;began=Time.timeAsDouble;}
            if(Time.timeAsDouble-began>12)throw new Exception($"Route {route} leg {leg} stuck at {body.position}, target {target.p}, velocity {body.linearVelocity} grounded={grounded}");
            float dx=target.p.x-(body.position.x+body.linearVelocity.x*.045f);
            var held=new System.Collections.Generic.List<Key>();if(Mathf.Abs(dx)>.16f)held.Add(dx>0?Key.D:Key.A);if(target.jump)held.Add(Key.Space);keys=new KeyboardState(held.ToArray());
            var view=Camera.main.WorldToViewportPoint(body.position);if(view.x<-.2f || view.x>1.2f || view.y<-.2f || view.y>1.2f)throw new Exception("Camera lost player on "+route+":"+leg);
            if(Time.timeAsDouble-began>.3 && grounded && Vector2.Distance(body.position,target.p)<.7f){Debug.Log($"DISTRICT LEG {route}:{leg} landed {body.position}");leg++;started=false;keys=new KeyboardState();until=Time.timeAsDouble+.15;}
        }catch(Exception e){Finish(e.ToString());}
    }
    static void Feed(){if(keyboard!=null && keyboard.added && InputState.currentUpdateType==InputUpdateType.Dynamic)InputSystem.QueueStateEvent(keyboard,keys);}
    static void Finish(string error)
    {EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=Feed;if(playerCollider && patrols!=null)foreach(var c in patrols)if(c)Physics2D.IgnoreCollision(playerCollider,c,false);if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);keyboard=null;if(setup)InputSystem.settings.editorInputBehaviorInPlayMode=oldInput;setup=false;string result=error==null?"NEW DISTRICTS PASSED: incubation corridor both directions, observatory stair ascent, pumpworks descent and return ascent using keyboard and ordinary player physics; patrol contacts isolated; camera retained player throughout.":"NEW DISTRICTS FAILED: "+error;File.WriteAllText("StationDistrictCheck.txt",result);if(error==null)Debug.Log(result);else Debug.LogError(result);}
    static void Preview()
    {if(!EditorApplication.isPlaying)return;var p=Object.FindFirstObjectByType<PlayerMove>();var b=p.GetComponent<Rigidbody2D>();Vector2[] points={new Vector2(-78,18.5f),new Vector2(16,-35.5f),new Vector2(37,43.5f),new Vector2(3,18.5f),new Vector2(-30,18.5f),new Vector2(40,22.5f),new Vector2(35,-2.5f)};b.position=points[preview++%points.Length];p.transform.position=b.position;b.linearVelocity=Vector2.zero;p.GetComponent<PlayerHealth>().GrantContactGrace(30);Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();}
    static int preview;
}
#endif
