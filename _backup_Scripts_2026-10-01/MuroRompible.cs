using UnityEngine;

public class MuroRompible : MonoBehaviour
{
    [Header("Resistencia")]
    public int vidas = 2;
    public float velocidadMinimaRotura = 5f;
    private float tiempoUltimoGolpe;

    [Header("Configuración de Rebote")]
    public float fuerzaRebote = 35f; 

    [Header("Feedback Visual")]
    public Renderer miRenderer;
    public Color colorDañado = Color.red;

    
    void OnTriggerEnter(Collider other)
    {
        PepperoniBase pBase = other.GetComponent<PepperoniBase>();
        Rigidbody rbAtacante = other.GetComponent<Rigidbody>();
        
        if (rbAtacante != null)
        {
            
            Vector3 direccionRebote = (other.transform.position - transform.position);
            direccionRebote.y = 0.1f; 
            direccionRebote.Normalize();

            
            rbAtacante.linearVelocity = Vector3.zero; 
            rbAtacante.AddForce(direccionRebote * fuerzaRebote, ForceMode.Impulse);

            if (Time.time > tiempoUltimoGolpe + 0.4f)
            {
                float velocidadImpacto = rbAtacante.linearVelocity.magnitude;
                bool esAtaque = (pBase != null && pBase.currentState == PepperoniBase.State.Attacking);

                if (velocidadImpacto > velocidadMinimaRotura || esAtaque)
                {
                    tiempoUltimoGolpe = Time.time;
                    RecibirGolpe();
                }
            }
        }
    }

    void RecibirGolpe()
    {
        vidas--;
        if (vidas == 1 && miRenderer) miRenderer.material.color = colorDañado;
        else if (vidas <= 0) gameObject.SetActive(false);
    }
}