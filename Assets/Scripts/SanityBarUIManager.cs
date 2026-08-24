using UnityEngine;
using System.Collections;

public class SanityBarUIManager : MonoBehaviour
{
    public GameObject HPBar;
    public GameObject currentHPBar;

    [SerializeField] private float animationDuration = 0.5f;

    private Coroutine currentAnimation;
    private float displayedValue = 0f; // 현재 화면에 실제로 보여지고 있는 값
    private float displayedMax = 100f;

    public void Applyvalue(float current, float max) // 현재 체력 UI 비율에 맞게 적용 (애니메이션 포함)
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        currentAnimation = StartCoroutine(AnimateGauge(displayedValue, current, max));
    }

    private IEnumerator AnimateGauge(float from, float to, float max)
    {
        float elapsed = 0f;
        displayedMax = max;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float easedT = 1f - Mathf.Pow(1f - t, 3f); // Ease Out

            float current = Mathf.Lerp(from, to, easedT);
            displayedValue = current;
            SetBarImmediate(current, max);

            yield return null;
        }

        displayedValue = to;
        SetBarImmediate(to, max);
        currentAnimation = null;
    }

    private void SetBarImmediate(float current, float max)
    {
        float hpRatio = current / max;
        RectTransform CRTHPBar = currentHPBar.GetComponent<RectTransform>();
        RectTransform MaxHPBar = HPBar.GetComponent<RectTransform>();

        CRTHPBar.sizeDelta = new Vector2(MaxHPBar.sizeDelta.x, MaxHPBar.sizeDelta.y * hpRatio);
    }
}