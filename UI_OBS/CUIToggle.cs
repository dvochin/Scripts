using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class CUIToggle : CUIWidget {
    [HideInInspector]   public Toggle     _oToggle;

    public static CUIToggle Create(CUIPanel oCanvas, CObj oObj) {
        GameObject oToggleResGO = Resources.Load("UI/CUIToggle") as GameObject;
        GameObject oToggleGO = Instantiate(oToggleResGO) as GameObject;
        oToggleGO.transform.SetParent(oCanvas.transform, false);
        CUIToggle oUIToggle = oToggleGO.GetComponent<CUIToggle>();
        oUIToggle.Init(oCanvas, oObj);
        return oUIToggle;
    }

    public override void Init(CUIPanel oCanvas, CObj oObj) {
        _oToggle = GetComponent<Toggle>();                              // Toggle is our node
        _oTextLabel = transform.GetChild(1).GetComponent<Text>();       // Label is always 2nd child of prefab
        base.Init(oCanvas, oObj);
    }

    public override void SetValue(float nValueNew) {
        _oToggle.isOn = nValueNew != 0.0f;
    }

    public void OnToggleValueChange(bool bSelected) {
        //_oObj.Set(bSelected ? 0.0f : 1.0f);            //###NOTE: Won't work!  'bSelected' always false regardless of toggle!
        _oObj.Set(_oToggle.isOn ? 1.0f : 0.0f);            // Note the inversion.  This is where we 'toggle'
    }
}
