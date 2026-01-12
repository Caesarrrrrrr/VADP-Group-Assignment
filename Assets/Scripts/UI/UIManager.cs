using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; 

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject sidePanel;
    public CanvasGroup sidePanelCanvasGroup;

    [Header("Animation Settings")]
    public float animationDuration = 0.4f;

    // This curve creates the "Bounce/Pop" effect. 
    // In Inspector, make it go slightly above 1.0 and then back to 1.0.
    public AnimationCurve popCurve = new AnimationCurve(
        new Keyframe(0, 0),
        new Keyframe(0.8f, 1.1f), // The "Overshoot" (Bounce)
        new Keyframe(1, 1)
    );

    private Vector3 openPosition;
    private Vector3 closedPosition;
    private bool isMenuOpen = false;
    private Coroutine currentRoutine;

    void Start()
    {
        if (sidePanel != null)
        {
            // Capture positions
            openPosition = sidePanel.transform.localPosition;
            closedPosition = Vector3.zero;

            // Hide immediately
            sidePanel.SetActive(false);
            sidePanel.transform.localPosition = closedPosition;
        }
    }

    public void ToggleSideMenu()
    {
        Debug.Log("Toggling Side Menu");
        if (isMenuOpen) CloseMenu();
        else OpenMenu();
    }

    public void OpenMenu()
    {
        if (sidePanel == null) return;

        isMenuOpen = true;
        sidePanel.SetActive(true);

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(AnimatePanel(closedPosition, openPosition, 0f, 1f));
    }

    public void CloseMenu()
    {
        if (sidePanel == null) return;

        isMenuOpen = false;

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(AnimatePanel(openPosition, closedPosition, 1f, 0f, true));
    }

    // A Coroutine that manually handles the animation
    private IEnumerator AnimatePanel(Vector3 startPos, Vector3 endPos, float startAlpha, float endAlpha, bool disableOnComplete = false)
    {
        float timer = 0f;

        while (timer < animationDuration)
        {
            timer += Time.deltaTime;
            float percent = Mathf.Clamp01(timer / animationDuration);

            // 1. Evaluate the Curve for that nice "Bounce"
            // If closing, we just use simple linear (percent), if opening we use the curve
            float curvePercent = disableOnComplete ? percent : popCurve.Evaluate(percent);

            // 2. Move
            sidePanel.transform.localPosition = Vector3.LerpUnclamped(startPos, endPos, curvePercent);

            // 3. Fade
            if (sidePanelCanvasGroup != null)
            {
                sidePanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, percent);
            }

            yield return null;
        }

        // Ensure we end exactly at the target
        sidePanel.transform.localPosition = endPos;
        if (sidePanelCanvasGroup != null) sidePanelCanvasGroup.alpha = endAlpha;

        if (disableOnComplete)
        {
            sidePanel.SetActive(false);
        }
    }

    public void LoadSceneByIndex(int sceneIndex)
    {
        Debug.Log($"Loading Scene Index: {sceneIndex}");
        SceneManager.LoadScene(sceneIndex);
    }

    public void QuitApplication()
    {
        Debug.Log("Quitting Application...");
        Application.Quit();
    }
}