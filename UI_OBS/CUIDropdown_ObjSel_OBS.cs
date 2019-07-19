using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

//public class CUIDropdown_ObjSel : CUIWidget {		//###OBS: Now in CUIPanel
//    [HideInInspector]   Dropdown		_oDropdown;
//	[HideInInspector]   List<CObj>   _aObjects = new List<CObj>();	// The flattened list of our game-global CObj tree at the time of creation.  Enables complete editing of everything currently in the scene

//    //public static CUIDropdown_ObjSel Create(CUIPanel oCanvas) {
//    //    GameObject oDropdownResGO = Resources.Load("UI/CUIDropdown") as GameObject;
//    //    GameObject oDropdownGO = Instantiate(oDropdownResGO) as GameObject;
//    //    oDropdownGO.transform.SetParent(oCanvas.transform, false);
//    //    CUIDropdown_ObjSel oUIDropdown = oDropdownGO.GetComponent<CUIDropdown_ObjSel>();
//    //    oUIDropdown.Init(oCanvas);
//    //    return oUIDropdown;
//    //}

//	void Util_FlattenTreeOfObjects_RECURSIVE(CObj oObj, ref List<CObj> aObjects) {
//		aObjects.Add(oObj);
//		if (oObj._aChildren != null) {
//			foreach (CObj oObjChild in oObj._aChildren)		//###IMPROVE: Sort by name?
//				Util_FlattenTreeOfObjects_RECURSIVE(oObjChild, ref aObjects);
//		}
//	}

//	void Start() { 
//		//public void Init(CUIPanel oCanvas) {
//		Util_FlattenTreeOfObjects_RECURSIVE(CGame._oObj, ref _aObjects);

//		_oDropdown  = GetComponent<Dropdown>();       // Dropdown is always 2nd child of prefab
//        _oDropdown.options.RemoveAll(null);
//		foreach (CObj oObj in _aObjects) { 
//            Dropdown.OptionData oOptData = new Dropdown.OptionData(oObj._sName);
//            _oDropdown.options.Add(oOptData);
//        }
//        //base.Init(oCanvas, null);
//        //_oDropdown.value = -1;                  //###INFO: Original setting of drop down won't 'take' if we first don't set to other value than start value.
//        //base.Init(oCanvas, null);              //###WEAK: We must init twice for above call to work
//    }

//	public override void SetValue(float nValueNew) {        //####BUG!!!: Initial setting doesn't take!  (Too early??)
//		//int nValueTrunc = (int)(nValueNew + 0.5f);
//		//_oDropdown.value = nValueTrunc;
//	}+

//	public void OnDropdownValueChange(int nChoice) {        //####DESIGN: Remove this call?
//        Debug.Log("Dropdown '" + _oObj._sLabel + "' = " + nChoice.ToString());
//        //OnValueChange(nChoice);         
//    }
//}
