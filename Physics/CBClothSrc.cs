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

using UnityEngine;

public class CBClothSrc: CBMesh, IObject, IFlexProcessor {	// CBClothSrc: Blender-based mesh that is cloth-simulated by our Flex code during gameplay.  Contains a skinned and simulated part
    //---------------------------------------------------------------------------	SUB MESHES
    CBMesh				_oBMeshClothAtStartup;              // The 'startup cloth' that is never simulated.  It is used to reset the simulated cloth to its start position
    //---------------------------------------------------------------------------	USER INTERFACE
	//CObject				_oObj;							// The multi-purpose CObject that stores CProp properties  to publicly define our object.  Provides client/server, GUI and scripting access to each of our 'super public' properties.
    //CHotSpot            _oHotSpot;                        // The hotspot object that will permit user to left/right click on us in the scene to move/rotate/scale us and invoke our context-sensitive menu.
    //---------------------------------------------------------------------------	BLENDER ACCESS
    uFlex.FlexParticles _oFlexParticles;


    public static CBClothSrc Create(CBodyBase oBodyBase) {
		string sNameClothSrc = "BodySuit";			//###IMPROVE<17>: Always this string?  Only one cloth source?  ###DESIGN<17>: Gender specific!
		string sBodyID = "CBodyBase_GetBodyBase(" + oBodyBase._nBodyID.ToString() + ").";
        CGame.gBL_SendCmd("CBody", sBodyID + "CreateClothSrc('" + sNameClothSrc + "')");
        CBClothSrc oBClothSrc = (CBClothSrc)CBMesh.Create(null, oBodyBase, ".oClothSrc.oMeshClothSrc", typeof(CBClothSrc), true);		// Obtain the simulated-part of the cloth that was created in call above.  Keep shared in Blender so we can upload our simulate vert positions
		return oBClothSrc;
	}

	public override void OnDeserializeFromBlender() {		//###DESIGN<17>: Not a natural way to wrapup the two meshes (simulated and skinned) by creating skinned in override function of simulated...
		base.OnDeserializeFromBlender();

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
		FlexObject_Create();
	}

	public void FlexObject_Create() {          // Create the Flex functionality when becomning active in game.
		if (_oFlexParticles != null)
			return;
        CFlex.CreateFlexObject(gameObject, _oMeshNow, _oMeshNow, uFlex.FlexBodyType.Cloth, uFlex.FlexInteractionType.None, CGame.INSTANCE.nMassCloth, Color.yellow);
        _oFlexParticles = GetComponent<uFlex.FlexParticles>();
    }

	public void FlexObject_Destroy() {          // Destroy the Flex functionality to save processing power when unused.  This enables a ClothSrc to be modified / re-modified without going back to Blender all the time.
		for (int nVert = 0; nVert < _memVerts.L.Length; nVert++)        // Copy last position of simulated verts back to mesh
			_memVerts.L[nVert] = _oFlexParticles.m_particles[nVert].pos;    //... making sure to convert each vert from the backup mesh's local coordinates to the global coordinates used in PhysX
		UpdateVertsToBlenderMesh();				// Update Blender with the freshly simulated Mesh.  Cloth cutting will take place from this starting point.
		if (_oFlexParticles != null) {
			Destroy(_oFlexParticles);
			_oFlexParticles = null;
		}
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
}
