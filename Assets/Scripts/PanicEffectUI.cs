using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PanicEffectUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image panicImage;      // 투명도 조절할 UI Image
    [SerializeField] private RectTransform panicRect; // 좌우/상하 반전시킬 RectTransform

    [Header("Flip Settings")]
    [SerializeField] private float minFlipInterval = 0.03f; // 체력 0%일 때 (가장 빠름)
    [SerializeField] private float maxFlipInterval = 0.3f;  // 체력 66% 지점일 때 (가장 느림)

    [Header("Opacity Settings")]
    [SerializeField] private float maxAlpha = 0.7f;    // 최대 불투명도(70%)
    [SerializeField] private float startThreshold = 0.67f; // 이 비율 이하부터 서서히 보이기 시작

    private Coroutine flipCoroutine;
    private bool flipX = false;
    private bool flipY = false;
    private float currentFlipInterval; // 지금 적용 중인 반전 속도

    public void UpdateEffect(float current, float max)
    {
        float ratio = current / max;

        float alpha;
        float t; // 0(66%지점) ~ 1(0%지점)로 정규화된 위험도

        if (ratio >= startThreshold)
        {
            alpha = 0f;
            t = 0f;
        }
        else
        {
            t = (startThreshold - ratio) / startThreshold;
            t = Mathf.Clamp01(t);
            alpha = t * maxAlpha;
        }

        Color color = panicImage.color;
        color.a = alpha;
        panicImage.color = color;

        // 위험도(t)가 높을수록(체력 낮을수록) 반전 속도가 빨라지도록 보간
        // t=0 -> maxFlipInterval(느림), t=1 -> minFlipInterval(빠름)
        currentFlipInterval = Mathf.Lerp(maxFlipInterval, minFlipInterval, t);

        if (alpha > 0.01f)
        {
            if (flipCoroutine == null)
            {
                flipCoroutine = StartCoroutine(FlipLoop());
            }
        }
        else
        {
            if (flipCoroutine != null)
            {
                StopCoroutine(flipCoroutine);
                flipCoroutine = null;
                ResetFlip();
            }
        }
    }

    private IEnumerator FlipLoop()
    {
        while (true)
        {
            flipX = !flipX;
            ApplyFlip();

            yield return new WaitForSeconds(currentFlipInterval);

            flipY = !flipY;
            ApplyFlip();

            yield return new WaitForSeconds(currentFlipInterval);
        }
    }

    private void ApplyFlip()
    {
        Vector3 scale = panicRect.localScale;
        scale.x = flipX ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        scale.y = flipY ? -Mathf.Abs(scale.y) : Mathf.Abs(scale.y);
        panicRect.localScale = scale;
    }

    private void ResetFlip()
    {
        flipX = false;
        flipY = false;
        Vector3 scale = panicRect.localScale;
        scale.x = Mathf.Abs(scale.x);
        scale.y = Mathf.Abs(scale.y);
        panicRect.localScale = scale;
    }
}