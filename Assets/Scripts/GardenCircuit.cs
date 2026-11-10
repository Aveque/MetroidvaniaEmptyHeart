using UnityEngine;
using UnityEngine.InputSystem;
public sealed class GardenCircuit : MonoBehaviour
{
 public bool Door;public string Id;
 public static int Count=>(StationJourney.Opened.Contains("garden-relay-west")?1:0)+(StationJourney.Opened.Contains("garden-relay-east")?1:0);
 public bool Ready=>Count==2;
 PlayerMove player;
 void Start(){player=FindFirstObjectByType<PlayerMove>();}
 public bool Activate(PlayerMove p){if(Door||StationJourney.Opened.Contains(Id)||!VascularResonance.InputAllowed||!p||Vector2.Distance(p.transform.position,transform.position)>2)return false;StationJourney.Opened.Add(Id);return true;}
 void Update(){if(!Door&&Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame)Activate(player);}
 void OnGUI(){if(Door||!player||PlayerOrgans.BlocksInput||StationAtlas.MapVisible||Vector2.Distance(player.transform.position,transform.position)>2)return;using(EmptyHeartUI.Begin())EmptyHeartUI.Interaction("E",StationJourney.Opened.Contains(Id)?"РЕЛЕ ЗАМКНУТО — КОНТУР "+Count+"/2":"ЗАМКНУТЬ РЕЛЕ СЕРДЦЕВИНЫ — "+Count+"/2","Релейная сеть сердцевины",190);}
}



