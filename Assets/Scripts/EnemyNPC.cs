using System.Collections;
using UnityEngine;

public class EnemyNPC : MonoBehaviour
{
    [Header("Ring")]
    [Tooltip("Altura Y a partir de la cual el enemigo muere")]
    public float alturaMuerte = -0.10f;

    [Header("Objetivo")]
    public string tagJugador = "Player";

    [Header("Detección")]
    public float radioDeteccion = 15f;
    public float distanciaAtaque = 6f;

    [Header("Slash / Movimiento")]
    public float fuerzaMinima = 6f;
    public float fuerzaMaxima = 14f;
    public float tiempoCargaMin = 0.3f;
    public float tiempoCargaMax = 1.2f;

    [Header("Ritmo")]
    public float cooldownAtaque = 2.5f;

    [Header("Empuje al jugador")]
    public float fuerzaEmpujeJugador = 12f;

    [Header("Rotación")]
    public float velocidadRotacion = 6f;

    Rigidbody rb;
    Transform jugador;

    bool atacando;
    float cooldownTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        jugador = GameObject.FindGameObjectWithTag(tagJugador)?.transform;

        rb.isKinematic = false;
        rb.WakeUp();
    }

    void Update()
    {
        
        if (transform.position.y < alturaMuerte)
        {
            Morir();
            return;
        }

        if (jugador == null) return;

        cooldownTimer -= Time.deltaTime;
        if (atacando || cooldownTimer > 0f) return;

        float dist = Vector3.Distance(transform.position, jugador.position);

        if (dist <= distanciaAtaque)
        {
            StartCoroutine(Atacar());
        }
        else if (dist <= radioDeteccion)
        {
            MirarAlJugador();
        }
    }

    void MirarAlJugador()
    {
        Vector3 dir = (jugador.position - transform.position).normalized;
        dir.y = 0;

        if (dir == Vector3.zero) return;

        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rot,
            Time.deltaTime * velocidadRotacion
        );
    }

    IEnumerator Atacar()
    {
        atacando = true;

        // Orientarse
        Vector3 dir = (jugador.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        // Carga configurable
        float tiempoCarga = Random.Range(tiempoCargaMin, tiempoCargaMax);
        yield return new WaitForSeconds(tiempoCarga);

        float porcentaje = Mathf.InverseLerp(tiempoCargaMin, tiempoCargaMax, tiempoCarga);
        float fuerza = Mathf.Lerp(fuerzaMinima, fuerzaMaxima, porcentaje);

        EjecutarSlash(fuerza);

        cooldownTimer = cooldownAtaque;
        atacando = false;
    }

    void EjecutarSlash(float fuerza)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();

        rb.AddForce(transform.forward * fuerza, ForceMode.Impulse);
    }

    void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.CompareTag(tagJugador)) return;

        Rigidbody rbJugador = other.gameObject.GetComponent<Rigidbody>();
        if (rbJugador == null) return;

        Vector3 dir = (other.transform.position - transform.position).normalized;
        dir.y = 0;

        rbJugador.AddForce(dir * fuerzaEmpujeJugador, ForceMode.Impulse);
    }

    void Morir()
    {
        Destroy(gameObject);
    }
}
