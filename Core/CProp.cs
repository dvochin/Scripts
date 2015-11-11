using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;


public class CProp {							// Important class that abstracts the concept of a 'property' and enables Unity and C++ dll to read/write this property with the other side being notified of the change.
	public CObject		_oObject;				// The object who'se property we hold.  Creates / Destroys / Manages us.
	public int			_nPropEnumOrdinal;
	public string		_sNameProp;				// Name of property as typed in the defining enum
	public string		_sLabel;				// Label of the property as shown in GUI
	public string		_sDescription;
	public float		_nValueLocal;			// The actual property value that is get / set *only* when _nPropFlags::Local is set.  (In non-local mode the C++ dll owns the actual memory of the float that represents our value).  Made private so nobody tries to access thinking they're getting the dll value.
	public float		_nDefault;
	public float		_nMin;
	public float		_nMax;
	public float		_nMinMaxRange;			// = _nMax - _nMin
	public float		_nHotSpotEdit_BeginPosY;	// Value of hotspot Y coordinate to change property from hotspot movement in ChangePropByHotSpotMove()
	public float		_nHotSpotEdit_BeginValue;	// Our value when hotspot editing began.
	public int			_nPropFlags;
	public Type			_oTypeChoice;			// The type of the enum that displays the combo-box choices for this property.  Parsed by GUI to display / choose  the proper enum
	public MethodInfo	_oMethodInfo_OnPropSet;	// The field info of a function on owning object that receives notification when this property changes.

	public float		_nRndVal;				// The actual random value added at the very end of property get/set (kept separated to not mix with user-set value)
	public float		_nRndValSource;			// Randomization source value (at beginning of smoothing iterations)
	public float		_nRndValTarget;			// Randomization target value (at end       of smoothing iterations)
	public float		_nRndValSmoothVelocity; // Smoothing velocity (utilty var needed for smoothing)

	public float        _nAdjustTargetVal;      // Runtime adjust functionality.  What the property value becomes RuntimeAdjust_SetTargetValue() gets passed a nDistThisFrame over _nAdjustDistPerFrame
	public float        _nAdjustDistPerFrame;	// How much movement distance required by frame to set the property to _nAdjustTargetVal

	//public iGUIElement	_oWidgetGUI;			// GUI element we visually render our property to (when shown in a property viewer)
    public CUIWidget    _oUIWidget;             // The widget who draws our property in the Unity UI


	public const int Local			= 1 << 0;			// Property exists in Unity only and doesn't have an equivalent CProp in the C++ dll.  (Properties that are not 'Local' and not 'Blender' are dll-side properties)		###IMPROVE: Have an enum for where property is from!!!
	public const int Blender		= 1 << 1;			// Property exists in Blender only.  Owning CObject *must* have a valid Blender object name for '_sNameBlenderObject' (Properties that are not 'Local' and not 'Blender' are dll-side properties)
	public const int ReadOnly		= 1 << 2;			// Read only property that user can't change through GUI.  ###CHECK: Formerly: Fake property without value or processing.  Only exists to draw a seperating header in GUI
	public const int Hide			= 1 << 3;			// Property doesn't draw anything in GUI
	public const int NeedReset		= 1 << 4;			// Property requires a reset of its owning object.  Automatically sets 'NotifyOnSet'.
	public const int AsCheckbox		= 1 << 5;			// Property takes only the float values of 0.0f or 1.0f and is drawn on GUI as a checkbox
	public const int AsButton		= 1 << 6;			// Property is drawn as a button and does not show a value (e.g. always zero)
	//###IMPROVE: Deactivate bounds if needed with a flag?)

	public const string C_Prefix_OnPropSet	= "OnPropSet_";			// Prefix name of functions of owning object that (if defined) automatically receive notification when this property changes)

	//---------------------------------------------------------------------------	

	public CProp() { }
	public CProp(CObject oObject, int nPropEnumOrdinal, string sLabel, float nDefault, float nMin, float nMax, string sDescription, int nPropFlags, Type oTypeChoice) {
		_oObject			= oObject;
		_nPropEnumOrdinal	= nPropEnumOrdinal;
		_sLabel				= sLabel;
		_nDefault			= nDefault;
		_nValueLocal		= nDefault;			// Reasonable way to init local value.  (Important for init flow)
		_nMin				= nMin;
		_nMax				= nMax;
		_nMinMaxRange		= _nMax - _nMin;
		_sDescription		= sDescription;
		_nPropFlags			= nPropFlags;
		_oTypeChoice		= oTypeChoice;

		FieldInfo[] aFieldsEnum = _oObject._oTypeFieldsEnum.GetFields();
		_sNameProp = aFieldsEnum[nPropEnumOrdinal + 1].Name;		// For some reason reflection on enums returns '_value' for index zero with the real enum fields starting at index 1

		if ((_nPropFlags & Local) == 0 && (_nPropFlags & Blender) == 0) {		// We only have a remote property to connect to if we're non-local
			int nError_PropConnect = ErosEngine.Object_PropConnect(_oObject._hObject, _nPropEnumOrdinal, _sNameProp);
			if (nError_PropConnect != 0)
				Debug.LogWarning(string.Format("WARNING: Non-local property '{0}' of id {1} on object '{2}' could not connect with server DLL equivalent.  Property will not function.", _sNameProp, _nPropEnumOrdinal, _oObject._sNameFull));
		}

		ConnectPropCallback();
		//Debug.Log(string.Format("{0} prop #{1} = '{2}'", _oObject._sNameFull, _nPropEnumOrdinal, _sNameProp));
	}
	public void OnDestroy() {
		if ((_nPropFlags & Local) == 0 && (_nPropFlags & Blender) == 0) {			// We only have a remote property to destroy if we're non-local
			int bSuccess = ErosEngine.Object_PropDestroy(_oObject._hObject, _nPropEnumOrdinal);
			if (bSuccess == 0) {
				Debug.LogError(string.Format("ERROR CProp.ReleaseGlobalHandles().  C++ returns error disconnecting property '{0}' of id {1} on object '{2}'", _sNameProp, _nPropEnumOrdinal, _oObject._sNameFull));
			}
		}
	}

	//---------------------------------------------------------------------------	GET / SET
	public float PropGet() {	
		if ((_nPropFlags & Local) != 0) {			// In local mode we just return our local value
			return _nValueLocal - _nRndVal;			// Remove random value off the very top.  Pushed in  / pulled in at last minute ###CHECK: Assumes we use PropGet() everywhere!  (Not true!!) ###BUG??
		} else if ((_nPropFlags & Blender) != 0) {	// For Blender-side objects we must fetch from related CProp_PropGet() in Blender python
			if (_oObject._sNameBlenderObject == null)
				throw new CException(string.Format("ERROR: PropGet('{0}') on Blender object '{1}' was flagged as a Blender-side property but CObject didn't have Blender object name set.", _sNameProp, _oObject._sNameObject));
			string sCmd = string.Format("CProp_PropGet('{0}','{1}')", _oObject._sNameBlenderObject, _sNameProp);
			string sResult = CGame.gBL_SendCmd("Client", sCmd);		// Will throw if error.
			_nValueLocal = float.Parse(sResult);
			return _nValueLocal;
		} else {
			return ErosEngine.Object_PropGet(_oObject._hObject, _nPropEnumOrdinal) - _nRndVal;
		}
	}

	public float PropSet(float nValueNew) {
		nValueNew += _nRndVal;				// Apply separated random value at the very top

		if (nValueNew < _nMin)
			nValueNew = _nMin;
		if (nValueNew > _nMax)
			nValueNew = _nMax;

		float nValueOld = _nValueLocal;
		bool bIsLocal = (_nPropFlags & Local) != 0;
		bool bIsBlender = (_nPropFlags & Blender) != 0;

		if (bIsLocal) {		// In local mode we just return our local value
			_nValueLocal = nValueNew;
		} else if (bIsBlender) {	// For blender properties we invoke its corresponding CProp_PropSet() method
			if (_oObject._sNameBlenderObject == null)
				throw new CException(string.Format("ERROR: PropSet('{0}') on Blender object '{1}' was flagged as a Blender-side property but CObject didn't have Blender object name set.", _sNameProp, _oObject._sNameObject));
			string sCmd = string.Format("CProp_PropSet('{0}','{1}','{2}')", _oObject._sNameBlenderObject, _sNameProp, nValueNew);
			string sResult = CGame.gBL_SendCmd("Client", sCmd);
			if (sResult != "OK")
				throw new CException(string.Format("ERROR: PropSet('{0}') on Blender object '{1}' failed with error '{2}'", _sNameProp, _oObject._sNameObject, sResult));
		} else {			// For dll-side properties we call the dll.
			_nValueLocal = ErosEngine.Object_PropSet(_oObject._hObject, _nPropEnumOrdinal, nValueNew);	//###IMPROVE: Trap failure conditions from C++ to display warning property wasn't set because of disconnect error!
			if (_nValueLocal != nValueNew)			// Occurs for INSTANCE if we set a value out of range and the C++ set function clipped it with the get function returning the clipped value... Check the valid range in the C++ code!
				Debug.Log(string.Format("Note: PropSet on {0}.{1} was set to {2} but is now {3}", _oObject._sNameFull, _sNameProp, nValueNew, _nValueLocal));	//###NOTE: Not necessarily bad.  Some properties adjust their settings, some round, etc.
		}

		//=== OnPropSet_ and OnReset_ notifications can only be sent when object is fully operational (e.g. not during init) ===
		if (_oObject._bInitialized) {
			//=== Notify owning object if it requested notification upon change ===
			if (_oMethodInfo_OnPropSet != null) {								// If property has a preconfigured 'OnPropSet_' function call it now to notify parent of change
				object[] aArgs = new object[] { nValueOld, nValueNew };
				_oMethodInfo_OnPropSet.Invoke(_oObject._iObj, aArgs);			// CObject's owner has the event properties we need to call, not CObject INSTANCE!!
			}
			//=== Notify owning object of reset if needed by property and object is fully initialized ===
			if ((_nPropFlags & NeedReset) != 0)									// Manually call the object 'OnPropSet_NeedReset' function if property requests it
				_oObject._iObj.OnPropSet_NeedReset(this, nValueOld, _nValueLocal);
		}

		//=== Set our GUI appearance if we're currently displayed ===
		//if (_oWidgetGUI != null)
		//	CProp.SetValueGUI(_oUIWidget, _oWidgetGUI, _sLabel, PropGet());

        //####HACK!: Rethink & cleanup
        if (_oUIWidget != null)
            _oUIWidget.SetValue(PropGet());

            //=== Write the just-set property to the script recorder.  This provides a very good starting point to animate a scene ===
        if (CGame.INSTANCE._oScriptRecordUserActions != null) {
			if (_oObject._nBodyID != 0 && _oObject._sNameScriptHandle != null) {	// We only write property set to script recorder on objects that have provided their script-access handle
				if (nValueNew != nValueOld) 			//###IMPROVE!?!?!? Should abort if same earlier?? Safe??
					CGame.INSTANCE._oScriptRecordUserActions.WriteProperty(this);
			}
		}
		return _nValueLocal;
	}

    //---------------------------------------------------------------------------	RUNTIME ADJUST

    public void RuntimeAdjust_SetTargetValue(float nAdjustTargetVal, float nAdjustDistPerFrame) {
		_nAdjustTargetVal		= nAdjustTargetVal;
		_nAdjustDistPerFrame	= nAdjustDistPerFrame;
    }
	public void RuntimeAdjust_SetTargetValue(float nDistThisFrame) {
		// Perform a 'runtime adjustment' of this property to move it toward _nAdjustTargetVal dependant on the distance travelled per frame.  Used to (greatly) stiffen cloth during movement
		if (nDistThisFrame < 0)
			nDistThisFrame = 0;
		if (nDistThisFrame > _nAdjustDistPerFrame)
			nDistThisFrame = _nAdjustDistPerFrame;
		float nBlendPropToAdjusted = nDistThisFrame / _nAdjustDistPerFrame;     // Toward zero means toward property value, toward one is adjusted value
		float nBlendPropToAdjustedInv = 1.0f - nBlendPropToAdjusted;
		float nValueNew = nBlendPropToAdjusted * _nAdjustTargetVal + nBlendPropToAdjustedInv * _nValueLocal;		// Blend property between its own value and target value
		ErosEngine.Object_PropSet(_oObject._hObject, _nPropEnumOrdinal, nValueNew);		//####NOTE: Directly set value without setting _nValueLocal.  ####BUG: Decouples UI from PropGet()!
		//####IMPROVE ###OPT!!!! Move this to C++
	}

    //---------------------------------------------------------------------------	RANDOMIZATION

    public void Randomize_SetNewRandomTarget(float nRndValMin, float nRndValMax) {	// Simple coroutine to efficiently apply randomization to the property value
		_nRndValSource = _nRndValTarget;
		_nRndValTarget = CGame.GetRandom(nRndValMin, nRndValMax);
	}

	public void Randomize_SmoothToTarget(float nPlaceInCycle) {
		float nValOld = PropGet();		// Get the value (with randomization removed right off the top)
		//_nRndVal = Mathf.SmoothDamp(_nRndValSource, _nRndValTarget, ref _nRndValSmoothVelocity, nPlaceInCycle);	// Compute new randomization
		_nRndVal = Mathf.Lerp(_nRndValSource, _nRndValTarget, nPlaceInCycle);	// nPlaceInCycle goes from 0..1 for Lerp value
		PropSet(nValOld);		// Apply the old value again. The random value we just set will be added in PropSet (to keep the two values separated)
		//Debug.Log("Rnd: " + _nRndVal + " Val: " + nValOld + " Cyc:" + nPlaceInCycle);
	}


	//---------------------------------------------------------------------------	GUI

	public int CreateWidget(CPropGroup oPropGrp) {
		if ((_nPropFlags & CProp.Hide) != 0) 							// We don't show in GUI properties marked as hidden... these can only be changed through code.
			return 0;

		if (_oTypeChoice != null) {

			FieldInfo[] aFieldsEnum = _oTypeChoice.GetFields();

			//if (aFieldsEnum.Length <= 2) {								// Multiple choices of two choices or less get radio button, other get combobox.

			//	iGUICheckboxGroup oRadioButtons;											//###WEAK!!!!: Have to ditch combo box because of Unity errors and display as radio buttons!!
			//	oRadioButtons = oPropGrp._oContainer.addElement<iGUICheckboxGroup>();
			//	oRadioButtons.labelWidth = G.C_Gui_LabelWidth / 2;							//###HACK!: Label width on checkboxes
			//	oRadioButtons.layout = iGUICheckboxLayout.Horizontal;
			//	oRadioButtons.type = iGUICheckboxType.Radio;
			//	oRadioButtons.isSingleSelect = true;
			//	foreach (FieldInfo oFieldInfo in aFieldsEnum)
			//		if (oFieldInfo.Name != "value__")				// Enums have a hidden 'value__' entry.  Don't add it to drop box...
			//			oRadioButtons.addOption(oFieldInfo.Name);
			//	oRadioButtons.valueChangeCallback += OnCheckboxValueChange;
			//	oRadioButtons.positionAndSize.Set(0, 0, 1.0f, G.C_Gui_WidgetsHeight);		//###DESIGN! KEEP??? We take the whole panel width for this control unless we have a parent and it has a container
			//	oRadioButtons.labelStyle.fontSize = oRadioButtons.style.fontSize = 13;
			//	oRadioButtons.style.alignment = TextAnchor.LowerLeft;			//###IMPROVE: Checkbox too high... how to lower??
			//	_oWidgetGUI = oRadioButtons;

			//} else {

			//	iGUIDropDownList oDropDown = oPropGrp._oContainer.addElement<iGUIDropDownList>();
			//	oDropDown.labelWidth = G.C_Gui_LabelWidth;
			//	oDropDown.listItemStyle.alignment = oDropDown.style.alignment = TextAnchor.MiddleLeft;			// Default combox box has text centered and its list box is far too high...
			//	oDropDown.listItemStyle.contentOffset = oDropDown.style.contentOffset = new Vector2(8, 0);		// ...reduce to more common appearance...
			//	oDropDown.listItemStyle.fixedHeight = 24;														// ... and shrink height per listbox entry.
			//	oDropDown.visibleListItemCount = 20;
			//	oDropDown.valueChangeCallback += OnDropDownValueChange;
			//	oDropDown.style.fixedHeight = G.C_Gui_WidgetsHeight;					// Normal dropdown box too tall at 27
			//	oDropDown.listItemStyle.fontSize = oDropDown.labelStyle.fontSize = oDropDown.style.fontSize = 13;
			//	foreach (FieldInfo oFieldInfo in aFieldsEnum)
			//		if (oFieldInfo.Name != "value__")				// Enums have a hidden 'value__' entry.  Don't add it to drop box...
			//			oDropDown.addOption(oFieldInfo.Name);
			//	_oWidgetGUI = oDropDown;
			//	_oWidgetGUI.positionAndSize.Set(0, 0, 1, G.C_Gui_WidgetsHeight);

   //         }
            //####SOON ####CLEANUP
            _oUIWidget = CUIDropdown.Create(oPropGrp._oUICanvas, this, aFieldsEnum);

        }
        else if ((_nPropFlags & CProp.AsCheckbox) != 0) {

			//iGUICheckboxGroup oCheckbox;
			//oCheckbox = oPropGrp._oContainer.addElement<iGUICheckboxGroup>();
			//oCheckbox.addOption(_sLabel);
			//oCheckbox.labelWidth = 0;
			//oCheckbox.valueChangeCallback += OnCheckboxValueChange;
			//oCheckbox.positionAndSize.Set(0, 3, 1.0f / (float)oPropGrp._nNumWidgetColumns, G.C_Gui_WidgetsHeight);		//###DESIGN! KEEP??? We take the whole panel width for this control unless we have a parent and it has a container
			//oCheckbox.labelStyle.fontSize = oCheckbox.style.fontSize = 13;
			//oCheckbox.style.alignment = TextAnchor.LowerLeft;			//###IMPROVE: Checkbox too high... how to lower??
			//_oWidgetGUI = oCheckbox;

            _oUIWidget = CUIToggle.Create(oPropGrp._oUICanvas, this);

        }
        else if ((_nPropFlags & CProp.AsButton) != 0) {

			//iGUIButton oButton;
			//oButton = oPropGrp._oContainer.addElement<iGUIButton>();
			//oButton.clickCallback  += OnButtonClicked;
			//oButton.positionAndSize.Set(0, 0, 1.0f / (float)oPropGrp._nNumWidgetColumns, G.C_Gui_WidgetsHeight - 7);	// We take the whole panel width for this control unless we have a parent and it has a container
			//oButton.style.fixedHeight = G.C_Gui_WidgetsHeight;
			//oButton.style.fontSize = 13;
			//_oWidgetGUI = oButton;

            _oUIWidget = CUIButton.Create(oPropGrp._oUICanvas, this);

        }
        else {

			//iGUIFloatHorizontalSlider oSlider = oPropGrp._oContainer.addElement<iGUIFloatHorizontalSlider>();		//###DESIGN: Change appearance of slider when used as bar... like height & color??
			//oSlider.labelWidth = G.C_Gui_LabelWidth;
			//oSlider.min = _nMin;
			//oSlider.max = _nMax;
			//oSlider.valueChangeCallback += OnSliderValueChange;		//###LEARN: Using message events & delegates!
			//oSlider.readOnly = (_nPropFlags & CProp.ReadOnly) != 0;
			//oSlider.labelStyle.fontSize = 13;
			//_oWidgetGUI = oSlider;
			//_oWidgetGUI.positionAndSize.Set(0, 0, 1, G.C_Gui_WidgetsHeight);

            _oUIWidget = CUISlider.Create(oPropGrp._oUICanvas, this);
        }
        //} 
  //      _oWidgetGUI.variableName = _sNameProp;			//###CHECK: Safe with spaces in-between??
		//_oWidgetGUI.label.text = _sLabel;
		//_oWidgetGUI.label.tooltip = _sDescription;

		//if (oPropGrp._nNumWidgetColumns == 1) 		// If our panel group only has a single column, every widget addition expands our height.  If not a single column group then we only have one row and we never grow (size was set during group creation)
		//	oPropGrp._oContainer.setHeight(oPropGrp._oContainer.positionAndSize.height + G.C_Gui_WidgetsHeight);

		CProp.SetValueGUI(_oUIWidget, _sLabel, PropGet());			

		return 1;		//###BUG???: We should return zero on multi-controls per line but strangely I don't see problem in GUI!  Check!!
	}

	public static void SetValueGUI(CUIWidget oUIWidget, string sNameProp, float nValue) {			// Important function called by CProp to update the GUI value of whatever control it's connected to...
		//###DESIGN!!!!!: Causes late-time init in some situations (disorienting!)

		//string sLabelText = string.Format("{0} [{1:F3}]", sNameProp, nValue);					//###TODO: Number specs!!

		//iGUIFloatHorizontalSlider oSlider = oGUI as iGUIFloatHorizontalSlider;
		//if (oSlider != null) {						//###DESIGN: Best way?		###TODO: Add other types
		//	oSlider.value = nValue;
		//	oSlider.label.text = sLabelText;
		//	return;
		//}
		//iGUIProgressBar oBar = oGUI as iGUIProgressBar;
		//if (oBar != null) {						//###DESIGN: Best way?		###TODO: Add other types
		//	oBar.value = nValue;
		//	iGUILabel oLabel = oBar.userData as iGUILabel;
		//	oLabel.label.text = sLabelText;
		//	return;
		//}
		//iGUICheckboxGroup oCheckboxGroup = oGUI as iGUICheckboxGroup;
		//if (oCheckboxGroup != null) {
		//	if (oCheckboxGroup.optionList.Count == 1) {				//###NOTE!!!: iGUI doesn't support single checkbox so we have to use radio buttons with only one entry.  Switch between the two modes here.
		//		if (nValue == 1.0f)
		//			oCheckboxGroup.selectOption(0);
		//		else
		//			oCheckboxGroup.deselectOption(0);
		//	} else {
		//		oCheckboxGroup.selectOption((int)nValue);
		//	}
		//	return;
		//}
		//iGUIDropDownList oDropDown = oGUI as iGUIDropDownList;
		//if (oDropDown != null) {
		//	oDropDown.selectedIndex = (int)nValue;
		//	return;
		//}

        if (oUIWidget != null)              //####TEMP
            oUIWidget.SetValue(nValue);
	}

	

	//---------------------------------------------------------------------------	VALUE CHANGE

	//void OnSliderValueChange(iGUIElement caller) {
	//	iGUIFloatHorizontalSlider oSlider = caller as iGUIFloatHorizontalSlider;
	//	/*float nValueGet = */ PropSet(oSlider.value);
	//}

	//void OnDropDownValueChange(iGUIElement caller) {
	//	iGUIDropDownList oDropDown = caller as iGUIDropDownList;
	//	PropSet(oDropDown.selectedIndex);
	//}

	//void OnCheckboxValueChange(iGUIElement caller) {			//###CHECK: Possible bug now with radio buttons!
	//	iGUICheckboxGroup oCheckboxGroup = caller as iGUICheckboxGroup;
	//	float nValue = 0;
	//	if (oCheckboxGroup.optionList.Count == 1) 				//###NOTE!!!: iGUI doesn't support single checkbox so we have to use radio buttons with only one entry.  Switch between the two modes here.
	//		nValue = (oCheckboxGroup.selectedIndex != -1) ? 1 : 0;
	//	else
	//		nValue = oCheckboxGroup.selectedIndex;
	//	PropSet(nValue);						// PropSet will in turn update its GUI (us) by calling our 'SetValueGUI'
	//}

	//void OnButtonClicked(iGUIElement caller) {
	//	//iGUIButton oButton = caller as iGUIButton;				// Buttons only send the 'set' value and don't care about return (always appear unpressed)
	//	PropSet(1.0f);
	//}

	//---------------------------------------------------------------------------	LOAD / SAVE

	public void Serialize(FileStream oStream) {
		if (oStream.CanWrite) {
			oStream.Write(BitConverter.GetBytes(PropGet()), 0, 4);
		} else {
			PropSet(CUtility.DeserializeFloat(oStream));		//###IMPROVE: Assert if out of min/max?
		}
	}

	//---------------------------------------------------------------------------	UTILITY

	public void ConnectPropCallback() {	// Attempt to find 'OnPropSet_' function for this property to optionally configure automatic notification of property changes.  Provided as a public function as code occasionally has to transfer 'this' ownership
		string sNameFnOnPropSet = C_Prefix_OnPropSet + _sNameProp;				// The precise name function must have to enable automatic notification
		Type oTypeFields = _oObject._iObj.GetType();		// Object's owner has the OnPropSet_ events we need, not CObject INSTANCE!
		_oMethodInfo_OnPropSet = oTypeFields.GetMethod(sNameFnOnPropSet);
	}
}

public class CPropGroup {
	public string			_sNamePropGrp;
	public string			_sDescPropGrp;
	public bool				_bInvisible;
	public int				_nNumWidgetColumns;					    // The number of widgets packed left-to-right in this property group.  Used for small controls like checkboxes and short buttons.  Create a container if non-zero
	//public iGUIContainer	_oContainer;							// The GUI container that will store our child properties.
    public CUICanvas        _oUICanvas;                             // The canvas that owns us ####DESIGN??
    public List<int>		_aPropIDs = new List<int>();

    public CPropGroup(string sNamePropGrp, string sDescPropGrp, bool bInvisible = false, int nNumWidgetColumns = 1) {
		_sNamePropGrp		= sNamePropGrp;
		_sDescPropGrp		= sDescPropGrp;
		_bInvisible			= bInvisible;
		_nNumWidgetColumns	= nNumWidgetColumns;
	}

	//public void CreateWidget(iGUIListBox oListBoxTop) {
		//if (_bInvisible) {
		//	_oContainer = oListBoxTop.addElement<iGUIContainer>();
		//} else {
		//	iGUIPanel oPanel = oListBoxTop.addElement<iGUIPanel>();
		//	oPanel.type = iGUIPanelType.Box;
		//	_oContainer = oPanel;
		//}

		//int nLineForTitle = (_sNamePropGrp != "") ? 1 : 0;		// If this property group has a title we need to allocate one additional line for it.
		//int nLineForSingleLineControls = 0;

		//if (_nNumWidgetColumns >= 2) { 							// If we have widget columns greater to 2 or equal we create a horizontal container so our controls can be packed left-to-right under us IN A SINGLE LINE
		//	_oContainer.layout = iGUILayout.HorizontalDense;
		//	nLineForSingleLineControls = 1;						// We never grow so we immediately allocate one line for all our one-line controls.  ###NOTE: Assumes all multi-column groups only have one row!
		//} else {
		//	_oContainer.layout = iGUILayout.VerticalDense;
		//}

		//float nHeightGrp = G.C_Gui_WidgetsHeight * (nLineForTitle + nLineForSingleLineControls);
		//if (nHeightGrp < G.C_Gui_WidgetsHeight * 2f)
		//	nHeightGrp = G.C_Gui_WidgetsHeight * 2f;
		//_oContainer.positionAndSize.Set(0, 0, oListBoxTop.positionAndSize.width, (int)nHeightGrp);		// Our height will never grow past our header size and line for our only line of controls

		//if (nLineForTitle != 0) {						// Header text only exists if group has a name.
		//	iGUILabel oLabelPropGrp = _oContainer.addElement<iGUILabel>();
		//	oLabelPropGrp.label.text = "=== " + _sNamePropGrp + " ===";
		//	oLabelPropGrp.label.tooltip = _sDescPropGrp;
		//	oLabelPropGrp.style.fontSize = 13;
		//	oLabelPropGrp.positionAndSize.Set(0, 0, 1, G.C_Gui_WidgetsHeight - 5);
		//	oLabelPropGrp.style.alignment = TextAnchor.MiddleCenter;
		//}
	//}
};

//public void OnGUI(ref Rect rectGuiLabel, ref Rect rectGuiValue, ref int nGuiLine) {			//###OBS!!		// Called from top-level app OnGUI to provide a simple UI for this property.  (Possibly overriden by base class to provide interfaces other than slider)
//float nValueProp = PropGet();
//rectGuiLabel.Set(5, nGuiLine * C_Gui_PropHeight, C_Gui_ValueWidth, C_Gui_PropHeight);
//rectGuiValue.Set(C_Gui_ValueWidth, nGuiLine * C_Gui_PropHeight + 6, C_Gui_ValueWidth, C_Gui_PropHeight);

//if ((_nPropFlags & AsCheckbox) != 0) {					// Draw a checkbox if we're a boolean property
//	bool bValueNew = GUI.Toggle(rectGuiValue, nValueProp != 0, _sLabel);
//	float nValueNew = bValueNew ? 1 : 0;
//	if (nValueNew != nValueProp)
//		PropSet(nValueNew);
//} else {
//	GUI.Label(rectGuiLabel, string.Format("{0} [{1:F3}]", _sLabel, nValueProp));
//	float nValueNew = GUI.HorizontalSlider(rectGuiValue, nValueProp, _nMin, _nMax);
//	if (nValueNew != nValueProp)
//		PropSet(nValueNew);
//}
//}

				//iGUISwitch oButton = oListBoxTop.addElement<iGUISwitch>();
				//oButton.labelWidth = G.C_Gui_LabelWidth;
				//oButton.valueChangeCallback += OnCheckboxValueChange;
				//_oWidgetGUI = oButton;
//iGUISwitch oButton = oGUI as iGUISwitch;			//###DESIGN: Is there a value in these simple controls being set here???
//if (oButton) {
//	oButton.value = nValueNew != 0;
//	return;
//}


//if ((_nPropFlags & CProp.ReadOnly) != 0) {				//###CHECK: Read only = status bar???		###BROKEN: Too many spacing problems trying to make bar & label line up... 
//	int nWidthPanel = (int)oListBoxTop.rect.width;		//###LEARN: How to obtain the real width in pixels (positionAndSize can return the percentage as user set it!)
//	iGUIContainer oContainer = oListBoxTop.addElement<iGUIContainer>();				
//	oContainer.positionAndSize.Set(0, 0, nWidthPanel, G.C_Gui_WidgetsHeight);		//###WEAK: Height needs an adjustment to look like sliders...
//	//oContainer.layout = iGUILayout.HorizontalDense;								//###IMPROVE: Really need the additional sub container if we don't use layout??
//	iGUILabel oLabel = oContainer.addElement<iGUILabel>();
//	//oLabel.positionAndSize.Set(0, 0, G.C_Gui_WidgetsWidth / 2, G.C_Gui_WidgetsHeight - 7);
//	oLabel.positionAndSize.Set(0, 0, 0.6f*nWidthPanel, G.C_Gui_WidgetsHeight);
//	iGUIProgressBar oBar = oContainer.addElement<iGUIProgressBar>();
//	//oBar.positionAndSize.Set(nWidthPanel / 2, 0, nWidthPanel / 2, G.C_Gui_WidgetsHeight - 7);		//###CHECK
//	oBar.positionAndSize.Set(0.6f * nWidthPanel, 0, 0.4f * nWidthPanel, G.C_Gui_WidgetsHeight);		//###CHECK
//	oBar.min = _nMin;
//	oBar.max = _nMax;
//	oBar.userData = oLabel;			// Store back-reference to label so update can change text value also
//	_oWidgetGUI = oBar;
//} else {



//_oContainer.padding = new RectOffset(0, 0, 0, 0);
//_oContainer.style.padding = new RectOffset(0, 0, 0, 0);
//_oContainer.style.margin = new RectOffset(0, 0, 0, 0);

