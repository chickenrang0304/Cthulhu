using UnityEngine;
using TMPro;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Yarn.Unity;

public class FilterableLastLine : MonoBehaviour
{
    public static FilterableLastLine Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI lastLineText;

    [Header("Colors")]
    [SerializeField] private string rawColorHex = "#4A7C3F";
    [SerializeField] private string redColorHex = "#FF0000";
    [SerializeField] private string yellowColorHex = "#FFFF00";
    [SerializeField] private string pinkColorHex = "#FF69B4";
    [SerializeField] private string indigoColorHex = "#4B0082";

    private static readonly Regex markerRegex = new Regex(@"\*(.+?)\*|~(.+?)~|\+(.+?)\+|=(.+?)=");

    private string filteredText;
    private string rawText;
    private bool hasCachedText = false;

    void Awake()
    {
        Instance = this;
    }

    public void CacheLine(LocalizedLine line)
    {
        filteredText = line.TextWithoutCharacterName.Text;
        rawText = filteredText;

        if (line.Metadata != null)
        {
            var rawTag = line.Metadata.FirstOrDefault(m => m.StartsWith("raw:"));
            if (rawTag != null)
            {
                rawText = rawTag.Substring("raw:".Length).Replace("_", " ");
            }
        }

        hasCachedText = true;

        bool filterOn = FilterManager.Instance != null ? FilterManager.Instance.FilterOn : true;
        RefreshText(filterOn);
    }

    public void RefreshText(bool filterOn)
    {
        if (!hasCachedText || lastLineText == null) return;

        lastLineText.text = filterOn ? filteredText : ProcessColorMarkers(rawText);
    }

    // 중첩 없이, 구간별로 독립된 <color> 태그를 순서대로 이어붙임
    private string ProcessColorMarkers(string input)
    {
        var sb = new StringBuilder();
        int lastIndex = 0;

        foreach (Match m in markerRegex.Matches(input))
        {
            // 마커 이전의 일반 구간 (기본색)
            if (m.Index > lastIndex)
            {
                string plain = input.Substring(lastIndex, m.Index - lastIndex);
                sb.Append($"<color={rawColorHex}>{plain}</color>");
            }

            string content;
            string color;

            if (m.Groups[1].Success) { content = m.Groups[1].Value; color = redColorHex; }
            else if (m.Groups[2].Success) { content = m.Groups[2].Value; color = yellowColorHex; }
            else if (m.Groups[3].Success) { content = m.Groups[3].Value; color = pinkColorHex; }
            else { content = m.Groups[4].Value; color = indigoColorHex; }

            sb.Append($"<color={color}>{content}</color>");

            lastIndex = m.Index + m.Length;
        }

        // 마지막 마커 이후 남은 구간 (기본색)
        if (lastIndex < input.Length)
        {
            sb.Append($"<color={rawColorHex}>{input.Substring(lastIndex)}</color>");
        }

        return sb.ToString();
    }
}