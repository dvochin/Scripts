using UnityEngine;

public class CUIPanelToggle : MonoBehaviour
{
    public void TogglePanel(GameObject panel)
    {
        panel.SetActive(!panel.activeSelf);
    }
}