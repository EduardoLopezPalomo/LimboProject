using UnityEngine;

public class MapToggle : MonoBehaviour
{
    [SerializeField] private EyeAnimation miniEye;
    [SerializeField] private EyeAnimation bigEye;
    [SerializeField] private GameObject miniRoot;
    [SerializeField] private GameObject bigRoot;
    [SerializeField] private KeyCode key = KeyCode.M;

    private enum State {Mini, Busy, Big}
    private State state = State.Mini;

    private void Start()
    {
        miniRoot.SetActive(true);
        bigRoot.SetActive(false);
        miniEye.SetOpenInstant();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(key)) return;
        if (state == State.Mini) OpenBigMap();
        else if (state == State.Big) CloseBigMap();
    }

    private void OpenBigMap()
    {
        state = State.Busy;
        miniEye.PlayClose(() =>
        {
            miniRoot.SetActive(false);
            bigRoot.SetActive(true);
            bigEye.SetClosedInstant();
            Time.timeScale = 0f; // Pausa el juego
            bigEye.PlayOpen(() => state = State.Big);
        });
    }

    private void CloseBigMap()
    {
        state = State.Busy;
        bigEye.PlayClose(() =>
        {
            bigRoot.SetActive(false);
            Time.timeScale = 1f; // Reanuda el juego
            miniRoot.SetActive(true);
            miniEye.SetClosedInstant();
            miniEye.PlayOpen(() => state = State.Mini);
        });
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f; // Asegura que el juego se reanude si el objeto se destruye
    }
}