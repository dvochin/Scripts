/*###DISCUSSION: GUI
=== NEW DESIGN ===

=== REVIVE ===
- Remove crappy gasket in CUtility
- Objects receive the OnEditingBegin() and OnEditingEnd() messages.
- Add the capacity of the panel to have 'action buttons' rerouted to owner objects
- Use interfaces throughtout for easy portability.

=== NEXT ===
- Remove link to CProp when destroying!
- Fully remove iGUI

=== OLD? ===

=== DESIGN ===

=== APPEARANCE ===
- Panel background
- X stretches

=== PROBLEMS ===

=== PROBLEMS??? ===

=== WISHLIST ===
- Separator ugly
- Tooltips!!

*/

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CUIPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {		//###DESIGN19: Rename to represent property editor more?

	object[]		_aObjects;						// The array of CObjects we edit.  A 'choose combobox' is created if this collection has more than one CObject
	CObject			_oObj_CurrentlyVisible;				// The CObject currently visible / rendered by GUI widgets.  Used to efficiently switch from CObject to CObject when this property editor can edit multiple objects.
    Text			_oTextLabel;						// The text primitive that draws the label for this panel.

	CUIDropdownEnum	_oPropChooser_ComboboxWidget;		// The 'chooser combobox' widget that is created only if _aObjects contains more than one CObject
	CProp			_oPropChooser_Prop;					// The property we create when editing a collection of objects in _aObjects.  This simply contains the name of each object for proper selection in _oUIWidgetChooserCombobox
	CPropGrp		_oPropChooser_PropGrp;				// The property group we create when editing a collection of objects in _aObjects.  This simply contains the name of each object for proper selection in _oUIWidgetChooserCombobox
	CObject			_oPropChooser_Obj;					// The dummy object we create to properly host _oPropChooser_Prop above


    public static CUIPanel Create(CUICanvas oCanvas, string sNameDialogLabel, string sNameChooserLabel = null, params object[] aObjects) {
        GameObject oPanelResGO = Resources.Load("UI/CUIPanel") as GameObject;
        GameObject oPanelGO = Instantiate(oPanelResGO) as GameObject;
        oPanelGO.transform.SetParent(oCanvas.transform, false);
        oPanelGO.transform.localPosition = Vector3.zero;
        oPanelGO.transform.localRotation = Quaternion.identity;
        CUIPanel oPanel = oPanelGO.GetComponent<CUIPanel>();
        oPanel.Init(sNameDialogLabel, sNameChooserLabel, aObjects);
        return oPanel;
    }

	void Init(string sNameDialogLabel, string sNameChooserLabel = null, params object[] aObjects) {
		_aObjects = aObjects;

		//=== Render the panel's label title (taken from the object we're editing) ===
        _oTextLabel = transform.GetChild(0).GetChild(0).GetComponent<Text>();           // Label is always first child of first child of panel
		_oTextLabel.text = sNameDialogLabel;

		//=== Construct the combobox multi-object picker if appropriate ===
		bool bCreateChooserWidget = (_aObjects.Length > 1);         // We only create the 'chooser combobox' control if we can edit more than one object.

		if (bCreateChooserWidget) {
			int nChoice = 0;
			string[] aStringChoices = new string[_aObjects.Length];
			foreach (object o in _aObjects) {
				CObject oObj = o as CObject;
				aStringChoices[nChoice++] = oObj._sLabelPanel;
			}
			_oPropChooser_Obj = new CObject(null, "Dummy Panel Chooser Object", "Dummy Panel Chooser Object Label");
			_oPropChooser_Obj.Event_PropertyValueChanged += Event_PropertyChangedValue;         // Add event so we're notified when user changes the chooser widget
			_oPropChooser_PropGrp = new CPropGrp(_oPropChooser_Obj, "Dummy Prop Group");
			_oPropChooser_Prop = new CProp(_oPropChooser_PropGrp, 0, sNameChooserLabel, sNameChooserLabel, 0, 0, _aObjects.Length - 1, "Pick one object to view / edit the properties of that object.", 0, aStringChoices);
            _oPropChooser_ComboboxWidget = CUIDropdownEnum.Create(this, _oPropChooser_Prop);
		}

		//=== At init-time we start editing the first CObject in our chooser array ===
		CreateWidgets(_aObjects[0] as CObject);
	}

	void CreateWidgets(CObject oObj) {
		Debug.LogFormat("CUIPanel changes its rendered widgets to render property group '{0}'", oObj._sNameObject);

		//=== Destroy the controls from the previously-rendered CObject (if one exists) ===
		if (_oObj_CurrentlyVisible != null) {
			foreach (CPropGrp oPropGrp in _oObj_CurrentlyVisible._aPropGrps) {
				oPropGrp.Widget_Separator_Destroy();
				foreach (CProp oProp in oPropGrp._aProps)
					oProp.Widget_Destroy();
			}
		}

        //=== Construct the dialog's content dependent on what type of dialog it is ===
		foreach (CPropGrp oPropGrp in oObj._aPropGrps) {
			oPropGrp.Widget_Separator_Create(this);
			foreach (CProp oProp in oPropGrp._aProps)
                oProp.Widget_Create(this);
        }

		_oObj_CurrentlyVisible = oObj;			// This object is now visible.  Its widgets will need to be destroyed if ever the user changes selection to edit another object
	}



    public void OnButtonClose() {
        Destroy(gameObject);                // Destroy the entire canvas to remove it from the scene.  //####SOON: Notify CProp
    }

    public void OnPointerEnter(PointerEventData eventData) {
        //Debug.LogFormat("UI Enter: " + eventData.ToString());
		if (CGame.INSTANCE._oCursor != null)
			CGame.INSTANCE._oCursor._oCurrentGuiObjOwnerect_HACK = transform;        //###HACK: Let cursor know user is over this panel (needed for depth adjustments)  ###IMPROVE: Can find a way to get this info in CCursor??
    }
    public void OnPointerExit(PointerEventData eventData) {
		//Debug.LogFormat("UI Exit: " + eventData.ToString());
		if (CGame.INSTANCE._oCursor != null)
			CGame.INSTANCE._oCursor._oCurrentGuiObjOwnerect_HACK = null;
    }


	void Event_PropertyChangedValue(object sender, EventArgs_PropertyValueChanged oArgs) {      // Fired everytime user adjusts a property.
		Debug.LogFormat("CUIPanel changes viewed / editing object because property '{0}' changed to value {1}", oArgs.PropertyName, oArgs.ValueNew);

		CreateWidgets(_aObjects[(int)oArgs.ValueNew] as CObject);
	}
}
