using UnityEngine;
public sealed class GardenOpenSky:MonoBehaviour
{
 public Sprite Sky;StationParallax backdrop;Sprite original;bool open;Color[] tints;
 void Start(){backdrop=FindFirstObjectByType<StationParallax>();if(backdrop&&backdrop.Planes.Length>0){original=backdrop.Planes[0].sprite;tints=new Color[backdrop.Planes.Length];for(int j=0;j<tints.Length;j++)tints[j]=backdrop.Planes[j].tint;}}
 void LateUpdate(){if(!Sky||!backdrop||!Camera.main)return;bool outdoors=Camera.main.transform.position.x>228;if(outdoors==open)return;open=outdoors;for(int j=1;j<backdrop.Planes.Length;j++){var tint=tints[j];if(open)tint.a*=j==1?.12f:.2f;backdrop.Planes[j].tint=tint;}var plane=backdrop.Planes[0];plane.sprite=open?Sky:original;if(plane.tiles==null)return;foreach(var tile in plane.tiles){tile.sprite=plane.sprite;var size=plane.sprite.bounds.size;tile.transform.localScale=new Vector3(plane.tileSize.x/size.x,plane.tileSize.y/size.y,1);}}
}

