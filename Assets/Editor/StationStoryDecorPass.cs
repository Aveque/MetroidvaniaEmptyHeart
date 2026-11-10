#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class StationStoryDecorPass
{
    public const string Dir="Assets/Art/StoryDecor/";
    [Serializable]class Segment{public Vector2 a,b;}
    [Serializable]class Surfaces{public Segment[] floors;}
    static Transform root; static Surfaces surfaces; static readonly List<string> report=new List<string>();
    static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
    static int mask=>1<<LayerMask.NameToLayer("Wall");
    public static void Build()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(EditorApplication.isPlaying || scene.name!="StationEcho_Organic")return;
        string backup="StoryDecorBackup";Directory.CreateDirectory(backup);
        if(!File.Exists(backup+"/StationEcho_Organic.disk.unity"))File.Copy(scene.path,backup+"/StationEcho_Organic.disk.unity");
        // Include the user's current scene edits in the recoverable starting point.
        EditorSceneManager.SaveScene(scene);
        if(!File.Exists(backup+"/StationEcho_Organic.before-decor.unity"))File.Copy(scene.path,backup+"/StationEcho_Organic.before-decor.unity");
        foreach(string name in new[]{"incubation-rack","maintenance-bench","quarantine-station","expedition-remains","living-filter"})sprites[name]=Import(name);
        surfaces=JsonUtility.FromJson<Surfaces>(File.ReadAllText("Assets/Art/StationRefinement/MountSurfaces.json"));
        var old=GameObject.Find("Station Story Decorations");if(old)Undo.DestroyObjectImmediate(old);
        root=new GameObject("Station Story Decorations").transform;Undo.RegisterCreatedObjectUndo(root.gameObject,"Compose story decorations");
        Physics2D.SyncTransforms();report.Clear();
        Place("01 Entrance — abandoned return team","expedition-remains",-26,-3,1.15f,"The last returning crew left its equipment beside the safe route.",.76f);
        Place("02 Relay — maintenance interrupted","maintenance-bench",14,-3,1.85f,"The relay technician left an unfinished shell repair.",.64f);
        Place("03 Gallery — sterile preparation station","quarantine-station",55,20,2.35f,"The operators isolated the impulse components before quarantine.",.72f);
        Place("04 Workshop — disassembled memory shell","maintenance-bench",3,18,2.15f,"An empty ceramic chest waits on the diagnostic bench.",.82f);
        Place("07 Gardens — cultivated circulation","living-filter",40,-3,2.8f,"The gardens were maintained as living filters for the machine's blood.",.78f);
        Place("09 Archive — inspection station","quarantine-station",-27,18,2.25f,"Every returning shell was inspected before access to the memory archive.",.65f);
        Place("10 Drainage — invasive filter","living-filter",40,-19,2.4f,"The filter is still alive although its service shift never returned.",.63f);
        Place("11 Incubation — unfinished cycle","incubation-rack",-72,18,3.35f,"Three connected vessels: empty, occupied, sealed. The cycle has not ended.",.79f);
        Place("11 Incubation — silent second bank","incubation-rack",-87,18,2.7f,"A second bank still receives power from the shared manifold.",.57f);
        Place("12 Pumpworks — blood treatment","living-filter",9,-36,3.0f,"Pressure lines feed the living treatment column beside the pumps.",.68f);
        Place("12 Pumpworks — abandoned service tools","maintenance-bench",46,-36,1.65f,"A repair stopped during the reverse-flow alarm.",.64f);
        Place("13 Observatory — abandoned listeners","expedition-remains",43,43,1.2f,"The listening crew left its equipment without disconnecting the receiver.",.76f);
        Place("14 Sealed annex — batch 09","incubation-rack",-107.0f,22,1.8f,"The sealed batch shares the same feeding circuit as the active incubators.",.61f);
        Place("15 Reservoir — reverse-flow sampling","quarantine-station",64,-33,2.25f,"Sampling apparatus was left running against the backflow.",.70f);
        Place("06 Core exit — missing expedition","expedition-remains",91,-3,1.0f,"The next expedition's equipment remains before the vascular threshold.",.68f);
        // Link the observatory receiver to its recorded six-impulse signal.
        var receiver=GameObject.Find("Observatory grounded receiver");
        if(receiver)Light(root,"Observatory inward signal",(Vector2)receiver.transform.position+new Vector2(-.25f,1.6f),.23f,true);
        if(File.Exists(Dir+"listening-array.png"))StationScaleDecorPass.ApplyToOpenScene();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("StoryDecorationBuild.txt",string.Join("\n",report));Debug.Log("STORY DECOR SAVED: "+report.Count+" coherent floor-mounted compositions. See StoryDecorationBuild.txt.");
    }
    public static Sprite Import(string name)
    {
        string path=Dir+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.alphaIsTransparency=true;i.mipmapEnabled=true;i.filterMode=FilterMode.Trilinear;i.wrapMode=TextureWrapMode.Clamp;i.textureCompression=TextureImporterCompression.Uncompressed;i.isReadable=true;i.maxTextureSize=2048;i.SaveAndReimport();
        string asset=Dir+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Sprite>(asset);if(existing)return existing;
        var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=t.GetPixels32();int x0=t.width,y0=t.height,x1=0,y1=0;
        for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++)if(pixels[y*t.width+x].a>=100){x0=Mathf.Min(x0,x);x1=Mathf.Max(x1,x);y0=Mathf.Min(y0,y);y1=Mathf.Max(y1,y);}
        var s=Sprite.Create(t,new Rect(x0,y0,x1-x0+1,y1-y0+1),new Vector2(.5f,0),100,0,SpriteMeshType.FullRect);s.name=name;AssetDatabase.CreateAsset(s,asset);return s;
    }
    static void Place(string name,string type,float requestedX,float requestedY,float height,string story,float brightness)
    {
        var sprite=sprites[type];float width=height*sprite.bounds.size.x/sprite.bounds.size.y;
        Vector2 chosen=Vector2.zero;float best=float.PositiveInfinity;
        foreach(var s in surfaces.floors)
        {
            if(Mathf.Abs(s.a.y-s.b.y)>.015f || Mathf.Abs(s.a.y-requestedY)>.15f)continue;
            float lo=Mathf.Min(s.a.x,s.b.x)+width*.5f+.15f,hi=Mathf.Max(s.a.x,s.b.x)-width*.5f-.15f;if(lo>hi)continue;
            for(int k=0;k<17;k++)
            {
                float dx=k==0?0:((k+1)/2)*.5f*(k%2==1?1:-1);float x=Mathf.Clamp(requestedX+dx,lo,hi);if(Mathf.Abs(x-requestedX)>4)continue;
                var p=new Vector2(x,s.a.y);
                if(!Clear(p,width,height))continue;
                float score=Mathf.Abs(x-requestedX);if(score>=best)continue;chosen=p;best=score;
            }
        }
        if(float.IsPositiveInfinity(best))throw new Exception("No grounded clear composition for "+name);
        var group=new GameObject(name);group.transform.SetParent(root,false);group.transform.position=chosen;
        var go=new GameObject(type);go.transform.SetParent(group.transform,false);var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=-8;r.color=new Color(brightness*.94f,brightness,brightness*.99f);go.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);
        var detail=go.AddComponent<StationStoryDetail>();detail.Story=story;detail.BaseSize=new Vector2(width,height);detail.FloorY=chosen.y;
        var mount=go.AddComponent<StationDecorMount>();mount.LocalMin=sprite.bounds.min;mount.LocalMax=sprite.bounds.max;mount.SupportY=chosen.y;
        // Each machine's service line terminates at real architecture above it.
        if(type!="expedition-remains")
        {
            Vector2 start=chosen+Vector2.up*(height*.94f);var hit=Physics2D.Raycast(start+Vector2.up*.12f,Vector2.up,12,mask);
            if(hit.collider && hit.distance>.2f)Cable(group.transform,start,hit.point);
            Light(group.transform,"Contained activity",chosen+new Vector2(0,height*.69f),.17f,false);
        }
        report.Add(name+" | "+chosen+" | "+width.ToString("F2")+" x "+height+" | "+story);
    }
    static bool Clear(Vector2 p,float width,float height)
    {
        if(Physics2D.OverlapBox(p+Vector2.up*(height*.5f+.08f),new Vector2(width*.95f,height-.12f),0,mask))return false;
        for(float x=-width*.45f;x<=width*.45f;x+=.3f){var hit=Physics2D.Raycast(p+new Vector2(x,.15f),Vector2.down,.3f,mask);if(!hit.collider || Mathf.Abs(hit.point.y-p.y)>.04f)return false;}
        foreach(var c in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if(!(c.GetComponent<StationLift>() || c.GetComponent<StationLoreNode>() || c.GetComponent<RestCheckpoint>() || c.GetComponent<ShellFragment>() || c.GetComponent<StationPassage>() || c.GetComponent<DashAbilityPickup>()))continue;
            Vector2 q=c.position;if(q.y<p.y-.5f || q.y>p.y+height+1)continue;
            if(Mathf.Abs(q.x-p.x)<width*.5f+1.15f)return false;
        }
        foreach(var d in root.GetComponentsInChildren<StationStoryDetail>())if(Mathf.Abs(d.FloorY-p.y)<.2f && Mathf.Abs(d.transform.position.x-p.x)<(d.BaseSize.x+width)*.5f+.4f)return false;
        return true;
    }
    static void Cable(Transform parent,Vector2 a,Vector2 b)
    {
        var go=new GameObject("Anchored service umbilical");go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=25;line.widthMultiplier=.065f;line.numCapVertices=4;line.numCornerVertices=3;line.sortingOrder=-10;line.sharedMaterial=AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");line.startColor=line.endColor=new Color(.20f,.23f,.23f);
        for(int k=0;k<25;k++){float t=k/24f;var p=Vector2.Lerp(a,b,t);p.x+=Mathf.Sin(t*Mathf.PI)*.18f;line.SetPosition(k,p);}
    }
    static void Light(Transform parent,string name,Vector2 p,float size,bool signal)
    {
        var glow=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/StationRefinement/MachineEmission.asset");if(!glow)return;
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=p;go.transform.localScale=Vector3.one*(size/glow.bounds.size.y);var r=go.AddComponent<SpriteRenderer>();r.sprite=glow;r.sortingOrder=-7;r.color=new Color(.40f,.78f,.65f,.7f);go.AddComponent<StationStoryLight>().SignalSequence=signal;
    }
}
#endif



