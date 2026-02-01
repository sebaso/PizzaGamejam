using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelPrincipal;
    public GameObject panelTutorial;
    public GameObject panelCreditos; // NUEVA REFERENCIA

    void Start()
    {
        MostrarPanelPrincipal();
        Time.timeScale = 1f;
    }

    public void BotonJugar()
    {
        SceneManager.LoadScene("ScenePepe");
    }

    public void BotonCreditos()
    {
        panelPrincipal.SetActive(false);
        panelCreditos.SetActive(true);
    }

    public void BotonVolverDeCreditos()
    {
        MostrarPanelPrincipal();
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
        panelCreditos.SetActive(false); 
    }
}