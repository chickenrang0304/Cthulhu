using UnityEngine;
using UnityEngine.UI;

public class CharProfileChanger : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite sprite1; // 67% 이상
    [SerializeField] private Sprite sprite2; // 34% 이상
    [SerializeField] private Sprite sprite3; // 그 이하

    [Header("References")]
    [SerializeField] private Image profileImage; // 스프라이트를 표시할 UI Image

    [Header("Shake Settings")]
    [SerializeField] private float shakeThreshold = 0.33f; // 이 비율 이하일 때 떨림
    [SerializeField] private float shakeAmount = 5f;        // 떨리는 정도
    [SerializeField] private float shakeSpeed = 30f;        // 떨리는 속도

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private bool isShaking = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
    }

    void Update()
    {
        if (isShaking)
        {
            float offsetX = (Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) - 0.5f) * 2f * shakeAmount;
            float offsetY = (Mathf.PerlinNoise(0f, Time.time * shakeSpeed) - 0.5f) * 2f * shakeAmount;
            rectTransform.anchoredPosition = originalPosition + new Vector2(offsetX, offsetY);
        }
        else
        {
            rectTransform.anchoredPosition = originalPosition;
        }
    }

    public void UpdateProfile(float current, float max)
    {
        float ratio = current / max;

        if (ratio >= 0.67f)
        {
            profileImage.sprite = sprite1;
            isShaking = false;
        }
        else if (ratio >= 0.34f)
        {
            profileImage.sprite = sprite2;
            isShaking = false;
        }
        else
        {
            profileImage.sprite = sprite3;
            isShaking = ratio <= shakeThreshold;
        }
    }
}