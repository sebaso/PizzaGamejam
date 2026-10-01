using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    private Vector3 posicionOriginal;
    private bool temblando = false;
    private float intensidadActual;

    static CameraShake instancia;
    static bool instanciaBuscada;

    // Acceso cómodo desde cualquier script; no hace nada si no hay CameraShake en la escena
    public static void Sacudir(float intensidad, float duracion = 0.2f)
    {
        if (!instancia && !instanciaBuscada)
        {
            instancia = FindFirstObjectByType<CameraShake>();
            instanciaBuscada = true;
        }
        if (instancia) instancia.Shake(intensidad, duracion);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetEstatico()
    {
        instancia = null;
        instanciaBuscada = false;
    }

    void Awake()
    {
        instancia = this;
    }

    void Start()
    {
        posicionOriginal = transform.localPosition;
    }

    void OnDestroy()
    {
        if (instancia == this)
        {
            instancia = null;
            instanciaBuscada = false;
        }
    }

    public void Shake(float intensidad, float duracion = 0.2f)
    {
        // Un temblor más fuerte sustituye al que está en curso
        if (temblando && intensidad <= intensidadActual) return;

        StopAllCoroutines();
        StartCoroutine(ShakeCoroutine(intensidad, duracion));
    }

    IEnumerator ShakeCoroutine(float intensidad, float duracion)
    {
        temblando = true;
        float elapsed = 0f;

        while (elapsed < duracion)
        {
            // Decae de forma lineal desde la intensidad inicial
            intensidadActual = intensidad * (1f - elapsed / duracion);
            float x = Random.Range(-1f, 1f) * intensidadActual;
            float y = Random.Range(-1f, 1f) * intensidadActual;

            transform.localPosition = posicionOriginal + new Vector3(x, y, 0);

            elapsed += Time.unscaledDeltaTime; // sigue temblando durante el hitstop
            yield return null;
        }

        transform.localPosition = posicionOriginal;
        intensidadActual = 0f;
        temblando = false;
    }
}
