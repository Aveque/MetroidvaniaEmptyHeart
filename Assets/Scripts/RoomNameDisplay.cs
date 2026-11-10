using System.Collections;
using UnityEngine;

public class RoomNameDisplay : MonoBehaviour
{
    [SerializeField] private float visibleTime = 2.2f;
    [SerializeField] private float fadeTime = 0.6f;

    private string currentName;
    private float alpha;
    private Coroutine fadeRoutine;

    public void Show(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName) || roomName == currentName)
            return;

        currentName = roomName;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(DisplayRoutine());
    }

    private IEnumerator DisplayRoutine()
    {
        alpha = 1f;
        yield return new WaitForSeconds(visibleTime);

        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            alpha = 1f - Mathf.Clamp01(elapsed / fadeTime);
            yield return null;
        }

        alpha = 0f;
        fadeRoutine = null;
    }

    private void OnGUI()
    {
        if(StationAtlas.MapVisible)return;
        if (alpha <= 0 || string.IsNullOrEmpty(currentName)) return;
        EmptyHeartUI.Announcement("СЕРДЦЕ ПУСТОТЫ", currentName.Replace("Станция Эхо — ", ""), UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="VascularThreshold"?"СОСУДИСТЫЕ ОРАНЖЕРЕИ":"СТАНЦИЯ ЭХО", alpha, 0.02f);
    }
}


