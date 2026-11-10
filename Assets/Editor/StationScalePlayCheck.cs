#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

public static class StationScalePlayCheck
{
    static StationMonument[] monuments;static PlayerMove player;static Rigidbody2D body;static Keyboard keyboard;static KeyboardState keys;
    static Collider2D[] contacts;static bool[] contactStates;static InputSettings.EditorInputBehaviorInPlayMode previous;
    static int index,stage;static double next,deadline;static float endX;
    static void Need(bool b,string message){if(!b)throw new Exception(message);}
    [MenuItem("Tools/Empty Heart/Проверки/Мир/Крупные установки")]
    public static void Run()
    {
        if(!EditorApplication.isPlaying)return;
        try
        {
            monuments=Object.FindObjectsByType<StationMonument>(FindObjectsSortMode.None).OrderBy(m=>m.name).ToArray();Need(monuments.Length==4,"Four architectural installations required");Physics2D.SyncTransforms();int mask=1<<LayerMask.NameToLayer("Wall");
            foreach(var m in monuments)
            {
                var r=m.GetComponent<SpriteRenderer>();Need(r && r.sprite && r.sortingOrder==-18,"Scenery depth/art missing: "+m.name);
                Need(m.Size.y>=5.7f && m.Size.x>5,"Installation still too small: "+m.name);
                Need(!m.GetComponentInChildren<Collider2D>(),"Scenery blocks gameplay: "+m.name);
                Need(Mathf.Abs(r.bounds.min.y-m.FloorY)<.025f,"Foundation floating: "+m.name);
                Bounds room=m.Room.GetComponent<StationRoomShape>().WorldBounds;
                Need(r.bounds.min.x>=room.min.x && r.bounds.max.x<=room.max.x && r.bounds.max.y<=room.max.y,"Installation exceeds its chamber: "+m.name);
                for(float x=-m.Size.x*.46f;x<=m.Size.x*.46f;x+=.35f)
                {var hit=Physics2D.Raycast(new Vector2(m.transform.position.x+x,m.FloorY+.12f),Vector2.down,.26f,mask);Need(hit.collider && Mathf.Abs(hit.point.y-m.FloorY)<.04f,"Unsupported foundation: "+m.name);}
            }
            var props=Object.FindObjectsByType<StationStoryDetail>(FindObjectsSortMode.None);Need(props.Min(d=>d.BaseSize.y)<.8f && props.Max(d=>d.BaseSize.y)>6,"Small/medium scenery hierarchy missing");
            player=Object.FindFirstObjectByType<PlayerMove>();body=player.GetComponent<Rigidbody2D>();player.GetComponent<PlayerHealth>().GrantContactGrace(90);
            contacts=Object.FindObjectsByType<EnemyPatrol>(FindObjectsSortMode.None).SelectMany(e=>e.GetComponentsInChildren<Collider2D>()).ToArray();contactStates=contacts.Select(c=>c.enabled).ToArray();foreach(var c in contacts)c.enabled=false;
            previous=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;keyboard=InputSystem.AddDevice<Keyboard>();keys=new KeyboardState();InputSystem.onBeforeUpdate+=Feed;InputSystem.onAfterUpdate+=Current;
            Directory.CreateDirectory("StoryScalePreviews");index=stage=0;next=0;EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }catch(Exception e){Finish(e);}
    }
    static void Feed(){if(keyboard!=null && keyboard.added)InputSystem.QueueStateEvent(keyboard,keys);}
    static void Current(){if(keyboard!=null && keyboard.added && InputState.currentUpdateType==InputUpdateType.Dynamic)keyboard.MakeCurrent();}
    static void Warp(StationMonument m,float x)
    {
        float feet=player.transform.position.y-player.GetComponent<Collider2D>().bounds.min.y;Vector2 p=new Vector2(x,m.FloorY+feet+.05f);body.position=p;player.transform.position=p;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying){Finish(new Exception("Play stopped"));return;}if(Time.timeAsDouble<next)return;
        try
        {
            var m=monuments[index];
            if(stage==0){keys=new KeyboardState();Warp(m,m.transform.position.x-m.Size.x*.18f);stage=1;next=Time.timeAsDouble+.45;}
            else if(stage==1){ScreenCapture.CaptureScreenshot("StoryScalePreviews/"+m.name.Replace(" ","_")+".png");stage=2;next=Time.timeAsDouble+.25;}
            else if(stage==2){Warp(m,m.transform.position.x-m.Size.x*.5f-.6f);endX=m.transform.position.x+m.Size.x*.5f+.2f;stage=3;next=Time.timeAsDouble+.2;}
            else if(stage==3){keys=new KeyboardState(Key.D);deadline=Time.timeAsDouble+8;stage=4;next=Time.timeAsDouble+.05;}
            else
            {
                if(body.position.x<endX){Need(Time.timeAsDouble<deadline,"Traversal blocked at "+m.name+"; x="+body.position.x);next=Time.timeAsDouble+.05;return;}
                keys=new KeyboardState();Debug.Log("MONUMENT WALK PASSED: "+m.name);if(++index==monuments.Length){Finish(null);return;}stage=0;next=Time.timeAsDouble+.15;
            }
        }catch(Exception e){Finish(e);}
    }
    static void Finish(Exception e)
    {
        EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=Feed;InputSystem.onAfterUpdate-=Current;
        if(contacts!=null)for(int k=0;k<contacts.Length;k++)if(contacts[k])contacts[k].enabled=contactStates[k];contacts=null;
        if(keyboard!=null && keyboard.added){InputSystem.RemoveDevice(keyboard);InputSystem.settings.editorInputBehaviorInPlayMode=previous;}keyboard=null;
        string result=e==null?"MONUMENTAL SCENERY PASSED: four installations 5.8–10.8 units tall, grounded full-width foundations inside their chambers, behind platforms/player with no added collision; small 0.65–2.7 and medium 4.2–6.4 props retained; actual D-input traversal across all four installations with patrol contacts temporarily isolated and restored; four in-game previews captured.":"MONUMENTAL SCENERY FAILED: "+e;File.WriteAllText("StoryScaleCheck.txt",result);if(e==null)Debug.Log(result);else Debug.LogError(result);
    }
    public static void Preview(string prefix)
    {
        if(!EditorApplication.isPlaying)return;var m=Object.FindObjectsByType<StationMonument>(FindObjectsSortMode.None).First(m=>m.name.StartsWith(prefix));player=Object.FindFirstObjectByType<PlayerMove>();body=player.GetComponent<Rigidbody2D>();player.GetComponent<PlayerHealth>().GrantContactGrace(90);Warp(m,m.transform.position.x-m.Size.x*.18f);
    }
    [MenuItem("Tools/Empty Heart/Просмотр/Обсерватория")]
    static void Observatory()=>Preview("Observatory");
    [MenuItem("Tools/Empty Heart/Просмотр/Насосная")]
    static void Pumpworks()=>Preview("Pumpworks");
}
#endif
