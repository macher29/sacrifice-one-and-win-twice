
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneChangeTrigger : MonoBehaviour
{
    [Header("SCENE SETTINGS")]
    [SerializeField] private string sceneName;

    [Header("BLACK SCREEN FADE")]
    [SerializeField] private Image blackScreen;
    [SerializeField] private float fadeDuration = 1.5f;

    private bool sceneChanging;

    private void Start()
    {
        if (blackScreen != null)
        {
            Color color = blackScreen.color;
            color.a = 0f;
            blackScreen.color = color;
            blackScreen.gameObject.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (sceneChanging || !other.CompareTag("Player"))
            return;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SceneChangeTrigger] Scene Name is empty!", this);
            return;
        }

        if (blackScreen == null)
        {
            Debug.LogError("[SceneChangeTrigger] Black Screen is not assigned!", this);
            return;
        }

        sceneChanging = true;
        StartCoroutine(ChangeScene());
    }

    private IEnumerator ChangeScene()
    {
        float elapsed = 0f;
        Color color = blackScreen.color;
        color.a = 0f;
        blackScreen.color = color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Clamp01(elapsed / fadeDuration);
            blackScreen.color = color;
            yield return null;
        }

        color.a = 1f;
        blackScreen.color = color;

        SceneManager.LoadScene(sceneName);
    }
}
