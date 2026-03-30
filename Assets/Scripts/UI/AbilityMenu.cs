using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AbilityMenu : MonoBehaviour
{
    public GameObject menuPanel; // Arrastra el Panel aquí
    public AbilityHolder abilityHolder; // Arrastra al Personaje aquí
    public GameObject buttonPrefab; // Un prefab de botón simple
    public Transform container; // El objeto con el Layout Group

    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isPaused = !isPaused;
        menuPanel.SetActive(isPaused);

        // Pausar o reanudar el tiempo
        Time.timeScale = isPaused ? 0f : 1f;

        if (isPaused)
        {
            Cursor.visible = true; // Hace que el ratón se vea
            Cursor.lockState = CursorLockMode.None; // Libera el ratón para que se mueva por la pantalla
            ActualizarListaHabilidades();
        }
        else
        {
            Cursor.visible = false; // Lo vuelve a ocultar al cerrar el menú
            Cursor.lockState = CursorLockMode.Locked; // Lo vuelve a bloquear (si tu juego es FPS/3ª persona)
        }

        if (isPaused)
        {
            ActualizarListaHabilidades();
        }
    }

    void ActualizarListaHabilidades()
    {
        // Limpiar botones antiguos
        foreach (Transform child in container) { Destroy(child.gameObject); }

        // Crear un botón por cada habilidad que tenga el personaje
        foreach (GameplayAbility ability in abilityHolder.OwnedAbilities)
        {
            GameObject btnObj = Instantiate(buttonPrefab, container);
            // Suponiendo que el botón tiene un componente Text en el hijo
            btnObj.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = ability.name;

            // Configurar el script del botón
            AbilityButton script = btnObj.AddComponent<AbilityButton>();
            script.Setup(ability, abilityHolder);
        }
    }
}
