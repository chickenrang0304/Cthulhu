using UnityEngine;

public class ItemManager : MonoBehaviour
{
    [SerializeField] private SanityManager sanityManager;
    [SerializeField] private PersuasionManager persuasionManager; // 담배 아이템용: 설득도 조작을 위해 추가 연결

    [Header("커피")]
    [SerializeField] private float coffeeRestoreAmount = 20f; // 커피로 회복할 SAN 양

    [Header("담배")]
    [SerializeField] private float cigaretteSanityRestoreAmount = 50f;  // 담배로 회복할 SAN 양 (커피보다 크게)
    [SerializeField] private float cigarettePersuasionCost = 15f;       // 담배 사용 시 깎이는 설득도 양

    public void UseMagnifyingGlass()
    {
        foreach (var tracker in CorrectAnswerTracker.ActiveTrackers)
        {
            if (tracker.IsCorrect())
            {
                tracker.SetHighlight(true);
            }
        }
    }

    public void UseCoffe()
    {
        sanityManager.RestoreSanity(coffeeRestoreAmount);
    }

    // 담배 아이템: 설득도를 깎는 대신 SAN을 크게 회복시키는 트레이드오프 아이템.
    // "긴장을 풀지만 상대에게 신뢰를 잃는다" 같은 컨셉에 맞는 페널티형 회복 스킬.
    public void UseCigarette()
    {
        // 설득도를 음수로 깎는다. AddPersuasion 내부에서 Mathf.Clamp(current + amount, min, max)를
        // 처리하기 때문에, 이미 설득도가 낮아서 0 밑으로 못 내려가는 상황이어도 에러 없이 0에서 멈춘다.
        persuasionManager.AddPersuasion(-cigarettePersuasionCost);

        // SAN을 크게 회복. SanityManager의 RestoreSanity가 내부적으로
        // 최대치를 넘지 않게 클램프 처리하고 있다는 전제하에 그대로 호출.
        sanityManager.RestoreSanity(cigaretteSanityRestoreAmount);
    }
}