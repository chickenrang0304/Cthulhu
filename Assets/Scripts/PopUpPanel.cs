using UnityEngine;

public class PopupUI : MonoBehaviour
{
    [SerializeField] private GameObject popupPanel;

    public void OpenPopup()
    {
        popupPanel.SetActive(true);
    }

    public void ClosePopup()
    {
        popupPanel.SetActive(false);
    }
}