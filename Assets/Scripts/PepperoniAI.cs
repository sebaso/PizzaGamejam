using UnityEngine;
using System.Collections;

public class PepperoniAI : PepperoniBase
{
    [Header("Referencias")]
    public Transform centroPizza;
    public Transform objetivo;
    
    [Header("Comportamiento")]
    public float radioLimiteArena = 9f;
    public float agresividad = 0.8f; 

    protected override void Start()
    {
        base.Start();
        if (centroPizza == null) centroPizza = GameObject.Find("CentroPizza")?.transform;
        if (objetivo == null) objetivo = FindFirstObjectByType<PepperoniPlayer>()?.transform;
        
        StartCoroutine(CerebroIA());
    }


    IEnumerator CerebroIA()
    {
        yield return new WaitForSeconds(2f); // Espera inicial mas larga
        
        while (vidas > 0)
        {
            // Solo tomar decisiones si estamos IDLE
            if (currentState != State.Idle)
            {
                yield return new WaitForSeconds(0.5f); // Chequeo menos agresivo
                continue;
            }

            // Distancia al centro para no caerse
            float distCentro = Vector3.Distance(transform.position, centroPizza.position);
            
            if (distCentro > radioLimiteArena)
            {
                yield return StartCoroutine(ManiobraMovimiento(centroPizza.position, 0.4f));
            }
            else if (objetivo != null && Random.value < agresividad)
            {
                yield return StartCoroutine(ManiobraAtaque());
            }
            else
            {
                Vector2 rnd = Random.insideUnitCircle * 4f;
                Vector3 dest = centroPizza.position + new Vector3(rnd.x, 0, rnd.y);
                yield return StartCoroutine(ManiobraMovimiento(dest, 0.2f));
            }

            // CALMA: Esperar entre 1.5 y 2.5 segundos antes de pensar otra vez
            yield return new WaitForSeconds(Random.Range(1.5f, 2.5f));
        }
    }

    IEnumerator ManiobraAtaque()
    {
        if (objetivo == null) yield break;

        // Predecir un poco el movimiento
        Vector3 meta = objetivo.position;
        if (objetivo.TryGetComponent<Rigidbody>(out Rigidbody targetRb))
        {
            meta += targetRb.linearVelocity * 0.5f;
        }

        EmpezarCarga();

        // Cargar un tiempo random
        float tiempo = Random.Range(0.3f, 0.8f);
        float t = 0;

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

        // Esperar a terminar el ataque antes de volver al loop principal
        while (currentState == State.Attacking || currentState == State.Hit)
        {
            yield return null;
        }
    }

    IEnumerator ManiobraMovimiento(Vector3 destino, float carga)
    {
        EmpezarCarga();
        float t = 0;
        while (t < carga && currentState == State.Charging)
        {
            t += Time.deltaTime;
            ProcesarCarga(Time.deltaTime);
            RotarHacia(destino);
            yield return null;
        }
        
        if (currentState == State.Charging) LanzarAtaque();

        while (currentState == State.Attacking) yield return null;
    }

    protected override void Morir()
    {
        base.Morir();
        if (vidas < 3) agresividad = 1f; // Se enfada si pierde vidas
    }
}