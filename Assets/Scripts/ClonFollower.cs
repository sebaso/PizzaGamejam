using System.Collections;
using UnityEngine;

public class ClonFollower : MonoBehaviour
{
    [Header("Vida")]
    public float duracionClon = 10f;

    [Header("Detección")]
    public float radioBusqueda = 25f;
    public LayerMask enemyLayer;

    [Header("Slash / Movimiento")]
    public float fuerzaMinima = 10f;
    public float fuerzaMaxima = 22f;
    public float tiempoCargaMax = 1f;
    public float cooldownSlash = 0.7f;

    [Header("Empuje a enemigos")]
    public float radioGolpe = 2.5f;
    public float fuerzaEmpuje = 18f;

    Rigidbody rb;
    Transform objetivo;
    bool ocupado;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Asegurar física activa
        rb.isKinematic = false;
        rb.WakeUp();

        Destroy(gameObject, duracionClon);
        StartCoroutine(IA());
    }

    IEnumerator IA()
    {
        while (true)
        {
            if (!ocupado)
            {
                BuscarEnemigo();

                if (objetivo != null)
                    yield return StartCoroutine(SlashHaciaObjetivo());
                else
                    yield return StartCoroutine(SlashAleatorio());
            }

            yield return null;
        }
    }

    void BuscarEnemigo()
    {
        Collider[] enemigos = Physics.OverlapSphere(transform.position, radioBusqueda, enemyLayer);

        float minDist = Mathf.Infinity;
        Transform mejor = null;

        foreach (Collider e in enemigos)
        {
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < minDist)
            {
                minDist = d;
                mejor = e.transform;
            }
        }

        objetivo = mejor;
    }

    IEnumerator SlashHaciaObjetivo()
    {
        ocupado = true;

        // Orientarse
        Vector3 dir = (objetivo.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        // Carga invisible
        float tiempoCarga = Random.Range(0.2f, tiempoCargaMax);
        yield return new WaitForSeconds(tiempoCarga);

        float porcentaje = Mathf.Clamp01(tiempoCarga / tiempoCargaMax);
        float fuerza = Mathf.Lerp(fuerzaMinima, fuerzaMaxima, porcentaje);

        EjecutarSlash(fuerza);

        yield return new WaitForSeconds(cooldownSlash);
        ocupado = false;
    }

    IEnumerator SlashAleatorio()
    {
        ocupado = true;

        Vector3 dir = Random.insideUnitSphere;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        float fuerza = Random.Range(fuerzaMinima, fuerzaMaxima);

        EjecutarSlash(fuerza);

        yield return new WaitForSeconds(cooldownSlash);
        ocupado = false;
    }

    void EjecutarSlash(float fuerza)
    {
        // Reset físico para que el impulso SIEMPRE funcione
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();

        // Movimiento = impulso
        rb.AddForce(transform.forward * fuerza, ForceMode.Impulse);

        // Empuje de enemigos
        Collider[] hits = Physics.OverlapSphere(transform.position, radioGolpe, enemyLayer);
        foreach (Collider hit in hits)
        {
            Rigidbody rbEnemy = hit.GetComponent<Rigidbody>();
            if (rbEnemy != null)
            {
                Vector3 empuje = (hit.transform.position - transform.position).normalized;
                empuje.y = 0;
                rbEnemy.AddForce(empuje * fuerzaEmpuje, ForceMode.Impulse);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioBusqueda);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioGolpe);
    }
}

    

      






