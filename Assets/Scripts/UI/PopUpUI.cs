using UnityEngine;
using TMPro;

public class PopUpUI : MonoBehaviour
{
    public TMP_Text infoDelPopUp;

    public void MostrarMensaje(string mensaje)
    {
        infoDelPopUp.text = mensaje;
    }
}
