using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Yarn.Unity;

public class FilterableOptionText : MonoBehaviour
{
    public static List<FilterableOptionText> ActiveTrackers = new List<FilterableOptionText>();

    [SerializeField] private TextMeshProUGUI text; // 이건 프리팹 안의 자기 자식이라 그대로 인스펙터 연결 가능

    private OptionItem optionItem;
    private string filteredText;
    private string rawText;
    private bool initialized = false;

    void Awake()
    {
        optionItem = GetComponent<OptionItem>();
    }

    void OnEnable()
    {
        if (!ActiveTrackers.Contains(this)) ActiveTrackers.Add(this);
        initialized = false;
    }

    void OnDisable()
    {
        ActiveTrackers.Remove(this);
    }

    void Update()
    {
        if (!initialized)
        {
            TryCacheTexts();
        }
    }

    private void TryCacheTexts()
    {
        if (optionItem == null) return;

        try
        {
            var option = optionItem.Option;
            filteredText = option.Line.TextWithoutCharacterName.Text;
            rawText = filteredText;

            if (option.Line.Metadata != null)
            {
                var rawTag = option.Line.Metadata.FirstOrDefault(m => m.StartsWith("raw:"));
                if (rawTag != null)
                {
                    rawText = rawTag.Substring("raw:".Length).Replace("_", " ");
                }
            }

            initialized = true;

            // FilterManager.Instance로 직접 접근 (인스펙터 연결 필요 없음)
            bool currentFilterState = FilterManager.Instance != null ? FilterManager.Instance.FilterOn : true;
            RefreshText(currentFilterState);
        }
        catch
        {
            // 아직 Option 미할당 - 다음 프레임 재시도
        }
    }

    public void RefreshText(bool filterOn)
    {
        if (!initialized || text == null) return;
        text.text = filterOn ? filteredText : rawText;
    }
}