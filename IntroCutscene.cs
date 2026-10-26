
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class IntroCutscene : MonoBehaviour
{
    [Header("CUTSCENE SETTINGS")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage cutsceneScreen;

    [Header("GAME SETTINGS")]
    [SerializeField] private string gameSceneName;

    private bool cutsceneFinished;
    private bool videoPrepared;

    private void Start()
    {
        if (videoPlayer == null || cutsceneScreen == null)
        {
            Debug.LogError(
                "[IntroCutscene] Assign Video Player and Cutscene Screen!",
                this
            );
            return;
        }

        if (videoPlayer.clip == null && string.IsNullOrEmpty(videoPlayer.url))
        {
            Debug.LogError(
                "[IntroCutscene] No video clip or URL assigned!",
                this
            );
            return;
        }

        cutsceneScreen.gameObject.SetActive(true);

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnCutsceneFinished;
        videoPlayer.errorReceived += OnVideoError;

        StartCoroutine(PrepareVideo());
    }

    private IEnumerator PrepareVideo()
    {
        Debug.Log("[IntroCutscene] Preparing video...", this);

        videoPlayer.Prepare();

        float timeout = 15f;
        float elapsed = 0f;

        while (!videoPlayer.isPrepared && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoPlayer.isPrepared)
        {
            Debug.LogError(
                "[IntroCutscene] Video preparation timed out. " +
                "Check the MP4 format and Video Player settings.",
                this
            );
        }
    }

    private void OnVideoPrepared(VideoPlayer player)
    {
        if (videoPrepared)
            return;

        videoPrepared = true;

        Debug.Log("[IntroCutscene] Video prepared. Starting playback.", this);

        player.Play();
    }

    private void OnVideoError(VideoPlayer player, string message)
    {
        Debug.LogError("[IntroCutscene] Video playback error: " + message, this);
    }

    private void OnCutsceneFinished(VideoPlayer player)
    {
        if (cutsceneFinished)
            return;

        cutsceneFinished = true;

        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError(
                "[IntroCutscene] Game Scene Name is empty!",
                this
            );
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnCutsceneFinished;
            videoPlayer.errorReceived -= OnVideoError;
        }
    }
}



