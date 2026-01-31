using UnityEngine;
using TMPro;
using Unity.VisualScripting;

[RequireComponent(typeof(Rigidbody))]
public class PepperoniBase : MonoBehaviour
{
    public enum State
    {
        Idle,
        Charging,
        Attacking,
        Hit,
        Respawning
    }

    [Header("Configuración del Smash")]
    public float velocidadGiro = 30f; 
    public float fuerzaMinima = 10f;  
    public float fuerzaMaxima = 50f; 
    public float tiempoCargaMax = 0.8f; 
    [Header("Detección de Suelo")]
    public bool estaEnEscenario;
    public float distanciaDeteccionSuelo = 1.5f;
    public LayerMask capaSuelo; //pizzasuelo

    [Header("Sistema de Daño")]
    public float porcentajeDaño = 0f;
    public float multiplicadorVuelo = 3.0f;
    public int vidas = 1; 

    [Header("Referencias Visuales")]
    public GameObject indicadorCarga;
    public TMP_Text textoDaño;
    public Renderer rend;

    protected Rigidbody rb;
    protected Color colorOriginal;
    
    // ESTADO ACTUAL
    public State currentState = State.Idle;
    protected float stateTimer = 0f; 

    // Variables de carga
    protected float tiempoCargaActual;

    [Header("Ajustes de Gameplay")]
    public float cooldownAtaque = 0.8f; 
    protected float tiempoUltimoFinalizacionAtaque = 0f;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rend == null) rend = GetComponentInChildren<Renderer>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        rb.mass = 1f; 

        if (rend != null) colorOriginal = rend.material.color;
        if (indicadorCarga) indicadorCarga.SetActive(false);

        ActualizarInterfaz();
        SetState(State.Idle);
    }

    protected virtual void Update()
    {
        ComprobarSuelo();

        if (transform.position.y < -5 && currentState != State.Respawning) 
        {
            Morir();
        }

        switch (currentState)
        {
            case State.Charging:
                ActualizarVisualesCarga();
                break;
            case State.Attacking:
                stateTimer += Time.deltaTime;
                if (stateTimer > 0.1f)
                {
                    if (rb.linearVelocity.magnitude < 0.5f) 
                    {
                        SetState(State.Idle);
                    }
                }
                break;
            case State.Hit:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0)
                {
                    SetState(State.Idle);
                }
                break;
        }
    }

    void ComprobarSuelo()
{
    estaEnEscenario = Physics.Raycast(transform.position, Vector3.down, distanciaDeteccionSuelo, capaSuelo);

    if (!estaEnEscenario && currentState != State.Respawning)
    {
        rb.AddForce(Vector3.down * 10f, ForceMode.Acceleration);
    }
}

    // --- MAQUINA DE ESTADOS ---

    public void SetState(State newState)
    {
        // Logica de SALIDA del estado anterior
        if (currentState == State.Attacking && newState == State.Idle)
        {
            tiempoUltimoFinalizacionAtaque = Time.time;
        }

        currentState = newState;
        stateTimer = 0f;

        switch (currentState)
        {
            case State.Idle:
                rb.linearDamping = 3f; // Freno fuerte
                rb.angularDamping = 5f; 
                if (rend) rend.material.color = colorOriginal;
                if (indicadorCarga) indicadorCarga.SetActive(false);
                break;

            case State.Charging:
                rb.linearDamping = 10f; 
                rb.angularDamping = 10f; 
                rb.linearVelocity = Vector3.zero; 
                rb.angularVelocity = Vector3.zero; 
                tiempoCargaActual = 0f;
                if (indicadorCarga) indicadorCarga.SetActive(true);
                break;

            case State.Attacking:
                rb.linearDamping = 1.2f; 
                rb.angularDamping = 0f; 
                if (rend) rend.material.color = Color.Lerp(colorOriginal, Color.black, 0.2f); 
                if (indicadorCarga) indicadorCarga.SetActive(false);
                break;

            case State.Hit:
                rb.linearDamping = 0.1f; 
                rb.angularDamping = 1f; 
                StartCoroutine(EfectoGolpeVisual());
                break;

            case State.Respawning:
            rb.isKinematic = false; 
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
    
            rb.isKinematic = true;
            if (rend) rend.enabled = false;
            break;
        }
    }

    // --- ACCIONES PRINCIPALES ---

    public virtual void EmpezarCarga()
    {
        if (!estaEnEscenario) return;

        if (currentState != State.Idle && currentState != State.Charging) return;

        if (Time.time < tiempoUltimoFinalizacionAtaque + cooldownAtaque) return;

        SetState(State.Charging);
    }

    public virtual void ProcesarCarga(float delta)
    {
        if (currentState != State.Charging) return;
        tiempoCargaActual += delta;
        if (tiempoCargaActual > tiempoCargaMax) tiempoCargaActual = tiempoCargaMax;
    }

    public virtual void LanzarAtaque()
    {
        if (currentState != State.Charging) return;

        float porcentaje = Mathf.Clamp01(tiempoCargaActual / tiempoCargaMax);
        float fuerzaFinal = Mathf.Lerp(fuerzaMinima, fuerzaMaxima, porcentaje);

        SetState(State.Attacking);

        // Impulso hacia adelante
        rb.linearVelocity = Vector3.zero; 
        rb.AddForce(transform.forward * fuerzaFinal, ForceMode.Impulse);
    }

    public void RecibirGolpe(Vector3 posicionAgresor, float fuerzaBase)
            {
    if (currentState == State.Attacking || 
    currentState == State.Respawning || 
    currentState == State.Hit) 
    return;

    porcentajeDaño += fuerzaBase; 
    ActualizarInterfaz();

    Vector3 direccion = (transform.position - posicionAgresor);
    direccion.y = 0; 
    direccion.Normalize();
    
    direccion.y = 0.15f; 

    float factorVuelo = (porcentajeDaño / 10f) * multiplicadorVuelo;
    float fuerzaTotal = fuerzaBase + factorVuelo;

    SetState(State.Hit);
    stateTimer = 0.5f;

    rb.linearVelocity = Vector3.zero;
    rb.AddForce(direccion * fuerzaTotal, ForceMode.Impulse);
}

    protected void RotarHacia(Vector3 destino)
    {
        Vector3 direccion = (destino - transform.position).normalized;
        direccion.y = 0;
        if (direccion != Vector3.zero)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * velocidadGiro);
        }
    }

    // --- COLISIONES ---

    void OnCollisionEnter(Collision collision)
    {
        PepperoniBase otro = collision.gameObject.GetComponent<PepperoniBase>();
        
        // --- PAREDES ---
        if (otro == null)
        {
            // Freno en seco si choco atacando
            if (currentState == State.Attacking)
            {
                rb.linearVelocity = Vector3.zero;
                SetState(State.Idle);
            }
            return;
        }

        if (currentState == State.Respawning || otro.currentState == State.Respawning) return;

        // --- RIVALES ---
        bool yoAtaco = currentState == State.Attacking;
        bool elAtaca = otro.currentState == State.Attacking;

        if (yoAtaco && !elAtaca)
        {
            // LE PEGO PUM PIUM PAPAPAPAPUM
            float velocidad = collision.relativeVelocity.magnitude;
            float fuerza = Mathf.Clamp(velocidad, 10f, 20f);

            
            otro.RecibirGolpe(transform.position, fuerza);

            // Rebote controlado
            rb.linearVelocity = -transform.forward * 5f; 
            SetState(State.Idle);
        }
        else if (yoAtaco && elAtaca)
        {
            // CHOQUE 
            rb.linearVelocity = -transform.forward * 10f;
            SetState(State.Idle);
        }
    }

    // --- UTILIDADES ---

    protected void ActualizarInterfaz()
    {
        if (textoDaño != null)
        {
            textoDaño.text = Mathf.RoundToInt(porcentajeDaño) + "%";
            textoDaño.color = Color.Lerp(Color.white, Color.red, porcentajeDaño / 100f);
        }
    }

    void ActualizarVisualesCarga()
    {
        float p = Mathf.Clamp01(tiempoCargaActual / tiempoCargaMax);
        if (rend) rend.material.color = Color.Lerp(colorOriginal, Color.red, p);
        if (indicadorCarga) 
            indicadorCarga.transform.localScale = Vector3.one * (1f + p);
    }

    System.Collections.IEnumerator EfectoGolpeVisual()
    {
        if (rend) rend.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (currentState != State.Charging && currentState != State.Attacking && rend) 
            rend.material.color = colorOriginal;
    }

    public virtual void Morir()
    {
        vidas--;
        Debug.Log($"PLUH... {name} MURIÓ. Vidas: {vidas}");
        SetState(State.Respawning);

        if (vidas > 0) StartCoroutine(RespawnRoutine());
        else gameObject.SetActive(false);
    }

    System.Collections.IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(2.0f);
        
        porcentajeDaño = 0;
        ActualizarInterfaz();

        // Respawn position
        Vector2 rnd = Random.insideUnitCircle * 3f;
        transform.position = new Vector3(rnd.x, 5f, rnd.y);
        
        rb.isKinematic = false;
        if (rend) rend.enabled = true;
        
        SetState(State.Idle);
    }
}
