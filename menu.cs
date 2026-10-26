
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuController : MonoBehaviour
{
    [Header("SCENE SETTINGS")]
    [SerializeField] private string gameSceneName;
    [SerializeField] private float sceneFadeDuration = 1.5f;

    [Header("SCREEN FADE")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float menuFadeInDuration = 1.2f;

    [Header("UI SOUNDS")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip uiHoverSound;
    [SerializeField] private AudioClip uiClickSound;
    [SerializeField, Range(0f, 1f)] private float uiVolume = 0.7f;

    [Header("BUTTON ANIMATIONS")]
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float pressScale = 0.94f;
    [SerializeField] private float animationSpeed = 12f;

    private bool isTransitioning;

    private void Start()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            SetFadeAlpha(1f);
            StartCoroutine(FadeScreen(1f, 0f, menuFadeInDuration));
        }
    }

    public void PlayGame()
    {
        if (isTransitioning)
            return;

        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError("Enter the game scene name in the Inspector.");
            return;
        }

        isTransitioning = true;

        if (uiAudioSource != null && uiClickSound != null)
            uiAudioSource.PlayOneShot(uiClickSound, uiVolume);

        StartCoroutine(LoadGameScene());
    }

    public void ExitGame()
    {
        if (isTransitioning)
            return;

        isTransitioning = true;

        if (uiAudioSource != null && uiClickSound != null)
            uiAudioSource.PlayOneShot(uiClickSound, uiVolume);

        StartCoroutine(ExitAfterSound());
    }

    public void PlayHoverSound()
    {
        if (isTransitioning)
            return;

        if (uiAudioSource != null && uiHoverSound != null)
            uiAudioSource.PlayOneShot(uiHoverSound, uiVolume);
    }

    public void PlayClickSound()
    {
        if (isTransitioning)
            return;

        if (uiAudioSource != null && uiClickSound != null)
            uiAudioSource.PlayOneShot(uiClickSound, uiVolume);
    }

    private IEnumerator LoadGameScene()
    {
        if (fadeImage != null)
            yield return StartCoroutine(
                FadeScreen(0f, 1f, sceneFadeDuration)
            );

        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator ExitAfterSound()
    {
        if (fadeImage != null)
            yield return StartCoroutine(
                FadeScreen(0f, 1f, sceneFadeDuration)
            );

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private IEnumerator FadeScreen(
        float startAlpha,
        float endAlpha,
        float duration)
    {
        if (fadeImage == null)
            yield break;

        float elapsed = 0f;
        SetFadeAlpha(startAlpha);

        if (duration <= 0f)
        {
            SetFadeAlpha(endAlpha);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            SetFadeAlpha(
                Mathf.Lerp(startAlpha, endAlpha, smoothProgress)
            );

            yield return null;
        }

        SetFadeAlpha(endAlpha);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null)
            return;

        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }

    public void AnimateButtonEnter(Transform button)
    {
        if (button == null || isTransitioning)
            return;

        StopAllCoroutines();
        StartCoroutine(ScaleButton(button, hoverScale));
        PlayHoverSound();
    }

    public void AnimateButtonExit(Transform button)
    {
        if (button == null || isTransitioning)
            return;

        StopAllCoroutines();
        StartCoroutine(ScaleButton(button, 1f));
    }

    public void AnimateButtonPress(Transform button)
    {
        if (button == null || isTransitioning)
            return;

        StartCoroutine(PressButton(button));
    }

    private IEnumerator PressButton(Transform button)
    {
        yield return StartCoroutine(ScaleButton(button, pressScale));
        yield return StartCoroutine(ScaleButton(button, hoverScale));
    }

    private IEnumerator ScaleButton(Transform button, float targetScale)
    {
        Vector3 startScale = button.localScale;
        Vector3 endScale = Vector3.one * targetScale;
        float elapsed = 0f;

        while (elapsed < 0.12f)
        {
            if (button == null)
                yield break;

            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / 0.12f);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            button.localScale = Vector3.Lerp(
                startScale,
                endScale,
                smoothProgress
            );

            yield return null;
        }

        if (button != null)
            button.localScale = endScale;
    }
}
