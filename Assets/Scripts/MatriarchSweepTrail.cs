using UnityEngine;

// A short history of the actual animated vine forms the stroke; no canned slash sprite.
public sealed class MatriarchSweepTrail : MonoBehaviour
{
 const int Samples=25,Frames=7;
 readonly Vector3[][] paths=new Vector3[Frames][];readonly float[] times=new float[Frames];int count;
 readonly Vector3[] vertices=new Vector3[(Frames-1)*(Samples-1)*6];readonly Color[] colors=new Color[(Frames-1)*(Samples-1)*6];
 Mesh mesh;MatriarchBoss owner;float next;public int StrokeCount{get;private set;}
 public void Initialize(MatriarchBoss boss,Material material){owner=boss;mesh=new Mesh{name="Vine gesture ink"};mesh.MarkDynamic();for(int i=0;i<Frames;i++)paths[i]=new Vector3[Samples];gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.sortingOrder=7;var indices=new int[vertices.Length];for(int i=0;i<indices.Length;i++)indices[i]=i;mesh.vertices=vertices;mesh.triangles=indices;}
 public void Record(Vector3[] worldPath,bool active)
 {
  if(!mesh)return;if(!owner.InCombat){count=0;System.Array.Clear(vertices,0,vertices.Length);mesh.vertices=vertices;return;}
  if(active&&Time.time>=next){next=Time.time+.018f;for(int i=Frames-1;i>0;i--){System.Array.Copy(paths[i-1],paths[i],Samples);times[i]=times[i-1];}for(int i=0;i<Samples;i++)paths[0][i]=transform.InverseTransformPoint(worldPath[16+i*32/(Samples-1)]);times[0]=Time.time;count=Mathf.Min(Frames,count+1);StrokeCount++;}
  int v=0;for(int f=0;f<Frames-1;f++)for(int i=0;i<Samples-1;i++){
   float age=Time.time-times[f+1];float alpha=f+1<count?Mathf.Clamp01(1-age/.16f)*.22f:0;float taper=Mathf.Sin(Mathf.PI*i/(Samples-1));Color c=new Color(.8f,.78f,.58f,alpha*taper);
   Vector3 a=paths[f][i],b=paths[f][i+1],d=paths[f+1][i],e=paths[f+1][i+1];vertices[v]=a;vertices[v+1]=b;vertices[v+2]=d;vertices[v+3]=b;vertices[v+4]=e;vertices[v+5]=d;for(int k=0;k<6;k++)colors[v+k]=c;v+=6;}
  mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateBounds();
 }
 void OnDestroy(){if(mesh)Destroy(mesh);}
}
