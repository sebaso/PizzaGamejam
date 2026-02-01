using UnityEngine;
using System.Collections;

public class PepperoniBossAI : PepperoniBase
{
    [Header("Referencias")]
    public Transform centroPizza;
    public Transform objetivo;

    [Header("IA del Boss")]
    public float radioLimiteArena = 9f;
    [Range(0f, 1f)] public float agresividad = 0.85f;

    [Header("Vida y Fase")]
    public int vidaMaxima = 6;
    private bool fase2Activa = false;

    [Header("Fase 2")]
    public float multiplicadorAgresividadFase2 = 1.4f;
    public float prediccionFase2 = 0.75f;
    public Vector2 esperaDecisionFase1 = new Vector2(1.5f, 2.5f);
    public Vector2 esperaDecisionFase2 = new Vector2(0.4f, 0.9f);

    protected override void Start()
    {
        base.Start();

        if (centroPizza == null)
            centroPizza = GameObject.Find("CentroPizza")?.transform;

        if (objetivo == null)
            objetivo = FindFirstObjectByType<PepperoniPlayer>().transform;

        vidas = vidaMaxima; // sincroniza con el sistema base

        StartCoroutine(CerebroIA());
    }

    IEnumerator CerebroIA()
    {
        yield return new WaitForSeconds(1.5f);

        while (vidas > 0)
        {
            if (currentState != State.Idle)
            {
                yield return null;
                continue;
            }

            // Moverse dentro del límite
            float distCentro = Vector3.Distance(transform.position, centroPizza.position);
            if (distCentro > radioLimiteArena)
            {
                yield return StartCoroutine(MoverHacia(centroPizza.position, 0.35f));
            }
            else if (objetivo != null && Random.value < agresividad)
            {
                yield return StartCoroutine(ManiobraAtaque());
            }
            else
            {
                Vector2 rnd = Random.insideUnitCircle * 3.5f;
                Vector3 destino = centroPizza.position + new Vector3(rnd.x, 0, rnd.y);
                yield return StartCoroutine(MoverHacia(destino, 0.25f));
            }

            Vector2 espera = fase2Activa ? esperaDecisionFase2 : esperaDecisionFase1;
            yield return new WaitForSeconds(Random.Range(espera.x, espera.y));
        }
    }

    IEnumerator ManiobraAtaque()
    {
        if (objetivo == null) yield break;

        Vector3 meta = objetivo.position;

        if (objetivo.TryGetComponent<Rigidbody>(out Rigidbody rbTarget))
        {
            float prediccion = fase2Activa ? prediccionFase2 : 0.45f;
            meta += rbTarget.linearVelocity * prediccion;
        }

        EmpezarCarga();

        float tiempoCarga = fase2Activa ? Random.Range(0.2f, 0.45f) : Random.Range(0.4f, 0.8f);
        float t = 0f;

        while (t < tiempoCarga && currentState == State.Charging)
        {
            t += Time.deltaTime;
            ProcesarCarga(Time.deltaTime);
            RotarHacia(meta);
            yield return null;
        }

        if (currentState == State.Charging)
            LanzarAtaque();

        while (currentState == State.Attacking || currentState == State.Hit)
            yield return null;
    }

    IEnumerator MoverHacia(Vector3 destino, float carga)
    {
        EmpezarCarga();

        float t = 0f;
        while (t < carga && currentState == State.Charging)
        {
            t += Time.deltaTime;
            ProcesarCarga(Time.deltaTime);
            RotarHacia(destino);
            yield return null;
        }

        if (currentState == State.Charging)
            LanzarAtaque();

        while (currentState == State.Attacking)
            yield return null;
    }

    public void PerderVida()
    {
        vidas--;
        if (!fase2Activa && vidas <= vidaMaxima / 2)
        {
            ActivarFase2();
        }

        if (vidas <= 0)
        {
            Morir();
        }
    }

    public override void Morir()
    {
        base.Morir(); // Handles vidas-- and SetState
        
        if (vidas <= 0)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EnemyDefeated(gameObject);
            }
        }
    }

    void ActivarFase2()
    {
        fase2Activa = true;
        agresividad = Mathf.Clamp01(agresividad * multiplicadorAgresividadFase2);
        Debug.Log("PEPPERONI BOSS EN FASE 2 ");
    }
}
