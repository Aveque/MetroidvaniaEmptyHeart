#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class StationScaleDecorPass
{
    [Serializable]class Segment{public Vector2 a,b;}
    [Serializable]class Surfaces{public Segment[] floors;}
    static Transform root;static Surfaces surfaces;static readonly List<string> report=new List<string>();
    public const string RootName="Station Monumental Installations";
    public static void Build()
    {
        var scene=EditorSceneManager.GetActiveScene();if(EditorApplication.isPlaying || scene.name!="StationEcho_Organic")return;
        Directory.CreateDirectory("StoryScaleBackup");
        if(!File.Exists("StoryScaleBackup/StationEcho_Organic.disk.unity"))File.Copy(scene.path,"StoryScaleBackup/StationEcho_Organic.disk.unity");
        EditorSceneManager.SaveScene(scene);
        if(!File.Exists("StoryScaleBackup/StationEcho_Organic.before-scale.unity"))File.Copy(scene.path,"StoryScaleBackup/StationEcho_Organic.before-scale.unity");
        ApplyToOpenScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("MONUMENTAL SCENERY SAVED: room-scale infrastructure and small human-scale service props. See StoryScaleBuild.txt.");
    }
    public static void ApplyToOpenScene()
    {
        var old=GameObject.Find(RootName);if(old)Undo.DestroyObjectImmediate(old);
        root=new GameObject(RootName).transform;Undo.RegisterCreatedObjectUndo(root.gameObject,"Compose monumental installations");
        surfaces=JsonUtility.FromJson<Surfaces>(File.ReadAllText("Assets/Art/StationRefinement/MountSurfaces.json"));report.Clear();
        Monument("Incubation multi-storey battery","incubation-battery",-73.5f,18,8.5f,"11 ",.64f);
        Monument("Pumpworks primary circulation collector","circulation-collector",9,-36,8.8f,"12 ",.59f);
        Monument("Observatory inward listening array","listening-array",40.5f,43,10.8f,"13 ",.58f);
        Monument("Relay sector circulation coupling","circulation-collector",-3,-3,5.8f,"02 ",.40f);
        var story=GameObject.Find("Station Story Decorations");
        if(story)
        {
            foreach(var d in story.GetComponentsInChildren<StationStoryDetail>(true))
            {
                string n=d.transform.parent.name;
                if(n.StartsWith("11 Incubation — unfinished")){Undo.RecordObject(d.transform.parent.gameObject,"Replace small incubation display");d.transform.parent.gameObject.SetActive(false);continue;}
                if(n.StartsWith("07 Gardens"))Resize(d,6.4f);
                if(n.StartsWith("10 Drainage"))Resize(d,4.4f);
                if(n.StartsWith("12 Pumpworks — blood")){Move(d,new Vector2(20,-36));Resize(d,4.2f);}
                if(n.StartsWith("01 Entrance"))Resize(d,.75f);
                if(n.StartsWith("13 Observatory"))Resize(d,.8f);
                if(n.StartsWith("06 Core exit"))Resize(d,.65f);
            }
        }
        // A small receiver is the operator's instrument beside the architectural array.
        var receiver=GameObject.Find("Observatory grounded receiver");
        if(receiver){Undo.RecordObject(receiver.transform,"Move operator receiver beside array");receiver.transform.position=new Vector2(31,43);}
        foreach(var n in new[]{"Observatory receiver activity","Observatory inward signal"})
        {var lamp=GameObject.Find(n);if(lamp){Undo.RecordObject(lamp.transform,"Move receiver lamp");lamp.transform.position=new Vector2(30.6f,44.5f);}}
        Physics2D.SyncTransforms();File.WriteAllText("StoryScaleBuild.txt",string.Join("\n",report));
    }
    static void Monument(string name,string art,float x,float floor,float height,string roomPrefix,float brightness)
    {
        var sprite=StationStoryDecorPass.Import(art);float width=height*sprite.bounds.size.x/sprite.bounds.size.y;
        var floorSegment=surfaces.floors.Where(s=>Mathf.Abs(s.a.y-floor)<.1f && Mathf.Abs(s.a.y-s.b.y)<.01f && Mathf.Abs(s.a.x-s.b.x)>width+.3f)
            .OrderBy(s=>Mathf.Abs(Mathf.Clamp(x,Mathf.Min(s.a.x,s.b.x)+width*.5f+.15f,Mathf.Max(s.a.x,s.b.x)-width*.5f-.15f)-x)).First();
        float fittedX=Mathf.Clamp(x,Mathf.Min(floorSegment.a.x,floorSegment.b.x)+width*.5f+.15f,Mathf.Max(floorSegment.a.x,floorSegment.b.x)-width*.5f-.15f);
        if(Mathf.Abs(fittedX-x)>3)throw new Exception("Foundation does not fit intended room: "+name);
        var room=Object.FindObjectsByType<CameraRoom>(FindObjectsSortMode.None).First(r=>r.name.StartsWith(roomPrefix));
        var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=new Vector2(fittedX,floor);go.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);
        var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=-18;r.color=new Color(brightness*.94f,brightness,brightness*1.02f);
        var monument=go.AddComponent<StationMonument>();monument.Room=room;monument.Size=new Vector2(width,height);monument.FloorY=floor;
        var mount=go.AddComponent<StationDecorMount>();mount.LocalMin=sprite.bounds.min;mount.LocalMax=sprite.bounds.max;mount.SupportY=floor;
        Signal(new Vector2(fittedX,floor+height*.57f),name.Contains("Observatory"),name.Contains("Observatory")?.5f:.34f);
        report.Add(name+" | "+new Vector2(fittedX,floor)+" | "+width.ToString("F2")+" x "+height+" | behind playable terrain, order -18");
    }
    static void Resize(StationStoryDetail d,float height)
    {
        var r=d.GetComponent<SpriteRenderer>();Undo.RecordObjects(new Object[]{d,d.transform},"Vary scenery scale");
        d.transform.localScale*=height/Mathf.Max(.01f,r.bounds.size.y);
        d.transform.position+=Vector3.up*(d.FloorY-r.bounds.min.y);d.BaseSize=r.bounds.size;
        var mount=d.GetComponent<StationDecorMount>();if(mount){Undo.RecordObject(mount,"Anchor resized artwork");mount.UseExplicitContact=true;mount.ExplicitLocalContact=d.transform.InverseTransformPoint(new Vector3(r.bounds.center.x,d.FloorY,0));EditorUtility.SetDirty(mount);}
        foreach(Transform t in d.transform.parent.Cast<Transform>().ToArray())
        {
            if(t.GetComponent<LineRenderer>())Undo.DestroyObjectImmediate(t.gameObject);
            else if(t.GetComponent<StationStoryLight>()){Undo.RecordObject(t,"Move equipment activity light");t.localPosition=new Vector2(0,height*.69f);}
        }
        EditorUtility.SetDirty(d);report.Add(d.transform.parent.name+" | service/living prop height "+height);
    }
    static void Move(StationStoryDetail d,Vector2 position)
    {Undo.RecordObject(d.transform.parent,"Relocate auxiliary filter");d.transform.parent.position=position;}
    static void Signal(Vector2 p,bool sequence,float size)
    {
        var glow=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/StationRefinement/MachineEmission.asset");if(!glow)return;
        var go=new GameObject(sequence?"Architectural receiver six-impulse signal":"Collector pressure pulse");go.transform.SetParent(root,false);go.transform.position=p;go.transform.localScale=Vector3.one*(size/glow.bounds.size.y);var r=go.AddComponent<SpriteRenderer>();r.sprite=glow;r.sortingOrder=-17;r.color=new Color(.35f,.7f,.61f,.5f);go.AddComponent<StationStoryLight>().SignalSequence=sequence;
    }
}
#endif

