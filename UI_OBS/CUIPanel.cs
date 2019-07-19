/*###DISCUSSION: GUI
=== DEV ===
- Title??
- Object names need a selector text
- Add concept of 'currently selected object'

=== NEXT ===
- Add ability to re-query scene

=== TODO ===
- Objects receive the OnEditingBegin() and OnEditingEnd() messages.
- Add the capacity of the panel to have 'action buttons' rerouted to owner objects
- Use interfaces throughtout for easy portability.
- Remove link to CObj when destroying!
- Fully remove iGUI

=== DESIGN ===

=== PROBLEMS ===
- Panel background
- X stretches
- ActorGuiPin centering based on top center.  Center-center is better?
- Combo-box selector cannot select all options on short GUI menus (combo box renders on top)

=== WISHLIST ===
- Separator ugly
- Tooltips!!

*/

//###OBS

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CUIPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {      //###DESIGN19: Rename to represent property editor more?    //###OBS:
	CUICanvas       _oCanvas;							// The canvas that owns us.
	List<CObj>	_aObjects = new List<CObj>();    // The flattened list of our game-global CObj tree at the time of creation.  Enables complete editing of everything currently in the scene
	CObj			_oObj_CurrentlyVisible;				// The CObj currently visible / rendered by GUI widgets.  Used to efficiently switch from CObj to CObj when this property editor can edit multiple objects.
    Text			_oTextLabel;                        // The text primitive that draws the label for this panel.
	public bool		_bCreateSelector;					// If true we create a combo-box 'selector' so user can manually select other objects in the scene to view/edit

	CUIDropdownEnum	_oObjChooser_ComboboxWidget;		// The 'chooser combobox' widget that is created only if _aObjects contains more than one CObj
	CObj			_oObjChooser_Prop;					// The property we create when editing a collection of objects in _aObjects.  This simply contains the name of each object for proper selection in _oUIWidgetChooserCombobox
	//CObjGrp		_oObjChooser_PropGrp;				// The property group we create when editing a collection of objects in _aObjects.  This simply contains the name of each object for proper selection in _oUIWidgetChooserCombobox
	CObj			_oObjChooser_Obj;					// The dummy object we create to properly host _oObjChooser_Prop above


    public static CUIPanel Create(CUICanvas oCanvas, string sNameDialogLabel, CObj oObjCurrent = null, bool bCreateSelector = false) {
        GameObject oPanelResGO = Resources.Load("UI/CUIPanel") as GameObject;
        GameObject oPanelGO = Instantiate(oPanelResGO) as GameObject;
		oPanelGO.name = "CUIPanel-" + oCanvas.gameObject.name;
		oPanelGO.transform.SetParent(oCanvas.transform, false);
        oPanelGO.transform.localPosition = Vector3.zero;
        oPanelGO.transform.localRotation = Quaternion.identity;
        CUIPanel oPanel = oPanelGO.GetComponent<CUIPanel>();
        oPanel.Init(oCanvas, sNameDialogLabel, oObjCurrent, bCreateSelector);
        return oPanel;
    }

	void Init(CUICanvas oCanvas, string sNameDialogLabel, CObj oObjCurrent = null, bool bCreateSelector = false) {
		_oCanvas = oCanvas;
		_bCreateSelector = bCreateSelector;
		Util_FlattenTreeOfObjects_RECURSIVE(CGame._oObj, ref _aObjects);

		//=== Render the panel's label title (taken from the object we're editing) ===
		_oTextLabel = transform.GetChild(0).GetChild(0).GetComponent<Text>();           // Label is always first child of first child of panel
		_oTextLabel.text = sNameDialogLabel;

		//=== Based on if we create a selector or not finish the creation of this panel ===
		if (_bCreateSelector) {
			ObjectSelect_Rebuild();							// If we have a selector we first populate it...
			ObjectSelect_DoChangeSelection(oObjCurrent);	//... then we change the combobox selection to the current object
		} else {
			ObjectSelect_OnChangeSelection(oObjCurrent);	// If we don't have a selector directly invoke the buildup of the properties of the current object
		}
	}

	public void ObjectSelect_Rebuild() {
		int nChoice = 0;
		string[] aStringChoices = new string[_aObjects.Count];
		foreach (CObj oObj in _aObjects)
			aStringChoices[nChoice++] = oObj._sName;
		//=== Construct the combobox multi-object chooser ===
		_oObjChooser_Obj = new CObj(null, "Dummy Panel Chooser Object");	//###WEAK: For us to have a fully populated / working we have to create a fake CObj  ###IMPROVE: Make less convoluted
		_oObjChooser_Obj.Event_PropertyValueChanged += Event_PropertyChangedValue;         // Add event so we're notified when user changes the chooser widget
		//###BROKEN
		//_oObjChooser_PropGrp = new CObjGrp(_oObjChooser_Obj, "Dummy Panel Chooser Property Group");
		//_oObjChooser_Prop = new CObj(_oObjChooser_PropGrp, 0, "", "", 0, 0, _aObjects.Count- 1, "Select form one of these items to edit the properties of that object.", 0, aStringChoices);
        _oObjChooser_ComboboxWidget = CUIDropdownEnum.Create(this, _oObjChooser_Prop);
	}

	public void ObjectSelect_DoChangeSelection(CObj oObjSelect) {        // We change the selection by setting the proper item in the combo box.  This will in turn result in 'ObjectSelect_OnChangeSelection()' being called to do the actual work
		int nIndex = _aObjects.FindIndex(oObj => (oObj == oObjSelect));		//###INFO: How to use predicates to find in a collection
		_oObjChooser_ComboboxWidget.SetValue(nIndex);
	}

	void ObjectSelect_OnChangeSelection(CObj oObj) {
		Debug.LogFormat("CUIPanel changes its rendered widgets to render property group '{0}'", oObj._sName);

		//###BROKEN
		////=== Destroy the controls from the previously-rendered CObj (if one exists) ===
		//if (_oObj_CurrentlyVisible != null) {
		//	foreach (CObjGrp oObjGrp in _oObj_CurrentlyVisible._aChildren) {
		//		_oObj.Widget_Separator_Destroy();
		//		foreach (CObj oObj in _oObj._aChildren)
		//			oObj.Widget_Destroy();
		//	}
		//}

  //      //=== Construct the dialog's content dependent on what type of dialog it is ===
		//foreach (CObjGrp oObjGrp in oObj._aChildren) {
		//	_oObj.Widget_Separator_Create(this);
		//	int nProps = _oObj._aChildren.Length;
		//	CObj[] aProps_Sorted = new CObj[nProps];
		//	System.Array.Copy(_oObj._aChildren, aProps_Sorted, nProps);
		//	string[] aProps_Names = new string[nProps];
		//	for (int nProp = 0; nProp < nProps; nProp++)
		//		aProps_Names[nProp] = aProps_Sorted[nProp]._sName;
		//	System.Array.Sort(aProps_Names, aProps_Sorted);				//###INFO: How to easily sort in C#
		//	foreach (CObj oObj in aProps_Sorted)
  //              oObj.Widget_Create(this);
  //      }

		//=== This object is now visible.  Its widgets will need to be destroyed if ever the user changes selection to edit another object ===
		_oObj_CurrentlyVisible = oObj;
		_oCanvas.UpdateCanvasSize();             // Created a new panel.  Need to update our size

		//###OBS: User selected a node with combobox selector.  Automatically assign to one of the wands?  (But... when to restore other body??)
		//MonoBehaviour oObjMB = oObj._iObjOwner as MonoBehaviour;
		//if (oObjMB)
		//	CGame._oVrWandL.AssignToObject_HACK(oObjMB.transform);
	}


	void Util_FlattenTreeOfObjects_RECURSIVE(CObj oObj, ref List<CObj> aObjects) {		//###IMPROVE: Add filtering so some panels have only pertinent objects
		aObjects.Add(oObj);
		if (oObj._aChildren != null) {
			foreach (CObj oObjChild in oObj._aChildren)      //###IMPROVE: Sort by name?
				Util_FlattenTreeOfObjects_RECURSIVE(oObjChild, ref aObjects);
		}
	}

	public void OnButtonClose() {
        Destroy(gameObject);                // Destroy the entire canvas to remove it from the scene.  //####SOON: Notify CObj
    }

    public void OnPointerEnter(PointerEventData eventData) {
        //Debug.LogFormat("UI Enter: " + eventData.ToString());
		if (CGame._oCursor != null)
			CGame._oCursor._oCurrentGuiObjOwnerect_HACK = transform;        //###HACK: Let cursor know user is over this panel (needed for depth adjustments)  ###IMPROVE: Can find a way to get this info in CCursor??
    }
    public void OnPointerExit(PointerEventData eventData) {
		//Debug.LogFormat("UI Exit: " + eventData.ToString());
		if (CGame._oCursor != null)
			CGame._oCursor._oCurrentGuiObjOwnerect_HACK = null;
    }

	void Event_PropertyChangedValue(object sender, EventArgs_PropertyValueChanged oArgs) {      // Fired everytime user adjusts a property.
		Debug.LogFormat("CUIPanel changes viewed / editing object because property '{0}' changed to value {1}", oArgs.CObj._sNameInCodebase, oArgs.CObj._nValue);
		ObjectSelect_OnChangeSelection(_aObjects[(int)oArgs.CObj._nValue] as CObj);
	}
}
