using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;


public class CObject {				// Centrally-important base class (with matching implementations in Unity & Blender & C++ DLL) that forms the base to nearly every entity in our engine DLL... SoftBody, Cloth, Fluid, Scene, etc...  Store a group of abstract properties in _aPropsOnGUI that essentially controls most of the DLL code.
	public	IObject				_iObj;					// The interface to the object that owns / manages this CObject.  Can be changed in UpdateOwningObject ###SOON: Any use as IObject is empty?  Change to generic 'object'?
	public	int					_nBodyID;				// The (optional) body ID that owns us.  (0 if no body, 1-based body ID if object is body-based)  Used by script recorder.
	public	string				_sNameObject;			// The name of the object.  Displayed in GUI	###DESIGN?????
	public	string				_sNameScriptHandle;		// The script name for this object.  Must match flattend object name in ???CBodyProxy???. (If null object doesn't write to script file during script recording)
	public	string				_sNameFull;
	public	string				_sNameEngineType;
	public	IntPtr				_hObject;				// The important server-side entity pointer.  Used on non-local objects & properties only
	public	CProp[]				_aProps;
	public	List<CPropGroup>	_aPropGroups = new List<CPropGroup>();
    CPropGroup			        _oPropGroup_LastAdded;	// The lastly-added 'property group' that provides a simple level of indirection between GUI grouping and our flat/fast properties.
	public	Type				_oTypeFieldsEnum;
	public	bool				_bInitialized;          // Object is fully initialized and ready for full functioning (set in FinishInitialization()


    public CObject(IObject iObj, int nBodyID, string sNameObject, string sNameScriptHandle = null) {
        _iObj = iObj;
        _nBodyID = nBodyID;
        _sNameObject = sNameObject;             //###IMPROVE!!!! ###SOON: Make sure this is intialized early in so members can create GUIs in their constructors!!
        _sNameScriptHandle = sNameScriptHandle;
        _sNameEngineType = this.GetType().Name;
        _sNameFull = _sNameEngineType + "." + _sNameObject;
    }

    public CObject(IObject iObj, int nBodyID, Type oTypeFieldsEnum, string sNameObject, string sNameScriptHandle=null) : this(iObj, nBodyID, sNameObject, sNameScriptHandle) {
		_oTypeFieldsEnum	= oTypeFieldsEnum;
		_aProps = new CProp[_oTypeFieldsEnum.GetFields().Length-1];             // Initialize our array at the right size from the number of fields defined in the associated enum of this object  (Reflection info on enums returns an extra hidden field called '_value' before our real enum... we just ignore it.)
    }

    public void OnDestroy() {       //###CHECK: Useful?
		RemoveAllProperties();
	}

	public void RemoveAllProperties() {
		if (_aProps != null) {					//###CHECK: Why can this be null during destruction flow???
			foreach (CProp oProp in _aProps)	//###TODO!!!: Destroy GUI if created!!	
				if (oProp != null)
					oProp.OnDestroy();
			_aProps = null;
		}			//###CHECK: Need to iterate through prop group??
	}
	//---------------------------------------------------------------------------	UPDATE

	public virtual void OnSimulatePre() {				//###DESIGN!!!! Revisit autorefresh concept??
		if (_hObject != IntPtr.Zero) {
			//	CGame.Object_SetPose(_hObject, _oTransform.position, _oTransform.rotation);	//###OBS??  No per-frame object move for efficiency?


			//###BROKEN: Auto-refresh of GUI from changing values... (Add flags for efficiency?)
			//if ((CGame.INSTANCE._nFrameCount_MainUpdate % CGame.C_PropAutoUpdatePeriod) == 0) {				//###DESIGN: We're 'polling' properties to keep design simple ###IMPROVE: Implemement full property-change notification from server to client??
			//	foreach (CProp oProp in _aProps) {
			//		if (oProp != null) {
			//			//if (oProp._oWidgetGUI != null && (oProp._nPropFlags & CProp.ReadOnly) != 0) {			// Only read-only properties (e.g. progress bars) need polling to update their values
			//			if (oProp._oWidgetGUI != null) {			// Only read-only properties (e.g. progress bars) need polling to update their values		//###HACK!!!!
			//				CProp.SetValueGUI(oProp._oWidgetGUI, oProp._sLabel, oProp.PropGet());
			//			}
			//		}
			//	}
			//}
		}
	}

	//---------------------------------------------------------------------------	PROP ADD

	public CProp PropAdd(object oPropEnumOrdinal, string sLabel, float nDefault, float nMin, float nMax, string sDescription, int nPropFlags = 0, Type oTypeChoice = null) {	//###DESIGN!!!!: _aPropsOnGUI retains holes if not added?  What to do???
		int nPropEnumOrdinal = (int)oPropEnumOrdinal;
		CProp oProp = new CProp(this, nPropEnumOrdinal, sLabel, nDefault, nMin, nMax, sDescription, nPropFlags, oTypeChoice);
		_aProps[nPropEnumOrdinal] = oProp;
		if (_oPropGroup_LastAdded != null)
			_oPropGroup_LastAdded._aPropIDs.Add(nPropEnumOrdinal);		//###TODO: Remove, etc
		return oProp;
	}

	public CProp PropAdd(object oPropEnumOrdinal, string sLabel, float nDefault, string sDescription, int nPropFlags = 0) {		// Convenience PropAdd override to facilitate the creation of boolean properties
		int nPropEnumOrdinal = (int)oPropEnumOrdinal;
		return PropAdd(nPropEnumOrdinal, sLabel, nDefault, 0, 1, sDescription, nPropFlags);
	}

	public CProp PropAdd(object oPropEnumOrdinal, string sLabel, Type oTypeChoice, int nDefault, string sDescription, int nPropFlags = 0) {		// Convenience PropAdd override to facilitate the creation of boolean properties
		int nPropEnumOrdinal = (int)oPropEnumOrdinal;		//###WEAK: Fill in max with # of choices in enum!!
		return PropAdd(nPropEnumOrdinal, sLabel, nDefault, 0, 255, sDescription, nPropFlags, oTypeChoice);
	}


	//---------------------------------------------------------------------------	UTILITY

	public byte GetNumProps() {
        return (byte)_aProps.Length;
		//FieldInfo[] aFieldsEnum = _oTypeFieldsEnum.GetFields();
		//return (byte)(aFieldsEnum.Length - 1);      // Reflection info on enums returns an extra hidden field called '_value' before our real enum... we just ignore it.
	}

	public CProp PropFind(object oPropEnumOrdinal) {
		int nPropEnumOrdinal = (int)oPropEnumOrdinal;
		if (nPropEnumOrdinal < 0 || nPropEnumOrdinal >= _aProps.Length)
			CUtility.ThrowException(string.Format("ERROR: CProp.PropFind() obtained invalid ordinal {0} while searching properties on object of type {1}", nPropEnumOrdinal, _sNameFull));
		CProp oProp = _aProps[nPropEnumOrdinal];
		return oProp;
	}
	public virtual float PropGet(object oPropEnumOrdinal) {
		int nPropEnumOrdinal = (int)oPropEnumOrdinal;
		CProp oProp = PropFind(nPropEnumOrdinal);
		return oProp.PropGet();
	}
	public virtual float PropSet(object oPropEnumOrdinal, float nValue) {
		int nPropEnumOrdinal = (int)oPropEnumOrdinal;
		CProp oProp = PropFind(nPropEnumOrdinal);
		return oProp.PropSet(nValue);
	}
	//public virtual float PropSet(string sPropName, float nValue) {            //###CHECK: Needed by scripting?
	//	int nPropEnumOrdinal = (int)Enum.Parse(_oTypeFieldsEnum, sPropName);		//###NOTROBUST!!!
	//	CProp oProp = PropFind(nPropEnumOrdinal);
	//	return oProp.PropSet(nValue);
	//}

	public CPropGroup PropGroupBegin(string sNamePropGrp, string sDescPropGrp, bool bInvisible = false, int nNumWidgetColumns = 1) {		// Indicate the beginning of a 'property group' = a Client-side-only entity that groups our flat property array into user-friendly groups (used for GUI separation)
		_oPropGroup_LastAdded = new CPropGroup(sNamePropGrp, sDescPropGrp, bInvisible, nNumWidgetColumns);
		_aPropGroups.Add(_oPropGroup_LastAdded);
		return _oPropGroup_LastAdded;
	}

	public void FinishInitialization() {				// Initialize all properties to their default value.  Done right after all properties have been added to push the default values onto their final destination (e.g. server)
		foreach (CProp oProp in _aProps)
			if (oProp != null)
				oProp.PropSet(oProp._nValueLocal);		// Push onto property the local value that was set during init.
		_bInitialized = true;
	}

	//public void UpdateOwningObject(IObject iObj) {	// Remap the owning object to a new reference and update the owning properties to reconnect to the updated callbacks
	//	_iObj = iObj;
	//	foreach (CProp oProp in _aProps)			// Attempt to find 'OnPropSet_' function on all our properties
	//		if (oProp != null)
	//			oProp.ConnectPropCallback();
	//}

	//---------------------------------------------------------------------------	LOAD / SAVE

	public void Serialize(FileStream oStream) {		//###OBS???  Only script load / save now?
		if (oStream.CanWrite) {
			oStream.WriteByte(GetNumProps());
		} else {
			byte nPropsInStream = (byte)oStream.ReadByte();
			byte nPropsInProp	= GetNumProps();
			if (nPropsInStream != nPropsInProp)
				CUtility.ThrowException(string.Format("ERROR: CProp.Serialize_Actors_OBS() attempted to load {0} properties on object of type '{1}' which has {2} properties", nPropsInStream, _sNameFull, nPropsInProp));
		}
		foreach (CProp oProp in _aProps)
			oProp.Serialize(oStream);

		if (_nBodyID != 0 && _sNameScriptHandle != null)	// We only write property set to script recorder on objects that have provided their script-access handle
			CGame.INSTANCE._oScriptRecordUserActions.WriteObject(this);
	}


	//---------------------------------------------------------------------------	EVENTS

	public void Notify_PropertyValueChanged(CProp oProp, float nValueOld) {			//###LEARN: How to implement events
		EventArgs_PropertyValueChanged oEventArgs = new EventArgs_PropertyValueChanged();
		oEventArgs.Property = oProp;
		oEventArgs.PropertyID = oProp._nPropEnumOrdinal;
		oEventArgs.PropertyName = oProp._sNameProp;
		oEventArgs.ValueNew = oProp._nValueLocal;
		oEventArgs.ValueOld = nValueOld;
		EventHandler<EventArgs_PropertyValueChanged> oHandler = Event_PropertyValueChanged;
		if (oHandler != null)
			oHandler(this, oEventArgs);
	}

	public event EventHandler<EventArgs_PropertyValueChanged> Event_PropertyValueChanged;
}

public class EventArgs_PropertyValueChanged : EventArgs {
	public CProp Property { get; set; }
	public int PropertyID { get; set; }
	public string PropertyName { get; set; }
	public float ValueNew { get; set; }
	public float ValueOld { get; set; }
}

public interface IObject {          //###DESIGN#10: Useful?  No members!
	//Type GetFieldsEnum();
};






/*###TODO#10: CObjectBlender
- Invert local flag
- IObject still of use?
- Class member accessor is temporary
- 
- Start working on new CGameModeConfigure (for now just crap out CGame)
- Get skinned mesh over... fixing bones... and updatable!
- Create Blender-side CGameModeConfigure?  Or CGame??
- It owns the Blender-side CObject shape keys.
- Create hotspot and CObject in Unity connecting to shape keys
- Demonstrate morphing on skinned body!
- Then... start implementing full body CFlexCollider to repell bodysuit!
*/

public class CObjectBlender : CObject {     // CObjectBlender: Specialized version of CObject that mirrors an equivalent CObject structure in Blender.  Used for remote Blender property access
    public string _sBlenderAccessString;    // The fully-qualified 'Blender Access String' where we can obtain our Blender-based CObject equivalent designed to communicate with this Unity-side object.

    public CObjectBlender(IObject iObj, string sBlenderAccessString, int nBodyID) : base(iObj, nBodyID, sBlenderAccessString) {     //###NOW object name!
        _sBlenderAccessString = sBlenderAccessString;

        string sSerializedCSV = CGame.gBL_SendCmd("CBody", _sBlenderAccessString + ".Serialize()");            //###MOVE#11 to another blender codefile?

		string[] aFields = CUtility.SplitCommaSeparatedPythonListOutput(sSerializedCSV);

        _sNameObject = aFields[0];
        int nProps = int.Parse(aFields[1]);
        _aProps = new CProp[nProps];
		CPropGroup oPropGrp = PropGroupBegin("", "", true);			//###CHECK#11: OK?  Group name?  Change default group functionality to auto insert of zero?

		for (int nProp = 0; nProp < nProps; nProp++) {
            sSerializedCSV = CGame.gBL_SendCmd("CBody", _sBlenderAccessString + ".SerializeProp(" + nProp.ToString() + ")");
            aFields = CUtility.SplitCommaSeparatedPythonListOutput(sSerializedCSV);
            string sName            = aFields[0];
            string sDescription     = aFields[1];
            float nValue            = float.Parse(aFields[2]);
            float nMin              = float.Parse(aFields[3]);
            float nMax              = float.Parse(aFields[4]);
			//int eFlags              = int  .Parse(aFields[5]);
			_aProps[nProp] = new CProp(this, nProp, sName, nValue, nMin, nMax, sDescription, 0, null);//, CProp.Blender, null);	//###NOTE: No longer a Blender property as we don't update every slider value change for better performance (we batch update during mode change now)
			oPropGrp._aPropIDs.Add(nProp);
			_aProps[nProp]._sNameProp = sName;              //###HACK!!!
            _aProps[nProp]._nValueLocal = nValue;              //###HACK!!!
        }
        //float nValue2 = _aProps[1].PropGet();
        //nValue2 = _aProps[1].PropSet(4);
        //nValue2 = _aProps[0].PropSet(0);
        //nValue2 = _aProps[0].PropSet(4);
    }
}

