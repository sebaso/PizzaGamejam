using UnityEngine;
using UnityEngine.InputSystem;

public class Ulti : MonoBehaviour
{
    [Header("Clon")]
    public GameObject prefabClon;
    public float duracionClon = 10f;
    public Vector3 offsetClon = new Vector3(1.2f, 0f, 0f);

    private GameObject clonActual;

    void Update()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame)
            ActivarUlti();
    }

    void ActivarUlti()
    {
        if (prefabClon == null) return;
        if (clonActual != null) return;

        clonActual = Instantiate(prefabClon, transform.position + offsetClon, transform.rotation);
        Destroy(clonActual, duracionClon);
    }
}






