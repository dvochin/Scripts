using UnityEngine;
using UnityEngine.UI;

public class CUISeparator : CUIWidget {
    public static CUISeparator Create(CUIPanel oPanel, CProp oProp) {
        GameObject oSeperatorResGO = Resources.Load("UI/CUISeparator") as GameObject;
        GameObject oSeparatorGO = Instantiate(oSeperatorResGO) as GameObject;
        oSeparatorGO.transform.SetParent(oPanel.transform, false);
        CUISeparator oUIButton = oSeparatorGO.GetComponent<CUISeparator>();
        //oUIButton.Init(oCanvas, oProp);
        return oUIButton;
    }

    public override void SetValue(float nValueNew) { }
}
