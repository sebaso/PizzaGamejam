using UnityEngine;
using UnityEngine.InputSystem;

public class PepperoniPlayer : PepperoniBase
{
    [Header("Input")]
    [Tooltip("Segundos que se recuerda una pulsación hecha durante el cooldown o el hitstun")]
    public float bufferEntrada = 0.15f;
    [Range(0.1f, 0.9f)] public float zonaMuertaStick = 0.3f;

    float tiempoPulsacionPendiente = -1f;
    bool botonMantenidoAntes;
    bool usandoMando;

    protected override void Start()
    {
        vidas = 3; // El jugador siempre empieza con 3
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
        if(vidas <= 0) GameManager.Instance.PlayerDied();

        // Se lee siempre, aunque no se pueda jugar, para no detectar pulsaciones fantasma
        bool botonMantenido = BotonAtaqueMantenido();

        // Bloquear input si estamos muertos o respawneando
        bool puedeJugar = currentState != State.Respawning
                       && GameManager.Instance.currentState == GameManager.GameState.Playing;

        if (puedeJugar)
        {
            Apuntar();
            InputAtaque(botonMantenido);
        }
        else
        {
            tiempoPulsacionPendiente = -1f;
        }

        botonMantenidoAntes = botonMantenido;
    }

    void Apuntar()
    {
        Gamepad mando = Gamepad.current;
        if (mando != null)
        {
            Vector2 stick = mando.leftStick.ReadValue();
            Vector2 stickDerecho = mando.rightStick.ReadValue();
            if (stickDerecho.sqrMagnitude > stick.sqrMagnitude) stick = stickDerecho;

            if (stick.sqrMagnitude > zonaMuertaStick * zonaMuertaStick)
            {
                usandoMando = true;
                RotarHacia(transform.position + DireccionDesdeCamara(stick));
                return;
            }
        }

        // Volver al ratón en cuanto se mueva
        if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 1f) usandoMando = false;
        if (!usandoMando) InputRaton();
    }

    // El stick hacia arriba apunta "hacia el fondo" de la pantalla, sea cual sea el ángulo de la cámara
    Vector3 DireccionDesdeCamara(Vector2 stick)
    {
        Transform cam = Camera.main != null ? Camera.main.transform : null;
        Vector3 adelante = cam != null ? Vector3.ProjectOnPlane(cam.forward, Vector3.up) : Vector3.forward;
        if (adelante.sqrMagnitude < 0.001f && cam != null) adelante = Vector3.ProjectOnPlane(cam.up, Vector3.up);
        adelante.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, adelante);
        return adelante * stick.y + derecha * stick.x;
    }

    void InputRaton()
    {
        if (Mouse.current == null || Camera.main == null) return;

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

    bool BotonAtaqueMantenido()
    {
        bool teclado = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        Gamepad mando = Gamepad.current;
        bool gamepad = mando != null && (mando.buttonSouth.isPressed || mando.rightTrigger.isPressed);
        return teclado || gamepad;
    }

    void InputAtaque(bool mantenido)
    {
        if (mantenido && !botonMantenidoAntes)
        {
            tiempoPulsacionPendiente = Time.time;
        }

        // Input buffer: si se pulsó hace poco y aún no se pudo cargar, se reintenta
        if (tiempoPulsacionPendiente >= 0f && currentState != State.Charging)
        {
            if (Time.time - tiempoPulsacionPendiente <= bufferEntrada)
            {
                EmpezarCarga();
                if (currentState == State.Charging)
                {
                    tiempoPulsacionPendiente = -1f;
                    // Fue un toque rápido que ya se soltó: sale con la fuerza mínima
                    if (!mantenido) LanzarAtaque();
                }
            }
            else
            {
                tiempoPulsacionPendiente = -1f;
            }
        }

        if (mantenido && currentState == State.Charging)
        {
            ProcesarCarga(Time.deltaTime);
        }

        if (!mantenido && botonMantenidoAntes && currentState == State.Charging)
        {
            LanzarAtaque();
        }
    }

    protected override void AlGolpear(PepperoniBase victima)
    {
        VibracionMando.Vibrar(0.2f, 0.6f, 0.08f);
    }

    protected override void AlRecibirGolpe(float intensidad)
    {
        VibracionMando.Vibrar(Mathf.Lerp(0.3f, 1f, intensidad), Mathf.Lerp(0.2f, 0.6f, intensidad), Mathf.Lerp(0.1f, 0.3f, intensidad));
    }


    public override void Morir()
{
    base.Morir();
    VibracionMando.Vibrar(0.8f, 0.4f, 0.4f);

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
