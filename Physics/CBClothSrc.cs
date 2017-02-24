/*###DISCUSSION: ClothSrc
=== NEXT ===

=== TODO ===
- Load / Save in Blender of ClothSrc vert positions for body (under filename being hash of all body props?)
	- This info stored in file for body props?
- Cleanup... need CObject?
- Refix CCloth to pull from ClothSrc mesh before cut!

=== LATER ===
- Need this instance to represent / Flex simulated multiple bodysuits (e.g. bodysuit pants, bodysuit dress, etc)

=== IMPROVE ===

=== DESIGN ===
--- Top-level Design of end-to-end cloth making in EroticVR ---
- 1. Game Mode Body Morph: User adjusts body shape while all the clothsource / bodysuits are Flex-simulated to flow around body
- 2. Game Mode Cloth cutting: User removes extra fabric on clothsource / bodysuit to create the gametime cloth.
- 3. Game Mode Play: User enjoys gametime interaction and animations with custom-adjusted body and custom-cut cloth.

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===
- Update Blender verts can have mem leak, redo

=== PROBLEMS??? ===

=== WISHLIST ===

*/


/*###DISCUSSION: Cloth editing GUI		###DESIGN<18>: Keep here?  In another class?  Improve docs!
=== LAST ===
- This class needed???

=== NEXT ===
- Think of global 'design time' GUI and its underlying classes & mechanisms (e.g. actions for each mode, what to display in property editor, etc)
- Code simple property editor to edit one curve... then work on curve selection
- When easy cloth editing becomes possible improve Blender-side with angle + dist on seam points
- Load and save of cloth curve.
- Clothing recipes... on Blender side only (CCloth sub-class?)

=== TODO ===

=== LATER ===

=== IMPROVE ===

=== DESIGN ===
- Entire 'design time' is one top-level button that switches between play mode and design mode.
- Design time mode always has a top-level menu on the left of body with one 'design time mode' combobox selecting the design mode...
	- and a 'sub combobox' for what to do in that particular design mode.

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===
=== QUESTIONS ===

=== WISHLIST ===
- Need central editing functionality: A central game mode combobox followed by what to edit (e.g. which cloth) and what to sub-edit (e.g. which curve)
	- Detail pane fills up from CObject enumeration from there.
	- Q: What to do about 'commit' button?  How does that flow with top-level editing framework?
	- Q: How to select multiple clothing? (idea: have a slot for all possible clothing with default being nothing populated?)
	- Q: How to save the whole thing?


###LATEST<17>: UV cloth cutting
# BMesh boolean cut can fail sometimes... go to carve and store custom layers differently?
# Merge var names of old and new
# Need to adopt angle + distance on seams... with backside its own different length (and same angle)    
# Currently no base properties of CCloth... do we put those in for easier cloth editing?
    # Specialize CCloth for different recipes like CClothTop, CClothPants, etc?    
# Go for a class for CCurvePt??
# We currently scan seam chains for L/R... given our simplifying assumptions do we keep overhead of right side?
# Need to redo bodysuit to new body.  Redo UVs too.

#- Unity edits CProps of CCloth at the global level (for cloth types, curve types, meta props like nipple X/Y, bra strap pos / width, etc)
#- User can edit only one curve at a time in a parent / child with CCloth / CCurve.  User picks by radio button one only.
    #- User adjusts sliders and Unity renders the cutting curves onto bodysuit (and cuts when user presses 'Cut' button)
#- CCurve has derived superclasses such as CCurveNeck, CCurveLegs, CCurveBottom, CCurveArms, etc which contains its own CObject / CProps for Unity editing / cloth loading/saving
#- Have to remove the old single-side source mesh, cut mesh, etc and its behavior (duplicate before batch cut)
#- Folder positions of cloth stuff... a new parent node?

        ###RESUME<17>: Need angles and dist for the beziers, need to define and update in same function (called everytime user changes anything)
#- Points not deleted on back mesh!!
#- Side curve bezier tedious to adjust as it is dependent on incident angle... make based on that angle??
    #- Should have different lenght possible for each side of seam beziers?  (go for angle + length with diff lenght on each side??)
#- Need to triangulate final 3D mesh
#- Scan through and remove old crap

*/



using UnityEngine;

public class CBClothSrc: CBMesh, IObject, uFlex.IFlexProcessor {	// CBClothSrc: Blender-based mesh that is cloth-simulated by our Flex code during gameplay.  Contains a skinned and simulated part
    //---------------------------------------------------------------------------	SUB MESHES
    //CBMesh				_oBMeshClothAtStartup;              // The 'startup cloth' that is never simulated.  It is used to reset the simulated cloth to its start position
    //---------------------------------------------------------------------------	USER INTERFACE
	//CObject				_oObj;							// The multi-purpose CObject that stores CProp properties  to publicly define our object.  Provides client/server, GUI and scripting access to each of our 'super public' properties.
    //CHotSpot            _oHotSpot;                        // The hotspot object that will permit user to left/right click on us in the scene to move/rotate/scale us and invoke our context-sensitive menu.
    //---------------------------------------------------------------------------	FLEX
	uFlex.FlexParticles _oFlexParticles;

    //---------------------------------------------------------------------------	CLOTH EDITING
	CObjectBlender      _oObj;					// The Blender-implemented Object that exposes RTTI-like information for change Blender shape keys from Unity UI panels
	CUICanvas			_oCanvas;				// Canvas that stores our cloth-editing GUI


    public static CBClothSrc Create(CBodyBase oBodyBase, string sNameClothSrc) {
        //CGame.gBL_SendCmd("CBody", oBodyBase._sBlenderInstancePath_CBodyBase + ".oClothSrcSelected_HACK = CGlobals.cm_aClothSources['" + sNameClothSrc + "']");		//###HACK<18>!! Injecting a temp variable so a limitation of CMesh create below can be overcome (needing all meshes to be accessible from our CBodyBase isntance)
        CGame.gBL_SendCmd("CBody", oBodyBase._sBlenderInstancePath_CBodyBase + ".SelectClothSrc_HACK('" + sNameClothSrc + "')");		//###HACK<18>!! Injecting a temp variable so a limitation of CMesh create below can be overcome (needing all meshes to be accessible from our CBodyBase isntance)
        CBClothSrc oBClothSrc = (CBClothSrc)CBMesh.Create(null, oBodyBase, ".oClothSrcSelected_HACK", typeof(CBClothSrc), true);		// Obtain the simulated-part of the cloth that was created in call above.  Keep shared in Blender so we can upload our simulate vert positions
		return oBClothSrc;
	}

	public override void OnDeserializeFromBlender(params object[] aExtraArgs) {		//###DESIGN<17>: Not a natural way to wrapup the two meshes (simulated and skinned) by creating skinned in override function of simulated...
		base.OnDeserializeFromBlender(aExtraArgs);

		gameObject.name = _oBodyBase._sBodyPrefix + "-ClothSrc";		//###IMPROVE: Put node rename in CBMesh.Create()!

        //=== Create the simulated part of the cloth ===
        MeshFilter oMeshFilter = GetComponent<MeshFilter>();
		MeshRenderer oMeshRend = GetComponent<MeshRenderer>();
        oMeshRend.sharedMaterial = Resources.Load("Materials/BasicColors/TransWhite25") as Material;        //####SOON? Get mats!  ###F
        //oMeshRend.sharedMaterial = Resources.Load("Materials/Test-2Sided") as Material;        //####SOON? Get mats!  ###F
		_oMeshNow = oMeshFilter.sharedMesh;
		_oMeshNow.MarkDynamic();                // Docs say "Call this before assigning vertices to get better performance when continually updating mesh"

		//=== Create the 'cloth at startup' mesh.  It won't get simulated and is used to reset simulated cloth to its startup position ===
		//###PROBLEM<17>: Makes UpdateBlenderVerts fail for some reason... because we pull from the same name twice I think.
		//_oBMeshClothAtStartup = CBMesh.Create(null, _oBodyBase, ".oClothSrc.oMeshClothSrc", typeof(CBMesh));
		//_oBMeshClothAtStartup.transform.SetParent(_oBodyBase.FindBone("chestUpper"));      // Reparent this 'backup' mesh to the chest bone so it rotates and moves with the body
        //_oBMeshClothAtStartup.GetComponent<MeshRenderer>().enabled = false;
		//_oBMeshClothAtStartup.gameObject.SetActive(false);      // De activate it so it takes no cycle.  It merely exists for backup purposes

		//=== Create the Flex processor so we can latch on our changes inside Flex internal flow ===
		uFlex.FlexProcessor oFlexProc = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexProcessor)) as uFlex.FlexProcessor;
		oFlexProc._oFlexProcessor = this;

		////=== Create the managing object and related hotspot ===
		//_oObj = new CObject(this, 0, typeof(EFlexCloth), "Cloth " + gameObject.name);
		//_oObj.PropGroupBegin("", "", true);
		//_oObj.PropAdd(EFlexCloth.Tightness,     "Tightness",    1.0f, 0.01f, 2.5f, "");
		//_oObj.FinishInitialization();
		//_oHotSpot = CHotSpot.CreateHotspot(this, _oWatchBone, "Clothing", false, new Vector3(0, 0.22f, 0.04f));

		//=== Bring up the Flex functinality right off construction so we simulate right away ===
		FlexObject_ClothSrc_Enable();
	}

	public void FlexObject_ClothSrc_Enable() {          // Create the Flex functionality when becomning active in game.
		if (_oFlexParticles == null) { 
			CGame.INSTANCE._oFlex.CreateFlexObject(gameObject, _oMeshNow, _oMeshNow, uFlex.FlexBodyType.Cloth, uFlex.FlexInteractionType.SelfCollideFiltered, CGame.INSTANCE.nMassCloth, Color.yellow);
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
		_oFlexParticles.gameObject.SetActive(false);			//###CHECK<18>: Keep de-activation instead of destruction??
	}

	void UploadClothSrcToBlender() {				// Update Blender verts from our Flex-simulated verts.  Blender will need this to cut cloth that fit the body!
		for (int nVert = 0; nVert < _memVerts.L.Length; nVert++)			// Copy last position of simulated verts back to mesh
			_memVerts.L[nVert] = _oFlexParticles.m_particles[nVert].pos;    //... making sure to convert each vert from the backup mesh's local coordinates to the global coordinates used in PhysX
		UpdateVertsToBlenderMesh();				// Update Blender with the freshly simulated Mesh.  Cloth cutting will take place from this starting point.
	}

	//--------------------------------------------------------------------------	UTILITY
	public void HideShowMeshes() {
        GetComponent<MeshRenderer>().enabled = CGame.INSTANCE.ShowPresentation;
        //_oBMeshClothAtStartup.GetComponent<MeshRenderer>().enabled = CGame.INSTANCE.ShowMeshStartup;
        GetComponent<uFlex.FlexParticlesRenderer>().enabled = CGame.INSTANCE.ShowFlexParticles;
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


	public void ClothEdit_Start() {
		string sBlenderInstancePath_CClothEdit_HACK = _oBodyBase._sBlenderInstancePath_CBodyBase + ".oClothSrc.aDictSeamCurves['TopL'].oObj";		//###HACK<18>!!!: Have to get directory of curves, dual L/R??  Improve access string

		
		//NOW
			// WRONG! We must interact with CCloth not CClothSrc!  Get CCloth working again in mode 3, split the edit cloth out of this class and merge with CCloth??
		
		
		
		
		
		
			
		//=== Create the managing object and related hotspot ===
		_oObj = new CObjectBlender(this, sBlenderInstancePath_CClothEdit_HACK, _oBodyBase._nBodyID);
		_oObj.Event_PropertyValueChanged += Event_PropertyChangedValue;
		//_oHotSpot = CHotSpot.CreateHotspot(this, null, "Body Morphing", false, new Vector3(0, 0, 0));
		_oCanvas = CUICanvas.Create(transform);		//#################
		_oCanvas.transform.position = new Vector3(0.31f, 1.35f, 0.13f);            //###WEAK<11>: Hardcoded panel placement in code?  Base on a node in a template instead??  ###IMPROVE: Autorotate?
		CUtility.WndPopup_Create(_oCanvas, EWndPopupType.PropertyEditor, new CObject[] { _oObj }, "Cloth Editing");
	}

	void Event_PropertyChangedValue(object sender, EventArgs_PropertyValueChanged oArgs) {      // Fired everytime user adjusts a property.
		// 'Bake' the morphing mesh as per the player's morphing parameters into a 'MorphResult' mesh that can be serialized to Unity.  Matches Blender's CBodyBase.UpdateMorphResultMesh()
		Debug.LogFormat("CBClothSrc updates on body'{0}' because property '{1}' changed to value {2}", _oBodyBase._sBodyPrefix, oArgs.PropertyName, oArgs.ValueNew);

		//if (oArgs.Property._oObjectExtraFunctionality == null)
		//	oArgs.Property._oObjectExtraFunctionality = new CMorphChannel(this, oArgs.PropertyName);
		//CMorphChannel oMorphChannel = oArgs.Property._oObjectExtraFunctionality as CMorphChannel;
		//bool bMeshChanged = oMorphChannel.ApplyMorph(oArgs.ValueNew);
		//if (bMeshChanged) {
		//	_oMeshMorphResult._oMeshNow.vertices = _oMeshMorphResult._memVerts.L;       //###IMPROVE: to some helper function?
		//	_oMeshMorphResult.UpdateNormals();							// Morphing invalidates normals... update
		//	_bParticlePositionsUpdated = true;                          // Flag Flex to perform an update of its particle positions...
		//}
	}
}
