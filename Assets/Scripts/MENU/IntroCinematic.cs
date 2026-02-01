using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class IntroCinematic : MonoBehaviour
{
    public Image displayImage;
    public Sprite[] cinematicSprites;
    public float timePerPanel = 5f;
    public string menuSceneName = "MainMenu";
    public bool useFade = true;
    public float fadeDuration = 1.0f;

    private int currentSpriteIndex = 0;
    private float timer = 0f;
    private bool isTransitioning = false;
    private Image backbufferImage;

    void Start()
    {
        if (displayImage == null || cinematicSprites == null || cinematicSprites.Length == 0)
        {
            LoadMenu();
            return;
        }

        GameObject backbufferGO = new GameObject("IntroBackbuffer");
        backbufferGO.transform.SetParent(displayImage.transform.parent);
        backbufferGO.transform.SetSiblingIndex(displayImage.transform.GetSiblingIndex());

        RectTransform rt = backbufferGO.AddComponent<RectTransform>();
        RectTransform mainRT = displayImage.GetComponent<RectTransform>();

        rt.anchorMin = mainRT.anchorMin;
        rt.anchorMax = mainRT.anchorMax;
        rt.anchoredPosition = mainRT.anchoredPosition;
        rt.sizeDelta = mainRT.sizeDelta;
        rt.localScale = mainRT.localScale;

        backbufferImage = backbufferGO.AddComponent<Image>();
        backbufferImage.color = displayImage.color;
        backbufferImage.raycastTarget = false;
        backbufferGO.SetActive(false);

        displayImage.sprite = cinematicSprites[0];
        SetAlpha(displayImage, 1f);
    }

    void Update()
    {
        if (isTransitioning) return;

        timer += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space) || timer >= timePerPanel)
        {
            NextPanel();
        }
    }

    void NextPanel()
    {
        if (currentSpriteIndex + 1 < cinematicSprites.Length)
        {
            currentSpriteIndex++;
            StartCoroutine(TransitionToNextSprite(currentSpriteIndex));
        }
        else
        {
            LoadMenu();
        }
    }

    IEnumerator TransitionToNextSprite(int index)
    {
        isTransitioning = true;

        backbufferImage.sprite = displayImage.sprite;
        backbufferImage.gameObject.SetActive(true);
        SetAlpha(backbufferImage, 1f);

        displayImage.sprite = cinematicSprites[index];

        if (useFade)
        {
            SetAlpha(displayImage, 0f);
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(displayImage, Mathf.Lerp(0, 1, elapsed / fadeDuration));
                yield return null;
            }
            SetAlpha(displayImage, 1f);
        }
        else
        {
            SetAlpha(displayImage, 1f);
        }

        backbufferImage.gameObject.SetActive(false);

        timer = 0f;
        isTransitioning = false;
    }

    private void SetAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    void LoadMenu()
    {
        SceneManager.LoadScene(menuSceneName);
    }
}
