using UnityEngine;

// Every strand keeps its length: joints bend towards the wall instead of scaling/fading away.
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(VascularResonance))]
public sealed class LivingRootLock : MonoBehaviour
{
    public Material TendrilMaterial;
    public int SortingOrder=8;
    const int Rows=72, Columns=4, PerBank=3;
    sealed class Strand
    {
        public GameObject go;public Mesh mesh;public Vector3[] vertices;public Color[] colors;
        public Vector2 anchor,inward,lateral;public float length,width,phase,hand,delay;
    }
    Strand[] strands;VascularResonance resonance;float shown=-1;
    public float VisibleProgress=>shown;
    public int StrandCount=>strands==null?0:strands.Length;
    void OnEnable(){resonance=GetComponent<VascularResonance>();}
    void LateUpdate()
    {
        if(!resonance)resonance=GetComponent<VascularResonance>();
        if(!TendrilMaterial||resonance.Type!=VascularResonance.Kind.RootSeal)return;
        if(strands==null)Build();
        float p=Application.isPlaying?(resonance.Open?1:resonance.Progress):0;
        // Edit preview stays still. Small idle joint motion makes closed roots feel alive.
        float breath=Application.isPlaying&&p<.001f?Mathf.Sin(Time.time*1.3f)*.012f:0;
        for(int i=0;i<strands.Length;i++)Pose(strands[i],p,breath);
        shown=p;
    }
    void Build()
    {
        bool horizontal=resonance.Size.x>resonance.Size.y;
        float span=horizontal?resonance.Size.x:resonance.Size.y;
        Vector2 axis=horizontal?Vector2.right:Vector2.up;
        Vector2 side=new(-axis.y,axis.x);
        strands=new Strand[PerBank*2];
        for(int bank=0;bank<2;bank++)for(int j=0;j<PerBank;j++)
        {
            float sign=bank==0?1:-1;int id=bank*PerBank+j;
            var s=new Strand();strands[id]=s;
            s.anchor=-axis*sign*(span*.5f+.12f)+side*((j-1)*.26f);
            s.inward=axis*sign;s.lateral=side;s.hand=j==1?-1:1;
            s.phase=id*1.73f;s.delay=j*.055f+bank*.025f;
            s.length=span*(.55f+j*.025f);s.width=s.length*(2f/3f)*(.78f+j*.06f);
            s.go=new GameObject("Живой корень "+(id+1));s.go.hideFlags=HideFlags.DontSave;
            s.go.transform.SetParent(transform,false);
            s.mesh=new Mesh{name="Гибкие суставы корня",hideFlags=HideFlags.DontSave};s.mesh.MarkDynamic();
            s.vertices=new Vector3[(Rows+1)*(Columns+1)];s.colors=new Color[s.vertices.Length];
            var uv=new Vector2[s.vertices.Length];var triangles=new int[Rows*Columns*6];
            for(int y=0;y<=Rows;y++)for(int x=0;x<=Columns;x++){int k=y*(Columns+1)+x;uv[k]=new Vector2(x/(float)Columns,y/(float)Rows);s.colors[k]=Color.white;}
            int n=0;for(int y=0;y<Rows;y++)for(int x=0;x<Columns;x++){int k=y*(Columns+1)+x;triangles[n++]=k;triangles[n++]=k+Columns+1;triangles[n++]=k+1;triangles[n++]=k+1;triangles[n++]=k+Columns+1;triangles[n++]=k+Columns+2;}
            s.mesh.vertices=s.vertices;s.mesh.uv=uv;s.mesh.colors=s.colors;s.mesh.triangles=triangles;
            s.go.AddComponent<MeshFilter>().sharedMesh=s.mesh;var renderer=s.go.AddComponent<MeshRenderer>();renderer.sharedMaterial=TendrilMaterial;renderer.sortingOrder=SortingOrder+id%2;
        }
    }
    void Pose(Strand s,float progress,float breath)
    {
        float raw=Mathf.Clamp01((progress-s.delay)/(1-s.delay));
        float wave=raw*raw*(3-2*raw);Vector2 p=s.anchor;
        for(int row=0;row<=Rows;row++)
        {
            float t=row/(float)Rows;
            // Delayed unweaving travels from base to tip; the final pose fans out along the wall.
            float idle=.12f*Mathf.Sin(t*9+s.phase)*(1-wave)+breath;
            float angle=idle+wave*s.hand*(.25f+1.4f*Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.22f))+.18f*Mathf.Sin(t*4+s.phase));
            Vector2 tangent=s.inward*Mathf.Cos(angle)+s.lateral*Mathf.Sin(angle);
            if(row>0)p+=tangent*(s.length/Rows);
            Vector2 normal=new(-tangent.y,tangent.x);
            float pulse=Mathf.Sin(Mathf.PI*raw)*.14f*(1-t);
            Color tint=Color.Lerp(new Color(.85f,.94f,.91f,1),new Color(1,1,.86f,1),pulse);
            for(int x=0;x<=Columns;x++)
            {
                int k=row*(Columns+1)+x;Vector2 v=p+normal*((x/(float)Columns-.5f)*s.width);
                s.vertices[k]=new Vector3(v.x,v.y,0);s.colors[k]=tint;
            }
        }
        s.mesh.vertices=s.vertices;s.mesh.colors=s.colors;s.mesh.RecalculateBounds();
    }
    void OnDisable(){if(strands==null)return;foreach(var s in strands){if(s.go)Release(s.go);if(s.mesh)Release(s.mesh);}strands=null;shown=-1;}
    static void Release(Object obj){if(Application.isPlaying)Destroy(obj);else DestroyImmediate(obj);}
}

