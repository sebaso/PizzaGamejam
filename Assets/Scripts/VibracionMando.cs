using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

// Vibración del gamepad. Se crea sola; si no hay mando conectado no hace nada.
public class VibracionMando : MonoBehaviour
{
    static VibracionMando instancia;
    float finVibracion;
    Coroutine rutina;

    /// <param name="grave">Motor de baja frecuencia (0-1): golpes pesados</param>
    /// <param name="agudo">Motor de alta frecuencia (0-1): golpes secos</param>
    public static void Vibrar(float grave, float agudo, float duracion)
    {
        Gamepad mando = Gamepad.current;
        if (mando == null || !instancia || duracion <= 0f) return;

        mando.SetMotorSpeeds(Mathf.Clamp01(grave), Mathf.Clamp01(agudo));
        // Tiempo real: la vibración no se alarga durante el hitstop ni la cámara lenta
        instancia.finVibracion = Mathf.Max(instancia.finVibracion, Time.unscaledTime + duracion);
        if (instancia.rutina == null) instancia.rutina = instancia.StartCoroutine(instancia.Parar());
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (instancia) return;
        GameObject go = new GameObject("VibracionMando");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<VibracionMando>();
    }

    IEnumerator Parar()
    {
        while (Time.unscaledTime < finVibracion) yield return null;
        Gamepad.current?.SetMotorSpeeds(0f, 0f);
        rutina = null;
    }

    // Que el mando nunca se quede vibrando
    void OnApplicationPause(bool pausado) { if (pausado) InputSystem.ResetHaptics(); }
    void OnApplicationQuit() { InputSystem.ResetHaptics(); }
    void OnDisable() { InputSystem.ResetHaptics(); }
}
