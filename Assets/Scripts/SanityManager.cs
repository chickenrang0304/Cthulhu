using UnityEngine;

public class SanityManager : MonoBehaviour
{
    [SerializeField] private float maxSanity = 100f;
    private float currentSanity;

    public SanityBarUIManager SanBarman;
    public CharProfileChanger ProfileChanger; // 추가

    private void Awake()
    {
        currentSanity = maxSanity;
    }
    private void Start()
    {
        SanBarman.Applyvalue(currentSanity, maxSanity);
        ProfileChanger.UpdateProfile(currentSanity, maxSanity); // 추가
    }
    public void DrainSanity(float amount)
    {
        currentSanity -= amount;
        Debug.Log("Current Sanity: " + currentSanity);
        SanBarman.Applyvalue(currentSanity, maxSanity);
        ProfileChanger.UpdateProfile(currentSanity, maxSanity); // 추가
    }
}