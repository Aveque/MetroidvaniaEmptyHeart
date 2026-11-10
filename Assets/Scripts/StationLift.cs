using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class StationLift : MonoBehaviour
{
    public string Id,DestinationName;public StationLift Other;public bool UnlockEnd;
    public Transform Cabin;
    PlayerMove player;bool riding;float progress;Vector3 rest;Vector3 cabinRest;
    public float TravelProgress=>progress;
    public bool IsRiding=>riding;
    public bool Available=>StationJourney.Opened.Contains(Id);
    void Start(){player=FindFirstObjectByType<PlayerMove>();rest=transform.position;if(Cabin)cabinRest=Cabin.position;}
    public bool TryUse(PlayerMove p)
    {
        if(Time.timeScale<=0 || StationLoreNode.BlocksInput || PlayerOrgans.BlocksInput || PlayerOrgans.IsChanneling)return false;
        if(!p || riding || StationJourney.Travelling || Vector2.Distance(p.transform.position,transform.position)>1.7f)return false;
        if(!Available && !UnlockEnd)return false;
        if(p.GetComponent<PlayerHealth>().IsRespawning || p.IsAttacking || p.IsDashing)return false;
        StationJourney.Opened.Add(Id);StartCoroutine(Ride(p));return true;
    }
    IEnumerator Ride(PlayerMove p)
    {
        riding=true;StationJourney.Travelling=true;var body=p.GetComponent<Rigidbody2D>();float gravity=body.gravityScale;
        p.GetComponent<PlayerHealth>().GrantContactGrace(2.5f);
        p.enabled=false;body.linearVelocity=Vector2.zero;body.gravityScale=0;Vector2 start=body.position;bool simulated=body.simulated;body.simulated=false;
        float direction=Mathf.Sign(Other.transform.position.y-rest.y);
        for(float t=0;t<.7f;t+=Time.deltaTime)
        {
            float ride=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.2f,.7f,t));progress=ride*.9f;
            Vector2 position=Vector2.Lerp(start,rest,Mathf.Clamp01(t/.2f))+Vector2.up*(direction*ride*.9f);
            body.position=position;p.transform.position=position;if(Cabin)Cabin.position=cabinRest+Vector3.up*(direction*ride*.9f);yield return null;
        }
        progress=1;yield return new WaitForSeconds(.12f);if(Cabin)Cabin.position=cabinRest;
        body.position=Other.transform.position+(Vector3)Vector2.up*(-direction*.9f);p.transform.position=body.position;Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();
        for(float t=0;t<.5f;t+=Time.deltaTime)
        {
            float offset=1-Mathf.SmoothStep(0,1,t/.5f);progress=offset;
            Vector3 movement=Vector3.up*(-direction*offset*.9f);body.position=Other.transform.position+movement;p.transform.position=body.position;
            if(Other.Cabin)Other.Cabin.position=Other.cabinRest+movement;yield return null;
        }
        if(Other.Cabin)Other.Cabin.position=Other.cabinRest;body.position=Other.transform.position;p.transform.position=body.position;
        body.simulated=simulated;body.gravityScale=gravity;p.enabled=true;p.GetComponent<PlayerHealth>().GrantContactGrace(1);progress=0;
        StationJourney.Travelling=false;riding=false;
    }
    void Update(){if(player && Keyboard.current!=null && Keyboard.current.eKey.wasPressedThisFrame)TryUse(player);}
    void OnGUI()
    {
        if(!player || StationAtlas.MapVisible || PlayerOrgans.BlocksInput)return;
        using(EmptyHeartUI.Begin())
        {
            if(riding){EmptyHeartUI.Fill(new Rect(0,0,EmptyHeartUI.Width,EmptyHeartUI.Height),new Color(.005f,.015f,.018f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,1,progress))));}
            else if(Vector2.Distance(player.transform.position,transform.position)<1.7f)
            {Vector3 s=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*1.5f);EmptyHeartUI.Interaction("E",Available||UnlockEnd?"ПОДЪЁМНИК — "+DestinationName:"ПРИВОД ОТКЛЮЧЁН",Available||UnlockEnd?"Поездка по обратному пути":"Открывается с противоположной стороны",230);}
        }
    }
}



