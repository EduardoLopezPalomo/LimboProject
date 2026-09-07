using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CreditsSceneController : MonoBehaviour
{
    public Animator animator;
    public string animationClipName = "CreditAnim";  
    public string levelToLoad = "Main Menu";       

    [Header("Skip by holding key")]
    public Image fillImage;          
    public float holdDuration = 3f;  
    public bool resetOnRelease = true;

    private float holdTimer = 0f;
    private bool hasSkipped = false;
    private bool animationPlaying = false;

    void Start()
    {
        if (animator == null)
        {
            Debug.LogError("Animator no asignado en CreditsSceneController");
            return;
        }

        animator.Play(animationClipName);
        animationPlaying = true;

        if (fillImage != null)
            fillImage.fillAmount = 0f;

     
    }

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
                SkipCredits();
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

        
        if (animationPlaying)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(animationClipName) && state.normalizedTime >= 1f)
            {
                animationPlaying = false;
                EndCredits();
            }
        }
    }

    void SkipCredits()
    {
        if (hasSkipped) return;
        hasSkipped = true;

        EndCredits();
    }

    void EndCredits()
    {
        DayNight.Instance.CanChangePhase = true;
        LevelLoader.Instance.LoadLevelAsync(levelToLoad);
    }
}


