using UnityEngine;
// Environmental motion remains local to the second biome.
public sealed class VascularAtmosphere : MonoBehaviour
{
 Transform[] foliage,backdrops;Vector3[] backgroundOrigins;Camera view;
 void Start(){view=Camera.main;var plants=new System.Collections.Generic.List<Transform>();var backs=new System.Collections.Generic.List<Transform>();foreach(var r in GetComponentsInChildren<SpriteRenderer>()){if(r.name=="Глубина оранжереи")backs.Add(r.transform);else if(r.name.Contains("растительность") || r.name.Contains("Древо") || r.name.Contains("Гигантский") || (r.sprite && r.sprite.name.Contains("RootButtress")))plants.Add(r.transform);}foliage=plants.ToArray();backdrops=backs.ToArray();backgroundOrigins=new Vector3[backdrops.Length];for(int i=0;i<backdrops.Length;i++)backgroundOrigins[i]=backdrops[i].position;}
 void LateUpdate(){for(int i=0;i<foliage.Length;i++)if(foliage[i])foliage[i].localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.time*.65f+i*1.7f)*.45f);if(!view)return;for(int i=0;i<backdrops.Length;i++)backdrops[i].position=backgroundOrigins[i]+new Vector3((view.transform.position.x-backgroundOrigins[i].x)*.035f,0,0);}
}

