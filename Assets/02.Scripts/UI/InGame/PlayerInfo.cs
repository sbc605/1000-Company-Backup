//코드 담당자: 유호정
using UnityEngine;
using TMPro;
using System.Collections;
// using UnityEngine.UI;

public class PlayerInfo : MonoBehaviour
{
    public static PlayerInfo Instance { get; private set; }
    [Header("Sanity UI")]
    public TextMeshProUGUI sanityText;
    // public Image sanityBar;

    [Header("Sanity Shake Effect")]
    [SerializeField] private Color dangerColor = Color.red;
    
    [Header("Warning (30% ~ 15%)")]
    [SerializeField] private float normalShakeMagnitude = 1.5f;
    [SerializeField] private float normalShakeSpeed = 0.1f;

    [Header("Danger (Below 15%)")]
    [SerializeField] private float dangerShakeMagnitude = 4.0f;
    [SerializeField] private float dangerShakeSpeed = 0.05f;

    private Vector2 _originalSanityTextPos;
    private Color _originalTextColor;
    private Coroutine _shakeCoroutine;
    [HideInInspector] public float maxSanity = 100f;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    private void Start()
    {
        if (sanityText != null)
        {
            _originalSanityTextPos = sanityText.rectTransform.anchoredPosition;
            _originalTextColor = sanityText.color;
        }
    }

    public void UpdateSanityUI(float currentSanity)
    {

        if (sanityText != null)
        {
            sanityText.text = $"{currentSanity:F0}%";
        }
        // if (sanityBar != null && maxSanity > 0)
        // {

        //     sanityBar.fillAmount = currentSanity / maxSanity;
        // }
    }

    public void UpdateSanityEffect(int level)
    {
        if (sanityText == null) return;
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = null;
            sanityText.rectTransform.anchoredPosition = _originalSanityTextPos;
        }

        switch (level)
        {
            case 0:
                sanityText.color = _originalTextColor;
                break;
            case 1:
                sanityText.color = _originalTextColor;
                _shakeCoroutine = StartCoroutine(ShakeTextCoroutine(normalShakeMagnitude, normalShakeSpeed));
                break;
            case 2:
                sanityText.color = dangerColor;
                _shakeCoroutine = StartCoroutine(ShakeTextCoroutine(dangerShakeMagnitude, dangerShakeSpeed));
                break;
        }
    }

    private IEnumerator ShakeTextCoroutine(float magnitude, float speed)
    {
        while (true)
        {
            Vector2 randomOffset = Random.insideUnitCircle * magnitude;
            sanityText.rectTransform.anchoredPosition = _originalSanityTextPos + randomOffset;
            yield return new WaitForSeconds(speed);
        }
    }


}