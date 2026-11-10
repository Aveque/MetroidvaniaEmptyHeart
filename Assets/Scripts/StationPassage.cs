using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class StationPassage : MonoBehaviour
{
    public string TargetScene,Label;public Vector2 Arrival;public bool RequiresBoss;
    PlayerMove player;bool loading;float fade;
    void Start(){player=FindFirstObjectByType<PlayerMove>();}
    public bool TryTravel(PlayerMove p)
    {
        if(Time.timeScale<=0 || StationLoreNode.BlocksInput || PlayerOrgans.BlocksInput || PlayerOrgans.IsChanneling)return false;
        if(!p || loading || StationJourney.Travelling || Vector2.Distance(p.transform.position,transform.position)>1.9f || p.GetComponent<PlayerHealth>().IsRespawning)return false;
        var boss=FindFirstObjectByType<BossArena>();if(RequiresBoss && (!boss || !boss.Defeated))return false;
        StartCoroutine(Travel(p));return true;
    }
    IEnumerator Travel(PlayerMove p)
    {
        loading=true;StationJourney.Travelling=true;StationJourney.Dash=p.DashUnlocked;StationJourney.MaxHealth=p.GetComponent<PlayerHealth>().MaxHealth;
        var health=p.GetComponent<PlayerHealth>();StationJourney.Health=health.CurrentHealth;
        health.GrantContactGrace(2);
        if(health.ActiveCheckpoint)StationJourney.Checkpoints[p.gameObject.scene.name]=health.ActiveCheckpoint.name;
        var boss=FindFirstObjectByType<BossArena>();if(boss)StationJourney.BossDefeated=boss.Defeated;
        StationJourney.Destination=TargetScene;StationJourney.Arrival=Arrival;p.enabled=false;p.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
        for(float t=0;t<.45f;t+=Time.unscaledDeltaTime){fade=t/.45f;yield return null;}
        var op=SceneManager.LoadSceneAsync(TargetScene);if(op==null){p.enabled=true;loading=false;StationJourney.Travelling=false;yield break;}
        while(!op.isDone)yield return null;
    }
    void Update(){if(player && Keyboard.current!=null && Keyboard.current.eKey.wasPressedThisFrame)TryTravel(player);}
    void OnGUI()
    {
        if(!player || StationAtlas.MapVisible)return;
        using(EmptyHeartUI.Begin())
        {
            if(loading){GUI.depth=-100;EmptyHeartUI.Fill(new Rect(0,0,EmptyHeartUI.Width,EmptyHeartUI.Height),new Color(.005f,.015f,.018f,fade));return;}
            if(Vector2.Distance(player.transform.position,transform.position)>1.9f)return;
            var boss=FindFirstObjectByType<BossArena>();bool open=!RequiresBoss || (boss && boss.Defeated);
            Vector3 s=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*1.4f);
            EmptyHeartUI.Interaction("E",open?Label:"КОНТУР ЗАКРЫТ СТРАЖЕМ","Переход между локациями",160);
        }
    }
}


