using UnityEngine;

public class SanityManager : MonoBehaviour
{
    [SerializeField] private float maxSanity = 100f;
    private float currentSanity;


    private void Awake()
    {
        currentSanity = maxSanity;
    }

    public void DrainSanity(float amount)
    {
        currentSanity -= amount;
        Debug.Log("Current Sanity: " + currentSanity);
    }



}
