using UnityEngine;
using UnityEngine.InputSystem;

public class PepperoniController : MonoBehaviour
{
    [Header("Configuración del Smash")]
    public float velocidadGiro = 10f;
    public float fuerzaMinima = 5f;
    public float fuerzaMaxima = 25f;
    public float tiempoCargaMax = 2f;

    [Header("Referencias Visuales")]
    public GameObject indicadorCarga;
    public bool esJugador = true;

    private Rigidbody rb;
    private float tiempoCargaActual;
    private bool cargando;
    private Renderer rend;
    private Color colorOriginal;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();
        colorOriginal = rend.material.color;
        if (indicadorCarga) indicadorCarga.SetActive(false);
    }

    void Update()
    {
        if (!esJugador) return;

        MiraAlRaton();
        GestionarCarga();
    }

    void MiraAlRaton()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        Plane planoSuelo = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
        float distancia;

        if (planoSuelo.Raycast(ray, out distancia))
        {
            Vector3 puntoObjetivo = ray.GetPoint(distancia);
            Vector3 direccion = (puntoObjetivo - transform.position).normalized;
            direccion.y = 0;

            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * velocidadGiro);
        }
    }

    void GestionarCarga()
    {
        bool espacioPresionado = Keyboard.current.spaceKey.isPressed;
        bool espacioEmpezado = Keyboard.current.spaceKey.wasPressedThisFrame;
        bool espacioSoltado = Keyboard.current.spaceKey.wasReleasedThisFrame;

        if (espacioEmpezado)
        {
            cargando = true;
            tiempoCargaActual = 0f;
            rb.mass = 0.5f;
            if (indicadorCarga) indicadorCarga.SetActive(true);
        }

        if (espacioPresionado && cargando)
        {
            tiempoCargaActual += Time.deltaTime;
            float porcentaje = Mathf.Clamp01(tiempoCargaActual / tiempoCargaMax);
            rend.material.color = Color.Lerp(colorOriginal, Color.red, porcentaje);

            if (indicadorCarga)
                indicadorCarga.transform.localScale = new Vector3(1, 1, 0.5f + (porcentaje * 2f));
        }

        if (espacioSoltado && cargando)
        {
            Lanzarse();
        }
    }

    void Lanzarse()
    {
        cargando = false;
        rb.mass = 1f;
        rend.material.color = colorOriginal;
        if (indicadorCarga) indicadorCarga.SetActive(false);

        float porcentajeCarga = Mathf.Clamp01(tiempoCargaActual / tiempoCargaMax);
        float fuerzaFinal = Mathf.Lerp(fuerzaMinima, fuerzaMaxima, porcentajeCarga);

        rb.AddForce(transform.forward * fuerzaFinal, ForceMode.Impulse);
    }
}
