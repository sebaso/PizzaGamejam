using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject panelPausa;
    
    private bool estaPausado = false;


    [Header("Game Over UI")]
    public GameObject panelGameOver;

    [Header("Victoria Menu UI")]
    public GameObject panelVictoria;

    public static PauseMenu instancia;

    void Awake()
    {
        instancia = this;
    }

    void Start()
    {
        if (panelPausa != null)
        {
            panelPausa.SetActive(false);
        }
    }

    void Update()
    {
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
        Time.timeScale = 0f; 
    }

    public void Reanudar()
    {
        estaPausado = false;
        panelPausa.SetActive(false);
        Time.timeScale = 1f; 
    }

    public void VolverAlMenu()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene("MenuPrincipal"); 
    }

    public void Salir()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void MostrarVictoria()
    {
        if (panelVictoria != null)
        {
            panelVictoria.SetActive(true);
            Time.timeScale = 0f; 
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
    }
    public void MostrarGameOver()
{
    
    if (panelGameOver != null)
    {
        panelGameOver.SetActive(true);
        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
}
}