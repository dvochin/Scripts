using UnityEngine;
	
public class CGameMorph_OBS : IObject, IHotSpotMgr  {
	public CObject              _oObj;					// The object containing public properties user can adjust
	public 	CHotSpot			_oHotSpot;              // Hotspot for the entire morph game mode
	public CBMesh               _oMeshMorph_TEMP;		// Static mesh version of body being morphed

	//---------------------------------------------------------------------------	INIT
	public CGameMorph_OBS() {
        Debug.Log("+++ Entering Cloth Cut Game Mode +++");

		_oObj = new CObject(this, 0, typeof(EMorphOps), "Morph Ops");
		_oObj.PropGroupBegin("", "", true);
		_oObj.PropAdd(EMorphOps.BreastSize,		"Breast Size",		1.0f, 0.5f, 2.5f, "", CProp.Local);
		_oObj.FinishInitialization();

		_oHotSpot = CHotSpot.CreateHotspot(this, null, "Body Morph", false, new Vector3(0, 0.10f, 0.20f), 2.0f);        //####TODO: Pos!

		//_oMeshMorph_TEMP = CBMesh.Create(null, null, "WomanA_Morph", "", "Client", "gBL_GetMesh", "'SkinInfo'", typeof(CBMesh));
		//CBMesh.Create(null, null, "WomanA_Face", "", "Client", "gBL_GetMesh", "'SkinInfo'", typeof(CBMesh));

		//=== Instantiate the requested hair and pin as child of the head bone ===
		string sNameHair = "HairW-TiedUp";
		GameObject oHairTemplateGO = Resources.Load("Models/Characters/Woman/Hair/" + sNameHair + "/" + sNameHair, typeof(GameObject)) as GameObject;	// Hair has name of folder and filename the same.	//###HACK: Path to hair, selection control, enumeration, etc
		GameObject.Instantiate(oHairTemplateGO);

		//CBCloth.Create(this, "FullShirt");
	}
	
	public void OnDestroy() {
		Debug.Log("--- Leaving Cloth Cut Game Mode ---");
	}
	
	//---------------------------------------------------------------------------	EVENTS
	public void OnUpdate() {
	}

	//--------------------------------------------------------------------------	IOBJECT INTERFACE
	public void OnPropSet_BreastSize(float nValueOld, float nValueNew) {
		Debug.Log("PropSet " + nValueNew.ToString());
		CGame.gBL_SendCmd("Breasts", "Breasts_ApplyOp('BodyA_Morph', 'WomanA', 'RESIZE', 'Nipple', 'Center', 'Wide', (" + nValueNew.ToString() + "," + nValueNew.ToString() + "," + nValueNew.ToString() + ",0), None)");
		_oMeshMorph_TEMP.UpdateVertsFromBlenderMesh(true);
	}

	public void OnPropSet_NeedReset(CProp oProp, float nValueOld, float nValueNew) {}		//###DESIGN!!! Damn this near-useless function getting a pain in the ass...

	//--------------------------------------------------------------------------	IHotspot interface
	public void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) { }
	public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {
		if (eHotSpotEvent == EHotSpotEvent.ContextMenu)
			_oHotSpot.WndPopup_Create(new CObject[] { _oObj });
	}
}
