using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraRoom : MonoBehaviour
{
    [SerializeField] private string displayName = "Станция Эхо";
    [SerializeField] private bool activateOnEnter;
    [SerializeField, Min(0f)] private float edgeReveal = 0.75f;
    [SerializeField, Min(0f)] private float cameraSize;

    private BoxCollider2D roomArea;

    public string DisplayName => displayName;
    public float CameraSize => cameraSize;
    public void SetCameraSize(float size) => cameraSize = Mathf.Max(0f, size);

    public Bounds WorldBounds
    {
        get
        {
            EnsureCollider();
            return roomArea.bounds;
        }
    }

    private void Awake()
    {
        EnsureCollider();
        roomArea.isTrigger = true;
    }

    public void Configure(string roomName, Vector2 size, bool shouldActivateOnEnter = false)
    {
        displayName = roomName;
        activateOnEnter = shouldActivateOnEnter;
        EnsureCollider();
        roomArea.isTrigger = true;
        roomArea.size = size;
    }

    public bool Contains(Vector3 worldPosition)
    {
        return WorldBounds.Contains(new Vector3(worldPosition.x, worldPosition.y, WorldBounds.center.z));
    }

    public Vector3 ClampCameraPosition(Vector3 desired, Camera cameraToClamp)
    {
        Bounds bounds = WorldBounds;
        float halfHeight = cameraToClamp != null && cameraToClamp.orthographic
            ? cameraToClamp.orthographicSize
            : 0f;
        float halfWidth = cameraToClamp != null
            ? halfHeight * cameraToClamp.aspect
            : 0f;

        float x = ClampAxis(desired.x, bounds.min.x, bounds.max.x, halfWidth, edgeReveal);
        float y = ClampAxis(desired.y, bounds.min.y, bounds.max.y, halfHeight, edgeReveal);
        return new Vector3(x, y, desired.z);
    }

    private static float ClampAxis(float value, float min, float max, float cameraHalfSize, float reveal)
    {
        float available = (max - min) * 0.5f;
        float protectedHalfSize = Mathf.Max(0f, cameraHalfSize - reveal);

        if (available <= protectedHalfSize)
            return (min + max) * 0.5f;

        return Mathf.Clamp(value, min + protectedHalfSize, max - protectedHalfSize);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ActivateFor(other);
    }

    private void ActivateFor(Collider2D other)
    {
        if (!activateOnEnter)
            return;

        if (other.GetComponentInParent<PlayerMove>() == null)
            return;

        CameraFollow cameraFollow = Camera.main != null
            ? Camera.main.GetComponent<CameraFollow>()
            : FindFirstObjectByType<CameraFollow>();

        if (cameraFollow != null)
            cameraFollow.SetRoom(this);
    }

    private void EnsureCollider()
    {
        if (roomArea == null)
            roomArea = GetComponent<BoxCollider2D>();
    }

    private void OnDrawGizmosSelected()
    {
        EnsureCollider();
        Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.35f);
        Gizmos.DrawCube(roomArea.bounds.center, roomArea.bounds.size);
        Gizmos.color = new Color(0.2f, 0.75f, 1f, 1f);
        Gizmos.DrawWireCube(roomArea.bounds.center, roomArea.bounds.size);
    }
}
