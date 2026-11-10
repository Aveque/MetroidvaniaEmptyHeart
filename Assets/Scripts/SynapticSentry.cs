using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SynapticSentry : MonoBehaviour,IDamageable
{
    public Transform Head,Lens;public Sprite ProjectileArt;public bool FacesLeft=true;
    public int CurrentHealth{get;private set;}=3;
    public int ShotsFired{get;private set;}
    public bool Charging{get;private set;}
    PlayerMove player;float timer,flash,death;bool dying;Quaternion rest;Vector3 headScale,lensScale;Vector2 aim;
    void Start(){player=FindFirstObjectByType<PlayerMove>();rest=Head.localRotation;headScale=Head.localScale;lensScale=Lens.localScale;timer=1.4f;}
    void Update()
    {
        if(dying){death+=Time.deltaTime;Head.localScale=headScale*Mathf.Max(0,1-death*2);Head.localRotation*=Quaternion.Euler(0,0,Time.deltaTime*90);if(death>.6f)Destroy(gameObject);return;}
        if(!player || StationJourney.Travelling)return;
        Vector2 delta=player.transform.position-Lens.position;
        bool visible=delta.magnitude<10 && Mathf.Sign(delta.x)==(FacesLeft?-1:1) && !player.GetComponent<PlayerHealth>().IsRespawning;
        if(visible){var hit=Physics2D.Linecast(Lens.position,player.transform.position,1<<LayerMask.NameToLayer("Wall"));visible=!hit.collider;}
        float angle=Mathf.Clamp(Mathf.Atan2(delta.y,Mathf.Abs(delta.x))*Mathf.Rad2Deg,-12,12)*(FacesLeft?-1:1);
        Head.localRotation=Quaternion.Slerp(Head.localRotation,rest*Quaternion.Euler(0,0,visible?angle:Mathf.Sin(Time.time)*4),Time.deltaTime*4);
        if(!visible){Charging=false;timer=Mathf.Max(timer,.7f);}else timer-=Time.deltaTime;
        Charging=visible && timer<.85f;
        if(Charging)aim=delta.normalized;
        Lens.localScale=lensScale*(1+(Charging?(1-timer/.85f)*.65f:Mathf.Sin(Time.time*2)*.04f));
        Lens.GetComponent<SpriteRenderer>().color=Charging?Color.Lerp(Color.white,new Color(1,.4f,.12f),1-timer/.85f):new Color(.7f,.9f,.8f);
        Head.localScale=headScale*(1+Mathf.Sin(Time.time*2)*.025f);
        if(visible && timer<=0){Fire();timer=2.6f;Charging=false;Head.localScale=headScale*.88f;}
        foreach(var r in Head.GetComponentsInChildren<SpriteRenderer>())if(r.transform!=Lens)r.color=Time.time<flash?new Color(1,.35f,.25f):new Color(.72f,.78f,.7f);
    }
    void Fire()
    {
        ShotsFired++;var go=new GameObject("Synaptic projectile");go.transform.position=Lens.position+(Vector3)aim*.13f;
        var sr=go.AddComponent<SpriteRenderer>();sr.sprite=ProjectileArt;sr.sortingOrder=6;go.transform.localScale=Vector3.one*(.35f/sr.sprite.bounds.size.y);
        go.AddComponent<SentryProjectile>().Configure(aim*6.5f,player.GetComponent<PlayerHealth>());
    }
    public void TakeDamage(int amount){if(dying || amount<=0)return;CurrentHealth=Mathf.Max(0,CurrentHealth-amount);flash=Time.time+.14f;if(CurrentHealth==0){dying=true;ImpulseResidue.Spawn(transform.position);GetComponent<Collider2D>().enabled=false;}}
}
