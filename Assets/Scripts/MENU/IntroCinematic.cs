using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class IntroCinematic : MonoBehaviour
{
    [Header("Cinematic Settings")]
    [Tooltip("The MAIN Image component that will display the slides.")]
    public Image displayImage;

    [Tooltip("The slides (Sprites) to display in sequence.")]
    public Sprite[] cinematicSprites;

    [Tooltip("Time each slide stays on screen before transitioning.")]
    public float timePerPanel = 5f;

    [Tooltip("Name of the scene to load after the cinematic finishes.")]
    public string menuSceneName = "MainMenu";

    [Header("Transition Settings")]
    [Tooltip("If true, the next slide will fade in on top of the current one.")]
    public bool useFade = true;
    public float fadeDuration = 1.0f;

    private int currentSpriteIndex = 0;
    private float timer = 0f;
    private bool isTransitioning = false;
    private Image backbufferImage; // Used to keep the old sprite visible during fade

    void Start()
    {
        if (displayImage == null || cinematicSprites == null || cinematicSprites.Length == 0)
        {
            Debug.LogWarning("IntroCinematic: Missing reference to Image or Sprites!");
            LoadMenu();
            return;
        }

        // Setup Backbuffer: This creates a copy of the display image behind it 
        // so we can keep the old slide visible while the new one fades in on top.
        GameObject backbufferGO = new GameObject("IntroBackbuffer");
        backbufferGO.transform.SetParent(displayImage.transform.parent);

        // Ensure it stays behind the main display image
        backbufferGO.transform.SetSiblingIndex(displayImage.transform.GetSiblingIndex());

        RectTransform rt = backbufferGO.AddComponent<RectTransform>();
        RectTransform mainRT = displayImage.GetComponent<RectTransform>();

        // Sync position and size
        rt.anchorMin = mainRT.anchorMin;
        rt.anchorMax = mainRT.anchorMax;
        rt.anchoredPosition = mainRT.anchoredPosition;
        rt.sizeDelta = mainRT.sizeDelta;
        rt.localScale = mainRT.localScale;

        backbufferImage = backbufferGO.AddComponent<Image>();
        backbufferImage.color = displayImage.color;
        backbufferImage.raycastTarget = false;
        backbufferGO.SetActive(false);

        // Show first slide
        displayImage.sprite = cinematicSprites[0];
        SetAlpha(displayImage, 1f);
    }

    void Update()
    {
        if (isTransitioning) return;

        timer += Time.deltaTime;

        // Transition on Space press or Time limit
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

        // 1. Put current sprite in the backbuffer so it stays visible underneath
        backbufferImage.sprite = displayImage.sprite;
        backbufferImage.gameObject.SetActive(true);
        SetAlpha(backbufferImage, 1f);

        // 2. Prepare main image with the new sprite (starting at 0 alpha)
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

        // 3. Hide backbuffer now that the new sprite is fully opaque
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
