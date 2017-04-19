//using UnityEngine;

//public class CBClothEdit: CBMesh, uFlex.IFlexProcessor {		// CBClothEdit: Blender-based mesh that is cut and re-cut via User adjustment of cutting curves.
//    //---------------------------------------------------------------------------	SUB MESHES
//    //CBMesh				_oBMeshClothAtStartup;              // The 'startup cloth' that is never simulated.  It is used to reset the simulated cloth to its start position
//    //---------------------------------------------------------------------------	USER INTERFACE
//	CObject				_oObj;							// The multi-purpose CObject that stores CProp properties  to publicly define our object.  Provides client/server, GUI and scripting access to each of our 'super public' properties.
//	CUICanvas			_oCanvas;				// Canvas that stores our cloth-editing GUI
//    //---------------------------------------------------------------------------	FLEX
//	uFlex.FlexParticles _oFlexParticles;
//    //---------------------------------------------------------------------------	MISC
//	string				_sNameCloth;


//    public static CBClothEdit Create(CBodyBase oBodyBase, string sNameClothEdit, string sClothType, string sNameClothSrc) {
//        string sBlenderAccessString_ClothInCollection = ".aCloths['" + sNameClothEdit + "']";
//		//###WEAK19:!!: Class has a lot of duplicated code with CBCloth.  Split so we can keep cloth cutting separate from more complex CBCloth
//        CGame.gBL_SendCmd("CBody", oBodyBase._sBlenderInstancePath_CBodyBase + ".CreateCloth('" + sNameClothEdit + "', '" + sClothType + "', '" + sNameClothSrc + "', '')");      // Create the Blender-side entity to service our requests
//        //CGame.gBL_SendCmd("CBody", sBodyID + CBCloth.s_sNameClothEditSrc_HACK + ".UpdateCutterCurves()");
//        CGame.gBL_SendCmd("CBody", oBodyBase._sBlenderInstancePath_CBodyBase + sBlenderAccessString_ClothInCollection + ".CutClothWithCutterCurves()");
//        //CGame.gBL_SendCmd("CBody", oBodyBase._sBlenderInstancePath_CBodyBase + sBlenderAccessString_ClothInCollection + ".PrepareClothForGame()");		//###IMPROVE13:!!  Damn period before... make consistent!!
//        CBClothEdit oBClothEdit = (CBClothEdit)CBMesh.Create(null, oBodyBase, sBlenderAccessString_ClothInCollection + ".oMesh_3DD", typeof(CBClothEdit), false, sNameClothEdit);		// Obtain the simulated-part of the cloth that was created in call above
//		return oBClothEdit;
//	}

//	public override void OnDeserializeFromBlender(params object[] aExtraArgs) {
//		base.OnDeserializeFromBlender(aExtraArgs);

//		_sNameCloth = aExtraArgs[0] as string;			// First arg is sNameCloth
//        string sBlenderAccessString_ClothInCollection = ".aCloths['" + _sNameCloth + "']";
//		gameObject.name = _oBodyBase._sBodyPrefix + "-Cloth-" + _sNameCloth;

//        //=== Create the simulated part of the cloth ===
//        MeshFilter oMeshFilter = GetComponent<MeshFilter>();
//		MeshRenderer oMeshRend = GetComponent<MeshRenderer>();
//        oMeshRend.sharedMaterial = Resources.Load("Materials/BasicColors/TransWhite50") as Material;        //####SOON? Get mats!  ###F
//        //oMeshRend.sharedMaterial = Resources.Load("Materials/Test-2Sided") as Material;        //####SOON? Get mats!  ###F
//		_oMeshNow = oMeshFilter.sharedMesh;
//		_oMeshNow.MarkDynamic();                // Docs say "Call this before assigning vertices to get better performance when continually updating mesh"

//		//=== Create the 'cloth at startup' mesh.  It won't get simulated and is used to reset simulated cloth to its startup position ===
//		//###PROBLEM17: Makes UpdateBlenderVerts fail for some reason... because we pull from the same name twice I think.
//		//_oBMeshClothAtStartup = CBMesh.Create(null, _oBodyBase, ".oClothEdit.oMeshClothEdit", typeof(CBMesh));
//		//_oBMeshClothAtStartup.transform.SetParent(_oBodyBase.FindBone("chestUpper"));      // Reparent this 'backup' mesh to the chest bone so it rotates and moves with the body
//        //_oBMeshClothAtStartup.GetComponent<MeshRenderer>().enabled = false;
//		//_oBMeshClothAtStartup.gameObject.SetActive(false);      // De activate it so it takes no cycle.  It merely exists for backup purposes

//		//=== Create the Flex processor so we can latch on our changes inside Flex internal flow ===
//		uFlex.FlexProcessor oFlexProc = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexProcessor)) as uFlex.FlexProcessor;
//		oFlexProc._oFlexProcessor = this;

//		//=== Create the managing object and related hotspot ===
//		_oObj = new CObject(this, "Cloth Cutting", "Cloth Cutting");
//		_oObj.Event_PropertyValueChanged += Event_PropertyChangedValue;
//		for (int nCurve = 0; nCurve < 3; nCurve++) {
//			string sBlenderAccessString_Curve = string.Format("{0}{1}.aCurves[{2}].oObj", _oBodyBase._sBlenderInstancePath_CBodyBase, sBlenderAccessString_ClothInCollection, nCurve);
//			CPropGrpBlender oPropGrpBlender = new CPropGrpBlender(_oObj, "", sBlenderAccessString_Curve);
//		}

//		//=== Create Canvas for GUI for this mode ===
//		_oCanvas = CUICanvas.Create(transform);
//		_oCanvas.transform.position = new Vector3(0.31f, 1.35f, 0.13f);            //###WEAK11: Hardcoded panel placement in code?  Base on a node in a template instead??  ###IMPROVE: Autorotate?
//		_oCanvas.CreatePanel("Cloth Cutting", null, _oObj);

//		//=== Bring up the Flex functinality right off construction so we simulate right away ===
//		FlexObject_ClothEdit_Enable();
//	}

//	public void FlexObject_ClothEdit_Enable() {          // Create the Flex functionality when becomning active in game.
//		if (_oFlexParticles == null) { 
//			CGame.INSTANCE._oFlex.CreateFlexObject(gameObject, _oMeshNow, _oMeshNow, uFlex.FlexBodyType.Cloth, uFlex.FlexInteractionType.SelfCollideFiltered, CGame.INSTANCE.nMassCloth, Color.yellow);
//			_oFlexParticles = GetComponent<uFlex.FlexParticles>();
//			GetComponent<MeshRenderer>().enabled = true;				// Make sure the mesh is visible in the scene so player can interact with it.
//		} else { 
//			_oFlexParticles.gameObject.SetActive(true);					// Just re-activate if we were deactivated by FlexObject_Deactivate()
//		}
//    }

//	public void FlexObject_ClothEdit_Disable() {          // Destroy the Flex functionality to save processing power when unused.  This enables a ClothEdit to be modified / re-modified without going back to Blender all the time.
//		_oFlexParticles.gameObject.SetActive(false);			//###CHECK18: Keep de-activation instead of destruction??
//	}


//	//--------------------------------------------------------------------------	UTILITY
//	public void HideShowMeshes() {
//        GetComponent<MeshRenderer>().enabled = CGame.INSTANCE.ShowPresentation;
//        //_oBMeshClothAtStartup.GetComponent<MeshRenderer>().enabled = CGame.INSTANCE.ShowMeshStartup;
//        GetComponent<uFlex.FlexParticlesRenderer>().enabled = CGame.INSTANCE.ShowFlexParticles;
//    }


//    //---------------------------------------------------------------------------	Flex
//    public void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
//        //if (Input.GetKeyDown(KeyCode.F10)) {			//####TEMP			####OBS ####CLEAN
//        //    for (int nVert = 0; nVert < _oBMeshClothAtStartup._memVerts.L.Length; nVert++) {		// Copy each vert from the 'backup' mesh to this simulated cloth...
//        //        _oFlexParticles.m_particles[nVert].pos = _oBMeshClothAtStartup.transform.localToWorldMatrix.MultiplyPoint(_oBMeshClothAtStartup._memVerts.L[nVert]);	//... making sure to convert each vert from the backup mesh's local coordinates to the global coordinates used in PhysX
//        //        _oFlexParticles.m_velocities[nVert] = Vector3.zero;
//        //    }
//        //}
//    }

//	void Event_PropertyChangedValue(object sender, EventArgs_PropertyValueChanged oArgs) {      // Fired everytime user adjusts a property.
//		// 'Bake' the morphing mesh as per the player's morphing parameters into a 'MorphResult' mesh that can be serialized to Unity.  Matches Blender's CBodyBase.UpdateMorphResultMesh()
//		Debug.LogFormat("CBClothEdit updates on body'{0}' because property '{1}' changed to value {2}", _oBodyBase._sBodyPrefix, oArgs.PropertyName, oArgs.ValueNew);

//	}
//}
