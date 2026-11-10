using UnityEngine;

[DefaultExecutionOrder(900)]
public class StationArrival : MonoBehaviour
{
    void Start()
    {
        if(StationJourney.Destination!=gameObject.scene.name)return;
        var p=FindFirstObjectByType<PlayerMove>();var h=p.GetComponent<PlayerHealth>();
        p.transform.position=StationJourney.Arrival;p.GetComponent<Rigidbody2D>().position=StationJourney.Arrival;
        h.SetTravelSpawn(StationJourney.Arrival);h.Configure(StationJourney.MaxHealth);if(StationJourney.Dash)p.UnlockDash();
        RestCheckpoint checkpoint=null;
        if(StationJourney.Checkpoints.TryGetValue(gameObject.scene.name,out string checkpointName))
            foreach(var candidate in FindObjectsByType<RestCheckpoint>(FindObjectsSortMode.None))if(candidate.name==checkpointName)checkpoint=candidate;
        h.RestoreTravelHealth(StationJourney.Health,checkpoint);
        var boss=FindFirstObjectByType<BossArena>();if(boss && StationJourney.BossDefeated)boss.RestoreDefeated(StationJourney.CoreCollected);
        foreach(var fragment in FindObjectsByType<ShellFragment>(FindObjectsSortMode.None))if(StationJourney.Collected.Contains(fragment.Id))Destroy(fragment.gameObject);
        StationJourney.Destination=null;StationJourney.Travelling=false;Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();h.GrantContactGrace(1);
    }
}
