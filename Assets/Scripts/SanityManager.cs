using UnityEngine;
// using UnityEngine.SceneManagement; // 이 using은 지워도 되고 남겨도 되지만, 어차피 아래서 풀네임을 쓸 거라 의미 없음

public class SanityManager : MonoBehaviour
{
    [SerializeField] private float maxSanity = 100f;
    private float currentSanity;

    public SanityBarUIManager SanBarman;
    public CharProfileChanger ProfileChanger;
    public PanicEffectUI PanicEffect;

    [Header("배드엔딩 연동")]
    [SerializeField] private string badEndingSceneName = "DefeatScene";
    private bool badEndingTriggered = false;

    private void Start()
    {
        currentSanity = maxSanity;
        SanBarman.Applyvalue(currentSanity, maxSanity);
        ProfileChanger.UpdateProfile(currentSanity, maxSanity);
        PanicEffect.UpdateEffect(currentSanity, maxSanity);
    }

    public void DrainSanity(float amount)
    {
        currentSanity -= amount;
        currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);
        Debug.Log("Current Sanity: " + currentSanity);
        SanBarman.Applyvalue(currentSanity, maxSanity);
        ProfileChanger.UpdateProfile(currentSanity, maxSanity);
        PanicEffect.UpdateEffect(currentSanity, maxSanity);

        CheckBadEnding();
    }

    public void RestoreSanity(float amount)
    {
        currentSanity += amount;
        currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);
        Debug.Log("Current Sanity: " + currentSanity);
        SanBarman.Applyvalue(currentSanity, maxSanity);
        ProfileChanger.UpdateProfile(currentSanity, maxSanity);
        PanicEffect.UpdateEffect(currentSanity, maxSanity);
    }

    private void CheckBadEnding()
    {
        if (badEndingTriggered) return;

        if (currentSanity <= 0f)
        {
            badEndingTriggered = true;
            // "SceneManager"만 쓰면 친구가 만든 동명의 클래스와 충돌하니
            // 풀네임(UnityEngine.SceneManagement.SceneManager)으로 명시해서 확실히 구분
            UnityEngine.SceneManagement.SceneManager.LoadScene(badEndingSceneName);
        }
    }
}