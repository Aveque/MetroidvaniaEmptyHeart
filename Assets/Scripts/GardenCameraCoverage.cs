using UnityEngine;
// Scene-local fallback: every playable point must have a camera owner, including widened atria.
[DefaultExecutionOrder(-20)]public sealed class GardenCameraCoverage:MonoBehaviour
{
 CameraRoom[] rooms;CameraFollow follow;PlayerMove player;
 void Awake(){rooms=FindObjectsByType<CameraRoom>(FindObjectsSortMode.None);follow=GetComponent<CameraFollow>();player=FindFirstObjectByType<PlayerMove>();}
 void LateUpdate(){if(!player||!follow)return;var current=follow.ActiveRoom;if(current&&current.Contains(player.transform.position))return;CameraRoom best=null;foreach(var r in rooms)if(r&&r.Contains(player.transform.position)&&(!best||r.WorldBounds.size.sqrMagnitude<best.WorldBounds.size.sqrMagnitude))best=r;if(best)follow.SetRoom(best);}
}
