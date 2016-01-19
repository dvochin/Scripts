/*###DISCUSSION: Cloth cutting curves
=== NEXT ===

=== TODO ===

=== LATER ===

=== IMPROVE ===

=== NEEDS ===
- Intelligent 'curve points' with public menu properties
	- Who is parent?
		- Parent can be verts on source body! (e.g. nipple, top of shoulder, etc)
			- These 'special nodes' are present in the recipe and get their initial position set during init from morphing body
	- Which X,Y,Z exposed to parent objects
		- Each X,Y,Z can have 'offsets', min and max?
	- Each node has a 'type'
		- Curve point: 
		- Bezier point: Moves one of the bezier point of its parent (relationship tells)
		- Center point
- Properties of curves:
	- Mirrored accross X or not:

=== DESIGN ===
- Moving of actual points via hotspot an 'advanced feature'
	- 'Public properties' of curves extracted from curve point hierarchy and presented into a flat slider menu for easy consumption.
	- Different 'recipes' provide users a way to define different cloths like tank-tops, T-shirts, panties, dresses, etc

=== IDEAS ===
- Create early curves from Unity objects?
- Nodes that are based on source body verts are created as such (in Blender).  During init they get their initial position set to body vert position

=== QUESTIONS ===
- Where to store curve recipes? (Problem early on)
	- Could store into Unity propriorety files but need a lot of editor functionality for everything (e.g. point insertion, deletion, relationships, etc)
	- Could store as Blender nodes...  Parent-child relationship and custom properties.
		- Once Blender recipe is created all Unity has to do is load and move!
- Some settings would benefit from % of values instead of actual distance... e.g. distance between nipples, between nipples and shoulder point, etc.
- Center point: can be calculated from center of other nodes?

=== LEARNED ===

=== PROBLEMS ===
- What about center point?

=== PROBLEMS??? ===

=== WISHLIST ===

*/



using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
	
public class CCutterCurve : IHotSpotMgr {

	public ECurveTypes 		_eCurveType = ECurveTypes.Top;		//###TODO: Picked by GUI
	public string			_sCurveName = "SportBra";			//###OBS???
	
	public List<CHotSpot>	_aHotSpots = new List<CHotSpot>();						// Hotspots pertinent to each of our derived game modes.  Created / destroyed at begin / end

	CBody			_oBody;
	CBMesh			_oBMesh_Cutter;

	//---------------------------------------------------------------------------	INIT
	public CCutterCurve(CBody oBody, ECurveTypes eCurveType, string sCurveName) {
		_oBody = oBody;
		_eCurveType = eCurveType;
		_sCurveName = sCurveName;
		LoadCutterCurve();
	}
	
	public void OnDestroy() {
		_oBMesh_Cutter.OnDestroy();			//####CHECK
	}
	
	//---------------------------------------------------------------------------	EVENTS
	public void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) {			//###MOVE: To game mode??		//###TODO: Block hotspot only for move mode!
		switch (eEditMode) {
			case EEditMode.Move:
				int nCurvePt = Int32.Parse(oGizmo.gameObject.name);				//###BROKEN?  (Now passing gizmo, not hotspot!!)
				UpdateCurvePoint(nCurvePt, oGizmo.transform.position, eHotSpotOp == EHotSpotOp.Last);
				UpdateUnityCutterMesh();
				break;
		}
	}
	public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) { }


	//---------------------------------------------------------------------------	PUBLIC MEMBERS
	public void SetCurveType(ECurveTypes eCurveType) {		//###DESIGN!! What to do here?  Set GUI??
		_eCurveType = eCurveType;	
		LoadCutterCurve();
	}
	
	public void DoCutSingle() {
		CHotSpot oHotSpotCenter = _aHotSpots[0];				// Blender's ApplyCut() needs the coordinates of the cutter's central point (always kept out-of-band of other curve points as it doesn't go with the curve)
		Vector3 v = oHotSpotCenter.transform.position;
		CGame.gBL_SendCmd("Cut", "gBL_ClothCut_ApplyCut('" + _eCurveType + "'," + v.x + "," + v.y + "," + v.z + ")");
		Debug.Log("DoCutSingle()");
	}
	
	
	//---------------------------------------------------------------------------	UTILITY
	void UpdateUnityCutterMesh() {						// ReleaseGlobalHandles the highly-transient cutter mesh rendering the curve in Unity and request an updated mesh from Blender. (This is the 'thick curve' glued to the side of the cloth providing user feedback on where the cut will occur)
		//if (_oBMesh_Cutter != null)		//???
		//	_oBMesh_Cutter.Destroy();
		//_oBMesh_Cutter = CBMesh.Create(null, _oBody, _sBlenderInstancePath_CSoftBody + ".oMeshSoftBodyRim", typeof(CBSkinBaked));           // Retrieve the skinned softbody rim mesh Blender just created so we can pin softbody at runtime
		//_oBMesh_Cutter = CBMesh.Create(null, null, G.C_NamePrefix_CutterAsMesh + _eCurveType.ToString(), "Curve", "gBL_Curve_GetCutterAsMesh", "", typeof(CBMesh));
	}
	
	void UpdateCurvePoint(int nCurvePt, Vector3 v, bool bUpdateHotspotToAdjustedCurvePt) {
		if (nCurvePt == 0)			// If this is the first hotspot (the center) it is handled by Blender only during cutting time and does not affect the actual curve itself
			return;
		bool bIsXatZeroPt = (IsCurveSymmetryX() == false && (nCurvePt == 1 || nCurvePt == _aHotSpots.Count - 1));		// With non-symmetry curve (e.g. curve points are mirrored about X=0 like neck opening or dress bottom), make sure the first and last curve point are at X=0 so that there is no gap in the cutter as it is mirrored about x=0
		if (bIsXatZeroPt)
			v.x = 0;	

		string sCmd = "gBL_Curve_UpdateCurvePoint('" + _eCurveType + "'," + (nCurvePt-1).ToString() + "," + v.x + "," + v.y + "," + v.z + ")";		// Update the point and set flag to rebuild curve ###NOTE: Note the -1 on point so we retain zero-based for the 'real' curve points (not center)
		string sResult = CGame.gBL_SendCmd("Curve", sCmd);
		
		if (bUpdateHotspotToAdjustedCurvePt) {								// If this is the last operation we update the hotspot to the Blender-adjusted position of the curve point (fitted to cloth)
			string[] aStringParts = sResult.Split(',');				// gBL_Curve_UpdateCurvePoint() returns in its outgoing string the adjusted position of the point as it fits to cloth.  Set our hotspot to this position.
			if (bIsXatZeroPt == false)
				v.x = Single.Parse(aStringParts[0]);
			v.y = Single.Parse(aStringParts[1]);
			v.z = Single.Parse(aStringParts[2]);
			CGame.INSTANCE._oCursor.CancelEditing();			// We are setting position of hotspot manually.  Cancel editing so gizmo is destroyed and doesn't interfere visually with our manual move of hotspot
			_aHotSpots[nCurvePt].transform.position = v;
		}
	}		

	void ResetCurve() {				// ReleaseGlobalHandles all the hotspots that make up the curve definition.
		//###BROKEN foreach (CHotSpot oHotSpot in _aHotSpots)
		//	oHotSpot.Destroy();
		_aHotSpots.Clear();
	}		
	

	//---------------------------------------------------------------------------	LOAD / SAVE
	public bool LoadCutterCurve() {
		string sFilePath = GetFilePath(_eCurveType, _sCurveName);
		if (File.Exists(sFilePath) == false) 
			return false;
		FileStream oFile = new FileStream(sFilePath, FileMode.Open);
	    BinaryFormatter oBF = new BinaryFormatter();
		int nFileVersion 	= (int)oBF.Deserialize(oFile);
		int nCurvePts 		= (int)oBF.Deserialize(oFile);
		if (nFileVersion != G.C_FileVersion_CurveDefinition)
			throw new CException("Exception in SaveCurrentFile().  Unrecognized file version " + nFileVersion + ".  Can only read version " + G.C_FileVersion_CurveDefinition);
		
		ResetCurve();				// Reset / destroy the curve before the load

		bool bSymmetryX = _eCurveType == ECurveTypes.Side;			//###WEAK: Hardcoded concept of symmetry as applying only to side curve.  May need more symmetry curves later on
		CGame.gBL_SendCmd("Curve", "gBL_Curve_Create('" + _eCurveType + "'," + (nCurvePts-1).ToString() + "," + bSymmetryX + ")");		// Rebuild entire curve now that all points set ###NOTE: -1 on num points as first one is center! (not real curve point)
		
		//=== Deserialize the vectors that have been stored in the curve definition file and create hotspots for each one of these positions ===
		for (int nCurvePt = 0; nCurvePt < nCurvePts; nCurvePt++) {
			Vector3 v = CUtility.DeserializeVec(oFile);
			CHotSpot oHotSpot = CHotSpot.CreateHotspot(this, null, nCurvePt.ToString(), true, v);	//###BROKEN???  Transform... what to pass in??
			oHotSpot.transform.parent = GameObject.Find("(CGame)/(CurveHotSpots)").transform;		//###WEAK: Constants!
			_aHotSpots.Add(oHotSpot);
		}
		for (int nCurvePt = 1; nCurvePt < nCurvePts; nCurvePt++)			// Update all the hotspot from the adjusted position to cloth as calculated by Blender. (Done in separated loop because UpdateCurvePoint needs to have properly set # of points)
			UpdateCurvePoint(nCurvePt, _aHotSpots[nCurvePt].transform.position, true);
		
		oFile.Close();
		UpdateUnityCutterMesh();
		Debug.Log("LoadCutterCurve() loaded " + sFilePath);
		return true;
	}
	
	public void SaveCutterCurve() {
		string sFilePath = GetFilePath(_eCurveType, _sCurveName);
		FileStream oFile = new FileStream(sFilePath, FileMode.Create);		//###TEMP
	    BinaryFormatter oBF = new BinaryFormatter();
		oBF.Serialize(oFile, G.C_FileVersion_CurveDefinition);			// Prepend each curve definition file with the version number
		oBF.Serialize(oFile, _aHotSpots.Count);
		foreach (CHotSpot oHotSpot in _aHotSpots)			// ReleaseGlobalHandles the hotspot game objects owned by this game mode to cleanup the scene before the next game mode
			CUtility.Serialize(oFile, oHotSpot.transform.position);
		oFile.Close();
		Debug.Log("SaveCutterCurve() saved " + sFilePath);
	}
	
	public static string GetFolderPath(ECurveTypes eCurveType) { return Application.dataPath + "/Resources/CurveDef/Body2/" + eCurveType + "/"; }	// Folder path derived from body type, curve type, curve name  //###HACK! Body type!
	public static string GetFilePath(ECurveTypes eCurveType, string sCurveName) { return GetFolderPath(eCurveType) + sCurveName + ".CrvDef"; }
	public bool IsCurveSymmetryX() { return _eCurveType == ECurveTypes.Side; }		//###NOTE: Only the side curve/cutter is symmetryX at this point... all others are mirrored about x=0 (e.g. neck opening, dress bottom, etc)
}

public enum ECurveTypes {		// Cloth cutting curve types
	Top,
	Side,
	Bottom,
}
