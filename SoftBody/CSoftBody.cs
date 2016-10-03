 /*###DISCUSSION: Soft Body
 === TODAY ====
- CArray and serialize functions
- from x import *
- Cleanup old skin implementation
- Massive work on reduction of body flex collider: We need to remove main body FlexCollider verts as we detach presentation meshes.
- Merge of thick skin into SoftBody subclass... then vagina... then penetration!
- Need to destroy everything when soft bodies no longer good? (e.g. for morphing)
    - For main body morphin and bodysuit refit we could go with intelligent FlexBodyCollider with breasts & penis being handled separately?  (LATER)
- Rethink 'FinishInit()'
- Fix visualization

=== NEXT ===

=== TODO ===
- RETHINK GAME MODES!!!!!!!
- Totally have to clean up the old crappy collider shit from Blender, breasts and penis!
- Cleanup horrible mess with mass
- Work on comprehensive destruction in Unity & Blender that is AS SIMPLE AS POSSIBLE! (Delete everything!)
=== LATER ===

=== IMPROVE ===

=== DESIGN ===

=== IDEAS ===
- We need to create a 'Flex Body Collider' mesh in Blender that has chunks removed from it as we remove soft bodies...
    - This mesh can assist creation of Vagina collision mesh?
    -+++IDEA! Have an intermediate 'bone mesh' at collider level that 'skins' the visible mesh!!!
        - 1. Blender constructs a 'pulled simplified collision mesh' from the visible soft body mesh. (detail smoothed out or entire mesh re-meshed)
        - 2. Flex constructs its particles & springs from this simplified mesh that collides against particles further than we'd like.
        - 3. Blender recieves this collision mesh to skin the visible mesh to it.
        - 4. At each frame the resultant 'simplified Flex softbody mesh' (itself skinned from Flex shapes) is the base mesh to skin the visible mesh.
        - Q: Can avoid 2 layers of skinning (bridging them for 1 skinning?)

=== LEARNED ===

=== PROBLEMS ===
- Unity2Blender not destroyed in Blender?
=== PROBLEMS??? ===

=== WISHLIST ===
- Port dildo into our full classes for its benefits?

=== OTHER AREAS ===
- Blender delete game meshes broken (not hierarchical)
*/




using UnityEngine;
using System;
using System.Collections.Generic;


public class CSoftBody : CSoftBodyBase {
    // Manages a single soft body object send to Flex implementation for soft body simulation.  These 'body parts' (such as breasts, penis, vagina) 
    //... are conneted to the main body skinned mesh via _oMeshRimBaked which pins this softbody's particles to those skinned from the main body

	//---------------------------------------------------------------------------	MEMBERS
    CPinnedParticles    _oPinnedParticles;                      // Special component in charge of 'pinning' through Flex springs some of the Flex particles in this softbody to their appropriate position on the body's surface
    CBMesh              _oMeshFlexCollider;                     // The 'collision' mesh fed to Flex.  It as a 'shrunken version' of the appearance mesh _oMeshNow by half the Flex collision margin so that the visible mesh appears to collide with other particles much closer than if collision mesh was rendered to the user.  (Created by Blender by a 'shrink' operation)
    SkinnedMeshRenderer _oFlexGeneratedSMR;                     // The skinned mesh generated by Flex and its internal skinning.  We bake this mesh everyframe and modify the edge verts for seamless connection to main skinned body
	Mesh				_oFlexGeneratedSMR_BakedMesh;           // The mesh recipent of GeneratedSMR above.  //###LEARN: Cannot initialize this member with = new Mesh() because when we create this component we get 'Unity Internal_Create is not allowed to be called from a MonoBehaviour constructor (or instance field initializer), call it in Awake or Start instead'

	//---------------------------------------------------------------------------	INIT

	public static CSoftBody Create(CBody oBody, Type oTypeBMesh, string sNameBoneAnchor_HACK) { 
		string sNameSoftBody = oTypeBMesh.Name.Substring(1);                            // Obtain the name of our detached body part ('Breasts', 'Penis', 'Vagina') from a substring of our class name.  Must match Blender!!  ###WEAK?
        _sNameBoneAnchor_HACK = sNameBoneAnchor_HACK;
        CGame.gBL_SendCmd("CBody", "CBody_GetBody(" + oBody._nBodyID.ToString() + ").CreateSoftBody('" + sNameSoftBody + "', " + CGame.INSTANCE.nSoftBodyFlexColliderShrinkRatio.ToString() + ")");      // Separate the softbody from the source body.
		CSoftBody oSoftBody = (CSoftBody)CBMesh.Create(null, oBody, "aSoftBodies['" + sNameSoftBody + "'].oMeshSoftBody", oTypeBMesh);       // Create the softbody mesh from the just-created Blender mesh.
        return oSoftBody;
    }

	public override void OnDeserializeFromBlender() {
        base.OnDeserializeFromBlender();                    // Call important base class first to serialize rim, pinned particle mesh, etc

        //=== Create the collision mesh from Blender ===
        _oMeshFlexCollider = CBMesh.Create(null, _oBody, _sBlenderInstancePath_CSoftBody + ".oMeshFlexCollider", typeof(CBMesh));       // Also obtain the Unity2Blender mesh call above created.
        _oMeshFlexCollider.GetComponent<MeshRenderer>().enabled = false;      // Collider does not render... only for Flex definition!
        _oMeshFlexCollider.transform.SetParent(transform);

        //=== Construct the Flex solid body to obtain the particles that need further processing Blender for pinning ===
        CFlex.CreateFlexObject(gameObject, _oMeshNow, _oMeshFlexCollider._oMeshNow, uFlex.FlexBodyType.Soft, uFlex.FlexInteractionType.SelfCollideFiltered, CGame.INSTANCE.nMassSoftBody, Color.red);

        //=== Obtain references to the components we'll need at runtime ===
        _oFlexParticles         = GetComponent<uFlex.FlexParticles>();              //###WEAK: Owned by base class, defined here
        _oFlexParticlesRenderer = GetComponent<uFlex.FlexParticlesRenderer>();
        _oFlexShapeMatching     = GetComponent<uFlex.FlexShapeMatching>();
        _oFlexGeneratedSMR      = GetComponent<SkinnedMeshRenderer>();

        //=== Ask Blender to create a 'Unity2Blender' mesh of the right number of verts so we can upload our Tetramesh to Blender for processing there ===
        int nVertTetras = _oFlexParticles.m_particlesCount;
        CGame.gBL_SendCmd("CBody", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".CreateMesh_Unity2Blender(" + nVertTetras.ToString() + ")");        // Our softbody instance will now have its 'oMeshUnity2Blender' member populated with a temporary mesh of exactly nVertTetras verts

        //=== Obtain the Unity2Blender mesh so we can pass particles to Blender for processing there ===
        CBMesh oMesh_Unity2Blender = CBMesh.Create(null, _oBody, _sBlenderInstancePath_CSoftBody + ".oMeshUnity2Blender", typeof(CBMesh), true);       // Also obtain the Unity2Blender mesh call above created.    // Keep link to Blender mesh open so we can upload our verts        //###IMPROVE: When/where to release??

        //=== Upload our particles to Blender so it can select those that are pinned and skin them ===
        for (int nVertTetra = 0; nVertTetra < nVertTetras; nVertTetra++)
			oMesh_Unity2Blender._memVerts.L[nVertTetra] = _oFlexParticles.m_particles[nVertTetra].pos;			//###LEARN: For some reason this is safe to do while still copying back to Blender... why?
		oMesh_Unity2Blender.UpdateVertsToBlenderMesh();                // Blender now has our particles.  It can now find the particles near the rim and skin them

        //=== Create and retrieve the softbody rim mesh responsible to pin softbody to skinned body ===
        float nDistParticlesFromBackmesh = CGame.INSTANCE.particleSpacing * CGame.INSTANCE.nDistParticlesFromBackmeshMult;
        CGame.gBL_SendCmd("CBody", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".FindPinnedFlexParticles(" + nDistParticlesFromBackmesh.ToString() + ")");        // Ask Blender select the particles near the rim and skin them
        Destroy(oMesh_Unity2Blender);       // We're done with Unity2Blender mesh after FindPinnedFlexParticles... delete
        oMesh_Unity2Blender = null;

        //=== Retreive the pinned particles skinned mesh so we can manually set the position of the pinned particles to the appropriate position on the skinned main body (so softbody doesn't float into space) ===
        _oMeshPinnedParticles = (CBSkinBaked)CBMesh.Create(null, _oBody, _sBlenderInstancePath_CSoftBody + ".oMeshPinnedParticles", typeof(CBSkinBaked));           // Retrieve the skinned softbody rim mesh Blender just created so we can pin softbody at runtime
        _oMeshPinnedParticles.transform.SetParent(transform);

        ////=== Obtain the map of pinned particles in skinned mesh to softbody particles in whole softbody mesh ===
        List<ushort> aMapPinnedParticles;
        CUtility.BlenderSerialize_GetSerializableCollection_USHORT("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aMapPinnedParticles()",	out aMapPinnedParticles);		// Read the particle traversal map from our CSoftBody instance

        //=== Create the Flex-to-skinned-mesh component responsible to guide selected Flex particles to skinned-mesh positions ===
        _oPinnedParticles = CUtility.FindOrCreateComponent(gameObject, typeof(CPinnedParticles)) as CPinnedParticles;
        _oPinnedParticles.Initialize(ref aMapPinnedParticles, _oMeshPinnedParticles);

		//=== Create our baked-recipient mesh ===
		_oFlexGeneratedSMR_BakedMesh = new Mesh();             // The mesh baked at every frame by _oFlexGeneratedSMR
	}

	public override void OnDestroy() {
		base.OnDestroy();
	}



    //--------------------------------------------------------------------------	UPDATE
    public override void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {

		base.PreContainerUpdate(solver, cntr, parameters);

        _oPinnedParticles.UpdatePositionsOfPinnedParticles();                // Update the position of our pinned particles so this softbody follows the surface of its body

        //=== Bake the skinned softbody into a regular mesh (so we can update edge-of-softbody position and normals to pertinent rim verts ===
        _oFlexGeneratedSMR.BakeMesh(_oFlexGeneratedSMR_BakedMesh);                  //###OPT!!! Check how expensive this is.  Is there a way for us to move verts & normals straight from skinned mesh from Flex?  (Have not found a way so far)
        Vector3[] aVertsFlexGenerated = _oFlexGeneratedSMR_BakedMesh.vertices;
        Vector3[] aNormalsFlexGenerated = _oFlexGeneratedSMR_BakedMesh.normals;

        UpdateVisibleSoftBodySurfaceMesh(ref aVertsFlexGenerated, ref aNormalsFlexGenerated);      // Call base class to update the visible mesh (and manually adjust sb rim verts for seamless connection to main skinned body)
    }


    //--------------------------------------------------------------------------	UTILITY
    public override void HideShowMeshes(bool bShowPresentation, bool bShowPhysxColliders, bool bShowMeshStartup, bool bShowPinningRims, bool bShowFlexSkinned, bool bShowFlexColliders, bool bShowFlexParticles) {
        base.HideShowMeshes(bShowPresentation, bShowPhysxColliders, bShowMeshStartup, bShowPinningRims, bShowFlexSkinned, bShowFlexColliders, bShowFlexParticles);
        ////###IMPROVE ###DESIGN Collect show/hide flags in a global array?
        GetComponent<MeshRenderer>().enabled = bShowPresentation;
        if (_oFlexGeneratedSMR != null)
            _oFlexGeneratedSMR.enabled = bShowFlexSkinned;
        if (_oMeshFlexCollider != null)
            _oMeshFlexCollider.GetComponent<MeshRenderer>().enabled = bShowFlexColliders;        // Add a flag for this intermediate mesh?  ###DESIGN: Or delete once done?
    }
}
