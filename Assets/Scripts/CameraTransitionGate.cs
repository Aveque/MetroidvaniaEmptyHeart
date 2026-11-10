using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraTransitionGate : MonoBehaviour
{
    public enum TravelAxis
    {
        Horizontal,
        Vertical
    }

    [SerializeField] private TravelAxis travelAxis;
    [SerializeField] private CameraRoom negativeSideRoom;
    [SerializeField] private CameraRoom positiveSideRoom;
    public CameraRoom NegativeRoom => negativeSideRoom;
    public CameraRoom PositiveRoom => positiveSideRoom;
    // A small dead band avoids camera flicker while standing exactly on the seam.
    private const float BoundaryTolerance = 0.03f;

    public void Configure(TravelAxis axis, CameraRoom negativeRoom, CameraRoom positiveRoom, Vector2 triggerSize)
    {
        travelAxis = axis;
        negativeSideRoom = negativeRoom;
        positiveSideRoom = positiveRoom;

        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = triggerSize;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        UpdateRoom(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        UpdateRoom(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Death/respawn and other teleports also produce exit callbacks. They
        // must not overwrite the room selected at the new spawn position.
        Rigidbody2D body = other.attachedRigidbody;
        float step = body != null ? body.linearVelocity.magnitude * Time.fixedDeltaTime : 0f;
        float reach = other.bounds.extents.magnitude + step + 0.1f;
        if (GetComponent<BoxCollider2D>().bounds.SqrDistance(other.bounds.center) > reach * reach)
            return;
        UpdateRoom(other);
    }

    private void UpdateRoom(Collider2D other)
    {
        PlayerMove player = other.GetComponentInParent<PlayerMove>();
        if (player == null)
            return;

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        Vector2 position = body != null ? body.position : (Vector2)player.transform.position;
        float playerCoordinate = travelAxis == TravelAxis.Horizontal
            ? position.x
            : position.y;
        Vector3 gateCenter = transform.TransformPoint(GetComponent<BoxCollider2D>().offset);
        float gateCoordinate = travelAxis == TravelAxis.Horizontal
            ? gateCenter.x
            : gateCenter.y;

        float distance = playerCoordinate - gateCoordinate;
        if (Mathf.Abs(distance) <= BoundaryTolerance)
            return;

        CameraRoom destination = distance > 0f ? positiveSideRoom : negativeSideRoom;

        CameraFollow cameraFollow = Camera.main != null
            ? Camera.main.GetComponent<CameraFollow>()
            : FindFirstObjectByType<CameraFollow>();

        if (cameraFollow == null || destination == null || cameraFollow.ActiveRoom == destination)
            return;

        if(cameraFollow.GetComponent<GardenCameraCoverage>() && !destination.Contains(position))return;
        cameraFollow.SetRoom(destination);

        ScreenTransitionFade fade = cameraFollow.GetComponent<ScreenTransitionFade>();
        if (fade != null)
            fade.Pulse();
    }
}

