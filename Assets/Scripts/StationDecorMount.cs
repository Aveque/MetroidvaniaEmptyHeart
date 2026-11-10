using UnityEngine;

// Visible alpha bounds, rather than texture gutters, define the contact point.
public sealed class StationDecorMount : MonoBehaviour
{
    public Vector2 LocalMin,LocalMax;
    public float SupportY;
    public bool Ceiling;
    public bool UseExplicitContact; public Vector2 ExplicitLocalContact;
    public Vector3 Contact=>transform.TransformPoint(UseExplicitContact?(Vector3)ExplicitLocalContact:new Vector3((LocalMin.x+LocalMax.x)*.5f,Ceiling?LocalMax.y:LocalMin.y,0));
    private void OnDrawGizmosSelected()
    {Gizmos.color=Color.cyan;Vector3 p=Contact;Gizmos.DrawLine(p+Vector3.left*.4f,p+Vector3.right*.4f);}
}

