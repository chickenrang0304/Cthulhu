using UnityEngine;

public class FilterOnOffUI : MonoBehaviour
{
    private Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       animator = GetComponent<Animator>();
    }

    public void SetOnOff(bool tf)
    {
        animator.SetBool("isOn", tf);
    }
}
