/*###DISCUSSION: Body Collider
=== NEXT ===
- Body col bones not working

=== TODO ===

=== LATER ===
- Remember hack to deep copy bodycol verts!

=== DESIGN ===

=== IDEAS ===

=== PROBLEMS ===
- Body origin at navel causes our anim to be way off... recalibrate 'floor'

=== PROBLEMS: ASSETS ===

=== PROBLEMS??? ===

=== WISHLIST ===

=== DOCUMENTATION ===
 * Fluid needs a CBBodyColFill containing hundreds of capsules to closely emulate the skinned human shape.
   * Soft body (breasts) and bone movement will possibly use the same (accurate) collider to avoid collision)
 * Each cloth item needs a (separate) CBBodyColTri containing 50-200 triangles to repel the cloth from the current skinned body.

=== DESIGN NEEDS ===
 * A single body needs to support many different instances of body colliders.
   * Each clothing item currently worn needs to send to PhysX only the nearby triangle colliders from BodyCldr.
   * The full body fluid-repelling mesh also needs a different full-body decimation to create/update hundreds of capsules in order to repel fluid.
 * Merging the output of softbody simulation into colliders with the simplest / fastest possible design important!
 * There are exceptions to collider creation from its source mesh: 
   * Penis repels cloth and fluid via world-space capsules
   * Breasts may repel cloth & fluid via capsules
 * Limb collision avoidance issues:
   * Cloth would benefit heavily from tapered capsule in local cloth space for arms and legs, but this 'quality collision' doesn't allow for that... how to put that in and 'blend'??

=== DESIGN IDEAS ===
 * Start with requirements of Fluid colliders: full body repelling including fingers, toes and face
   * Hand-tune a 'BodyCldr' mesh at shipping-time.  It glues fingers, toes, underside of breasts, face, nose, ears, etc and covers the whole body except hair area which doesn't repell fluid
     * Body2 mesh has colliders for vagina, and if penis is present its different capsule colliders will repel nearby fluid.
   * This mesh is skin-baked and generates at every frame capsules for areas of the body where cum is near (via large triggers so Unity only updates PhysX colliders that have cum in trigger with autocleanup)
   * This same mesh also generates the 'TriCol' colliders for every cloth. (Blender picks which cloth uses which tri so PhysX processes only nearby collision triangles for each cloth)
   * When body is morphed, we apply same morphing to 'BodyCldr' (more complex implementation possible if quality suffers because of topology change)
   * Optimization trick: Don't create capsules for the 'short edges' of any verts... the longer edges will create capsules that are big enough
   * Q: Do we change sphere size / capsule size at runtime as body bends???
 * With the above obligatory requirements met for Fluid we adapt this obligatory design for cloth repelling:
   * During cloth creation phase, Blender tags which tris of the 'BodyCldr' mesh are needed to repel each piece of clothing.
   * Q: Do we ever use spheres and tapered capsules to repel clothing???
 * Vagina area requirements:
   * Need detailed colliders there to repel cum properly as vagina opens / closes
   * If we go with vagina simulated by cloth instead of softbody we'll need triangle colliders backing it.
   * If we go with vagina simulated by softbody we'll need 

=== DESIGN UNCERTAINTIES ===
 * ++++ What do we do about previous 'rim'??  We still need to attach softbody pins to a quick per-frame skin and BodyCldr already skin so is a 'sunk cost'  Need to map to closest tri and calculate offset
 * +++ During body morphing, will related BodyCldr mesh deform similarly??  (e.g. underside of breasts merged)
 * ++ For Limb collision avoidance, do we avoid collisions with detailed BodyCldr mesh connected to bones or do we connect crude capsules to bones??
 * ++ We need some area of high detail (e.g. breasts) and low-detail (e.g. belly)... how to automate this??
 * + Breasts would likely repel cloth better/more efficiently as a collection of tapered capsules... how to fit that in entire workflow (including softbody, fluid, etc)???
   * Go with simple capsules in global space so same code can repel cloth and fluid??
 * +++ Can multiple layers of clothing be supported?  (Can we adjust distance-to-colliders on a per cloth basis for both tris and scene colliders??)

=== IDEAS ===
 * We could proceed with a carefully-constructed hand-crafted BodyCldr and create code to move verts from that given our first body shape moving its verts to new body shape!
   * Otherwise we'll need to create tons of BodyCldr-automation creation code that will never be as good as handcrafting.
 * It is highly desirable to preserver the ability to adapt to new DAZ meshes... Can 'automate' all steps of BodyCldr creation??
   * Define each local heavy decimation area in its own group and do tricks like local decimation, merge to a single vert, extrude then merge, etc
 * +++++ Cloth can also have 'collision convex'... would defining each breast in this way be faster than generic triangles (which can assume concave shapes?)
   * Probably not as there are only 32 planes possible!!
=== DESIGN DECISIONS ===
 * At runtime, the body's single CBBodyRim INSTANCE bakes its reduced-triangle mesh subset at every frame to service the position-update requests of a body's multiple body colliders that come later during frame processing.

=== DESIGN FACTORS ===
 * Skinning a body mesh without hands and feet results in about 5K verts that skins in 0.8 ms = A bit expensive but greatly reduces code complexity!!
   * With some optimization (e.g. nostrils to one vert, ears merged, etc) we can get a decent full body to 1K vert without fingers... abount .15ms!!

=== NEXT ===
 * Create a hand-crafted basic fluid-repelling mesh.
 * Remove old crap about rim in Blender so now cloth defines which triangle from BodyCldr it needs to send to PhysX
 * Create Unity class for BodyCldr, bake at each frame, so that each cloth can send to PhysX its triangle colliders!!
   * Q!!!: What about need to insert extra dummy triangles for PhysX???
*/


/*###DISCUSSION: Full-scene BodyCol design on C++ side
 * === REQUIREMENTS ===
 * Cloth requirements:
   * Should adapt quickly enough to shift enabled colliders and be able to support full unclothing (e.g. panty / bra removal)
   * Should prevent the PhysX limitation of tripping on 'nearest triangle plane expanded to infinity' that can cause cloth to snag to expanded planes from faraway triangles.
   * Any body's cloth can be affected / moved by any body's colliders that are close enough: Truly global-space collision optimization.
     * If we get this working remember that hands will need these triangle colliders so this works!
   * Must be extremely fast, yield the smallest possible set of triangle colliders to repel the cloth (e.g. small range) and triangle activation/deactivation run infrequently (e.g. every second or so)
 * Fluid-repelling / hand touch / penis collider repelling: boxes based
   * Vagina requirements:
     * As PhysX33-based penis-colliders can't influence body's kinematic colliders, we must tell Unity to push some 'vagina opening' bones so that detailed colliders can be adjusted.
       * Does it make sense for penis colliders to collide against normal detailed colliders at vagina entry??
         * We could keep the normally-defined boxes at vagina entry unchanged so it keeps repelling cum properly
         * That way we can have an easy 'vagina guide tunnel' inside woman body to properly guide penis and have it properly expand soft-body vagina tissue around its capsule chain colliders

 * === DESIGN UNCERTAINTIES ===
 * ++++ Are individual colliders capsules or boxes???
   * Capsules PROS:
     * ++ Possibly higher performance than boxes? (PhysX collision only)
   * Capsules CONS:
     * ---- Filled-in nature prevents penis from entering woman's vagina
     * ---- Much higher per-frame runtime cost to align spheres and capsules properly
   * Boxes PROS:
     * +++++ Enables sophisticated penis repelling, penis entry, vagina bone adjustment, etc
     * ++++ Much faster to orient boxes (if we don't resize)
   * Boxes CONS:
     * -- Assumes a quad-based source Blender mesh: Much harder to decimate, some collider overlap near triangle-like faces, etc
     * --- More work to take from Blender to C++ through Unity!
       * OR: Could triangulate as usual and use FastFluid code to calculate smallest box (WE GO WITH THIS)
 * Q: How do we generate boxes from decimated triangle-based mesh?  Using existing Blender at design-time or at run-time in C++??
 * Q: What about cloth tapered capsules??

 * === DESIGN DECISIONS ===
 * We preserve two-stage collision:
   * Coarse colliders stay in Unity are are owned by bones (1-3 colliders per bone)
   * Unity skins the BodyCol source mesh and we bake it so detailed colliders can be extracted below
   * Detailed colliders are generated from the skinned body mesh above and broken into boxes for Fluid / touch and tris for cloth

 
*/

using UnityEngine;
using System;
using System.Collections;

public class CBBodyColCloth : CBSkinBaked {		// Manages a 'body collider' that very roughly approximates a body's shape from a Blender-decimated body mesh and constructs very large triangles (to repell cloth) (e.g. torso consumes about 30 triangles)

	public 	CMemAlloc<ushort>	_memEdges;					// Flat array of edges (each containing vert1,vert2) to enable fast creation of capsules (linked to edges) from spheres (linked to verts)
	public 	CMemAlloc<ushort>	_memVertToVerts;			// Flat array of vert-to-verts to greatly speed up mesh traversal by verts

	public	IntPtr				_hBodyColCloth;					// Handle to our corresponding entity in our C++ dll.

	public static CBBodyColCloth Create(GameObject oBMeshGO, CBody oBody, string sNameCharacter) {	// Static function override from CBMesh::Create() to route Blender request to BodyCol module and deserialize its additional information for the local creation of a CBBodyColCloth
		//###BROKEN: CBBodyColCloth oBBodyColCloth = (CBBodyColCloth)CBMesh.Create(oBMeshGO, oBody, sNameCharacter, G.C_NameSuffix_BodyColCloth, "CBBodyCol", "CBBodyColCloth_GetMesh", "", typeof(CBBodyColCloth));
		return null;//oBBodyColCloth;
	}


	public override void OnDeserializeFromBlender() {
		base.OnDeserializeFromBlender();

		//####DEV ####BROKEN
		////=== Receive the 'aEdges' flat array that exists on _BodyCol meshes ===
		//int nArrayElements = BitConverter.ToInt32(oBA, nPosBA) / 2; nPosBA += 4;
		//_memEdges = new CMemAlloc<ushort>(nArrayElements);
		//for (int nArrayElement = 0; nArrayElement < nArrayElements; nArrayElement++) {	// Stream in the flat array and store in memArray for sharing with C++ side
		//	_memEdges.L[nArrayElement] = BitConverter.ToUInt16(oBA, nPosBA); nPosBA += 2;
		//}

		////=== Receive the 'aVertToVerts' flat array that exists on _BodyCol meshes ===
		//nArrayElements = BitConverter.ToInt32(oBA, nPosBA) / 2; nPosBA += 4;
		//_memVertToVerts = new CMemAlloc<ushort>(nArrayElements);
		//for (int nArrayElement = 0; nArrayElement < nArrayElements; nArrayElement++) {
		//	_memVertToVerts.L[nArrayElement] = BitConverter.ToUInt16(oBA, nPosBA); nPosBA += 2;
		//}

		//CheckMagicNumber(ref oBA, ref nPosBA, true);				// Read the 'end magic number' that always follows a stream.

		//GetComponent<Renderer>().enabled = false;
		//base.Baking_UpdateBakedMesh();			// Bake mesh once so that we initialize PhysX with valid data at the first game frame.
		//_hBodyColCloth = ErosEngine.BodyColCloth_Create(_memVerts.L.Length, _memVerts.P, _memNormals.P, _memTris.L.Length / 3, _memTris.P, _memEdges.L.Length / 2, _memEdges.P, _memVertToVerts.P);
	}

	public override void OnDestroy() {
		ErosEngine.BodyColCloth_Destroy(_hBodyColCloth);
		base.OnDestroy();
	}
}
