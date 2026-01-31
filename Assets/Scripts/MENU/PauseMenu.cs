using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject panelPausa;
    
    private bool estaPausado = false;

    void Start()
    {
        // Asegurar que el menú está oculto al inicio
        if (panelPausa != null)
        {
            panelPausa.SetActive(false);
        }
    }

    void Update()
    {
        // Detectar tecla ESC para pausar/despausar
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (estaPausado)
            {
                Reanudar();
            }
            else
            {
                Pausar();
            }
        }
    }

    public void Pausar()
    {
        estaPausado = true;
        panelPausa.SetActive(true);
        Time.timeScale = 0f; // Congela el juego
    }

    public void Reanudar()
    {
        estaPausado = false;
        panelPausa.SetActive(false);
        Time.timeScale = 1f; // Reanuda el juego
    }

    public void VolverAlMenu()
    {
        Time.timeScale = 1f; // Asegurar que el tiempo vuelva a la normalidad
        SceneManager.LoadScene("MainMenu"); // PONLO CON EL QUE SEA BUENO
    }

    public void Salir()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}