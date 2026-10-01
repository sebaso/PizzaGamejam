using UnityEngine;
using TMPro;
using System.Collections;

public class CountdownUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Vector3 slideOffset = new Vector3(-100f, 0f, 0f);
    [SerializeField] private float baseScale = 1f;
    [SerializeField] private float maxScale = 3f;
    [SerializeField] private float punchDuration = 0.5f;
    [SerializeField] private float holdDuration = 0.3f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField]
    private Color[] numberColors = new Color[]
    {
        new Color(1f, 0.3f, 0.3f),
        new Color(1f, 0.7f, 0.2f),
        new Color(1f, 1f, 0.3f),
        new Color(0.3f, 1f, 0.3f)
    };
    [SerializeField] private float shakeIntensity = 10f;
    [SerializeField] private bool enableScreenShake = true;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip countSound;
    [SerializeField] private AudioClip goSound;

    public System.Action OnCountdownComplete;

    private Vector2 originalPosition = Vector2.zero;
    private Vector2 currentShakeOffset = Vector2.zero;

    private void Awake()
    {
        if (countdownText != null)
        {
            countdownText.alignment = TextAlignmentOptions.Center;
            countdownText.rectTransform.anchoredPosition = originalPosition;
        }
    }

    public void StartCountdown()
    {
        StopAllCoroutines();
        StartCoroutine(CountdownSequence());
    }

    private IEnumerator CountdownSequence()
    {
        if (countdownText == null) yield break;

        countdownText.rectTransform.anchoredPosition = (Vector2)slideOffset;
        countdownText.gameObject.SetActive(true);

        yield return StartCoroutine(ShowNumber("3", numberColors[0], 0.8f));
        yield return StartCoroutine(ShowNumber("2", numberColors[1], 1.0f));
        yield return StartCoroutine(ShowNumber("1", numberColors[2], 1.2f));
        yield return StartCoroutine(ShowNumber("GO!", numberColors[3], 2f, true));

        countdownText.rectTransform.anchoredPosition = originalPosition;
        countdownText.gameObject.SetActive(false);
        OnCountdownComplete?.Invoke();
    }

    private IEnumerator ShowNumber(string text, Color color, float scaleMultiplier, bool isGo = false)
    {
        countdownText.text = text;
        countdownText.color = color;
        currentShakeOffset = Vector2.zero;

        countdownText.rectTransform.anchoredPosition = (Vector2)slideOffset;

        if (audioSource != null)
        {
            AudioClip clip = isGo ? goSound : countSound;
            if (clip != null) audioSource.PlayOneShot(clip);
        }

        float targetMaxScale = maxScale * scaleMultiplier;
        float punchUpDuration = punchDuration * 0.3f;
        float elapsed = 0f;

        while (elapsed < punchUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / punchUpDuration);

            float scale = Mathf.Lerp(baseScale, targetMaxScale * (1f + Mathf.Sin(t * Mathf.PI) * 0.3f), scaleCurve.Evaluate(t));
            countdownText.rectTransform.localScale = Vector3.one * scale;

            Vector2 currentBasePos = Vector2.Lerp((Vector2)slideOffset, originalPosition, t);
            UpdateShake(elapsed, punchDuration, shakeIntensity * scaleMultiplier);
            countdownText.rectTransform.anchoredPosition = currentBasePos + currentShakeOffset;

            float wobble = Mathf.Sin(elapsed * 40f) * (1f - t) * 5f;
            countdownText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, wobble);

            yield return null;
        }

        float shakeElapsed = 0f;
        elapsed = 0f;
        while (elapsed < holdDuration)
        {
            elapsed += Time.deltaTime;
            shakeElapsed += Time.deltaTime;

            float pulse = 1f + Mathf.Sin(elapsed * 20f) * 0.05f;
            countdownText.rectTransform.localScale = Vector3.one * targetMaxScale * pulse;

            UpdateShake(punchUpDuration + shakeElapsed, punchDuration, shakeIntensity * scaleMultiplier);
            countdownText.rectTransform.anchoredPosition = originalPosition + currentShakeOffset;

            yield return null;
        }

        elapsed = 0f;
        float shrinkDuration = punchDuration * 0.4f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            shakeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shrinkDuration);

            float bounce = 1f - Mathf.Pow(2f, -10f * t) * Mathf.Cos(t * Mathf.PI * 2f);
            countdownText.rectTransform.localScale = Vector3.one * Mathf.Lerp(targetMaxScale, 0f, bounce);

            UpdateShake(punchUpDuration + holdDuration + shakeElapsed, punchDuration, shakeIntensity * scaleMultiplier);
            countdownText.rectTransform.anchoredPosition = originalPosition + currentShakeOffset;

            Color fadeColor = color;
            fadeColor.a = 1f - t;
            countdownText.color = fadeColor;

            yield return null;
        }

        countdownText.rectTransform.anchoredPosition = originalPosition;
        countdownText.rectTransform.localScale = Vector3.one * baseScale;
        countdownText.rectTransform.localRotation = Quaternion.identity;
    }

    private void UpdateShake(float totalElapsed, float duration, float intensity)
    {
        if (!enableScreenShake || totalElapsed >= duration)
        {
            currentShakeOffset = Vector2.zero;
            return;
        }
        float damper = 1f - (totalElapsed / duration);
        currentShakeOffset = Random.insideUnitCircle * intensity * damper;
    }

    [ContextMenu("Test Countdown")]
    public void TestCountdown()
    {
        StartCountdown();
    }
}
