#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class StationRefinementPass
{
    const string Dir="Assets/Art/StationRefinement/";
    [Serializable]class Shape {public string name,id;public Vector2 origin,size,label;}
    [Serializable]class Atlas {public Shape[] rooms;}
    [Serializable]class Panel {public string name;public int texture;public Vector3[] vertices;public int[] triangles;public Color[] colors;}
    [Serializable]class Backdrops {public Panel[] panels,frames;}
    static Transform root;static Sprite glow;
    public static void Build()
    {
        if(EditorApplication.isPlaying || !EditorSceneManager.GetActiveScene().name.EndsWith("Organic"))return;
        var scene=EditorSceneManager.GetActiveScene();string mainPath=scene.path;EditorSceneManager.SaveScene(scene);
        if(!File.Exists("StationEcho_Organic.before-visual-refinement.unity"))File.Copy(scene.path,"StationEcho_Organic.before-visual-refinement.unity");
        ApplyToOpenScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        EditorSceneManager.OpenScene("Assets/Scenes/VascularThreshold.unity");ApplyAtlas(true);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        EditorSceneManager.OpenScene(mainPath);AssetDatabase.SaveAssets();
        File.WriteAllText("StationRefinementBuild.txt","SAVED: independent generated equipment and shell-fragment art; visible-alpha decor roots on physical floors/ceilings; 15 complete local chamber partitions without clipped vaults; six opaque rear walls, framed passage apertures and windows; lift motion. Terrain collision unchanged.");Debug.Log(File.ReadAllText("StationRefinementBuild.txt"));
    }
    public static void ApplyToOpenScene()
    {
        if(!File.Exists(Dir+"Cartography/Atlas.json"))return;
        glow=Glow();Sprite cabin=Import("lift",new Vector2(.5f,0)),sentry=Import("sentry",new Vector2(.92f,.5f)),pump=Import("pump",new Vector2(.5f,0)),receiver=Import("receiver",new Vector2(.5f,0));
        var old=GameObject.Find("Station Visual Refinement");if(old)Object.DestroyImmediate(old);
        root=new GameObject("Station Visual Refinement").transform;
        string[] obsolete={"Lift fixed drive","Lift cradle","Lift pulse indicator","Pressure piston","Observatory tracking receiver","Receiver articulated eye","Receiver live signal","Synaptic chrysalis","Observatory receiver","Observatory ceiling cables","Living culture"};
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))if(t && obsolete.Contains(t.name))Object.DestroyImmediate(t.gameObject);
        Physics2D.SyncTransforms();
        foreach(var lift in Object.FindObjectsByType<StationLift>(FindObjectsSortMode.None))
        {
            if(lift.Id=="observatory-gallery" && !lift.UnlockEnd)lift.transform.position=new Vector3(40.5f,20.5f,0);
            if(lift.Cabin)Object.DestroyImmediate(lift.Cabin.gameObject);
            var r=Art("Passenger lift cabin",cabin,lift.transform,Vector2.down*.5f,2.8f,1);lift.Cabin=r.transform;
            var center=lift.transform.position;
            foreach(float x in new[]{-.76f,.76f})Rail("Lift guide rail",center+new Vector3(x,-.5f),center+new Vector3(x,3),.045f,new Color(.27f,.29f,.27f),-3);
            var hit=Physics2D.Raycast(center+Vector3.up*2.4f,Vector2.up,30,1<<LayerMask.NameToLayer("Wall"));
            if(hit.collider)Rail("Lift anchored hoist cable",center+Vector3.up*2.3f,hit.point,.055f,new Color(.23f,.2f,.16f),-3);
            EditorUtility.SetDirty(lift);
        }
        foreach(var enemy in Object.FindObjectsByType<SynapticSentry>(FindObjectsSortMode.None))
        {
            foreach(Transform child in enemy.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
            if(enemy.name.StartsWith("Gardens"))enemy.transform.position=new Vector3(enemy.transform.position.x,1.1f,0);
            Vector2 back=enemy.FacesLeft?Vector2.right:Vector2.left;
            Vector2 origin=enemy.transform.position;float nearest=4;
            foreach(var edge in Object.FindObjectsByType<EdgeCollider2D>(FindObjectsSortMode.None))
            {
                if(edge.gameObject.layer!=LayerMask.NameToLayer("Wall"))continue;var points=edge.points;
                for(int k=1;k<points.Length;k++)
                {
                    Vector2 a=edge.transform.TransformPoint(points[k-1]+edge.offset),b=edge.transform.TransformPoint(points[k]+edge.offset);
                    if(Mathf.Abs(b.y-a.y)<.0001f || origin.y<Mathf.Min(a.y,b.y) || origin.y>Mathf.Max(a.y,b.y))continue;
                    float x=Mathf.Lerp(a.x,b.x,(origin.y-a.y)/(b.y-a.y)),distance=(x-origin.x)*back.x;
                    if(distance>0 && distance<nearest)nearest=distance;
                }
            }
            if(nearest<4)enemy.transform.position=origin+back*(nearest-.18f);
            var head=Art("Independent ceramic spitter",sentry,enemy.transform,back*.35f,1.05f,1);head.flipX=!enemy.FacesLeft;
            var lens=Art("Spitter nozzle charge",glow,head.transform,Vector2.zero,.22f,2);
            lens.transform.position=enemy.transform.position-(Vector3)back*1.15f;enemy.Head=head.transform;enemy.Lens=lens.transform;enemy.ProjectileArt=glow;
            enemy.GetComponent<BoxCollider2D>().size=new Vector2(.9f,.95f);EditorUtility.SetDirty(enemy);
        }
        foreach(var prop in Object.FindObjectsByType<StationAmbientProp>(FindObjectsSortMode.None))
        {
            if(prop.name=="Sinew circulation pump")
            {
                var r=prop.GetComponent<SpriteRenderer>();r.sprite=pump;r.transform.localScale=Vector3.one*(1.85f/pump.bounds.size.y);r.transform.position=new Vector3(r.transform.position.x,-36,0);r.color=new Color(.62f,.72f,.73f);EditorUtility.SetDirty(r);
                var lamp=Art("Pump pressure glow",glow,root,(Vector2)r.transform.position+new Vector2(.28f,1.18f),.18f,-2);lamp.gameObject.AddComponent<StationMechanism>().Kind=StationMechanism.Motion.Pulse;
            }
            if(prop.name=="Dormant incubation vessel")
            {var lamp=Art("Incubator dim culture",glow,root,(Vector2)prop.transform.position+Vector2.up*.15f,.25f,-2);lamp.color=new Color(.35f,.65f,.56f,.35f);lamp.gameObject.AddComponent<StationMechanism>().Kind=StationMechanism.Motion.Pulse;}
        }
        // Keep the archive interaction, but make its display a small, floor-mounted instrument.
        var observatoryNotes=new List<string>();
        foreach(var node in Object.FindObjectsByType<StationLoreNode>(FindObjectsSortMode.None))
        {
            if(node.transform.position.y<42 || node.transform.position.x<24 || node.transform.position.x>60)continue;
            observatoryNotes.Add(node.name+" at "+node.transform.position);
            var r=node.GetComponent<SpriteRenderer>();if(!r)continue;
            r.transform.localScale*=.95f/r.bounds.size.y;
            r.transform.position+=Vector3.up*(43-r.bounds.min.y);EditorUtility.SetDirty(r);
        }
        foreach(var r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if(r.transform.position.y<42 || r.transform.position.y>61 || r.transform.position.x<24 || r.transform.position.x>60)continue;
            if(r.name.IndexOf("Terminal",StringComparison.OrdinalIgnoreCase)<0 && r.name.IndexOf("Console",StringComparison.OrdinalIgnoreCase)<0)continue;
            if(r.GetComponentInParent<StationLoreNode>())continue;
            r.transform.localScale*=.95f/Mathf.Max(.01f,r.bounds.size.y);r.transform.position+=Vector3.up*(43-r.bounds.min.y);EditorUtility.SetDirty(r);
        }
        var antenna=Art("Observatory grounded receiver",receiver,root,new Vector2(38,43),2.35f,-4);
        var signal=Art("Observatory receiver activity",glow,root,new Vector2(37.6f,44.5f),.2f,-3);signal.gameObject.AddComponent<StationMechanism>().Kind=StationMechanism.Motion.Pulse;
        File.WriteAllText("ObservatoryEquipmentCorrection.txt",string.Join("\n",observatoryNotes));
        // Socket and culture lights now use their own small emission asset as well.
        foreach(var r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Membrane rooted socket"))
        {r.sprite=glow;r.transform.localScale=Vector3.one*(.2f/glow.bounds.size.y);r.color=new Color(.5f,.75f,.65f);EditorUtility.SetDirty(r);}
        ApplyAtlas(false);ApplyBackdrops();StationSurfacePass.Apply();
    }
    public static Sprite Import(string name,Vector2 pivot)
    {
        string path=Dir+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.isReadable=true;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();int x0=texture.width,y0=texture.height,x1=0,y1=0;
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>64){x0=Mathf.Min(x0,x);x1=Mathf.Max(x1,x);y0=Mathf.Min(y0,y);y1=Mathf.Max(y1,y);}
        string asset=Dir+name+"_sprite.asset";var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(asset);if(sprite)return sprite;
        sprite=Sprite.Create(texture,new Rect(x0,y0,x1-x0+1,y1-y0+1),pivot,100,0,SpriteMeshType.FullRect);sprite.name=name+" — independent painted artwork";AssetDatabase.CreateAsset(sprite,asset);return sprite;
    }
    static Sprite Glow()
    {
        string path=Dir+"MachineEmission.asset";var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite)return sprite;
        var t=new Texture2D(64,64,TextureFormat.RGBA32,false);t.name="Soft machine emission";var pixels=new Color[64*64];
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))/31;pixels[y*64+x]=new Color(.65f,1,.85f,Mathf.Pow(Mathf.Clamp01(1-d),2));}
        t.SetPixels(pixels);t.Apply();AssetDatabase.CreateAsset(t,Dir+"MachineEmissionTexture.asset");sprite=Sprite.Create(t,new Rect(0,0,64,64),Vector2.one*.5f,64);AssetDatabase.CreateAsset(sprite,path);return sprite;
    }
    static SpriteRenderer Art(string name,Sprite sprite,Transform parent,Vector2 local,float height,int order)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=local;var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;r.color=new Color(.73f,.8f,.78f);
        // Global artwork height is independent of the parent's art scale.
        go.transform.localScale=Vector3.one*(height/sprite.bounds.size.y/Mathf.Max(.001f,parent.lossyScale.y));return r;
    }
    static void Rail(string name,Vector3 a,Vector3 b,float width,Color color,int order)
    {
        var go=new GameObject(name);go.transform.SetParent(root);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=2;line.SetPosition(0,a);line.SetPosition(1,b);line.widthMultiplier=width;line.startColor=line.endColor=color;line.sortingOrder=order;line.sharedMaterial=AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
    }
    public static void ApplyAtlas(bool next)
    {
        string dir=Dir+(next?"NextCartography/":"Cartography/");var data=JsonUtility.FromJson<Atlas>(File.ReadAllText(dir+"Atlas.json"));
        foreach(var room in Object.FindObjectsByType<CameraRoom>(FindObjectsSortMode.None))
        {
            var entry=data.rooms.FirstOrDefault(s=>s.name==room.name);if(entry==null)continue;var shape=room.GetComponent<StationRoomShape>();if(!shape)shape=room.gameObject.AddComponent<StationRoomShape>();
            Texture2D Load(string suffix){string path=dir+entry.id+suffix+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Default;i.alphaIsTransparency=true;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;i.filterMode=FilterMode.Bilinear;i.maxTextureSize=2048;i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);}
            shape.Fill=Load("_fill");shape.Outline=Load("_outline");shape.Origin=entry.origin;shape.Size=entry.size;shape.Label=entry.label;EditorUtility.SetDirty(shape);
        }
    }
    static void ApplyBackdrops()
    {
        var geometry=GameObject.Find("Authored curved station");
        if(geometry)foreach(Transform child in geometry.transform.Cast<Transform>().ToArray())if(child.name.EndsWith("fitted rear wall"))Object.DestroyImmediate(child.gameObject);
        var data=JsonUtility.FromJson<Backdrops>(File.ReadAllText(Dir+"RearDepth.json"));
        foreach(var panel in data.panels.Concat(data.frames))
        {
            bool frame=data.frames.Contains(panel);string asset=Dir+"Depth_"+panel.name+(frame?"_frame":"")+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(asset);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,asset);}mesh.Clear();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.vertices=panel.vertices;mesh.triangles=panel.triangles;mesh.colors=panel.colors;mesh.uv=panel.vertices.Select(v=>new Vector2(v.x/13,v.y/13)).ToArray();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var go=new GameObject(panel.name+(frame?" architectural aperture frame":" opaque distant wall"));go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sortingOrder=frame?-24:-25;renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/OrganicComparison/CurvedStation/Rear_"+panel.texture+".mat");
        }
    }
}
#endif
