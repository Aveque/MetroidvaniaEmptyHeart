using UnityEngine;

public sealed class MatriarchHazard:MonoBehaviour
{
 public MatriarchBoss Owner;
 SpriteRenderer[] buds;SpriteRenderer art;Transform rootPivot;LineRenderer fissure;
 Vector2 velocity;float age,warning,life,nextWake;bool root,erupted;
 public bool Damaging=>root?age>warning+.15f&&age<warning+.55f:true;
 public bool IsRoot=>root;public float Age=>age;public float Warning=>warning;
 public static void Root(MatriarchBoss owner,Sprite sprite,Vector2 at,float delay)
 {
  var h=Make(owner,sprite,at);h.root=true;h.warning=delay;h.life=delay+1.1f;
  h.rootPivot=new GameObject("Укоренённый изгиб").transform;h.rootPivot.SetParent(h.transform,false);h.art.transform.SetParent(h.rootPivot,false);
  h.buds=new SpriteRenderer[3];for(int j=0;j<3;j++){var bud=new GameObject("Набухающий корневой узел");bud.transform.SetParent(h.transform,false);bud.transform.localPosition=new Vector3((j-1)*.32f,.14f,0);h.buds[j]=bud.AddComponent<SpriteRenderer>();h.buds[j].sprite=owner.Seed;bud.transform.localScale=Vector3.one*(.42f/owner.Seed.bounds.size.y);h.buds[j].sortingOrder=9;}
  var crack=new GameObject("Трещина — почва над корнем");crack.transform.SetParent(h.transform,false);h.fissure=crack.AddComponent<LineRenderer>();h.fissure.sharedMaterial=owner.Effects.TrailMaterial;h.fissure.useWorldSpace=false;h.fissure.sortingOrder=8;h.fissure.widthMultiplier=.045f;h.fissure.positionCount=9;h.fissure.startColor=h.fissure.endColor=Color.clear;
  for(int i=0;i<9;i++)h.fissure.SetPosition(i,new Vector3((i-4)*.17f,.045f+(i%2)*.035f,0));
  h.art.transform.localScale=Vector3.one*(2.8f/sprite.bounds.size.y);
 }
 public static void Projectile(MatriarchBoss owner,Sprite sprite,Vector2 at,Vector2 speed)
 {
  var h=Make(owner,sprite,at);h.velocity=speed;h.life=5;h.art.transform.localScale=Vector3.one*(.55f/sprite.bounds.size.y);owner.Effects.Cast(at,speed.normalized);
  var trail=h.gameObject.AddComponent<TrailRenderer>();trail.sharedMaterial=owner.Effects.TrailMaterial;trail.sortingOrder=7;trail.time=.18f;trail.minVertexDistance=.04f;trail.widthMultiplier=.11f;trail.widthCurve=AnimationCurve.EaseInOut(0,1,1,0);trail.numCapVertices=2;
  var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(.78f,.74f,.51f),0),new GradientColorKey(new Color(.28f,.44f,.37f),1)},new[]{new GradientAlphaKey(.5f,0),new GradientAlphaKey(0,1)});trail.colorGradient=gradient;
 }
 static MatriarchHazard Make(MatriarchBoss owner,Sprite sprite,Vector2 at)
 {
  var g=new GameObject("Материнский цветок — корень/семя");var h=g.AddComponent<MatriarchHazard>();h.Owner=owner;g.transform.position=at;var child=new GameObject("Рисунок угрозы");child.transform.SetParent(g.transform,false);h.art=child.AddComponent<SpriteRenderer>();h.art.sprite=sprite;h.art.sortingOrder=8;return h;
 }
 void Update()
 {
  if(!Owner||!Owner.InCombat){Destroy(gameObject);return;}age+=Time.deltaTime;if(age>life){Destroy(gameObject);return;}
  if(root)
  {
   float charge=Mathf.Clamp01(age/Mathf.Max(.01f,warning));
   for(int i=0;i<buds.Length;i++){var bud=buds[i];bud.enabled=age<warning+.12f;bud.transform.localScale=Vector3.one*((.42f+charge*.13f+Mathf.Sin(age*22+i)*.025f)/bud.sprite.bounds.size.y);bud.color=new Color(1.2f,1.05f,.72f,.8f);}
   fissure.startColor=fissure.endColor=new Color(.12f,.19f,.15f,age<warning?charge:Mathf.Clamp01(1-(age-warning)*4));
   if(age<warning&&charge>.5f&&age>nextWake){nextWake=age+.15f;Owner.Effects.Emit(transform.position,new Vector2(Mathf.Sin(age*83)*.6f,.5f),.07f,.24f,true);}
   if(age>=warning&&!erupted){erupted=true;Owner.Effects.Ground(transform.position);}
   float height=age<warning?.3f:age<warning+.18f?Mathf.Lerp(.12f,2.8f,(age-warning)/.18f):age<warning+.55f?2.8f:Mathf.Lerp(2.8f,0,(age-warning-.55f)/.55f);
   art.color=age<warning?new Color(1.15f,1,.65f,.8f):Color.white;
   var scale=art.transform.localScale;scale.y=height/art.sprite.bounds.size.y;scale.x=2.8f/art.sprite.bounds.size.y*(1+Mathf.Sin((age-warning)*27)*.07f);art.transform.localScale=scale;art.transform.localPosition=Vector3.up*(height*.5f);
   float emerge=Mathf.Max(0,age-warning);rootPivot.localRotation=Quaternion.Euler(0,0,age<warning?0:Mathf.Sin(emerge*23)*7*Mathf.Exp(-emerge*4));
  }
  else
  {
   velocity+=Vector2.down*Time.deltaTime*2;transform.position+=(Vector3)(velocity*Time.deltaTime);
   art.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(velocity.y,velocity.x)*Mathf.Rad2Deg-90+Mathf.Sin(age*17)*8);
   art.transform.localScale=new Vector3(1+Mathf.Sin(age*22)*.08f,1.1f,1)*(.55f/art.sprite.bounds.size.y);
   if(age>nextWake){nextWake=age+.09f;Owner.Effects.Wake(transform.position,velocity.normalized);}
   if(transform.position.x<99||transform.position.x>131||transform.position.y< -3||transform.position.y>10){if(transform.position.y< -3)Owner.Effects.SeedImpact(new Vector2(Mathf.Clamp(transform.position.x,99,131),-2.98f));Destroy(gameObject);return;}
  }
  var p=FindFirstObjectByType<PlayerMove>();if(!p||!Damaging)return;Bounds danger=root?new Bounds(transform.position+Vector3.up*1.2f,new Vector3(.8f,2.4f,2)):new Bounds(transform.position,new Vector3(.5f,.5f,2));if(danger.Intersects(p.GetComponent<Collider2D>().bounds))Owner.HurtPlayer(transform.position);
 }
}
