using UnityEngine;

// Continuous mesh deformation keeps the painted roots anchored while their tips articulate.
public sealed class MatriarchRig : MonoBehaviour
{
 public MatriarchBoss Owner;
 MatriarchSweepTrail[] strokes=new MatriarchSweepTrail[2];Vector3[][] sweepPath={new Vector3[49],new Vector3[49]};float[] sprayAt=new float[2];
 const int Rows=48,Cols=4;
 Mesh trunk;Mesh[] vines=new Mesh[2];Material bark,vineMaterial;Transform visual;Transform[] petals=new Transform[2];Sprite[] cuts=new Sprite[2];Transform[] barbs=new Transform[4];SpriteRenderer[] barbArt=new SpriteRenderer[4];SpriteRenderer closedArt,coreArt,mawArt;PlayerMove target;Collider2D targetBody;SpriteRenderer[] petalArt=new SpriteRenderer[2];Vector3[][] points=new Vector3[2][];Vector3[] trunkPoints;float width,height=MatriarchBoss.VisualHeight,cut=.55f,flashUntil;
 float[,] jointAngles=new float[2,Rows+1];bool built;public float CrownAngle{get;private set;}public int AnimatedJoints=>built?Rows*3:0;
 public void Flash()=>flashUntil=Time.time+.12f;
 Material MakeMaterial(Texture texture){var m=new Material(Shader.Find("Sprites/Default"));m.mainTexture=texture;return m;}
 Mesh MakeMesh(string label,Transform parent,Material material,int order)
 {
  var g=new GameObject(label);g.transform.SetParent(parent,false);var mesh=new Mesh{name=label};mesh.MarkDynamic();var vertices=new Vector3[(Rows+1)*(Cols+1)];var uv=new Vector2[vertices.Length];var indices=new int[Rows*Cols*6];int k=0;
  for(int r=0;r<=Rows;r++)for(int c=0;c<=Cols;c++)uv[r*(Cols+1)+c]=new Vector2((float)c/Cols,(float)r/Rows);
  for(int r=0;r<Rows;r++)for(int c=0;c<Cols;c++){int a=r*(Cols+1)+c;indices[k++]=a;indices[k++]=a+Cols+1;indices[k++]=a+1;indices[k++]=a+1;indices[k++]=a+Cols+1;indices[k++]=a+Cols+2;}
  mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices;g.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=order;return mesh;
 }
 void Build()
 {
  built=true;target=FindFirstObjectByType<PlayerMove>();targetBody=target?target.GetComponent<Collider2D>():null;visual=new GameObject("Живая анатомия — непрерывная анимация").transform;visual.SetParent(transform,false);visual.localPosition=Vector3.down*.12f;
  width=height*Owner.Plant.rect.width/Owner.Plant.rect.height;bark=MakeMaterial(Owner.Plant.texture);vineMaterial=MakeMaterial(Owner.Vine.texture);trunk=MakeMesh("Укоренённый ствол",visual,bark,5);trunkPoints=trunk.vertices;var uv=trunk.uv;for(int i=0;i<uv.Length;i++)uv[i].y*=cut;trunk.uv=uv;
  for(int j=0;j<2;j++){
   var stroke=new GameObject("След движения лозы");stroke.transform.SetParent(transform,false);strokes[j]=stroke.AddComponent<MatriarchSweepTrail>();strokes[j].Initialize(Owner,Owner.Effects.TrailMaterial);
   vines[j]=MakeMesh(j==0?"Левая лоза":"Правая лоза",visual,vineMaterial,6);points[j]=vines[j].vertices;
   petals[j]=new GameObject(j==0?"Левый лепесток":"Правый лепесток").transform;petals[j].SetParent(visual,false);petals[j].localScale=Vector3.one*(4.25f/Owner.Petal.bounds.size.y);petalArt[j]=petals[j].gameObject.AddComponent<SpriteRenderer>();petalArt[j].sprite=Owner.Petal;petalArt[j].flipX=j==1;petalArt[j].sortingOrder=8;

  }
  for(int i=0;i<4;i++){var g=new GameObject("Венец шипов стадии II");g.transform.SetParent(visual,false);barbs[i]=g.transform;barbArt[i]=g.AddComponent<SpriteRenderer>();barbArt[i].sprite=Owner.Petal;barbArt[i].flipX=i%2==1;barbArt[i].sortingOrder=10;g.transform.localScale=Vector3.one*(1.7f/Owner.Petal.bounds.size.y);}
  var jaw=Resources.Load<Sprite>("Matriarch/EnragedCrown");if(jaw){var g=new GameObject("Хищный венчик стадии II");g.transform.SetParent(visual,false);mawArt=g.AddComponent<SpriteRenderer>();mawArt.sprite=jaw;mawArt.sortingOrder=11;}
  var closedRect=Owner.Plant.rect;closedRect.y+=closedRect.height*cut;closedRect.height*=1-cut;cuts[0]=Sprite.Create(Owner.Plant.texture,closedRect,new Vector2(.5f,0),Owner.Plant.pixelsPerUnit,0,SpriteMeshType.FullRect);
  var closed=new GameObject("Закрытый бутон");closed.transform.SetParent(visual,false);closed.transform.localScale=Vector3.one*(height/Owner.Plant.bounds.size.y);closedArt=closed.AddComponent<SpriteRenderer>();closedArt.sprite=cuts[0];closedArt.sortingOrder=9;
  var core=new GameObject("Пестик — живая сердцевина");core.transform.SetParent(visual,false);core.transform.localScale=Vector3.one*(1.5f/Owner.Seed.bounds.size.y);coreArt=core.AddComponent<SpriteRenderer>();coreArt.sprite=Owner.Seed;coreArt.sortingOrder=7;

 }
 void LateUpdate()
 {
  if(!Owner||!Owner.Plant||!Owner.Vine)return;if(!built)Build();float t=Time.time;bool dying=Owner.Pose=="Death"||Owner.Defeated;float death=Owner.Defeated?1:Owner.Pose=="Death"?Owner.Motion:0;float arrival=Owner.Pose=="Arrival"?Mathf.SmoothStep(0,1,Owner.Motion):1;
  visual.localScale=new Vector3(1,Mathf.Lerp(.7f,1,arrival),1);float kick=Owner.Effects?Owner.Effects.Recoil:0;float bend=kick*(Owner.SweepSide==0?1:-Owner.SweepSide)+Mathf.Sin(t*(1.4f+Owner.Mutation))*Mathf.Lerp(.12f,.25f,Owner.Mutation)+death*.6f;float pulse=1+Mathf.Sin(t*2.2f)*.018f+kick*.08f;float top=height*cut*(1-death*.32f-kick*.07f);
  for(int r=0;r<=Rows;r++){float f=(float)r/Rows;for(int c=0;c<=Cols;c++){int i=r*(Cols+1)+c;trunkPoints[i]=new Vector3(((float)c/Cols-.5f)*width*pulse+bend*f*f,top*f,0);}}
  trunk.vertices=trunkPoints;trunk.RecalculateBounds();CrownAngle=Owner.Crown*38+death*38+Owner.Mutation*18;for(int i=0;i<4;i++){int side=i%2==0?-1:1;barbs[i].localPosition=new Vector3(bend+side*.35f,top-.6f+(i/2)*.5f,0);barbs[i].localRotation=Quaternion.Euler(0,0,-side*(55+i/2*22+Mathf.Sin(t*3+i)*4));barbArt[i].color=new Color(.8f,.67f,.48f,Owner.Mutation*(1-death*.7f));}
  Color tint=Time.time<flashUntil?new Color(1.8f,1.6f,1.3f,1):Color.Lerp(Color.Lerp(Color.white,new Color(.76f,.75f,.64f),Owner.Mutation),new Color(.48f,.58f,.52f,.72f),death);bark.color=vineMaterial.color=tint;float opening=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.4f,Owner.Crown+death*.3f));closedArt.transform.localPosition=new Vector3(bend,top,0);closedArt.color=new Color(tint.r,tint.g,tint.b,tint.a*(1-opening));coreArt.transform.localPosition=new Vector3(bend,top+.65f,0);coreArt.color=Color.Lerp(new Color(.65f,.74f,.56f,opening*tint.a),new Color(1,.38f,.2f,opening*tint.a),Owner.Mutation);
   if(mawArt){mawArt.transform.localPosition=new Vector3(bend,top+.65f,0);float breath=Mathf.Sin(t*4)*.035f;mawArt.transform.localScale=new Vector3(1+breath,1-breath,1)*(3f/mawArt.sprite.bounds.size.y);mawArt.transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*2.3f)*5);mawArt.color=new Color(tint.r,tint.g,tint.b,Owner.Mutation*tint.a);var cc=coreArt.color;cc.a*=1-Owner.Mutation;coreArt.color=cc;}
  for(int j=0;j<2;j++){
   int side=j==0?-1:1;petals[j].localPosition=new Vector3(bend,top-.15f,0);petals[j].localRotation=Quaternion.Euler(0,0,-side*(CrownAngle+Mathf.Sin(t*1.8f)*2));petalArt[j].color=new Color(tint.r,tint.g,tint.b,tint.a*opening);

   Vector2 spine=new Vector2(side*.95f,.7f);float step=6.2f/Rows;
   for(int r=0;r<=Rows;r++){
    float f=(float)r/Rows;float angle=.55f+f*1.15f+Mathf.Sin(t*1.8f+f*3+j)*.12f;
    if((Owner.Pose=="VinePrepare"||Owner.Pose=="PincerPrepare"||Owner.Pose=="PincerRecoil")&&(side==Owner.SweepSide||Owner.SweepSide==0))angle=Mathf.Lerp(angle,.08f+f*.35f,Mathf.SmoothStep(0,1,Owner.Motion));
    if((Owner.Pose=="VineSweep"||Owner.Pose=="PincerSweep")&&(side==Owner.SweepSide||Owner.SweepSide==0))angle=Mathf.Lerp(.08f+f*.35f,1.35f+f*.35f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.48f,Owner.Motion)));
    if((Owner.Pose=="RootPrepare"||Owner.Pose=="RootMarch"))angle=Mathf.Lerp(.65f+f*1.2f,2+f*.6f,Mathf.Sin(Owner.Motion*Mathf.PI));
    if(Owner.Pose=="SeedPrepare"||Owner.Pose=="SeedVolley"||Owner.Pose=="Molting"||Owner.Pose=="EnragedRoar")angle=.3f+f*.6f+Mathf.Sin(t*6)*.05f;
    if(Owner.Pose=="RainPrepare"||Owner.Pose=="SeedRain")angle=.1f+f*.25f+Mathf.Sin(t*5+f*4)*.12f;
    if(Owner.Pose=="SpiralPrepare"||Owner.Pose=="SpiralBurst")angle=.5f+f*1.4f+Mathf.Sin(t*7-f*5)*.4f;
    angle=Mathf.Lerp(angle,2.35f+f*.65f,death);jointAngles[j,r]=Mathf.Lerp(jointAngles[j,r],angle,1-Mathf.Exp(-Time.deltaTime*((Owner.Pose=="VineSweep"||Owner.Pose=="PincerSweep")?35:16)));angle=jointAngles[j,r];Vector2 direction=new Vector2(side*Mathf.Sin(angle),Mathf.Cos(angle));if(r>0)spine+=direction*step;spine.y=Mathf.Max(.12f,spine.y);Vector2 normal=new Vector2(direction.y,-direction.x);float meshWidth=5.1f*Owner.Vine.rect.width/Owner.Vine.rect.height;
    sweepPath[j][r]=visual.TransformPoint(spine);
    for(int c=0;c<=Cols;c++)points[j][r*(Cols+1)+c]=spine+normal*(((float)c/Cols-.5f)*meshWidth);
    if(!dying&&(Owner.Pose=="VineSweep"||Owner.Pose=="PincerSweep")&&(side==Owner.SweepSide||Owner.SweepSide==0)&&Owner.Motion>.18f&&Owner.Motion<.8f&&r>12&&r%3==0){if(targetBody&&targetBody.bounds.SqrDistance(visual.TransformPoint(spine))<.32f*.32f)Owner.HurtPlayer(visual.TransformPoint(spine));}
   }
   bool sweeping=(Owner.Pose=="VineSweep"||Owner.Pose=="PincerSweep")&&(Owner.SweepSide==0||Owner.SweepSide==side)&&Owner.Motion>.1f&&Owner.Motion<.8f;
   strokes[j].Record(sweepPath[j],sweeping);if(sweeping&&Time.time>sprayAt[j]){sprayAt[j]=Time.time+.045f;Owner.Effects.Emit(sweepPath[j][44],new Vector2(side*2,1),.14f,.3f);}
   vines[j].vertices=points[j];vines[j].RecalculateBounds();
  }
 }
 void OnDestroy(){if(trunk)Destroy(trunk);foreach(var m in vines)if(m)Destroy(m);foreach(var s in cuts)if(s)Destroy(s);if(bark)Destroy(bark);if(vineMaterial)Destroy(vineMaterial);}
}
