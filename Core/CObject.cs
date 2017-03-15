/*###DISCUSSION: CObject global object / property / GUI integration
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

using System;
using System.IO;
using System.Collections.Generic;


public class CObject {				// Centrally-important base class (with matching implementations in Unity & Blender & C++ DLL) that forms the base to nearly every entity in our engine DLL... SoftBody, Cloth, Fluid, Scene, etc...  Store a group of abstract properties in _aPropsOnGUI that essentially controls most of the DLL code.
	public	object				_iObjOwner;				// The interface to the object that owns / manages this CObject.  Can be changed in UpdateOwningObject ###SOON: Any use as IObject is empty?  Change to generic 'object'?
	public	string				_sNameObject;			// The name of the object.  Displayed in GUI	###DESIGN?????
	public	string				_sLabelPanel;			// The string drawn in our editing panel when we're visible on the GUI (as a 'choice' in multi-object property editor)
	public	List<CPropGrp>		_aPropGrps = new List<CPropGrp>();
	public	bool				_bInitialized;          // Object is fully initialized and ready for full functioning (set in FinishInitialization()


    public CObject(object iObjOwner, string sNameObject, string sLabelPanel) {
        _iObjOwner = iObjOwner;
        _sNameObject = sNameObject;             //###IMPROVE!!!! ###SOON: Make sure this is intialized early in so members can create GUIs in their constructors!!
		_sLabelPanel = sLabelPanel;
    }


	//---------------------------------------------------------------------------	REDIRECTION TO CPropGrp
	public CProp PropAdd(int nPropGrpIndex, object oPropOrdinal, string sNameProp, string sLabel, float nDefault, float nMin, float nMax, string sDescription, int nPropFlags = 0, object oChoices = null) {
		CPropGrp oPropGrp = _aPropGrps[nPropGrpIndex];
		return oPropGrp.PropAdd(oPropOrdinal, sNameProp, sLabel, nDefault, nMin, nMax, sDescription, nPropFlags, oChoices);
	}

	public float PropGet(int nPropGrpIndex, object oPropOrdinal) {
		CPropGrp oPropGrp = _aPropGrps[nPropGrpIndex];
		return oPropGrp.PropGet(oPropOrdinal);
	}

	public float PropGet(int nPropGrpIndex, string sNameProp) {
		CPropGrp oPropGrp = _aPropGrps[nPropGrpIndex];
		return oPropGrp.PropGet(sNameProp);
	}

	public void PropSet(int nPropGrpIndex, string sNameProp, float nValue) {
		CPropGrp oPropGrp = _aPropGrps[nPropGrpIndex];
		oPropGrp.PropSet(sNameProp, nValue);
	}

	public virtual float PropSet(int nPropGrpIndex, object oPropOrdinal, float nValue) {
		CPropGrp oPropGrp = _aPropGrps[nPropGrpIndex];
		return oPropGrp.PropSet(oPropOrdinal, nValue);
	}

	public CProp PropFind(int nPropGrpIndex, object oPropOrdinal) {
		CPropGrp oPropGrp = _aPropGrps[nPropGrpIndex];
		return oPropGrp.PropFind(oPropOrdinal);
	}

	public CProp PropFind(int nPropGrpIndex, string sNameProp) {
		CPropGrp oPropGrp = _aPropGrps[nPropGrpIndex];
		return oPropGrp.PropFind(sNameProp);
	}

	//---------------------------------------------------------------------------	UTILITY

	public void AddPropGrp(CPropGrp oPropGrp) {
		_aPropGrps.Add(oPropGrp);
	}

	public void FinishInitialization() {				// Initialize all properties to their default value.  Done right after all properties have been added to push the default values onto their final destination (e.g. server)
		//###TODO<19>: Still relevant?  need to propagate
		//foreach (CProp oProp in _aProps)
		//	if (oProp != null)
		//		oProp.PropSet(oProp._nValueLocal);		// Push onto property the local value that was set during init.
		_bInitialized = true;
	}


	//---------------------------------------------------------------------------	LOAD / SAVE

	public void Serialize(FileStream oStream) {		//###BROKEN<19>: Delegate to CPropGrp too now
		//if (oStream.CanWrite) {
		//	oStream.WriteByte(GetNumProps());
		//} else {
		//	byte nPropsInStream = (byte)oStream.ReadByte();
		//	byte nPropsInProp	= GetNumProps();
		//	if (nPropsInStream != nPropsInProp)
		//		CUtility.ThrowException(string.Format("ERROR: CProp.Serialize_Actors_OBS() attempted to load {0} properties on object of type '{1}' which has {2} properties", nPropsInStream, _sNameObject, nPropsInProp));
		//}
		//foreach (CProp oProp in _aProps)
		//	oProp.Serialize(oStream);

		//if (_nBodyID != 0 && _sNameScriptHandle != null)	// We only write property set to script recorder on objects that have provided their script-access handle
		//	CGame.INSTANCE._oScriptRecordUserActions.WriteObject(this);
	}

	//---------------------------------------------------------------------------	EVENTS

	public void Notify_PropertyValueChanged(CProp oProp, float nValueOld) {			//###LEARN: How to implement events
		EventHandler<EventArgs_PropertyValueChanged> oHandler = Event_PropertyValueChanged;
		if (oHandler != null) {
			EventArgs_PropertyValueChanged oEventArgs = new EventArgs_PropertyValueChanged();
			oEventArgs.Property = oProp;
			oEventArgs.PropertyGroup = oProp._oPropGrp;
			oEventArgs.PropertyID = oProp._nPropOrdinal;
			oEventArgs.PropertyName = oProp._sNameProp;
			oEventArgs.ValueNew = oProp._nValueLocal;
			oEventArgs.ValueOld = nValueOld;
			oHandler(this, oEventArgs);
		}
	}

	public event EventHandler<EventArgs_PropertyValueChanged> Event_PropertyValueChanged;
}

public class EventArgs_PropertyValueChanged : EventArgs {
	public CProp		Property		{ get; set; }
	public CPropGrp		PropertyGroup	{ get; set; }
	public int			PropertyID		{ get; set; }
	public string		PropertyName	{ get; set; }
	public float		ValueNew		{ get; set; }
	public float		ValueOld		{ get; set; }
}
