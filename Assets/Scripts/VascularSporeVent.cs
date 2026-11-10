using UnityEngine;
// The closed bud brightens before releasing its spore cloud; safe gaps last half the cycle.
public sealed class VascularSporeVent : MonoBehaviour, IDamageable
{
 public float Phase;public float Radius=1.35f;public int Health=3;float nextHurt;SpriteRenderer glow;Vector3 baseScale;bool dead;
 public bool Releasing=>!dead && Mathf.Repeat(Time.time+Phase,5)>3;
 void Start(){var g=new GameObject("Предупреждение спор");g.transform.SetParent(transform,false);glow=g.AddComponent<SpriteRenderer>();glow.sprite=Resources.Load<Sprite>("Organs/impulse");glow.sortingOrder=3;baseScale=Vector3.one*(Radius*2/glow.sprite.bounds.size.y);g.transform.localScale=baseScale;}
 void Update(){if(dead)return;float t=Mathf.Repeat(Time.time+Phase,5);glow.color=Releasing?new Color(.65f,.8f,.18f,.5f):new Color(.8f,.7f,.2f,Mathf.InverseLerp(1.8f,3,t)*.32f);glow.transform.localScale=baseScale*(Releasing?1:.4f+.6f*Mathf.InverseLerp(1.8f,3,t));var p=PlayerOrgans.Instance;if(Releasing && p && Time.time>nextHurt && Vector2.Distance(p.transform.position,transform.position)<Radius){p.GetComponent<PlayerHealth>().TakeContactDamage(1,transform.position);nextHurt=Time.time+.8f;}}
 public void TakeDamage(int n){if(dead || n<=0)return;Health-=n;if(Health>0)return;dead=true;ImpulseResidue.Spawn(transform.position);foreach(var r in GetComponentsInChildren<SpriteRenderer>())r.enabled=false;foreach(var c in GetComponents<Collider2D>())c.enabled=false;}
}
