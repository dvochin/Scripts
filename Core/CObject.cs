using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;


public class CObject {				// Centrally-important base class (with matching implementations in Unity & DLL) that forms the base to nearly every entity in our engine DLL... SoftBody, Cloth, Fluid, Scene, etc...  Store a group of abstract properties in _aPropsOnGUI that essentially controls most of the DLL code.
	public	IObject				_iObj;					// The interface to the object that owns / manages this CObject.  Can be changed in UpdateOwningObject
	public	int					_nBodyID;				// The (optional) body ID that owns us.  (0 if no body, 1-based body ID if object is body-based)  Used by script recorder.
	public	string				_sNameObject;			// The name of the object.  Displayed in GUI	###DESIGN?????
	public	string				_sNameScriptHandle;		// The script name for this object.  Must match flattend object name in CBodyProxy. (If null object doesn't write to script file during script recording)
	public	string				_sNameBlenderObject;	// The name of the Blender object (only for properties containing the CProp.Blender flag).  Used to communicate with Blender
	public	string				_sNameFull;
	public	string				_sNameEngineType;
	public	IntPtr				_hObject;				// The important server-side entity pointer.  Used on non-local objects & properties only
	public	CProp[]				_aProps;
	public	List<CPropGroup>	_aPropGroups = new List<CPropGroup>();
    CPropGroup			        _oPropGroup_LastAdded;	// The lastly-added 'property group' that provides a simple level of indirection between GUI grouping and our flat/fast properties.
	public	Type				_oTypeFieldsEnum;
	public	bool				_bInitialized;			// Object is fully initialized and ready for full functioning (set in FinishInitialization()


	public CObject(IObject iObj, int nBodyID, Type oTypeFieldsEnum, string sNameObject, string sNameScriptHandle=null, string sNameBlenderObject=null) {
		_iObj				= iObj;
		_nBodyID			= nBodyID;
		_oTypeFieldsEnum	= oTypeFieldsEnum;
		_sNameObject		= sNameObject;				//###IMPROVE!!!! ###SOON: Make sure this is intialized early in so members can create GUIs in their constructors!!
		_sNameScriptHandle	= sNameScriptHandle;
		_sNameBlenderObject		= sNameBlenderObject;
		_sNameEngineType	= this.GetType().Name;
		_sNameFull			= _sNameEngineType + "." + _sNameObject;
		_aProps = new CProp[GetNumProps()];				// Initialize our array at the right size from the number of fields defined in the associated enum of this object
	}
	public void OnDestroy() {
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


			//####SOON ###BROKEN: JS
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
		FieldInfo[] aFieldsEnum = _oTypeFieldsEnum.GetFields();
		return (byte)(aFieldsEnum.Length - 1);							// Reflection info on enums returns an extra hidden field called '_value' before our real enum... we just ignore it.
	}

	public CProp PropFind(object oPropEnumOrdinal) {
		int nPropEnumOrdinal = (int)oPropEnumOrdinal;
		if (nPropEnumOrdinal < 0 || nPropEnumOrdinal >= _aProps.Length)
			throw new CException(string.Format("ERROR: CProp.PropFind() obtained invalid ordinal {0} while searching properties on object of type {1}", nPropEnumOrdinal, _sNameFull));
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
	public virtual float PropSet(string sPropName, float nValue) {
		int nPropEnumOrdinal = (int)Enum.Parse(_oTypeFieldsEnum, sPropName);		//###NOTROBUST!!!
		CProp oProp = PropFind(nPropEnumOrdinal);
		return oProp.PropSet(nValue);
	}

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

	public void UpdateOwningObject(IObject iObj) {	// Remap the owning object to a new reference and update the owning properties to reconnect to the updated callbacks
		_iObj = iObj;
		foreach (CProp oProp in _aProps)			// Attempt to find 'OnPropSet_' function on all our properties
			if (oProp != null)
				oProp.ConnectPropCallback();
	}

	//---------------------------------------------------------------------------	LOAD / SAVE

	public void Serialize(FileStream oStream) {		//###OBS???  Only script load / save now?
		if (oStream.CanWrite) {
			oStream.WriteByte(GetNumProps());
		} else {
			byte nPropsInStream = (byte)oStream.ReadByte();
			byte nPropsInProp	= GetNumProps();
			if (nPropsInStream != nPropsInProp)
				throw new CException(string.Format("ERROR: CProp.Serialize_Actors_OBS() attempted to load {0} properties on object of type '{1}' which has {2} properties", nPropsInStream, _sNameFull, nPropsInProp));
		}
		foreach (CProp oProp in _aProps)
			oProp.Serialize(oStream);

		if (_nBodyID != 0 && _sNameScriptHandle != null)	// We only write property set to script recorder on objects that have provided their script-access handle
			CGame.INSTANCE._oScriptRecordUserActions.WriteObject(this);
	}
}

public interface IObject {
	void OnPropSet_NeedReset(CProp oProp, float nValueOld, float nValueNew);			// Called when a property created with the 'NeedReset' flag gets changed so owning object can adjust its global state	//###DESIGN: Can get rid of this annoying requirement of reset for each CObject???
	//Type GetFieldsEnum();
};
