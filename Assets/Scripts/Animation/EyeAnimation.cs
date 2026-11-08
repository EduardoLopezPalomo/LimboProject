using System;
using UnityEngine;

public class EyeAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private Action onDone;

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