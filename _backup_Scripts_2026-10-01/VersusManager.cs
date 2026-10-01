using UnityEngine;

public class VersusManager : MonoBehaviour
{
    [Header("Sprites por Día (asigna en orden: Día 1, Día 2, etc.)")]
    public GameObject[] daySprites;

    void Start()
    {
        ActualizarSpriteDia();
        
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayStart += ActualizarSpriteDia;
        }
    }

    void OnDestroy()
    {
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayStart -= ActualizarSpriteDia;
        }
    }

    public void ActualizarSpriteDia()
    {
        if (GameManager.Instance == null || daySprites == null || daySprites.Length == 0) return;

        int currentDay = GameManager.Instance.currentDay;
        
        for (int i = 0; i < daySprites.Length; i++)
        {
            if (daySprites[i] != null)
            {
                
                daySprites[i].SetActive(i == currentDay - 1);
            }
        }
    }
}
