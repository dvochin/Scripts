using UnityEngine;
using System;
using System.Collections.Generic;

public class CSoftBodyBase : CBMesh, IHotSpotMgr, uFlex.IFlexProcessor
{
    //---------------------------------------------------------------------------	SUB MESHES
    [HideInInspector]	public	CBSkinBaked _oMeshRim;					    // The skinned 'rim mesh' that is baked everyframe.  Contains rim and particles.  Rim is to adjust normals at softbody mesh boundary and the particles in this mesh are to 'pin' our softbody particles to the skinned body (so softbody doesn't go 'flying off')
    [HideInInspector]	public	CBSkinBaked _oMeshPinnedParticles;          // The pinned particles skinned mesh.  Used so we can manually set the position of the pinned particles to the appropriate position on the skinned main body (so softbody doesn't float into space) ===     
	//---------------------------------------------------------------------------	BLENDER ACCESS
	[HideInInspector]	public	string				_sNameSoftBody;					// The name of our 'detached softbody' in Blender.  ('BreastL', 'BreastR', 'Penis', 'VaginaL', 'VaginaR') from a substring of our class name.  Must match Blender!!
	[HideInInspector]	public	string				_sBlenderInstancePath_CSoftBody;				// The Blender instance path where our corresponding CSoftBody is access from CBody's Blender instance
	[HideInInspector]	public	string				_sBlenderInstancePath_CSoftBody_FullyQualfied;	// The Blender instance path where our corresponding CSoftBody is access from CBody's Blender instance (Fully qualified (includes CBody access string)
    //---------------------------------------------------------------------------	COMPONENT REFERENCE
    [HideInInspector]	public	uFlex.FlexParticles         _oFlexParticles;
    [HideInInspector]	public	uFlex.FlexShapeMatching     _oFlexShapeMatching;
    [HideInInspector]	public	uFlex.FlexParticlesRenderer _oFlexParticlesRenderer;
    //---------------------------------------------------------------------------	USER INTERFACE
    [HideInInspector]	public 	CObject	    _oObj;							// The multi-purpose CObject that stores CProp properties  to publicly define our object.  Provides client/server, GUI and scripting access to each of our 'super public' properties.
    CHotSpot    _oHotSpot;                      // The hotspot object that will permit user to left/right click on us in the scene to move/rotate/scale us and invoke our context-sensitive menu.
    //---------------------------------------------------------------------------	BACKUP STRUCTURES
    Vector3[] _aShapeRestPosOrig;               // Backup of shape rest positions at startup.  Used for runtime softbody volume adjustments (without loss of data)
    Vector3[] _aFlexParticlesAtStart;           // Backup of particle position at startup.  Used to 'reset' softbody to original particle position
    bool _bSoftBodyInReset_HACK;                // When true soft body will be reset this frame.  Used during pose loading for fast teleport
    //---------------------------------------------------------------------------	MISC
	List<ushort> _aMapRimVerts = new List<ushort>();    // Collection of mapping between our verts and the verts of our BodyRim.  Used to set softbody mesh rim verts and normals to their skinned-equivalent
    Transform _oBoneAnchor;                     // The bone this softbody 'anchors to' = Resets Flex softbody particles to the world-space position / rotation during reset.  Makes teleportation & rapid movement possible


    //---------------------------------------------------------------------------	INIT
    public override void OnDeserializeFromBlender(params object[] aExtraArgs) {
        base.OnDeserializeFromBlender(aExtraArgs);

		string sNameBoneAnchor = aExtraArgs[0] as string;			// First argument is for sNameBoneAnchor

		transform.SetParent(_oBodyBase._oBody._oBodySkinnedMeshGO_HACK.transform);			// Parent to our body's main skinned mesh	###WEAK<14>: Crappy circumvent way of obtaining node we need early in init!

        _sNameSoftBody = GetType().Name.Substring(1);                            // Obtain the name of our detached body part ('Breasts', 'Penis', 'Vagina') from a substring of our class name.  Must match Blender!!  ###WEAK?
        _sBlenderInstancePath_CSoftBody = ".oBody.aSoftBodies['" + _sNameSoftBody + "']";                          // Simplify access to Blender CSoftBody instance
        _sBlenderInstancePath_CSoftBody_FullyQualfied = _oBodyBase._sBlenderInstancePath_CBodyBase + _sBlenderInstancePath_CSoftBody; // Simplify access to fully-qualified Blender CSoftBody instance (from CBody instance)
        _oBoneAnchor = _oBodyBase._oBody._oBodyBase.FindBone(sNameBoneAnchor);

        //=== Set bounds to infinite so our dynamically-created mesh never has to recalculate bounds ===
        _oMeshNow.bounds = CGame._oBoundsInfinite;          //####IMPROVE: This can hurt performance ####OPT!!
        _oMeshNow.MarkDynamic();        // Docs say "Call this before assigning vertices to get better performance when continually updating mesh"

        //=== Create the managing object and related hotspot ===
		_oObj = new CObject(this, "SoftBody " + gameObject.name, "SoftBody " + gameObject.name);
		CPropGrpEnum oPropGrp = new CPropGrpEnum(_oObj, transform.name, typeof(EActorNode));
        oPropGrp.PropAdd(EFlexSoftBody.Volume,         "Volume",       1.0f, 0.6f, 1.6f, "");
        oPropGrp.PropAdd(EFlexSoftBody.Stiffness,      "Stiffness",    1.0f, 0.001f, 1.0f, "");       //###IMPROVE: Log scale!
        oPropGrp.PropAdd(EFlexSoftBody.SoftBodyMass,   "Mass",         1.0f, 0.0001f, 1000.0f, "");
        _oObj.FinishInitialization();
        if (GetType() != typeof(CBreastR))          //###HACK!: Right breast doesn't get hotspot (left breast gets it and manually broadcasts to right one)
            _oHotSpot = CHotSpot.CreateHotspot(this, _oBoneAnchor, "SoftBody", false, new Vector3(0, 0.10f, 0.08f));     //###IMPROVE!!! Position offset that makes sense for that piece of clothing (from center of its verts?)
    }

    public override void FinishIntialization() {
        base.FinishIntialization();

        //=== Retreive the rim skinned mesh so we can manually set the softbody rim verts to the position & normals for seamless connection to main skinned body ===
        _oMeshRim = (CBSkinBaked)CBMesh.Create(null, _oBodyBase, _sBlenderInstancePath_CSoftBody + ".oMeshSoftBodyRim", typeof(CBSkinBaked));           // Retrieve the skinned softbody rim mesh Blender just created so we can pin softbody at runtime
        _oMeshRim.transform.SetParent(transform);
		_aMapRimVerts = CByteArray.GetArray_USHORT("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".aMapRimVerts.Unity_GetBytes()");               // Read the rim traversal map from our CSoftBodyBase instance

        //=== Backup the position of the particles at startup time (so we can keep softbody from extreme deformation during rapid body movements like pose teleport) ===
        _aFlexParticlesAtStart = new Vector3[_oFlexParticles.m_particlesCount];
        for (int nParticle = 0; nParticle < _oFlexParticles.m_particlesCount; nParticle++)
            _aFlexParticlesAtStart[nParticle] = _oBoneAnchor.worldToLocalMatrix.MultiplyPoint(_oFlexParticles.m_particles[nParticle].pos);      //###LEARN: How to properly convert from world to local (taking into account the full path of the transform we're converting about)

        //=== Backup each shape's rest position so we can expand / contract soft body volume without loss of information ===
        _aShapeRestPosOrig = new Vector3[_oFlexShapeMatching.m_shapeIndicesCount];
        Array.Copy(_oFlexShapeMatching.m_shapeRestPositions, _aShapeRestPosOrig, _oFlexShapeMatching.m_shapeIndicesCount);

        //=== Instantiate the FlexProcessor component so we get hooks to update ourselves during game frames ===
        uFlex.FlexProcessor oFlexProc = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexProcessor)) as uFlex.FlexProcessor;
        oFlexProc._oFlexProcessor = this;

        //=== Instantiate the debug visualizer for internal Softbody structure analysis ===
        CVisualizeSoftBody oVisSB = CUtility.FindOrCreateComponent(gameObject, typeof(CVisualizeSoftBody)) as CVisualizeSoftBody;
        oVisSB.enabled = false;
    }

    public override void OnDestroy() {
		base.OnDestroy();
	}



    //--------------------------------------------------------------------------	UPDATE
    public virtual void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
        if (_bSoftBodyInReset_HACK)                  //###OPT?
            Reset_SoftBody_DoReset();
    }

    public void UpdateVisibleSoftBodySurfaceMesh(ref Vector3[] aVertsSoftBodyMesh, ref Vector3[] aNormalsSoftBodyMesh) {
        // Sets the visible softbody mesh from the vert & normal position given, updating rim position and normals for seamless connection to main skinned body ===
        //=== Bake rim skinned mesh and update position of softbody particle pins ===
        _oMeshRim.Baking_UpdateBakedMesh();     // Bake the skinned portion of the mesh.  We need its verts so we can manually move the softbody rim verts for a seamless connection to main skinned body ===
        //=== Iterate through all softbody edge verts to update their position and normals.  This is critical for a 'seamless connection' between the softbody presentation mesh and the main skinned body ===
        Vector3[] aVertsRimBaked    = _oMeshRim._oMeshBaked.vertices;       // Obtain the verts and normals from baked rim mesh so we can manually set rim verts & normals for seamless connection to main body mesh.
        Vector3[] aNormalsRimBaked  = _oMeshRim._oMeshBaked.normals;
        for (int nIndex = 0; nIndex < _aMapRimVerts.Count;) {         // Iterate through the twin vert flattened map...
            ushort nVertMesh    = _aMapRimVerts[nIndex++];            // The simple list has been flattened into <nVertMesh0, nVertRim0>, <nVertMesh1, nVertRim1>, etc
            ushort nVertRim     = _aMapRimVerts[nIndex++];
            aVertsSoftBodyMesh  [nVertMesh] = aVertsRimBaked[nVertRim];
            aNormalsSoftBodyMesh[nVertMesh] = aNormalsRimBaked[nVertRim];
        }
        _oMeshNow.vertices  = aVertsSoftBodyMesh;               // Set our visible mesh's verts and normals to what we calculated above.
        _oMeshNow.normals   = aNormalsSoftBodyMesh;
		_oMeshNow.RecalculateBounds();                          //###LEARN: Unity will complain about 'Expanding invalid MinMaxAABB' if we have invalid bounds.  ###OPT!!!!: Too expensive?  Since we have verts just cludge to yuge size?
	}



    //--------------------------------------------------------------------------	RESET
    public void HoldSoftBodiesInReset(bool bSoftBodyInReset) {
        _bSoftBodyInReset_HACK = bSoftBodyInReset;
    }

    public void Reset_SoftBody_DoReset() {                       // Reset softbody to its startup state around anchor bone.  Essential during pose load / teleportation!
        if (_oFlexParticles != null) { 
            for (int nParticle = 0; nParticle < _aFlexParticlesAtStart.Length; nParticle++) {       //_oSoftFlexParticles.m_particlesCount
                _oFlexParticles.m_particles [nParticle].pos = _oBoneAnchor.localToWorldMatrix.MultiplyPoint(_aFlexParticlesAtStart[nParticle]);
                _oFlexParticles.m_velocities[nParticle] = Vector3.zero;
            }
        }
    }


    //--------------------------------------------------------------------------	UI
    public virtual void HideShowMeshes() {
        GetComponent<MeshRenderer>().enabled = CGame.INSTANCE.ShowPresentation;
        if (_oMeshRim != null)
            _oMeshRim.GetComponent<SkinnedMeshRenderer>().enabled = CGame.INSTANCE.ShowPinningRims;
        if (_oMeshPinnedParticles != null)
            _oMeshPinnedParticles._oSkinMeshRendNow.enabled = CGame.INSTANCE.ShowPinningRims;
        if (_oFlexParticlesRenderer != null)
            _oFlexParticlesRenderer.enabled = CGame.INSTANCE.ShowFlexParticles;
    }




    //--------------------------------------------------------------------------	IHotspot interface

    public void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) { }         //###F: ###DESIGN: Duplication with cloth (and other Flex objects... combine in one class?)

	public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {		//###DESIGN? Currently an interface call... but if only GUI interface occurs through CObject just have cursor directly invoke the GUI_Create() method??
		if (eHotSpotEvent == EHotSpotEvent.ContextMenu)
			_oHotSpot.WndPopup_Create(_oBodyBase._oBody.FindClosestCanvas(), new CObject[] { _oObj });
	}

    public void OnPropSet_Volume(float nValueOld, float nValueNew) {
        //###LEARN: Flex softbody volume is expanded / contracted by scaling the shape rest particle position (as they are all about the shape center / bone)
        for (int nShapeIndex = 0; nShapeIndex < _oFlexShapeMatching.m_shapeIndicesCount; nShapeIndex++)
            _oFlexShapeMatching.m_shapeRestPositions[nShapeIndex] = _aShapeRestPosOrig[nShapeIndex] * nValueNew;
        if (GetType() == typeof(CBreastL))          //###HACK!: Manually call right breast equivalent from left breast... crappy hack to avoid forming a CBreasts object to broadcast to both
            _oBodyBase._oBody._oBreastR.OnPropSet_Volume(nValueOld, nValueNew);
        //Debug.LogFormat("SoftBody Volume: {0}", nValueNew);
    }

    public void OnPropSet_Stiffness(float nValueOld, float nValueNew) {
        for (int nShape = 0; nShape < _oFlexShapeMatching.m_shapesCount; nShape++)
            _oFlexShapeMatching.m_shapeCoefficients[nShape] = nValueNew;
        if (GetType() == typeof(CBreastL))          //###HACK!: Manually call right breast equivalent from left breast... crappy hack to avoid forming a CBreasts object to broadcast to both
            _oBodyBase._oBody._oBreastR.OnPropSet_Stiffness(nValueOld, nValueNew);
        //Debug.LogFormat("SoftBody Stiffness: {0}", nValueNew);               //###IMPROVE: Remove per-function logging and add flag so CProp does it (with various logging levels)
    }

    public void OnPropSet_SoftBodyMass(float nValueOld, float nValueNew) {
        float nInvMassPerParticle = 1 / (nValueNew * _oFlexParticles.m_particlesCount);     // Note that this count is all particles (including pinned)... change to only unpinned particles for mass?
        for (int nPar = 0; nPar < _oFlexParticles.m_particlesCount; nPar++)                     //###BUG: Doesn't appear to do anything!
            _oFlexParticles.m_particles[nPar].invMass = nInvMassPerParticle;        //###BUG: Pin particles  ###F
   //     for (int nIndex = 0; nIndex < _aMapRimParticle2Particle.Count; ) {            // Iterate through the rim particles to update the position of their corresponding particles
   //         nIndex++;   // ushort nVertTetraRim	= _aMapRimParticle2Particle[nIndex++];			// The simple list has been flattened into <nVertTetraRim0, nVertTetra0>, <nVertTetraRim1, nVertTetra1>, etc...
			//ushort nVertTetra		= _aMapRimParticle2Particle[nIndex++];
   //         _oSoftFlexParticles.m_particles[nVertTetra].invMass = 0;
   //     }
        if (GetType() == typeof(CBreastL))          //###HACK!: Manually call right breast equivalent from left breast... crappy hack to avoid forming a CBreasts object to broadcast to both
            _oBodyBase._oBody._oBreastR.OnPropSet_SoftBodyMass(nValueOld, nValueNew);
        //Debug.LogFormat("SoftBody Mass {0}", nValueNew);
    }
}
