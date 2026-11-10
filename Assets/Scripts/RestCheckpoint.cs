using System.Collections.Generic;
using UnityEngine;

public class RestCheckpoint : MonoBehaviour
{
    public static readonly List<RestCheckpoint> Instances=new List<RestCheckpoint>();
    public string DisplayName="Синаптическая колыбель";
    public Vector2 RestOffset=new Vector2(-.3f,.52f);
    public Vector3 SpawnPoint => transform.position+(Vector3)RestOffset;
    public bool IsActive => playerHealth && playerHealth.ActiveCheckpoint==this;
    private PlayerHealth playerHealth;
    private PlayerRest rest;
    private LineRenderer halo, pulse;
    private Material material;
    private void OnEnable() { if(!Instances.Contains(this)) Instances.Add(this); }
    private void OnDisable() { Instances.Remove(this); }
    private void Start()
    {
        playerHealth=FindFirstObjectByType<PlayerHealth>(); rest=playerHealth?playerHealth.GetComponent<PlayerRest>():null;
        material=new Material(Shader.Find("Sprites/Default")){hideFlags=HideFlags.DontSave};
        halo=Ring("Reactor — status",.025f); pulse=Ring("Synchronization pulse",.018f);
    }
    public bool CanUse(PlayerRest player)
    {
        if (!player.CanRest) return false;
        Vector2 delta=player.transform.position-SpawnPoint;
        if(Mathf.Abs(delta.x)>1.5f || Mathf.Abs(delta.y)>.35f) return false;
        // A station on the other side of a wall must never be interactable.
        var hits=Physics2D.LinecastAll(player.transform.position,SpawnPoint);
        foreach(var hit in hits)
            if(hit.collider && !hit.collider.isTrigger && !hit.collider.GetComponentInParent<PlayerMove>()) return false;
        return true;
    }
    private LineRenderer Ring(string name,float width)
    {
        var go=new GameObject(name); go.transform.SetParent(transform,false);
        var r=go.AddComponent<LineRenderer>();r.sharedMaterial=material;r.useWorldSpace=false;
        r.loop=true;r.positionCount=48;r.widthMultiplier=width;r.sortingOrder=4;return r;
    }
    private void Update()
    {
        bool usingHere=rest && rest.IsResting && rest.Using==this;
        Color tint=IsActive?EmptyHeartUI.Jade:EmptyHeartUI.Copper;
        if(usingHere)tint=Color.Lerp(EmptyHeartUI.Copper,EmptyHeartUI.Jade,rest.Progress);
        Draw(halo,.14f+Mathf.Sin(Time.time*2.5f)*.012f,tint*.85f);
        pulse.enabled=usingHere;
        if(usingHere){float p=Mathf.Repeat(rest.Progress*3,1);Color c=EmptyHeartUI.Jade;c.a=(1-p)*.75f;Draw(pulse,.2f+p*.9f,c);}
    }
    private void Draw(LineRenderer r,float radius,Color tint)
    {
        if(!r)return;r.startColor=r.endColor=tint;
        for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;r.SetPosition(i,new Vector3(.55f+Mathf.Cos(a)*radius,1.1f+Mathf.Sin(a)*radius,0));}
    }
    private void OnDestroy(){if(material)Destroy(material);}
    private void OnDrawGizmosSelected(){Gizmos.color=Color.cyan;Gizmos.DrawWireSphere(SpawnPoint,.3f);}
}
