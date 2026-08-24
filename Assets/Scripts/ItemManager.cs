using UnityEngine;

public class ItemManager : MonoBehaviour
{
    [SerializeField] private SanityManager sanityManager;
    [SerializeField] private float coffeeRestoreAmount = 20f; // 커피로 회복할 양

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
}