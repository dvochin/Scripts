/*
=== CSoftBodySkin design decisions ===
- CSoftBodySkin skinning:
    - Done in Blender by distance. (not geometry)
    - Q: Do we need to 'push back' near Flex collider mesh temporarily to skin depth during pose binding?
        - Try this as a last resort... might work with collider a particle distance away.

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

using UnityEngine;
using System;
using System.Collections.Generic;

public class CSoftBodySkin : CSoftBodyBase
{
    List<int> aShapeVerts            = new List<int>();       // Array of which vert / particle is also a shape
    List<int> aShapeParticleIndices  = new List<int>();       // Flattened array of which shape match to which particle (as per Flex softbody requirements)
    List<int> aShapeParticleCutoffs  = new List<int>();       // Cutoff in 'aShapeParticleIndices' between sets defining which particle goes to which shape. 


                    ////=== Obtain the collections for the edge and non-edge verts that Blender calculated for us ===
                    //CUtility.BlenderSerialize_GetSerializableCollection_INT("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aShapeVerts()",              out aShapeVerts);
                    //CUtility.BlenderSerialize_GetSerializableCollection_INT("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aShapeParticleIndices()",    out aShapeParticleIndices);
                    //CUtility.BlenderSerialize_GetSerializableCollection_INT("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aShapeParticleCutoffs()",    out aShapeParticleCutoffs);

                    ////=== Define Flex particles from Blender mesh made for Flex ===
                    //int nParticles = GetNumVerts();
                    //_oFlexParticles = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexParticles)) as uFlex.FlexParticles;
                    //_oFlexParticles.m_particlesCount = nParticles;
                    //_oFlexParticles.m_particles = new uFlex.Particle[nParticles];
                    //_oFlexParticles.m_colours = new Color[nParticles];
                    //_oFlexParticles.m_velocities = new Vector3[nParticles];
                    //_oFlexParticles.m_densities = new float[nParticles];
                    //_oFlexParticles.m_particlesActivity = new bool[nParticles];
                    //_oFlexParticles.m_colour = Color.green;                //###TODO: Colors!
                    //_oFlexParticles.m_interactionType = uFlex.FlexInteractionType.SelfCollideFiltered;
                    //_oFlexParticles.m_collisionGroup = -1;
                    ////part.m_bounds.SetMinMax(min, max);            //###IMPROVE Bounds?
                    //for (int nParticle = 0; nParticle < nParticles; nParticle++) {
                    //    _oFlexParticles.m_particles[nParticle].pos = _memVerts.L[nParticle];
                    //    //_oFlexParticles.m_particles[nParticle].invMass = 1;            //###TODO: Mass
                    //    //_oFlexParticles.m_particles[nParticle].invMass = 0;            //###TODO: Mass
                    //    _oFlexParticles.m_colours[nParticle] = _oFlexParticles.m_colour;
                    //    _oFlexParticles.m_particlesActivity[nParticle] = true;
                    //}

                    ////=== Define Flex shapes from the Blender particles that have been set as shapes too ===
                    //int nShapes = aShapeVerts.Count;
                    //_oFlexShapeMatching = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexShapeMatching)) as uFlex.FlexShapeMatching;
                    //_oFlexShapeMatching.m_shapesCount = nShapes;
                    //_oFlexShapeMatching.m_shapeIndicesCount = aShapeParticleIndices.Count;
                    //_oFlexShapeMatching.m_shapeIndices = aShapeParticleIndices.ToArray();            //###LEARN: How to convert a list to a straight .Net array.
                    //_oFlexShapeMatching.m_shapeOffsets = aShapeParticleCutoffs.ToArray();
                    //_oFlexShapeMatching.m_shapeCenters = new Vector3[nShapes];
                    //_oFlexShapeMatching.m_shapeCoefficients = new float[nShapes];
                    //_oFlexShapeMatching.m_shapeTranslations = new Vector3[nShapes];
                    //_oFlexShapeMatching.m_shapeRotations = new Quaternion[nShapes];
                    //_oFlexShapeMatching.m_shapeRestPositions = new Vector3[_oFlexShapeMatching.m_shapeIndicesCount];

                    ////=== Calculate shape centers from attached particles ===
                    ////int nShape = 0;
                    ////foreach (int nShapeParticle in aShapeVerts) {
                    ////    _oFlexShapeMatching.m_shapeCoefficients[nShape] = 0.05f;                   //###NOW###
                    ////    _oFlexShapeMatching.m_shapeCenters[nShape] = _oFlexParticles.m_particles[nShapeParticle].pos;
                    ////    nShape++;
                    ////}

                    //int nShapeStart = 0;
                    //for (int nShape = 0; nShape < _oFlexShapeMatching.m_shapesCount; nShape++) {
                    //    _oFlexShapeMatching.m_shapeCoefficients[nShape] = 0.05f;                   //###NOW###   Calculate shape center here or just accept particle pos?

                    //    int nShapeEnd = _oFlexShapeMatching.m_shapeOffsets[nShape];
                    //    Vector3 vecCenter = Vector3.zero;
                    //    for (int nShapeIndex = nShapeStart; nShapeIndex < nShapeEnd; ++nShapeIndex) {
                    //        int nParticle = _oFlexShapeMatching.m_shapeIndices[nShapeIndex];
                    //        Vector3 vecParticlePos = _oFlexParticles.m_particles[nParticle].pos;          // remap indices and create local space positions for each shape
                    //        vecCenter += vecParticlePos;
                    //    }

                    //    vecCenter /= (nShapeEnd - nShapeStart);
                    //    _oFlexShapeMatching.m_shapeCenters[nShape] = vecCenter;
                    //    nShapeStart = nShapeEnd;
                    //}

                    ////=== Set the shape rest positions ===
                    //nShapeStart = 0;
                    //int nShapeIndexOffset = 0;
                    //for (int nShape = 0; nShape < _oFlexShapeMatching.m_shapesCount; nShape++) {
                    //    int nShapeEnd = _oFlexShapeMatching.m_shapeOffsets[nShape];
                    //    for (int nShapeIndex = nShapeStart; nShapeIndex < nShapeEnd; ++nShapeIndex) {
                    //        int nParticle = _oFlexShapeMatching.m_shapeIndices[nShapeIndex];
                    //        Vector3 vecParticle = _oFlexParticles.m_particles[nParticle].pos;          // remap indices and create local space positions for each shape
                    //        _oFlexShapeMatching.m_shapeRestPositions[nShapeIndexOffset] = vecParticle - _oFlexShapeMatching.m_shapeCenters[nShape];
                    //        nShapeIndexOffset++;
                    //    }
                    //    nShapeStart = nShapeEnd;
                    //}

                    //foreach (int nShapeParticle in aShapeVerts) {
                    //    _oFlexParticles.m_particles[nShapeParticle].invMass = 1;          //###NOW###
                    //}

                    ////=== Add particle renderer ===
                    //_oFlexParticlesRenderer = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexParticlesRenderer)) as uFlex.FlexParticlesRenderer;
                    //_oFlexParticlesRenderer.m_size = CGame.INSTANCE.particleSpacing;
                    //_oFlexParticlesRenderer.m_radius = _oFlexParticlesRenderer.m_size / 2.0f;
                    //_oFlexParticlesRenderer.enabled = false;           // Hidden by default

    public override void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
        base.PreContainerUpdate(solver, cntr, parameters);
        //////=== Iterate through all softbody edge verts to update their position and normals.  This is critical for a 'seamless connection' between the softbody presentation mesh and the main skinned body ===
        ////Vector3[] aVertsRimBaked    = _oMeshSoftBodyRim._oMeshBaked.vertices;       // Obtain the verts and normals from baked rim mesh so we can manually set rim verts & normals for seamless connection to main body mesh.
        ////Vector3[] aNormalsRimBaked  = _oMeshSoftBodyRim._oMeshBaked.normals;
        ////for (int nIndex = 0; nIndex < _aMapRimVerts.Count;) {         // Iterate through the twin vert flattened map...
        ////    ushort nVertMesh    = _aMapRimVerts[nIndex++];            // The simple list has been flattened into <nVertMesh0, nVertRim0>, <nVertMesh1, nVertRim1>, etc
        ////    ushort nVertRim     = _aMapRimVerts[nIndex++];
        ////    _oFlexParticles.m_particles[nVertMesh].pos = aVertsRimBaked[nVertRim];
        ////    _oFlexParticles.m_particles[nVertMesh].invMass = 0;     //###NOW###
        ////    //aVertsFlexGenerated  [nVertMesh] = aVertsRimBaked  [nVertRim];
        ////    //aNormalsFlexGenerated[nVertMesh] = aNormalsRimBaked[nVertRim];
        ////}
        ////=== Set the visible mesh verts to the particle verts (1:1 ratio) ===
        //Vector3[] aVertsPresentation = _oMeshNow.vertices;
        //for (int nVert = 0; nVert < GetNumVerts(); nVert++)
        //    aVertsPresentation[nVert] = _oFlexParticles.m_particles[nVert].pos;
        //_oMeshNow.vertices = aVertsPresentation;

        //_oMeshNow.RecalculateNormals();         //###NOW###
        //_oMeshNow.RecalculateBounds();         //###NOW###
    }
}
