using System.Collections;
using UnityEngine;

public class RoniAI : PepperoniBase
{
    [Header("Roni Config")]
    public float radioBusqueda = 25f;    // A qué distancia busca enemigos
    public float agresividad = 0.9f;     // Probabilidad de atacar si encuentra objetivo
    public float radioGolpe = 2.5f;      // Empuje de golpe
    public float fuerzaEmpuje = 18f;     // Fuerza de empuje

    [Header("Duración")]
    public float duracionRoni = 10f;     // Tiempo de vida del clon

    private PepperoniBase objetivo;
    private bool ocupado = false;

    protected override void Start()
    {
        base.Start();

        // Destruirse automáticamente tras duracionRoni
        Destroy(gameObject, duracionRoni);

        // Comenzar IA
        StartCoroutine(CerebroRoni());
    }

    IEnumerator CerebroRoni()
    {
        yield return new WaitForSeconds(0.5f);

        while (vidas > 0)
        {
            if (ocupado)
            {
                yield return null;
                continue;
            }

            BuscarObjetivo();

            if (objetivo != null && Random.value < agresividad)
            {
                yield return StartCoroutine(ManiobraAtaque());
            }
            else
            {
                yield return StartCoroutine(MovimientoAleatorio());
            }

            yield return new WaitForSeconds(Random.Range(1f, 2f));
        }
    }

    void BuscarObjetivo()
    {
        objetivo = null;
        float minDist = Mathf.Infinity;

        Collider[] colliders = Physics.OverlapSphere(transform.position, radioBusqueda);
        foreach (var col in colliders)
        {
            PepperoniBase posible = col.GetComponentInParent<PepperoniBase>();
            if (posible == null || posible == this) continue;

            // Ignorar jugadores
            if (posible.GetComponent<PepperoniPlayer>() != null) continue;

            // Ignorar otros Roni
            if (posible is RoniAI) continue;

            float d = Vector3.Distance(transform.position, posible.transform.position);
            if (d < minDist)
            {
                minDist = d;
                objetivo = posible;
            }
        }
    }

    IEnumerator ManiobraAtaque()
    {
        ocupado = true;

        if (objetivo == null)
        {
            ocupado = false;
            yield break;
        }

        Vector3 meta = objetivo.transform.position;
        if (objetivo.TryGetComponent<Rigidbody>(out Rigidbody rbTarget))
        {
            meta += rbTarget.linearVelocity * 0.5f; // predicción ligera
        }

        EmpezarCarga();

        float tiempo = Random.Range(0.3f, 0.8f);
        float t = 0f;

        while (t < tiempo && currentState == State.Charging)
        {
            t += Time.deltaTime;
            ProcesarCarga(Time.deltaTime);
            RotarHacia(meta);
            yield return null;
        }

        if (currentState == State.Charging)
        {
            LanzarAtaque();
        }

        EmpujarEnemigos();

        // Esperar a que termine ataque
        while (currentState == State.Attacking || currentState == State.Hit)
        {
            yield return null;
        }

        ocupado = false;
    }

    IEnumerator MovimientoAleatorio()
    {
        ocupado = true;

        Vector2 rnd = Random.insideUnitCircle * 4f;
        Vector3 destino = transform.position + new Vector3(rnd.x, 0, rnd.y);

        EmpezarCarga();
        float t = 0f;
        float carga = 0.3f;

        while (t < carga && currentState == State.Charging)
        {
            t += Time.deltaTime;
            ProcesarCarga(Time.deltaTime);
            RotarHacia(destino);
            yield return null;
        }

        if (currentState == State.Charging)
        {
            LanzarAtaque();
        }

        ocupado = false;
    }

    void EmpujarEnemigos()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radioGolpe);
        foreach (var hit in hits)
        {
            PepperoniBase enemigo = hit.GetComponentInParent<PepperoniBase>();
            if (enemigo == null || enemigo == this) continue;

            // Ignorar jugadores y otros clones
            if (enemigo is RoniAI) continue;
            if (enemigo.GetComponent<PepperoniPlayer>() != null) continue;

            Rigidbody rbEnemigo = hit.GetComponent<Rigidbody>();
            if (rbEnemigo != null)
            {
                Vector3 empuje = (hit.transform.position - transform.position).normalized;
                empuje.y = 0;
                rbEnemigo.AddForce(empuje * fuerzaEmpuje, ForceMode.Impulse);
            }
        }
    }

    public override void Morir()
    {
        base.Morir();
        Destroy(gameObject);
    }
}
