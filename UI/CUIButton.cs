using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class CUIButton : CUIWidget {
    [HideInInspector]   public Button     _oButton;

    public static CUIButton Create(CUICanvas oCanvas, CProp oProp) {
        GameObject oButtonResGO = Resources.Load("UI/CUIButton") as GameObject;
        GameObject oButtonGO = Instantiate(oButtonResGO) as GameObject;
        oButtonGO.transform.SetParent(oCanvas.transform, false);
        CUIButton oUIButton = oButtonGO.GetComponent<CUIButton>();
        oUIButton.Init(oCanvas, oProp);
        return oUIButton;
    }

    public override void Init(CUICanvas oCanvas, CProp oProp) {
        _oButton = GetComponent<Button>();                              // Button is our node
        _oTextLabel = transform.GetChild(0).GetComponent<Text>();       // Label is always 1st child of prefab
        base.Init(oCanvas, oProp);
    }

    public override void SetValue(float nValueNew) { }

    public void OnButtonClick() {
        _oProp.PropSet(1.0f);
    }
}
