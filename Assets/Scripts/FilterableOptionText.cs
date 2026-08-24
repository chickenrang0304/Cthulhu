using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using Yarn.Unity;

public class FilterableOptionText : MonoBehaviour
{
    public static List<FilterableOptionText> ActiveTrackers = new List<FilterableOptionText>();

    [SerializeField] private TextMeshProUGUI text;

    [Header("Colors")]
    [SerializeField] private string rawColorHex = "#4A7C3F";   // 필터 꺼졌을 때 기본 색
    [SerializeField] private string redColorHex = "#FF0000";    // *텍스트*
    [SerializeField] private string yellowColorHex = "#FFFF00"; // ~텍스트~
    [SerializeField] private string pinkColorHex = "#FF69B4";   // +텍스트+
    [SerializeField] private string indigoColorHex = "#4B0082"; // =텍스트=

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

        if (filterOn)
        {
            text.text = filteredText;
        }
        else
        {
            text.text = $"<color={rawColorHex}>{ProcessColorMarkers(rawText)}</color>";
        }
    }

    private string ProcessColorMarkers(string input)
    {
        input = ReplaceMarker(input, @"\*", redColorHex);
        input = ReplaceMarker(input, @"~", yellowColorHex);
        input = ReplaceMarker(input, @"\+", pinkColorHex);
        input = ReplaceMarker(input, @"=", indigoColorHex);
        return input;
    }

    private string ReplaceMarker(string input, string escapedMarker, string colorHex)
    {
        return Regex.Replace(
            input,
            $@"{escapedMarker}(.+?){escapedMarker}",
            $"<color={colorHex}>$1</color><color={rawColorHex}>"
        );
    }
}