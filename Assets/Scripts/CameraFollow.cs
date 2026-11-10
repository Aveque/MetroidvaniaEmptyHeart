using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float smoothSpeed = 7f;
    [SerializeField] private Vector3 offset;

    private Camera gameCamera;
    private CameraRoom activeRoom;
    private float defaultCameraSize;
    public CameraRoom ActiveRoom => activeRoom;

    private void Awake()
    {
        gameCamera = GetComponent<Camera>();
        defaultCameraSize = gameCamera.orthographicSize;
        FindPlayerIfNeeded();
    }

    private void Start()
    {
        SnapToPlayerRoom();
    }

    private void LateUpdate()
    {
        FindPlayerIfNeeded();
        if (player == null)
            return;

        Vector3 targetPosition = player.position + offset;

        if (activeRoom != null)
            targetPosition = activeRoom.ClampCameraPosition(targetPosition, gameCamera);

        targetPosition.z = transform.position.z;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            1f - Mathf.Exp(-smoothSpeed * Time.deltaTime)
        );
    }

    public void SetRoom(CameraRoom room, bool snap = false)
    {
        if (room == null)
            return;

        activeRoom = room;
        gameCamera.orthographicSize = room.CameraSize > 0f ? room.CameraSize : defaultCameraSize;

        RoomNameDisplay display = GetComponent<RoomNameDisplay>();
        if (display != null)
            display.Show(room.DisplayName);

        if (snap && player != null)
        {
            Vector3 target = activeRoom.ClampCameraPosition(player.position + offset, gameCamera);
            target.z = transform.position.z;
            transform.position = target;
        }
    }

    public void SnapToPlayerRoom()
    {
        FindPlayerIfNeeded();
        if (player == null)
            return;

        CameraRoom[] rooms = FindObjectsByType<CameraRoom>(FindObjectsSortMode.None);
        foreach (CameraRoom room in rooms)
        {
            if (!room.Contains(player.position))
                continue;

            SetRoom(room, true);
            return;
        }

        activeRoom = null;
        gameCamera.orthographicSize = defaultCameraSize;
        Vector3 target = player.position + offset;
        target.z = transform.position.z;
        transform.position = target;
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null)
            return;

        PlayerMove controller = FindFirstObjectByType<PlayerMove>();
        if (controller != null)
            player = controller.transform;
    }
}

