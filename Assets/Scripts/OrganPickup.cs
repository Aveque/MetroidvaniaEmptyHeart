using UnityEngine;
using UnityEngine.InputSystem;

public sealed class OrganPickup : MonoBehaviour
{
    public int Id;Vector3 origin;PlayerMove player;
    public static Sprite Art(int id)=>Resources.Load<Sprite>("Organs/"+new[]{"siphon","reservoir","tendon","myomer","servo","filter"}[id]);
    public static void Spawn(int id,Vector2 position)
    {
        if((PlayerOrgans.Owned&(1<<id))!=0 || GameObject.Find("Organ module "+id))return;
        var go=new GameObject("Organ module "+id);go.transform.position=position;var p=go.AddComponent<OrganPickup>();p.Id=id;
    }
    void Start(){if((PlayerOrgans.Owned&(1<<Id))!=0){Destroy(gameObject);return;}origin=transform.position;player=FindFirstObjectByType<PlayerMove>();var r=gameObject.AddComponent<SpriteRenderer>();r.sprite=Art(Id);r.sortingOrder=5;if(r.sprite)transform.localScale=Vector3.one*(.65f/r.sprite.bounds.size.y);}
    bool Near=>player && Vector2.Distance(player.transform.position,transform.position)<1.4f && !Physics2D.Linecast(player.transform.position,transform.position,1<<LayerMask.NameToLayer("Wall")).collider;
    void Update()
    {
        transform.position=origin+Vector3.up*Mathf.Sin(Time.time*2)*.06f;
        if(Near && Time.timeScale>0 && !StationLoreNode.BlocksInput && !PlayerOrgans.BlocksInput && !PlayerOrgans.IsChanneling && Keyboard.current!=null && Keyboard.current.eKey.wasPressedThisFrame && PlayerOrgans.Acquire(Id))Destroy(gameObject);
    }
    void OnGUI(){if(Near && Time.timeScale>0 && !StationAtlas.MapVisible && !PlayerOrgans.BlocksInput)using(EmptyHeartUI.Begin())EmptyHeartUI.Interaction("E",PlayerOrgans.Names[Id],"Подобрать компонент • установка у колыбели [G]",210);}
}




