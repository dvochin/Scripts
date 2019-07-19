/*###DOCS26: CObj global object / property / GUI integration

=== DEV ===
- When flattening to jscript, instead of issuing error-prone static idea, keep our reference to how we flattened by keeping our flattened array
	- This way when jscript sends events to an object deep in the tree there is constant-time lookup!!
- Make CObjs have a transform?  (Some value having Unity implement our simple tree so we can see in Unity editor?)

=== NEXT ===

=== TODO ===

=== LATER ===

=== IMPROVE ===

=== DESIGN ===
- Have proxy class in jscript for each CObj class?

=== IDEAS ===
- Have 'object types'?  (Like 'object group' (for morph grouping), 'value' (e.g. morphs), bones (with full pos/rot xyz)
	- Type determine by CObj subclass??

=== LEARNED ===

=== PROBLEMS ===

=== QUESTIONS ===

=== WISHLIST ===

*/

using UnityEngine;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

public class CObj3 : CObj {                 // Subclass of CObj specialized to store a triplet of values such as needed by 3D position and euler rotation
	public Vector3      _vecValue;

	public CObj3(string sName, object iObjOwner = null, CObj oObjParent = null) : base(sName, iObjOwner, oObjParent) {
		_nFlags |= CObj.TripleValues;
	}

	public new Vector3 Get() {
		if (_aChildren != null)         // Parent objects have no value
			return Vector3.zero;
		return _vecValue;
	}

	public Vector3 Set(Vector3 vecValueNew) {
		if (_aChildren != null)         // Parent objects have no value
			return Vector3.zero;

		//=== Avoid doing again if we're setting to the same value ===
		if (_vecValue == vecValueNew)					//###DESIGN:!!! Need a way to disable this optimization in some cases... e.g. pose load.
			return _vecValue;

		////=== Cap the value to pre-set bounds ===
		//if (nValueNew < _nMin)
		//    nValueNew = _nMin;
		//if (nValueNew > _nMax)
		//    nValueNew = _nMax;

		//=== Set the value in our remote counterparts if flagged as such ===
		Vector3 vecValueOld = _vecValue;
		_vecValue = vecValueNew;

		//=== OnSet_ notifications can only be sent when object is fully operational (e.g. not during init) ===
		if (_bInitialized_HACK) {
			//=== Notify owning object if it requested notification upon change ===
			if (_oMethodInfo_OnSet != null) {                               // If property has a preconfigured 'OnSet_' function call it now to notify parent of change
				object[] aArgs = new object[] { vecValueOld, vecValueNew };
				_oMethodInfo_OnSet.Invoke(_iObjOwner, aArgs);           // CObj's owner has the event properties we need to call, not CObj INSTANCE!!
			}
		}

		//=== Notify object that we really did change.  This will in turn notify any owning object that have registered for this event ===
		//#DEV26:??? Notify_PropertyValueChanged(_nValue);     //###TODO13: Convert previous codebase that needed this functionality with this new event-based mechanism!

        //=== Set our timestamp-at-last-change and increment the global counter ===
        _nTimeStamp = CObj._nTimeStamp_Global;
        CObj._nTimeStamp_Global++;

		return _vecValue;
	}
};

public class CObj {				// Centrally-important base class (with matching implementations in Unity & Blender & C++ DLL) that forms the base to nearly every entity in our engine DLL... SoftBody, Cloth, Fluid, Scene, etc...  Store a group of abstract properties in _aChildrenOnGUI that essentially controls most of the DLL code.
	public	object		_iObjOwner;				    // The interface to the object that owns / manages this CObj.  Can be changed in UpdateOwningObject ###SOON: Any use as IObject is empty?  Change to generic 'object'?
	public  CObj        _oObjParent;			    // Parent of this CObj in our tree of parent/child relationships.  (If null this CObj is the top-level game root)
	public  List<CObj>  _aChildren;                 // (Optional) children of this CObj in our tree of parent/child relationships.  (If null this object doesn't have children)
	public	event		EventHandler<EventArgs_PropertyValueChanged> Event_PropertyValueChanged;		// The event our owner can listen to to apply property changes
	public  bool		_bInitialized_HACK;         // Object is fully initialized and ready for full functioning

	public string       _sName;                     // Human-friendly object name.  Also doubles as label.  The 'codebase name' is the non-human-friendly name used by the codebase
	public string       _sNameInCodebase;	        // The technical name of this object used in the codebase.  Never shown to user.
	public string       _sPath;                     // The fully-qualified 'Object path' to this property.  (e.g. <TopParent>/<OurGrandParent>/<OurParent>/<Us>)
	public string       _sDescription;
	public float        _nValue;			        // The actual property value that is get / set *only* when _nFlags::Local is set.  (In non-local mode the C++ dll owns the actual memory of the float that represents our value).  Made private so nobody tries to access thinking they're getting the dll value.
	public int          _nFlattenedTreeOrdinal;		// 'Flattened' ID the web-browser tree control needs.  Makes possible tree rendering / interaction with web browser tree control
	public float        _nDefault;
	public float        _nMin;
	public float        _nMax;
	public float        _nMinMaxRange;              // = _nMax - _nMin
	public int          _nFlags;
	public Type         _oEnumChoices_OBS;          // The type of the enum that displays the combo-box choices for this property.  Parsed by GUI to display / choose  the proper enum
	public string[]     _aStringChoices_OBS;        // The possible choices for this property.  _nValue is set to the index in this string list.
	public MethodInfo   _oMethodInfo_OnGet;         // The field info of a function on owning object that receives notification when this property requires an update
	public MethodInfo   _oMethodInfo_OnSet;         // The field info of a function on owning object that receives notification when this property changes.
	public object       _oObjectExtraFunctionality; // Properties can have reference to extra class instances that extend their functionality.  This is currently used for morph channel caching

    public Int64        _nTimeStamp = 0;            // Value of global '_nTimeStamp_Global' the last time we were set.  Used for efficient view refreshes.
    public static Int64 _nTimeStamp_Global = 0;     // Global counter that increments every time an object is set.  This is used as a type of 'timestamp' so view update mechanisms only receive the properties that have changed since the last view refresh.

	public const int ReadOnly           = 1 << 0;   // Read only property that user can't change through GUI.  ###CHECK: Formerly: Fake property without value or processing.  Only exists to draw a seperating header in GUI
	public const int Hide               = 1 << 1;   // Property doesn't draw anything in GUI
	public const int TripleValues       = 1 << 2;   // Property has triple values (is a CObj3)
	public const int AsCheckbox         = 1 << 3;   // Property takes only the float values of 0.0f or 1.0f and is drawn on GUI as a checkbox
	public const int AsButton           = 1 << 4;   // Property is drawn as a button and does not show a value (e.g. always zero)
	public const int AsSpinner          = 1 << 5;   // Property is drawn as a 'CSpinner' combination control.  (Left and right buttons with value shown in middle)
	public const int AsGroup            = 1 << 6;   // Property is drawn as invisible group control
	public const int AsRoot             = 1 << 7;   // Property is the ROOT GUI on web-side
	public const int FromBlender_HACK   = 1 << 8;   // Property comes value comes from Blender and we must notified Blender when setting ###WEAK: Everything about this flag is a hack!
													//###IMPROVE: Deactivate bounds if needed with a flag?)
	public const string C_Prefix_OnGet  = "OnGet_";         // Prefix name of functions of owning object that (if defined) automatically receive notification when this property changes)
	public const string C_Prefix_OnSet  = "OnSet_";         // Prefix name of functions of owning object that (if defined) automatically receive notification when this property changes)


	public CObj(string sName, object iObjOwner = null, CObj oObjParent = null) {
		_sName  = sName;
		_iObjOwner = iObjOwner;
		_oObjParent = oObjParent;
		if (_oObjParent != null) {
			if (_oObjParent._aChildren == null)
				_oObjParent._aChildren = new List<CObj>();
			_oObjParent._aChildren.Add(this);
		}

        //=== Determine the fully-qualified 'path' to this object all the way from the root ===
        oObjParent = this;
        List<CObj> aObjAncestors = new List<CObj>();
        while (oObjParent._oObjParent != null && oObjParent._oObjParent._oObjParent != null) {
            aObjAncestors.Add(oObjParent._oObjParent);
            oObjParent = oObjParent._oObjParent;
        }
        aObjAncestors.Reverse();
        StringBuilder oStringBuilder_Path = new StringBuilder();
        foreach (CObj oObj in aObjAncestors)
            oStringBuilder_Path.Append($"/{oObj._sName}");
        oStringBuilder_Path.Append("/" + _sName);     // Append our name to form the fully qualified path.
        _sPath = oStringBuilder_Path.ToString();

        //=== Connect getter and setter methods ===
		if (_iObjOwner != null) {
			Type oTypeFields = _iObjOwner.GetType();         // Object's owner has the OnSet_ events we need
			_oMethodInfo_OnGet = oTypeFields.GetMethod(C_Prefix_OnGet + _sName);
			_oMethodInfo_OnSet = oTypeFields.GetMethod(C_Prefix_OnSet + _sName);        // The precise name function must have to enable automatic notification
		}

		//=== Obtain access to the nearest event handler up our parent chain ===
		EventHandler<EventArgs_PropertyValueChanged> ePropertyValueChanged = null;
		CObj oObjIterator = this;
		while (oObjIterator != null && ePropertyValueChanged == null) {
			ePropertyValueChanged = oObjIterator.Event_PropertyValueChanged;
			oObjIterator = oObjIterator._oObjParent;
		}
		Event_PropertyValueChanged = ePropertyValueChanged;			// Set to the closest parent up our chain with this event defined.  (Caller can always manually override right after this ctor call)

		_bInitialized_HACK = true;
	}

	public CObj(string sName, object iObjOwner, CObj oObjParent, float nDefault, float nMin, float nMax, string sDescription = null, int nFlags = 0, object oChoices = null) : this(sName, iObjOwner, oObjParent) {
		_nDefault           = nDefault;
		_nValue             = nDefault;         // Reasonable way to init local value.  (Important for init flow)
		_nMin               = nMin;
		_nMax               = nMax;
		_nMinMaxRange       = _nMax - _nMin;
		_sDescription       = sDescription;
		_nFlags             = nFlags;
		//_oEnumChoices       = oChoices as Type;         // Choices will either be from reflection as a Type...
		//_aStringChoices     = oChoices as string[];     // Or a string array.
		//Debug.Log(string.Format("{0} prop #{1} = '{2}'", _oObject._sNameFull, _nPropOrdinal, _sName));
	}


	~CObj() {
		if (_oObjParent != null)
			_oObjParent._aChildren.Remove(this);
	}


	public CObj Add(string sName, object iObjOwner = null) {
		return new CObj(sName, iObjOwner, this);
	}

	public CObj Add3(string sName, object iObjOwner = null) {
		return new CObj3(sName, iObjOwner, this);
	}

	public CObj Add(string sName, object iObjOwner, float nDefault, float nMin, float nMax, string sDescription = null, int nFlags = 0, object oChoices = null) {
		return new CObj(sName, iObjOwner, this, nDefault, nMin, nMax, sDescription, nFlags, oChoices);
	}


	//---------------------------------------------------------------------------	FIND
	public virtual CObj Find(string sName, bool bThrowIfNotFound = true) {			//###TODO: Recursive by name needed too?
		if (_aChildren != null) {
			foreach (CObj oObj in _aChildren) {
				if (oObj._sName == sName)
					return oObj;
			}
		}
		if (bThrowIfNotFound)
			CUtility.ThrowExceptionF("###EXCEPTION: CObj.Find() cannot find object '{0}'", sName);
		return null;
	}

	//public static CObj FindByFlatTreeID_RECURSIVE(CObj oObjParent, ushort nFlatTreeID) {
	//	if (oObjParent._nFlattenedTreeOrdinal == nFlatTreeID)
	//		return oObjParent;
	//	if (oObjParent._aChildren != null) {
	//		CObj oObjFound = null;
	//		foreach (CObj oObjChild in oObjParent._aChildren) {
	//			oObjFound = FindByFlatTreeID_RECURSIVE(oObjChild, nFlatTreeID);
	//			if (oObjFound != null)
	//				return oObjFound;
	//		}
	//	}
	//	return null;
	//}


	//---------------------------------------------------------------------------	GET / SET
	public virtual float Get(string sName) {
		CObj oObjChild = Find(sName);
		return oObjChild.Get();
	}

	public virtual void Set(string sName, float nValue) {
		CObj oObjChild = Find(sName);
		oObjChild.Set(nValue);
	}

	public virtual float Get() {
		if ((_nFlags & CObj.TripleValues) != 0)
			CUtility.ThrowExceptionF("Invalid access to Get() on triple-property '{0}'", _sNameInCodebase);

		if (_aChildren != null)         // Parent objects have no value
			return 0;
		
		//if (GetType() == typeof(CObjBlender)) {	// For Blender-side objects we must fetch from related CObj_Get() in Blender python		###DESIGN19: Catch Blender get/set in CObjGrpBlender instead?
		//	CObjBlender oObjectBlender = _oObj as CObjBlender;       // Blender property means that we must be owned by a CObjBlender
		//	_nValue = float.Parse(CGame.gBL_SendCmd("CBody", oObjectBlender._sBlenderAccessString + ".GetString('" + _sName + "')"));
		//	return _nValue;
		//} else if (_oObj._bInitialized_HACK && _oMethodInfo_OnGet != null) {		//=== OnSet_ notifications can only be sent when object is fully operational (e.g. not during init) ===
		//	object[] aArgs = new object[] { };
		//	object oReturn = _oMethodInfo_OnGet.Invoke(_oObjParent._oObj._iObjOwner, aArgs);       // If property has a preconfigured 'OnGet_' function call it now to notify parent of change
		//	return (float)oReturn;
		//} else {
		return _nValue;         // If all else fail return local value  ###DESIGN: Keep random?  // Remove random value off the very top.  Pushed in  / pulled in at last minute ###CHECK: Assumes we use Get() everywhere!  (Not true!!) ###BUG??
		//}
	}

	public virtual float Set(float nValueNew) {
		if ((_nFlags & CObj.TripleValues) != 0)
			CUtility.ThrowExceptionF("Invalid access to Get() on triple-property '{0}'", _sNameInCodebase);

		if (_aChildren != null)			// Parent objects have no value
			return 0;

		//nValueNew += _nRndVal;					// Apply separated random value at the very top		###OBS? Random value still used??

		//=== Avoid doing again if we're setting to the same value ===
		//if (_nValue == nValueNew)					//###DESIGN:!!! Need a way to disable this optimization in some cases... e.g. pose load.
		//	return _nValue;

		//=== Cap the value to pre-set bounds ===
		if (nValueNew < _nMin)
			nValueNew = _nMin;
		if (nValueNew > _nMax)
			nValueNew = _nMax;

		float nValueOld = _nValue;

		//=== Set the value in our remote counterparts if flagged as such ===
		_nValue = nValueNew;

		if ((_nFlags & CObj.FromBlender_HACK) != 0) {      //###HACK: Re-route to Blender when set
			CObjBlender oObjParentBlender = _oObjParent._oObjParent._oObjParent as CObjBlender;     //###HACK:!!!!
			_nValue = float.Parse(CGame.gBL_SendCmd("CBody", oObjParentBlender._sBlenderAccessString + ".PropSetString('" + _sNameInCodebase + "'," + nValueNew.ToString() + ")"));
		}

		//=== OnSet_ notifications can only be sent when object is fully operational (e.g. not during init) ===
		if (_bInitialized_HACK) {
			//=== Notify owning object if it requested notification upon change ===
			if (_oMethodInfo_OnSet != null) {								// If property has a preconfigured 'OnSet_' function call it now to notify parent of change
				object[] aArgs = new object[] { nValueOld, nValueNew };
				_oMethodInfo_OnSet.Invoke(_iObjOwner, aArgs);			// CObj's owner has the event properties we need to call, not CObj INSTANCE!!
			}
		}

		//=== Set our GUI appearance if we're currently displayed ===
		//if (_oUIWidget != null)
		//    _oUIWidget.SetValue(_nValue);          //###CHECK: Was Get()!

		//=== Write the just-set property to the script recorder.  This provides a very good starting point to animate a scene ===
		//if (CGame._oScriptRecordUserActions != null) {
		//	if (_sName!= null) {	// We only write property set to script recorder on objects that have provided their script-access handle
		//		if (nValueNew != nValueOld) 			//###IMPROVE!?!?!? Should abort if same earlier?? Safe??
		//			CGame._oScriptRecordUserActions.WriteProperty(this);
		//	}
		//}
		//=== Notify object that we really did change.  This will in turn notify any owning object that have registered for this event ===
		Notify_PropertyValueChanged(nValueOld);		//###TODO13: Convert previous codebase that needed this functionality with this new event-based mechanism!
        
        //=== Set our timestamp-at-last-change and increment the global counter ===
        _nTimeStamp = CObj._nTimeStamp_Global;
        CObj._nTimeStamp_Global++;

		return _nValue;
	}



	//---------------------------------------------------------------------------	LOAD / SAVE

	public void Load(string sSerializedProps) {
		//=== Flatten our tree of objects into one easy-to-iterate flat list ===
		Dictionary<string, CObj> mapFlatObjectTree = GetFlattenObjectTree();

        //=== Reset all our properties to zero (we only saved non-zero ones) ===
		foreach (KeyValuePair<string, CObj> oKeyValuePair in mapFlatObjectTree) {
			CObj oObj = oKeyValuePair.Value;
            if (oObj.Get() != 0)
                oObj.Set(0);
        }

        //=== Split the serialized stream into its individual records (separated by comma) ===
        string[] aSeparator = new string[1];
        aSeparator[0] = ",";
        string[] aSerializedProps = sSerializedProps.Split(aSeparator, StringSplitOptions.RemoveEmptyEntries);

        //=== Iterate through every saved record, match to the right object and set it! ===
        foreach (string sPropRecord in aSerializedProps) {
            string[] aPropRecordNameValue = sPropRecord.Split('=');
            string sPath = aPropRecordNameValue[0];
            float nPropValue;
            Debug.Assert(float.TryParse(aPropRecordNameValue[1], out nPropValue));
            bool bFound = false;
		    foreach (KeyValuePair<string, CObj> oKeyValuePair in mapFlatObjectTree) {
			    CObj oObj = oKeyValuePair.Value;
                if (oObj._sPath == sPath) {
                    oObj.Set(nPropValue);
                    bFound = true;
                    break;
                }
            }
            if (bFound == false)
                Debug.LogWarning($"#WARNING: CObj.Load() could not find object '{sPath}' that was referred to in serialized stream.");
        }
	}

	public string Save() {
		//=== Flatten our tree of objects into one easy-to-iterate flat list ===
		Dictionary<string, CObj> mapFlatObjectTree = GetFlattenObjectTree();

		//=== Write the to-be-saved properties in two flat arrays for sorting ===
        StringBuilder oStringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, CObj> oKeyValuePair in mapFlatObjectTree) {
			CObj oObj = oKeyValuePair.Value;
			float nPropValue = oObj.Get();
			if (nPropValue != 0)
                oStringBuilder.Append($"{oObj._sPath}={nPropValue},");
		}
        string sSerializedProps = oStringBuilder.ToString();
		Debug.Log($"CObjGrp.Save('{_sName}') saved property tree as '{sSerializedProps}'.");
        return sSerializedProps;
	}

	Dictionary<string, CObj> GetFlattenObjectTree() {
		Dictionary<string, CObj> mapFlatObjectTree = new Dictionary<string, CObj>();
		GetFlattenObjectTree_RECURSIVE(ref mapFlatObjectTree);
		return mapFlatObjectTree;
	}

	void GetFlattenObjectTree_RECURSIVE(ref Dictionary<string, CObj> mapFlatObjectTree) {
        //=== Add this object only if it is a leaf (no children) ===
        if (_aChildren == null || _aChildren.Count == 0)
		    mapFlatObjectTree.Add(_sPath, this);
        if (_aChildren != null) {
		    foreach (CObj oObjChildren in _aChildren)
			    oObjChildren.GetFlattenObjectTree_RECURSIVE(ref mapFlatObjectTree);
        }
	}


	//---------------------------------------------------------------------------	EVENTS

	public void Notify_PropertyValueChanged(float nValueOld) {			//###INFO: How to implement events
		if (Event_PropertyValueChanged != null) {
			EventArgs_PropertyValueChanged oEventArgs = new EventArgs_PropertyValueChanged();
			oEventArgs.CObj = this;
			Event_PropertyValueChanged(this, oEventArgs);
		}
	}

	//---------------------------------------------------------------------------	GUI GENERATION
    public void BuildWebGui_RECURSIVE(CBrowser oBrowser, string sNameParent_Global) {
        string sTypeWidget = "";
        if ((_nFlags & CObj.AsSpinner) != 0)
            sTypeWidget = "CSpinner";
        else if ((_nFlags & CObj.AsButton) != 0)
            sTypeWidget = "CButton";
        else
            Debug.LogWarning($"#WARNING: CObj.BuildWebGui_RECURSIVE() gets invalid control type! on '{_sName}'");
        
        //window.CObj_Create = function(sNameParent_Global, sLabel, nValue, nMin, nMax, sDescription, sTypeWidget)
        string sEval = $"window.CObj_Create(\"{sNameParent_Global}\", \"{_sName}\", {_nValue.ToString()}, {_nMin.ToString()}, {_nMax.ToString()}, \"{_sDescription}\", \"{sTypeWidget}\");";
        oBrowser.SendEvalToBrowser(sEval);

        if (_aChildren != null) {
		    foreach (CObj oObjChildren in _aChildren)
                oObjChildren.BuildWebGui_RECURSIVE(oBrowser, sNameParent_Global);
        }
    }
}

public class EventArgs_PropertyValueChanged : EventArgs {
	public CObj		CObj					{ get; set; }
}




public class CObjBlender : CObj {           // CObjBlender: Specialized version of CObjProp that mirrors an equivalent CObj structure in Blender.  Used for remote Blender property access

	public string _sBlenderAccessString;            // The fully-qualified 'Blender Access String' where we can obtain our Blender-based CObj equivalent designed to communicate with this Unity-side object.

	public CObjBlender(string sName, string sBlenderAccessString, object iObjOwner = null, CObj oObjParent = null) : base(sName, iObjOwner, oObjParent) {
		_sBlenderAccessString = sBlenderAccessString;
	}

	public void ObtainBlenderObjects() { 
		string sSerializedCSV = CGame.gBL_SendCmd("CBody", _sBlenderAccessString + ".Serialize()");            //###MOVE11: to another blender codefile?

		string[] aFields = CUtility.SplitDelimiterString_Python(sSerializedCSV);

		//_sName = aFields[0];				//###CHECK19: Save Blender's name as our own?
		int nProps = int.Parse(aFields[1]);
		//int nFlatTreeID = 0;
		CObj oObjCat1 = null;
		CObj oObjCat2 = null;

		for (int nProp = 0; nProp < nProps; nProp++) {
			sSerializedCSV = CGame.gBL_SendCmd("CBody", _sBlenderAccessString + ".SerializeProp(" + nProp.ToString() + ")");
			aFields = CUtility.SplitDelimiterString_Python(sSerializedCSV);
			string sNameInCodebase = aFields[0];
			string[] aFieldsName = CUtility.SplitDelimiterString(sNameInCodebase, "_");

			string sCat1		= aFieldsName[0];
			string sCat2		= aFieldsName[1];
			string sNameObj		= aFieldsName[2];
			string sLevel		= aFieldsName[3];

			oObjCat1 = Find(sCat1, /*bThrowIfNotFound=*/ false);
			if (oObjCat1 == null) {
				oObjCat1 = new CObj(sCat1, null, this);
				//oObjCat1._nFlattenedTreeOrdinal = ++nFlatTreeID;
			}

			oObjCat2 = oObjCat1.Find(sCat2, /*bThrowIfNotFound=*/ false);
			if (oObjCat2 == null) { 
				oObjCat2 = new CObj(sCat2, null, oObjCat1);
				//oObjCat2._nFlattenedTreeOrdinal = ++nFlatTreeID;
			}

			string sDescription = aFields[1];
			float nValue = float.Parse(aFields[2]);
			float nMin = float.Parse(aFields[3]);
			float nMax = float.Parse(aFields[4]);
			//int eFlags              = int  .Parse(aFields[5]);		//###CHECK19: Name = label below??

			CObj oObj = new CObj(sNameObj, null, oObjCat2, nValue, nMin, nMax, sDescription);
			oObj._nFlags |= CObj.FromBlender_HACK;          //###HACK: Manually flag this propery as 'being from Blender' (so Blender is updated when property changes)
			oObj._sNameInCodebase = sNameInCodebase;		// Remember Blender's name for this property as we need it during changes.
			//oObj._nFlattenedTreeOrdinal = ++nFlatTreeID;
		}
	}
}

//public class CObjEnum : CObj {          // CObjBlender: Specialized version of CObjProp that holds only enumerated properties (e.g. each property key is a member of an C# enum)
//	public Type             _oTypeEnum;         // The type of the enum that is the basis of this property group.
//	public FieldInfo[]      _aFieldsEnum;

//	public CObjEnum(string sNameObject, Type oTypeEnum) : base(null, sNameObject, sNameObject) {
//		_oTypeEnum      = oTypeEnum;
//		_aFieldsEnum = _oTypeEnum.GetFields();
//	}

//	public CObj Add(object oObjOrdinal, string sLabel, float nDefault, float nMin, float nMax, string sDescription, int nFlags = 0) {
//		int nPropOrdinal = (int)oObjOrdinal;
//		string sName = _aFieldsEnum[nPropOrdinal + 1].Name;     // For some reason reflection on enums returns '_value' for index zero with the real enum fields starting at index 1
//		return base.Add(null, sName, sLabel, nDefault, nMin, nMax, sDescription, nFlags);
//	}
//}


//public float		_nRndVal;				// The actual random value added at the very end of property get/set (kept separated to not mix with user-set value)
//public float		_nRndValSource;			// Randomization source value (at beginning of smoothing iterations)
//public float		_nRndValTarget;			// Randomization target value (at end       of smoothing iterations)
//public float		_nRndValSmoothVelocity; // Smoothing velocity (utilty var needed for smoothing)
//public float        _nAdjustTargetVal;      // Runtime adjust functionality.  What the property value becomes RuntimeAdjust_SetTargetValue() gets passed a nDistThisFrame over _nAdjustDistPerFrame
//public float        _nAdjustDistPerFrame;	// How much movement distance required by frame to set the property to _nAdjustTargetVal


//public void Serialize(FileStream oStream) {		//###BROKEN19: Delegate to CObj too now
//if (oStream.CanWrite) {
//	oStream.WriteByte(GetNumProps());
//} else {
//	byte nPropsInStream = (byte)oStream.ReadByte();
//	byte nPropsInProp	= GetNumProps();
//	if (nPropsInStream != nPropsInProp)
//		CUtility.ThrowException(string.Format("ERROR: CObj.Serialize_Actors_OBS() attempted to load {0} properties on object of type '{1}' which has {2} properties", nPropsInStream, _sName, nPropsInProp));
//}
//foreach (CObj oObj in _aChildren)
//	oObj.Serialize(oStream);

//if (_nBodyID != 0 && _sNameScriptHandle != null)	// We only write property set to script recorder on objects that have provided their script-access handle
//	CGame._oScriptRecordUserActions.WriteObject(this);
//}




//-------- Old file base serialize stuff
		////=== Count the number of non-zero properties to save ===
		//int nPropsToSave = 0;
		//foreach (KeyValuePair<string, CObj> oKeyValuePair in mapFlatObjectTree) {
		//	CObj oObj = oKeyValuePair.Value;
		//	float nPropValue = oObj.Get();
		//	if (nPropValue != 0)
		//		nPropsToSave++;
		//}

		////=== Write the to-be-saved properties in two flat arrays for sorting ===
		//CObj[] aObjToSave = new CObj[nPropsToSave];
		//string[] aPropsToSave_Name = new string[nPropsToSave];
		//int nObj = 0;
		//foreach (KeyValuePair<string, CObj> oKeyValuePair in mapFlatObjectTree) {
		//	CObj oObj = oKeyValuePair.Value;
		//	float nPropValue = oObj.Get();
		//	if (nPropValue != 0) {
		//		aObjToSave[nObj] = oObj;
		//		aPropsToSave_Name[nObj] = oObj._sName;
		//		nObj++;
		//	}
		//}
		//System.Array.Sort(aPropsToSave_Name, aObjToSave);

		//=== Write to file the sorted array of properties ===
		//string sPathFile = CGame.GetPath_Properties() + sPropFilterPrefix + "-" + sFileSuffix + ".txt";
		//System.IO.StreamWriter oStreamWrite = new System.IO.StreamWriter(sPathFile);
		//for (nObj = 0; nObj < nPropsToSave; nObj++) {
		//	CObj oObjToSave = aObjToSave[nObj];
		//	float nPropValue = oObjToSave.Get();
		//	string sPropSuffix = oObjToSave._sName.Substring(sPropFilterPrefix.Length + 1);
		//	string sLine = string.Format("{0:F6}\t{1}", nPropValue, sPropSuffix);           //###TODO:!!! Remove prop buy level suffix from file!!!
		//	oStreamWrite.WriteLine(sLine);
		//}
		//oStreamWrite.Close();



		//string sPathFile = CGame.GetPath_Properties() + sPropFilterPrefix + "-" + sFileSuffix + ".txt";
		//if (System.IO.File.Exists(sPathFile)) {
		//	Debug.LogFormat("CObj.Load('{0}') saving properties with prefix filter '{1}' and file suffix '{2}'", _sName, sPropFilterPrefix, sFileSuffix);
		//	System.IO.StreamReader oStreamReader = new System.IO.StreamReader(sPathFile);

		//	//=== Reset all our filtered properties to zero.  File only stores the non-zero properties ===
		//	foreach (CObj oObjChild in _aChildren)
		//		if (oObjChild._sName.StartsWith(sPropFilterPrefix) && oObjChild._nValue != 0)
		//			oObjChild.Set(0);                   //###OPT:!! Slow!  Recalc normals at each prop set?  Can recalc once after all properties loaded?  Also going to Blender... neeeded??

		//	//=== Read each line of the file and attempt to find that property and set it ===
		//	float nPropValue = 0;
		//	while (oStreamReader.Peek() >= 0) {
		//		string sLine = oStreamReader.ReadLine();
		//		string[] aLineFields = sLine.Split('\t');           // Properties file is a very simple text file with 'value' + <TabCharacter> + 'property name' format
		//		string sPropValue = aLineFields[0];
		//		string sNameSuffix = aLineFields[1];
		//		string sName = sPropFilterPrefix + "_" + sNameSuffix;
		//		float.TryParse(sPropValue, out nPropValue);
		//		CObj oObj = Find(sName, /*bThrowIfNotFound=*/ false);
		//		if (oObj != null) {
		//			oObj.Set(nPropValue);
		//		} else {
		//			Debug.LogWarningFormat("#Warning: CObjGrp.Load() could not find property '{0}'", sName);
		//		}
		//	}
		//	oStreamReader.Close();
		//	return true;        // Success
		//} else {
		//	Debug.LogErrorFormat("###ERROR: CObjGrp.Load() could not find file '{0}'", sPathFile);
		//	return false;           // Failure
		//}
