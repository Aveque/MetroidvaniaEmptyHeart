using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DashMembrane : MonoBehaviour
{
    public Sprite Plasma; public Vector2 Size=new(.35f,3);
    PlayerMove player; Collider2D body; BoxCollider2D wall; LivingEnergyRibbon ribbon; bool ignored;float nextHurt;
    public bool Passing=>ignored;
    void Start()
    {
        player=FindFirstObjectByType<PlayerMove>();body=player.GetComponent<Collider2D>();wall=GetComponent<BoxCollider2D>();wall.size=Size;wall.isTrigger=false;
        var go=new GameObject("Pulsing membrane");go.transform.SetParent(transform,false);ribbon=go.AddComponent<LivingEnergyRibbon>();
        var m=new Material(Shader.Find("Sprites/Default"));ribbon.Configure(Plasma,m,new Vector2(.62f,Size.y),5);Destroy(m);ribbon.Tint=new Color(.7f,1,.87f);
    }
    void FixedUpdate()
    {
        if(!body)return;
        bool pass=player.IsDashing;
        // Finish an admitted crossing even if the dash ends while inside the thin seal.
        if(ignored && wall.bounds.Intersects(body.bounds))pass=true;
        if(pass!=ignored){Physics2D.IgnoreCollision(body,wall,pass);ignored=pass;}
        ribbon.Opacity=ignored?.35f:1;
        if(!ignored && Time.time>nextHurt && wall.bounds.SqrDistance(body.bounds.center)<.5f && Mathf.Abs(body.bounds.center.y-transform.position.y)<Size.y/2)
        {player.GetComponent<PlayerHealth>().TakeContactDamage(1,transform.position);nextHurt=Time.time+.9f;}
    }
    void OnDisable(){if(body && wall)Physics2D.IgnoreCollision(body,wall,false);ignored=false;}
}
