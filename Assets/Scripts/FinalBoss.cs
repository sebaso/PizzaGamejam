using System.Collections;
using UnityEngine;

public class FinalBoss : MonoBehaviour
{
    [Header("Vida del Jefe")]
    public int golpesParaMorir = 10;

    [Header("Movimiento")]
    public float moveSpeed = 3f;
    public float rotSpeed = 4f;
    public float orbitFactor = 0.6f; 

    [Header("Carga / Smash")]
    public float fuerzaMinima = 5f;
    public float fuerzaMaxima = 25f;
    public float tiempoCargaMax = 2f;

    [Header("IA")]
    public float distanciaAtaque = 6f;
    public float distanciaEspecial = 8f;
    public float cooldownAtaque = 2f;
    public float cooldownEspecial = 10f;

    [Header("Daño ")]
    public float fuerzaImpacto = 15f;
    public float fuerzaEspecial = 25f;

    [Header("Visual")]
    public Renderer rend;
    public Color colorOriginal;
    public Color colorCargando = Color.red;

    Rigidbody rb;
    Transform player;

    float cooldownAtaqueTimer;
    float cooldownEspecialTimer;

    bool cargando;
    bool enAtaque;
    bool enEspecial;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        if (!rend) rend = GetComponent<Renderer>();
        if (rend) colorOriginal = rend.material.color;
    }

    void Update()
    {
        if (golpesParaMorir <= 0) return;

        cooldownAtaqueTimer -= Time.deltaTime;
        cooldownEspecialTimer -= Time.deltaTime;

        if (!cargando && !enAtaque && !enEspecial)
        {
            float dist = Vector3.Distance(transform.position, player.position);

            if (dist <= distanciaAtaque && cooldownAtaqueTimer <= 0)
            {
                StartCoroutine(Atacar());
            }
            else if (dist <= distanciaEspecial && cooldownEspecialTimer <= 0)
            {
                StartCoroutine(Especial());
            }
            else
            {
                MoverAlrededor();
            }
        }
    }

    void MoverAlrededor()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotSpeed);

        Vector3 lateral = Vector3.Cross(Vector3.up, dir);
        rb.linearVelocity = (dir + lateral * orbitFactor) * moveSpeed;
    }

    IEnumerator Atacar()
    {
        cargando = true;
        float tiempoCarga = Random.Range(0.5f, tiempoCargaMax);

        if (rend) rend.material.color = colorCargando;

        yield return new WaitForSeconds(tiempoCarga);

        cargando = false;
        enAtaque = true;
        if (rend) rend.material.color = colorOriginal;

        float porcentaje = Mathf.Clamp01(tiempoCarga / tiempoCargaMax);
        float fuerza = Mathf.Lerp(fuerzaMinima, fuerzaMaxima, porcentaje);

        // fase 2: más agresivo
        if (golpesParaMorir <= 4) fuerza *= 1.2f;

        rb.AddForce(transform.forward * fuerza, ForceMode.Impulse);

        yield return new WaitForSeconds(0.6f);

        enAtaque = false;
        cooldownAtaqueTimer = cooldownAtaque;
    }

    IEnumerator Especial()
    {
        cargando = true;
        float tiempoCarga = tiempoCargaMax;

        if (rend) rend.material.color = colorCargando;

        yield return new WaitForSeconds(tiempoCarga);

        cargando = false;
        enEspecial = true;
        if (rend) rend.material.color = colorOriginal;

        float fuerza = fuerzaEspecial;
        // fase 2
        if (golpesParaMorir <= 4) fuerza *= 1.3f; 

        rb.AddForce(transform.forward * fuerza, ForceMode.Impulse);

        yield return new WaitForSeconds(1f);

        enEspecial = false;
        cooldownEspecialTimer = cooldownEspecial;
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Rigidbody rbPlayer = other.gameObject.GetComponent<Rigidbody>();
            if (rbPlayer != null)
            {
                if (enAtaque)
                    rbPlayer.AddForce(transform.forward * fuerzaImpacto, ForceMode.Impulse);

                if (enEspecial)
                    rbPlayer.AddForce(transform.forward * fuerzaEspecial, ForceMode.Impulse);
            }

            // Golpe del jugador
            float fuerzaImpactoJugador = other.relativeVelocity.magnitude;
            if (fuerzaImpactoJugador > 10f)
            {
                RecibirGolpe();
            }
        }
    }

    public void RecibirGolpe()
    {
        golpesParaMorir--;
        Debug.Log("Jefe golpeado. Golpes restantes: " + golpesParaMorir);

        if (golpesParaMorir <= 0)
            Morir();
    }

    void Morir()
    {
        Debug.Log("Jefe derrotado");
        rb.linearVelocity = Vector3.zero;
        Destroy(gameObject);
    }
}
