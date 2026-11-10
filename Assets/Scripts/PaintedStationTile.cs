using UnityEngine;
using UnityEngine.Tilemaps;

// Visual skin only: the original terrain still owns all collisions.
[CreateAssetMenu(menuName="Empty Heart/Painted Station Surface")]
public class PaintedStationTile : TileBase
{
    public Sprite[] surfaces;
    public override void GetTileData(Vector3Int position, ITilemap map, ref TileData data)
    {
        if(surfaces==null || surfaces.Length<16) return;
        int variant=(int)((uint)(position.x*73856093 ^ position.y*19349663)%4);
        int row=3; bool mirror=false;
        if(!map.GetTile(position+Vector3Int.up)) row=0;
        else if(!map.GetTile(position+Vector3Int.left)) row=1;
        else if(!map.GetTile(position+Vector3Int.right)) {row=1;mirror=true;}
        else if(!map.GetTile(position+Vector3Int.down)) row=2;
        data.sprite=surfaces[row*4+variant]; data.color=Color.white;
        Vector2 size=data.sprite.bounds.size;
        data.transform=Matrix4x4.Scale(new Vector3((mirror?-1:1)/size.x,1/size.y,1));
        data.flags=TileFlags.LockAll; data.colliderType=Tile.ColliderType.None;
    }
    public override void RefreshTile(Vector3Int position,ITilemap map)
    {
        for(int x=-1;x<=1;x++) for(int y=-1;y<=1;y++) map.RefreshTile(position+new Vector3Int(x,y,0));
    }
}
