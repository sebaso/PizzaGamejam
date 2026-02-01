using UnityEngine;
using UnityEngine.InputSystem;

public class Ulti : MonoBehaviour
{
    [Header("Prefab de Roni")]
    public GameObject roniPrefab;        // Prefab de Roni con RoniAI
    public Vector3 offsetRoni = new Vector3(1.2f, 0f, 0f);
    public float cooldown = 20f;
    private float lastUsedTime = 0;

    private GameObject clonRoni;

    void Update()
    {
        if (roniPrefab == null) return;

        // Pulsar Q para invocar Roni
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            if (Time.time >= lastUsedTime + cooldown)
            {
                ActivarUlti();
            }
            else
            {
                Debug.Log($"Ultimate on cooldown. Time remaining: {Mathf.Ceil(lastUsedTime + cooldown - Time.time)}s");
            }
        }
    }

    void ActivarUlti()
    {
        // Evita duplicados
        if (clonRoni != null) return;

        lastUsedTime = Time.time;

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
