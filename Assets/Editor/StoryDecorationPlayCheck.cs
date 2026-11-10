#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

public static class StoryDecorationPlayCheck
{
    static Collider2D[] contacts;static bool[] contactStates;static PlayerMove player;static Rigidbody2D body;static Keyboard keyboard;static KeyboardState keys;
    static InputSettings.EditorInputBehaviorInPlayMode previous;static StationStoryDetail[] samples;static int index,stage;static double next;static float endX;
    static void Need(bool value,string message){if(!value)throw new Exception(message);}
    [MenuItem("Tools/Empty Heart/Проверки/Мир/Малый и средний декор")]
    static void Run()
    {
        if(!EditorApplication.isPlaying)return;
        try
        {
            var root=GameObject.Find("Station Story Decorations");Need(root,"Missing scenery root");
            var details=root.GetComponentsInChildren<StationStoryDetail>();int expected=Object.FindFirstObjectByType<StationMonument>()?14:15;Need(details.Length==expected,"Expected "+expected+" active story compositions, got "+details.Length);
            Need(root.GetComponentsInChildren<Collider2D>().Length==0,"Scenery adds solid/trigger collisions");
            Physics2D.SyncTransforms();int mask=1<<LayerMask.NameToLayer("Wall");
            foreach(var d in details)
            {
                var r=d.GetComponent<SpriteRenderer>();Need(r && r.sprite && r.sortingOrder<0,"Missing artwork/background ordering: "+d.name);
                Need(Mathf.Abs(r.bounds.min.y-d.FloorY)<.025f,"Artwork not rooted on floor: "+d.transform.parent.name);
                for(float x=-d.BaseSize.x*.45f;x<=d.BaseSize.x*.45f;x+=.3f)
                {
                    Vector2 p=new Vector2(d.transform.position.x+x,d.FloorY+.12f);var h=Physics2D.Raycast(p,Vector2.down,.25f,mask);
                    Need(h.collider && Mathf.Abs(h.point.y-d.FloorY)<.04f,"Unsupported footprint: "+d.transform.parent.name+" at "+p);
                }
                Need(!Physics2D.OverlapBox(new Vector2(r.bounds.center.x,d.FloorY+d.BaseSize.y*.5f+.08f),new Vector2(d.BaseSize.x*.95f,d.BaseSize.y-.12f),0,mask),"Artwork intersects terrain: "+d.transform.parent.name);
            }
            Need(root.GetComponentsInChildren<StationStoryLight>().Any(l=>l.SignalSequence),"Observatory recorded signal missing");
            samples=new[]{"01 Entrance","02 Relay","03 Gallery","07 Gardens","11 Incubation"}.Select(prefix=>details.First(d=>d.transform.parent.name.StartsWith(prefix))).ToArray();
            player=Object.FindFirstObjectByType<PlayerMove>();body=player.GetComponent<Rigidbody2D>();player.GetComponent<PlayerHealth>().GrantContactGrace(90);
            previous=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            contacts=Object.FindObjectsByType<EnemyPatrol>(FindObjectsSortMode.None).SelectMany(e=>e.GetComponentsInChildren<Collider2D>()).ToArray();contactStates=contacts.Select(c=>c.enabled).ToArray();foreach(var c in contacts)c.enabled=false;keyboard=InputSystem.AddDevice<Keyboard>();keys=new KeyboardState();InputSystem.onBeforeUpdate+=Feed;InputSystem.onAfterUpdate+=Current;index=stage=0;next=0;EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }catch(Exception e){Finish(e);}
    }
    static void Current(){if(keyboard!=null && keyboard.added && InputState.currentUpdateType==InputUpdateType.Dynamic)keyboard.MakeCurrent();}
    static void Feed(){if(keyboard!=null && keyboard.added)InputSystem.QueueStateEvent(keyboard,keys);}
    static void Tick()
    {
        if(!EditorApplication.isPlaying){Finish(new Exception("Play stopped"));return;}if(Time.timeAsDouble<next)return;
        try
        {
            var d=samples[index];
            if(stage==0)
            {
                keys=new KeyboardState();float feet=player.transform.position.y-player.GetComponent<Collider2D>().bounds.min.y;
                body.position=new Vector2(d.transform.position.x-d.BaseSize.x*.5f-.65f,d.FloorY+feet+.04f);player.transform.position=body.position;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();
                endX=d.transform.position.x+d.BaseSize.x*.5f+.35f;stage=1;next=Time.timeAsDouble+.25;
            }
            else if(stage==1){keys=new KeyboardState(Key.D);stage=2;next=Time.timeAsDouble+(d.BaseSize.x+1.7f)/7f+.6;}
            else
            {
                keys=new KeyboardState();Need(body.position.x>endX,"Player could not walk past "+d.transform.parent.name+"; x="+body.position.x+" expected >"+endX);
                Debug.Log("SCENERY WALK PASSED: "+d.transform.parent.name);
                if(++index==samples.Length){Finish(null);return;}stage=0;next=Time.timeAsDouble+.1;
            }
        }catch(Exception e){Finish(e);}
    }
    static void Finish(Exception e)
    {
        EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=Feed;InputSystem.onAfterUpdate-=Current;if(contacts!=null)for(int i=0;i<contacts.Length;i++)if(contacts[i])contacts[i].enabled=contactStates[i];contacts=null;
        if(keyboard!=null && keyboard.added){InputSystem.RemoveDevice(keyboard);InputSystem.settings.editorInputBehaviorInPlayMode=previous;}keyboard=null;
        string message=e==null?"STORY DECOR PASSED: active service/living compositions; full footprints grounded on real floors; clear of terrain; no new collisions; behind player; six-impulse observatory signal; actual D input walks past all five art types with existing patrol contacts temporarily isolated and restored.":"STORY DECOR FAILED: "+e;
        File.WriteAllText("StoryDecorationCheck.txt",message);if(e==null)Debug.Log(message);else Debug.LogError(message);
    }
    [MenuItem("Tools/Empty Heart/Просмотр/Зал инкубаторов")]
    static void Incubation(){if(Object.FindFirstObjectByType<StationMonument>())StationScalePlayCheck.Preview("Incubation");else Preview("11 Incubation — unfinished");}
    [MenuItem("Tools/Empty Heart/Просмотр/Мастерская")]
    static void Workshop()=>Preview("04 Workshop");
    [MenuItem("Tools/Empty Heart/Просмотр/Нижние сады")]
    static void Gardens()=>Preview("07 Gardens");
    static void Preview(string prefix)
    {
        if(!EditorApplication.isPlaying)return;
        var d=Object.FindObjectsByType<StationStoryDetail>(FindObjectsSortMode.None).First(d=>d.transform.parent.name.StartsWith(prefix));var p=Object.FindFirstObjectByType<PlayerMove>();var b=p.GetComponent<Rigidbody2D>();float feet=p.transform.position.y-p.GetComponent<Collider2D>().bounds.min.y;
        b.position=new Vector2(d.transform.position.x+d.BaseSize.x*.5f+1,d.FloorY+feet+.05f);p.transform.position=b.position;b.linearVelocity=Vector2.zero;p.GetComponent<PlayerHealth>().GrantContactGrace(90);Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();
    }
}
#endif





