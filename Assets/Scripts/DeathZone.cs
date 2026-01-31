using UnityEngine;

public class DeathZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        PepperoniBase victima = other.GetComponent<PepperoniBase>();
        if (victima != null && victima.currentState != PepperoniBase.State.Respawning)
        {
            victima.Morir(); 
        }
    }
}