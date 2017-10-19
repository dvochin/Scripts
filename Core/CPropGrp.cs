/*###DISCUSSION: New Property Group System
=== LAST ===
- Can only get/set find from prop group now?
- Should CProp know anything about Blender or enums?
- Revisit usage of flags, etc.

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
using System.Reflection;
using System.Collections.Generic;


public class CPropGrp {		// CPropGrp: Enables children CProp properties of this object to be 'grouped' by functionality such as all enum properties, all Blender properties, etc
	public CObject			_oObj;							// The CObject that owns us
	public string			_sNamePropGrp;					// Name of this property group.  Can be shown in GUI widgets
    public CProp[]			_aProps;						// The properties owned by this property group.  They all have commonality based on the subclass of CPropGrp
	CUISeparator			_oWidgetSeparator;				// The 'separator' widget created when rendered in a GUI to visually separate properties by their property group

    public CPropGrp(CObject oObj, string sNamePropGrp) {
		_oObj				= oObj;
		_sNamePropGrp		= sNamePropGrp;
		_oObj.AddPropGrp(this);							// Automatically add our property group to our owner's collection for us.
	}

	public CProp PropAdd(object oPropOrdinal, string sNameProp, string sLabel, float nDefault, float nMin, float nMax, string sDescription, int nPropFlags = 0, object oChoices = null) {
		int nPropOrdinal = (int)oPropOrdinal;
		if (_aProps == null)
			_aProps = new CProp[nPropOrdinal + 1];
		if (_aProps.Length < nPropOrdinal + 1)
			Array.Resize<CProp>(ref _aProps, nPropOrdinal + 1);
		CProp oProp = new CProp(this, nPropOrdinal, sNameProp, sLabel, nDefault, nMin, nMax, sDescription, nPropFlags, oChoices);
		_aProps[nPropOrdinal] = oProp;
		return oProp;
	}


	//---------------------------------------------------------------------------	FIND
	public CProp PropFind(object oPropOrdinal) {
		int nPropOrdinal = (int)oPropOrdinal;
		if (nPropOrdinal < 0 || nPropOrdinal >= _aProps.Length)
			CUtility.ThrowException(string.Format("ERROR: CProp.PropFind() obtained invalid ordinal {0} while searching properties on object of type {1}", nPropOrdinal, _sNamePropGrp));
		CProp oProp = _aProps[nPropOrdinal];
		return oProp;
	}

	public CProp PropFind(string sNameProp, bool bThrowIfNotFound = true) {
		foreach (CProp oProp in _aProps) {
			if (oProp._sNameProp == sNameProp)
				return oProp;
		}
		if (bThrowIfNotFound)
			CUtility.ThrowExceptionF("###EXCEPTION: CProp.PropFind() cannot find property '{0}'", sNameProp);
		return null;
	}


	//---------------------------------------------------------------------------	GET / SET
	public virtual float PropGet(object oPropOrdinal) {
		int nPropOrdinal = (int)oPropOrdinal;
		CProp oProp = PropFind(nPropOrdinal);
		return oProp.PropGet();
	}
	public float PropGet(string sNameProp) {
		return PropFind(sNameProp).PropGet();
	}

	public void PropSet(string sNameProp, float nValue) {
		PropFind(sNameProp).PropSet(nValue);
	}

	public virtual float PropSet(object oPropOrdinal, float nValue) {
		int nPropOrdinal = (int)oPropOrdinal;
		CProp oProp = PropFind(nPropOrdinal);
		return oProp.PropSet(nValue);
	}

	//public virtual float PropSet(string sPropName, float nValue) {
	//	int nPropOrdinal = (int)Enum.Parse(_oTypeFieldsEnum, sPropName);
	//	CProp oProp = PropFind(nPropOrdinal);
	//	return oProp.PropSet(nValue);
	//}

	//---------------------------------------------------------------------------	WIDGETS
	public void Widget_Separator_Create(CUIPanel oPanel) {
		_oWidgetSeparator = CUISeparator.Create(oPanel, null);
	}

	public void Widget_Separator_Destroy() {
		if (_oWidgetSeparator != null) {
			GameObject.Destroy(_oWidgetSeparator.gameObject);
			_oWidgetSeparator = null;
		}
	}

	//---------------------------------------------------------------------------	LOAD / SAVE

	public bool Load(string sPropFilterPrefix, string sFileSuffix) {
		string sPathFile = CGame.GetPath_Properties() + sPropFilterPrefix + "-" + sFileSuffix + ".txt";

		if (System.IO.File.Exists(sPathFile)) {
			Debug.LogFormat("CPropGrp.Load('{0}.{1}') saving properties with prefix filter '{2}' and file suffix '{3}'", _oObj._sNameObject, _sNamePropGrp, sPropFilterPrefix, sFileSuffix);
			System.IO.StreamReader oStreamRead = new System.IO.StreamReader(sPathFile);

			//=== Reset all our filtered properties to zero.  File only stores the non-zero properties ===
			foreach (CProp oProp in _aProps)
				if (oProp._sNameProp.StartsWith(sPropFilterPrefix) && oProp._nValueLocal != 0)
					oProp.PropSet(0);                   //###OPT:!! Slow!  Recalc normals at each prop set?  Can recalc once after all properties loaded?  Also going to Blender... neeeded??

			//=== Read each line of the file and attempt to find that property and set it ===
			float nPropValue = 0;
			while (oStreamRead.Peek() >= 0) {
				string sLine = oStreamRead.ReadLine();
				string[] aLineFields = sLine.Split('\t');           // Properties file is a very simple text file with 'value' + <TabCharacter> + 'property name' format
				string sPropValue		= aLineFields[0];
				string sNamePropSuffix	= aLineFields[1];
				string sNameProp = sPropFilterPrefix + "_" + sNamePropSuffix;
				float.TryParse(sPropValue, out nPropValue);
				CProp oProp = PropFind(sNameProp, /*bThrowIfNotFound=*/ false);
				if (oProp != null) {
					oProp.PropSet(nPropValue);
				} else {
					Debug.LogWarningFormat("#Warning: CPropGrp.Load() could not find property '{0}'", sNameProp);
				}
			}
			oStreamRead.Close();
			return true;        // Success
		} else {
			Debug.LogErrorFormat("###ERROR: CPropGrp.Load() could not find file '{0}'", sPathFile);
			return false;           // Failure
		}
	}

	public void Save(string sPropFilterPrefix, string sFileSuffix) {
		Debug.LogFormat("CPropGrp.Save('{0}.{1}') saving properties with prefix filter '{2}' and file suffix '{3}'", _oObj._sNameObject, _sNamePropGrp, sPropFilterPrefix, sFileSuffix);

		//=== Count the number of properties to save ===
		int nPropsToSave = 0;
		foreach (CProp oProp in _aProps) {
			float nPropValue = oProp.PropGet();
			if (nPropValue != 0 && oProp._sNameProp.StartsWith(sPropFilterPrefix))
				nPropsToSave++;
		}

		//=== Write the to-be-saved properties in two flat arrays for sorting ===
		CProp[]  aPropsToSave_Prop	= new CProp[nPropsToSave];
		string[] aPropsToSave_Name	= new string[nPropsToSave];
		int nProp = 0;
		foreach (CProp oProp in _aProps) {
			if (oProp.PropGet() != 0 && oProp._sNameProp.StartsWith(sPropFilterPrefix)) {
				aPropsToSave_Prop[nProp] = oProp;
				aPropsToSave_Name[nProp] = oProp._sNameProp;
				nProp++;
			}
		}
		System.Array.Sort(aPropsToSave_Name, aPropsToSave_Prop);

		//=== Write to file the sorted array of properties ===
		string sPathFile = CGame.GetPath_Properties() + sPropFilterPrefix + "-" + sFileSuffix + ".txt";
		System.IO.StreamWriter oStreamWrite = new System.IO.StreamWriter(sPathFile);
		for (nProp = 0; nProp < nPropsToSave; nProp++) {
			CProp oPropToSave = aPropsToSave_Prop[nProp];
			float nPropValue = oPropToSave.PropGet();
			string sPropSuffix = oPropToSave._sNameProp.Substring(sPropFilterPrefix.Length + 1);
			string sLine = string.Format("{0:F6}\t{1}", nPropValue, sPropSuffix);           //###TODO:!!! Remove prop buy level suffix from file!!!
			oStreamWrite.WriteLine(sLine);
		}
		oStreamWrite.Close();
	}

	//---------------------------------------------------------------------------	UTILITY
	public byte GetNumProps() {
		return (byte)_aProps.Length;
	}
};



public class CPropGrpBlender : CPropGrp {           // CObjectBlender: Specialized version of CObjProp that mirrors an equivalent CObject structure in Blender.  Used for remote Blender property access

	public string _sBlenderAccessString;			// The fully-qualified 'Blender Access String' where we can obtain our Blender-based CObject equivalent designed to communicate with this Unity-side object.

	public CPropGrpBlender(CObject oObj, string sNamePropGrp, string sBlenderAccessString) : base(oObj, sNamePropGrp) {
		_sBlenderAccessString = sBlenderAccessString;

		string sSerializedCSV = CGame.gBL_SendCmd("CBody", _sBlenderAccessString + ".Serialize()");            //###MOVE11: to another blender codefile?

		string[] aFields = CUtility.SplitCommaSeparatedPythonListOutput(sSerializedCSV);

		//_sNameObject = aFields[0];				//###CHECK19: Save Blender's name as our own?
		int nProps = int.Parse(aFields[1]);

		for (int nProp = 0; nProp < nProps; nProp++) {
			sSerializedCSV = CGame.gBL_SendCmd("CBody", _sBlenderAccessString + ".SerializeProp(" + nProp.ToString() + ")");
			aFields = CUtility.SplitCommaSeparatedPythonListOutput(sSerializedCSV);
			string sName = aFields[0];
			string sDescription = aFields[1];
			float nValue = float.Parse(aFields[2]);
			float nMin = float.Parse(aFields[3]);
			float nMax = float.Parse(aFields[4]);
			//int eFlags              = int  .Parse(aFields[5]);		//###CHECK19: Name = label below??
			CProp oProp = PropAdd(nProp, sName, sName, nValue, nMin, nMax, sDescription, 0, null);//, CProp.Blender, null);	//###NOTE: No longer a Blender property as we don't update every slider value change for better performance (we batch update during mode change now)
			oProp._nValueLocal = nValue;              // Bit of a hack for optimization: set value directly as we just retrieved it from Blender to save a trip back and forth.
		}
	}
};

public class CPropGrpEnum : CPropGrp {          // CObjectBlender: Specialized version of CObjProp that holds only enumerated properties (e.g. each property key is a member of an C# enum)
	public Type				_oTypeEnum;			// The type of the enum that is the basis of this property group.
	public FieldInfo[]		_aFieldsEnum;

	public CPropGrpEnum(CObject oObj, string sNamePropGrp, Type oTypeEnum) : base(oObj, sNamePropGrp) {
		_oTypeEnum		= oTypeEnum;
		_aFieldsEnum = _oTypeEnum.GetFields();
	}

	public CProp PropAdd(object oPropOrdinal, string sLabel, float nDefault, float nMin, float nMax, string sDescription, int nPropFlags = 0) {
		int nPropOrdinal = (int)oPropOrdinal;
		string sNameProp = _aFieldsEnum[nPropOrdinal + 1].Name;		// For some reason reflection on enums returns '_value' for index zero with the real enum fields starting at index 1
		return base.PropAdd(nPropOrdinal, sNameProp, sLabel, nDefault, nMin, nMax, sDescription, nPropFlags);
	}
}
