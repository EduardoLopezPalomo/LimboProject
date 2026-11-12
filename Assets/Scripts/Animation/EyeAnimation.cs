using System;
using UnityEngine;

public class EyeAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [SerializeField, Tooltip("Keep the eyelid playing while Time.timeScale is 0. MapToggle pauses the game as it opens the map, and an Animator does not advance at timeScale 0: without this the close never finishes, its animation event never fires, and the map wedges halfway open. Ignored while the game is running normally, so nothing else changes.")]
    private bool playWhilePaused = true;

    private Action onDone;

    private void Update()
    {
        if (!playWhilePaused || animator == null) return;
        if (Time.timeScale >= 0.0001f) return;
        if (animator.isActiveAndEnabled == false || animator.gameObject.activeInHierarchy == false) return;

        animator.Update(Time.unscaledDeltaTime * animator.speed);
    }

    public void PlayClose(Action callback = null)
    {
        onDone = callback;
        animator.ResetTrigger("Open");
        animator.SetTrigger("Close");
    }

    public void PlayOpen(Action callback = null)
    {
        onDone = callback;
        animator.ResetTrigger("Close");
        animator.SetTrigger("Open");
    }

    // Salta directamente al último fotograma de cada animación
    public void SetClosedInstant() => animator.Play("Close", 0, 1f);
    public void SetOpenInstant() => animator.Play("Open", 0, 1f);

    // Los llaman los Animation Events (último fotograma de cada clip)
    public void OnCloseFinished() => Finish();
    public void OnOpenFinished() => Finish();

    private void Finish()
    {
        Action cb = onDone;
        onDone = null;
        cb?.Invoke();
    }
}