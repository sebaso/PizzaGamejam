using UnityEngine;
using UnityEngine.InputSystem;

public class Ulti : MonoBehaviour
{
    [Header("Prefab de Roni")]
    public GameObject roniPrefab;        // Prefab de Roni con RoniAI
    public Vector3 offsetRoni = new Vector3(1.2f, 0f, 0f);

    private GameObject clonRoni;

    void Update()
    {
        if (roniPrefab == null) return;

        // Pulsar Q para invocar Roni
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            ActivarUlti();
        }
    }

    void ActivarUlti()
    {
        // Evita duplicados
        if (clonRoni != null) return;

        // Instanciar Roni
        clonRoni = Instantiate(roniPrefab, transform.position + offsetRoni, transform.rotation);

        // Destruir automáticamente tras duracionRoni
        RoniAI roniAI = clonRoni.GetComponent<RoniAI>();
        if (roniAI != null)
        {
            Destroy(clonRoni, roniAI.duracionRoni);
        }
    }
}
