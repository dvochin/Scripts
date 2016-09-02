//###NOW###
/*

=== CSoftBodySkin design decisions ===
- Need to split CSoftBody into CSoftBodyBase and CSoftBody / CSoftBodySkin
    - Algorithm to define rim and its arrays into base class

- CSoftBodySkin skinning:
    - Done in Blender by distance. (not geometry)
    - Q: Do we need to 'push back' near Flex collider mesh temporarily to skin depth during pose binding?
        - Try this as a last resort... might work with collider a particle distance away.
        - Q: Do we 


=== NEEDS ===
- Need to modify virgin body for vagina hole with proper UV mapping.

=== WISHLIST ===
- Would be awesome if we can programatically generate vagina/anus colliders straight from virgin body mesh... (prevents body morphs)






=== Vagina collider generation ===
- Returning back to DAZ body, we're now sliding verts one ring outward (so no longer a need to clean up mesh)
    - Do we manually slide the verts for 2cm particles or create code for exact opening size?
    - How do we approach dick size vertex slide?
- Need to cut the opening, extrude in for 3 rings
- Then generation of outer collider mesh... 
    - Glue a cylinder with right # of verts or move existing verts to form a cylinder??
    - 




###DESIGN: COrifice: Blender and Unity classes to support Flex-based penetration (vagina and anus)
=== KEY GOALS ===
- Create all dependant meshes from base body mesh and vertex groups.
- Adjust opening size from penis diameter (presentation and both collider meshes)


=== IMPLEMENTATION DECISIONS ===
- Manages the presentation mesh (visible to user), the 'near' and 'far' Flex collider
    - No benefit to use presentation mesh's geometry for the two collider meshes.  For efficiency and flexibility they should be decoupled.
- Need to reduce verts of presentation mesh for near (and far) collider meshes... but how to deal with 'penetration tunnel'??
    - We *must* have this tunnel fully defined (along with UVs and textures) in each vagina/anus.  They all have the same topology and same vert groups for the code to use.
    - We could use 'limited dissolve' to remove some of the extraneous geometry of every vert except penetration tunnle
        -IDEA: Triangulate before limited dissolve

=== PROBLEMS ===
- Skinning presentation mesh to near particles (or shapes?) = orifice will probably skin to particles on both sides
    - Decouple those by removing cross-side bone influences (and re-scale bones on same side)

=== QUESTIONS ===
- Do we operate on half-meshes or full mesh??
    - Half-mesh makes it easier for quality skinning of presentation mesh to near collider... also enforces symmetry during 'limited dissolve'

=== IDEAS ===
- Compute a 'center of opening' along with 'penetration angle' and base penetration tunnel from that.
- Use 'loop tools flatten' to make penetration vert rings planar?
- Use 'loop tools circle' to make far collision mesh?


=== HOW IMPORTANT OPERATIONS ARE DONE ===
- Opening size adjustment (presentation mesh first, which affects both collider meshes)
    - Take vert groups representing the front and the back of the orifice and move with smoothing area.
- Setting verts of penetration tunnel for near collider:
    - Move the side verts of the penetration tunnel +x the distance of the Flex particle distance.  (from vert group)
- Setting verts of penetration tunnel for far collider:
    - Select entire pillars (either through vert groups or by bmesh traversal) and move to the appropriate spot on demi circle.



*/

/* Now have to play with collider in Blender to see how to best acheive large penetration
 *  Blender not updating verts!  Look into that?
 * Need to support a 'tunnel' concept that grows with penis size for the backmesh
 *  Simplify backmesh too?
 * How to write code... have code generate backmesh programmatically or does it join previously-created backmesh asset?
 * Generate different size penises for penetration testing :)
 * 
 * 
 * 
 * We were'nt using particle verts!
 * Work on real rim with body / pin particles
 * Cleanup horrible mess with mass
 * mess with mesh collider / presentation mesh in FlexSkin mode
 * Still at single thickness mesh... do we really need two?
 * We'll definitively need anchor springs
 * Get visualizer working again.
 * Port dildo into our full classes for its benefits?
 * Work on skinned representation
 * Vaginal full opening still a problem to do right
 * 
 * */
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


public class CSoftBody : CBMesh, IObject, IHotSpotMgr, IFlexProcessor {                    //####DEV ####DESIGN: Based on CBMesh or CBSkin??
                                                                                        // Manages a single soft body object send to Flex implementation for soft body simulation.  These 'body parts' (such as breasts, penis, vagina) 
                                                                                        //... are conneted to the main body skinned mesh via _oMeshRimBaked which pins this softbody's tetraverts to those skinned from the main body

	//---------------------------------------------------------------------------	MEMBERS
	[HideInInspector]	public 	CObject				_oObj;							// The multi-purpose CObject that stores CProp properties  to publicly define our object.  Provides client/server, GUI and scripting access to each of our 'super public' properties.
	[HideInInspector]	public	CBSkinBaked			_oMeshSoftBodyRim;					// The skinned 'rim mesh' that is baked everyframe.  Contains rim and tetraverts.  Rim is to adjust normals at softbody mesh boundary and the tetraverts in this mesh are to 'pin' our softbody tetraverts to the skinned body (so softbody doesn't go 'flying off')
	
	[HideInInspector]	public	List<ushort>		_aMapRimVerts2Verts = new List<ushort>();		// Collection of mapping between our verts and the verts of our BodyRim.  Used to set softbody mesh rim verts and normals to their skinned-equivalent
	//[HideInInspector]	public	List<ushort>		_aMapRimVerts2SourceVerts;		// Map of flattened rim vert IDs to source vert IDs.  Allows Unity to reset rim vert normals messed-up by capping to default normal for seamless rendering
	
	//---------------------------------------------------------------------------	Flex-related properties sent during SoftBody_Init()
	[HideInInspector]	public	string				_sNameSoftBody;					// The name of our 'detached softbody' in Blender.  ('BreastL', 'BreastR', 'Penis', 'VaginaL', 'VaginaR') from a substring of our class name.  Must match Blender!!
	[HideInInspector]	public  int					_SoftBodyDetailLevel;			// Detail level of the associated Flex tetramesh... a range between 20 (low) and 50 (very high) is reasonable  ###CLEAN
	[HideInInspector]	public	EColGroups			_eColGroup;                     // The Flex collider group for this softbody.  Used to properly determine what this softbody collides with...###CLEAN
    [HideInInspector]	public	float				_nRangeTetraPinHunt_OBS = 0.035f;	//###OBS??? The maximum distance between the rim mesh and the tetraverts generated by Flex.  Determins which softbody tetraverts are 'pinned' to the skinned body

	//---------------------------------------------------------------------------	MISC
	[HideInInspector]	public	string				_sBlenderInstancePath_CSoftBody;				// The Blender instance path where our corresponding CSoftBody is access from CBody's Blender instance
	[HideInInspector]	public	string				_sBlenderInstancePath_CSoftBody_FullyQualfied;	// The Blender instance path where our corresponding CSoftBody is access from CBody's Blender instance (Fully qualified (includes CBody access string)
	[HideInInspector]	public	CBMesh				oMesh_Unity2Blender;							// The Unity2Blender mesh we use to pass meshes from Unity to Blender for processing there (e.g. Softbody tetramesh skinning & pinning)

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
    Vector3[] _aShapeRestPosOrig;
    CFlexToSkinnedMesh _oFlexToSkinnedMesh;
    bool _bIsFlexSkin;                          // Important switch of implementation between 'false' which is for body parts like breasts and penis where Flex+Unity generates the solid geometry and 'true' where Blender generates precise 'thick skin' geometry from the presentation mesh itself.  (Used for vagina & anus for ultra accurate penetration collision)
    //=== Flexskin-related ===
    List<int> aShapeVerts            = new List<int>();       // Array of which vert / particle is also a shape
    List<int> aShapeParticleIndices  = new List<int>();       // Flattened array of which shape match to which particle (as per Flex softbody requirements)
    List<int> aShapeParticleCutoffs  = new List<int>();       // Cutoff in 'aShapeParticleIndices' between sets defining which particle goes to which shape. 


    //---------------------------------------------------------------------------	INIT

    public CSoftBody() {                           // Setup the default arguments... usually overriden by our derived class   //###BUG??? Why are these settings not overriding those in instanced node???
		_nRangeTetraPinHunt_OBS = CGame.INSTANCE.particleSpacing * 1.0f;       //###TUNE: Make relative to all-important Flex particle size!
	}

	public static CSoftBody Create(CBody oBody, Type oTypeBMesh, string sNameBoneAnchor_HACK) { 
		string sNameSoftBody = oTypeBMesh.Name.Substring(1);                            // Obtain the name of our detached body part ('Breasts', 'Penis', 'Vagina') from a substring of our class name.  Must match Blender!!  ###WEAK?
        _sNameBoneAnchor_HACK = sNameBoneAnchor_HACK;
        bool bIsFlexSkin = oTypeBMesh == typeof(CVagina);           //###WEAK: Duplication between static and instance of same thing
        CGame.gBL_SendCmd("CBody", "CBody_GetBody(" + oBody._nBodyID.ToString() + ").CreateSoftBody('" + sNameSoftBody + "', " + CGame.INSTANCE.nSoftBodyFlexColliderShrinkRatio.ToString() + "," + bIsFlexSkin.ToString() + ")");      // Separate the softbody from the source body.
        
        CSoftBody oSoftBody = (CSoftBody)CBMesh.Create(null, oBody, "aSoftBodies['" + sNameSoftBody + "'].oMeshSoftBody", oTypeBMesh);       // Create the softbody mesh from the just-created Blender mesh.
        //###NOW### //CSoftBody oSoftBody = (CSoftBody)CBMesh.Create(null, oBody, "aSoftBodies['" + sNameSoftBody + "'].oMeshVAGINABAKED_HACK", oTypeBMesh, true);       // Create the softbody mesh from the just-created Blender mesh.
        //////////////CSoftBody oSoftBody = (CSoftBody)CBMesh.Create(null, oBody, "aSoftBodies['" + sNameSoftBody + "'].oMeshFlexCollider", oTypeBMesh);		// Create the softbody mesh from the just-created Blender mesh.
        return oSoftBody;
    }

	public override void OnDeserializeFromBlender() {
		base.OnDeserializeFromBlender();

        _bIsFlexSkin = GetType() == typeof(CVagina);                            // Determine if majority of solid processing is done in Unity+Flex (most solid meshes) or specialized FlexSkin done in Blender
        _sNameSoftBody = GetType().Name.Substring(1);                            // Obtain the name of our detached body part ('Breasts', 'Penis', 'Vagina') from a substring of our class name.  Must match Blender!!  ###WEAK?
		_sBlenderInstancePath_CSoftBody = "aSoftBodies['" + _sNameSoftBody + "']";							// Simplify access to Blender CSoftBody instance
		_sBlenderInstancePath_CSoftBody_FullyQualfied = _oBody._sBlenderInstancePath_CBody + "." + _sBlenderInstancePath_CSoftBody; // Simplify access to fully-qualified Blender CSoftBody instance (from CBody instance)
        _oBoneAnchor = _oBody.FindBone(_sNameBoneAnchor_HACK);

        if (GetComponent<Collider>() != null)
			Destroy(GetComponent<Collider>());                      //###LEARN: Hugely expensive mesh collider created by the above lines... turn it off!

		//=== Set bounds to infinite so our dynamically-created mesh never has to recalculate bounds ===
		_oMeshNow.bounds = CGame._oBoundsInfinite;          //####IMPROVE: This can hurt performance ####OPT!!
		_oMeshNow.MarkDynamic();        // Docs say "Call this before assigning vertices to get better performance when continually updating mesh"

        //###NOW###
        //=== Create the collision mesh from Blender ===
        _oMeshFlexCollider = CBMesh.Create(null, _oBody, _sBlenderInstancePath_CSoftBody + ".oMeshFlexCollider", typeof(CBMesh));       // Also obtain the Unity2Blender mesh call above created.
        _oMeshFlexCollider.GetComponent<MeshRenderer>().enabled = false;      // Collider does not render... only for Flex definition!
        _oMeshFlexCollider.transform.SetParent(transform);
    }



    public override void OnDestroy() {
		Debug.Log("Destroy CSoftBody " + gameObject.name);
		base.OnDestroy();
	}


    public virtual void OnChangeGameMode(EGameModes eGameModeNew, EGameModes eGameModeOld) {

        switch (eGameModeNew) { 
			case EGameModes.Play:

                if (_bIsFlexSkin == false) {                // Non flexskin gets solid geometry processed here in Unity with Flex.

                    //=== Call our C++ side to construct the solid tetra mesh.  We need that to assign tetrapins ===		//###DESIGN!: Major design problem between cutter sent here... can cut cloth too??  (Will have to redesign cutter on C++ side for this problem!)
                    //###DEV ###DESIGN: Recreate public properties each time???
                    CFlex.CreateFlexObject(gameObject, _oMeshNow, _oMeshFlexCollider._oMeshNow, uFlex.FlexBodyType.Soft, uFlex.FlexInteractionType.SelfCollideFiltered, CGame.INSTANCE.nMassSoftBody, Color.red);  //SelfCollideFiltered

                    //=== Obtain references to the components we'll need at runtime ===
                    _oFlexParticles         = GetComponent<uFlex.FlexParticles>();
                    _oFlexParticlesRenderer = GetComponent<uFlex.FlexParticlesRenderer>();
                    _oFlexShapeMatching     = GetComponent<uFlex.FlexShapeMatching>();
                    _oFlexGeneratedSMR      = GetComponent<SkinnedMeshRenderer>();

                    //=== Ask Blender to create a 'Unity2Blender' mesh of the right number of verts so we can upload our Tetramesh to Blender for processing there ===
                    int nVertTetras = _oFlexParticles.m_particlesCount;
                    CGame.gBL_SendCmd("CBody", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".CreateMesh_Unity2Blender(" + nVertTetras.ToString() + ")");        // Our softbody instance will now have its 'oMeshUnity2Blender' member populated with a temporary mesh of exactly nVertTetras verts

                    //=== Obtain the Unity2Blender mesh so we can pass tetraverts to Blender for processing there ===
                    CBMesh oMesh_Unity2Blender = CBMesh.Create(null, _oBody, _sBlenderInstancePath_CSoftBody + ".oMeshUnity2Blender", typeof(CBMesh), true);       // Also obtain the Unity2Blender mesh call above created.    // Keep link to Blender mesh open so we can upload our verts        //###IMPROVE: When/where to release??
                    oMesh_Unity2Blender.transform.SetParent(transform);

                    //=== Upload our tetraverts to Blender so it can select those that are pinned and skin them ===
                    for (int nVertTetra = 0; nVertTetra < nVertTetras; nVertTetra++)
                        oMesh_Unity2Blender._memVerts.L[nVertTetra] = _oFlexParticles.m_particles[nVertTetra].pos;
				    oMesh_Unity2Blender.UpdateVertsToBlenderMesh();                // Blender now has our tetraverts.  It can now find the tetraverts near the rim and skin them

                    //=== Create and retrieve the softbody rim mesh responsible to pin softbody to skinned body ===
                    float nRangeTetraPinHunt = CGame.INSTANCE.particleSpacing * CGame.INSTANCE.nRimTetraVertHuntDistanceMult;       //###TUNE: Make relative to all-important Flex particle size!
                    CGame.gBL_SendCmd("CBody", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".ProcessTetraVerts(" + nRangeTetraPinHunt.ToString() + ")");        // Ask Blender select the tetraverts near the rim and skin them
                    Destroy(oMesh_Unity2Blender);       // We're done with Unity2Blender mesh after ProcessTetraVerts... delete
                    oMesh_Unity2Blender = null;

                }
                else {            // Flexskin implementation get the Flex solid generated in Blender with Unity receiving its generated output.

                    //=== Obtain the collections for the edge and non-edge verts that Blender calculated for us ===
                    CUtility.BlenderSerialize_GetSerializableCollection_INT("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aShapeVerts()",              out aShapeVerts);
                    CUtility.BlenderSerialize_GetSerializableCollection_INT("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aShapeParticleIndices()",    out aShapeParticleIndices);
                    CUtility.BlenderSerialize_GetSerializableCollection_INT("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aShapeParticleCutoffs()",    out aShapeParticleCutoffs);

                    //=== Define Flex particles from Blender mesh made for Flex ===
                    int nParticles = GetNumVerts();
                    _oFlexParticles = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexParticles)) as uFlex.FlexParticles;
                    _oFlexParticles.m_particlesCount = nParticles;
                    _oFlexParticles.m_particles = new uFlex.Particle[nParticles];
                    _oFlexParticles.m_colours = new Color[nParticles];
                    _oFlexParticles.m_velocities = new Vector3[nParticles];
                    _oFlexParticles.m_densities = new float[nParticles];
                    _oFlexParticles.m_particlesActivity = new bool[nParticles];
                    _oFlexParticles.m_colour = Color.green;                //###TODO: Colors!
                    _oFlexParticles.m_interactionType = uFlex.FlexInteractionType.SelfCollideFiltered;
                    _oFlexParticles.m_collisionGroup = -1;
                    //part.m_bounds.SetMinMax(min, max);            //###IMPROVE Bounds?
                    for (int nParticle = 0; nParticle < nParticles; nParticle++) {
                        _oFlexParticles.m_particles[nParticle].pos = _memVerts.L[nParticle];
                        //_oFlexParticles.m_particles[nParticle].invMass = 1;            //###TODO: Mass
                        //_oFlexParticles.m_particles[nParticle].invMass = 0;            //###TODO: Mass
                        _oFlexParticles.m_colours[nParticle] = _oFlexParticles.m_colour;
                        _oFlexParticles.m_particlesActivity[nParticle] = true;
                    }

                    //=== Define Flex shapes from the Blender particles that have been set as shapes too ===
                    int nShapes = aShapeVerts.Count;
                    _oFlexShapeMatching = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexShapeMatching)) as uFlex.FlexShapeMatching;
                    _oFlexShapeMatching.m_shapesCount = nShapes;
                    _oFlexShapeMatching.m_shapeIndicesCount = aShapeParticleIndices.Count;
                    _oFlexShapeMatching.m_shapeIndices = aShapeParticleIndices.ToArray();            //###LEARN: How to convert a list to a straight .Net array.
                    _oFlexShapeMatching.m_shapeOffsets = aShapeParticleCutoffs.ToArray();
                    _oFlexShapeMatching.m_shapeCenters = new Vector3[nShapes];
                    _oFlexShapeMatching.m_shapeCoefficients = new float[nShapes];
                    _oFlexShapeMatching.m_shapeTranslations = new Vector3[nShapes];
                    _oFlexShapeMatching.m_shapeRotations = new Quaternion[nShapes];
                    _oFlexShapeMatching.m_shapeRestPositions = new Vector3[_oFlexShapeMatching.m_shapeIndicesCount];

                    //=== Calculate shape centers from attached particles ===
                    //int nShape = 0;
                    //foreach (int nShapeParticle in aShapeVerts) {
                    //    _oFlexShapeMatching.m_shapeCoefficients[nShape] = 0.05f;                   //###NOW###
                    //    _oFlexShapeMatching.m_shapeCenters[nShape] = _oFlexParticles.m_particles[nShapeParticle].pos;
                    //    nShape++;
                    //}

                    int nShapeStart = 0;
                    for (int nShape = 0; nShape < _oFlexShapeMatching.m_shapesCount; nShape++) {
                        _oFlexShapeMatching.m_shapeCoefficients[nShape] = 0.05f;                   //###NOW###   Calculate shape center here or just accept particle pos?

                        int nShapeEnd = _oFlexShapeMatching.m_shapeOffsets[nShape];
                        Vector3 vecCenter = Vector3.zero;
                        for (int nShapeIndex = nShapeStart; nShapeIndex < nShapeEnd; ++nShapeIndex) {
                            int nParticle = _oFlexShapeMatching.m_shapeIndices[nShapeIndex];
                            Vector3 vecParticlePos = _oFlexParticles.m_particles[nParticle].pos;          // remap indices and create local space positions for each shape
                            vecCenter += vecParticlePos;
                        }

                        vecCenter /= (nShapeEnd - nShapeStart);
                        _oFlexShapeMatching.m_shapeCenters[nShape] = vecCenter;
                        nShapeStart = nShapeEnd;
                    }

                    //=== Set the shape rest positions ===
                    nShapeStart = 0;
                    int nShapeIndexOffset = 0;
                    for (int nShape = 0; nShape < _oFlexShapeMatching.m_shapesCount; nShape++) {
                        int nShapeEnd = _oFlexShapeMatching.m_shapeOffsets[nShape];
                        for (int nShapeIndex = nShapeStart; nShapeIndex < nShapeEnd; ++nShapeIndex) {
                            int nParticle = _oFlexShapeMatching.m_shapeIndices[nShapeIndex];
                            Vector3 vecParticle = _oFlexParticles.m_particles[nParticle].pos;          // remap indices and create local space positions for each shape
                            _oFlexShapeMatching.m_shapeRestPositions[nShapeIndexOffset] = vecParticle - _oFlexShapeMatching.m_shapeCenters[nShape];
                            nShapeIndexOffset++;
                        }
                        nShapeStart = nShapeEnd;
                    }

                    foreach (int nShapeParticle in aShapeVerts) {
                        _oFlexParticles.m_particles[nShapeParticle].invMass = 1;          //###NOW###
                    }

                    //=== Add particle renderer ===
                    _oFlexParticlesRenderer = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexParticlesRenderer)) as uFlex.FlexParticlesRenderer;
                    _oFlexParticlesRenderer.m_size = CGame.INSTANCE.particleSpacing;
                    _oFlexParticlesRenderer.m_radius = _oFlexParticlesRenderer.m_size / 2.0f;
                    _oFlexParticlesRenderer.enabled = false;           // Hidden by default
                }

                //=== Retreive the skinned rim mesh so pinned particles of Flex softbody can fix softbody to main body mesh and not float into space ===
                _oMeshSoftBodyRim = (CBSkinBaked)CBMesh.Create(null, _oBody, _sBlenderInstancePath_CSoftBody + ".oMeshSoftBodyRim", typeof(CBSkinBaked));           // Retrieve the skinned softbody rim mesh Blender just created so we can pin softbody at runtime
                _oMeshSoftBodyRim.transform.SetParent(transform);

                //=== Receive the important arrays Blender has prepared for softbody-connection to skinned mesh.  (to map the softbody edge vertices to the skinned-body vertices they should attach to)
                List<ushort> aMapPinnedFlexParticles;
                CUtility.BlenderSerialize_GetSerializableCollection_USHORT("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aMapPinnedFlexParticles()",	out aMapPinnedFlexParticles);		// Read the tetravert traversal map from our CSoftBody instance
				CUtility.BlenderSerialize_GetSerializableCollection_USHORT("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aMapRimVerts2Verts()",	out _aMapRimVerts2Verts);               // Read the twin-vert traversal map from our CSoftBody instance

                //=== Create the Flex-to-skinned-mesh component responsible to guide selected Flex particles to skinned-mesh positions ===
                if (_bIsFlexSkin == false) {                // Non FlexSkin uses sophisticated CFlexToSkinnedMesh to pin particles via springs.  FlexSkin is direct particle pinning
                    _oFlexToSkinnedMesh = CUtility.FindOrCreateComponent(gameObject, typeof(CFlexToSkinnedMesh)) as CFlexToSkinnedMesh;
                    _oFlexToSkinnedMesh.Initialize(ref aMapPinnedFlexParticles, _oMeshSoftBodyRim);
                }

                //=== Backup the rest position so we can expand / contract soft body volume without loss of information ===
                _aShapeRestPosOrig = new Vector3[_oFlexShapeMatching.m_shapeIndicesCount];
                Array.Copy(_oFlexShapeMatching.m_shapeRestPositions, _aShapeRestPosOrig, _oFlexShapeMatching.m_shapeIndicesCount);

                //=== Backup the position of the particles at startup time (so we can reset softbody after pose teleport) ===
                _aFlexParticlesAtStart = new Vector3[_oFlexParticles.m_particlesCount];
                for (int nParticle = 0; nParticle < _oFlexParticles.m_particlesCount; nParticle++)
                    _aFlexParticlesAtStart[nParticle] = _oBoneAnchor.worldToLocalMatrix.MultiplyPoint(_oFlexParticles.m_particles[nParticle].pos);      //###LEARN: How to properly convert from world to local (taking into account the full path of the transform we're converting about)

                //=== Instantiate the FlexProcessor component so we get hooks to update ourselves during game frames ===
                uFlex.FlexProcessor oFlexProc = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexProcessor)) as uFlex.FlexProcessor;
                oFlexProc._oFlexProcessor = this;

                //=== Add debug visualizer ===
                CVisualizeSoftBody oVisSB = CUtility.FindOrCreateComponent(gameObject, typeof(CVisualizeSoftBody)) as CVisualizeSoftBody;
                oVisSB.enabled = false;

                //=== Create the managing object and related hotspot ===
                _oObj = new CObject(this, 0, typeof(EFlexSoftBody), "SoftBody " + gameObject.name);        //###IMPROVE: Name of soft body to GUI
                _oObj.PropGroupBegin("", "", true);
                _oObj.PropAdd(EFlexSoftBody.Volume,         "Volume",       1.0f, 0.6f, 1.6f, "", CProp.Local);
                _oObj.PropAdd(EFlexSoftBody.Stiffness,      "Stiffness",    1.0f, 0.001f, 1.0f, "", CProp.Local);       //###IMPROVE: Log scale!
                _oObj.PropAdd(EFlexSoftBody.SoftBodyMass,     "Mass",         1.0f, 0.0001f, 1000.0f, "", CProp.Local);
                _oObj.FinishInitialization();
                if (GetType() != typeof(CBreastR))          //###HACK!: Right breast doesn't get hotspot (left breast gets it and manually broadcasts to right one)
                    _oHotSpot = CHotSpot.CreateHotspot(this, _oBoneAnchor, "SoftBody", false, new Vector3(0, 0.10f, 0.08f));     //###IMPROVE!!! Position offset that makes sense for that piece of clothing (from center of its verts?)

                //Debug.Break();        //###LEARN: How to pause the game in the Editor.

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
                CUtility.DestroyComponent(_oFlexToSkinnedMesh); _oFlexToSkinnedMesh = null;
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
                //###F ###BROKEN ###OBS getting normals from source body?
                //CUtility.BlenderSerialize_GetSerializableCollection("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aMapRimVerts2SourceVerts()",	out _aMapRimVerts2SourceVerts);
                //for (int nIndex = 0; nIndex < _aMapRimVerts2SourceVerts.Count;) {         // Iterate through the flattened map...
                //	int nVertID			= _aMapRimVerts2SourceVerts[nIndex++];            // The simple list has been flattened into <nVertID0, nVertSourceID0>, etc...
                //	int nVertSourceID   = _aMapRimVerts2SourceVerts[nIndex++];
                //	_memNormals.L[nVertID] = _oBody._oBodySource._memNormals.L[nVertSourceID];
                //}
                //_oMeshNow.normals   = _memNormals.L;

                ///UpdateVertsFromBlenderMesh(true);       //###NOW### ###TEMP

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
        if (_oFlexToSkinnedMesh != null)
            _oFlexToSkinnedMesh._oMeshSoftBodyRim.enabled = bShowPinningRims;
        if (_oFlexGeneratedSMR != null)
            _oFlexGeneratedSMR.enabled = bShowFlexSkinned;
        if (_oMeshFlexCollider != null)
            _oMeshFlexCollider.GetComponent<MeshRenderer>().enabled = bShowFlexColliders;        // Add a flag for this intermediate mesh?  ###DESIGN: Or delete once done?
        if (_oFlexParticlesRenderer != null)
            _oFlexParticlesRenderer.enabled = bShowFlexParticles;
        if (oMesh_Unity2Blender != null)
            oMesh_Unity2Blender.GetComponent<MeshRenderer>().enabled = false;      // Always hide this mesh... no visible value?
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

    public void OnPropSet_SoftBodyMass(float nValueOld, float nValueNew) {
        float nInvMassPerParticle = 1 / (nValueNew * _oFlexParticles.m_particlesCount);     // Note that this count is all particles (including pinned)... change to only unpinned particles for mass?
        for (int nPar = 0; nPar < _oFlexParticles.m_particlesCount; nPar++)                     //###BUG: Doesn't appear to do anything!
            _oFlexParticles.m_particles[nPar].invMass = nInvMassPerParticle;        //###BUG: Pin particles  ###F
   //     for (int nIndex = 0; nIndex < _aMapRimTetravert2Tetravert.Count; ) {            // Iterate through the rim tetraverts to update the position of their corresponding tetraverts
   //         nIndex++;   // ushort nVertTetraRim	= _aMapRimTetravert2Tetravert[nIndex++];			// The simple list has been flattened into <nVertTetraRim0, nVertTetra0>, <nVertTetraRim1, nVertTetra1>, etc...
			//ushort nVertTetra		= _aMapRimTetravert2Tetravert[nIndex++];
   //         _oSoftFlexParticles.m_particles[nVertTetra].invMass = 0;
   //     }
        if (GetType() == typeof(CBreastL))          //###HACK!: Manually call right breast equivalent from left breast... crappy hack to avoid forming a CBreasts object to broadcast to both
            _oBody._oBreastR.OnPropSet_SoftBodyMass(nValueOld, nValueNew);
        //Debug.LogFormat("SoftBody Mass {0}", nValueNew);
    }


    //---------------------------------------------------------------------------	UPDATE

    void Update() {
        if (Input.GetKeyDown(KeyCode.F9)) {         //###TEMP
            Debug.LogWarning("Updating SB mesh (###HACK!)");
            UpdateVertsFromBlenderMesh(true);
            //Vector3[] aVertsPresentation = _oMeshNow.vertices;
            //for (int nVert = 0; nVert < GetNumVerts(); nVert++)
            //    _oFlexParticles.m_particles[nVert].pos = aVertsPresentation[nVert];
        }
    }

    public void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
        //=== Reset soft body if marked for reset (e.g. during pose loading) ===
        if (_bSoftBodyInReset)
            Reset_SoftBody_DoReset();

        if (_oMeshSoftBodyRim == null)
            return;

        //=== Bake rim skinned mesh and update position of softbody tetravert pins ===
        _oMeshSoftBodyRim.Baking_UpdateBakedMesh();     // Bake the skinned portion of the mesh.  We need its verts to pin the 'pinned particles' which in turn move the 'moving particles' toward them via a spring we created in init ===

        if (_bIsFlexSkin == false) {
            _oFlexToSkinnedMesh.UpdateFlexParticleToSkinnedMesh();      // Calls _oMeshSoftBodyRim.Baking_UpdateBakedMesh() we need in this call
            //=== Bake the skinned softbody into a regular mesh (so we can update edge-of-softbody position and normals to pertinent rim verts ===
            _oFlexGeneratedSMR.BakeMesh(_oMeshFlexGenerated);                  //###OPT!!! Check how expensive this is.  Is there a way for us to move verts & normals straight from skinned mesh from Flex?  (Have not found a way so far)
            Vector3[] aVertsFlexGenerated    = _oMeshFlexGenerated.vertices;
            Vector3[] aNormalsFlexGenerated  = _oMeshFlexGenerated.normals;

            //=== Iterate through all softbody edge verts to update their position and normals.  This is critical for a 'seamless connection' between the softbody presentation mesh and the main skinned body ===
            Vector3[] aVertsRimBaked    = _oMeshSoftBodyRim._oMeshBaked.vertices;       // Obtain the verts and normals from baked rim mesh so we can manually set rim verts & normals for seamless connection to main body mesh.
            Vector3[] aNormalsRimBaked  = _oMeshSoftBodyRim._oMeshBaked.normals;
            for (int nIndex = 0; nIndex < _aMapRimVerts2Verts.Count;) {         // Iterate through the twin vert flattened map...
                ushort nVertMesh    = _aMapRimVerts2Verts[nIndex++];            // The simple list has been flattened into <nVertMesh0, nVertRim0>, <nVertMesh1, nVertRim1>, etc
                ushort nVertRim     = _aMapRimVerts2Verts[nIndex++];
                aVertsFlexGenerated  [nVertMesh] = aVertsRimBaked  [nVertRim];
                aNormalsFlexGenerated[nVertMesh] = aNormalsRimBaked[nVertRim];
            }
            _oMeshNow.vertices = aVertsFlexGenerated;
            _oMeshNow.normals  = aNormalsFlexGenerated;
        }
        else {
            ////=== Iterate through all softbody edge verts to update their position and normals.  This is critical for a 'seamless connection' between the softbody presentation mesh and the main skinned body ===
            //Vector3[] aVertsRimBaked    = _oMeshSoftBodyRim._oMeshBaked.vertices;       // Obtain the verts and normals from baked rim mesh so we can manually set rim verts & normals for seamless connection to main body mesh.
            //Vector3[] aNormalsRimBaked  = _oMeshSoftBodyRim._oMeshBaked.normals;
            //for (int nIndex = 0; nIndex < _aMapRimVerts2Verts.Count;) {         // Iterate through the twin vert flattened map...
            //    ushort nVertMesh    = _aMapRimVerts2Verts[nIndex++];            // The simple list has been flattened into <nVertMesh0, nVertRim0>, <nVertMesh1, nVertRim1>, etc
            //    ushort nVertRim     = _aMapRimVerts2Verts[nIndex++];
            //    _oFlexParticles.m_particles[nVertMesh].pos = aVertsRimBaked[nVertRim];
            //    _oFlexParticles.m_particles[nVertMesh].invMass = 0;     //###NOW###
            //    //aVertsFlexGenerated  [nVertMesh] = aVertsRimBaked  [nVertRim];
            //    //aNormalsFlexGenerated[nVertMesh] = aNormalsRimBaked[nVertRim];
            //}
            //=== Set the visible mesh verts to the particle verts (1:1 ratio) ===
            Vector3[] aVertsPresentation = _oMeshNow.vertices;
            for (int nVert = 0; nVert < GetNumVerts(); nVert++)
                aVertsPresentation[nVert] = _oFlexParticles.m_particles[nVert].pos;
            _oMeshNow.vertices = aVertsPresentation;

            _oMeshNow.RecalculateNormals();         //###NOW###
            _oMeshNow.RecalculateBounds();         //###NOW###
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
    //    public int _VertToShowSkinningBones_HACK = -1;
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


//###OBS: Was in play init!
//=== Bake the rim tetramesh a first time so its rim and tetraverts are updated to its skinned body ===
//###OBS? _oMeshRimBaked.Baking_UpdateBakedMesh();
//     //=== Pin the close-to-rim-backplate tetraverts by setting them as infinite mass.  They will be moved by us every frame (not simulated) ===
//     for (int nIndex = 0; nIndex < _aMapRimTetravert2Tetravert.Count; ) {            // Iterate through the rim tetraverts to update the position of their corresponding tetraverts
//         nIndex++;    //ushort nVertTetraRim	= _aMapRimTetravert2Tetravert[nIndex++];			// The simple list has been flattened into <nVertTetraRim0, nVertTetra0>, <nVertTetraRim1, nVertTetra1>, etc...
//ushort nVertTetra		= _aMapRimTetravert2Tetravert[nIndex++];
//         _oSoftFlexParticles.m_particles[nVertTetra].invMass = 0;                    // Remove pinned tetraverts from SoftBody simulation by setting their 1/mass to zero (infinite weight = no movement)
//         _oSoftFlexParticles.m_colours[nVertTetra] = Color.magenta;                  // Color pin verts separately so we can visualize where they are.
//     }

