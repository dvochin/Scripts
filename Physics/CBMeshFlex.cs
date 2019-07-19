using UnityEngine;

public class CBMeshFlex: CBMesh, uFlex.IFlexProcessor {		// CBMeshFlex: Simple Flex-simulated mesh.  Used by ClothSrc and ClothEdit to render and simulate simple Flex-based meshes.
    //---------------------------------------------------------------------------	SUB MESHES
    //CBMesh				_oBMeshClothAtStartup;              // The 'startup cloth' that is never simulated.  It is used to reset the simulated cloth to its start position
    //---------------------------------------------------------------------------	USER INTERFACE
	//CObj				_oObj;							// The multi-purpose CObj that stores CObj properties  to publicly define our object.  Provides client/server, GUI and scripting access to each of our 'super public' properties.
    //CHotSpot            _oHotSpot;                        // The hotspot object that will permit user to left/right click on us in the scene to move/rotate/scale us and invoke our context-sensitive menu.
    //---------------------------------------------------------------------------	FLEX
	uFlex.FlexParticles _oFlexParticles;
    //---------------------------------------------------------------------------	CLOTH EDITING


    public static CBMeshFlex CreateForClothSrc(CBodyBase oBodyBase, string sNameClothSrc) {
        //CGame.gBL_SendCmd("CBody", oBodyBase._sBlenderInstancePath_CBodyBase + ".oClothSrcSelected_HACK = CGlobals.cm_aClothSources['" + sNameClothSrc + "']");		//###HACK18:!! Injecting a temp variable so a limitation of CMesh create below can be overcome (needing all meshes to be accessible from our CBodyBase isntance)
        CGame.gBL_SendCmd("CBody", oBodyBase._sBlenderInstancePath_CBodyBase + ".SelectClothSrc_HACK('" + sNameClothSrc + "')");		//###HACK18:!! Injecting a temp variable so a limitation of CMesh create below can be overcome (needing all meshes to be accessible from our CBodyBase isntance)
        CBMeshFlex oBMeshFlex = (CBMeshFlex)CBMesh.Create(null, oBodyBase, ".oClothSrcSelected_HACK", typeof(CBMeshFlex), true);		// Obtain the simulated-part of the cloth that was created in call above.  Keep shared in Blender so we can upload our simulate vert positions
		oBMeshFlex.gameObject.name = oBodyBase._sBodyPrefix + "-ClothSrc";
		return oBMeshFlex;
	}

    public static CBMeshFlex CreateForClothEdit(CBodyBase oBodyBase, string sNameClothEdit) {
        string sBlenderAccessString_ClothInCollection = ".aCloths['" + sNameClothEdit + "']";
		//###WEAK19:!!: Class has a lot of duplicated code with CBCloth.  Split so we can keep cloth cutting separate from more complex CBCloth
        //CGame.gBL_SendCmd("CBody", oBodyBase._sBlenderInstancePath_CBodyBase + ".CreateCloth('" + sNameClothEdit + "', '" + sClothType + "', '" + sNameClothSrc + "', '')");      // Create the Blender-side entity to service our requests
        //CGame.gBL_SendCmd("CBody", sBodyID + CBCloth.s_sNameClothEditSrc_HACK + ".UpdateCutterCurves()");
		//###TODO19: Need to load cutting properties before cutting... this cut at init is only good to cut Blender cutting defaults!
        CGame.gBL_SendCmd("CBody", oBodyBase._sBlenderInstancePath_CBodyBase + sBlenderAccessString_ClothInCollection + ".CutClothWithCutterCurves()");
        //CGame.gBL_SendCmd("CBody", oBodyBase._sBlenderInstancePath_CBodyBase + sBlenderAccessString_ClothInCollection + ".PrepareClothForGame()");		//###IMPROVE13:!!  Damn period before... make consistent!!
        CBMeshFlex oBMeshFlex = (CBMeshFlex)CBMesh.Create(null, oBodyBase, sBlenderAccessString_ClothInCollection + ".oMesh_3DD", typeof(CBMeshFlex), false, sNameClothEdit);		// Obtain the simulated-part of the cloth that was created in call above
		oBMeshFlex.gameObject.name = oBodyBase._sBodyPrefix + "-ClothEdit";

		return oBMeshFlex;
	}

	public override void OnDeserializeFromBlender(params object[] aExtraArgs) {		//###DESIGN17: Not a natural way to wrapup the two meshes (simulated and skinned) by creating skinned in override function of simulated...
		base.OnDeserializeFromBlender(aExtraArgs);

        //=== Create the simulated part of the cloth ===
        MeshFilter oMeshFilter = GetComponent<MeshFilter>();
		MeshRenderer oMeshRend = GetComponent<MeshRenderer>();
        oMeshRend.sharedMaterial = Resources.Load("Materials/BasicColors/TransWhite25") as Material;        //####SOON? Get mats!  ###F
        //oMeshRend.sharedMaterial = Resources.Load("Materials/Test-2Sided") as Material;        //####SOON? Get mats!  ###F
		_oMeshNow = oMeshFilter.sharedMesh;
		_oMeshNow.MarkDynamic();                // Docs say "Call this before assigning vertices to get better performance when continually updating mesh"

		//=== Create the 'cloth at startup' mesh.  It won't get simulated and is used to reset simulated cloth to its startup position ===
		//###PROBLEM17: Makes UpdateBlenderVerts fail for some reason... because we pull from the same name twice I think.
		//_oBMeshClothAtStartup = CBMesh.Create(null, _oBodyBase, ".oClothSrc.oMeshClothSrc", typeof(CBMesh));
		//_oBMeshClothAtStartup.transform.SetParent(_oBodyBase.FindBone("chestUpper"));      // Reparent this 'backup' mesh to the chest bone so it rotates and moves with the body
        //_oBMeshClothAtStartup.GetComponent<MeshRenderer>().enabled = false;
		//_oBMeshClothAtStartup.gameObject.SetActive(false);      // De activate it so it takes no cycle.  It merely exists for backup purposes

		//=== Create the Flex processor so we can latch on our changes inside Flex internal flow ===
		uFlex.FlexProcessor oFlexProc = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexProcessor)) as uFlex.FlexProcessor;
		oFlexProc._oFlexProcessor = this;

		//=== Bring up the Flex functinality right off construction so we simulate right away ===
		FlexObject_ClothSrc_Enable();
	}

	public void FlexObject_ClothSrc_Enable() {          // Create the Flex functionality when becomning active in game.
		if (_oFlexParticles == null) { 
			//###BROKEN: CGame._oFlex.CreateFlexObject(gameObject, _oMeshNow, _oMeshNow, uFlex.FlexBodyType.Cloth, uFlex.FlexInteractionType.SelfCollideFiltered, CGame.nMassCloth, Color.yellow);
			_oFlexParticles = GetComponent<uFlex.FlexParticles>();
			GetComponent<MeshRenderer>().enabled = true;				// Make sure the mesh is visible in the scene so player can interact with it.
		} else { 
			_oFlexParticles.gameObject.SetActive(true);					// Just re-activate if we were deactivated by FlexObject_Deactivate()
		}
    }

	public void FlexObject_ClothSrc_Disable() {          // Destroy the Flex functionality to save processing power when unused.  This enables a ClothSrc to be modified / re-modified without going back to Blender all the time.
		UploadClothSrcToBlender();
		//if (_oFlexParticles != null) {
		//	Destroy(_oFlexParticles);
		//	_oFlexParticles = null;
		//}
		_oFlexParticles.gameObject.SetActive(false);			//###CHECK18: Keep de-activation instead of destruction??
	}

	void UploadClothSrcToBlender() {				// Update Blender verts from our Flex-simulated verts.  Blender will need this to cut cloth that fit the body!
		for (int nVert = 0; nVert < _memVerts.L.Length; nVert++)			// Copy last position of simulated verts back to mesh
			_memVerts.L[nVert] = _oFlexParticles.m_particles[nVert].pos;    //... making sure to convert each vert from the backup mesh's local coordinates to the global coordinates used in PhysX
		UpdateVertsToBlenderMesh();				// Update Blender with the freshly simulated Mesh.  Cloth cutting will take place from this starting point.
	}

	//--------------------------------------------------------------------------	UTILITY
	public void HideShowMeshes() {
        GetComponent<MeshRenderer>().enabled = CGame.ShowPresentation;
        //_oBMeshClothAtStartup.GetComponent<MeshRenderer>().enabled = CGame.ShowMeshStartup;
        GetComponent<uFlex.FlexParticlesRenderer>().enabled = CGame.ShowFlexParticles;
    }


    //---------------------------------------------------------------------------	Flex
    public void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
        //if (Input.GetKeyDown(KeyCode.F10)) {			//####TEMP			####OBS ####CLEAN
        //    for (int nVert = 0; nVert < _oBMeshClothAtStartup._memVerts.L.Length; nVert++) {		// Copy each vert from the 'backup' mesh to this simulated cloth...
        //        _oFlexParticles.m_particles[nVert].pos = _oBMeshClothAtStartup.transform.localToWorldMatrix.MultiplyPoint(_oBMeshClothAtStartup._memVerts.L[nVert]);	//... making sure to convert each vert from the backup mesh's local coordinates to the global coordinates used in PhysX
        //        _oFlexParticles.m_velocities[nVert] = Vector3.zero;
        //    }
        //}
    }

	void Event_PropertyChangedValue(object sender, EventArgs_PropertyValueChanged oArgs) {      // Fired everytime user adjusts a property.
		// 'Bake' the morphing mesh as per the player's morphing parameters into a 'MorphResult' mesh that can be serialized to Unity.  Matches Blender's CBodyBase.UpdateMorphResultMesh()
		Debug.LogFormat("CBMeshFlex updates on body'{0}' because property '{1}' changed to value {2}", _oBodyBase._sBodyPrefix, oArgs.CObj._sNameInCodebase , oArgs.CObj._nValue);

		//if (oArgs.Property._oObjectExtraFunctionality == null)
		//	oArgs.Property._oObjectExtraFunctionality = new CMorphChannel(this, oArgs.PropertyName);
		//CMorphChannel oMorphChannel = oArgs.Property._oObjectExtraFunctionality as CMorphChannel;
		//bool bMeshChanged = oMorphChannel.ApplyMorph(oArgs.CObj._nValue);
		//if (bMeshChanged) {
		//	_oSkinMeshMorph._oMeshNow.vertices = _oSkinMeshMorph._memVerts.L;       //###IMPROVE: to some helper function?
		//	_oSkinMeshMorph.UpdateNormals();							// Morphing invalidates normals... update
		//	_bParticlePositionsUpdated = true;                          // Flag Flex to perform an update of its particle positions...
		//}
	}
}
