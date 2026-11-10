using UnityEngine;

public class SentryProjectile : MonoBehaviour
{
    Vector2 velocity;PlayerHealth target;float age;
    public void Configure(Vector2 speed,PlayerHealth player){velocity=speed;target=player;}
    void FixedUpdate()
    {
        age+=Time.fixedDeltaTime;if(age>3 || !target){Destroy(gameObject);return;}
        Vector2 from=transform.position,to=from+velocity*Time.fixedDeltaTime;
        if(Physics2D.CircleCast(from,.12f,velocity.normalized,Vector2.Distance(from,to),1<<LayerMask.NameToLayer("Wall"))){Destroy(gameObject);return;}
        Bounds swept=new Bounds((from+to)/2,new Vector3(Mathf.Abs(to.x-from.x)+.25f,Mathf.Abs(to.y-from.y)+.25f,4));
        if(swept.Intersects(target.GetComponent<Collider2D>().bounds))
        {if(!target.GetComponent<PlayerMove>().IsDashing)target.TakeContactDamage(1,from);Destroy(gameObject);return;}
        transform.position=to;transform.Rotate(0,0,Time.fixedDeltaTime*140);
    }
}
