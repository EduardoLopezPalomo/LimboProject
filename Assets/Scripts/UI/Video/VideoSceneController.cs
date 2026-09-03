using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class VideoSceneController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string levelToLoad = "FirstForest";

    void Start()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.Play();
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        LevelLoader.Instance.LoadLevelAsync(levelToLoad);
    }
}

