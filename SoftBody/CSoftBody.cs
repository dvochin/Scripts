/*###DOCS24: Aug 2017 - Rewrite of SoftBody (around Blender-centric full solid creation for Flex)

=== DEV ===
- Just flattened...  Unity should know nothing of the old CSoftBody and only know of CSoftBody.
- Clean up old FlexRig naming in U+B
- CBody has collection of CSoftBody objects
	- Need to add other Softbodies
	- Need to parse from Blender list and map to proper override class
		- Two breasts??
- Blender-side checks for softbodies now extraneous!
- Bone finding now local!
	- Rename bones from DynBones to SB name?
- Completely rescan through the code!
	- Consider having BoneID = ShapeID (for each softbody) so we get rid of indirection layer??

=== NEXT ===
- Reconnect the breasts to ensure they still work!
	- How to approach the two breasts?

=== TODO ===
- Re-shift ParticleInfo as we removed SoftBodyID
- Flex iterations at 70 right now.  Tune down!

=== LATER ===
- Continue cleanup of Blender codebase around new CBody and CMesh

=== OPTIMIZATIONS ===

=== REMINDERS ===

=== IMPROVE ===

=== NEEDS ===

=== DESIGN ===

=== QUESTIONS ===

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===

=== WISHLIST ===
- Better usage of visualizer colors: Visualizer has intimate knowledge of shapes & particles and what the are used for.

*/


using UnityEngine;
using System.Collections.Generic;



public class CSoftBody : CBSkinBaked, uFlex.IFlexProcessor {		// CSoftBody: Important class in charge of simulate ALL of a body's softbody parts (breasts, penis, etc) from a complex Blender-provide mesh and support data
	//---------------------------------------------------------------------------	MAIN MEMBERS
	[HideInInspector]	public 	CBody						_oBody;
	[HideInInspector]	public	int							_nSoftBodyID;			// The SoftBody ID we simulated (e.g. Penis, BreastL, BreastR, etc)
	[HideInInspector]	public 	CObject						_oObj;
	//---------------------------------------------------------------------------	FLEX
	[HideInInspector]	public	uFlex.FlexParticles			_oFlexParticles;
    [HideInInspector]	public	uFlex.FlexShapeMatching		_oFlexShapeMatching;
    [HideInInspector]	public	int							_nParticles;
    [HideInInspector]	public	int							_nShapes;
	//---------------------------------------------------------------------------	VISUALIZATION
								CVisualizeShape[]           _aVisShapes;
								CVisualizeParticle[]		_aVisParticles;
						public	bool						_bEnableVisualizer = false;
    [HideInInspector]	public	bool						_bEnableVisualizer_COMPARE = true;
						public	Vector3						_vecVisualiserOffset = new Vector3();//(0.05f, 0.05f, 0.05f);
						public	float						_SizeParticles_Mult	= 0.25f;		//###IMPROVE: Add capacity for dev to change these at runtime via Unity property editor
						public	float						_SizeShapes_Mult	= 0.50f;
    [HideInInspector]	public	Vector3						_vecSizeParticles;
    [HideInInspector]	public	Vector3						_vecSizeShapes;
						public	Color32						_color_ParticlesSelected	= new Color32(255, 255, 0, 255);	// Selected = solid yellow
						public	Color32						_color_ParticlesPinned		= new Color32(255, 165, 0, 255);	// Pinned (infinite mass) = solid orange
						public	Color32						_color_ShapeDefault			= new Color32(0, 0, 255, 64);		// Default shape color a mostly transparent blue		//###IMPROVE: Shape color influenced by CSoftBody status?
	//---------------------------------------------------------------------------	COLLECTIONS
	[HideInInspector]	public	List<int>					_aParticleInfo;				// Array that describes what 'type' a particle is: skinned, simulated-surface (usually with bone) or simulated-inner (no bone).  Lower 12 bits are 1-based bone ID (if it exists)
    [HideInInspector]	public	List<int>					_aShapeVerts;				//###OPT: Any point in keeping these?  Use direct arrays stuffed into Flex instead
    [HideInInspector]	public	List<int>					_aShapeParticleIndices;		//###WEAK:!! Possible for confusion between these List<> and the Arrays[] in Flex!!
    [HideInInspector]	public	List<int>					_aShapeParticleOffsets;
    [HideInInspector]	public	List<ushort>				_aFlatMapBoneIdToShapeId;	//# Serialiazable array storing what shapeID each bone has.  Flat map is a simple list of <Bone1>, <Shape1>, <Bone2>, <Shape2>, etc.
	//---------------------------------------------------------------------------	DOMAIN-TRAVERSAL MAPS
	[HideInInspector]	public	Dictionary<ushort, Transform> _mapBonesDynamic			= new Dictionary<ushort, Transform>();
	[HideInInspector]	public	Dictionary<ushort, ushort>	_mapParticlesToShapes_Base	= new Dictionary<ushort, ushort>();
	[HideInInspector]	public	Dictionary<ushort, ushort>	_mapParticlesToShapes;		//###OBS:? Remove if we never put back particle trim?
	[HideInInspector]	public	Dictionary<ushort, ushort>	_mapShapesToParticles_Base	= new Dictionary<ushort, ushort>();
	[HideInInspector]	public	Dictionary<ushort, ushort>	_mapShapesToParticles;
	//---------------------------------------------------------------------------	MISC
	[HideInInspector]	public	SkinnedMeshRenderer			_oSMR;
						public	float						C_VaginaBoneOffset = CGame.INSTANCE.particleRadius;		//###IMPORTANT:!!! We super-size the penis particles and add an equivalent offset to the vagina bones collision sphere so the larger penis spheres smooth out penetration
	//---------------------------------------------------------------------------	CONSTANTS.  These MUST match their equivalent values in Blender CSoftBody!!!
    //#-------------------------------------------------- Bit masks used in the 'self.aParticleInfo' collection sent to Unity 
    public static int C_ParticleInfo_Mask_Type        = 0x000000FF;     //# This mask enables the multi-purpose ParticleInfo field to read/write the C_ParticleType_XXX particle type
	public static int C_ParticleInfo_Mask_Stiffness   = 0x0000F000;		//# This mask enables the ParticleType field to read/write the particle stiffness (e.g. how resistant to bending that shape is) 
    public static int C_ParticleInfo_Mask_BoneID      = 0x0FFF0000;		//# This mask enables the ParticleType field to store the bone ID when it is of type 'C_ParticleType_SimulatedSurface'  Bones are 1-based and BoneID 0 means 'no bone' 
    public static int C_ParticleInfo_Mask_Flags       = 0x70000000;     //# This mask enables the ParticleType field to store custom flags to identify 'special particles' such as Penis uretra, breast nipples, etc 
    //#-------------------------------------------------- Types of particles / verts in the rig.  Contained in 'self.aParticleInfo' collection sent to Unity 
    public static int C_ParticleType_SkinnedBackplate     = 0x02;			//# Particle is skinned and not simulated but is a member of a softbody 'backplate' so these particles can be recipients of new links / edges to dynamic particles.  (Prevents dynamic particles connecting to unwanted parts of the body like legs or under breast)
    public static int C_ParticleType_SkinnedBackplateRim  = 0x03;			//# Particle is skinned and not simulated but is a member of a softbody 'backplate rim'  so these particles can be recipients of new links / edges to dynamic particles.  Kept separate from backplate as algorithm needs to find them through decimation
    public static int C_ParticleType_SimulatedSurface     = 0x04;			//# Particle is Flex-simulated at at the skin surface.  They all drive a 'dynamic bone' that has been created to move that area of the presentation mesh along with the moving particle.  (e.g. breast surface, penis surface, etc)  The bone is a one-based ID in the lower 12 bits of our ParticleType field (0 = no bone)
    public static int C_ParticleType_SimulatedInner       = 0x05;			//# Particle is Flex-simulated but not on the skin surface.  It has no bone and exists solely for softbody shapes to provide the softbody look and feel.
    //#-------------------------------------------------- Bit tests used in the 'self.aParticleInfo' collection sent to Unity 
    public static int C_ParticleInfo_BitTest_IsOnBackpate     = C_ParticleType_SkinnedBackplate;      //# Bitmask to find backplate particles only.  Catches both C_ParticleType_SkinnedBackplate and C_ParticleType_SkinnedBackplateRim
    public static int C_ParticleInfo_BitTest_IsSimulated      = C_ParticleType_SimulatedSurface;      //# Bitmask to find simulated particles only.  Catches both C_ParticleType_SimulatedSurface and C_ParticleType_SimulatedInner   
    //#-------------------------------------------------- Bit shifts used in the 'self.aParticleInfo' collection sent to Unity 
    public static int C_ParticleInfo_BitShift_Stiffness   = 12;				//# Stiffness is stored in bits 12-15 for 16 possible values
    public static int C_ParticleInfo_BitShift_BoneID      = 16;				//# BoneIDs are stored from bits 16-27
    public static int C_ParticleInfo_BitShift_Flags       = 28;				//# Flags are stored from bits 28-30
    //#-------------------------------------------------- Flags used in C_ParticleInfo_Mask_Flags collection
    public static int C_ParticleInfo_BitFlag_Uretra       = 1 << C_ParticleInfo_BitShift_Flags;        //# Flags the uretra particle / vert
    //#-------------------------------------------------- Types of softbodies
    public static byte C_SoftBodyID_None       = 0x00;                      //# Particle that have a softbody of zero mean they are not a member of a softbody (these should ALL be C_ParticleType_Skinned particles) 
	public static byte C_SoftBodyID_Vagina     = 0x01;
    public static byte C_SoftBodyID_BreastL    = 0x02;						//# Note that both breasts occupy bit 1 for convenience
    public static byte C_SoftBodyID_BreastR    = 0x03;
    public static byte C_SoftBodyID_Penis      = 0x04;
    //#-------------------------------------------------- Dynamic Bone naming constants shared between Blender and Unity		###CHECK: Needed?
    public static string C_Prefix_DynBone_Penis			= "+Penis-";		//# The dynamic penis bones.   Created and skinned in Blender and responsible to repel (only) vagina bones
    public static string C_Prefix_DynBone_Vagina		= "+Vagina-";		//# The dynamic vagina bones.  Created and skinned in Blender and repeled (only) by penis bones
    public static string C_Prefix_DynBone_VaginaHole	= "+VaginaHole-";	//# The dynamic vagina hole bones.  Created and skinned in Blender and responsible to guide penis in rig body.

	//---------------------------------------------------------------------------	DEV
	CProp               _oPropChanged;              // Last property user adjusted.  Set by our CObject property changed event to notify Flex processor loop to perform the work (adjusting Flex data *must* be done in processor call)
	public Transform	_oBoneRootT;
	public Quaternion[] _aQuatParticleRotations;	// Stores particle rotations: So that morphs and bends can properly rotate each 'particle bone'
	public Vector3[] _aShapeRestPositionsBAK;       // Backup of init-time '_oFlexShapeMatching.m_shapeRestPositions'.  Used to scale entire softbody size (Breast game-time resize uses this simple technique)


	public static CSoftBody Create(CBody oBody, int nSoftBodyID, Transform oBoneRootT, System.Type oType) {
		CSoftBody oSoftBody = CBMesh.Create(null, oBody._oBodyBase, ".oBody.mapFlexSoftBodies[" + nSoftBodyID.ToString() + "].oMeshSoftBody", oType) as CSoftBody;
		string sNameSoftBody = CSoftBody.Util_GetSoftbodyNameFromID(nSoftBodyID);
		oSoftBody.name = oBody._oBodyBase._sBodyPrefix + "-SoftBody-" + sNameSoftBody;
		oSoftBody.transform.SetParent(oBody._oBodyBase._oBodyRootGO.transform);        //###IMPROVE: Put this common re-parenting and re-naming in Create!
		oSoftBody.GetComponent<SkinnedMeshRenderer>().enabled = false;
		oSoftBody.Initialize(oBody, nSoftBodyID, oBoneRootT);
		return oSoftBody;
	}


	public virtual void Initialize(CBody oBody, int nSoftBodyID, Transform oBoneRootT) {
		//=== Obtain access to the Blender-constructed complex Flex rig skinned mesh and bake it so we can access its latest vert positions ===
		_oBody			= oBody;
		_nSoftBodyID	= nSoftBodyID;
		_oBoneRootT		= oBoneRootT;
		_oSMR = gameObject.GetComponent<SkinnedMeshRenderer>();
		
		//=== Bake skinned mesh of softbody rig and obtain its default init-time vertices ===
		Baking_UpdateBakedMesh();
		Vector3[] aVerts_ShapeDef = _oMeshBaked.vertices;

		//=== Obtain the collections Blender constructed for us for easy Flex softbody creation ===
		string sBlenderInstancePath_SoftBody = _oBody._oBodyBase._sBlenderInstancePath_CBodyBase + ".oBody.mapFlexSoftBodies[" + nSoftBodyID.ToString() + "]";		//###WEAK: Complex & convoluted!  Rethink these global access vars!
		_aParticleInfo				= CByteArray.GetArray_INT	 ("'CBody'", sBlenderInstancePath_SoftBody + ".aParticleInfo.Unity_GetBytes()");
		_aShapeVerts				= CByteArray.GetArray_INT	 ("'CBody'", sBlenderInstancePath_SoftBody + ".aShapeVerts.Unity_GetBytes()");
		_aShapeParticleIndices		= CByteArray.GetArray_INT	 ("'CBody'", sBlenderInstancePath_SoftBody + ".aShapeParticleIndices.Unity_GetBytes()");
		_aShapeParticleOffsets		= CByteArray.GetArray_INT	 ("'CBody'", sBlenderInstancePath_SoftBody + ".aShapeParticleOffsets.Unity_GetBytes()");
		_aFlatMapBoneIdToShapeId	= CByteArray.GetArray_USHORT ("'CBody'", sBlenderInstancePath_SoftBody + ".aFlatMapBoneIdToShapeId.Unity_GetBytes()");
		
		_nParticles = _oMeshBaked.vertexCount;					// Each vertex in the rig mesh is a particle!  (A small portion are simulated (in softbody areas) while most are skinned (in limbs / non-softbody areas))
		_nShapes	= _aShapeVerts.Count;                           // Only some simulated particles / verts have actual shapes.  (Most 'simulated particles' have a shape, and most of the surface ones have a bone)
		_aQuatParticleRotations = new Quaternion[_nParticles];


		//===== CREATE FLEX PARTICLES OBJECT =====
		//=== Define Flex particles from Blender mesh made for Flex ===
		_oFlexParticles = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexParticles)) as uFlex.FlexParticles;
		_oFlexParticles.m_particlesCount = _oFlexParticles.m_maxParticlesCount = _nParticles;
		_oFlexParticles.m_particles				= new uFlex.Particle[_nParticles];
		_oFlexParticles.m_restParticles			= new uFlex.Particle[_nParticles];		//###OPT19: Wasteful?  Remove from uFlex??
		_oFlexParticles.m_colours				= new Color[_nParticles];
		_oFlexParticles.m_velocities			= new Vector3[_nParticles];
		_oFlexParticles.m_densities				= new float[_nParticles];
		_oFlexParticles.m_particlesActivity		= new bool[_nParticles];
		_oFlexParticles.m_colour				= Color.white;				//###TODO: Colors!
		_oFlexParticles.m_interactionType		= uFlex.FlexInteractionType.SelfCollideAll;		// We now go for full 'all' collision.  This makes particles inside the same FlexParticle group collide against one-another (like left breast and right breast)
		_oFlexParticles.m_collisionGroup		= -1;
		_oFlexParticles.m_overrideMass			= true;							//###INFO: when m_overrideMass is true ALL particles have invMass set to 1 / m_mass.  So we *MUST* have this always true so we can have kinematic particles
		_oFlexParticles.m_mass					= 1f;							//###INFO: when m_overrideMass is true this has NO influence so we can set to whatever.
		_oFlexParticles.m_bounds = _oMeshBaked.bounds;

		Quaternion quatRotParticle_HACK = Quaternion.Euler(-90, 0, 180);        //###HACK:!! This is how Blender orients all the dynamic bones = Gives really crappy constant!  ###IMPROVE: Change default Blender bone rotation to match Unity null-rotation?
		for (int nParticle = 0; nParticle < _nParticles; nParticle++) {
			_oFlexParticles.m_restParticles[nParticle].pos = _oFlexParticles.m_particles[nParticle].pos = aVerts_ShapeDef[nParticle];
			_aQuatParticleRotations[nParticle] = quatRotParticle_HACK;
			int nParticleType = _aParticleInfo[nParticle] & C_ParticleInfo_Mask_Type;
			if ((nParticleType & C_ParticleInfo_BitTest_IsSimulated) != 0)
				_oFlexParticles.m_restParticles[nParticle].invMass = _oFlexParticles.m_particles[nParticle].invMass = 1 / _oFlexParticles.m_mass;        // These are simulated particles.  They move freely (and are responsible for driving softbody shapes that in turn drive presentation bones)
			else
				_oFlexParticles.m_restParticles[nParticle].invMass = _oFlexParticles.m_particles[nParticle].invMass = 0;        // These are pinned particles.  They never move from the simulation (we move them to repel clothing, softbody and fluids)
			_oFlexParticles.m_colours[nParticle] = _oFlexParticles.m_colour;	//###IMPROVE: Default color?
			_oFlexParticles.m_particlesActivity[nParticle] = true;				// All our particles are always active every frame.  (Including kinematic ones with infinite mass pinning the softbody to the body)
		}


		//===== CREATE FLEX SHAPES =====
		//=== Define Flex shapes from the Blender particles that Blender has flagged as shapes (most simulated particles) ===
		_oFlexShapeMatching = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexShapeMatching)) as uFlex.FlexShapeMatching;
		_oFlexShapeMatching.m_shapesCount			= _nShapes;
		_oFlexShapeMatching.m_shapeIndicesCount		= _aShapeParticleIndices.Count;
		_oFlexShapeMatching.m_shapeIndices			= _aShapeParticleIndices.ToArray();            //###INFO: How to convert a list to a straight .Net array.
		_oFlexShapeMatching.m_shapeOffsets			= _aShapeParticleOffsets.ToArray();
		_oFlexShapeMatching.m_shapeCenters          = new Vector3[_nShapes];
		_oFlexShapeMatching.m_shapeCoefficients		= new float[_nShapes];
		_oFlexShapeMatching.m_shapeTranslations		= new Vector3[_nShapes];
		_oFlexShapeMatching.m_shapeRotations		= new Quaternion[_nShapes];
		_oFlexShapeMatching.m_shapeRestPositions	= new Vector3[_oFlexShapeMatching.m_shapeIndicesCount];

        //=== Instantiate the FlexProcessor component so can update Flex data at the proper time (Flex data exists in GPU memory) ===
        uFlex.FlexProcessor oFlexProc = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexProcessor)) as uFlex.FlexProcessor;
        oFlexProc._oFlexProcessor = this;

		//=== Recursively find all the dynamic bones for softbody.  This CSoftBody instance will move them once per frame ===
		string sBonePrefix = "+" + CSoftBody.Util_GetSoftbodyNameFromID(nSoftBodyID) + "-";
		Util_FindDynamicBones_RECURSIVE(sBonePrefix, _oBoneRootT, ref _mapBonesDynamic);

		//=== Create the maps between shapes and particles (and vice-versa) ===
		int nShapeLinkStart = 0;
		for (int nShape = 0; nShape < _nShapes; nShape++) {
			int nShapeLinkEnd = _oFlexShapeMatching.m_shapeOffsets[nShape];
			_oFlexShapeMatching.m_shapeCoefficients[nShape] = CGame.INSTANCE.D_SoftBodyStiffness_HACK;                        //###TODO:!!!!! Implement property pushing at init like old CPenis!
			_mapParticlesToShapes_Base.Add((ushort)_oFlexShapeMatching.m_shapeIndices[nShapeLinkStart], (ushort)nShape);     // Now that we have first index available map two way relationship between a shape and its 'master particle' (ie. the particle that created it in Blender)
			_mapShapesToParticles_Base.Add((ushort)nShape, (ushort)_oFlexShapeMatching.m_shapeIndices[nShapeLinkStart]);
			nShapeLinkStart = nShapeLinkEnd;
		}
		_mapParticlesToShapes = _mapParticlesToShapes_Base;		// Base never changes and represents state of softbody as Blender defines it.  Non-base changes the Flex softbody topology as the softbody grows / shinks in size
		_mapShapesToParticles = _mapShapesToParticles_Base;

		//=== Create the needed 'indirection bones so we can efficiently traverse from the shape domain to the particle / bone domain ===
		//###NOTE: This is needed because Blender defines bones at the particles while Unity / Flex *must* have Flex shapes properly centered about their particles.  To facilitates this domain traversal we add a fake 'shape bone' that we will move / orient at each frame and read the position / rotation of its only child (particle = bone)
		for (int nArrayIndex = 0; nArrayIndex < _aFlatMapBoneIdToShapeId.Count; nArrayIndex += 2) {
			ushort nBoneID	= _aFlatMapBoneIdToShapeId[nArrayIndex + 0];		//# Serialiazable array storing what shapeID each bone has.  Flat map is a simple list of <Bone1>, <Shape1>, <Bone2>, <Shape2>, etc.
			ushort nShapeID = _aFlatMapBoneIdToShapeId[nArrayIndex + 1];
			Transform oBoneParticleT = _mapBonesDynamic[nBoneID];
			Transform oBoneShapeT = new GameObject("Shape" + nShapeID.ToString()).transform;		//###IMPROVE: Name
			oBoneShapeT.SetParent(_oBoneRootT);				// Position of this new bone is done in Reshape() below.
			oBoneParticleT.SetParent(oBoneShapeT);
		}

		//=== Call Reshape() the first time to properly initialize the init-time softbody ===
		ShapeDef_DefineSoftBodyShape(true);
	}


	public virtual void DoDestroy() {
		//=== Destroy the dynamic bones we created ===
		foreach (KeyValuePair<ushort, Transform> oPair in _mapBonesDynamic)
			GameObject.Destroy(oPair.Value.gameObject);
		GameObject.Destroy(gameObject);
	}

	public void ShapeDef_DefineSoftBodyShape(bool bInitialize = false) {
		//=== Calculate shape centers from each shape's particles ===
		int nShapeLinkStart = 0;            //###INFO: Setting shape center to our vert center makes the result much less stable!  (By definition I think resting state assumes shape is truly at center of its related particles!)
		for (int nShape = 0; nShape < _nShapes; nShape++) {
			int nShapeLinkEnd = _oFlexShapeMatching.m_shapeOffsets[nShape];
			Vector3 vecCenter = Vector3.zero;
			int nNumParticlesInThisShape = 0;
			for (int nShapeIndex = nShapeLinkStart; nShapeIndex < nShapeLinkEnd; ++nShapeIndex) {
				int nParticle = _oFlexShapeMatching.m_shapeIndices[nShapeIndex];
				Vector3 vecParticle = _oFlexParticles.m_restParticles[nParticle].pos;
				vecCenter += vecParticle;
				nNumParticlesInThisShape++;
			}

			vecCenter /= nNumParticlesInThisShape;
			_oFlexShapeMatching.m_shapeCenters[nShape] = vecCenter;
			nShapeLinkStart = nShapeLinkEnd;
		}

		//=== Set the shape rest positions ===#!
		nShapeLinkStart = 0;
		int nShapeIndexOffset = 0;
		for (int nShape = 0; nShape < _nShapes; nShape++) {
			int nShapeLinkEnd = _oFlexShapeMatching.m_shapeOffsets[nShape];
			for (int nShapeIndex = nShapeLinkStart; nShapeIndex < nShapeLinkEnd; ++nShapeIndex) {
				int nParticle = _oFlexShapeMatching.m_shapeIndices[nShapeIndex];
				Vector3 vecParticle = _oFlexParticles.m_restParticles[nParticle].pos;
				_oFlexShapeMatching.m_shapeRestPositions[nShapeIndexOffset] = vecParticle - _oFlexShapeMatching.m_shapeCenters[nShape];
				nShapeIndexOffset++;
			}
			nShapeLinkStart = nShapeLinkEnd;
		}
		_aShapeRestPositionsBAK = new Vector3[_oFlexShapeMatching.m_shapeRestPositions.Length];
		System.Array.Copy(_oFlexShapeMatching.m_shapeRestPositions, _aShapeRestPositionsBAK, _oFlexShapeMatching.m_shapeRestPositions.Length);		// Copy the shape rest position into this 'backup array' so whole-softbody resize can work (like used on breasts)

		//=== Update the shape bone positions ===
		Vector3 vecPosShapeBackup = Vector3.zero;
		Quaternion quatRotShapeBackup = Quaternion.identity;
		for (int nArrayIndex = 0; nArrayIndex < _aFlatMapBoneIdToShapeId.Count; nArrayIndex += 2) {
			ushort nBoneID	= _aFlatMapBoneIdToShapeId[nArrayIndex + 0];     //# Serialiazable array storing what shapeID each bone has.  Flat map is a simple list of <Bone1>, <Shape1>, <Bone2>, <Shape2>, etc.
			ushort nShapeID = _aFlatMapBoneIdToShapeId[nArrayIndex + 1];
			ushort nParticleID = _mapShapesToParticles[nShapeID];
			Transform oBoneParticleT = _mapBonesDynamic[nBoneID];
			Transform oBoneShapeT = oBoneParticleT.parent;

			//=== Set shapes and particles to their 'at rest' positions with default orientation... then apply the current position / rotation to the parent shape bone to seamless morphing without disturbing softbody shape / position
			if (bInitialize == false) {
				vecPosShapeBackup	= oBoneShapeT.position;
				quatRotShapeBackup	= oBoneShapeT.rotation;
			}
			oBoneShapeT.position = _oFlexShapeMatching.m_shapeCenters[nShapeID];
			oBoneShapeT.rotation = Quaternion.identity;
			oBoneParticleT.position = _oFlexParticles.m_restParticles[nParticleID].pos;
			oBoneParticleT.rotation = _aQuatParticleRotations[nParticleID];		// These rotations are updated during morphing / reshaping operations.
			if (bInitialize == false) {
				oBoneShapeT.position = vecPosShapeBackup;
				oBoneShapeT.rotation = quatRotShapeBackup;
			}
		}
	}

	public override void OnDestroy() {
        Debug.Log("CSoftBody.OnDestroy() cleaning up.");
		Visualization_Hide();
		base.OnDestroy();
	}




	//=========================================================================	UPDATE
	void Update() {     //###DESIGN: In Flex callback or Update()?		###OPT:!! Doesn't need to run each frame and certainly not in Flex callback!
		//if (Input.GetKey(KeyCode.Backspace))
		//	CGame.INSTANCE._bDisableFlexOutputStage_HACK = !CGame.INSTANCE._bDisableFlexOutputStage_HACK;

		if (_bEnableVisualizer != _bEnableVisualizer_COMPARE) {
			Visualization_Set(_bEnableVisualizer);
			_bEnableVisualizer_COMPARE = _bEnableVisualizer;
		}

		if (_bEnableVisualizer) {
			float nSizeParticles	= CGame.INSTANCE.particleRadius * _SizeParticles_Mult;
			float nSizeShapes		= CGame.INSTANCE.particleRadius * _SizeShapes_Mult;
			_vecSizeParticles.Set(nSizeParticles, nSizeParticles, nSizeParticles);
			_vecSizeShapes.Set(nSizeShapes, nSizeShapes, nSizeShapes);

			//=== Update position of every particle ===
			foreach (CVisualizeParticle oVisParticle in _aVisParticles)
				oVisParticle.DoUpdate();

			//=== Update position & orientation of every shape ===
			foreach (CVisualizeShape oVisShape in _aVisShapes)
				oVisShape.DoUpdate();
		}
    }


	//=========================================================================	FLEX CALLBACK
    public virtual void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
		//=== Bake rim skinned mesh and update position of softbody particle pins ===
		Baking_UpdateBakedMesh();								//###OPT:!! Would be worth it to separate the pinning particles from the creation mesh so we don't have to bake as many verts each frame?
		Vector3[] aVertsBakedMesh = _oMeshBaked.vertices;

		//=== FLEX INPUT STAGE: Iterate through all skinned particles to set their position from the baked Flex mesh.  Skinned particle position is the ONLY information fed into Flex ===
		for (int nParticle = 0; nParticle < _nParticles; nParticle++) {      //###OPT!!  Expensive loop per frame that is unfortunately required because different memory footprints... :-(
			int nParticleType = _aParticleInfo[nParticle] & C_ParticleInfo_Mask_Type;
			if ((nParticleType & C_ParticleInfo_BitTest_IsSimulated) == 0) {				//###OPT:!!!! Improve speed by flattening array of just-pinned particles
				_oFlexParticles.m_particles[nParticle].pos = aVertsBakedMesh[nParticle];
				_oFlexParticles.m_particles[nParticle].invMass = 0;		//###OPT:!  ###NOTE: uFlex has a BUG in it in which it overwrites the invMass value we set in init!  Re-evaluate need for this when uFlex updates.
			}
		}

		//=== FLEX OUTPUT STAGE: Iterate through all the Unity bones to set their position / rotation from Flex shapes ===
		for (int nArrayIndex = 0; nArrayIndex < _aFlatMapBoneIdToShapeId.Count; nArrayIndex += 2) {
			ushort nBoneID	= _aFlatMapBoneIdToShapeId[nArrayIndex + 0];		//# Serialiazable array storing what shapeID each bone has.  Flat map is a simple list of <Bone1>, <Shape1>, <Bone2>, <Shape2>, etc.
			ushort nShapeID = _aFlatMapBoneIdToShapeId[nArrayIndex + 1];
			Transform oBoneParticleT = _mapBonesDynamic[nBoneID];           // Obtain reference to the particle / bone and its parent (fake shape 'bone').  We set the shape 'bone' the position / rotation of the corresponding Flex shape knowing that the presentation mesh will be updated only from the real particle bone.
			Transform oBoneShapeT = oBoneParticleT.parent;                  //###NOTE: This is needed because Blender defines bones at the particles while Unity / Flex *must* have Flex shapes properly centered about their particles.  To facilitates this domain traversal we add a fake 'shape bone' that we will move / orient at each frame and read the position / rotation of its only child (particle = bone)
			oBoneShapeT.position = _oFlexShapeMatching.m_shapeTranslations[nShapeID];
			oBoneShapeT.rotation = _oFlexShapeMatching.m_shapeRotations[nShapeID];
		}

		//=== Modify properties within the context of Flex update ===
		if (_oPropChanged != null) {
			OnPropChanged(_oPropChanged);
			_oPropChanged = null;
		}
	}

	//=========================================================================	PROPERTIES
	public virtual void OnPropChanged(CProp oProp) {
	}
	public void Event_PropertyChangedValue(object sender, EventArgs_PropertyValueChanged oArgs) {
		_oPropChanged = oArgs.Property;         // Fired everytime user adjusts a property: Notify flex container update so it can perform the work properly
	}

	//=========================================================================	SHAPE DEFINTION
	public virtual void ShapeDef_Enter() {}

	public virtual void ShapeDef_Leave() {
		ShapeDef_DefineSoftBodyShape();
	}

	public void ShapeDef_SetStiffness(float nStiffness) {
		for (ushort nShape = 0; nShape < _nShapes; nShape++) {
			ushort nParticle = _mapShapesToParticles[nShape];
			int nParticleInfo = _aParticleInfo[nParticle];
			int nShapeStiffness = nParticleInfo & CSoftBody.C_ParticleInfo_Mask_Stiffness;
			_oFlexShapeMatching.m_shapeCoefficients[nShape] = nStiffness;
		}
	}

	//=========================================================================	UTILITY
	void Util_FindDynamicBones_RECURSIVE(string sNameBonePrefix, Transform oBoneNow, ref Dictionary<ushort, Transform> mapBonesDynamic) {
		if (oBoneNow.name.StartsWith(sNameBonePrefix)) {			// Dynamic bones are in the form "+BreastL-123" where '+' indicates a dynamic bone (generated on the fly by Blender), the '-' is a separator before the bone ID
			int nPosSeparator = oBoneNow.name.IndexOf("-");
			ushort nBoneID = 0;
			bool bFoundID = ushort.TryParse(oBoneNow.name.Substring(nPosSeparator+1), out nBoneID);
			if (bFoundID)
				mapBonesDynamic[nBoneID] = oBoneNow;
		}

		int nChilds = oBoneNow.childCount;
		for (int nChild = 0; nChild < nChilds; nChild++) {
			Transform oBoneChild = oBoneNow.GetChild(nChild);
			Util_FindDynamicBones_RECURSIVE(sNameBonePrefix, oBoneChild, ref mapBonesDynamic);
		}
	}

	static public string Util_GetSoftbodyNameFromID(int nSoftBodyID) {
		if (nSoftBodyID == C_SoftBodyID_Vagina)
			return "Vagina";
		else if (nSoftBodyID == C_SoftBodyID_BreastL)
			return "BreastL";
		else if (nSoftBodyID == C_SoftBodyID_BreastR)
			return "BreastR";
		else if (nSoftBodyID == C_SoftBodyID_Penis)
			return "Penis";
		else
			return "###ERROR###";
	}


	#region =========================================================================	VISUALIZATION
	public void Visualization_Show() {
		if (_aVisParticles == null) {

			//=== Create new nodes to render all particles ===
			_aVisParticles = new CVisualizeParticle[_oFlexParticles.m_particlesCount];
			for (int nParticle = 0; nParticle < _oFlexParticles.m_particlesCount; nParticle++) {
				int nParticleType = _aParticleInfo[nParticle] & C_ParticleInfo_Mask_Type;
				GameObject oTemplateGO = Resources.Load("Prefabs/CVisualizeParticle", typeof(GameObject)) as GameObject;
				GameObject oParticleGO = Instantiate(oTemplateGO) as GameObject;
				CVisualizeParticle oVisParticle = CUtility.FindOrCreateComponent(oParticleGO, typeof(CVisualizeParticle)) as CVisualizeParticle;        //###IMPROVE: Set its color based on our type!
				_aVisParticles[nParticle] = oVisParticle;
				oVisParticle.Initialize(this, nParticle);
			}

			//=== Create new nodes to render all shapes ===
			_aVisShapes = new CVisualizeShape[_oFlexShapeMatching.m_shapesCount];
			for (int nShape = 0; nShape < _oFlexShapeMatching.m_shapesCount; nShape++) {
				GameObject oTemplateGO = Resources.Load("Prefabs/CVisualizeShape", typeof(GameObject)) as GameObject;
				GameObject oParticleGO = Instantiate(oTemplateGO) as GameObject;
				CVisualizeShape oVisShape = CUtility.FindOrCreateComponent(oParticleGO, typeof(CVisualizeShape)) as CVisualizeShape;
				_aVisShapes[nShape] = oVisShape;
				oVisShape.Initialize(this, nShape);
			}

			//=== Conveniently hide renderers on our gameObject so we can see our shapes ===
			uFlex.FlexParticlesRenderer oParRend = GetComponent<uFlex.FlexParticlesRenderer>();
			if (oParRend != null)
				oParRend.enabled = false;
			SkinnedMeshRenderer oSMR = GetComponent<SkinnedMeshRenderer>();
			if (oSMR)
				oSMR.enabled = false;
			MeshRenderer oMeshRenderer = GetComponent<MeshRenderer>();
			if (oMeshRenderer != null)
				oMeshRenderer.enabled = false;
			//_oBody._oBodySkinnedMesh._oSkinMeshRendNow.enabled = false;		//###IMPROVE: Set body semi transparent when we're invoked

			_bEnableVisualizer = _bEnableVisualizer_COMPARE = true;
		}
	}

	public void Visualization_Hide() {                  //###IMPROVE: Un-hide renderers hidden in Show()?
		if (_aVisParticles != null) {
			foreach (CVisualizeParticle oVisParticle in _aVisParticles)
				Destroy(oVisParticle.gameObject);
			foreach (CVisualizeShape oVisShape in _aVisShapes)
				Destroy(oVisShape.gameObject);
			_aVisParticles = null;
			_aVisShapes = null;
			_bEnableVisualizer = _bEnableVisualizer_COMPARE = false;
		}
	}

	public void Visualization_Set(bool bShow) {
		if (bShow)
			Visualization_Show();
		else
			Visualization_Hide();
	}
	#endregion


	#region ================================================================	FlexParticleTrim
	//- Penis particle trim problems: (unfinished)
	//	- Trim snaps particles to startup position and breaks immersion
	//	- Trims that remove too many particles move the particle bones poorly and presentation mesh looks bad.
	//	- Remove Blender penis bone update?
	//public void FlexParticleTrim_Launch() {
	//	//=== Reset all the shape flags to active and not-traversed ===
	//	for (int nShape = 0; nShape < _nShapes; nShape++) {
	//		_aCloseParticleSearch_IsActive[nShape] = true;
	//		_aCloseParticleSearch_WasTraversed[nShape] = false;
	//	}

	//	//=== Reset all the particles colors to 'active' ===
	//	for (int nParticle = 0; nParticle < _nParticles; nParticle++) {
	//		_oFlexParticles.m_colours[nParticle] = Color.green;
	//		_oFlexParticles.m_particlesActivity[nParticle] = true;
	//		_oFlexParticles.m_particles[nParticle].pos		= _oFlexParticles.m_restParticles[nParticle].pos;
	//		//_oFlexParticles.m_particles[nParticle].invMass  = _oFlexParticles.m_restParticles[nParticle].invMass;
	//		int nParticleType = _aParticleInfo[nParticle] & C_ParticleInfo_Mask_Type;
	//		if ((nParticleType & C_ParticleInfo_BitTest_IsSimulated) != 0)
	//			_oFlexParticles.m_restParticles[nParticle].invMass = _oFlexParticles.m_particles[nParticle].invMass = 1 / _oFlexParticles.m_mass;        // These are simulated particles.  They move freely (and are responsible for driving softbody shapes that in turn drive presentation bones)
	//		else
	//			_oFlexParticles.m_restParticles[nParticle].invMass = _oFlexParticles.m_particles[nParticle].invMass = 0;        // These are pinned particles.  They never move from the simulation (we move them to repel clothing, softbody and fluids)
	//	}

	//	//=== Starting from a particle we want to keep (uretra for penis, nipple for breast), recursively traverse the softbody particle / shape graph to flag those that are too close to their neighbors ===
	//	FlexParticleTrim_EvaluateShape_RECURSIVE(0);        //###NOW: Uretra (when penis)

	//	//=== Create working shape definition arrays to store a compacted version of the softbody that leaves out all particles trimmed by overly-close neighbor recursive search above ===
	//	List<int> aShapeParticleIndices_Work = new List<int>();
	//	List<int> aShapeParticleOffsets_Work = new List<int>();

	//	//=== Compact the shape definition arrays to leave only the particles that are active ===
	//	int nShapeLinkStart = 0;
	//	for (ushort nShapeThis = 0; nShapeThis < _nShapes; nShapeThis++) {
	//		int nShapeLinkEnd = _aShapeParticleOffsets[nShapeThis];
	//		for (int nShapeIndex = nShapeLinkStart; nShapeIndex < nShapeLinkEnd; ++nShapeIndex) {
	//			ushort nParticleNeighbor = (ushort)_aShapeParticleIndices[nShapeIndex];
	//			if (_mapParticlesToShapes.ContainsKey(nParticleNeighbor)) {                 // Shapes that have pinned particles would not find those in the map so skip
	//				ushort nShapeNeighbor = _mapParticlesToShapes[nParticleNeighbor];
	//				if (nShapeNeighbor < _aCloseParticleSearch_IsActive.Length && _aCloseParticleSearch_IsActive[nShapeNeighbor]) {
	//					aShapeParticleIndices_Work.Add(nParticleNeighbor);
	//				} else {
	//					_oFlexParticles.m_particlesActivity[nParticleNeighbor] = false;
	//					_oFlexParticles.m_particles[nParticleNeighbor].pos = _oFlexParticles.m_restParticles[nParticleNeighbor].pos;
	//				}
	//			} else {
	//				aShapeParticleIndices_Work.Add(nParticleNeighbor);		// Particles that have no shapes = pinned particles = always in!
	//			}
	//		}
	//		aShapeParticleOffsets_Work.Add(aShapeParticleIndices_Work.Count);
	//		nShapeLinkStart = nShapeLinkEnd;
	//	}
	//	if (_nShapes != aShapeParticleOffsets_Work.Count)
	//		CUtility.ThrowExceptionF("###EXCEPTION in CSoftBody '{0}'.  FlexParticleTrim() tried to update to {1} shapes while {2} were present at init-time.", gameObject.name, aShapeParticleOffsets_Work.Count, _nShapes);

	//	//=== Assign the resultant Flex shape indices array to the packed copy we created above ===
	//	_oFlexShapeMatching.m_shapeIndices          = aShapeParticleIndices_Work.ToArray();
	//	_oFlexShapeMatching.m_shapeOffsets          = aShapeParticleOffsets_Work.ToArray();
	//	_oFlexShapeMatching.m_shapeIndicesCount     = aShapeParticleIndices_Work.Count;
	//	_oFlexShapeMatching.m_shapeRestPositions    = new Vector3[_oFlexShapeMatching.m_shapeIndicesCount];

	//	//=== Re-define the shape ===
	//	ShapeDef_DefineSoftBodyShape(false);
	//}

	//void FlexParticleTrim_EvaluateShape_RECURSIVE(ushort nShapeThis) {
	//	_aCloseParticleSearch_WasTraversed[nShapeThis] = true;      // Flag this particle / shape as being traversed so we don't recurse through it again
	//	ushort nParticleThis = _mapShapesToParticles[nShapeThis];
	//	Vector3 vecParticleThis = _oFlexParticles.m_restParticles[nParticleThis].pos;

	//	//=== Obtain the list of links to the particles of this shape ===
	//	int nShapeLinkStart = (nShapeThis == 0) ? 0 : _aShapeParticleOffsets[nShapeThis-1];
	//	int nShapeLinkEnd = _aShapeParticleOffsets[nShapeThis];

	//	//=== First iterate through all this shape's neighbors to deactivate those that are too close ===
	//	if (_aCloseParticleSearch_IsActive[nShapeThis]) {
	//		for (int nShapeIndex = nShapeLinkStart; nShapeIndex < nShapeLinkEnd; nShapeIndex++) {
	//			ushort nParticleNeighbor = (ushort)_aShapeParticleIndices[nShapeIndex];
	//			if (nParticleThis != nParticleNeighbor) {			// All shapes have a link to their own particle.  Skip those!
	//				if (_mapParticlesToShapes.ContainsKey(nParticleNeighbor)) {					// Shapes that have pinned particles would not find those in the map so skip
	//					ushort nShapeNeighbor = _mapParticlesToShapes[nParticleNeighbor];
	//					Vector3 vecParticleNeighbor = _oFlexParticles.m_restParticles[nParticleNeighbor].pos;
	//					Vector3 vecThisToNeighbor = vecParticleNeighbor - vecParticleThis;
	//					float nDistThisToNeighbor = vecThisToNeighbor.magnitude;
	//					if (nDistThisToNeighbor < CGame.INSTANCE.particleSpacing) {
	//						_aCloseParticleSearch_IsActive[nShapeNeighbor] = false;
	//						_oFlexParticles.m_colours[nParticleNeighbor] = Color.red;
	//						_oFlexParticles.m_particlesActivity[nParticleNeighbor] = false;
	//					}
	//				}
	//			}
	//		}
	//	}

	//	//=== Iterate through all the neighbors that are still active and recurse this same function through them ===
	//	for (int nShapeIndex = nShapeLinkStart; nShapeIndex < nShapeLinkEnd; nShapeIndex++) {
	//		ushort nParticleNeighbor = (ushort)_aShapeParticleIndices[nShapeIndex];
	//		if (nParticleThis != nParticleNeighbor) {       // All shapes have a link to their own particle.  Skip those!
	//			if (_mapParticlesToShapes.ContainsKey(nParticleNeighbor)) {                 // Shapes that have pinned particles would not find those in the map so skip
	//				ushort nShapeNeighbor = _mapParticlesToShapes[nParticleNeighbor];
	//				if (_aCloseParticleSearch_WasTraversed[nShapeNeighbor] == false)
	//					FlexParticleTrim_EvaluateShape_RECURSIVE(nShapeNeighbor);
	//			}
	//		}
	//	}
	//}
	#endregion
}



//bool[] _aCloseParticleSearch_WasTraversed;
//bool[] _aCloseParticleSearch_IsActive;
//public Vector3[] _aShapeRestPositionsBAK;       // Backup of init-time '_oFlexShapeMatching.m_shapeRestPositions'.  Used to scale entire softbody size (Breast game-time resize uses this simple technique)
//_aCloseParticleSearch_WasTraversed = new bool[_nShapes];
//_aCloseParticleSearch_IsActive = new bool[_nShapes];
