using UnityEngine;

public sealed class StationAmbientProp : MonoBehaviour
{
    public bool Hanging;
    private Quaternion rest;
    private SpriteRenderer sprite;
    private Color tint;
    private void Start(){rest=transform.localRotation;sprite=GetComponent<SpriteRenderer>();tint=sprite.color;}
    private void Update()
    {
        float phase=Time.time*1.3f+transform.position.x*.7f;
        if(Hanging)transform.localRotation=rest*Quaternion.Euler(0,0,Mathf.Sin(phase)*1.2f);
        sprite.color=tint*new Color(.96f+Mathf.Sin(phase)*.04f,1,1,1);
    }
}
