using UnityEngine;
// Anchored foliage breathes around its base; motes are a sparse depth cue rather than gameplay obstacles.
public sealed class VascularGardenAtmosphere : MonoBehaviour
{
 public Sprite Fern,ParticleSprite;
 Transform[] plants;Quaternion[] rotations;Material moteMaterial;
 void Start(){var list=new System.Collections.Generic.List<Transform>();foreach(var r in GetComponentsInChildren<SpriteRenderer>())if(r.sprite==Fern&&r.name=="Укоренённые светлые листья")list.Add(r.transform);plants=list.ToArray();rotations=new Quaternion[plants.Length];for(int i=0;i<plants.Length;i++)rotations[i]=plants[i].localRotation;
 if(!ParticleSprite)return;moteMaterial=new Material(Shader.Find("Sprites/Default"));moteMaterial.mainTexture=ParticleSprite.texture;
 foreach(var room in FindObjectsByType<CameraRoom>(FindObjectsSortMode.None)){var g=new GameObject("Мягкие частицы сада");g.transform.SetParent(transform,false);g.transform.position=room.WorldBounds.center;var ps=g.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.duration=12;main.loop=true;main.startLifetime=new ParticleSystem.MinMaxCurve(6,11);main.startSpeed=new ParticleSystem.MinMaxCurve(.06f,.16f);main.startSize=new ParticleSystem.MinMaxCurve(.035f,.09f);main.maxParticles=18;main.startColor=new Color(.78f,.9f,.77f,.3f);main.simulationSpace=ParticleSystemSimulationSpace.World;var emission=ps.emission;emission.rateOverTime=.65f;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Rectangle;shape.scale=new Vector3(room.WorldBounds.size.x*.75f,room.WorldBounds.size.y*.6f,.1f);var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(.7f,.86f,.75f),0),new GradientColorKey(new Color(.7f,.86f,.75f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.3f,.25f),new GradientAlphaKey(0,1)});color.color=gradient;var renderer=g.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=moteMaterial;renderer.sortingOrder=-8;ps.Play();}
 }
 void LateUpdate(){for(int i=0;i<plants.Length;i++)if(plants[i])plants[i].localRotation=rotations[i]*Quaternion.Euler(0,0,Mathf.Sin(Time.time*.7f+i*1.3f)*.6f);}
 void OnDestroy(){if(moteMaterial)Destroy(moteMaterial);}
}
