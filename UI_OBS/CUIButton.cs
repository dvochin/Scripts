using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class CUIButton : CUIWidget {
    [HideInInspector]   public Button     _oButton;

    public static CUIButton Create(CUIPanel oCanvas, CObj oObj) {
        GameObject oButtonResGO = Resources.Load("UI/CUIButton") as GameObject;
        GameObject oButtonGO = Instantiate(oButtonResGO) as GameObject;
        oButtonGO.transform.SetParent(oCanvas.transform, false);
        CUIButton oUIButton = oButtonGO.GetComponent<CUIButton>();
        oUIButton.Init(oCanvas, oObj);
        return oUIButton;
    }

    public override void Init(CUIPanel oCanvas, CObj oObj) {
        _oButton = GetComponent<Button>();                              // Button is our node
        _oTextLabel = transform.GetChild(0).GetComponent<Text>();       // Label is always 1st child of prefab
        base.Init(oCanvas, oObj);
    }

    public override void SetValue(float nValueNew) { }

    public void OnButtonClick() {
        _oObj.Set(1.0f);
    }
}
