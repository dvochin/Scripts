using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class CGameClothFit : IHotSpotMgr {			//###DESIGN!!!:  ###SOON: Important flaw with this class appears by recreating most of what CBody does to avoid its animation... Better to just use it and create a 'special mode' so it doesn't animate???

//	CBMesh			_oBBodyMorphing;				// The body mesh we morph in this mode.
//	CBCloth			_oBClothFit;
//	CBBodyColCloth	_oBBodyColCloth_Morphed_OBS;			// Our body collider in 'morphed mode': It will adjust its low-density collision mesh from the position of the related high-density verts of the body we morph in this game mode
//	public List<CHotSpot>	_aHotSpots = new List<CHotSpot>();						// Hotspots pertinent to each of our derived game modes.  Created / destroyed at begin / end
	
	public CGameClothFit() {
        Debug.Log("+++ Entering Cloth Fit Game Mode on character +++");

//		CGame.gBL_SendCmd("Client", "GameMode_ClothFit('" + G.C_NameBaseCharacter_HACK + "')");		// Tell Blender we're entering cloth fit mode so it copies the cut cloth into the fit cloth
//
//        _oBBodyMorphing    = CBMesh   .Create(null, null, G.C_NameBaseCharacter_HACK + G.C_NameSuffix_BodyMorph, "Client", "gBL_GetMesh", "'SkinInfo'", typeof(CBMesh));
//		_oBBodyColCloth_Morphed_OBS = CBBodyColCloth.Create(null, null, G.C_NameBaseCharacter_HACK);		//###DESIGN??? Important design issue with creating CBodyCol without body, as receiving bones requires node path!  Consider redesigning this class to use a CBody (and all its dependencies) in a 'special' non-animated mode??
//
//
//		return;			//###BROKEN?
//
//		_oBClothFit 		= CBCloth  .Create(null, G.C_NameBaseCharacter_HACK+G.C_NameSuffix_ClothFit);
//
//		//=== Obtain the list of breast morph operation hotspots and create them in Unity ===
//		CMemAlloc<byte> mem = new CMemAlloc<byte>();
//		int nSizeData = CGame.gBL_SendCmd_GetMemBuffer("Breasts", "Breasts_GetMorphList()", ref mem);
//		int i = 0;
//		while (i < nSizeData) {
//			string sNameMorph = CUtility.BlenderStream_ReadStringPascal(ref mem.L, ref i);
//			Vector3 vecMorphCenter = CUtility.ByteArray_ReadVector(ref mem.L, ref i);
//			Debug.Log("Morph " + sNameMorph + " = " + vecMorphCenter.ToString());
//			CHotSpot oHotSpot = CHotSpot.CreateHotspot(this, null, sNameMorph, false, vecMorphCenter);	//###BROKEN: Transform... what to pass in??
//			_aHotSpots.Add(oHotSpot);
//		}		
	}
	
	public void OnDestroy() {
		Debug.Log("--- Leaving Cloth Fit Game Mode ---");

//		_oBClothFit.gBL_UpdateBlenderVerts();					//###IMPORTANT: Before we leave cloth fit mode we upload our verts to Blender so it builds from our latest cloth fitting for other game modes.
//		CGame.gBL_SendCmd("Client", "GameMode_ClothFit_End()");		// Tell blender we're done with cloth fit so it can cleanup and smooth our simulated output
//		//_oBBodyMorphing.Destroy();				//###CHECK: Leak?
//		//_oBClothFit.Destroy();
//		//_oBBodyColCloth_Morphed_OBS.Destroy();
//		//foreach (CHotSpot oHotSpot in _aHotSpots)			// ReleaseGlobalHandles the hotspot game objects owned by this game mode to cleanup the scene before the next game mode
//		//	oHotSpot.Destroy();
	}
	
	public void OnUpdate() {
//		if (Input.GetKeyDown(KeyCode.KeypadEnter))		//###TEMP
//			_oBClothFit.gBL_UpdateBlenderVerts();
//		
//		//###BROKEN: _oBClothFit.OnSimulateBetweenPhysX23();			
//
//		//###BROKEN: CGame.INSTANCE.UpdatePhysX();
//
//		_oBClothFit.OnSimulatePost();
	}
	
	
	public void OnBodyMorphOperationApplied() {					// Sent by GUI when user modifies body.  We now update the character static mesh and clothing...
//		_oBBodyMorphing.gBL_UpdateClientVerts();
//		//###BROKEN _oBBodyColCloth_Morphed_OBS.BodyCol_Update();
	}	
	
	public void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) {			//###MOVE: To game mode??
		//###BROKEN: Now that we pass in gizmo...

		//string sPivot  = iGUICode_Root.INSTANCE.oBtnGrpPivot. optionList[iGUICode_Root.INSTANCE.oBtnGrpPivot. selectedIndex].text;
		//string sEffect = iGUICode_Root.INSTANCE.oBtnGrpEffect.optionList[iGUICode_Root.INSTANCE.oBtnGrpEffect.selectedIndex].text;
		//string sCmdType = "";
		//string sCmdValue = "";
		//string sCmdAxis = "None";
		//Vector3 v;

		//switch (eEditMode) {
		//	case EEditMode.Move:
		//		sCmdType = "TRANSLATION";
		//		v = CGame.s_quatRotOffset90degDown * oGizmoT.GetPositionChangeSinceStartup();		//###NOTE: Important conversion needed between Unity space (left-handed/Yup) and Blender space (right-handed/Zup)
		//		sCmdValue = "(" + -v.x + "," + v.y + "," + v.z + ",0)";		//###NOTE: Note the important inversion of X to convert between Unity's left-hand-rule and Blender right-hand-rule!
		//		break;
		//	case EEditMode.Rotate:
		//		sCmdType = "ROTATION";
		//		float nAngle;
		//		Vector3 vecAxis;
		//		oGizmoT.rotation.ToAngleAxis(out nAngle, out vecAxis);
		//		sCmdValue = (nAngle * Mathf.Deg2Rad).ToString();
		//		sCmdAxis = "(" + vecAxis.x + "," + vecAxis.z + "," + vecAxis.y + ")";	//###NOTE: Note the important inversion of X to convert between Unity's left-hand-rule and Blender right-hand-rule!
		//		break;
		//	case EEditMode.Scale:
		//		sCmdType = "RESIZE";
		//		v = oGizmoT.GetScaleChangeSinceStartup();
		//		v = new Vector3(v.x, v.z, v.y);					// As we're scaling we simply need to invert y and z to convert from y-up Unity space and z-up Blender space
		//		sCmdValue = "(" + v.x + "," + v.y + "," + v.z + ",0)";
		//		break;
		//}
		////Debug.Log(sCmdValue);
		//string sCmdFull = "Breasts_ApplyOp('" + sCmdType + "','" + oGizmoT.name + "','" + sPivot + "','" + sEffect + "'," + sCmdValue + "," + sCmdAxis + ")";
		//CGame.gBL_SendCmd("Breasts", sCmdFull);
		//CGame.INSTANCE._oGameClothFit.OnBodyMorphOperationApplied();				// Cloth fit game mode will now update the character static mesh and clothing...
	}
	public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) { }
}
