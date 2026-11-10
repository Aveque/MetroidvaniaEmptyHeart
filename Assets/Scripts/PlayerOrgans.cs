using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-150)]
public sealed class PlayerOrgans : MonoBehaviour
{
    public static readonly string[] Names={"НЕРВНЫЙ СИФОН","РЕГЕНЕРАТИВНЫЙ МЕШОК","ИМПУЛЬСНОЕ СУХОЖИЛИЕ","СИЛОВОЙ МИОМЕР","ХОДОВОЙ ПРИВОД","РЕЗОНАНСНЫЙ ФИЛЬТР"};
    public static readonly string[] Details={"Поглощение: 0,7 с вместо 1,4 с. Нервные петли быстрее замыкают чужой импульс.","Поглощение восстанавливает 2 HP вместо 1. Выращенный резервуар удерживает дополнительную порцию проводящей жидкости.","Перезарядка Dash короче на 30%. Сухожилие возвращает часть энергии в привод. Сам Dash по-прежнему нужно найти.","Меч наносит на 1 единицу больше урона. Дублирующие волокна усиливают сокращение руки.","Скорость бега выше на 15%. Дополнительная тяга включается только на земле и в воздухе при обычном движении.","Поглощение быстрее на 25%. Совместим с нервным сифоном; занимает отдельное гнездо."};
    public static int Owned{get;private set;} public static int Equipped{get;private set;}
    public static PlayerOrgans Instance{get;private set;}
    static int closedFrame=-1;
    public static bool BlocksInput=>(Instance && Instance.MenuOpen)||closedFrame==Time.frameCount;
    public static bool IsChanneling=>Instance && Instance.Channeling;
    public static bool Has(int id)=>(Equipped&(1<<id))!=0;
    public static float DashCooldownMultiplier=>Has(2)?.7f:1;
    public bool MenuOpen{get;private set;} public bool Channeling=>target;
    public float Progress{get;private set;}
    public ImpulseResidue Target=>target;
    public float AbsorbDuration=>(Has(0)?.7f:1.4f)*(Has(5)?.75f:1);
    PlayerMove move;PlayerHealth health;Rigidbody2D body;Animator animator;ImpulseResidue target;
    RestCheckpoint nearby;float oldScale,noticeUntil;string notice;int damageAtStart;
    LineRenderer thread;Material material;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){Owned=Equipped=0;Instance=null;closedFrame=-1;SceneManager.sceneLoaded-=Loaded;ImpulseResidue.ClearRegistry();}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){SceneManager.sceneLoaded+=Loaded;Attach();}
    static void Loaded(Scene scene,LoadSceneMode mode)=>Attach();
    static void Attach()
    {
        var p=FindFirstObjectByType<PlayerMove>();if(!p)return;if(!p.GetComponent<PlayerOrgans>())p.gameObject.AddComponent<PlayerOrgans>();
        if(p.gameObject.scene.name!="StationEcho_Organic")return;
        OrganPickup.Spawn(0,new Vector2(7,18.65f));OrganPickup.Spawn(1,new Vector2(45,-2.35f));OrganPickup.Spawn(2,new Vector2(40,-35.35f));
    }
    void Awake(){Instance=this;move=GetComponent<PlayerMove>();health=GetComponent<PlayerHealth>();body=GetComponent<Rigidbody2D>();animator=GetComponent<Animator>();}
    public static bool Acquire(int id)
    {if(id<0 || id>=Names.Length || (Owned&(1<<id))!=0)return false;Owned|=1<<id;if(Instance)Instance.Notice(Names[id]+" — найден. Установи у колыбели [G].");return true;}
    void Notice(string s){notice=s;noticeUntil=Time.unscaledTime+3.5f;}
    public bool ToggleOrgan(int id)
    {
        if(!MenuOpen || id<0 || id>=Names.Length || (Owned&(1<<id))==0)return false;
        if(Has(id)){Equipped&=~(1<<id);return true;}
        if(Enumerable.Range(0,Names.Length).Count(Has)>=2){Notice("Оба гнезда заняты. Сначала извлеки один орган.");return false;}
        Equipped|=1<<id;return true;
    }
    bool Ready=>move.enabled && !move.IsAttacking && !move.IsDashing && !health.IsRespawning && health.CurrentHealth>0 && !StationJourney.Travelling && Time.timeScale>0 && !StationLoreNode.BlocksInput && !(GetComponent<PlayerRest>()?.IsResting??false);
    public bool TryOpenMenu()
    {
        nearby=RestCheckpoint.Instances.FirstOrDefault(c=>c && Vector2.Distance(c.SpawnPoint,transform.position)<1.8f && Clear(c.SpawnPoint));
        var boss=FindFirstObjectByType<BossArena>();
        if(MenuOpen || Channeling || !nearby || !Ready || !animator.GetBool("IsGrounded") || (boss && boss.InCombat) || MatriarchBoss.AnyCombat)return false;
        oldScale=Time.timeScale;body.linearVelocity=Vector2.zero;MenuOpen=true;Time.timeScale=0;return true;
    }
    public void CloseMenu(){if(!MenuOpen)return;MenuOpen=false;noticeUntil=0;closedFrame=Time.frameCount;Time.timeScale=oldScale;}
    bool Clear(Vector2 to)=>!Physics2D.Linecast(transform.position,to,1<<LayerMask.NameToLayer("Wall")).collider;
    public bool TryBegin(ImpulseResidue residue)
    {
        if(!residue || !Ready || MenuOpen || !animator.GetBool("IsGrounded") || health.CurrentHealth>=health.MaxHealth || Vector2.Distance(residue.transform.position,transform.position)>1.8f || !Clear(residue.transform.position))return false;
        target=residue;damageAtStart=health.DamageVersion;Progress=0;body.linearVelocity=Vector2.zero;return true;
    }
    public void Cancel(){target=null;Progress=0;if(thread)thread.enabled=false;}
    void Update()
    {
        var k=Keyboard.current;
        if(MenuOpen){if(k!=null){if(k.escapeKey.wasPressedThisFrame)CloseMenu();else for(int i=0;i<Names.Length;i++)if(k[(Key)((int)Key.Digit1+i)].wasPressedThisFrame)ToggleOrgan(i);}return;}
        if(Channeling)
        {
            bool input=k!=null && k.fKey.isPressed && !k.aKey.isPressed && !k.dKey.isPressed && !k.wKey.isPressed && !k.sKey.isPressed && !k.spaceKey.isPressed && !k.leftShiftKey.isPressed && !(Mouse.current?.leftButton.isPressed??false);
            if(!input || !Ready || health.DamageVersion!=damageAtStart || !animator.GetBool("IsGrounded") || Vector2.Distance(target.transform.position,transform.position)>1.8f || !Clear(target.transform.position)){Cancel();return;}
            body.linearVelocity=Vector2.zero;Progress+=Time.deltaTime/AbsorbDuration;DrawThread();
            if(Progress>=1){var residue=target;Cancel();int healed=health.Heal(Has(1)?2:1);if(healed>0){residue.Consume();Notice("ИМПУЛЬС ПРИНЯТ  +"+healed+" HP");}}return;
        }
        if(!Ready)return;
        nearby=RestCheckpoint.Instances.FirstOrDefault(c=>c && Vector2.Distance(c.SpawnPoint,transform.position)<1.8f && Clear(c.SpawnPoint));
        if(k!=null && k.gKey.wasPressedThisFrame){TryOpenMenu();return;}
        if(k!=null && k.fKey.isPressed && !k.aKey.isPressed && !k.dKey.isPressed)
        {var residue=ImpulseResidue.Nearest(transform.position);if(residue)TryBegin(residue);}
    }
    void DrawThread()
    {
        if(!thread){material=new Material(Shader.Find("Sprites/Default"));thread=new GameObject("Absorbed impulse filament").AddComponent<LineRenderer>();thread.transform.SetParent(transform);thread.sharedMaterial=material;thread.positionCount=24;thread.widthMultiplier=.025f;thread.sortingOrder=8;thread.numCapVertices=3;}
        thread.enabled=true;thread.startColor=new Color(.3f,.7f,.65f,.8f);thread.endColor=new Color(.7f,1,.8f,.9f);
        Vector3 a=target.transform.position,b=transform.position+Vector3.up*.15f;
        for(int i=0;i<24;i++){float t=i/23f;Vector3 p=Vector3.Lerp(a,b,t);p.y+=Mathf.Sin(t*Mathf.PI)*.3f+Mathf.Sin(t*20-Time.time*12)*.045f*Mathf.Sin(t*Mathf.PI);thread.SetPosition(i,p);}
    }
    void OnDisable(){Cancel();CloseMenu();if(Instance==this)Instance=null;}
    void OnDestroy(){if(material)Destroy(material);}
    void OnGUI()
    {
        if(StationAtlas.MapVisible)return;
        using(EmptyHeartUI.Begin())
        {
            if(MenuOpen)
            {
                GUI.depth=-210;float w=Mathf.Min(920,EmptyHeartUI.Width-40),h=660,x=(EmptyHeartUI.Width-w)/2,y=(EmptyHeartUI.Height-h)/2;
                EmptyHeartUI.Fill(new Rect(0,0,EmptyHeartUI.Width,EmptyHeartUI.Height),new Color(.003f,.01f,.015f,.9f));EmptyHeartUI.Panel(new Rect(x,y,w,h),EmptyHeartUI.Copper);
                EmptyHeartUI.Text(new Rect(x+28,y+18,w-56,40),"СМЕННЫЕ ОРГАНЫ",25,EmptyHeartUI.Paper,true);
                EmptyHeartUI.Text(new Rect(x+28,y+64,w-56,32),"ДВА ГНЕЗДА ОБОЛОЧКИ / Установка только у колыбели",16,EmptyHeartUI.Jade);
                for(int i=0;i<Names.Length;i++)
                {
                    float cy=y+114+(i%3)*158;float cx=x+(i/3)*(w/2);bool owned=(Owned&(1<<i))!=0,on=Has(i);EmptyHeartUI.Panel(new Rect(cx+16,cy,w/2-32,145),on?EmptyHeartUI.Jade:EmptyHeartUI.Copper);
                    var art=OrganPickup.Art(i);if(art){GUI.color=owned?Color.white:new Color(.25f,.3f,.3f);GUI.DrawTextureWithTexCoords(new Rect(cx+24,cy+14,60,60),art.texture,new Rect(art.rect.x/art.texture.width,art.rect.y/art.texture.height,art.rect.width/art.texture.width,art.rect.height/art.texture.height));GUI.color=Color.white;}
                    EmptyHeartUI.Text(new Rect(cx+94,cy+10,w/2-114,28),Names[i],18,owned?EmptyHeartUI.Paper:EmptyHeartUI.Muted,true);
                    EmptyHeartUI.Text(new Rect(cx+94,cy+40,w/2-114,62),owned?Details[i]:"Не найден. Исследуй станцию и оранжереи.",16,EmptyHeartUI.Muted);
                    if(owned){Rect button=new Rect(cx+w/2-172,cy+105,144,30);EmptyHeartUI.Panel(button,on?EmptyHeartUI.Jade:EmptyHeartUI.Copper);EmptyHeartUI.Text(button,on?"ИЗВЛЕЧЬ ["+(i+1)+"]":"УСТАНОВИТЬ ["+(i+1)+"]",13,EmptyHeartUI.Paper,false,TextAnchor.MiddleCenter);if(GUI.Button(button,"",GUIStyle.none))ToggleOrgan(i);}
                }
                EmptyHeartUI.Text(new Rect(x+28,y+h-48,w-56,32),"[1–6] Установить / извлечь     •     [ESC] Вернуться",15,EmptyHeartUI.Paper);
            }
            else
            {
                EmptyHeartUI.Text(new Rect(24,214,320,24),"ОРГАНЫ  "+Enumerable.Range(0,Names.Length).Count(Has)+"/2",12,EmptyHeartUI.Muted);
                int slot=0;for(int i=0;i<Names.Length;i++)if(Has(i)){var icon=OrganPickup.Art(i);if(icon)GUI.DrawTextureWithTexCoords(new Rect(24+slot*42,240,34,34),icon.texture,new Rect(icon.rect.x/icon.texture.width,icon.rect.y/icon.texture.height,icon.rect.width/icon.texture.width,icon.rect.height/icon.texture.height));slot++;}
                if(nearby && Ready)EmptyHeartUI.Interaction("G","СМЕННЫЕ ОРГАНЫ","Два гнезда • выбрать сборку",85);
                var residue=Channeling?target:ImpulseResidue.Nearest(transform.position);
                if(residue && Ready && Clear(residue.transform.position))
                {float x=EmptyHeartUI.Width/2-175,y=EmptyHeartUI.Height-132;EmptyHeartUI.Panel(new Rect(x,y,350,43),EmptyHeartUI.Jade);EmptyHeartUI.Text(new Rect(x+12,y+4,326,30),Channeling?"ПОГЛОЩЕНИЕ ИМПУЛЬСА…":health.CurrentHealth>=health.MaxHealth?"ОБОЛОЧКА ЦЕЛА — ИМПУЛЬС СОХРАНЁН":"[F] УДЕРЖИВАТЬ — ПОГЛОТИТЬ ИМПУЛЬС",13,EmptyHeartUI.Paper,false,TextAnchor.MiddleCenter);if(Channeling)EmptyHeartUI.Fill(new Rect(x+8,y+38,334*Mathf.Clamp01(Progress),2),EmptyHeartUI.Jade);}
            }
            if(Time.unscaledTime<noticeUntil)EmptyHeartUI.Text(new Rect(EmptyHeartUI.Width/2-370,EmptyHeartUI.Height-44,740,36),notice,16,EmptyHeartUI.Jade,false,TextAnchor.MiddleCenter);
        }
    }
}






