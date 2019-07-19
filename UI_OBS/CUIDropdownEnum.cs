using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class CUIDropdownEnum : CUIWidget {
    [HideInInspector]   Dropdown		_oDropdown;

    public static CUIDropdownEnum Create(CUIPanel oCanvas, CObj oObj) {
        GameObject oDropdownResGO = Resources.Load("UI/CUIDropdownEnum") as GameObject;
        GameObject oDropdownGO = Instantiate(oDropdownResGO) as GameObject;
        oDropdownGO.transform.SetParent(oCanvas.transform, false);
        CUIDropdownEnum oUIDropdown = oDropdownGO.GetComponent<CUIDropdownEnum>();
        oUIDropdown.Init(oCanvas, oObj);
        return oUIDropdown;
    }

    public override void Init(CUIPanel oCanvas, CObj oObj) {
        _oTextLabel = transform.GetChild(0).GetComponent<Text>();           // Label is always first child of prefab
        _oDropdown  = transform.GetChild(1).GetComponent<Dropdown>();       // Dropdown is always 2nd child of prefab

		if (oObj._oEnumChoices_OBS != null) { 
			FieldInfo[] aFieldsEnum = oObj._oEnumChoices_OBS.GetFields();
			foreach (FieldInfo oFieldInfo in aFieldsEnum) { 
				if (oFieldInfo.Name != "value__") {               // Enums have a hidden 'value__' entry.  Don't add it to drop box...
					Dropdown.OptionData oOptData = new Dropdown.OptionData(oFieldInfo.Name);
					_oDropdown.options.Add(oOptData);
				}
			}
		} else if (oObj._aStringChoices_OBS != null) {
			foreach (string sChoice in oObj._aStringChoices_OBS) { 
				Dropdown.OptionData oOptData = new Dropdown.OptionData(sChoice);
				_oDropdown.options.Add(oOptData);
			}
		} else {
			CUtility.ThrowExceptionF("Exception in CUIDropdownEnum.  CObj '{0}' has no choices defined!", oObj);
		}
        base.Init(oCanvas, oObj);
        _oDropdown.value = -1;                  //###INFO: Original setting of drop down won't 'take' if we first don't set to other value than start value.
        base.Init(oCanvas, oObj);              //###WEAK: We must init twice for above call to work
    }

    public override void SetValue(float nValueNew) {        //####BUG!!!: Initial setting doesn't take!  (Too early??)
        int nValueTrunc = (int)(nValueNew + 0.5f);
        _oDropdown.value = nValueTrunc;
    }

    public void OnDropdownValueChange(int nChoice) {        //####DESIGN: Remove this call?
        Debug.Log("Dropdown '" + _oObj._sName + "' = " + nChoice.ToString());
        OnValueChange(nChoice);         
    }
}
