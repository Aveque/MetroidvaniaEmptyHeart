using UnityEngine;
using UnityEngine.InputSystem;
// Session state shares the same lifetime as checkpoints, organs and biome travel.
public sealed class VascularResonance : MonoBehaviour
{
 public enum Kind { Ability, RootSeal, VitalSeed }
 public Kind Type;public string Id;public Vector2 Size=new(.6f,4.5f);
 public static bool Unlocked=>StationJourney.Collected.Contains("vascular-resonance");
 public bool Open=>Type==Kind.RootSeal && StationJourney.Opened.Contains(Id);
 public bool Collected=>Type==Kind.Ability?Unlocked:StationJourney.Collected.Contains(Id);
 public float Progress{get;private set;}
 LivingRootLock livingRoots; public const float RootOpeningDuration=1.75f;
 PlayerMove player;BoxCollider2D seal;SpriteRenderer[] art;Vector3[] scales;float noticeUntil;
 public static bool InputAllowed=>Time.timeScale>0 && !StationLoreNode.BlocksInput && !PlayerOrgans.BlocksInput && !PlayerOrgans.IsChanneling && !StationJourney.Travelling;
 void Start(){livingRoots=GetComponent<LivingRootLock>();player=FindFirstObjectByType<PlayerMove>();art=GetComponentsInChildren<SpriteRenderer>();scales=new Vector3[art.Length];for(int i=0;i<art.Length;i++)scales[i]=art[i].transform.localScale;
 if(Type==Kind.RootSeal){seal=GetComponent<BoxCollider2D>();seal.size=Size;if(livingRoots)foreach(var r in art)r.enabled=false;if(Open){Progress=1;seal.enabled=false;foreach(var r in art)r.enabled=false;}}
 else if(Collected)foreach(var r in art)r.enabled=false;
 }
 public bool Near(PlayerMove p)=>p && (Type==Kind.RootSeal?GetComponent<BoxCollider2D>().bounds.SqrDistance(p.transform.position)<2.5f:Vector2.Distance(p.transform.position,transform.position)<1.8f);
 public bool TryUse(PlayerMove p){if(!InputAllowed || !Near(p) || p.GetComponent<PlayerHealth>().IsRespawning || p.IsDashing || p.IsAttacking)return false;
 if(Type==Kind.RootSeal){if(!Unlocked || Open || Progress>0 || (GetComponent<GardenCircuit>() && !GetComponent<GardenCircuit>().Ready))return false;Progress=.001f;return true;}
 if(Collected)return false;
 if(Type==Kind.Ability)StationJourney.Collected.Add("vascular-resonance");else{StationJourney.Collected.Add(Id);var h=p.GetComponent<PlayerHealth>();h.Configure(h.MaxHealth+1);StationJourney.MaxHealth=h.MaxHealth;StationJourney.Health=h.CurrentHealth;}
 foreach(var r in art)r.enabled=false;noticeUntil=Time.unscaledTime+5;return true;
 }
 void Update(){if(Type==Kind.RootSeal && Progress>0 && Progress<1){Progress=Mathf.Min(1,Progress+Time.deltaTime/RootOpeningDuration);if(!livingRoots)for(int i=0;i<art.Length;i++)art[i].transform.localScale=scales[i]*(1-Mathf.SmoothStep(0,1,Progress));if(Progress>=1){StationJourney.Opened.Add(Id);seal.enabled=false;foreach(var r in art)r.enabled=false;}}
 if(!player || Keyboard.current==null)return;bool use=Type==Kind.RootSeal?Keyboard.current.rKey.wasPressedThisFrame:Keyboard.current.eKey.wasPressedThisFrame;if(use)TryUse(player);
 }
 void OnGUI(){if(!player || StationAtlas.MapVisible)return;using(EmptyHeartUI.Begin()){
 if(Type==Kind.Ability && Unlocked)EmptyHeartUI.Text(new Rect(24,282,360,28),"R — СОСУДИСТЫЙ РЕЗОНАНС",12,EmptyHeartUI.Jade);
 if(Time.unscaledTime<noticeUntil)EmptyHeartUI.Announcement(Type==Kind.Ability?"СОСУДИСТЫЙ РЕЗОНАНС":"СЕМЯ ЖИВОЙ ТКАНИ",Type==Kind.Ability?"НОВАЯ СПОСОБНОСТЬ":"ОБОЛОЧКА УСИЛЕНА",Type==Kind.Ability?"R у живого узла — разомкнуть корневой контур. Путь останется открытым.":"Максимальное здоровье увеличено на один сегмент.",Mathf.Clamp01(noticeUntil-Time.unscaledTime),.52f);
 if(!InputAllowed || !Near(player) || (Type==Kind.RootSeal?Open || Progress>0:Collected))return;
 string label=GetComponent<GardenCircuit>() && !GetComponent<GardenCircuit>().Ready?"КОНТУР СЕРДЦЕВИНЫ — НАЙДЕНО РЕЛЕ "+GardenCircuit.Count+"/2":Type==Kind.RootSeal?(Unlocked?"[R] СОСУДИСТЫЙ РЕЗОНАНС":"КОРНИ НЕ ОТЗЫВАЮТСЯ — НУЖЕН ЖИВОЙ ИМПУЛЬС"):Type==Kind.Ability?"[E] ПРИНЯТЬ ЖИВОЙ ИМПУЛЬС":"[E] ЗАБРАТЬ СЕМЯ ЖИВОЙ ТКАНИ";
 EmptyHeartUI.Interaction(Type==Kind.RootSeal?"R":"E",label.Replace("[E] ","").Replace("[R] ",""),"",145);
 }}
}




