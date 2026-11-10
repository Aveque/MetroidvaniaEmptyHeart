using UnityEngine;
[DisallowMultipleComponent]public sealed class RelayMechanicalGate:MonoBehaviour
{
 public Sprite DoorArt;public Vector2 Size=new(.7f,4.5f);public bool BattleLocked;public bool Open=>GardenCircuit.Count==2&&!BattleLocked;public float Aperture{get;private set;}
 Transform[] leaves;BoxCollider2D wall;Sprite[] slices;
 void Start(){var preview=transform.Find("Затвор — предпросмотр");if(preview)preview.gameObject.SetActive(false);wall=GetComponent<BoxCollider2D>();wall.size=Size;leaves=new Transform[2];slices=new Sprite[2];if(!DoorArt)return;for(int j=0;j<2;j++){var rect=DoorArt.rect;rect.height*=.5f;rect.y+=j*rect.height;var sprite=Sprite.Create(DoorArt.texture,rect,new Vector2(.5f,.5f),DoorArt.pixelsPerUnit,0,SpriteMeshType.FullRect);slices[j]=sprite;var g=new GameObject(j==0?"Нижняя створка":"Верхняя створка");g.transform.SetParent(transform,false);leaves[j]=g.transform;var r=g.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=9;float scale=Size.y/DoorArt.bounds.size.y;g.transform.localScale=Vector3.one*scale;}}
 void Update(){Aperture=Mathf.MoveTowards(Aperture,Open?1:0,Time.deltaTime*1.8f);wall.enabled=!Open||Aperture<.92f;if(leaves==null)return;for(int j=0;j<2;j++)if(leaves[j])leaves[j].localPosition=Vector3.up*((j==0?-1:1)*(Size.y*.25f+Aperture*Size.y*.55f));}
 void OnGUI(){var p=FindFirstObjectByType<PlayerMove>();if(!p||BattleLocked||Open||StationAtlas.MapVisible||PlayerOrgans.BlocksInput||Vector2.Distance(p.transform.position,transform.position)>2)return;using(EmptyHeartUI.Begin())EmptyHeartUI.Interaction("E","МЕХАНИЧЕСКИЙ КОНТУР ЗАКРЫТ","Реле питания: "+GardenCircuit.Count+"/2",180);}
 void OnDestroy(){if(slices!=null)foreach(var s in slices)if(s)Destroy(s);}
}
