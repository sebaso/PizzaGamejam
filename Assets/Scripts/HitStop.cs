using UnityEngine;
using System.Collections;

// Congela el juego unos milisegundos al golpear para que el impacto "pese",
// y gestiona la cámara lenta del KO final.
// Se crea solo la primera vez que se usa; no hace falta ponerlo en la escena.
public class HitStop : MonoBehaviour
{
    // Valor distinto de 0 para no confundirlo con la pausa del PauseMenu
    const float ESCALA_HITSTOP = 0.02f;

    static HitStop instancia;
    float fixedDeltaBase;
    float finEfecto;
    float escalaActual;
    bool activo;
    Coroutine rutina;

    public static void Congelar(float duracion)
    {
        Aplicar(duracion, ESCALA_HITSTOP, false);
    }

    // Tiene prioridad sobre un hitstop en curso
    public static void CamaraLenta(float duracion, float escala)
    {
        Aplicar(duracion, Mathf.Clamp(escala, ESCALA_HITSTOP, 1f), true);
    }

    static void Aplicar(float duracion, float escala, bool prioritario)
    {
        if (duracion <= 0f || !instancia) return;

        if (instancia.activo)
        {
            // Mismo efecto: se alarga en vez de reiniciarse
            if (Mathf.Approximately(escala, instancia.escalaActual))
            {
                instancia.finEfecto = Mathf.Max(instancia.finEfecto, Time.unscaledTime + duracion);
                return;
            }
            if (!prioritario) return;
        }
        // No pisar la pausa ni otros cambios de timeScale
        else if (!Mathf.Approximately(Time.timeScale, 1f)) return;

        instancia.Iniciar(duracion, escala);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (instancia) return;
        GameObject go = new GameObject("HitStop");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<HitStop>();
        instancia.fixedDeltaBase = Time.fixedDeltaTime;
    }

    void Iniciar(float duracion, float escala)
    {
        if (rutina != null) StopCoroutine(rutina);

        escalaActual = escala;
        finEfecto = Time.unscaledTime + duracion;
        activo = true;
        Time.timeScale = escala;
        // La física sigue a 50 Hz reales: la cámara lenta va fluida, no a saltos
        Time.fixedDeltaTime = fixedDeltaBase * escala;
        rutina = StartCoroutine(Rutina());
    }

    IEnumerator Rutina()
    {
        while (Time.unscaledTime < finEfecto)
        {
            // Si alguien pausa o cambia de escena, le cedemos el control
            if (!Mathf.Approximately(Time.timeScale, escalaActual))
            {
                Terminar(false);
                yield break;
            }
            yield return null;
        }

        Terminar(true);
    }

    void Terminar(bool restaurarEscala)
    {
        if (restaurarEscala) Time.timeScale = 1f;
        // Siempre: un fixedDeltaTime diminuto con timeScale 1 hundiría el rendimiento
        Time.fixedDeltaTime = fixedDeltaBase;
        activo = false;
        rutina = null;
    }
}
