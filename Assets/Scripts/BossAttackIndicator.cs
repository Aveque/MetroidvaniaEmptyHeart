using System.Collections.Generic;
using UnityEngine;

// Painted effects only. BossHazard owns the unchanged damage bounds.
public sealed class BossAttackIndicator : MonoBehaviour
{
    private readonly List<SpriteRenderer> visuals=new List<SpriteRenderer>();
    private readonly List<Material> materials=new List<Material>();
    private Vector2 size;
    private float born,duration,direction;
    private bool damage,beam,roots;
    private Sprite[] art;
    private SpriteRenderer source,main,bed;
    private SpriteRenderer ceiling;
    private LivingEnergyRibbon ribbon;
    private readonly List<SpriteRenderer> sparks = new List<SpriteRenderer>();
    public bool IsWarning => !damage;
    public float Progress => Mathf.Clamp01((Time.time-born)/duration);
    public bool Painted => art!=null && art.Length==4 && main;
    public Vector2 VisualExtent => size;
    public void SetExtent(Vector2 value){size=value;if(ribbon)ribbon.SetExtent(value);}

    public void Configure(Vector2 extent,float seconds,bool damaging,bool dash,float travelDirection,Sprite[] effects=null)
    {
        size=extent;duration=Mathf.Max(.01f,seconds);born=Time.time;damage=damaging;direction=travelDirection;
        beam=size.y>3 && size.x<1;roots=!damage && size.x<4 && size.y<1;
        source=GetComponent<SpriteRenderer>();art=effects;
        if(art==null || art.Length!=4)return;
        source.enabled=false;transform.localScale=Vector3.one;
        if(beam)
        {
            main=Part("Living plasma filament",0,10); main.enabled=false;
            ribbon=new GameObject("Deforming plasma stream").AddComponent<LivingEnergyRibbon>();
            ribbon.transform.SetParent(transform,false);
            ribbon.Configure(art[0],source.sharedMaterial,size,10);
            bed=Part("Floor discharge roots",2,10);ceiling=Part("Ceiling discharge roots",2,10);
            for(int i=0;i<5;i++)sparks.Add(Part("Travelling plasma knot",0,11));
        }
        else if(roots || damage)
        {
            bed=Part("Agitated tendon bed",2,9);
            main=Part("Chitin root spears",1,10);
        }
        else
        {
            main=Part("Sickle ground wash",2,9);
            for(int i=1;i<6;i++)Part("Charge lane tendons",2,9);
        }
        LateUpdate();
    }
    private SpriteRenderer Part(string name,int index,int order)
    {
        var go=new GameObject(name);go.transform.SetParent(transform,false);
        var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=art[index];renderer.sortingOrder=order;
        var material=new Material(source.sharedMaterial){hideFlags=HideFlags.DontSave,name="Painted boss effect"};
        material.mainTexture=art[index].texture;renderer.sharedMaterial=material;materials.Add(material);visuals.Add(renderer);
        return renderer;
    }
    private static void Pose(SpriteRenderer renderer,Vector2 position,Vector2 extent,float alpha,float angle=0)
    {
        renderer.transform.localPosition=position;renderer.transform.localRotation=Quaternion.Euler(0,0,angle);
        // Ignore the 24px transparent import gutters when fitting the visible silhouette.
        Vector2 content=renderer.sprite.bounds.size-(Vector3)(Vector2.one*(48f/renderer.sprite.pixelsPerUnit));
        renderer.transform.localScale=new Vector3(extent.x/content.x,extent.y/content.y,1);
        renderer.color=new Color(1,1,1,Mathf.Clamp01(alpha));
    }
    private void LateUpdate()
    {
        if(!Painted)return;
        float p=Progress,age=Time.time-born;
        if(beam)
        {
            float width=damage?size.x:Mathf.Lerp(.12f,.4f,p*p);
            ribbon.transform.localScale=new Vector3(width/size.x,1,1);
            ribbon.Opacity=damage?1:.35f+p*.55f;
            float flare=.14f+.06f*Mathf.Sin(age*23);
            Pose(bed,new Vector2(0,-size.y/2+flare/2),new Vector2(width,flare),ribbon.Opacity);
            Pose(ceiling,new Vector2(0,size.y/2-flare/2),new Vector2(width,flare),ribbon.Opacity,180);
            for(int i=0;i<sparks.Count;i++)
            {
                float flow=Mathf.Repeat(age*(damage?.9f:.45f)+i/(float)sparks.Count,1);
                float alpha=Mathf.Sin(flow*Mathf.PI)*ribbon.Opacity;
                Pose(sparks[i],new Vector2(Mathf.Sin(age*14+i*2)*width*.25f,
                    Mathf.Lerp(-size.y/2+.45f,size.y/2-.45f,flow)),new Vector2(width*.26f,.45f),alpha);
            }
        }
        else if(roots)
        {
            float floor=-.2f;
            Pose(bed,new Vector2(0,floor+.18f),new Vector2(size.x,.36f+Mathf.Sin(age*19)*.035f+p*.14f),.65f+p*.35f);
            float rise=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,1,p));
            float height=.1f+rise*1.45f;
            Pose(main,new Vector2(0,floor+height/2),new Vector2(size.x,height),rise*.85f);
        }
        else if(damage)
        {
            float floor=-size.y/2;
            float height=size.y*(.85f+.15f*Mathf.SmoothStep(0,1,p/.06f));
            Pose(main,new Vector2(0,floor+height/2),new Vector2(size.x,height),1);
            Pose(bed,new Vector2(0,floor+.22f),new Vector2(size.x*1.05f,.55f),1);
        }
        else
        {
            for(int i=0;i<visuals.Count;i++)
            {
                float x=-size.x/2+size.x*(i+.5f)/visuals.Count;
                float surge=.5f+.5f*Mathf.Sin(age*10-i*direction*.8f);
                Pose(visuals[i],new Vector2(x,-.24f),new Vector2(size.x/visuals.Count*1.06f,.18f+p*.23f+surge*.06f),.4f+p*.45f);
                visuals[i].flipX=direction<0;
            }
        }
    }
    private void OnDestroy(){foreach(var material in materials)if(material)Destroy(material);}
}
