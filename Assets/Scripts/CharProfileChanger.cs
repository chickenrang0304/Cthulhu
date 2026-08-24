using UnityEngine;
using UnityEngine.UI;

public class CharProfileChanger : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite sprite1; // 67% 이상
    [SerializeField] private Sprite sprite2; // 34% 이상
    [SerializeField] private Sprite sprite3; // 그 이하

    [Header("References")]
    [SerializeField] private Image profileImage; // 스프라이트를 표시할 UI Image

    public void UpdateProfile(float current, float max)
    {
        float ratio = current / max;

        if (ratio >= 0.67f)
        {
            profileImage.sprite = sprite1;
        }
        else if (ratio >= 0.34f)
        {
            profileImage.sprite = sprite2;
        }
        else
        {
            profileImage.sprite = sprite3;
        }
    }
}