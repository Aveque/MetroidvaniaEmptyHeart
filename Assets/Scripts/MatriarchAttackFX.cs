using UnityEngine;

// Painted fragments are reused from the creature's own anatomy. These effects have no hitboxes.
public sealed class MatriarchAttackFX : MonoBehaviour
{
 const int Budget=128;
 sealed class Mote { public SpriteRenderer Art;public Vector2 Velocity;public float Age,Life,Size,Spin;public Color Tint; }
 readonly Mote[] motes=new Mote[Budget];
 MatriarchBoss owner;string lastPose;float lastMotion,chargeClock,kick;int cursor;
 public int Emissions{get;private set;}public int GroundBursts{get;private set;}public int CastBursts{get;private set;}
 public int ActiveMotes{get;private set;}public float Recoil=>kick;
 public Material TrailMaterial{get;private set;}
 public void Initialize(MatriarchBoss boss){owner=boss;TrailMaterial=new Material(Shader.Find("Sprites/Default")){name="Botanical ink trails"};TrailMaterial.mainTexture=Texture2D.whiteTexture;}
 public void Emit(Vector2 at,Vector2 velocity,float size,float life,bool soil=false)
 {
  if(!owner||!owner.InCombat)return;
  int index=-1;for(int i=0;i<Budget;i++){int n=(cursor+i)%Budget;if(motes[n]==null||!motes[n].Art.enabled){index=n;break;}}if(index<0)return;cursor=(index+1)%Budget;
  var m=motes[index];if(m==null){var g=new GameObject("Листва и кора — частица");g.transform.SetParent(transform,false);m=motes[index]=new Mote{Art=g.AddComponent<SpriteRenderer>()};m.Art.sortingOrder=12;}
  m.Art.sprite=soil?owner.Seed:owner.Petal;m.Art.enabled=true;m.Art.transform.position=new Vector3(Mathf.Clamp(at.x,99,131),Mathf.Max(-2.96f,at.y),0);m.Art.transform.rotation=Quaternion.Euler(0,0,Emissions*137.5f);
  m.Velocity=velocity;m.Age=0;m.Life=life;m.Size=size;m.Spin=(Emissions%2==0?1:-1)*(140+Emissions%9*13);
  m.Tint=soil?new Color(.26f,.34f,.28f,.65f):new Color(.84f,.78f,.55f,.65f);m.Art.transform.localScale=new Vector3(1,.65f,1)*(size/m.Art.sprite.bounds.size.y);m.Art.color=new Color(m.Tint.r,m.Tint.g,m.Tint.b,0);Emissions++;
 }
 public void Ground(Vector2 at)
 {
  GroundBursts++;kick=Mathf.Max(kick,.18f);
  for(int i=0;i<14;i++){float a=(22+i*137.5f)*Mathf.Deg2Rad;Vector2 v=new Vector2(Mathf.Cos(a)*2.8f,.7f+Mathf.Abs(Mathf.Sin(a))*3.4f);Emit(at,v,.1f+(i%4)*.045f,.45f+(i%3)*.12f,i%3!=0);}
 }
 public void Cast(Vector2 at,Vector2 direction)
 {
  CastBursts++;kick=Mathf.Min(.34f,kick+.075f);
  for(int i=0;i<4;i++){Vector2 side=new Vector2(-direction.y,direction.x);Emit(at,direction*(1+i*.4f)+side*(i-1.5f)*1.3f,.1f+i*.025f,.3f+i*.04f);}
 }
 public void SeedImpact(Vector2 at){for(int i=0;i<5;i++)Emit(at,new Vector2((i-2)*.8f,.6f+(i%3)*.5f),.09f+i*.015f,.3f+i*.04f,i%2==0);}
 public void Wake(Vector2 at,Vector2 direction)=>Emit(at,-direction*.7f+Vector2.up*.25f,.08f,.25f);
 void Update()
 {
  if(!owner)return;kick=Mathf.MoveTowards(kick,0,Time.deltaTime*1.8f);
  if(!owner.InCombat){Clear();return;}
  string pose=owner.Pose;bool changed=pose!=lastPose||owner.Motion<lastMotion-.3f;
  if(changed){chargeClock=0;if(pose=="VineSweep"||pose=="PincerSweep")kick=.26f;if(pose=="EnragedRoar")Ground(owner.transform.position);}
  if(pose=="SeedPrepare"||pose=="RainPrepare"||pose=="SpiralPrepare"){
   chargeClock-=Time.deltaTime;if(chargeClock<=0){chargeClock=.065f;float angle=Time.time*8;Vector2 center=(Vector2)owner.transform.position+Vector2.up*6.7f;Vector2 radial=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));Emit(center+radial*1.1f,-radial*1.8f,.11f,.45f);}}
  lastPose=pose;lastMotion=owner.Motion;ActiveMotes=0;
  foreach(var m in motes){if(m==null||!m.Art.enabled)continue;m.Age+=Time.deltaTime;if(m.Age>=m.Life){m.Art.enabled=false;continue;}ActiveMotes++;m.Velocity+=Vector2.down*Time.deltaTime*5;Vector3 at=m.Art.transform.position+(Vector3)(m.Velocity*Time.deltaTime);at.x=Mathf.Clamp(at.x,99,131);if(at.y< -2.98f){at.y=-2.98f;m.Velocity*=.7f;m.Velocity.y=Mathf.Abs(m.Velocity.y)*.15f;}m.Art.transform.position=at;m.Art.transform.Rotate(0,0,m.Spin*Time.deltaTime);float p=m.Age/m.Life;float scale=m.Size/m.Art.sprite.bounds.size.y;m.Art.transform.localScale=new Vector3(1+Mathf.Sin(p*9)*.16f,.65f,1)*scale;m.Art.color=new Color(m.Tint.r,m.Tint.g,m.Tint.b,m.Tint.a*Mathf.Sin(Mathf.PI*Mathf.Clamp01(p*1.2f)));}
 }
 public void Clear(){ActiveMotes=0;kick=0;foreach(var m in motes)if(m!=null&&m.Art)m.Art.enabled=false;lastPose=null;}
 void OnDestroy(){if(TrailMaterial)Destroy(TrailMaterial);}
}
