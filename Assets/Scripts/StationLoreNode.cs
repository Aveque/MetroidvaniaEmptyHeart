using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-200)]
public sealed class StationLoreNode : MonoBehaviour
{
    public string Title;
    [TextArea(4,16)]public string Message;
    static StationLoreNode active;static int closedFrame=-1;
    PlayerMove player;float oldScale;Vector2 scroll;
    public static bool IsReading=>active;
    public static bool BlocksInput=>active || closedFrame==Time.frameCount;
    public bool IsOpen=>active==this;
    public string ArchiveText=>StationArchiveText.Resolve(Title,Message);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState(){active=null;closedFrame=-1;}
    void Start(){player=FindFirstObjectByType<PlayerMove>();}
    public bool TryOpen()
    {
        if(!player)player=FindFirstObjectByType<PlayerMove>();
        if(PlayerOrgans.BlocksInput || PlayerOrgans.IsChanneling || BlocksInput || !player || Time.timeScale<=0 || StationJourney.Travelling || player.IsAttacking || player.IsDashing)return false;
        var health=player.GetComponent<PlayerHealth>();if(health.IsRespawning || health.CurrentHealth<=0)return false;
        if(Vector2.Distance(player.transform.position,transform.position)>1.9f)return false;
        var rest=player.GetComponent<PlayerRest>();if(rest && rest.IsResting)return false;
        var boss=FindFirstObjectByType<BossArena>();if(boss && boss.InCombat)return false;
        foreach(var hit in Physics2D.LinecastAll(player.transform.position,transform.position))
            if(hit.collider && !hit.collider.isTrigger && hit.collider.gameObject.layer==LayerMask.NameToLayer("Wall"))return false;
        oldScale=Time.timeScale;active=this;scroll=Vector2.zero;Time.timeScale=0;return true;
    }
    void Close(){if(!IsOpen)return;active=null;closedFrame=Time.frameCount;Time.timeScale=oldScale;}
    void OnDisable()=>Close();
    void Update()
    {
        var k=Keyboard.current;if(k==null)return;
        if(IsOpen){if(k.escapeKey.wasPressedThisFrame)Close();return;}
        if(k.eKey.wasPressedThisFrame)TryOpen();
    }
    void OnGUI()
    {
        if(!player)return;
        using(EmptyHeartUI.Begin())
        {
            if(IsOpen)
            {
                GUI.depth=-200;float w=Mathf.Min(820,EmptyHeartUI.Width-72),h=Mathf.Min(670,EmptyHeartUI.Height-90);
                float x=(EmptyHeartUI.Width-w)/2,y=(EmptyHeartUI.Height-h)/2;
                EmptyHeartUI.Fill(new Rect(0,0,EmptyHeartUI.Width,EmptyHeartUI.Height),new Color(.003f,.01f,.015f,.84f));
                EmptyHeartUI.Panel(new Rect(x,y,w,h),EmptyHeartUI.Copper);
                EmptyHeartUI.Text(new Rect(x+32,y+24,w-64,25),"ЭХО / ЛОКАЛЬНЫЙ АРХИВ",13,EmptyHeartUI.Jade);
                EmptyHeartUI.Text(new Rect(x+32,y+62,w-64,60),Title,25,EmptyHeartUI.Paper,true);
                EmptyHeartUI.Ornament(x+w/2,y+137,w-64,EmptyHeartUI.Copper);
                var style=new GUIStyle(GUI.skin.label){font=EmptyHeartUI.BodyFont,fontSize=20,wordWrap=true,richText=false,padding=new RectOffset(0,0,0,0)};
                style.normal.textColor=EmptyHeartUI.Paper;
                Rect viewport=new Rect(x+34,y+161,w-68,h-233);float contentHeight=Mathf.Max(viewport.height,style.CalcHeight(new GUIContent(ArchiveText),viewport.width-25));
                scroll=GUI.BeginScrollView(viewport,scroll,new Rect(0,0,viewport.width-25,contentHeight));
                GUI.Label(new Rect(0,0,viewport.width-25,contentHeight),ArchiveText,style);GUI.EndScrollView();
                EmptyHeartUI.Fill(new Rect(x+32,y+h-57,w-64,1),EmptyHeartUI.Copper);
                EmptyHeartUI.Keycap(new Rect(x+w-182,y+h-44,50,25),"ESC",EmptyHeartUI.Copper);
                EmptyHeartUI.Text(new Rect(x+w-121,y+h-44,90,25),"ЗАКРЫТЬ",13,EmptyHeartUI.Paper);
                if(contentHeight>viewport.height)EmptyHeartUI.Text(new Rect(x+34,y+h-43,w-240,24),"КОЛЕСО МЫШИ — ПРОКРУТКА",12,EmptyHeartUI.Muted);
            }
            else if(!BlocksInput && Time.timeScale>0 && Vector2.Distance(player.transform.position,transform.position)<1.9f && Camera.main)
            {
                Vector3 p=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*1.2f);
                EmptyHeartUI.Interaction("E","ОТКРЫТЬ АРХИВ","Запись терминала • ESC — закрыть",230);
            }
        }
    }
}

