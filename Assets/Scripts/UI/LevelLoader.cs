using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


public class LevelLoader : MonoBehaviour
{
    public Animator animator;
    public float transitionTime = 1f;
    public float MusicFadeTime = 2f;
    
    [SerializeField] private PlayerInput playerInput;
    
    public void LoadLevel(int index)
    {
       StartCoroutine(LoadLevelCoroutine(index));
    }

    IEnumerator LoadLevelCoroutine(int levelIndex)
    {
        animator.SetTrigger("StartLoading");
        playerInput.DeactivateInput();
        float MaxVolume = SoundMixerManager.instance.GetMasterVolume();
        SoundMixerManager.instance.FadeVolumeToTarget("MasterVolume",-80f, MusicFadeTime);
        
        yield return new WaitForSeconds(transitionTime);
        
        SoundMixerManager.instance.FadeVolumeToTarget("MasterVolume", MaxVolume, MusicFadeTime);
        SceneManager.LoadScene(levelIndex);
        playerInput.ActivateInput();
    }
}
