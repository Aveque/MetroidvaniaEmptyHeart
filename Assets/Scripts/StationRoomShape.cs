using UnityEngine;

// Baked from the same free-space polygons and passage planes as the level.
public class StationRoomShape : MonoBehaviour
{
    public Texture2D Fill,Outline;
    public Vector2 Origin,Size,Label;
    public Bounds WorldBounds=>new Bounds(Origin+Size*.5f,new Vector3(Size.x,Size.y,1));
}
