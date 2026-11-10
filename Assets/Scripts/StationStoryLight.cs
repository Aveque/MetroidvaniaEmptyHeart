using UnityEngine;

public sealed class StationStoryLight : MonoBehaviour
{
    public bool SignalSequence;
    public float Phase;
    SpriteRenderer art; Color tint;
    void Awake(){art=GetComponent<SpriteRenderer>();if(art)tint=art.color;}
    void Update()
    {
        if(!art)return;
        float t=Time.time+Phase;
        // Five short impulses, a pause, then a weak sixth: the observatory log.
        float p=Mathf.Repeat(t,8);
        float intensity=SignalSequence?(p<2.5f?(Mathf.Repeat(p,.5f)<.16f?1:.15f):(p>5 && p<5.25f?.4f:.12f)):.72f+.18f*Mathf.Sin(t*.9f);
        art.color=new Color(tint.r,tint.g,tint.b,tint.a*intensity);
    }
}
