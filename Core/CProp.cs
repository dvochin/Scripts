/*###DISCUSSION: CProp: global object / property / GUI integration
=== LAST ===

=== NEXT ===

=== TODO ===

=== LATER ===

=== IMPROVE ===

=== DESIGN ===

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===

=== QUESTIONS ===

=== WISHLIST ===

*/

using UnityEngine;
using System;
using System.IO;
using System.Reflection;


public class CProp {							// Important class that abstracts the concept of a 'property' and enables Unity and C++ dll to read/write this property with the other side being notified of the change.
	public CPropGrp		_oPropGrp;				// The property group that owns us.  It is in turned owned by its own CObject

	public int			_nPropOrdinal;
	public string		_sNameProp;				// Name of property as typed in the defining enum
	public string		_sLabel;				// Label of the property as shown in GUI
	public string		_sDescription;
	public float		_nValueLocal;			// The actual property value that is get / set *only* when _nPropFlags::Local is set.  (In non-local mode the C++ dll owns the actual memory of the float that represents our value).  Made private so nobody tries to access thinking they're getting the dll value.
	public float		_nDefault;
	public float		_nMin;
	public float		_nMax;
	public float		_nMinMaxRange;			// = _nMax - _nMin
	//public float		_nHotSpotEdit_BeginPosY;	// Value of hotspot Y coordinate to change property from hotspot movement in ChangePropByHotSpotMove()
	//public float		_nHotSpotEdit_BeginValue;	// Our value when hotspot editing began.
	public int			_nPropFlags;
	public Type			_oEnumChoices;			// The type of the enum that displays the combo-box choices for this property.  Parsed by GUI to display / choose  the proper enum
	public string[]		_aStringChoices;		// The possible choices for this property.  _nValue is set to the index in this string list.
	public MethodInfo	_oMethodInfo_OnPropSet; // The field info of a function on owning object that receives notification when this property changes.
	public object       _oObjectExtraFunctionality;		// Properties can have reference to extra class instances that extend their functionality.  This is currently used for morph channel caching

	public float		_nRndVal;				// The actual random value added at the very end of property get/set (kept separated to not mix with user-set value)
	public float		_nRndValSource;			// Randomization source value (at beginning of smoothing iterations)
	public float		_nRndValTarget;			// Randomization target value (at end       of smoothing iterations)
	public float		_nRndValSmoothVelocity; // Smoothing velocity (utilty var needed for smoothing)

	public float        _nAdjustTargetVal;      // Runtime adjust functionality.  What the property value becomes RuntimeAdjust_SetTargetValue() gets passed a nDistThisFrame over _nAdjustDistPerFrame
	public float        _nAdjustDistPerFrame;	// How much movement distance required by frame to set the property to _nAdjustTargetVal

    public CUIWidget    _oUIWidget;             // The widget who draws our property in the Unity UI


	public const int ReadOnly			= 1 << 0;   // Read only property that user can't change through GUI.  ###CHECK: Formerly: Fake property without value or processing.  Only exists to draw a seperating header in GUI
	public const int Hide				= 1 << 1;   // Property doesn't draw anything in GUI
//	public const int CPlusPlusDll		= 1 << 3;	// Property exists in C++ dll.
	public const int AsCheckbox			= 1 << 4;	// Property takes only the float values of 0.0f or 1.0f and is drawn on GUI as a checkbox
	public const int AsButton			= 1 << 5;	// Property is drawn as a button and does not show a value (e.g. always zero)
	//###IMPROVE: Deactivate bounds if needed with a flag?)

	public const string C_Prefix_OnPropSet	= "OnPropSet_";			// Prefix name of functions of owning object that (if defined) automatically receive notification when this property changes)

	//---------------------------------------------------------------------------	

	public CProp() { }

	public CProp(CPropGrp oPropGrp, int nPropOrdinal, string sNameProp, string sLabel, float nDefault, float nMin, float nMax, string sDescription, int nPropFlags, object oChoices = null) {
		_oPropGrp			= oPropGrp;
		_nPropOrdinal		= nPropOrdinal;
		_sNameProp			= sNameProp;
		_sLabel				= sLabel;
		_nDefault			= nDefault;
		_nValueLocal		= nDefault;			// Reasonable way to init local value.  (Important for init flow)
		_nMin				= nMin;
		_nMax				= nMax;
		_nMinMaxRange		= _nMax - _nMin;
		_sDescription		= sDescription;
		_nPropFlags			= nPropFlags;
		_oEnumChoices		= oChoices as Type;			// Choices will either be from reflection as a Type...
		_aStringChoices		= oChoices as string[];     // Or a string array.

		ConnectPropCallback();
		//Debug.Log(string.Format("{0} prop #{1} = '{2}'", _oObject._sNameFull, _nPropOrdinal, _sNameProp));
	}
	public void OnDestroy() {
	}

	//---------------------------------------------------------------------------	GET / SET
	public float PropGet() {
		if (_oPropGrp.GetType() == typeof(CPropGrpBlender)) {	// For Blender-side objects we must fetch from related CProp_PropGet() in Blender python		###DESIGN<19>: Catch Blender get/set in CPropGrpBlender instead?
			CPropGrpBlender oObjectBlender = _oPropGrp as CPropGrpBlender;       // Blender property means that we must be owned by a CObjectBlender
			_nValueLocal = float.Parse(CGame.gBL_SendCmd("CBody", oObjectBlender._sBlenderAccessString + ".PropGetString('" + _sNameProp + "')"));
			return _nValueLocal;
		} else {
			return _nValueLocal - _nRndVal;         // Remove random value off the very top.  Pushed in  / pulled in at last minute ###CHECK: Assumes we use PropGet() everywhere!  (Not true!!) ###BUG??
		}
	}

	public float PropSet(float nValueNew) {
		nValueNew += _nRndVal;					// Apply separated random value at the very top		###OBS? Random value still used??

		//=== Avoid doing again if we're setting to the same value ===
		if (_nValueLocal == nValueNew)			//###CHECK<11>: Really safe?  Could some use cases be affected?
			return _nValueLocal;

		//=== Cap the value to pre-set bounds ===
		if (nValueNew < _nMin)
			nValueNew = _nMin;
		if (nValueNew > _nMax)
			nValueNew = _nMax;

		float nValueOld = _nValueLocal;

		//=== Set the value in our remote counterparts if flagged as such ===
		if (_oPropGrp.GetType() == typeof(CPropGrpBlender)) {    // For blender properties we invoke its corresponding CProp_PropSet() method
			CPropGrpBlender oObjectBlender = _oPropGrp as CPropGrpBlender;  //###CHECK<19> Move??     // Blender property means that we must be owned by a CObjectBlender
			_nValueLocal = float.Parse(CGame.gBL_SendCmd("CBody", oObjectBlender._sBlenderAccessString + ".PropSetString('" + _sNameProp + "'," + nValueNew.ToString() + ")"));
		} else {      // In local mode we just set local value directly
			_nValueLocal = nValueNew;
		}

		//=== OnPropSet_ notifications can only be sent when object is fully operational (e.g. not during init) ===
		if (_oPropGrp._oObj._bInitialized) {
			//=== Notify owning object if it requested notification upon change ===
			if (_oMethodInfo_OnPropSet != null) {								// If property has a preconfigured 'OnPropSet_' function call it now to notify parent of change
				object[] aArgs = new object[] { nValueOld, nValueNew };
				_oMethodInfo_OnPropSet.Invoke(_oPropGrp._oObj._iObjOwner, aArgs);			// CObject's owner has the event properties we need to call, not CObject INSTANCE!!
			}
		}

		//=== Set our GUI appearance if we're currently displayed ===
        if (_oUIWidget != null)
            _oUIWidget.SetValue(_nValueLocal);          //###CHECK: Was PropGet()!

		//=== Write the just-set property to the script recorder.  This provides a very good starting point to animate a scene ===
		if (CGame.INSTANCE._oScriptRecordUserActions != null) {
			if (_oPropGrp._oObj._sNameObject!= null) {	// We only write property set to script recorder on objects that have provided their script-access handle
				if (nValueNew != nValueOld) 			//###IMPROVE!?!?!? Should abort if same earlier?? Safe??
					CGame.INSTANCE._oScriptRecordUserActions.WriteProperty(this);
			}
		}
		//=== Notify object that we really did change.  This will in turn notify any owning object that have registered for this event ===
		_oPropGrp._oObj.Notify_PropertyValueChanged(this, nValueOld);		//###TODO<13>: Convert previous codebase that needed this functionality with this new event-based mechanism!

		return _nValueLocal;
	}

    //---------------------------------------------------------------------------	RUNTIME ADJUST
	//###BROKEN: CProp randomization.  Still needed??
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
		//###NOTE: Set property here! ErosEngine.Object_PropSet(_oObject._hObject, _nPropOrdinal, nValueNew);		//####NOTE: Directly set value without setting _nValueLocal.  ####BUG: Decouples UI from PropGet()!
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

	public int Widget_Create(CUIPanel oPanel) {
		if ((_nPropFlags & CProp.Hide) != 0) 							// We don't show in GUI properties marked as hidden... these can only be changed through code.
			return 0;

		if (_oEnumChoices != null || _aStringChoices != null) {			// Properties that have either multiple choices are GUI-rendered as a combobox

            _oUIWidget = CUIDropdownEnum.Create(oPanel, this);

        }
        else if ((_nPropFlags & CProp.AsCheckbox) != 0) {

            _oUIWidget = CUIToggle.Create(oPanel, this);

        }
        else if ((_nPropFlags & CProp.AsButton) != 0) {

            _oUIWidget = CUIButton.Create(oPanel, this);

        } else {

            _oUIWidget = CUISlider.Create(oPanel, this);
        }
		_oUIWidget.SetValue(PropGet());

		return 1;
	}

	public void Widget_Destroy() {
		if (_oUIWidget != null) {
			GameObject.Destroy(_oUIWidget.gameObject);
			_oUIWidget = null;
		}
	}


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
		if (_oPropGrp._oObj._iObjOwner != null) { 
			Type oTypeFields = _oPropGrp._oObj._iObjOwner.GetType();		// Object's owner has the OnPropSet_ events we need, not CObject INSTANCE!
			_oMethodInfo_OnPropSet = oTypeFields.GetMethod(sNameFnOnPropSet);
		}
	}
}
