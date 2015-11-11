using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class CUIDropdown : CUIWidget {
    [HideInInspector]   public Dropdown     _oDropdown;

    public static CUIDropdown Create(CUICanvas oCanvas, CProp oProp, FieldInfo[] aFieldsEnum) {
        GameObject oDropdownResGO = Resources.Load("UI/CUIDropdown") as GameObject;
        GameObject oDropdownGO = Instantiate(oDropdownResGO) as GameObject;
        oDropdownGO.transform.SetParent(oCanvas.transform, false);
        CUIDropdown oUIDropdown = oDropdownGO.GetComponent<CUIDropdown>();
        oUIDropdown.Init(oCanvas, oProp, aFieldsEnum);
        return oUIDropdown;
    }

    public void Init(CUICanvas oCanvas, CProp oProp, FieldInfo[] aFieldsEnum) {
        _oTextLabel = transform.GetChild(0).GetComponent<Text>();           // Label is always first child of prefab
        _oDropdown  = transform.GetChild(1).GetComponent<Dropdown>();       // Dropdown is always 2nd child of prefab
        //_oDropdown.options.RemoveAll();
        foreach (FieldInfo oFieldInfo in aFieldsEnum) { 
            if (oFieldInfo.Name != "value__") {               // Enums have a hidden 'value__' entry.  Don't add it to drop box...
                Dropdown.OptionData oOptData = new Dropdown.OptionData(oFieldInfo.Name);
                _oDropdown.options.Add(oOptData);
            }
        }
        base.Init(oCanvas, oProp);
        _oDropdown.value = -1;                  //###LEARN: Original setting of drop down won't 'take' if we first don't set to other value than start value.
        base.Init(oCanvas, oProp);              //###WEAK: We must init twice for above call to work
    }

    public override void SetValue(float nValueNew) {        //####BUG!!!: Initial setting doesn't take!  (Too early??)
        int nValueTrunc = (int)(nValueNew + 0.5f);
        _oDropdown.value = nValueTrunc;
    }

    public void OnDropdownValueChange(int nChoice) {        //####DESIGN: Remove this call?
        Debug.Log("Dropdown '" + _oProp._sLabel + "' = " + nChoice.ToString());
        OnValueChange(nChoice);         
    }
}
