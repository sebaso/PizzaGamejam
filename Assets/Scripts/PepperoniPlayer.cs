using UnityEngine;
using UnityEngine.InputSystem;

public class PepperoniPlayer : PepperoniBase
{
    protected override void Start()
    {
        vidas = 3; // El jugador siempre empieza con 3
        base.Start(); 
    }

    protected override void Update()
    {
        base.Update();
        if(vidas <= 0) GameManager.Instance.PlayerDied();
        // Bloquear input si estamos muertos o respawneando
        if (currentState == State.Respawning) return;
        if(GameManager.Instance.currentState != GameManager.GameState.Playing) return;

        InputRaton();
        InputTeclado();
    }

    void InputRaton()
    {

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        Plane planoSuelo = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
        float distancia;

        if (planoSuelo.Raycast(ray, out distancia))
        {
            Vector3 puntoObjetivo = ray.GetPoint(distancia);
            RotarHacia(puntoObjetivo);
        }
    }

    void InputTeclado()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame) 
        {
            EmpezarCarga();
        }
        
        if (Keyboard.current.spaceKey.isPressed && currentState == State.Charging) 
        {
            ProcesarCarga(Time.deltaTime);
        }
        
        if (Keyboard.current.spaceKey.wasReleasedThisFrame && currentState == State.Charging) 
        {
            LanzarAtaque();
        }
    }


    public override void Morir()
{
    base.Morir(); 

    if (vidas <= 0)
    {
        PauseMenu menu = FindFirstObjectByType<PauseMenu>();
        if (menu != null)
        {
            menu.MostrarGameOver();
        }
    }
}
}