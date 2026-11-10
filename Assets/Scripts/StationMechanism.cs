using UnityEngine;

public class StationMechanism : MonoBehaviour
{
    public enum Motion{Piston,Pulse,Receiver}
    public Motion Kind;public float Phase;
    Vector3 origin,scale;Quaternion rotation;SpriteRenderer sprite;Color tint;
    void Start(){origin=transform.localPosition;scale=transform.localScale;rotation=transform.localRotation;sprite=GetComponent<SpriteRenderer>();if(sprite)tint=sprite.color;}
    void Update()
    {
        float t=Time.time*1.6f+Phase;
        if(Kind==Motion.Piston)transform.localPosition=origin+Vector3.up*(Mathf.Sin(t)*.18f);
        if(Kind==Motion.Pulse){transform.localScale=Vector3.Scale(scale,new Vector3(1+Mathf.Sin(t)*.035f,1+Mathf.Sin(t+.4f)*.045f,1));if(sprite)sprite.color=Color.Lerp(tint,new Color(.65f,1,.85f,tint.a),(.5f+.5f*Mathf.Sin(t))* .3f);var mount=GetComponent<StationDecorMount>();if(mount)transform.position+=Vector3.up*(mount.SupportY-mount.Contact.y);}
        if(Kind==Motion.Receiver)transform.localRotation=rotation*Quaternion.Euler(0,0,Mathf.Sin(t*.4f)*7);
    }
}
