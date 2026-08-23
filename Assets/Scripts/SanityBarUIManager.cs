using UnityEngine;

public class SanityBarUIManager : MonoBehaviour
{
    public GameObject HPBar;
    public GameObject currentHPBar;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Applyvalue(float current,float max) //현재 체력 UI 비율에 맞게 적용
    {
        float hpRatio = current / max;
        RectTransform CRTHPBar = currentHPBar.GetComponent<RectTransform>();
        RectTransform MaxHPBar = HPBar.GetComponent<RectTransform>();

        CRTHPBar.sizeDelta = new Vector2(MaxHPBar.sizeDelta.x , MaxHPBar.sizeDelta.y * hpRatio);
    }
}
