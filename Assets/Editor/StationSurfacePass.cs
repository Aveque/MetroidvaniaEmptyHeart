#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;

public static class StationSurfacePass
{
    [Serializable]class Segment{public Vector2 a,b;}
    [Serializable]class Surfaces{public Segment[] floors,ceilings;}
    const string Dir="Assets/Art/StationRefinement/";
    static readonly Dictionary<Sprite,Rect> alpha=new Dictionary<Sprite,Rect>();
    public static void Apply()
    {
        if(!File.Exists(Dir+"MountSurfaces.json"))return;
        alpha.Clear();var surfaces=JsonUtility.FromJson<Surfaces>(File.ReadAllText(Dir+"MountSurfaces.json"));
        var rows=new List<string>();
        var decorations=Object.FindObjectsByType<StationAmbientProp>(FindObjectsSortMode.None).Select(p=>p.GetComponent<SpriteRenderer>())
            .Concat(Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(r=>r.name=="Garden circulation organ" || r.name=="Vascular floor growth" || r.GetComponent<StationLoreNode>())).Distinct();
        foreach(var r in decorations)
        {
            if(!r || !r.sprite)continue;
            var prop=r.GetComponent<StationAmbientProp>();bool ceiling=prop && prop.Hanging;
            var rect=Visible(r.sprite);r.transform.localRotation=Quaternion.identity;
            var mount=r.GetComponent<StationDecorMount>();if(!mount)mount=r.gameObject.AddComponent<StationDecorMount>();
            mount.LocalMin=rect.min;mount.LocalMax=rect.max;mount.Ceiling=ceiling;
            Vector2 contact=mount.Contact;Vector2 preferred=contact;
            // Keep floor growth out of the drainage shaft mouth, instead of
            // relocating it onto a ledge several metres down the shaft.
            if(r.name=="Garden circulation organ"){preferred.y=-3;if(preferred.x>46.5f && preferred.x<52.5f)preferred.x=53.5f;}
            float halfWidth=Mathf.Abs(r.transform.lossyScale.x)*rect.width*.32f;
            float best=float.PositiveInfinity,targetY=0,targetX=contact.x;
            foreach(var s in ceiling?surfaces.ceilings:surfaces.floors)
            {
                float lo=Mathf.Min(s.a.x,s.b.x)+halfWidth,hi=Mathf.Max(s.a.x,s.b.x)-halfWidth;if(lo>hi)continue;
                float x=Mathf.Clamp(preferred.x,lo,hi),y=Mathf.Lerp(s.a.y,s.b.y,(x-s.a.x)/(s.b.x-s.a.x));
                float dx=Mathf.Abs(x-preferred.x),dy=Mathf.Abs(y-preferred.y);if(dx>3 || dy>10)continue;
                float score=dy+dx*2;if(score>=best)continue;best=score;targetX=x;targetY=y;
            }
            if(float.IsPositiveInfinity(best))throw new Exception("No supporting surface for "+r.name+" at "+contact);
            r.transform.position+=new Vector3(targetX-contact.x,targetY-contact.y,0);mount.SupportY=targetY;
            EditorUtility.SetDirty(r.transform);EditorUtility.SetDirty(mount);
            rows.Add(r.name+": "+contact+" -> "+mount.Contact+"; "+(ceiling?"ceiling":"floor"));
        }
        if(File.Exists(Dir+"shell-fragment.png"))
        {
            Sprite sprite=StationRefinementPass.Import("shell-fragment",Vector2.one*.5f);
            foreach(var fragment in Object.FindObjectsByType<ShellFragment>(FindObjectsSortMode.None))
            {
                var r=fragment.GetComponent<SpriteRenderer>();r.sprite=sprite;r.color=Color.white;
                r.transform.localScale=Vector3.one*(.65f/sprite.bounds.size.y);
                var collider=fragment.GetComponent<BoxCollider2D>();collider.size=new Vector2(sprite.bounds.size.x,sprite.bounds.size.y)*1.35f;collider.offset=Vector2.zero;
                EditorUtility.SetDirty(r);EditorUtility.SetDirty(collider);EditorUtility.SetDirty(r.transform);
            }
        }
        File.WriteAllText("StationDecorationMounts.txt",string.Join("\n",rows));
    }
    static Rect Visible(Sprite sprite)
    {
        if(alpha.TryGetValue(sprite,out var result))return result;
        var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));var pixels=texture.GetPixels32();Rect rect=sprite.rect;
        int minX=(int)rect.xMax,minY=(int)rect.yMax,maxX=(int)rect.x,maxY=(int)rect.y;
        for(int y=(int)rect.y;y<rect.yMax;y++)for(int x=(int)rect.x;x<rect.xMax;x++)if(pixels[y*texture.width+x].a>=100){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
        Object.DestroyImmediate(texture);Vector2 lo=(new Vector2(minX-rect.x,minY-rect.y)-sprite.pivot)/sprite.pixelsPerUnit,hi=(new Vector2(maxX-rect.x+1,maxY-rect.y+1)-sprite.pivot)/sprite.pixelsPerUnit;
        result=Rect.MinMaxRect(lo.x,lo.y,hi.x,hi.y);alpha[sprite]=result;return result;
    }
}
#endif
