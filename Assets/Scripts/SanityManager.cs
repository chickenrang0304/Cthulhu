using UnityEngine;

public class SanityManager : MonoBehaviour
{
    [SerializeField] private float maxSanity = 100f;
    private float currentSanity;

    public SanityBarUIManager SanBarman;
    public CharProfileChanger ProfileChanger;
    public PanicEffectUI PanicEffect;

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
    }

    public void RestoreSanity(float amount) // 추가
    {
        currentSanity += amount;
        currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);
        Debug.Log("Current Sanity: " + currentSanity);
        SanBarman.Applyvalue(currentSanity, maxSanity);
        ProfileChanger.UpdateProfile(currentSanity, maxSanity);
        PanicEffect.UpdateEffect(currentSanity, maxSanity);
    }
}