using UnityEngine;

public class ItemManager : MonoBehaviour
{
    [SerializeField] private SanityManager sanityManager;
    [SerializeField] private PersuasionManager persuasionManager;

    [Header("커피")]
    [SerializeField] private float coffeeRestoreAmount = 10f;
    [SerializeField] private GameObject coffeeItemObject; 

    [Header("돋보기")]
    [SerializeField] private GameObject magnifyingGlassItemObject;

    [Header("담배")]
    [SerializeField] private float cigaretteSanityRestoreAmount = 15f;
    [SerializeField] private float cigarettePersuasionCost = 5f;
    [SerializeField] private GameObject cigaretteItemObject;

    private void Awake()
    {
        SetupItemTooltips();
    }

    private void SetupItemTooltips()
    {
        AddTooltip(coffeeItemObject, $"커피\n정신력 +{coffeeRestoreAmount:0}");
        AddTooltip(magnifyingGlassItemObject, "돋보기\n정답 선택지를 표시합니다");
        AddTooltip(
            cigaretteItemObject,
            $"담배\n정신력 +{cigaretteSanityRestoreAmount:0}\n설득도 -{cigarettePersuasionCost:0}");
    }

    private static void AddTooltip(GameObject itemObject, string description)
    {
        if (itemObject == null) return;

        var tooltip = itemObject.GetComponent<ItemHoverTooltip>();
        if (tooltip == null)
        {
            tooltip = itemObject.AddComponent<ItemHoverTooltip>();
        }

        tooltip.SetDescription(description);
    }

    public void UseMagnifyingGlass()
    {
        // 이미 사용해서 비활성화된 상태라면 아무것도 하지 않고 즉시 종료
        if (magnifyingGlassItemObject != null && !magnifyingGlassItemObject.activeSelf) return;

        foreach (var tracker in CorrectAnswerTracker.ActiveTrackers)
        {
            if (tracker.IsCorrect())
            {
                tracker.SetHighlight(true);
            }
        }
        DisableItem(magnifyingGlassItemObject);
    }

    public void UseCoffe()
    {
        if (coffeeItemObject != null && !coffeeItemObject.activeSelf) return;

        sanityManager.RestoreSanity(coffeeRestoreAmount);

        DisableItem(coffeeItemObject);
    }

    // 담배
    public void UseCigarette()
    {
        if (cigaretteItemObject != null && !cigaretteItemObject.activeSelf) return;

        persuasionManager.AddPersuasion(-cigarettePersuasionCost);
        sanityManager.RestoreSanity(cigaretteSanityRestoreAmount);

        DisableItem(cigaretteItemObject);
    }

    // 아이템 오브젝트 비활성화하는놈
    private void DisableItem(GameObject itemObject)
    {
        if (itemObject == null)
        {
            Debug.LogWarning("[ItemManager] 비활성화할 아이템 오브젝트가 연결되어 있지 않습니다.");
            return;
        }

        itemObject.SetActive(false);
    }
}
