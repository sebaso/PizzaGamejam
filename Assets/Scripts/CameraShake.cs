using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    private Vector3 posicionOriginal;
    private bool temblando = false;

    void Start()
    {
        posicionOriginal = transform.localPosition;
    }

    public void Shake(float intensidad, float duracion = 0.2f)
    {
        if (!temblando)
        {
            StartCoroutine(ShakeCoroutine(intensidad, duracion));
        }
    }

    IEnumerator ShakeCoroutine(float intensidad, float duracion)
    {
        temblando = true;
        float elapsed = 0f;
        
        while (elapsed < duracion)
        {
            float x = Random.Range(-1f, 1f) * intensidad;
            float y = Random.Range(-1f, 1f) * intensidad;
            
            transform.localPosition = posicionOriginal + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            intensidad = Mathf.Lerp(intensidad, 0, elapsed / duracion);
            
            yield return null;
        }
        
        transform.localPosition = posicionOriginal;
        temblando = false;
    }
}