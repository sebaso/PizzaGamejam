using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PepperoniBase : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip sonidoGolpe;
    public AudioClip sonidoSlash;
    protected AudioSource audioSource;

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
    public LayerMask capaSuelo;
    public float gravedadExtra = 10f;

    [Header("Sistema de Daño")]
    public float porcentajeDaño = 0f;
    public float multiplicadorVuelo = 3.0f;
    public int vidas = 1;

    [Header("Física")]
    [Tooltip("Más masa = sale volando menos al recibir golpes. El ataque no se ve afectado.")]
    public float masa = 1f;

    [Header("Knockback")]
    [Tooltip("0 = sale despedido según la posición; 1 = en la dirección en que venía el atacante")]
    [Range(0f, 1f)] public float pesoDireccionAtacante = 0.5f;
    public float elevacionMinima = 0.15f;
    public float elevacionMaxima = 0.35f;
    public float hitstunMinimo = 0.25f;
    public float hitstunMaximo = 0.9f;

    [Header("Choque entre ataques")]
    [Tooltip("Cuánto más rápido tiene que ir un atacante para ganar el choque")]
    public float ventajaParaGanarChoque = 1.25f;

    [Header("Game Feel")]
    public float hitstopMinimo = 0.03f;
    public float hitstopMaximo = 0.12f;
    public float shakeMinimo = 0.05f;
    public float shakeMaximo = 0.35f;
    [Tooltip("Color del destello al recibir golpe (valores >1 lo hacen más brillante)")]
    public Color colorDestello = new Color(1.8f, 1.8f, 1.8f, 1f);

    [Header("Feedback de Carga")]
    [Tooltip("Grados máximos que tiembla la pizza con la carga al máximo")]
    public float temblorCargaMax = 4f;
    [Tooltip("Velocidad del temblor de carga")]
    public float frecuenciaTemblorCarga = 22f;
    [Tooltip("Opcional: sonido al llegar a carga máxima")]
    public AudioClip sonidoCargaCompleta;

    [Header("Estela")]
    [Tooltip("Opcional en el Editor; en builds conviene asignarlo (p.ej. un material URP Particles/Unlit)")]
    public Material materialEstela;
    public float duracionEstela = 0.25f;
    [Tooltip("% de daño a partir del cual deja estela al salir despedido")]
    public float umbralEstelaDaño = 60f;

    [Header("Sonido")]
    public float pitchMinimo = 0.9f;
    public float pitchMaximo = 1.25f;
    public float variacionPitch = 0.06f;

    [Header("Referencias Visuales")]
    public GameObject indicadorCarga;
    public TMP_Text textoDaño;
    public Image imageDaño;
    public Renderer rend;

    protected Rigidbody rb;
    protected Color colorOriginal;


    public State currentState = State.Idle;
    protected float stateTimer = 0f;


    protected float tiempoCargaActual;

    [Header("Ajustes de Gameplay")]
    public float cooldownAtaque = 0.8f;
    protected float tiempoUltimoFinalizacionAtaque = 0f;

    // Velocidad antes del paso de física: en OnCollisionEnter rb.linearVelocity ya viene resuelta
    protected Vector3 velocidadPreImpacto;

    Quaternion rotacionObjetivo;
    bool hayRotacionObjetivo;

    // Orientación de apuntado durante la carga, sin el temblor visual
    Quaternion rotacionApuntado;
    Quaternion rotacionApuntadoPrevia;
    float semillaTemblor;

    // Pose del indicador relativa al cuerpo, para mantenerlo quieto mientras el cuerpo tiembla
    Quaternion rotacionRelativaIndicador = Quaternion.identity;
    Vector3 offsetIndicador;
    float destelloCargaHasta;
    float popIndicadorHasta;
    const float DURACION_POP_INDICADOR = 0.12f;

    TrailRenderer estela;
    bool estelaModoVuelo;

    Vector3 escalaTextoOriginal = Vector3.one;
    Vector3 posicionTextoOriginal;
    Coroutine popTexto;

    // Fuerza total de golpe a partir de la cual el feedback es máximo
    const float FUERZA_FEEDBACK_MAX = 50f;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (rend == null) rend = GetComponentInChildren<Renderer>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.mass = masa;

        if (rend != null) colorOriginal = rend.material.color;
        if (indicadorCarga) indicadorCarga.SetActive(false);
        if (textoDaño != null)
        {
            escalaTextoOriginal = textoDaño.transform.localScale;
            posicionTextoOriginal = textoDaño.transform.localPosition;
        }
        rotacionApuntado = rotacionApuntadoPrevia = rb.rotation;
        semillaTemblor = Random.value * 100f;
        if (indicadorCarga)
        {
            Quaternion inversa = Quaternion.Inverse(transform.rotation);
            rotacionRelativaIndicador = inversa * indicadorCarga.transform.rotation;
            offsetIndicador = inversa * (indicadorCarga.transform.position - transform.position);
        }
        CrearEstela();

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

        ActualizarEstela();
    }

    protected virtual void LateUpdate()
    {
        if (currentState != State.Charging || !indicadorCarga) return;

        // La línea muestra siempre la puntería real, sin el temblor del cuerpo.
        // Se interpola entre pasos de física igual que hace el Rigidbody.
        float t = Time.fixedDeltaTime > 0f ? Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime) : 1f;
        Quaternion apuntadoVisual = Quaternion.Slerp(rotacionApuntadoPrevia, rotacionApuntado, t);
        indicadorCarga.transform.SetPositionAndRotation(
            transform.position + apuntadoVisual * offsetIndicador,
            apuntadoVisual * rotacionRelativaIndicador);
    }

    protected virtual void FixedUpdate()
    {
        if (currentState == State.Respawning) return;

        ComprobarSuelo();

        if (currentState == State.Charging)
        {
            // El apuntado real va por separado; el temblor es solo visual
            rotacionApuntadoPrevia = rotacionApuntado;
            if (hayRotacionObjetivo)
                rotacionApuntado = Quaternion.Slerp(rotacionApuntado, rotacionObjetivo, Time.fixedDeltaTime * velocidadGiro);
            rb.MoveRotation(rotacionApuntado * Quaternion.Euler(0f, TemblorCarga(), 0f));
        }
        else if (hayRotacionObjetivo)
        {
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, rotacionObjetivo, Time.fixedDeltaTime * velocidadGiro));
        }

        velocidadPreImpacto = rb.linearVelocity;
    }

    void ComprobarSuelo()
    {
        estaEnEscenario = Physics.Raycast(rb.position, Vector3.down, distanciaDeteccionSuelo, capaSuelo);

        if (!estaEnEscenario)
        {
            rb.AddForce(Vector3.down * gravedadExtra, ForceMode.Acceleration);
        }
    }



    public void SetState(State newState)
    {

        if (currentState == State.Attacking && newState == State.Idle)
        {
            tiempoUltimoFinalizacionAtaque = Time.time;
        }

        currentState = newState;
        stateTimer = 0f;

        switch (currentState)
        {
            case State.Idle:
                rb.linearDamping = 3f;
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
                rotacionApuntado = rotacionApuntadoPrevia = rb.rotation;
                destelloCargaHasta = 0f;
                popIndicadorHasta = 0f;
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
                if (indicadorCarga) indicadorCarga.SetActive(false);
                StartCoroutine(EfectoGolpeVisual());
                break;

            case State.Respawning:
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = true;
            hayRotacionObjetivo = false;
            if (estela) { estela.emitting = false; estela.Clear(); }
            if (rend) rend.enabled = false;
            break;
        }
    }



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
        bool yaEstabaLlena = tiempoCargaActual >= tiempoCargaMax;
        tiempoCargaActual += delta;
        if (tiempoCargaActual > tiempoCargaMax) tiempoCargaActual = tiempoCargaMax;
        if (!yaEstabaLlena && tiempoCargaActual >= tiempoCargaMax) AlCompletarCarga();
    }

    public virtual void LanzarAtaque()
    {
        if (currentState != State.Charging) return;

        float porcentaje = Mathf.Clamp01(tiempoCargaActual / tiempoCargaMax);
        float fuerzaFinal = Mathf.Lerp(fuerzaMinima, fuerzaMaxima, porcentaje);

        // Dirección de apuntado sin el temblor de la carga
        Quaternion apuntado = rotacionApuntado;
        SetState(State.Attacking);
        rb.rotation = apuntado;

        Vector3 direccion = apuntado * Vector3.forward;
        direccion.y = 0f;
        direccion.Normalize();

        rb.linearVelocity = Vector3.zero;
        // VelocityChange: el lanzamiento es igual de rápido pese lo que pese el personaje
        rb.AddForce(direccion * fuerzaFinal, ForceMode.VelocityChange);
        // Por si choca en el primer paso de física tras el lanzamiento
        velocidadPreImpacto = direccion * fuerzaFinal;
    }

    public void RecibirGolpe(Vector3 posicionAgresor, float fuerzaBase)
    {
        RecibirGolpe(posicionAgresor, fuerzaBase, Vector3.zero, false);
    }

    /// <param name="direccionAgresor">Dirección en la que se movía el atacante (Vector3.zero si no aplica)</param>
    /// <param name="ignorarArmadura">Permite golpear a alguien que está atacando (choque perdido)</param>
    public void RecibirGolpe(Vector3 posicionAgresor, float fuerzaBase, Vector3 direccionAgresor, bool ignorarArmadura)
    {
        if (currentState == State.Respawning || currentState == State.Hit) return;
        if (currentState == State.Attacking && !ignorarArmadura) return;

        porcentajeDaño += fuerzaBase;
        ActualizarInterfaz();

        Vector3 direccion = transform.position - posicionAgresor;
        direccion.y = 0;
        direccion.Normalize();

        direccionAgresor.y = 0;
        if (direccionAgresor.sqrMagnitude > 0.01f)
        {
            Vector3 mezcla = Vector3.Lerp(direccion, direccionAgresor.normalized, pesoDireccionAtacante);
            if (mezcla.sqrMagnitude > 0.01f) direccion = mezcla.normalized;
        }

        // Cuanto más daño acumulado, más sale por los aires
        direccion.y = Mathf.Lerp(elevacionMinima, elevacionMaxima, porcentajeDaño / 150f);

        float factorVuelo = (porcentajeDaño / 10f) * multiplicadorVuelo;
        float fuerzaTotal = fuerzaBase + factorVuelo;
        float intensidad = Mathf.Clamp01(fuerzaTotal / FUERZA_FEEDBACK_MAX);

        ReproducirGolpe(intensidad);

        SetState(State.Hit);
        stateTimer = Mathf.Lerp(hitstunMinimo, hitstunMaximo, intensidad);

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(direccion * fuerzaTotal, ForceMode.Impulse);

        HitStop.Congelar(Mathf.Lerp(hitstopMinimo, hitstopMaximo, intensidad));
        CameraShake.Sacudir(Mathf.Lerp(shakeMinimo, shakeMaximo, intensidad));
        PopTextoDaño(intensidad);
        AlRecibirGolpe(intensidad);
    }

    protected void RotarHacia(Vector3 destino)
    {
        Vector3 direccion = destino - transform.position;
        direccion.y = 0;
        if (direccion.sqrMagnitude > 0.0001f)
        {
            // Se aplica en FixedUpdate con rb.MoveRotation para no pelearse con la física
            rotacionObjetivo = Quaternion.LookRotation(direccion.normalized);
            hayRotacionObjetivo = true;
        }
    }

    // Permite a las subclases excluir objetivos (p.ej. que Roni no golpee al jugador)
    protected virtual bool PuedeGolpear(PepperoniBase otro) => true;

    // Se llama cuando este personaje conecta un golpe
    protected virtual void AlGolpear(PepperoniBase victima) { }

    // Se llama cuando este personaje recibe un golpe (intensidad 0-1)
    protected virtual void AlRecibirGolpe(float intensidad) { }

    void OnCollisionEnter(Collision collision)
    {
        PepperoniBase otro = collision.gameObject.GetComponent<PepperoniBase>();


        if (otro == null)
        {
            // Solo una pared detiene la embestida; el suelo o un bache no
            if (currentState == State.Attacking && EsChoqueLateral(collision))
            {
                rb.linearVelocity = Vector3.zero;
                SetState(State.Idle);
            }
            return;
        }

        if (currentState == State.Respawning || otro.currentState == State.Respawning) return;


        bool yoAtaco = currentState == State.Attacking;
        bool elAtaca = otro.currentState == State.Attacking;

        if (yoAtaco && !elAtaca)
        {
            if (!PuedeGolpear(otro)) return;
            GolpearA(otro);
        }
        else if (yoAtaco && elAtaca)
        {
            // Lo resuelve entero el primero que recibe la colisión;
            // cuando le llegue al otro, ya no estarán los dos atacando.
            ResolverChoque(otro);
        }
    }

    void GolpearA(PepperoniBase otro)
    {
        float velocidad = velocidadPreImpacto.magnitude;
        float fuerza = Mathf.Clamp(velocidad, 10f, 20f);

        otro.RecibirGolpe(transform.position, fuerza, velocidadPreImpacto, currentState == State.Attacking && otro.currentState == State.Attacking);
        if (otro.CompareTag("Enemy"))
        {
            ReproducirGolpe(Mathf.InverseLerp(10f, 20f, fuerza));
        }
        AlGolpear(otro);

        Retroceder(5f);
    }

    void ResolverChoque(PepperoniBase otro)
    {
        float miVelocidad = velocidadPreImpacto.magnitude;
        float suVelocidad = otro.velocidadPreImpacto.magnitude;

        if (miVelocidad > suVelocidad * ventajaParaGanarChoque && PuedeGolpear(otro))
        {
            GolpearA(otro);
        }
        else if (suVelocidad > miVelocidad * ventajaParaGanarChoque && otro.PuedeGolpear(this))
        {
            otro.GolpearA(this);
        }
        else
        {
            Retroceder(10f);
            otro.Retroceder(10f);
        }
    }

    void Retroceder(float velocidad)
    {
        Vector3 atras = -(rb.rotation * Vector3.forward);
        atras.y = 0f;
        rb.linearVelocity = atras.normalized * velocidad;
        SetState(State.Idle);
    }

    static bool EsChoqueLateral(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Mathf.Abs(collision.GetContact(i).normal.y) < 0.5f) return true;
        }
        return false;
    }

    // ---------- Game feel ----------

    void ReproducirGolpe(float intensidad)
    {
        if (audioSource == null || sonidoGolpe == null) return;
        // Más agudo cuanto más fuerte, con algo de variación para que no suene siempre igual
        audioSource.pitch = Mathf.Lerp(pitchMinimo, pitchMaximo, intensidad)
                          + Random.Range(-variacionPitch, variacionPitch);
        audioSource.PlayOneShot(sonidoGolpe);
    }

    float TemblorCarga()
    {
        float p = Mathf.Clamp01(tiempoCargaActual / tiempoCargaMax);
        // Ruido continuo (no saltos aleatorios): con la interpolación se ve como vibración, no como caos
        float ruido = Mathf.PerlinNoise(Time.time * frecuenciaTemblorCarga, semillaTemblor) * 2f - 1f;
        return ruido * Mathf.Lerp(0.3f, temblorCargaMax, p * p);
    }

    void AlCompletarCarga()
    {
        destelloCargaHasta = Time.time + 0.08f;
        popIndicadorHasta = Time.time + DURACION_POP_INDICADOR;

        if (audioSource != null && sonidoCargaCompleta != null)
        {
            audioSource.pitch = 1.2f;
            audioSource.PlayOneShot(sonidoCargaCompleta);
        }
    }

    void CrearEstela()
    {
        Material mat = materialEstela;
        if (mat == null)
        {
            // Fallback para el Editor; en builds puede no estar incluido
            Shader sh = Shader.Find("Sprites/Default");
            if (sh != null) mat = new Material(sh);
        }
        if (mat == null) return;

        GameObject go = new GameObject("Estela");
        go.transform.SetParent(transform, false);
        estela = go.AddComponent<TrailRenderer>();
        estela.sharedMaterial = mat;
        estela.time = duracionEstela;
        estela.minVertexDistance = 0.1f;
        estela.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        float ancho = rend != null ? Mathf.Max(rend.bounds.size.x, rend.bounds.size.z) * 0.6f : 0.6f;
        estela.widthMultiplier = Mathf.Max(0.1f, ancho);
        estela.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        estela.receiveShadows = false;
        estela.emitting = false;
        PonerColorEstela(false);
    }

    void PonerColorEstela(bool vuelo)
    {
        estelaModoVuelo = vuelo;
        // Embestida: color del personaje. Salir despedido: humo blanco
        Color c = vuelo ? new Color(1f, 1f, 1f, 0.7f) : new Color(colorOriginal.r, colorOriginal.g, colorOriginal.b, 0.6f);
        estela.startColor = c;
        estela.endColor = new Color(c.r, c.g, c.b, 0f);
    }

    void ActualizarEstela()
    {
        if (!estela) return;

        float velocidad = rb.linearVelocity.magnitude;
        bool enAtaque = currentState == State.Attacking && velocidad > 3f;
        bool enVuelo = currentState == State.Hit && porcentajeDaño >= umbralEstelaDaño && velocidad > 6f;

        if ((enAtaque || enVuelo) && enVuelo != estelaModoVuelo) PonerColorEstela(enVuelo);
        estela.emitting = enAtaque || enVuelo;
    }

    void PopTextoDaño(float intensidad)
    {
        if (textoDaño == null || !isActiveAndEnabled) return;
        if (popTexto != null) StopCoroutine(popTexto);
        popTexto = StartCoroutine(RutinaPopTexto(intensidad));
    }

    System.Collections.IEnumerator RutinaPopTexto(float intensidad)
    {
        Transform t = textoDaño.transform;
        const float duracion = 0.3f;
        float escalaExtra = Mathf.Lerp(0.25f, 0.7f, intensidad);
        float temblor = Mathf.Lerp(3f, 12f, intensidad);
        float transcurrido = 0f;

        while (transcurrido < duracion)
        {
            float k = transcurrido / duracion;
            // Salta de golpe y vuelve suave a su tamaño
            float punch = (1f - k) * (1f - k);
            t.localScale = escalaTextoOriginal * (1f + escalaExtra * punch);
            t.localPosition = posicionTextoOriginal + (Vector3)(Random.insideUnitCircle * temblor * (1f - k));
            // Tiempo real: el pop se ve también durante el hitstop
            transcurrido += Time.unscaledDeltaTime;
            yield return null;
        }

        RestaurarTexto();
    }

    void RestaurarTexto()
    {
        popTexto = null;
        if (textoDaño == null) return;
        textoDaño.transform.localScale = escalaTextoOriginal;
        textoDaño.transform.localPosition = posicionTextoOriginal;
    }

    protected virtual void OnDisable()
    {
        // Si se desactiva a mitad del pop, que el texto no se quede deformado
        if (popTexto != null) RestaurarTexto();
    }



    protected void ActualizarInterfaz()
    {
        if (textoDaño != null)
        {
            textoDaño.text = Mathf.RoundToInt(porcentajeDaño) + "%";
            textoDaño.color = Color.Lerp(Color.white, Color.red, porcentajeDaño / 100f);
            if(imageDaño != null)
                imageDaño.fillAmount = porcentajeDaño/100f;
        }
    }

    void ActualizarVisualesCarga()
    {
        float p = Mathf.Clamp01(tiempoCargaActual / tiempoCargaMax);
        if (rend)
            rend.material.color = Time.time < destelloCargaHasta ? colorDestello : Color.Lerp(colorOriginal, Color.red, p);
        if (indicadorCarga)
        {
            float pop = Mathf.Clamp01((popIndicadorHasta - Time.time) / DURACION_POP_INDICADOR) * 0.4f;
            indicadorCarga.transform.localScale = Vector3.one * (1f + p + pop);
        }
    }

    System.Collections.IEnumerator EfectoGolpeVisual()
    {
        if (rend) rend.material.color = colorDestello;
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


        Vector2 rnd = Random.insideUnitCircle * 3f;
        Vector3 puntoRespawn = new Vector3(rnd.x, 5f, rnd.y);
        rb.position = puntoRespawn;
        transform.position = puntoRespawn;
        rotacionApuntado = rotacionApuntadoPrevia = rb.rotation;
        if (estela) estela.Clear();

        rb.isKinematic = false;
        if (rend) rend.enabled = true;

        SetState(State.Idle);
    }
}
