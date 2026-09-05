using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoSceneController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string levelToLoad = "FirstForest";

    [Header("Skip por tecla mantenida")]
    public Image fillImage;
    public float holdDuration = 3f;
    public bool resetOnRelease = true;

    private float holdTimer = 0f;
    private bool hasSkipped = false;

    void Update()
    {
        if (hasSkipped) return;

        if (Input.anyKey)
        {
            holdTimer += Time.deltaTime;

            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(holdTimer / holdDuration);

            if (holdTimer >= holdDuration)
            {
                SkipCinematic();
            }
        }
        else
        {
            if (resetOnRelease)
            {
                holdTimer = 0f;
                if (fillImage != null)
                    fillImage.fillAmount = 0f;
            }
        }
    }

    void Start()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.Play();
        }

        if (fillImage != null)
            fillImage.fillAmount = 0f;
    }

    void SkipCinematic()
    {
        if (hasSkipped) return;
        hasSkipped = true;

        DayNight.Instance.CanChangePhase = true;
        LevelLoader.Instance.LoadLevelAsync(levelToLoad);
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        DayNight.Instance.CanChangePhase = true;
        LevelLoader.Instance.LoadLevelAsync(levelToLoad);
    }
}

