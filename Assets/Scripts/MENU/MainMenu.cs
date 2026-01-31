using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelPrincipal;
    public GameObject panelTutorial;

    void Start()
    {
        // Mostrar panel principal al inicio
        MostrarPanelPrincipal();
        
        // Asegurar que el tiempo está corriendo
        Time.timeScale = 1f;
    }

    public void BotonJugar()
    {
        SceneManager.LoadScene("SceneSebas"); // O EL QUE SEA
    }

    public void BotonTutorial()
    {
        panelPrincipal.SetActive(false);
        panelTutorial.SetActive(true);
    }

    public void BotonVolverDeTutorial()
    {
        MostrarPanelPrincipal();
    }

    public void BotonSalir()
    {
            Application.Quit();
    }

    void MostrarPanelPrincipal()
    {
        panelPrincipal.SetActive(true);
        panelTutorial.SetActive(false);
    }
}