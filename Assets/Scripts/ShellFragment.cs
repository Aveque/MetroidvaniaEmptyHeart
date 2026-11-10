using UnityEngine;

public class ShellFragment : MonoBehaviour
{
    public string Id;float messageUntil;Vector3 origin;bool collected;
    void Start(){origin=transform.position;if(StationJourney.Collected.Contains(Id))Destroy(gameObject);}
    void Update(){if(!collected)transform.position=origin+Vector3.up*(Mathf.Sin(Time.time*2)*.09f);else if(Time.time>messageUntil)Destroy(gameObject);}
    void OnTriggerEnter2D(Collider2D c)=>TryCollect(c.GetComponentInParent<PlayerHealth>());
    public bool TryCollect(PlayerHealth p)
    {
        if(!p || p.IsRespawning || collected || !StationJourney.Collected.Add(Id))return false;
        collected=true;StationJourney.Fragments++;GetComponent<Collider2D>().enabled=false;foreach(var r in GetComponentsInChildren<SpriteRenderer>())r.enabled=false;
        if(StationJourney.Fragments==3){p.Configure(p.MaxHealth+1);StationJourney.MaxHealth=p.MaxHealth;}
        messageUntil=Time.time+4;return true;
    }
    void OnGUI(){if(collected && Time.time<messageUntil)EmptyHeartUI.Announcement("ФРАГМЕНТ ОБОЛОЧКИ",StationJourney.Fragments==3?"ОБОЛОЧКА УСИЛЕНА":StationJourney.Fragments+" / 3","Три фрагмента увеличивают запас здоровья на один сегмент.",Mathf.Clamp01(messageUntil-Time.time),.64f);}
}
