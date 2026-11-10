using System.Collections;
using UnityEngine;

public class ScreenTransitionFade : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float fadeInDuration = 0.11f;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.22f;
    [SerializeField, Range(0f, 1f)] private float maximumAlpha = 0.82f;

    private float alpha;
    private Coroutine activeFade;

    public void Pulse()
    {
        if (activeFade != null)
            StopCoroutine(activeFade);

        activeFade = StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        yield return Fade(0f, maximumAlpha, fadeInDuration);
        yield return Fade(maximumAlpha, 0f, fadeOutDuration);
        alpha = 0f;
        activeFade = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        alpha = to;
    }

    private void OnGUI()
    {
        if (alpha <= 0.001f)
            return;

        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0.03f, 0.07f, alpha);
        GUI.depth = -1000;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;
    }
}
