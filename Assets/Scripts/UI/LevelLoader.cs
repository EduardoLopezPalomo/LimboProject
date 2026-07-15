using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


public class LevelLoader : MonoBehaviour
{
    public Animator animator;
    public float transitionTime = 1f;
    public float MusicFadeTime = 2f;
    public int sceneIndex;
    
    [SerializeField] private PlayerInput playerInput;
    
    public void LoadLevel()
    {
       StartCoroutine(LoadLevel(sceneIndex));
    }

    IEnumerator LoadLevel(int levelIndex)
    {
        animator.SetTrigger("StartLoading");
        playerInput.DeactivateInput();
        SoundMixerManager.instance.FadeOutVolume("MasterVolume",MusicFadeTime);
        
        yield return new WaitForSeconds(transitionTime);
        
        SoundMixerManager.instance.FadeInVolume("MasterVolume", MusicFadeTime);
        SceneManager.LoadScene(levelIndex);
        playerInput.ActivateInput();
    }
}
