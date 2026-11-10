using UnityEngine;
using UnityEngine.InputSystem;
public sealed class ReturnDrivePickup:MonoBehaviour
{
 PlayerMove player;float notice;
 void Start(){player=FindFirstObjectByType<PlayerMove>();if(player.DoubleJumpUnlocked)foreach(var r in GetComponentsInChildren<SpriteRenderer>())r.enabled=false;}
 public bool TryCollect(PlayerMove p){if(!p||p.DoubleJumpUnlocked||!VascularResonance.InputAllowed||Vector2.Distance(p.transform.position,transform.position)>1.7f)return false;p.UnlockDoubleJump();foreach(var r in GetComponentsInChildren<SpriteRenderer>())r.enabled=false;notice=Time.unscaledTime+4;return true;}
 void Update(){if(Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame)TryCollect(player);}
 void OnGUI(){if(!player||StationAtlas.MapVisible||PlayerOrgans.BlocksInput)return;using(EmptyHeartUI.Begin()){if(Time.unscaledTime<notice)EmptyHeartUI.Announcement("ВОЗВРАТНЫЙ ПРИВОД","ДВОЙНОЙ ПРЫЖОК","Нажми ПРОБЕЛ ещё раз в воздухе. Верхние корни теперь достижимы.",Mathf.Min(1,notice-Time.unscaledTime),.3f);if(!player.DoubleJumpUnlocked&&Vector2.Distance(player.transform.position,transform.position)<1.7f)EmptyHeartUI.Interaction("E","ВОЗВРАТНЫЙ ПРИВОД","Открывает второй прыжок в воздухе",145);}}
}
