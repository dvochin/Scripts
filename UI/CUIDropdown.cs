using UnityEngine;
using UnityEngine.UI;

//###OBS: String enums now based on CProp
//public class CUIDropdown : CUIWidget {
//    [HideInInspector]   Dropdown     _oDropdown;
//	[HideInInspector]   string[]	_aChoices;					// String list of choices we render / interact for our combobox widget

//    public static CUIDropdown Create(CUIPanel oCanvas, string sLabelCombobox, string[] aChoices) {
//        GameObject oDropdownResGO = Resources.Load("UI/CUIDropdown") as GameObject;
//        GameObject oDropdownGO = Instantiate(oDropdownResGO) as GameObject;
//        oDropdownGO.transform.SetParent(oCanvas.transform, false);
//        CUIDropdown oUIDropdown = oDropdownGO.GetComponent<CUIDropdown>();
//        oUIDropdown.Init(oCanvas, sLabelCombobox, aChoices);
//        return oUIDropdown;
//    }

//    public void Init(CUIPanel oCanvas, string sLabelCombobox, string[] aChoices) {
//		_aChoices = aChoices;
//        _oTextLabel = transform.GetChild(0).GetComponent<Text>();           // Label is always first child of prefab
//        _oDropdown  = transform.GetChild(1).GetComponent<Dropdown>();       // Dropdown is always 2nd child of prefab
//		_oTextLabel.text = sLabelCombobox;
//        //_oDropdown.options.RemoveAll();
//        foreach (string sChoice in aChoices) { 
//            Dropdown.OptionData oOptData = new Dropdown.OptionData(sChoice);
//            _oDropdown.options.Add(oOptData);
//        }
//        base.Init(oCanvas, null);
//        _oDropdown.value = -1;                  //###LEARN: Original setting of drop down won't 'take' if we first don't set to other value than start value.
//        base.Init(oCanvas, null);              //###WEAK: We must init twice for above call to work
//    }

//    public override void SetValue(float nValueNew) {        //####BUG!!!: Initial setting doesn't take!  (Too early??)
//        int nValueTrunc = (int)(nValueNew + 0.5f);
//        _oDropdown.value = nValueTrunc;
//    }

//    public void OnDropdownValueChange(int nChoice) {        //####DESIGN: Remove this call?
//        Debug.Log("Dropdown '" + _oProp._sLabel + "' = " + nChoice.ToString());
//        OnValueChange(nChoice);         
//    }
//}
