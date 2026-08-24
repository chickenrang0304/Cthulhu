using System.Collections;
using UnityEngine;
using Yarn.Unity;

public class PersuasionManager : MonoBehaviour
{
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private float minValue = 0f;
    [SerializeField] private float maxValue = 100f;

    [Header("UI")]
    [SerializeField] private GameObject persuasionBar;
    [SerializeField] private GameObject currentPersuasionBar;

    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.5f; // 애니메이션 총 시간(초)

    private const string VariableName = "$persuasion";
    private Coroutine currentAnimation;
    private float displayedValue = 0f; // 현재 화면에 실제로 보여지고 있는 값

    void Start()
    {
        RectTransform CRTPersuasionBar = currentPersuasionBar.GetComponent<RectTransform>();
        RectTransform MaxPersuasionBar = persuasionBar.GetComponent<RectTransform>();
        CRTPersuasionBar.sizeDelta = new Vector2(MaxPersuasionBar.sizeDelta.x, 0);

        dialogueRunner.VariableStorage.AddChangeListener<float>(VariableName, OnPersuasionChanged);
    }

    [YarnCommand("add_persuasion")]
    public void AddPersuasion(float amount)
    {
        dialogueRunner.VariableStorage.TryGetValue<float>(VariableName, out float current);
        float newValue = Mathf.Clamp(current + amount, minValue, maxValue);
        dialogueRunner.VariableStorage.SetValue(VariableName, newValue);
    }

    private void OnPersuasionChanged(float newValue)
    {
        // 이미 실행 중인 애니메이션이 있으면 중단하고 새로 시작
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        currentAnimation = StartCoroutine(AnimateGauge(displayedValue, newValue));
    }

    private IEnumerator AnimateGauge(float from, float to)
    {
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Ease Out: 처음엔 빠르게, 끝에 가까워질수록 느려짐
            float easedT = 1f - Mathf.Pow(1f - t, 3f); // 세제곱 감속 곡선

            float current = Mathf.Lerp(from, to, easedT);
            displayedValue = current;
            ApplyValue(current, maxValue);

            yield return null;
        }

        // 마지막에 정확히 목표값으로 스냅
        displayedValue = to;
        ApplyValue(to, maxValue);
        currentAnimation = null;
    }

    public void ApplyValue(float current, float max) // 현재 설득도 UI 비율에 맞게 적용
    {
        float persuasionRatio = current / max;
        RectTransform CRTPersuasionBar = currentPersuasionBar.GetComponent<RectTransform>();
        RectTransform MaxPersuasionBar = persuasionBar.GetComponent<RectTransform>();

        CRTPersuasionBar.sizeDelta = new Vector2(MaxPersuasionBar.sizeDelta.x, MaxPersuasionBar.sizeDelta.y * persuasionRatio);
    }
}