using UnityEngine;
using UnityEngine.InputSystem;

public class panime : MonoBehaviour
{
    private InputAction spaceOnOff;
    [SerializeField] private GameObject popupPanel;
    [SerializeField]  private Animator animator;

    void Start()
    {
        spaceOnOff = InputSystem.actions.FindAction("Paper");
    }
    public void SetOnOff()
    {
        bool tf = animator.GetBool("isSel");
        animator.SetBool("isSel", !tf);
    }

    private void Update()
    {
        // 버튼형 액션: 이번 프레임에 눌렸는지
        if (spaceOnOff.WasPressedThisFrame())
            SetOnOff();
    }
}
