/*###DISCUSSION: Cloth

=== NEXT ===
- Better textures

- Now have full Flex with master/slave skinned-to-simulated!
    - Some of this caused by the unsynced timing?
- Clean up old codebase and link to Flex update frames!
- Clean up Blender codebase with old collider shit!

=== TODO ===
- Play with mass (e.g. during large body movement)

=== LATER ===

=== IMPROVE ===
- Redo param increasing during large movements??

=== DESIGN ===
- Continue with softbody two modes?  Simplify to one mode and have some kind of reset / stiffening during sharp movements?

=== IDEAS ===
- Reduce cloth clip-through with body by pulling verts of body under cloth a certain distance?

=== LEARNED ===

=== PROBLEMS ===
- Cloth cutting can be inverted... how do we make certain what side we keep??
- Blender cutter doens't clean up cuts... leaving many verts!
=== PROBLEMS??? ===

=== WISHLIST ===

*/

using UnityEngine;
using System.Collections.Generic;

public class CBCloth : CBMesh, IObject, IHotSpotMgr, IFlexProcessor {						// CBCloth: Blender-based mesh that is cloth-simulated by our PhysX code.
    //---------------------------------------------------------------------------	SUB MESHES
    CBMesh				_oBMeshClothAtStartup;              // The 'startup cloth' that is never simulated.  It is used to reset the simulated cloth to its start position
	CBSkinBaked			_oBSkinBaked_SkinnedPortion;		// The 'skinned portion' of our cloth.  Skinned alongside its owning body.  We use all its verts in a master-spring-slave Flex relationship to force these verts to stay very close to their original position on the body
    CPinnedParticles    _oPinnedParticles;              
    //---------------------------------------------------------------------------	USER INTERFACE
	CObject				_oObj;                              // The multi-purpose CObject that stores CProp properties  to publicly define our object.  Provides client/server, GUI and scripting access to each of our 'super public' properties.
    CHotSpot            _oHotSpot;                          // The hotspot object that will permit user to left/right click on us in the scene to move/rotate/scale us and invoke our context-sensitive menu.
    Transform           _oWatchBone;                        // Bone position we watch from our owning body to 'autotune' cloth simulation parameters so cloth stays on body during rapid movement (e.g. pose changing)
    //---------------------------------------------------------------------------	BLENDER ACCESS
    public string       _sBlenderInstancePath_CCloth;				// Blender access string to our instance (form our CBody instance)
	static string       s_sNameClothSrc_HACK;                   //####HACK!: To pass in name from static creation!
    //---------------------------------------------------------------------------	COMPONENT REFERENCE
    uFlex.FlexParticles _oFlexParticles;
    uFlex.FlexSprings   _oFlexSprings;
    //---------------------------------------------------------------------------	BACKUP STRUCTURES
    float[] _aSpringRestLengthsBAK;                 // Backup of spring rest lengths.  Used to perform non-destructive ratio adjustments


    public static CBCloth Create(CBody oBody, string sNameCloth, string sClothType, string sNameClothSrc, string sVertGrp_ClothSkinArea) {    // Static function override from CBMesh::Create() to route Blender request to Body Col module and deserialize its additional information for the local creation of a CBBodyColCloth
        string sBodyID = "CBody_GetBody(" + oBody._nBodyID.ToString() + ").";
        CBCloth.s_sNameClothSrc_HACK = "aCloths['" + sNameCloth + "']";
        CGame.gBL_SendCmd("CBody", sBodyID + "CreateCloth('" + sNameCloth + "', '" + sClothType + "', '" + sNameClothSrc + "', '" + sVertGrp_ClothSkinArea + "')");      // Create the Blender-side CCloth entity to service our requests
        //CGame.gBL_SendCmd("CBody", sBodyID + CBCloth.s_sNameClothSrc_HACK + ".UpdateCutterCurves()");
        //CGame.gBL_SendCmd("CBody", sBodyID + CBCloth.s_sNameClothSrc_HACK + ".CutClothWithCutterCurves()");
        CGame.gBL_SendCmd("CBody", sBodyID + CBCloth.s_sNameClothSrc_HACK + ".PrepareClothForGame()");
        CBCloth oBCloth = (CBCloth)CBMesh.Create(null, oBody, CBCloth.s_sNameClothSrc_HACK + ".oMeshClothSimulated", typeof(CBCloth));		// Obtain the simulated-part of the cloth that was created in call above
		//####IDEA: Modify static creation by first creating instance, stuffing it with custom data and feeding instance in Create to be filled in!
		return oBCloth;
	}

	public override void OnDeserializeFromBlender() {
		base.OnDeserializeFromBlender();

		_sBlenderInstancePath_CCloth = CBCloth.s_sNameClothSrc_HACK;

		//=== Create the skinned-portion of the cloth.  It will be responsible for driving Flex particles that heavily influence their corresponding particles in fully-simulated cloth mesh ===
		_oBSkinBaked_SkinnedPortion = (CBSkinBaked)CBSkinBaked.Create(null, _oBody, _sBlenderInstancePath_CCloth + ".oMeshClothSkinned", typeof(CBSkinBaked));
        _oBSkinBaked_SkinnedPortion.transform.SetParent(transform);
        _oBSkinBaked_SkinnedPortion._oSkinMeshRendNow.enabled = false;          // Skinned portion invisible to the user.  Only used to guide simulated portion

        //=== Receive the aMapPinnedParticles array Blender created to map the skinned verts to their pertinent simulated ones ===
        List<ushort> aMapPinnedParticles;
        CUtility.BlenderSerialize_GetSerializableCollection_USHORT("'CBody'", _oBody._sBlenderInstancePath_CBody + "." + _sBlenderInstancePath_CCloth + ".SerializeCollection_aMapPinnedParticles()", out aMapPinnedParticles);

        //=== Create the simulated part of the cloth ===
        MeshFilter oMeshFilter = GetComponent<MeshFilter>();
		MeshRenderer oMeshRend = GetComponent<MeshRenderer>();
        //oMeshRend.sharedMaterial = Resources.Load("Materials/BasicColors/TransWhite25") as Material;        //####SOON? Get mats!  ###F
        oMeshRend.sharedMaterial = Resources.Load("Materials/Test-2Sided") as Material;        //####SOON? Get mats!  ###F
        _oBSkinBaked_SkinnedPortion._oSkinMeshRendNow.sharedMaterial = oMeshRend.sharedMaterial;        // Skinned part has same material
		_oMeshNow = oMeshFilter.sharedMesh;
		_oMeshNow.MarkDynamic();                // Docs say "Call this before assigning vertices to get better performance when continually updating mesh"

		//=== Create the 'cloth at startup' mesh.  It won't get simulated and is used to reset simulated cloth to its startup position ===
		_oBMeshClothAtStartup = CBMesh.Create(null, _oBody, _sBlenderInstancePath_CCloth + ".oMeshClothSimulated", typeof(CBMesh));
		_oBMeshClothAtStartup.transform.SetParent(_oBody.FindBone("chest").transform);      // Reparent this 'backup' mesh to the chest bone so it rotates and moves with the body
        _oBMeshClothAtStartup.GetComponent<MeshRenderer>().enabled = false;
		//_oBMeshClothAtStartup.gameObject.SetActive(false);      // De activate it so it takes no cycle.  It merely exists for backup purposes

        //=== Create the Flex object for our simulated part ===
        CFlex.CreateFlexObject(gameObject, _oMeshNow, _oMeshNow, uFlex.FlexBodyType.Cloth, uFlex.FlexInteractionType.None, CGame.INSTANCE.nMassCloth, Color.yellow);
        uFlex.FlexProcessor oFlexProc = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexProcessor)) as uFlex.FlexProcessor;
        oFlexProc._oFlexProcessor = this;
        _oFlexParticles = GetComponent<uFlex.FlexParticles>();
        _oFlexSprings   = GetComponent<uFlex.FlexSprings>();

        //=== Create the managing object and related hotspot ===
        _oObj = new CObject(this, 0, typeof(EFlexCloth), "Cloth " + gameObject.name);        //###IMPROVE: Name of soft body to GUI
        _oObj.PropGroupBegin("", "", true);
        _oObj.PropAdd(EFlexCloth.Tightness,     "Tightness",    1.0f, 0.01f, 2.5f, "", CProp.Local);
        _oObj.PropAdd(EFlexCloth.Length,        "Length",       1.0f, 0.50f, 1.10f, "", CProp.Local);
        _oObj.PropAdd(EFlexCloth.ClothMass,     "Mass",         1.0f, 0.0001f, 1000.0f, "", CProp.Local);
        _oObj.FinishInitialization();
        _oWatchBone = _oBody.FindBone("chest");            //####HACK ####DESIGN: Assumes this cloth is a top!
        _oHotSpot = CHotSpot.CreateHotspot(this, _oWatchBone, "Clothing", false, new Vector3(0, 0.22f, 0.04f));     //###IMPROVE!!! Position offset that makes sense for that piece of clothing (from center of its verts?)

        //=== Backup the startup cloth arrays so we can adjust in a non-destructive way ===
        _aSpringRestLengthsBAK = new float[_oFlexSprings.m_springsCount];
        System.Array.Copy(_oFlexSprings.m_springRestLengths, _aSpringRestLengthsBAK, _oFlexSprings.m_springsCount);

        //=== Create the Flex-to-skinned-mesh component responsible to guide selected Flex particles to skinned-mesh positions ===
        _oPinnedParticles = CUtility.FindOrCreateComponent(gameObject, typeof(CPinnedParticles)) as CPinnedParticles;
        _oPinnedParticles.Initialize(ref aMapPinnedParticles, _oBSkinBaked_SkinnedPortion);
    }

    public override void OnDestroy() {
		base.OnDestroy();
	}

	public void Cloth_Reset() {
    }



    //--------------------------------------------------------------------------	UTILITY
    public void HideShowMeshes(bool bShowPresentation, bool bShowPhysxColliders, bool bShowMeshStartup, bool bShowPinningRims, bool bShowFlexSkinned, bool bShowFlexColliders, bool bShowFlexParticles) {
        GetComponent<MeshRenderer>().enabled = bShowPresentation;
        _oBMeshClothAtStartup.GetComponent<MeshRenderer>().enabled = bShowMeshStartup;
        _oBSkinBaked_SkinnedPortion._oSkinMeshRendNow.enabled = bShowPinningRims;
        GetComponent<uFlex.FlexParticlesRenderer>().enabled = bShowFlexParticles;
    }


	//--------------------------------------------------------------------------	IHotspot interface

	public void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) { }

	public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {		//###DESIGN? Currently an interface call... but if only GUI interface occurs through CObject just have cursor directly invoke the GUI_Create() method??
		if (eHotSpotEvent == EHotSpotEvent.ContextMenu)
			_oHotSpot.WndPopup_Create(_oBody, new CObject[] { _oObj });
	}

    public void OnPropSet_Tightness(float nValueOld, float nValueNew) {
        for (int nSpring = 0; nSpring < _oFlexSprings.m_springsCount - _oPinnedParticles._nNumMappingsSkinToSim; nSpring++)
            _oFlexSprings.m_springCoefficients[nSpring] = nValueNew;
        Debug.LogFormat("Cloth Tightness {0}", nValueNew);
    }

    public void OnPropSet_Length(float nValueOld, float nValueNew) {
        for (int nSpring = 0; nSpring < _oFlexSprings.m_springsCount - _oPinnedParticles._nNumMappingsSkinToSim; nSpring++)
            _oFlexSprings.m_springRestLengths[nSpring] = _aSpringRestLengthsBAK[nSpring] * nValueNew;
        Debug.LogFormat("Cloth Length {0}", nValueNew);
    }

    public void OnPropSet_ClothMass(float nValueOld, float nValueNew) {      //###OBS? Doesn't appear to do anything!!
        float nInvMassPerParticle = 1 / (nValueNew * _oFlexParticles.m_particlesCount - _oPinnedParticles._nNumMappingsSkinToSim);
        for (int nPar = 0; nPar < _oFlexParticles.m_particlesCount - _oPinnedParticles._nNumMappingsSkinToSim; nPar++)
            _oFlexParticles.m_particles[nPar].invMass = nInvMassPerParticle;
        Debug.LogFormat("Cloth Mass {0}", nValueNew);
    }

    //---------------------------------------------------------------------------	Flex
    public void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
        //=== Bake the baked part of our cloth.  We need its periphery verts to pin our simulated part of the cloth! ===
        _oPinnedParticles._oMeshSoftBodyPinnedParticles.Baking_UpdateBakedMesh();     // Bake the skinned portion of the mesh.  We need its verts to pin the 'pinned particles' which in turn move the 'moving particles' toward them via a spring we created in init ===
        _oPinnedParticles.UpdatePositionsOfPinnedParticles();

        if (Input.GetKeyDown(KeyCode.F10)) {			//####TEMP			####OBS ####CLEAN
            for (int nVert = 0; nVert < _oBMeshClothAtStartup._memVerts.L.Length; nVert++) {		// Copy each vert from the 'backup' mesh to this simulated cloth...
                _oFlexParticles.m_particles[nVert].pos = _oBMeshClothAtStartup.transform.localToWorldMatrix.MultiplyPoint(_oBMeshClothAtStartup._memVerts.L[nVert]);	//... making sure to convert each vert from the backup mesh's local coordinates to the global coordinates used in PhysX
                _oFlexParticles.m_velocities[nVert] = Vector3.zero;
            }
        }
    }
}
