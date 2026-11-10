using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMove), typeof(PlayerHealth), typeof(Rigidbody2D))]
public class PlayerRest : MonoBehaviour
{
    public bool IsResting { get; private set; }
    public float Progress => IsResting ? Mathf.Clamp01((Time.time - started) / Duration) : 0;
    public const float Duration = 1.8f;
    public RestCheckpoint Nearby { get; private set; }
    public RestCheckpoint Using { get; private set; }
    private PlayerMove movement;
    private PlayerHealth health;
    private Rigidbody2D body;
    private Animator animator;
    private float started, gravity, noticeUntil;
    private Vector2 from, target;
    private bool wasEnabled;

    private void Awake()
    {
        movement=GetComponent<PlayerMove>(); health=GetComponent<PlayerHealth>();
        body=GetComponent<Rigidbody2D>(); animator=GetComponent<Animator>();
    }
    private void Update()
    {
        if(StationLoreNode.BlocksInput)return;
        Nearby=null;
        if (IsResting)
        {
            if (health.IsRespawning || !Using) { Finish(false); return; }
            body.linearVelocity=Vector2.zero;
            body.position=Vector2.Lerp(from,target,Mathf.SmoothStep(0,1,Progress*5));
            if (Progress>=1) Finish(true);
            return;
        }
        foreach (RestCheckpoint checkpoint in RestCheckpoint.Instances)
            if (checkpoint && checkpoint.CanUse(this) && (!Nearby ||
                Vector2.Distance(transform.position,checkpoint.SpawnPoint)<Vector2.Distance(transform.position,Nearby.SpawnPoint))) Nearby=checkpoint;
        if (Nearby && Keyboard.current!=null && Keyboard.current.eKey.wasPressedThisFrame) TryRest(Nearby);
    }
    public bool CanRest => !MatriarchBoss.AnyCombat && Time.timeScale>0 && !PlayerOrgans.BlocksInput && !PlayerOrgans.IsChanneling && !IsResting && movement && movement.enabled && !movement.IsDashing &&
        !health.IsRespawning && health.CurrentHealth>0 && animator.GetBool("IsGrounded") &&
        !animator.GetBool("IsAttacking") && Mathf.Abs(body.linearVelocity.y)<.25f;
    public bool TryRest(RestCheckpoint checkpoint)
    {
        if (!checkpoint || !checkpoint.CanUse(this)) return false;
        Using=checkpoint; IsResting=true; started=Time.time;
        from=body.position; target=checkpoint.SpawnPoint;
        wasEnabled=movement.enabled; movement.enabled=false;
        gravity=body.gravityScale; body.gravityScale=0; body.linearVelocity=Vector2.zero;
        animator.SetFloat("Speed",0); animator.SetBool("IsGrounded",true);
        health.GrantContactGrace(Duration+.5f);
        return true;
    }
    private void Finish(bool completed)
    {
        if (completed && health.RestAt(Using)) noticeUntil=Time.time+2.5f;
        IsResting=false; body.gravityScale=gravity;
        if (!health.IsRespawning && wasEnabled) movement.enabled=true;
        Using=null;
    }
    private void OnDisable() { if (IsResting) Finish(false); }
    private void OnGUI()
    {
        if(StationAtlas.MapVisible || PlayerOrgans.BlocksInput)return;
        RestCheckpoint checkpoint=IsResting?Using:Nearby;
        if (checkpoint && Camera.main)
        {
            Vector3 screen=Camera.main.WorldToScreenPoint(checkpoint.transform.position+Vector3.up*2);
            if (screen.z>0) using(EmptyHeartUI.Begin())
            {
                float x=Mathf.Clamp(screen.x/EmptyHeartUI.Scale,150,EmptyHeartUI.Width-150);
                float y=Mathf.Clamp((Screen.height-screen.y)/EmptyHeartUI.Scale,170,EmptyHeartUI.Height-90);
                Rect panel=new Rect(x-145,y,290,62);
                EmptyHeartUI.Panel(panel,EmptyHeartUI.Jade);
                EmptyHeartUI.Keycap(new Rect(x-131,y+16,30,28),"E",EmptyHeartUI.Copper);
                EmptyHeartUI.Text(new Rect(x-90,y+5,225,28),IsResting?"СИНХРОНИЗАЦИЯ…":"ОТДОХНУТЬ",16,EmptyHeartUI.Paper,true);
                EmptyHeartUI.Text(new Rect(x-90,y+31,225,23),checkpoint.DisplayName,13,EmptyHeartUI.Muted);
                if (IsResting) EmptyHeartUI.Fill(new Rect(x-133,y+58,266*Progress,2),EmptyHeartUI.Jade);
            }
        }
        if (Time.time<noticeUntil)
            EmptyHeartUI.Announcement("СИНХРОНИЗАЦИЯ ЗАВЕРШЕНА","ОБОЛОЧКА ВОССТАНОВЛЕНА","Точка возрождения обновлена",Mathf.Min(1,(noticeUntil-Time.time)*2),.25f);
    }
}




