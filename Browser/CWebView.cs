using UnityEngine;
using System;
using System.Collections.Generic;

public class CWebView: IDisposable                  // CWebView: Proxy to the equivalent 'CWebView' in Browser J-script codebase.  Abstract class that implements a viewer / editor of some sort.
{        
    public CBrowser     _oBrowser;                  // Browser that 'owns' us.  It has to point to j-script code that implements the browser equivalent of 'CWebViewTree'
    public string       _sScriptVariableName;       // The name of our script variable.  Our web codebase will notify this instance through the script runtime with the use this variable.
    public string       _sTabTitle;                 // The title shown in our tab header.

    public CWebView(CBrowser oBrowser, string sScriptVariableName, string sTabTitle) {
        _oBrowser = oBrowser;
        _sScriptVariableName = sScriptVariableName;
        _sTabTitle = sTabTitle;
        CGame._oScriptPlay.ExportAdd(_sScriptVariableName, this);        // Register this instance under the '_sScriptVariableName' global name so jscript can call our member functions.
    }

    public virtual void Dispose() {
        //=== Send message to web to destroy this view and its associated tab ===
        _oBrowser.SendEvalToBrowser($"window.{_sScriptVariableName}.U2W_DoDestroy(); window.{_sScriptVariableName} = null;");
        CGame._oScriptPlay.ExportRemove(_sScriptVariableName);           // Unregister this instance from the global script exports (jscript doesn't need to call this instance anymore)
        _sScriptVariableName = null;
        // ~CWebView() {       //###LEARN: Finalizer NOT deterministic (runs whenever GC runs)  To force call GC.Collect(); then GC.WaitForPendingFinalizers();
    }

    public virtual void View_Reset() {
        //=== Clear everything and re-upload everything to update the properties.  (Not needed in usual cases where underlying property strange doesn't change and value changes are only made from Web Browser) ===
		_oBrowser.SendEvalToBrowser($"window.{_sScriptVariableName}.U2W_View_Reset();");
    }

    public void U2W_ManuallyLoadRecord(string sKeyValue) {
		_oBrowser.SendEvalToBrowser($"window.{_sScriptVariableName}.U2W_ManuallyLoadRecord(\"{sKeyValue}\");");
        // JScript will load the requested record and call our 'W2U_DataLoad_ApplyDbData()' with the data
    }

    public virtual void View_Update() { }
    public virtual void W2U_DataLoad_ApplyDbData(int nID, string sValueToLoad) { }    // Load functionality: User has clicked on a 'load' icon in this web GUI.  Notify Unity of the just-obtained DB record so it updates its state.
	public virtual void W2U_DataSave1_RequestDataForDbSave(string sJscriptFunctionUnityCallsToRespond) { }   // Save part 1: User has clicked on a 'save' button in this Web GUI.  This call is send from web to Unity to request it to serialize its data and call U2W_DataSave2_SavedDataForDbStorage() below for us to store it.
}

public class CWebViewTree : CWebView                // CWebViewTree: Proxy to the equivalent 'CWebViewTree' in Browser J-script codebase.  Implements a tree-based 'property editor' functionality capable of editing a tree of our universal CObj
{        
    public CObj         _oObj;                      // The root object we edit.  (The root is not shown, only its immediate siblings appear as 1st-level properties)
    public List<CObj>   _aFlatLookupArray_Objects = new List<CObj>();    // The 'flattened' version of the CObj instances we are editing in our equivalent browser tree grid.  Enables the browser to precisely refer to the our proper CObj instance by the ordinal provided by this list.
    public Int64        _nTimeStamp_AtLastUpdate;   // Timestamp (pulled from CObj._nTimeStamp_Global) when view was last updated.

    public CWebViewTree(CBrowser oBrowser, CObj oObj, string sScriptVariableName, string sTabTitle) : base(oBrowser, sScriptVariableName, sTabTitle) {
        _oObj = oObj;
        // Ask our web codebase to create a new tab and populate it with a view of type 'CWebViewTree' (This will create the tab and also insert a global variable of name '_sScriptVariableName' into the accessible 'window' global namespace by internally calling window[ID] = this;
		_oBrowser.SendEvalToBrowser($"window.{_sScriptVariableName} = isc.CWebViewTree.create();");
        _oBrowser.SendEvalToBrowser($"window.{_sScriptVariableName}.U2W_DoCreate(\"{_sScriptVariableName}\", \"{_sTabTitle}\");");
        View_Reset();          // Perform the first update so user sees our properties at init.
    }

    public override void Dispose() {
        base.Dispose();
    }

    public override void View_Reset() {
        base.View_Reset();
        //=== Clear everything and re-upload everything to update the properties.  (Not needed in usual cases where underlying property strange doesn't change and value changes are only made from Web Browser) ===
        _aFlatLookupArray_Objects.Clear();
        View_DefineTreeNodes_RECURSIVE(_oObj);
        _nTimeStamp_AtLastUpdate = CObj._nTimeStamp_Global;     // Mark ourselves as current.
    }

    public override void View_Update() {
        //=== Send only the properties that have changed since the last call to 'View_Update' or 'View_Reset' === 
        foreach (CObj oObj in _aFlatLookupArray_Objects) {
            if (oObj._nTimeStamp >= _nTimeStamp_AtLastUpdate)       // If this object has been changed since we last marked our last time-at-refresh, then send it as Web view value is stale
                _oBrowser.SendEvalToBrowser($"window.{_sScriptVariableName}.U2W_Node_Update('{oObj._sName}', {oObj._nFlattenedTreeOrdinal}, {oObj._nValue});");
        }
        _nTimeStamp_AtLastUpdate = CObj._nTimeStamp_Global;     // Mark ourselves as current.
    }

	public override void W2U_DataLoad_ApplyDbData(int nID, string sValueToLoad) {
		// Load functionality: User has clicked on a 'load' icon in this web GUI.  Notify Unity of the just-obtained DB record so it updates its state.
        float nValueNew;
        float.TryParse(sValueToLoad, out nValueNew);
        ushort nFlattenedTreeOrdinal = (ushort)nID;
        CObj oObj = _aFlatLookupArray_Objects[nFlattenedTreeOrdinal];
        Debug.Log($"W2U_OnChangedPropertyValue sets object '{oObj._sName}' at flat tree ID {nFlattenedTreeOrdinal} to value {nValueNew}");
        oObj.Set(nValueNew);
	}
	public override void W2U_DataSave1_RequestDataForDbSave(string sJscriptFunctionUnityCallsToRespond) {
		// Save part 1: User has clicked on a 'save' button in this Web GUI.  This call is send from web to Unity to request it to serialize its data and call U2W_DataSave2_SavedDataForDbStorage() below for us to store it.
	}


	void View_DefineTreeNodes_RECURSIVE(CObj oObj) {
		oObj._nFlattenedTreeOrdinal = _aFlatLookupArray_Objects.Count;                // Tell the CObj its transient ID into our flattened ordinal
		_aFlatLookupArray_Objects.Add(oObj);
        _oBrowser.SendEvalToBrowser($"window.{_sScriptVariableName}.U2W_Node_Add('{oObj._sName}', {oObj._nFlattenedTreeOrdinal}, {((oObj._oObjParent != null) ? oObj._oObjParent._nFlattenedTreeOrdinal : -1)}, {oObj._nValue}, {oObj._nMin}, {oObj._nMax}, '{oObj._sDescription}');");
		if (oObj._aChildren != null) {
			foreach (CObj oObjChild in oObj._aChildren)
				View_DefineTreeNodes_RECURSIVE(oObjChild);
		}
	}
}



//==========================================================================    CWebViewGrid
public class CWebViewGrid: CWebView                // CWebViewGrid: Proxy to the equivalent 'CWebViewGrid' in Browser J-script codebase.  Implements a 'grid base' 'database editor' functionality capable of viewing / editing flat database records in a tabular + detail-view format
{        
    public CWebViewGrid(CBrowser oBrowser, string sNameClass, string sScriptVariableName, string sTabTitle)
        : base(oBrowser, sScriptVariableName, sTabTitle) {
        // Ask our web codebase to create a new tab and populate it with a view of type 'CWebViewGrid' (This will create the tab and also insert a global variable of name '_sScriptVariableName' into the accessible 'window' global namespace by internally calling window[ID] = this;
		_oBrowser.SendEvalToBrowser($"window.{_sScriptVariableName} = isc.CWebViewGrid.create();");
        _oBrowser.SendEvalToBrowser($"window.{_sScriptVariableName}.U2W_DoCreate(\"{sNameClass}\", \"{_sScriptVariableName}\", \"{_sTabTitle}\");");
        View_Reset();          // Perform the first update so user sees our properties at init.
    }

    //public override void Dispose() {
    //    base.Dispose();
    //}

    //public override void View_Reset() {
    //    base.View_Reset();
    //}
}

//==========================================================================    CWebViewGridPose
public class CWebViewGridPose: CWebViewGrid                // CWebViewGridPose: Derived from CWebViewGrid to support pose loading / saving
{        
    public CWebViewGridPose(CBrowser oBrowser, string sNameClass, string sScriptVariableName, string sTabTitle)
        : base(oBrowser, sNameClass, sScriptVariableName, sTabTitle) {
    }

	public override void W2U_DataLoad_ApplyDbData(int nID, string sValueToLoadEncoded) {
        CGame._aBodyBases[0].Pose_Load(sValueToLoadEncoded);
	}
	public override void W2U_DataSave1_RequestDataForDbSave(string sJscriptFunctionUnityCallsToRespond) {
		// Save part 1: User has clicked on a 'save' button in this Web GUI.  This call is send from web to Unity to request it to serialize its data and call U2W_DataSave2_SavedDataForDbStorage() below for us to store it.
        string sDataPoseEncoded = CGame._aBodyBases[0].Pose_Save();                //#DEV27:  Pose??? this is CWebViewGrid!!  ###DESIGN!!!!!       ###TODO: Which body???   ###TODO: nID not used??
        string sEvalToBrowser = sJscriptFunctionUnityCallsToRespond + "('" + sDataPoseEncoded + "');";
		_oBrowser.SendEvalToBrowser(sEvalToBrowser);
	}
}

//==========================================================================    CWebViewGridBody
public class CWebViewGridBody: CWebViewGrid                // CWebViewGridBody: Derived from CWebViewGrid to support body shape def load / save
{        
    public CWebViewGridBody(CBrowser oBrowser, string sNameClass, string sScriptVariableName, string sTabTitle)
        : base(oBrowser, sNameClass, sScriptVariableName, sTabTitle) {
    }

	public override void W2U_DataLoad_ApplyDbData(int nID, string sValueToLoad) {
        //string sValueToLoad = System.Net.WebUtility.HtmlDecode(sValueToLoadEncoded);
        CGame._aBodyBases[0]._oObj.Load(sValueToLoad);

	}
	public override void W2U_DataSave1_RequestDataForDbSave(string sJscriptFunctionUnityCallsToRespond) {
		// Save part 1: User has clicked on a 'save' button in this Web GUI.  This call is send from web to Unity to request it to serialize its data and call U2W_DataSave2_SavedDataForDbStorage() below for us to store it.
        string sDataBody = CGame._aBodyBases[0]._oObj.Save();                //#DEV27:  Pose??? this is CWebViewGrid!!  ###DESIGN!!!!!       ###TODO: Which body???   ###TODO: nID not used??
        //string sDataBodyEncoded = Convert.FromBase64String(sDataBody);
        //string sDataBodyEncoded = System.Net.WebUtility.HtmlEncode(sDataBody);
        string sEvalToBrowser = sJscriptFunctionUnityCallsToRespond + "('" + sDataBody + "');";
		_oBrowser.SendEvalToBrowser(sEvalToBrowser);
	}
}


#region (JUNK)
//void Util_SendTree_Bones_RECURSIVE(Transform oBoneT, int nFlattenedTreeOrdinal_Parent, ref List<Transform> aFlatLookupArray_Bones) {
//###DESIGN: Any value in editing bones in browser (e.g. browser providing the 'upper smarts' of game??
//	//oObj._nFlattenedTreeOrdinal = aFlatLookupArray_Bones.Count;                // Tell the CObj its transient ID into our flattened ordinal
//	if (oBoneT.name[0] == '+')
//		return;
//	if (oBoneT.name.Contains("Hand"))
//		return;
//	if (oBoneT.name.Contains("Foot"))
//		return;
//	int nFlattenedTreeOrdinal_Now = aFlatLookupArray_Bones.Count;
//	CGame._oBrowser_HACK.SendEvalToBrowser("AddData(" + aFlatLookupArray_Bones.Count + "," + nFlattenedTreeOrdinal_Parent + ",'P','" + oBoneT.name + "',0)");
//	aFlatLookupArray_Bones.Add(oBoneT);
//	int nChildren = oBoneT.childCount;
//	for (int nChild = 0; nChild < nChildren; nChild++) {
//		Transform oBoneChildT = oBoneT.GetChild(nChild);
//		Util_SendTree_Bones_RECURSIVE(oBoneChildT, nFlattenedTreeOrdinal_Now, ref aFlatLookupArray_Bones);
//	}
//}

    //public override string View_SaveProperties() {          //#DEV27: OBS?
    //    //=== Save all properties rendered by this view into a single string ===
    //    return _oObj.Save();            
    //}
    //public override void View_LoadProperties(string sSerializedObjects) {
    //    //=== Restore all properties rendered by this view from a single string ===
    //    _oObj.Load(sSerializedObjects);
    //}

    //public virtual string View_SaveProperties() { return ""; }      //#DEV27: OBS?
    //public virtual void View_LoadProperties(string sSerializedObjects) {}

#endregion