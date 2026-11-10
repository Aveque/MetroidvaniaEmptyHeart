using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ImpulseResidue : MonoBehaviour
{
    static readonly List<ImpulseResidue> all=new();SpriteRenderer art;Vector3 origin;
    public static void ClearRegistry()=>all.Clear();
    public static ImpulseResidue Nearest(Vector2 position)=>all.Where(r=>r && Vector2.Distance(position,r.transform.position)<1.8f).OrderBy(r=>Vector2.Distance(position,r.transform.position)).FirstOrDefault();
    public static ImpulseResidue Spawn(Vector2 position)
    {
        var p=FindFirstObjectByType<PlayerMove>();if(!p || !p.GetComponent<PlayerOrgans>())return null;
        var hit=Physics2D.Raycast(position+Vector2.up*.4f,Vector2.down,3,1<<LayerMask.NameToLayer("Wall"));if(hit.collider)position=new Vector2(position.x,hit.point.y+.35f);
        var go=new GameObject("Residual neural impulse");go.transform.position=position;return go.AddComponent<ImpulseResidue>();
    }
    void Awake(){all.Add(this);origin=transform.position;art=gameObject.AddComponent<SpriteRenderer>();art.sprite=Resources.Load<Sprite>("Organs/impulse");art.sortingOrder=6;if(art.sprite)transform.localScale=Vector3.one*(.48f/art.sprite.bounds.size.y);}
    void Update(){transform.position=origin+Vector3.up*(Mathf.Sin(Time.time*2.3f)*.07f);art.color=Color.Lerp(new Color(.25f,.65f,.55f,.65f),new Color(.7f,1,.83f),.5f+.5f*Mathf.Sin(Time.time*3));}
    public void Consume(){all.Remove(this);Destroy(gameObject);}
    void OnDestroy()=>all.Remove(this);
}
