/*###DISCUSSION: Soft Body
------ Just got better visualization...
- Now onto create/destroy of sb... bone problem or Blender problem?
    - Revisit proper cleanup between the two apps!








NOW:
- Vagina mostly formed in blender... although we have a problem with disjointed verts in vagina backplate!  WTF??
- Rim verts not linking... because of cap join!
- For backplate to work we need more intermediate verts!


- Finally can teleport... except when game mode play starts!
    - Rethink the game modes carefully... integrate the teleport functionality as a new game mode?
- Early version of making Flex synchronous...
    - But do we do everything in Update() and reduce its frame rate??  Think carefully!


=== NEXT ===
- Big decision in regards to needing dual skinned reference points (all particles versus just rim ones)
    - Possible to not need fully skinned particles by increasing sb stiffness to max and disabling collisions?

- Add hotspot and options
- Much softer breasts!
- FETCH OTHER LEARNED?

=== TODO ===
- Can't intialize twice... keep the 'dual mode'?
- Totally have to clean up the old crappy collider shit from Blender, breasts and penis!
=== LATER ===

=== IMPROVE ===

=== DESIGN ===
- Softbody particles repel too far!  What to do???
    - Could push the visible mesh the rest of the way but problems with finely-detailed areas like nipple and penis tip...
        - Handle these areas differently?
    -+++IDEA! Have an intermediate 'bone mesh' at collider level that 'skins' the visible mesh!!!
        - 1. Blender constructs a 'pulled simplified collision mesh' from the visible soft body mesh. (detail smoothed out or entire mesh re-meshed)
        - 2. Flex constructs its particles & springs from this simplified mesh that collides against particles further than we'd like.
        - 3. Blender recieves this collision mesh to skin the visible mesh to it.
        - 4. At each frame the resultant 'simplified Flex softbody mesh' (itself skinned from Flex shapes) is the base mesh to skin the visible mesh.
        - Q: Can avoid 2 layers of skinning (bridging them for 1 skinning?)

=== IDEAS ===
- We need to create a 'Flex Body Collider' mesh in Blender that has chunks removed from it as we remove soft bodies...
    - This mesh can assist creation of Vagina collision mesh?

=== LEARNED ===
- Skinned body does not appear to be able to use the (awesome) Adhesion!  (Fortunately soft body can!)
    - Might have to have another layer of particles to stick cloth to body??
- Cloth stickiness to SB depends only on 'Dynamic Friction' and 'Particle Friction' (Adhesion not needed?)
- How to freeze PhysX bones: iterate through all actors and set them kinematic!

=== PROBLEMS ===
- BUG with SB pinning and moving pose root!  WTF!!  (Check skinned rim mesh)

=== PROBLEMS??? ===

=== WISHLIST ===

*/




using UnityEngine;
using System;
using System.Collections.Generic;


public class CBSoft : CBMesh, IObject, IHotSpotMgr, IFlexProcessor {                    //####DEV ####DESIGN: Based on CBMesh or CBSkin??
                                                                                        // Manages a single soft body object send to Flex implementation for soft body simulation.  These 'body parts' (such as breasts, penis, vagina) 
                                                                                        //... are conneted to the main body skinned mesh via _oMeshRimBaked which pins this softbody's tetraverts to those skinned from the main body

    public int _VertToShowSkinningBones_HACK = -1;

	//---------------------------------------------------------------------------	MEMBERS
	[HideInInspector]	public 	CObject				_oObj;							// The multi-purpose CObject that stores CProp properties  to publicly define our object.  Provides client/server, GUI and scripting access to each of our 'super public' properties.
	[HideInInspector]	public	CBSkinBaked			_oMeshSoftBodyRim;					// The skinned 'rim mesh' that is baked everyframe.  Contains rim and tetraverts.  Rim is to adjust normals at softbody mesh boundary and the tetraverts in this mesh are to 'pin' our softbody tetraverts to the skinned body (so softbody doesn't go 'flying off')
	
	[HideInInspector]	public	List<ushort>		_aMapRimVerts2Verts = new List<ushort>();		// Collection of mapping between our verts and the verts of our BodyRim.  Used to set softbody mesh rim verts and normals to their skinned-equivalent
	[HideInInspector]	public	List<ushort>		_aMapRimVerts2SourceVerts;		// Map of flattened rim vert IDs to source vert IDs.  Allows Unity to reset rim vert normals messed-up by capping to default normal for seamless rendering
	
	//---------------------------------------------------------------------------	Flex-related properties sent during BSoft_Init()
	[HideInInspector]	public	string				_sNameSoftBody;					// The name of our 'detached softbody' in Blender.  ('BreastL', 'BreastR', 'Penis', 'VaginaL', 'VaginaR') from a substring of our class name.  Must match Blender!!
	[HideInInspector]	public  int					_SoftBodyDetailLevel;			// Detail level of the associated Flex tetramesh... a range between 20 (low) and 50 (very high) is reasonable  ###CLEAN
	[HideInInspector]	public	EColGroups			_eColGroup;                     // The Flex collider group for this softbody.  Used to properly determine what this softbody collides with...###CLEAN
    [HideInInspector]	public	float				_nRangeTetraPinHunt = 0.035f;	//###OBS??? The maximum distance between the rim mesh and the tetraverts generated by Flex.  Determins which softbody tetraverts are 'pinned' to the skinned body

	//---------------------------------------------------------------------------	MISC
	[HideInInspector]	public	string				_sBlenderInstancePath_CSoftBody;				// The Blender instance path where our corresponding CSoftBody is access from CBody's Blender instance
	[HideInInspector]	public	string				_sBlenderInstancePath_CSoftBody_FullyQualfied;	// The Blender instance path where our corresponding CSoftBody is access from CBody's Blender instance (Fully qualified (includes CBody access string)
	[HideInInspector]	public	CBMesh				_oMesh_Unity2Blender;							// The Unity2Blender mesh we use to pass meshes from Unity to Blender for processing there (e.g. Softbody tetramesh skinning & pinning)

    CBMesh _oMeshFlexCollider;                           // The 'collision' mesh fed to Flex.  It as a 'shrunken version' of the appearance mesh _oMeshNow by half the Flex collision margin so that the visible mesh appears to collide with other particles much closer than if collision mesh was rendered to the user.  (Created by Blender by a 'shrink' operation)
    CHotSpot _oHotSpot;                          // The hotspot object that will permit user to left/right click on us in the scene to move/rotate/scale us and invoke our context-sensitive menu.
    uFlex.FlexParticles     _oFlexParticles;
    uFlex.FlexShapeMatching _oFlexShapeMatching;
    uFlex.FlexParticlesRenderer _oFlexParticlesRenderer;
    Mesh _oMeshFlexGenerated = new Mesh();
    Vector3[] _aFlexParticlesAtStart;
    Transform _oBoneAnchor;                     // The bone this softbody 'anchors to' = Resets to the world-space position / rotation during reset
    static string _sNameBoneAnchor_HACK;        // Horrible hack method of passing bone name to class instance... forced by CBMesh calling init code too early.  ###IMPROVE!
    bool _bSoftBodyInReset;              // When true soft body will be reset at next frame.  Used during pose loading.
    SkinnedMeshRenderer _oFlexGeneratedSMR;
    CFlexSkinnedSpringDriver_OBSOLETE _oFlexSkinnedSpringDriver;
    Vector3[] _aShapeRestPosOrig;


    //---------------------------------------------------------------------------	INIT

    public CBSoft() {                           // Setup the default arguments... usually overriden by our derived class   //###BUG??? Why are these settings not overriding those in instanced node???
		_nRangeTetraPinHunt = CGame.INSTANCE.particleSpacing * 1.0f;       //###TUNE: Make relative to all-important Flex particle size!
	}

	public static CBSoft Create(CBody oBody, Type oTypeBMesh, string sNameBoneAnchor_HACK) { 
		string sNameSoftBody = oTypeBMesh.Name.Substring(1);                            // Obtain the name of our detached body part ('Breasts', 'Penis', 'Vagina') from a substring of our class name.  Must match Blender!!  ###WEAK?
        _sNameBoneAnchor_HACK = sNameBoneAnchor_HACK;
        float nSoftBodyFlexColliderShrinkDist = CGame.INSTANCE.particleSpacing * CGame.INSTANCE.nSoftBodyFlexColliderShrinkDist;
        CGame.gBL_SendCmd("CBody", "CBody_GetBody(" + oBody._nBodyID.ToString() + ").CreateSoftBody('" + sNameSoftBody + "', " + nSoftBodyFlexColliderShrinkDist.ToString() + ")");		// Separate the softbody from the source body.
		CBSoft oBSoft = (CBSoft)CBMesh.Create(null, oBody, "aSoftBodies['" + sNameSoftBody + "'].oMeshSoftBody", oTypeBMesh);		// Create the softbody mesh from the just-created Blender mesh.
		return oBSoft;
	}

	public override void OnDeserializeFromBlender() {
		base.OnDeserializeFromBlender();

		_sNameSoftBody = GetType().Name.Substring(1);                            // Obtain the name of our detached body part ('Breasts', 'Penis', 'Vagina') from a substring of our class name.  Must match Blender!!  ###WEAK?
		_sBlenderInstancePath_CSoftBody = "aSoftBodies['" + _sNameSoftBody + "']";							// Simplify access to Blender CSoftBody instance
		_sBlenderInstancePath_CSoftBody_FullyQualfied = _oBody._sBlenderInstancePath_CBody + "." + _sBlenderInstancePath_CSoftBody; // Simplify access to fully-qualified Blender CSoftBody instance (from CBody instance)
        _oBoneAnchor = _oBody.FindBone(_sNameBoneAnchor_HACK);

        if (GetComponent<Collider>() != null)
			Destroy(GetComponent<Collider>());                      //###LEARN: Hugely expensive mesh collider created by the above lines... turn it off!

		//=== Set bounds to infinite so our dynamically-created mesh never has to recalculate bounds ===
		_oMeshNow.bounds = CGame._oBoundsInfinite;          //####IMPROVE: This can hurt performance ####OPT!!
		_oMeshNow.MarkDynamic();        // Docs say "Call this before assigning vertices to get better performance when continually updating mesh"

		//=== Create the collision mesh from Blender ===
		_oMeshFlexCollider = CBMesh.Create(null, _oBody, _sBlenderInstancePath_CSoftBody + ".oMeshFlexCollider", typeof(CBMesh));       // Also obtain the Unity2Blender mesh call above created.
        _oMeshFlexCollider.GetComponent<MeshRenderer>().enabled = false;      // Collider does not render... only for Flex definition!
        _oMeshFlexCollider.transform.SetParent(transform);

        //=== Create the Unity2Blender mesh so we can pass tetraverts to Blender for processing there ===
        _oMesh_Unity2Blender = CBMesh.Create(null, _oBody, _sBlenderInstancePath_CSoftBody + ".oMeshUnity2Blender", typeof(CBMesh), true);       // Also obtain the Unity2Blender mesh call above created.    // Keep link to Blender mesh open so we can upload our verts        //###IMPROVE: When/where to release??
        _oMesh_Unity2Blender.transform.SetParent(transform);
    }



    public override void OnDestroy() {
		Debug.Log("Destroy CBSoft " + gameObject.name);
		base.OnDestroy();
	}


    public virtual void OnChangeGameMode(EGameModes eGameModeNew, EGameModes eGameModeOld) {

		switch (eGameModeNew) { 
			case EGameModes.Play:
                //=== Call our C++ side to construct the solid tetra mesh.  We need that to assign tetrapins ===		//###DESIGN!: Major design problem between cutter sent here... can cut cloth too??  (Will have to redesign cutter on C++ side for this problem!)
                //###DEV ###DESIGN: Recreate public properties each time???
                CFlex.CreateFlexObject(gameObject, _oMeshNow, _oMeshFlexCollider._oMeshNow, uFlex.FlexBodyType.Soft, uFlex.FlexInteractionType.SelfCollideFiltered, CGame.INSTANCE.nMassSoftBody, Color.red);  //SelfCollideFiltered
                uFlex.FlexProcessor oFlexProc = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexProcessor)) as uFlex.FlexProcessor;
                oFlexProc._oFlexProcessor = this;

                //=== Obtain references to the components we'll need at runtime ===
                _oFlexParticles         = GetComponent<uFlex.FlexParticles>();
                _oFlexParticlesRenderer = GetComponent<uFlex.FlexParticlesRenderer>();
                _oFlexShapeMatching     = GetComponent<uFlex.FlexShapeMatching>();
                _oFlexGeneratedSMR      = GetComponent<SkinnedMeshRenderer>();

                //=== Backup the rest position so we can expand / contract soft body volume without loss of information ===
                _aShapeRestPosOrig = new Vector3[_oFlexShapeMatching.m_shapeIndicesCount];
                Array.Copy(_oFlexShapeMatching.m_shapeRestPositions, _aShapeRestPosOrig, _oFlexShapeMatching.m_shapeIndicesCount);

                //=== Backup the position of the particles at startup time (so we can reset softbody after pose teleport) ===
                _aFlexParticlesAtStart = new Vector3[_oFlexParticles.m_particlesCount];
                for (int nParticle = 0; nParticle < _oFlexParticles.m_particlesCount; nParticle++)
                    _aFlexParticlesAtStart[nParticle] = _oBoneAnchor.worldToLocalMatrix.MultiplyPoint(_oFlexParticles.m_particles[nParticle].pos);      //###LEARN: How to properly convert from world to local (taking into account the full path of the transform we're converting about)


                //=== Fill in the Flex tetraverts into our 'Unity2Blender' mesh so it can quickly skin and pin the appropriate verts ===
                //###F: Enhance Unity2Blender functionality to accept any # of verts!
                int nVertTetras = _oFlexParticles.m_particlesCount;
                if (nVertTetras > CBody.C_Unity2Blender_MaxVerts)			// Unity to Blender mesh created at a fixed size with the max number of verts we're expecting.  Check if we're within our set limit
                	throw new Exception("ERROR in CBSoft.Init()  More tetraverts than # of verts in Unity2Blender mesh!");

                //=== Upload our tetraverts to Blender so it can select those that are pinned and skin them ===
                for (int nVertTetra = 0; nVertTetra < nVertTetras; nVertTetra++)
                    _oMesh_Unity2Blender._memVerts.L[nVertTetra] = _oFlexParticles.m_particles[nVertTetra].pos;
				_oMesh_Unity2Blender.UpdateVertsToBlenderMesh();                // Blender now has our tetraverts.  It can now find the tetraverts near the rim and skin them

                //=== Create and retrieve the softbody rim mesh responsible to pin softbody to skinned body ===
                float nRangeTetraPinHunt = CGame.INSTANCE.particleSpacing * CGame.INSTANCE.nRimTetraVertHuntDistanceMult;       //###TUNE: Make relative to all-important Flex particle size!
                CGame.gBL_SendCmd("CBody", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".ProcessTetraVerts(" + nVertTetras.ToString() + ", " + nRangeTetraPinHunt.ToString() + ")");		// Ask Blender select the tetraverts near the rim and skin them
				_oMeshSoftBodyRim = (CBSkinBaked)CBMesh.Create(null, _oBody, _sBlenderInstancePath_CSoftBody + ".oMeshSoftBodyRim", typeof(CBSkinBaked));           // Retrieve the skinned softbody rim mesh Blender just created so we can pin softbody at runtime
                _oMeshSoftBodyRim.transform.SetParent(transform);

                //=== Receive the important 'CSoftBody.aMapRimTetravert2Tetravert' and 'CSoftBody.aMapTwinVerts' array Blender has prepared for softbody-connection to skinned mesh.  (to map the softbody edge vertices to the skinned-body vertices they should attach to)
                List<ushort> aMapVertsSkinToSim;
                CUtility.BlenderSerialize_GetSerializableCollection_USHORT("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aMapVertsSkinToSim()",	out aMapVertsSkinToSim);		// Read the tetravert traversal map from our CSoftBody instance
				CUtility.BlenderSerialize_GetSerializableCollection_USHORT("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aMapRimVerts2Verts()",	out _aMapRimVerts2Verts);               // Read the twin-vert traversal map from our CSoftBody instance

                //=== Create the Flex-to-skinned-mesh component responsible to guide selected Flex particles to skinned-mesh positions ===
                //###NOW###
                //_oFlexSkinnedSpringDriver = CUtility.FindOrCreateComponent(gameObject, typeof(CFlexSkinnedSpringDriver)) as CFlexSkinnedSpringDriver;
                //_oFlexSkinnedSpringDriver.Initialize(ref aMapVertsSkinToSim, _oMeshSoftBodyRim._oSkinMeshRendNow);

                //=== Bake the rim tetramesh a first time so its rim and tetraverts are updated to its skinned body ===
                //###OBS? _oMeshRimBaked.Baking_UpdateBakedMesh();

                //     //=== Pin the close-to-rim-backplate tetraverts by setting them as infinite mass.  They will be moved by us every frame (not simulated) ===
                //     for (int nIndex = 0; nIndex < _aMapRimTetravert2Tetravert.Count; ) {            // Iterate through the rim tetraverts to update the position of their corresponding tetraverts
                //         nIndex++;    //ushort nVertTetraRim	= _aMapRimTetravert2Tetravert[nIndex++];			// The simple list has been flattened into <nVertTetraRim0, nVertTetra0>, <nVertTetraRim1, nVertTetra1>, etc...
                //ushort nVertTetra		= _aMapRimTetravert2Tetravert[nIndex++];
                //         _oSoftFlexParticles.m_particles[nVertTetra].invMass = 0;                    // Remove pinned tetraverts from SoftBody simulation by setting their 1/mass to zero (infinite weight = no movement)
                //         _oSoftFlexParticles.m_colours[nVertTetra] = Color.magenta;                  // Color pin verts separately so we can visualize where they are.
                //     }


                //=== Create the managing object and related hotspot ===
                _oObj = new CObject(this, 0, typeof(EFlexSoftBody), "SoftBody " + gameObject.name);        //###IMPROVE: Name of soft body to GUI
                _oObj.PropGroupBegin("", "", true);
                _oObj.PropAdd(EFlexSoftBody.Volume,         "Volume",       1.0f, 0.6f, 1.6f, "", CProp.Local);
                _oObj.PropAdd(EFlexSoftBody.Stiffness,      "Stiffness",    1.0f, 0.001f, 1.0f, "", CProp.Local);       //###IMPROVE: Log scale!
                _oObj.PropAdd(EFlexSoftBody.Mass,           "Mass",         1.0f, 0.0001f, 1000.0f, "", CProp.Local);
                _oObj.FinishInitialization();
                if (GetType() != typeof(CBreastR))          //###HACK!: Right breast doesn't get hotspot (left breast gets it and manually broadcasts to right one)
                    _oHotSpot = CHotSpot.CreateHotspot(this, _oBoneAnchor, "SoftBody", false, new Vector3(0, 0.10f, 0.08f));     //###IMPROVE!!! Position offset that makes sense for that piece of clothing (from center of its verts?)

                break;

			case EGameModes.Configure:
                //=== Destroy the components created when Play was launched ===
                CUtility.DestroyComponent(GetComponent<uFlex.FlexProcessor>());
                CUtility.DestroyComponent(GetComponent<uFlex.FlexParticlesRenderer>());
                CUtility.DestroyComponent(GetComponent<uFlex.FlexShapeMatching>());
                CUtility.DestroyComponent(GetComponent<uFlex.FlexSkinnedMesh>());
                CUtility.DestroyComponent(GetComponent<uFlex.FlexSprings>());
                CUtility.DestroyComponent(GetComponent<CVisualizeSoftBody>());
                CUtility.DestroyComponent(GetComponent<SkinnedMeshRenderer>());
                CUtility.DestroyComponent(_oFlexSkinnedSpringDriver); _oFlexSkinnedSpringDriver = null;
                CUtility.DestroyComponent(_oFlexParticles);     _oFlexParticles = null;

                if (_oObj != null)
					_oObj = null;
				if (_oMeshSoftBodyRim != null)
					Destroy(_oMeshSoftBodyRim.gameObject);
				_oMeshSoftBodyRim = null;
				_aMapRimVerts2Verts = null;

                //=== Return the visible verts to the starting position (so next conversion to Softbody starts from the same data and skins properly ===
                CopyOriginalVertsToVerts(false);

                //=== Capped softbodies have messed up normals due to capping.  Blender constructed for us a map of which rim verts map to which source verts.  Reset the rim normals to the corresponding source vert normal for seamless rendering ===
                //###F ###BROKEN
                //CUtility.BlenderSerialize_GetSerializableCollection("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aMapRimVerts2SourceVerts()",	out _aMapRimVerts2SourceVerts);
                //for (int nIndex = 0; nIndex < _aMapRimVerts2SourceVerts.Count;) {         // Iterate through the flattened map...
                //	int nVertID			= _aMapRimVerts2SourceVerts[nIndex++];            // The simple list has been flattened into <nVertID0, nVertSourceID0>, etc...
                //	int nVertSourceID   = _aMapRimVerts2SourceVerts[nIndex++];
                //	_memNormals.L[nVertID] = _oBody._oBodySource._memNormals.L[nVertSourceID];
                //}
                //_oMeshNow.normals   = _memNormals.L;

                break;
		}
	}

    public void HoldSoftBodiesInReset(bool bSoftBodyInReset) {
        _bSoftBodyInReset = bSoftBodyInReset;
    }

    public void Reset_SoftBody_DoReset() {                       // Reset softbody to its startup state around anchor bone.  Essential during pose load / teleportation!
        if (_oFlexParticles != null) { 
            for (int nParticle = 0; nParticle < _aFlexParticlesAtStart.Length; nParticle++) {       //_oSoftFlexParticles.m_particlesCount
                _oFlexParticles.m_particles [nParticle].pos = _oBoneAnchor.localToWorldMatrix.MultiplyPoint(_aFlexParticlesAtStart[nParticle]);
                _oFlexParticles.m_velocities[nParticle] = Vector3.zero;
            }
        }
    }

    
    //--------------------------------------------------------------------------	UTILITY
    public void HideShowMeshes(bool bShowPresentation, bool bShowPhysxColliders, bool bShowMeshStartup, bool bShowPinningRims, bool bShowFlexSkinned, bool bShowFlexColliders, bool bShowFlexParticles) {
        //###IMPROVE ###DESIGN Collect show/hide flags in a global array?
        GetComponent<MeshRenderer>().enabled = bShowPresentation;
        if (_oFlexSkinnedSpringDriver != null)
            _oFlexSkinnedSpringDriver._oSMR_Driver.enabled = bShowPinningRims;
        if (_oFlexGeneratedSMR != null)
            _oFlexGeneratedSMR.enabled = bShowFlexSkinned;
        if (_oMeshFlexCollider != null)
            _oMeshFlexCollider.GetComponent<MeshRenderer>().enabled = bShowFlexColliders;        // Add a flag for this intermediate mesh?  ###DESIGN: Or delete once done?
        if (_oFlexParticlesRenderer != null)
            _oFlexParticlesRenderer.enabled = bShowFlexParticles;
        if (_oMesh_Unity2Blender != null)
            _oMesh_Unity2Blender.GetComponent<MeshRenderer>().enabled = false;      // Always hide this mesh... no visible value?
    }


    //--------------------------------------------------------------------------	IHotspot interface

    public void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) { }         //###F: ###DESIGN: Duplication with cloth (and other Flex objects... combine in one class?)

	public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {		//###DESIGN? Currently an interface call... but if only GUI interface occurs through CObject just have cursor directly invoke the GUI_Create() method??
		if (eHotSpotEvent == EHotSpotEvent.ContextMenu)
			_oHotSpot.WndPopup_Create(_oBody, new CObject[] { _oObj });
	}

    public void OnPropSet_Volume(float nValueOld, float nValueNew) {
        //###LEARN: Flex softbody volume is expanded / contracted by scaling the shape rest particle position (as they are all about the shape center / bone)
        for (int nShapeIndex = 0; nShapeIndex < _oFlexShapeMatching.m_shapeIndicesCount; nShapeIndex++)
            _oFlexShapeMatching.m_shapeRestPositions[nShapeIndex] = _aShapeRestPosOrig[nShapeIndex] * nValueNew;
        if (GetType() == typeof(CBreastL))          //###HACK!: Manually call right breast equivalent from left breast... crappy hack to avoid forming a CBreasts object to broadcast to both
            _oBody._oBreastR.OnPropSet_Volume(nValueOld, nValueNew);
        //Debug.LogFormat("SoftBody Volume: {0}", nValueNew);
    }

    public void OnPropSet_Stiffness(float nValueOld, float nValueNew) {
        for (int nShape = 0; nShape < _oFlexShapeMatching.m_shapesCount; nShape++)
            _oFlexShapeMatching.m_shapeCoefficients[nShape] = nValueNew;
        if (GetType() == typeof(CBreastL))          //###HACK!: Manually call right breast equivalent from left breast... crappy hack to avoid forming a CBreasts object to broadcast to both
            _oBody._oBreastR.OnPropSet_Stiffness(nValueOld, nValueNew);
        //Debug.LogFormat("SoftBody Stiffness: {0}", nValueNew);               //###IMPROVE: Remove per-function logging and add flag so CProp does it (with various logging levels)
    }

    public void OnPropSet_Mass(float nValueOld, float nValueNew) {
        float nInvMassPerParticle = 1 / (nValueNew * _oFlexParticles.m_particlesCount);     // Note that this count is all particles (including pinned)... change to only unpinned particles for mass?
        for (int nPar = 0; nPar < _oFlexParticles.m_particlesCount; nPar++)                     //###BUG: Doesn't appear to do anything!
            _oFlexParticles.m_particles[nPar].invMass = nInvMassPerParticle;        //###BUG: Pin particles  ###F
   //     for (int nIndex = 0; nIndex < _aMapRimTetravert2Tetravert.Count; ) {            // Iterate through the rim tetraverts to update the position of their corresponding tetraverts
   //         nIndex++;   // ushort nVertTetraRim	= _aMapRimTetravert2Tetravert[nIndex++];			// The simple list has been flattened into <nVertTetraRim0, nVertTetra0>, <nVertTetraRim1, nVertTetra1>, etc...
			//ushort nVertTetra		= _aMapRimTetravert2Tetravert[nIndex++];
   //         _oSoftFlexParticles.m_particles[nVertTetra].invMass = 0;
   //     }
        if (GetType() == typeof(CBreastL))          //###HACK!: Manually call right breast equivalent from left breast... crappy hack to avoid forming a CBreasts object to broadcast to both
            _oBody._oBreastR.OnPropSet_Mass(nValueOld, nValueNew);
        //Debug.LogFormat("SoftBody Mass {0}", nValueNew);
    }


    //---------------------------------------------------------------------------	UPDATE

    public void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
        //=== Reset soft body if marked for reset (e.g. during pose loading) ===
        if (_bSoftBodyInReset) {
            Reset_SoftBody_DoReset();
        }

        //=== Bake rim skinned mesh and update position of softbody tetravert pins ===
        if (_oMeshSoftBodyRim) { 
		    _oMeshSoftBodyRim.Baking_UpdateBakedMesh();                                        // Bake the rim tetramesh so its rim-backplate and tetraverts are updated to its skinned body.
            _oFlexSkinnedSpringDriver.UpdateFlexParticleToSkinnedMesh();

            Vector3[] aVertsRimBaked    = _oMeshSoftBodyRim._oMeshBaked.vertices;     //###LEARN!!!!!: Absolutely IMPERATIVE to obtain whole array before loop like the one below... with individual access profiler reveals 7ms per frame if not!!
            Vector3[] aNormalsRimBaked  = _oMeshSoftBodyRim._oMeshBaked.normals;
       //     for (int nIndex = 0; nIndex < _aMapRimTetravert2Tetravert.Count; ) {			// Iterate through the rim tetraverts to update the position of their corresponding tetraverts
			    //ushort nVertTetraRim	= _aMapRimTetravert2Tetravert[nIndex++];			// The simple list has been flattened into <nVertTetraRim0, nVertTetra0>, <nVertTetraRim1, nVertTetra1>, etc...
			    //ushort nVertTetra		= _aMapRimTetravert2Tetravert[nIndex++];
       //         _oSoftFlexParticles.m_particles[nVertTetra].pos = aVertsRimBaked[nVertTetraRim];  // Set position of tetramesh pinned vert to its corresponding vert in skinned rim mesh.  This will 'pin' the edge of the soft body to where it should go on skinned body.
       //     }

            //=== Bake the skinned softbody into a regular mesh (so we can update edge-of-softbody position and normals to pertinent rim verts ===
            _oFlexGeneratedSMR.BakeMesh(_oMeshFlexGenerated);                  //###OPT!!! Check how expensive this is.  Is there a way for us to move verts & normals straight from skinned mesh from Flex?  (Have not found a way so far)
            Vector3[] aVertsFlexGenerated    = _oMeshFlexGenerated.vertices;
            Vector3[] aNormalsFlexGenerated  = _oMeshFlexGenerated.normals;

            //=== Iterate through all softbody edge verts to update their position and normals ===
            for (int nIndex = 0; nIndex < _aMapRimVerts2Verts.Count;) {         // Iterate through the twin vert flattened map...
                ushort nVertMesh    = _aMapRimVerts2Verts[nIndex++];            // The simple list has been flattened into <nVertMesh0, nVertRim0>, <nVertMesh1, nVertRim1>, etc
                ushort nVertRim     = _aMapRimVerts2Verts[nIndex++];
                aVertsFlexGenerated  [nVertMesh] = aVertsRimBaked  [nVertRim];
                aNormalsFlexGenerated[nVertMesh] = aNormalsRimBaked[nVertRim];
            }
            _oMeshNow.vertices = aVertsFlexGenerated;
            _oMeshNow.normals  = aNormalsFlexGenerated;
        }
    }

    public virtual void OnSimulatePre() {
		//if (Input.GetKeyDown(KeyCode.F10))			//####TEMP			####OBS ####CLEAN
		//	UpdateVertsFromBlenderMesh(false);

		switch (CGame.INSTANCE._GameMode) {
			case EGameModes.Play:
                break;
			case EGameModes.Configure:
				break;
		}
	}

	public virtual void OnSimulateBetweenFlex3() {}

	public virtual void OnSimulatePost() {      //###OBS
		switch (CGame.INSTANCE._GameMode) {
			case EGameModes.Play:       //=== Update the position and normals of the softbody mesh rim vertices to their equivalent on baked skinned rim mesh.  (This prevents gaps in the two meshes and aligns normals so shading is ok accross the two meshes) ===
				break;
			case EGameModes.Configure:
				break;
		}
	}
    //void OnDrawGizmos() {
    //    if (_oMeshRimBaked == null)
    //        return;
    //    Vector3[] aVertsRimBaked    = _oMeshRimBaked._oMeshBaked.vertices;		//###LEARN!!!!!: Absolutely IMPERATIVE to obtain whole array before loop like the one below... with individual access profiler reveals 7ms per frame if not!!
    //    Vector3[] aNormalsRimBaked  = _oMeshRimBaked._oMeshBaked.normals;
    //    SkinnedMeshRenderer oSMR = go.GetComponent<SkinnedMeshRenderer>();
    //    Vector3[] aVertsOrig = oSMR.sharedMesh.vertices;
    //    Vector3[] aNormalsOrig = oSMR.sharedMesh.normals;

    //    for (int nIndex = 0; nIndex < _aMapRimVerts2Verts.Count;) {         // Iterate through the twin vert flattened map...
    //        ushort nVertMesh = _aMapRimVerts2Verts[nIndex++];            // The simple list has been flattened into <nVertMesh0, nVertRim0>, <nVertMesh1, nVertRim1>, etc
    //        ushort nVertRim = _aMapRimVerts2Verts[nIndex++];
    //        Gizmos.color = Color.red;
    //        Gizmos.DrawLine(aVertsRimBaked[nVertRim], aVertsRimBaked[nVertRim] + 0.05f * aNormalsRimBaked[nVertRim]);
    //        Gizmos.color = Color.green;
    //        Gizmos.DrawLine(aVertsOrig[nVertMesh], aVertsOrig[nVertMesh] + 0.05f * aNormalsOrig[nVertMesh]);
    //    }
    //}




    //void DrawDebugBoneInfo(Vector3 vecVert, Vector3 vecSize, uFlex.FlexSkinnedMesh oFlexSkinned, int nBone) {
    //    Vector3 vecBone = oFlexSkinned.m_bones[nBone].position;
    //    Gizmos.DrawWireCube(vecBone, vecSize);
    //    Gizmos.DrawLine(vecVert, vecBone);
    //}

    //public virtual void OnDrawGizmos() {            //###DEBUG: Temp debug function to visually draw one skinned vert and its four source bones.
    //    if (_VertToShowSkinningBones_HACK != -1) { 
    //        float nSize = CGame.INSTANCE.particleSpacing / 2;
    //        Vector3 vecSize = new Vector3(nSize, nSize, nSize);
    //        Gizmos.color = Color.magenta;
    //        Vector3 vecVert = _oMeshNow.vertices[_VertToShowSkinningBones_HACK];
    //        Gizmos.DrawSphere(vecVert, nSize);

    //        uFlex.FlexSkinnedMesh oFlexSkinned = gameObject.GetComponent<uFlex.FlexSkinnedMesh>();
    //        BoneWeight oBoneWeight = _oFlexGeneratedSMR.sharedMesh.boneWeights[_VertToShowSkinningBones_HACK];
    //        DrawDebugBoneInfo(vecVert, vecSize, oFlexSkinned, oBoneWeight.boneIndex0);
    //        DrawDebugBoneInfo(vecVert, vecSize, oFlexSkinned, oBoneWeight.boneIndex1);
    //        DrawDebugBoneInfo(vecVert, vecSize, oFlexSkinned, oBoneWeight.boneIndex2);
    //        DrawDebugBoneInfo(vecVert, vecSize, oFlexSkinned, oBoneWeight.boneIndex3);
    //    }
    //}

    public void OnPropSet_NeedReset(CProp oProp, float nValueOld, float nValueNew) { }
}
