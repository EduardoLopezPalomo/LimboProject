using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


public class LevelLoader : MonoBehaviour
{
    public Animator animator;
    public float transitionTime = 1f;
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
        
        yield return new WaitForSeconds(transitionTime);
        
        SceneManager.LoadScene(levelIndex);
        playerInput.ActivateInput();
    }
}
