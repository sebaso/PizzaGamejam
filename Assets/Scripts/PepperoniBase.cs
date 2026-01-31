using UnityEngine;
using TMPro;

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
    [Tooltip("Cuanto tarda en cargar la fuerza maxima (segundos)")]
    public float tiempoCargaMax = 0.8f; 

    [Header("Sistema de Daño")]
    public float porcentajeDaño = 0f;
    public float multiplicadorVuelo = 3.0f;
    public int vidas = 1; // Por defecto 1 (Enemigos)

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
    public float cooldownAtaque = 0.8f; // Tiempo de espera tras parar de deslizar
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
                rb.linearVelocity = Vector3.zero; // STOP TOTAL al cargar
                rb.angularVelocity = Vector3.zero; // NO MAS GIROS
                tiempoCargaActual = 0f;
                if (indicadorCarga) indicadorCarga.SetActive(true);
                break;

            case State.Attacking:
                rb.linearDamping = 1.2f; // Mas rozamiento para que no sea infinito (Antes 0.6)
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
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
                if (rend) rend.enabled = false;
                break;
        }
    }

    // --- ACCIONES PRINCIPALES ---

    public virtual void EmpezarCarga()
    {
        // CHEQUEOS:
        // 1. Solo si estoy quieto (Idle) o ya cargando
        if (currentState != State.Idle && currentState != State.Charging) return;
        
        // 2. Respetar Cooldown
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
        // INMUNIDAD: Si ataco, NO recibo daño.
        if (currentState == State.Attacking || currentState == State.Respawning || currentState == State.Hit) return;

        // Calcular daño
        porcentajeDaño += fuerzaBase; 
        ActualizarInterfaz();
        

        // Calcular direccion
        Vector3 direccion = (transform.position - posicionAgresor).normalized;
        direccion.y = 0.4f; // Mas arco hacia arriba

        // Formula: Cuanto mas daño, mas vuelas
        float factorVuelo = (porcentajeDaño / 10f) * multiplicadorVuelo;
        float fuerzaTotal = fuerzaBase + factorVuelo;

        SetState(State.Hit);
        stateTimer = 0.5f; // Tiempo aturdido

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero; // STOP SPINNING
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
            // LE PEGO
            // CLAMP DE DAÑO REDUCIDO: Entre 10 y 20. 
            float velocidad = collision.relativeVelocity.magnitude;
            float fuerza = Mathf.Clamp(velocidad, 10f, 20f);

            
            otro.RecibirGolpe(transform.position, fuerza);

            // Rebote controlado
            rb.linearVelocity = -transform.forward * 5f; 
            SetState(State.Idle);
        }
        else if (yoAtaco && elAtaca)
        {
            // CHOQUE (Clash)
            // Reboto hacia atras
            rb.linearVelocity = -transform.forward * 10f;
            SetState(State.Idle);
            // No hacemos daño, solo rebote mutuo
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

    protected virtual void Morir()
    {
        vidas--;
        Debug.Log($"💀 {name} MURIÓ. Vidas: {vidas}");
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
