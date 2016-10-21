/*###OBS: Now merged into SoftBody
- Got early version of new cunt. Now need presentation mesh.
- Try visualizer
- Neighbors now up quite high... can 'smarten it up'?  Or wait until remesh?
- Enhance Blender implementation to generate Vagina from vanilla mesh
- What is wrong with cunt mesh at inner point?  Why split??
- Add the visualizer as a possible rendering type?
*/



using UnityEngine;
using System.Collections.Generic;


public class CFlexSkin_OBS : CBMesh, IFlexProcessor {       // CFlexSkin: a specialized Flex softbody that handles like 'thick skin'
    // Created from Blender implementation that converts a regular mesh portion and gives it 'thickness' by pulling presentation mesh along its normals
    // Blender defines what is a particle and what is a shape by sending us arrays that are compatible with the Flex implementation for 'FlexShapeMatching' (e.g. softbody)

    List<int> aShapeVerts            = new List<int>();       // Array of which vert / particle is also a shape
    List<int> aShapeParticleIndices  = new List<int>();       // Flattened array of which shape match to which particle (as per Flex softbody requirements)
    List<int> aShapeParticleCutoffs  = new List<int>();       // Cutoff in 'aShapeParticleIndices' between sets defining which particle goes to which shape. 

    public string _sBlenderInstancePath_CFlexSkin;				// Blender access string to our instance (form our CBody instance)
    static string s_sNameFlexSkin_HACK;

    public static CFlexSkin_OBS Create(CBody oBody, string sNameFlexSkin) {    // Static function override from CBMesh::Create() to route to proper Blender request command
        string sBodyID = "CBodyBase_GetBodyBase(" + oBody._oBodyBase._nBodyID.ToString() + ").";
        CFlexSkin_OBS.s_sNameFlexSkin_HACK = "aFlexSkins['" + sNameFlexSkin + "']";
        string sBlenderInstancePath = CFlexSkin_OBS.s_sNameFlexSkin_HACK + ".oMeshFlexSkin";
        CGame.gBL_SendCmd("CBody", sBodyID + "CreateFlexSkin('" + sNameFlexSkin + "')");      // Create the Blender-side CCloth entity to service our requests
        CFlexSkin_OBS oFlexSkin = (CFlexSkin_OBS)CBMesh.Create(null, oBody._oBodyBase, sBlenderInstancePath, typeof(CFlexSkin_OBS));
		return oFlexSkin;
	}

	public override void OnDeserializeFromBlender() {
        base.OnDeserializeFromBlender();

        //=== Construct the fully-qualified path to the Blender CMesh instance we need ===
        _sBlenderInstancePath_CFlexSkin = CFlexSkin_OBS.s_sNameFlexSkin_HACK;
        string sBlenderInstancePath = _sBlenderInstancePath_CFlexSkin + ".oMeshFlexSkin";       // Both the visible (driven) mesh and the driving skinned Flex mesh are from the same Blender CMesh

		//=== Obtain the collections for the edge and non-edge verts that Blender calculated for us ===
		aShapeVerts				= CByteArray.GetArray_INT("'CBody'", _oBodyBase._sBlenderInstancePath_CBodyBase + "." + _sBlenderInstancePath_CFlexSkin + ".aShapeVerts.Unity_GetBytes()");
		aShapeParticleIndices	= CByteArray.GetArray_INT("'CBody'", _oBodyBase._sBlenderInstancePath_CBodyBase + "." + _sBlenderInstancePath_CFlexSkin + ".aShapeParticleIndices.Unity_GetBytes()");
		aShapeParticleCutoffs	= CByteArray.GetArray_INT("'CBody'", _oBodyBase._sBlenderInstancePath_CBodyBase + "." + _sBlenderInstancePath_CFlexSkin + ".aShapeParticleCutoffs.Unity_GetBytes()");

        ////=== Instantiate the FlexProcessor component so we get hooks to update ourselves during game frames ===
        //uFlex.FlexProcessor oFlexProc = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexProcessor)) as uFlex.FlexProcessor;
        //oFlexProc._oFlexProcessor = this;

        //=== Define Flex particles from Blender mesh made for Flex ===
        int nParticles = GetNumVerts();
        uFlex.FlexParticles oFlexParticles = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexParticles)) as uFlex.FlexParticles;
        oFlexParticles.m_particlesCount = nParticles;
        oFlexParticles.m_particles = new uFlex.Particle[nParticles];
        oFlexParticles.m_colours = new Color[nParticles];
        oFlexParticles.m_velocities = new Vector3[nParticles];
        oFlexParticles.m_densities = new float[nParticles];
        oFlexParticles.m_particlesActivity = new bool[nParticles];
        oFlexParticles.m_colour = Color.green;                //###TODO: Colors!
        oFlexParticles.m_interactionType = uFlex.FlexInteractionType.SelfCollideFiltered;
        oFlexParticles.m_collisionGroup = -1;
        //part.m_bounds.SetMinMax(min, max);            //###IMPROVE Bounds?
        for (int nParticle = 0; nParticle < nParticles; nParticle++) {
            oFlexParticles.m_particles[nParticle].pos = _memVerts.L[nParticle];
            oFlexParticles.m_particles[nParticle].invMass = 1;            //###TODO: Mass
            oFlexParticles.m_colours[nParticle] = oFlexParticles.m_colour;
            oFlexParticles.m_particlesActivity[nParticle] = true;
        }

        //=== Define Flex shapes from the Blender particles that have been set as shapes too ===
        int nShapes = aShapeVerts.Count;
        uFlex.FlexShapeMatching oFlexShapeMatching = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexShapeMatching)) as uFlex.FlexShapeMatching;
        oFlexShapeMatching.m_shapesCount = nShapes;
        oFlexShapeMatching.m_shapeIndicesCount = aShapeParticleIndices.Count;
        oFlexShapeMatching.m_shapeIndices = aShapeParticleIndices.ToArray();            //###LEARN: How to convert a list to a straight .Net array.
        oFlexShapeMatching.m_shapeOffsets = aShapeParticleCutoffs.ToArray();
        oFlexShapeMatching.m_shapeCenters = new Vector3[nShapes];
        oFlexShapeMatching.m_shapeCoefficients = new float[nShapes];
        oFlexShapeMatching.m_shapeTranslations = new Vector3[nShapes];
        oFlexShapeMatching.m_shapeRotations = new Quaternion[nShapes];
        oFlexShapeMatching.m_shapeRestPositions = new Vector3[oFlexShapeMatching.m_shapeIndicesCount];
        
        //=== Calculate shape centers from attached particles ===
        int nShapeStart = 0;
        for (int nShape = 0; nShape < oFlexShapeMatching.m_shapesCount; nShape++) {
            oFlexShapeMatching.m_shapeCoefficients[nShape] = 0.05f;                   //###TODO

            int nShapeEnd = oFlexShapeMatching.m_shapeOffsets[nShape];
            Vector3 vecCenter = Vector3.zero;
            for (int nShapeIndex = nShapeStart; nShapeIndex < nShapeEnd; ++nShapeIndex) {
                int nParticle = oFlexShapeMatching.m_shapeIndices[nShapeIndex];
                Vector3 vecParticlePos = oFlexParticles.m_particles[nParticle].pos;          // remap indices and create local space positions for each shape
                vecCenter += vecParticlePos;
            }
            vecCenter /= (nShapeEnd - nShapeStart);       //###TODO Off by one??
            oFlexShapeMatching.m_shapeCenters[nShape] = vecCenter;
            nShapeStart = nShapeEnd;
        }
        
        //=== Set the shape rest positions ===
        nShapeStart = 0;
        int nShapeIndexOffset = 0;
        for (int nShape = 0; nShape < oFlexShapeMatching.m_shapesCount; nShape++) {
            int nShapeEnd = oFlexShapeMatching.m_shapeOffsets[nShape];
            for (int nShapeIndex = nShapeStart; nShapeIndex < nShapeEnd; ++nShapeIndex) {
                int nParticle = oFlexShapeMatching.m_shapeIndices[nShapeIndex];
                Vector3 vecParticle = oFlexParticles.m_particles[nParticle].pos;          // remap indices and create local space positions for each shape
                oFlexShapeMatching.m_shapeRestPositions[nShapeIndexOffset] = vecParticle - oFlexShapeMatching.m_shapeCenters[nShape];
                nShapeIndexOffset++;
            }
            nShapeStart = nShapeEnd;
        }

        //=== Add particle renderer ===
        uFlex.FlexParticlesRenderer partRend = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexParticlesRenderer)) as uFlex.FlexParticlesRenderer;
        partRend.m_size = CGame.INSTANCE.particleSpacing;
        partRend.m_radius = partRend.m_size / 2.0f;
        partRend.enabled = false;           // Hidden by default

        //=== Add visualizer ===
        CVisualizeSoftBody oVisSB = CUtility.FindOrCreateComponent(gameObject, typeof(CVisualizeSoftBody)) as CVisualizeSoftBody;
        //oVisSB.enabled = false;
    }

    public void HideShowMeshes() {
        //###IMPROVE ###DESIGN Collect show/hide flags in a global array?
        GetComponent<MeshRenderer>().enabled = CGame.INSTANCE.ShowPresentation;
        if (GetComponent<uFlex.FlexParticlesRenderer>() != null)
            GetComponent<uFlex.FlexParticlesRenderer>().enabled = CGame.INSTANCE.ShowFlexParticles;
    }


    public void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
    }
}









/*###OBSOLETE: Old implementation of CFlexSkin where three triangulated Flex particles would pull with three springs from a skinned mesh would pull to the desired position the same verts of a Flex-simulated verts / particles.  Failed effort due to vert crunching
- WTF can't collide against penis rigid body??
- Triangulated particule positions are not global... (pose changes offset!)
- What to do about the other two contexts??  Can use the vagina approach or must revert old ideas?
    - If new idea, then only need one (not triangulated)... or go for 3 too?
- WTF wrong with vagina inner vert?
- Problem: All particles completely independant!  Could cause extreme stretching (adjacent verts don't pull other nearby!) -> switch to cloth?
- recalc bounds... switch off culling!
- Re-iterate through the code to clarify
- Would be nice to have a solid Flex dildo we could manipulate! :)
- Appears expensive?

*/



//public class CFlexSkin : CBMesh, IFlexProcessor {       // CFlexSkin: Creates a hybrid flex-simulated / skinned mesh of the same mesh were the skinned duplicate drives the (non-simulated) Flex particles that in turn drive Flex springs that in turn drive the (simulated) Flex particles driving the visible presentation mesh.
//    // Used to provide a part of the body's skin as a 2D mesh that is Flex-simulated instead of totally rigid like the skinned body.  Currently used for vagina

//    CFlexSkinnedSpringDriver_OBSOLETE _oFlexSkinnedSpringDriver;
//    uFlex.FlexParticles _oFlexParticles_Driven;
//    List<ushort> aVertsEdge     = new List<ushort>();       // The verts on the edge of the FlexSkin are only used to manually set the verts+normals of the presentation match to provide a seamless integration with the main skinned body
//    List<ushort> aVertsNonEdge  = new List<ushort>();       // The verts not on the edge of the FlexSkin are used to drive (non-simulated) Flex particles that in turn drive Flex springs that in turn guide the movement of the simulated verts of the presentation mesh.

//    public string _sBlenderInstancePath_CFlexSkin;				// Blender access string to our instance (form our CBody instance)
//    static string s_sNameFlexSkin_HACK;

//    public static CFlexSkin Create(CBody oBody, string sNameFlexSkin) {    // Static function override from CBMesh::Create() to route to proper Blender request command
//        string sBodyID = "CBodyBase_GetBodyBase(" + oBody._oBodyBase._nBodyID.ToString() + ").";
//        CFlexSkin.s_sNameFlexSkin_HACK = "aFlexSkins['" + sNameFlexSkin + "']";
//        string sBlenderInstancePath = CFlexSkin.s_sNameFlexSkin_HACK + ".oMeshFlexSkin";
//        CGame.gBL_SendCmd("CBody", sBodyID + "CreateFlexSkin('" + sNameFlexSkin + "')");      // Create the Blender-side CCloth entity to service our requests
//        CFlexSkin oFlexSkin = (CFlexSkin)CBMesh.Create(null, oBody, sBlenderInstancePath, typeof(CFlexSkin));
//		return oFlexSkin;
//	}

//	public override void OnDeserializeFromBlender() {
//        base.OnDeserializeFromBlender();

//        //=== Construct the fully-qualified path to the Blender CMesh instance we need ===
//        _sBlenderInstancePath_CFlexSkin = CFlexSkin.s_sNameFlexSkin_HACK;
//        string sBlenderInstancePath = _sBlenderInstancePath_CFlexSkin + ".oMeshFlexSkin";       // Both the visible (driven) mesh and the driving skinned Flex mesh are from the same Blender CMesh

//        //=== Obtain the collections for the edge and non-edge verts that Blender calculated for us ===
//        CUtility.BlenderSerialize_GetSerializableCollection("'CBody'", _oBody._sBlenderInstancePath_CBody + "." + _sBlenderInstancePath_CFlexSkin + ".SerializeCollection_aVertsEdge()",    out aVertsEdge);
//        CUtility.BlenderSerialize_GetSerializableCollection("'CBody'", _oBody._sBlenderInstancePath_CBody + "." + _sBlenderInstancePath_CFlexSkin + ".SerializeCollection_aVertsNonEdge()", out aVertsNonEdge);

//        //=== Create the driven particles.  These have a 1:1 mapping to the verts in our non-skinned mesh ===
//        _oFlexParticles_Driven = CUtility.CreateFlexParticles(gameObject, aVertsNonEdge.Count, uFlex.FlexInteractionType.SelfCollideFiltered, Color.yellow);

//        //=== Create the skinned spring driver component responsible to guide selected Flex particles to skinned-mesh positions ===
//        _oFlexSkinnedSpringDriver  = CFlexSkinnedSpringDriver_OBSOLETE.Create(_oBody, sBlenderInstancePath, ref aVertsNonEdge, ref aVertsNonEdge, ref _oFlexParticles_Driven);         // Pass in non-edge collection twice as both driver and driven mesh are the same.
        
//        //=== Instantiate the FlexProcessor component so we get hooks to update ourselves during game frames ===
//        uFlex.FlexProcessor oFlexProc = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexProcessor)) as uFlex.FlexProcessor;
//        oFlexProc._oFlexProcessor = this;

//        //_oMeshNow.bounds.SetMinMax(new Vector3(-100, -100, -100), new Vector3(100, 100, 100));              // Set bounds very large so we always draw and not have to recalc bounds at every frame  ###IMPROVE: A flag for always draw??
//    }

//    public void HideShowMeshes() {
//        //###IMPROVE ###DESIGN Collect show/hide flags in a global array?
//        GetComponent<MeshRenderer>().enabled = CGame.INSTANCE.ShowPresentation;
//        if (_oFlexSkinnedSpringDriver != null)
//            _oFlexSkinnedSpringDriver._oSMR_Driver.enabled = CGame.INSTANCE.ShowPinningRims;
//        if (GetComponent<uFlex.FlexParticlesRenderer>() != null)
//            GetComponent<uFlex.FlexParticlesRenderer>().enabled = CGame.INSTANCE.ShowFlexParticles;
//        if (_oFlexSkinnedSpringDriver.GetComponent<uFlex.FlexParticlesRenderer>() != null)
//            _oFlexSkinnedSpringDriver.GetComponent<uFlex.FlexParticlesRenderer>().enabled = CGame.INSTANCE.ShowFlexParticles;
//    }


//    public void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
//        //=== Delegate the update to our spring driver so our visible mesh gets driven by Flex springs ===
//        _oFlexSkinnedSpringDriver.UpdateFlexParticleToSkinnedMesh();

//        //=== Update the position of our the non-edge verts from the current position our driven particles ===
//        for (int nMappingNonEdge = 0; nMappingNonEdge < aVertsNonEdge.Count; nMappingNonEdge++) {
//            ushort nVertNonEdge = aVertsNonEdge[nMappingNonEdge];
//            _memVerts.L[nVertNonEdge] = _oFlexParticles_Driven.m_particles[nMappingNonEdge].pos;
//        }

//        //=== Update the position of the edge verts  ===
//        _oMeshNow.RecalculateNormals();         //###IMPROVE: Do occasionally?  ###OPT!!!
//        Vector3[] aVertsSkinned = _oFlexSkinnedSpringDriver._oBSkinBaked_Driver._oMeshBaked.vertices;        // Edge verts are updated from baked skinned mesh.
//        Vector3[] aNormalsSkinned = _oFlexSkinnedSpringDriver._oBSkinBaked_Driver._oMeshBaked.normals;
//        for (int nMappingEdge = 0; nMappingEdge < aVertsEdge.Count; nMappingEdge++) {
//            ushort nVertEdge = aVertsEdge[nMappingEdge];
//            _memVerts.L  [nVertEdge] = aVertsSkinned[nVertEdge];
//            _memNormals.L[nVertEdge] = aNormalsSkinned[nVertEdge];
//        }
//        _oMeshNow.vertices = _memVerts.L;       //###CHECK: Best way?
//        _oMeshNow.normals = _memNormals.L;     // Set the normals from skinned for Flex-calculated  ###BUG!! Small differences during deformation!!
//        //_oMeshNow.normals = aNormalSkinned;     // Set the normals from skinned for Flex-calculated  ###BUG!! Small differences during deformation!!
//        _oMeshNow.RecalculateBounds();      //###OPT!!!!!  ###IMPROVE: Disable hiding mesh!
//    }
//}
