/*
- Now: 2nd to Edge particles don't appear to move when skinned... distorting the mesh!!

- Have Blender construct a collision-mesh-centric data structure that is sent wholesale to Unity.  It contains:
	- SkinnedParticleID
	- SimulatedParticleID
	- ShapeID
	- PresVertID
	- Flags: HasSkinnedParticle, HasSimulatedParticle, IsBone
	- BoneID
	- Neighbors
- Unity gets all that, creates the particles as needed from flags and neighbor info
	- Could also specialize Particle class so visualizer can render more info?
- Iteration to feed skinned info into shapes just iterates through all collision verts (by their ID) and sets the positions of 'SkinnedParticleID'
- Iteration to set verts of presentation iterates through all collision verts and for those that have a 'SimulatedParticleID' set the position of 'PresVertID' to it.
- Collision verts that are bones set their bone.
	- Once that submesh is baked the results are copied back to other mesh
- Could process rim normal setting directly from this loop... so remove dependency on CSoftBodyBase and merge these two?



=== NEW DESIGN ===
- New implementation capable of efficiently providing Flex collision and presentation rendering with penetration.  
- There are two areas to the collider rig & presentation mesh:
	- The normal skin area: part of the collider rig where each shape links to particles / verts of their adjacent particle / vert AND a skinned particle that keeps each shape close to where it would be on the skinned body.
	- The penetration area: part of the collider rig where each shape links to particles / verts of their adjacent particle / vert and NOT a skinned particle that would have kept it close to its skinned position.
		- The each particle in the penetration part of the collider rig also has extra an extra 'spring' to an 'anchor particle' preventing the penetration area from going into or out of the body too far.
	- The penetration part of the rig has a 'hexagon' type structure with each particle of the hexagon only in contact with their adjacent one (e.g. not accross) so penetration can occur.
		- The particles are spaced at init time as a 'flattened hexagon' (taller than wider) with the spacing set to the minimum inter-particule distance.
		- These particles are skinned differently from the others.  The skinned verts depending on them have their 'rest pose' at the legal flex position (spaced appart) with the presentation mesh at its startup position.

- Steps:
	- Create a vert group in Blender for the whole patch and for the inner penetration area
	- Duplicate the code for softbody soft pining
	- Create the pinned particle, the free particles, and the shape that binds them.
	- For the penetration area create the same but no link to pinned particle
	- See how that runs around penetration area... then we need to change the geometry to insert a hexagon instead.

IDEA: have Blender create fully-configured Flex objects with Unity being a thin / dumb client!

=== TODAY ===
# Continue work in Unity to process rim and draw sb mesh.  Need to tag which verts of rim are for pinning and their map to their associated vert in sb mesh.
# So need a map from one domain to the other.  Then add extra particle to each shape for skinned position and add update loop to update their pos every frame...
# then catch which vert in each shape is the vert used for rendering the update the visible mesh with that.  (and apply particle distance to it...) 
###DEV19: Remember the extra rim!
###DEV19: Remove all groups from rim!
###IDEA: For sb can have the skinned back plate added to the rim?

###PROBLEM: Why does Blender body mesh keep duplicating verts along material seams??? We remove double early and they keep getting re-created!!  (Interesting that our Unity normal adjustment accross seams holds!!)
- Getting close but shimmer at particle lever a huge concern!
	- Won't be so bad when a huge dick inserts?
	- Design backplate for real dick penetration and retest
	- Also play with stiffness and mass... Q: is there friction param?
	- Distance really smoothings things up!  Short dist on skin part and wide on vagina could do it!!!
- Now need to skin back to body!  (Different skin than rim?)
- Material on mesh... and default to renderers!
- Quickly setting stiffness and mass would be good!
- Move to own file!!

=== QUESTIONS ===
- 

=== Overview of the entire CSoftBodySkin workflow ===
- A 'backmesh' is created from the Blender soft body submesh.  (Both presentation, Flex collider and backmesh have same topology)
- For now this backmesh is user created, soon it will hopefully be programmatically generated from source body
- Q: What to do about virgin body not having holes for vagina / anus?
    - Programmatically remove those during body init for now?
- The technique below assumes that the vertex IDs of the user-created backmesh and the flex collider geometry will remain in lock-step!  (Be careful about hole opening above)
    - Of course flex collider is different than presentation mesh!
- We need to decide on how to properly form extra geometry for vagina / anus holes (with uv modification) soon.
    - Try to generate backmesh programmatically... saves code!




=== CSoftBodySkin design decisions ===

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






//###OBS19: Really well done implementation of 'soft body skin' with a large scale effort from Blender and Unity to create a sophisticated 'soft body area' that is then glued to the skinned body
//###OBS19: The problem encountered is that the simulated part of the body would move too much and would provide a sharp contrast versus the unmoving skinned body.
//###OBS19: As a much more stable solution was found (re-boning the main skinned body around vagina and moving these new bones via Flex soft body shapes and particles this approach was shelved.
//using UnityEngine;
//using System;
//using System.Collections.Generic;

//public class CSoftBodySkin_VertRimInfo {                  // Helper storage object to CSoftBodySkin.  Acts as central storage to store everything there is to know about a vert in the 'rim collider mesh'.  Has equivalent class in Blender by the same name
    
//    public const ushort C_HasShape                  = 1;                // This rim vert has a soft-body shape representing it (e.g. it and neighbor verts participate in making this area of the mesh feel like a 'thick skin' soft body.
//    public const ushort C_HasParticle               = 2;                // This rim vert has a 'particle'. (e.g. it participates in the simulate of shapes in the area).  Every shape has a particle but not all of these particles drive a corresponding presentation vert (e.g. C_ParticleSetsPresentation flag)    
//    public const ushort C_HasParticleSkinned        = 4;                // This rim vert has a 'skinned particle'.    This particle's position is set at every frame form a skinned position from our associated nVertRim (us) skinned mesh vert.  It is responsible for influencing its shape to move as close to possible to the skin surface.  Setting this particle's position forms the 'input stage' and is done first. 
//    public const ushort C_ParticleSetsPresentation  = 8;                // This rim vert has its particle set its associated 'presentation vert'.  This particle's position is read at every frame and sets the position of our associated nVertSoftBody presentation just before presentation mesh rendering.  Reading this particle's position forms the 'output stage' and is done last.
//    public const ushort C_HasBone                   = 16;               // This rim vert is a bone.  It drives the position of a single bone which is in turn in charge of setting presentation verts around the 'hole' of this CSoftBodySkin implementation.
//    public const ushort C_Serialization_EndOfRecord = 0xFFFF;           // Magic number added at the end of each record in Serialize().  Used to trap serialization errors
//    public const ushort C_Empty = 0xFFFF;								// Constant used to indicate 'no entity' in this class' variables (0 is taken)

//	//public ushort nVertSrcBody;						// The vertex ID in the original untouched source body mesh. (The only 'authoritative' vert ID) 
//    public ushort	_nVertRim;							// The vertex ID in the rim collision mesh.
//    public ushort	_nVertSoftBody;						// The vertex ID in the soft body presentation mesh.
//	public ushort	_nFlags;							// Collection of C_xxx flags above.  Helps Unity construct the softbody shape & bones as appropriate for each rim collider vert.
//	public ushort[] _aNeighbors;						// Collection of rim vert IDs representing our the immediate rim vert neighbor to this rim vert.  (Used for Flex shape creation in Unity for soft skin effect)

//	public ushort	_nParticleSimulated	= C_Empty;		// The ID of the Flex particle acting as the 'simulated particle' for this rim vert = Reacts to colliders and drives the position of our corresponding vert in the presentation mesh '_nVertSoftBody'
//	public ushort	_nParticleSkinned	= C_Empty;		// The ID of the Flex particle 'grouding' this area of the Flex softbody close to where it should be on the skin position.  These are not defined near the 'hole' in the collider mesh so objects can enter and properly push the walls of the hole away.
//	public ushort	_nShape				= C_Empty;		// The ID of the Flex softbody 'shape' that is responsible to 'bind' both our skinned and simluated particles along with the neighboring simulated particles into a physical structure that will attempt to maintain its starting state at each frame (e.g. act like a 'softbody')
//	public Vector3	_vecStartingPos;					// The starting position of this vertex / simulated particle / skinned partice / shape center at initialization

//	public CSoftBodySkin_VertRimInfo(List<ushort> aRawData, ref ushort nDataPointer) {            // Perform the inverse of what our Blender equivalent did
//		_nVertRim			= aRawData[nDataPointer++];
//		_nVertSoftBody		= aRawData[nDataPointer++];
//		_nFlags				= aRawData[nDataPointer++];
//		ushort nNeighbors	= aRawData[nDataPointer++];
//		_aNeighbors = new ushort[nNeighbors];
//		for (ushort nNeighbor = 0; nNeighbor < nNeighbors; nNeighbor++)
//			_aNeighbors[nNeighbor] = aRawData[nDataPointer++];
//		ushort nChecksum_EndOfRecord = aRawData[nDataPointer++];
//		if (nChecksum_EndOfRecord != C_Serialization_EndOfRecord)
//			CUtility.ThrowException("Bad serialization data detected in CSoftBodySkin_VertRimInfo()");
//	}
//}

//public class CSoftBodySkin : CSoftBodyBase
//{
//	Dictionary<ushort, CSoftBodySkin_VertRimInfo> _mapVertRimInfos = new Dictionary<ushort, CSoftBodySkin_VertRimInfo>();

//	public static CSoftBodySkin Create(CBody oBody, Type oTypeBMesh, string sNameBoneAnchor) {
//		string sNameSoftBody = oTypeBMesh.Name.Substring(1);                            // Obtain the name of our detached body part ('Breasts', 'Penis', 'Vagina') from a substring of our class name.  Must match Blender!!  ###WEAK?
//        //CGame.gBL_SendCmd("CBody", "CBodyBase_GetBodyBase(" + oBody._oBodyBase._nBodyID.ToString() + ").oBody.CreateSoftBodySkin('" + sNameSoftBody + "', " + CGame.INSTANCE.nSoftBodyFlexColliderShrinkRatio.ToString() + ")");      // Separate the softbody from the source body.
//        CGame.gBL_SendCmd("CBody", "CBodyBase_GetBodyBase(" + oBody._oBodyBase._nBodyID.ToString() + ").oBody.CreateSoftBodySkin('" + sNameSoftBody + "', 2.0, 0.035)");      // Separate the softbody from the source body.		###TODO19: Argument for hole radius!
//		CSoftBodySkin oSoftBodySkin = (CSoftBodySkin)CBMesh.Create(null, oBody._oBodyBase, ".oBody.aSoftBodies['" + sNameSoftBody + "'].oMeshSoftBody", oTypeBMesh, false, sNameBoneAnchor);       // Create the softbody mesh from the just-created Blender mesh.
//		oSoftBodySkin.gameObject.name = oBody._oBodyBase._sBodyPrefix + "-SoftBody-" + oTypeBMesh.ToString();		//###IMPROVE18: Put this stuff in CBMesh.Create()?
//        return oSoftBodySkin;
//    }

//	public override void OnDeserializeFromBlender(params object[] aExtraArgs) {
//        base.OnDeserializeFromBlender(aExtraArgs);                    // Call important base class first to serialize rim, pinned particle mesh, etc

//		//===== A. DE-SERIALIZE BLENDER DATA =====
//		//=== Read the complex collection of 'CSoftBodySkin_VertRimInfo' objects Blender has prepared for us ===
//		List<ushort> aRawData = CByteArray.GetArray_USHORT("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeVertRimInfos()");

//		//=== De-serialize the CSoftBodySkin_VertRimInfo array from its raw serialized form ===
//		ushort nDataPointer = 0;
//		ushort nVertRimInfos = aRawData[nDataPointer++];
//		for (ushort nVertRimInfo = 0; nVertRimInfo < nVertRimInfos; nVertRimInfo++) {
//			CSoftBodySkin_VertRimInfo oVRI = new CSoftBodySkin_VertRimInfo(aRawData, ref nDataPointer);
//			_mapVertRimInfos.Add(oVRI._nVertRim, oVRI);			// The valuable key is _nVertRim, NOT nVertRimInfo which is just for efficient serialization
//		}


//		//===== B. DEFINE ALL PARTICLES =====
//		//=== Define the temporary variables we'll need to populate before being ready for Flex ===
//		List<uFlex.Particle> aParticles = new List<uFlex.Particle>();

//		//=== Create the two types of particles a rim vert can have: simulated and/or skinned ===
//		foreach (KeyValuePair<ushort, CSoftBodySkin_VertRimInfo> oPair in _mapVertRimInfos) {
//			CSoftBodySkin_VertRimInfo oVRI = oPair.Value;
//			oVRI._vecStartingPos = _oMeshRim._memVerts.L[oVRI._nVertRim];			// Remember our starting position as both our skinned and simulated particles start there.  (Shape center calculated)

//			//=== Create the 'input' skinned particle if this rim vert has one ===
//			if ((oVRI._nFlags & CSoftBodySkin_VertRimInfo.C_HasParticleSkinned) != 0) {
//				uFlex.Particle oParticle = new uFlex.Particle();
//				oParticle.pos		= oVRI._vecStartingPos;				// Skinned particle also starts at the position of our vert
//				oParticle.invMass	= 0;								// The skinned particle is NOT simulated so mass is infinite (we set its position at every frame from the skinned mesh to keep this area of the mesh close to where it should be on the skin)
//				oVRI._nParticleSkinned = (ushort)aParticles.Count;		// Remember the skinned particle ID we just created as we'll need this to form softbody shapes!
//				aParticles.Add(oParticle);
//			}

//			//=== Create the 'output' simulated particle if this rim vert has one ===
//			if ((oVRI._nFlags & CSoftBodySkin_VertRimInfo.C_HasParticle) != 0) {
//				uFlex.Particle oParticle = new uFlex.Particle();
//				oParticle.pos		= oVRI._vecStartingPos;				// Simulated particle starts at the position of our vert
//				oParticle.invMass	= 1;								// Particle is simulated so invMass is not zero  ###IMPROVE: What mass?
//				oVRI._nParticleSimulated = (ushort)aParticles.Count;	// Remember the simulated particle ID we just created as we'll need this to form softbody shapes!
//				aParticles.Add(oParticle);
//			}
//		}



//		//===== C. DEFINE ALL SHAPES =====
//		//=== Define the temporary variables we'll need to populate before being ready for Flex ===
//		ushort nShapes = 0;
//		List<int> aShapeParticleIndices = new List<int>();
//		List<int> aShapeParticleCutoffs = new List<int>();

//		foreach (KeyValuePair<ushort, CSoftBodySkin_VertRimInfo> oPair in _mapVertRimInfos) {
//			CSoftBodySkin_VertRimInfo oVRI = oPair.Value;
//			if ((oVRI._nFlags & CSoftBodySkin_VertRimInfo.C_HasShape) != 0) {
//				//--- Add our skinned particle if we have one ---
//				if (oVRI._nParticleSkinned != CSoftBodySkin_VertRimInfo.C_Empty)
//					aShapeParticleIndices.Add(oVRI._nParticleSkinned);
//				//--- Add our simulated particle if we have one ---
//				if (oVRI._nParticleSimulated != CSoftBodySkin_VertRimInfo.C_Empty)
//					aShapeParticleIndices.Add(oVRI._nParticleSimulated);
//				//--- Add the simulated particles of all our neighbors ---
//				foreach (ushort nNeighbor in oVRI._aNeighbors) {
//					CSoftBodySkin_VertRimInfo oVRI_Neighbor = _mapVertRimInfos[nNeighbor];
//					if (oVRI_Neighbor._nParticleSimulated != CSoftBodySkin_VertRimInfo.C_Empty)
//						aShapeParticleIndices.Add(oVRI_Neighbor._nParticleSimulated);
//				}
//				aShapeParticleCutoffs.Add(aShapeParticleIndices.Count);						// Specify the 'cutoff' for the next shape to be where we're at in our index collection once we're done with this shape
//				oVRI._nShape = nShapes++;													// Remember what shape we created
//			}
//		}

//		//===== D. CREATE FLEX PARTICLES OBJECT =====
//		//=== Define Flex particles from Blender mesh made for Flex ===
//		int nParticles = aParticles.Count;
//		_oFlexParticles = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexParticles)) as uFlex.FlexParticles;
//		_oFlexParticles.m_particlesCount = _oFlexParticles.m_maxParticlesCount = nParticles;
//		_oFlexParticles.m_particles = aParticles.ToArray();
//        _oFlexParticles.m_restParticles = aParticles.ToArray();		//###OPT19: Wasteful?  Remove from uFlex??
//		_oFlexParticles.m_colours = new Color[nParticles];
//		_oFlexParticles.m_velocities = new Vector3[nParticles];
//		_oFlexParticles.m_densities = new float[nParticles];
//		_oFlexParticles.m_particlesActivity = new bool[nParticles];
//		_oFlexParticles.m_colour = Color.green;                //###TODO: Colors!
//		_oFlexParticles.m_interactionType = uFlex.FlexInteractionType.SelfCollideFiltered;
//		_oFlexParticles.m_collisionGroup = -1;
//        _oFlexParticles.m_bounds.SetMinMax(new Vector3(-1,-1,-1), new Vector3(1,1,1));        //###CHECK: Better with some reasonable values than zero?
//		for (int nParticle = 0; nParticle < nParticles; nParticle++) {
//			_oFlexParticles.m_colours[nParticle] = _oFlexParticles.m_colour;
//			_oFlexParticles.m_particlesActivity[nParticle] = true;
//		}


//		//===== E. CREATE FLEX SHAPES =====
//		//=== Define Flex shapes from the Blender particles that have been set as shapes too ===
//		_oFlexShapeMatching = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexShapeMatching)) as uFlex.FlexShapeMatching;
//		_oFlexShapeMatching.m_shapesCount = nShapes;
//		_oFlexShapeMatching.m_shapeIndicesCount = aShapeParticleIndices.Count;
//		_oFlexShapeMatching.m_shapeIndices = aShapeParticleIndices.ToArray();            //###LEARN: How to convert a list to a straight .Net array.
//		_oFlexShapeMatching.m_shapeOffsets = aShapeParticleCutoffs.ToArray();
//		_oFlexShapeMatching.m_shapeCenters = new Vector3[nShapes];
//		_oFlexShapeMatching.m_shapeCoefficients = new float[nShapes];
//		_oFlexShapeMatching.m_shapeTranslations = new Vector3[nShapes];
//		_oFlexShapeMatching.m_shapeRotations = new Quaternion[nShapes];
//		_oFlexShapeMatching.m_shapeRestPositions = new Vector3[_oFlexShapeMatching.m_shapeIndicesCount];

//		//=== Calculate shape centers from attached particles ===
//		int nShapeStart = 0;			//###LEARN: Setting shape center to our vert center makes the result much less stable!  (By definition I think resting state assumes shape is truly at center of its related particles!)
//		for (int nShape = 0; nShape < _oFlexShapeMatching.m_shapesCount; nShape++) {
//			_oFlexShapeMatching.m_shapeCoefficients[nShape] = 0.1f;                   //###NOW###   Calculate shape center here or just accept particle pos?

//			int nShapeEnd = _oFlexShapeMatching.m_shapeOffsets[nShape];
//			Vector3 vecCenter = new Vector3();
//			for (int nShapeIndex = nShapeStart; nShapeIndex < nShapeEnd; ++nShapeIndex) {
//				int nParticle = _oFlexShapeMatching.m_shapeIndices[nShapeIndex];
//				Vector3 vecParticlePos = _oFlexParticles.m_particles[nParticle].pos;          // remap indices and create local space positions for each shape
//				vecCenter += vecParticlePos;
//			}

//			vecCenter /= (nShapeEnd - nShapeStart);
//			_oFlexShapeMatching.m_shapeCenters[nShape] = vecCenter;
//			nShapeStart = nShapeEnd;
//		}

//		//=== Set the shape rest positions ===
//		nShapeStart = 0;
//		int nShapeIndexOffset = 0;
//		for (int nShape = 0; nShape < _oFlexShapeMatching.m_shapesCount; nShape++) {
//			int nShapeEnd = _oFlexShapeMatching.m_shapeOffsets[nShape];
//			for (int nShapeIndex = nShapeStart; nShapeIndex < nShapeEnd; ++nShapeIndex) {
//				int nParticle = _oFlexShapeMatching.m_shapeIndices[nShapeIndex];
//				Vector3 vecParticle = _oFlexParticles.m_particles[nParticle].pos;          // remap indices and create local space positions for each shape
//				_oFlexShapeMatching.m_shapeRestPositions[nShapeIndexOffset] = vecParticle - _oFlexShapeMatching.m_shapeCenters[nShape];
//				nShapeIndexOffset++;
//			}
//			nShapeStart = nShapeEnd;
//		}

//		//=== Add particle renderer ===
//		_oFlexParticlesRenderer = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexParticlesRenderer)) as uFlex.FlexParticlesRenderer;
//		_oFlexParticlesRenderer.m_size = CGame.INSTANCE.particleSpacing;
//		_oFlexParticlesRenderer.m_radius = _oFlexParticlesRenderer.m_size / 2.0f;
//		_oFlexParticlesRenderer.enabled = false;           // Hidden by default
//	}

//    public override void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
//        base.PreContainerUpdate(solver, cntr, parameters);

//		_oMeshRim.Baking_UpdateBakedMesh();     // Bake the skinned portion of the mesh.  We need its verts so we can manually move the softbody rim verts for a seamless connection to main skinned body ===
//	    Vector3[] aVertsRimBaked    = _oMeshRim._oMeshBaked.vertices;       // Obtain the verts and normals from baked rim mesh so we can manually set rim verts & normals for seamless connection to main body mesh.

//		foreach (KeyValuePair<ushort, CSoftBodySkin_VertRimInfo> oPair in _mapVertRimInfos) {
//			CSoftBodySkin_VertRimInfo oVRI = oPair.Value;
//			if (oVRI._nParticleSkinned != CSoftBodySkin_VertRimInfo.C_Empty)
//				_oFlexParticles.m_particles[oVRI._nParticleSkinned].pos = aVertsRimBaked[oVRI._nVertRim];
//		}

//		//=== Set the visible mesh verts to the simulated particle verts ===
//		Vector3[] aVertsPresentation = _oMeshNow.vertices;
//		foreach (KeyValuePair<ushort, CSoftBodySkin_VertRimInfo> oPair in _mapVertRimInfos) {
//			CSoftBodySkin_VertRimInfo oVRI = oPair.Value;
//			if (oVRI._nParticleSimulated != CSoftBodySkin_VertRimInfo.C_Empty)
//				aVertsPresentation[oVRI._nVertSoftBody] = _oFlexParticles.m_particles[oVRI._nParticleSimulated].pos;
//		}
//		_oMeshNow.vertices = aVertsPresentation;
//		_oMeshNow.RecalculateNormals();         //###NOW###
//		_oMeshNow.RecalculateBounds();         //###NOW###

//		////=== Copy the particles represening each vert back to the mesh.  Note that we copy the 'assigned particle' and NOT the shape (as its position gets averaged from its many particles)  This means the shapes maintained the link between the particles to make it appear like 'thick skin' while still being extremely responsible to collision like the 'assigned particle' ===
//		//Vector3[] aVertsPresentation = _oMeshNow.vertices;
//		//for (int nVert = 0; nVert < GetNumVerts(); nVert++)
//		//	aVertsPresentation[nVert] = _oFlexParticles.m_particles[nVert].pos;
//		//_oMeshNow.vertices = aVertsPresentation;

//		//////=== Iterate through all softbody edge verts to update their position and normals.  This is critical for a 'seamless connection' between the softbody presentation mesh and the main skinned body ===
//		////Vector3[] aVertsRimBaked    = _oMeshSoftBodyRim._oMeshBaked.vertices;       // Obtain the verts and normals from baked rim mesh so we can manually set rim verts & normals for seamless connection to main body mesh.
//		////Vector3[] aNormalsRimBaked  = _oMeshSoftBodyRim._oMeshBaked.normals;
//		////for (int nIndex = 0; nIndex < _aMapRimVerts.Count;) {         // Iterate through the twin vert flattened map...
//		////    ushort nVertMesh    = _aMapRimVerts[nIndex++];            // The simple list has been flattened into <nVertMesh0, nVertRim0>, <nVertMesh1, nVertRim1>, etc
//		////    ushort nVertRim     = _aMapRimVerts[nIndex++];
//		////    _oFlexParticles.m_particles[nVertMesh].pos = aVertsRimBaked[nVertRim];
//		////    _oFlexParticles.m_particles[nVertMesh].invMass = 0;     //###NOW###
//		////    //aVertsFlexGenerated  [nVertMesh] = aVertsRimBaked  [nVertRim];
//		////    //aNormalsFlexGenerated[nVertMesh] = aNormalsRimBaked[nVertRim];
//		////}
//		////=== Set the visible mesh verts to the particle verts (1:1 ratio) ===
//		//Vector3[] aVertsPresentation = _oMeshNow.vertices;
//		//for (int nVert = 0; nVert < GetNumVerts(); nVert++)
//		//    aVertsPresentation[nVert] = _oFlexParticles.m_particles[nVert].pos;
//		//_oMeshNow.vertices = aVertsPresentation;

//		//_oMeshNow.RecalculateNormals();         //###NOW###
//		//_oMeshNow.RecalculateBounds();         //###NOW###
//	}
//}

















//###OBS: Old implementation before redoing everything in Blender centered on its complex creation of CSoftBodySkin_VertRimInfo() array
		//List<int> aShapeVerts			= CByteArray.GetArray_INT("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".aShapeVerts.Unity_GetBytes()");					// Array of which vert / particle is also a shape
		//List<int> aShapeParticleIndices	= CByteArray.GetArray_INT("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".aShapeParticleIndices.Unity_GetBytes()");		// Flattened array of which shape match to which particle (as per Flex softbody requirements)
		//List<int> aShapeParticleCutoffs	= CByteArray.GetArray_INT("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".aShapeParticleCutoffs.Unity_GetBytes()");		// Cutoff in 'aShapeParticleIndices' between sets defining which particle goes to which shape. 
		//List<ushort> aPinnedParticles_TEMP = CByteArray.GetArray_USHORT("'CBody'", _sBlenderInstancePath_CSoftBody_FullyQualfied + ".aPinnedParticles_TEMP.Unity_GetBytes()");

		////=== Define Flex particles from Blender mesh made for Flex ===
		////int nParticles = _oMeshRim.GetNumVerts();
		//_oFlexParticles = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexParticles)) as uFlex.FlexParticles;
		//_oFlexParticles.m_particlesCount = _oFlexParticles.m_maxParticlesCount = nParticles;
		//_oFlexParticles.m_particles = new uFlex.Particle[nParticles];
  //      _oFlexParticles.m_restParticles = new uFlex.Particle[nParticles];		//###OPT19: Wasteful?  Remove from uFlex??
		//_oFlexParticles.m_colours = new Color[nParticles];
		//_oFlexParticles.m_velocities = new Vector3[nParticles];
		//_oFlexParticles.m_densities = new float[nParticles];
		//_oFlexParticles.m_particlesActivity = new bool[nParticles];
		//_oFlexParticles.m_colour = Color.green;                //###TODO: Colors!
		//_oFlexParticles.m_interactionType = uFlex.FlexInteractionType.SelfCollideFiltered;
		//_oFlexParticles.m_collisionGroup = -1;
  //      _oFlexParticles.m_bounds.SetMinMax(new Vector3(-1,-1,-1), new Vector3(1,1,1));        //###CHECK: Better with some reasonable values than zero?
		//for (int nParticle = 0; nParticle < nParticles; nParticle++) {
		//	_oFlexParticles.m_restParticles[nParticle].pos = _oFlexParticles.m_particles[nParticle].pos = _oMeshRim._memVerts.L[nParticle];
		//	_oFlexParticles.m_restParticles[nParticle].invMass = _oFlexParticles.m_particles[nParticle].invMass = 1;			// All particles are simulated unless pinned by loop below
		//	_oFlexParticles.m_colours[nParticle] = _oFlexParticles.m_colour;
		//	_oFlexParticles.m_particlesActivity[nParticle] = true;
		//}

		//foreach (ushort nParticlePinned in aPinnedParticles_TEMP)
		//	_oFlexParticles.m_restParticles[nParticlePinned].invMass = _oFlexParticles.m_particles[nParticlePinned].invMass = 0;

		////=== Define Flex shapes from the Blender particles that have been set as shapes too ===
		////int nShapes = aShapeVerts.Count;
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
		////int nShape = 0;			//###DESIGN19: Shape at real center or at its 'designed particle'?
		////foreach (int nShapeParticle in aShapeVerts) {
		////	Vector3 vecParticle = _oFlexParticles.m_particles[nShapeParticle].pos;
		////	//float nStrenth = Math.Abs(vecParticle.x) / 0.005f;
		////	//nStrenth = Math.Min(nStrenth, 1);
		////    _oFlexShapeMatching.m_shapeCoefficients[nShape] = 0.01f;
		////    _oFlexShapeMatching.m_shapeCenters[nShape] = vecParticle;
		////    nShape++;
		////}

		//int nShapeStart = 0;			//###DESIGN19: Center is real center or first particle?  Now at first particle given that shape is defined by its primary particle
		//for (int nShape = 0; nShape < _oFlexShapeMatching.m_shapesCount; nShape++) {
		//	_oFlexShapeMatching.m_shapeCoefficients[nShape] = 0.02f;                   //###NOW###   Calculate shape center here or just accept particle pos?

		//	int nShapeEnd = _oFlexShapeMatching.m_shapeOffsets[nShape];
		//	Vector3 vecCenter = new Vector3();
		//	for (int nShapeIndex = nShapeStart; nShapeIndex < nShapeEnd; ++nShapeIndex) {
		//		int nParticle = _oFlexShapeMatching.m_shapeIndices[nShapeIndex];
		//		Vector3 vecParticlePos = _oFlexParticles.m_particles[nParticle].pos;          // remap indices and create local space positions for each shape
		//		vecCenter += vecParticlePos;
		//	}

		//	vecCenter /= (nShapeEnd - nShapeStart);
		//	_oFlexShapeMatching.m_shapeCenters[nShape] = vecCenter;
		//	nShapeStart = nShapeEnd;
		//}

		////=== Set the shape rest positions ===
		//nShapeStart = 0;
		//int nShapeIndexOffset = 0;
		//for (int nShape = 0; nShape < _oFlexShapeMatching.m_shapesCount; nShape++) {
		//	int nShapeEnd = _oFlexShapeMatching.m_shapeOffsets[nShape];
		//	for (int nShapeIndex = nShapeStart; nShapeIndex < nShapeEnd; ++nShapeIndex) {
		//		int nParticle = _oFlexShapeMatching.m_shapeIndices[nShapeIndex];
		//		Vector3 vecParticle = _oFlexParticles.m_particles[nParticle].pos;          // remap indices and create local space positions for each shape
		//		_oFlexShapeMatching.m_shapeRestPositions[nShapeIndexOffset] = vecParticle - _oFlexShapeMatching.m_shapeCenters[nShape];
		//		nShapeIndexOffset++;
		//	}
		//	nShapeStart = nShapeEnd;
		//}

		////=== Add particle renderer ===
		//_oFlexParticlesRenderer = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexParticlesRenderer)) as uFlex.FlexParticlesRenderer;
		//_oFlexParticlesRenderer.m_size = CGame.INSTANCE.particleSpacing;
		//_oFlexParticlesRenderer.m_radius = _oFlexParticlesRenderer.m_size / 2.0f;
		//_oFlexParticlesRenderer.enabled = true;           // Hidden by default




	
	
		//###OBS: Older code to set shape center and particle rest vectors
		//int nShape = 0;			//###DESIGN19: Shape at real center or at its 'designed particle'?
		//foreach (int nShapeParticle in aShapeVerts) {
		//	Vector3 vecParticle = _oFlexParticles.m_particles[nShapeParticle].pos;
		//    _oFlexShapeMatching.m_shapeCoefficients[nShape] = 0.01f;
		//    _oFlexShapeMatching.m_shapeCenters[nShape] = vecParticle;
		//    nShape++;
		//}

		//nShapeStart = 0;			//###DESIGN19: Center is real center or first particle?  Now at first particle given that shape is defined by its primary particle
		//for (int nShape = 0; nShape < _oFlexShapeMatching.m_shapesCount; nShape++) {
		//	_oFlexShapeMatching.m_shapeCoefficients[nShape] = 0.02f;                   //###NOW###   Calculate shape center here or just accept particle pos?

		//	int nShapeEnd = _oFlexShapeMatching.m_shapeOffsets[nShape];
		//	Vector3 vecCenter = new Vector3();
		//	for (int nShapeIndex = nShapeStart; nShapeIndex < nShapeEnd; ++nShapeIndex) {
		//		int nParticle = _oFlexShapeMatching.m_shapeIndices[nShapeIndex];
		//		Vector3 vecParticlePos = _oFlexParticles.m_particles[nParticle].pos;          // remap indices and create local space positions for each shape
		//		vecCenter += vecParticlePos;
		//	}

		//	vecCenter /= (nShapeEnd - nShapeStart);
		//	_oFlexShapeMatching.m_shapeCenters[nShape] = vecCenter;
		//	nShapeStart = nShapeEnd;
		//}

		//###OLDER CODE TO SET SHAPE CENTER TO OUR POSITION = LESS STABLE!
		//foreach (KeyValuePair<ushort, CSoftBodySkin_VertRimInfo> oPair in _mapVertRimInfos) {
		//	CSoftBodySkin_VertRimInfo oVRI = oPair.Value;
		//	if ((oVRI._nFlags & CSoftBodySkin_VertRimInfo.C_HasShape) != 0) {
		//		_oFlexShapeMatching.m_shapeCoefficients[oVRI._nShape] = 0.01f;
		//		_oFlexShapeMatching.m_shapeCenters[oVRI._nShape] = oVRI._vecStartingPos;		// Our rim vert's shape starts at our rim vert position (along with our particles)
		//	}
		//}

