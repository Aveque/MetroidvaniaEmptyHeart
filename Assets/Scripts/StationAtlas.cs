using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class StationAtlas : MonoBehaviour
{
    PlayerMove player; CameraFollow follow; CameraRoom[] rooms; RestCheckpoint[] rests;
    public bool IsOpen {get;private set;}
    public static bool MapVisible {get;private set;}
    float oldScale;
    string Key(CameraRoom r)=>r.gameObject.scene.name+"/"+r.name;
    public bool Discovered(CameraRoom r)=>r && StationJourney.Visited.Contains(Key(r));
    void Start(){player=FindFirstObjectByType<PlayerMove>();follow=Camera.main.GetComponent<CameraFollow>();rooms=FindObjectsByType<CameraRoom>(FindObjectsSortMode.None);rests=FindObjectsByType<RestCheckpoint>(FindObjectsSortMode.None);}
    void Update()
    {
        if(follow && follow.ActiveRoom)StationJourney.Visited.Add(Key(follow.ActiveRoom));
        var k=Keyboard.current;
        if(k!=null && (k.mKey.wasPressedThisFrame || (IsOpen && k.escapeKey.wasPressedThisFrame)))Toggle();
        if(IsOpen && player.GetComponent<PlayerHealth>().IsRespawning)Close();
    }
    public bool Toggle()
    {
        if(StationLoreNode.BlocksInput || PlayerOrgans.BlocksInput || PlayerOrgans.IsChanneling)return false;
        if(IsOpen){Close();return true;}
        var boss=FindFirstObjectByType<BossArena>();
        if(!player || player.GetComponent<PlayerHealth>().IsRespawning || (boss && boss.InCombat) || MatriarchBoss.AnyCombat || StationJourney.Travelling)return false;
        IsOpen=true;MapVisible=true;oldScale=Time.timeScale;Time.timeScale=0;return true;
    }
    void Close(){if(IsOpen)Time.timeScale=oldScale;IsOpen=false;MapVisible=false;}
    void OnDisable()=>Close();
    Vector2 Project(Vector3 p,Rect area,Bounds bounds,float scale)=>new(area.center.x+(p.x-bounds.center.x)*scale,area.center.y-(p.y-bounds.center.y)*scale);
    bool KnownAt(Vector3 p)=>rooms.Any(r=>Discovered(r) && r.Contains(p));
    static void Line(Vector2 a,Vector2 b,Color color)
    {
        if(Mathf.Abs(a.x-b.x)<.01f)EmptyHeartUI.Fill(new Rect(a.x-1,Mathf.Min(a.y,b.y),2,Mathf.Abs(b.y-a.y)),color);
        else EmptyHeartUI.Fill(new Rect(Mathf.Min(a.x,b.x),a.y-1,Mathf.Abs(b.x-a.x),2),color);
    }
    static string ShortName(string name)=>name.Replace("Станция Эхо — ","").Replace("Нижние сады — ","").Replace("Западное крыло — ","").Replace("Верхний ярус — ","");
    void OnGUI()
    {
        if(!player || rooms==null)return;
        using(EmptyHeartUI.Begin())
        {
            if(!IsOpen){EmptyHeartUI.Text(new Rect(22,184,250,24),"[M] КАРТА СТАНЦИИ",13,EmptyHeartUI.Muted);return;}
            GUI.depth=-90;float w=EmptyHeartUI.Width,h=EmptyHeartUI.Height;
            EmptyHeartUI.Fill(new Rect(0,0,w,h),new Color(.008f,.018f,.022f,.98f));
            EmptyHeartUI.Text(new Rect(40,22,w-80,40),"НЕЙРОГРАФ / "+(player.gameObject.scene.name=="StationEcho_Organic"?"СТАНЦИЯ ЭХО":"СОСУДИСТЫЕ ОРАНЖЕРЕИ"),24,EmptyHeartUI.Paper,true);
            var visible=rooms.Where(Discovered).ToArray();if(visible.Length==0)return;
            Bounds ShapeBounds(CameraRoom r){var s=r.GetComponent<StationRoomShape>();return s?s.WorldBounds:r.WorldBounds;}
            Bounds bounds=ShapeBounds(rooms[0]);foreach(var r in rooms)bounds.Encapsulate(ShapeBounds(r));
            Rect area=new Rect(60,110,w-120,h-220);float scale=Mathf.Min(12,Mathf.Min(area.width/Mathf.Max(bounds.size.x,25),area.height/Mathf.Max(bounds.size.y,18)));
            foreach(var room in visible)
            {
                var shape=room.GetComponent<StationRoomShape>();Bounds b=ShapeBounds(room);Vector2 p=Project(b.center,area,bounds,scale);Rect rr=new Rect(p.x-b.size.x*scale/2,p.y-b.size.y*scale/2,b.size.x*scale,b.size.y*scale);
                bool active=follow.ActiveRoom==room;
                if(shape && shape.Fill && shape.Outline)
                {
                    Color old=GUI.color;GUI.color=active?new Color(.07f,.24f,.21f):new Color(.045f,.085f,.095f);GUI.DrawTexture(rr,shape.Fill);
                    GUI.color=active?EmptyHeartUI.Jade:new Color(.55f,.65f,.61f);GUI.DrawTexture(rr,shape.Outline);GUI.color=old;
                    Vector2 label=Project(shape.Label,area,bounds,scale);float lw=Mathf.Max(60,Mathf.Min(220,room.WorldBounds.size.x*scale*.85f));
                    EmptyHeartUI.Text(new Rect(label.x-lw/2,label.y-20,lw,40),ShortName(room.DisplayName),12,active?EmptyHeartUI.Paper:EmptyHeartUI.Muted,false,TextAnchor.MiddleCenter);
                }
                else
                {
                    EmptyHeartUI.Fill(rr,EmptyHeartUI.Copper);EmptyHeartUI.Fill(new Rect(rr.x+2,rr.y+2,rr.width-4,rr.height-4),EmptyHeartUI.Ink);
                    EmptyHeartUI.Text(rr,ShortName(room.DisplayName),14,EmptyHeartUI.Paper,false,TextAnchor.MiddleCenter);
                }
            }
            foreach(var rest in rests)if(KnownAt(rest.transform.position)){Vector2 p=Project(rest.transform.position,area,bounds,scale);EmptyHeartUI.Hex(new Rect(p.x-5,p.y-5,10,10),rest.IsActive?EmptyHeartUI.Jade:EmptyHeartUI.Paper);}
            foreach(var barrier in FindObjectsByType<DashMembrane>(FindObjectsSortMode.None))if(KnownAt(barrier.transform.position)){Vector2 p=Project(barrier.transform.position,area,bounds,scale);EmptyHeartUI.Fill(new Rect(p.x-2,p.y-7,4,14),EmptyHeartUI.Danger);}
            foreach(var root in FindObjectsByType<VascularResonance>(FindObjectsSortMode.None))if(KnownAt(root.transform.position) && !root.Collected && !root.Open){Vector2 p=Project(root.transform.position,area,bounds,scale);EmptyHeartUI.Text(new Rect(p.x-10,p.y-10,20,20),root.Type==VascularResonance.Kind.RootSeal?"✦":"◆",18,root.Type==VascularResonance.Kind.RootSeal?EmptyHeartUI.Jade:EmptyHeartUI.Paper,false,TextAnchor.MiddleCenter);}
            foreach(var lift in FindObjectsByType<StationLift>(FindObjectsSortMode.None))if(KnownAt(lift.transform.position)){Vector2 p=Project(lift.transform.position,area,bounds,scale);EmptyHeartUI.Text(new Rect(p.x-10,p.y-10,20,20),"↕",18,StationJourney.Opened.Contains(lift.Id)?EmptyHeartUI.Jade:EmptyHeartUI.Copper,false,TextAnchor.MiddleCenter);}
            Vector2 dot=Project(player.transform.position,area,bounds,scale);EmptyHeartUI.Hex(new Rect(dot.x-5,dot.y-5,10,10),EmptyHeartUI.Jade);
            EmptyHeartUI.Text(new Rect(40,h-82,w-80,28),"ЗЕЛЁНЫЙ — ВЫ / РОМБ — КОЛЫБЕЛЬ / РАЗРЫВ КОНТУРА — ПРОХОД / КРАСНЫЙ — БАРЬЕР",13,EmptyHeartUI.Paper);
            EmptyHeartUI.Text(new Rect(40,h-48,w-80,24),"Исследовано: "+visible.Length+"    •    Фрагменты оболочки: "+StationJourney.Fragments+"/3    •    M / Esc — закрыть",14,EmptyHeartUI.Muted);
        }
    }
}


