using UnityEngine;

// Boss energy has no solid collider: a timed Dash phases through it.
public class BossHazard : MonoBehaviour
{
    private PlayerHealth target;
    private Vector2 size;
    private Vector2 velocity;
    private float expires;
    private bool damaging;
    private bool dashPassable;
    private Vector3 previous;
    public bool DashPassable => dashPassable;
    public bool Damaging => damaging;
    public BiomechBoss CoreCircuitOwner { get; set; }
    private bool circuitBroken;
    private Bounds? confinement;
    public Vector2 HitSize => size;
    public void ConfineTo(Bounds bounds) { confinement = bounds; FitToRoom(); }
    // Both ends follow the physical room, including the curved vault. Sample
    // the beam's whole width so its outer edge cannot protrude through the roof.
    private void FitToRoom()
    {
        if(!confinement.HasValue)return;
        int mask=1<<LayerMask.NameToLayer("Wall");float floor=-100,roof=100;
        for(int i=-1;i<=1;i++)
        {
            Vector2 origin=new Vector2(transform.position.x+i*size.x*.5f,-1.5f);
            var down=Physics2D.Raycast(origin,Vector2.down,20,mask);var up=Physics2D.Raycast(origin,Vector2.up,25,mask);
            if(!down.collider || !up.collider)return;
            floor=Mathf.Max(floor,down.point.y+.04f);roof=Mathf.Min(roof,up.point.y-.04f);
        }
        if(roof-floor<.1f)return;
        size.y=roof-floor;var p=transform.position;p.y=(floor+roof)*.5f;transform.position=p;
        var visual=GetComponent<BossAttackIndicator>();if(visual)visual.SetExtent(size);
    }

    public void Configure(PlayerHealth player, Vector2 hitSize, Vector2 speed, float lifetime, bool dealsDamage, bool canDashThrough)
    {
        target = player; size = hitSize; velocity = speed;
        expires = Time.time + lifetime; damaging = dealsDamage; dashPassable = canDashThrough;
        previous = transform.position;
    }

    private void FixedUpdate()
    {
        if (Time.time >= expires) { Destroy(gameObject); return; }
        previous = transform.position;
        Vector3 next = transform.position + (Vector3)(velocity * Time.fixedDeltaTime);
        if (confinement.HasValue)
        {
            Bounds bounds = confinement.Value;
            float min = bounds.min.x + size.x / 2, max = bounds.max.x - size.x / 2;
            if (next.x < min || next.x > max)
            {
                transform.position = new Vector3(Mathf.Clamp(next.x, min, max), next.y, next.z);
                Destroy(gameObject); return;
            }
        }
        transform.position = next;
        FitToRoom();
        if (!damaging || target == null || target.CurrentHealth <= 0) return;
        PlayerMove player = target.GetComponent<PlayerMove>();
        Vector3 delta = transform.position - previous;
        Bounds swept = new Bounds((transform.position + previous) * 0.5f,
            new Vector3(size.x + Mathf.Abs(delta.x), size.y + Mathf.Abs(delta.y), 10f));
        if (!swept.Intersects(target.GetComponent<Collider2D>().bounds)) return;
        if (dashPassable && player != null && player.IsDashing)
        {
            if (!circuitBroken && CoreCircuitOwner != null)
            {
                circuitBroken = true;
                CoreCircuitOwner.BreakCoreCircuit();
            }
            return;
        }
        if (swept.Intersects(target.GetComponent<Collider2D>().bounds))
            target.TakeContactDamage(1, transform.position);
    }
}
