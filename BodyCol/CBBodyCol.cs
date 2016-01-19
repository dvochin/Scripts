/*###DISCUSSION: Body Collider
=== NEXT ===
 * How to link breasts with fluid collider a major design question...
   * Vagina easy or difficult???

=== TODO ===
 * Penis top rung colliders need to collide with nothing...
 * How to get Blender to process toes and face... create hull first and (manual?) fix?

=== LATER ===
-? Remember hack to deep copy bodycol verts!

=== DESIGN ===

=== IDEAS ===

=== PROBLEMS ===
+++ Delay before colliders start colliding!  Not in time to repell a new cum blast!
 * Problem during collider creation of legs glued together.
+++ Rapidly-changing capsule radius causes jump??
- Body origin at navel causes our anim to be way off... recalibrate 'floor'

=== PROBLEMS: ASSETS ===

=== PROBLEMS??? ===

=== WISHLIST ===
 * Remove short edges to trim # of capsules
 * Optimize update by cascade of verts that changed updating their capsules
 
*/

using UnityEngine;
using System;
using System.Collections;

public class CBBodyCol : CBSkinBaked {		// Manages a 'body collider' that approximates a body's shape with spheres and capsules (sent to PhysX to act as cloth colliders)
	//###DESIGN!!!: Skin rim and this class share a lot in common with runtime baking... create subclass?  Who is subclass, who is superclass??

	public 	CMemAlloc<ushort>	_memEdges;					// Flat array of edges (each containing vert1,vert2) to enable fast creation of capsules (linked to edges) from spheres (linked to verts)
	public 	CMemAlloc<ushort>	_memVertToVerts;			// Flat array of vert-to-verts to greatly speed up mesh traversal by verts

	public	IntPtr				_hBodyCol;					// Handle to our corresponding entity in our C++ dll.

	public static CBBodyCol Create(GameObject oBMeshGO, CBody oBody, string sNameCharacter) {	// Static function override from CBMesh::Create() to route Blender request to BodyCol module and deserialize its additional information for the local creation of a CBBodyCol
		//###BROKEN: CBBodyCol oBBodyCol = (CBBodyCol)CBMesh.Create(oBMeshGO, oBody, oBody._sMeshSource, G.C_NameSuffix_BodyCol, "CBBodyCol", "CBBodyCol_GetMesh", "", typeof(CBBodyCol));
		return null;	//oBBodyCol;
	}

	//####BROKEN
	//public override void OnSerializeIn(ref byte[] oBA, ref int nPosBA) {			// Extended deserialization for this object must extract additional arrays sent from Blender for our type.

	//	//=== Receive the 'aEdges' flat array that exists on _BodyCol meshes ===
	//	int nArrayElements = BitConverter.ToInt32(oBA, nPosBA) / 2; nPosBA += 4;
	//	_memEdges = new CMemAlloc<ushort>(nArrayElements);
	//	for (int nArrayElement = 0; nArrayElement < nArrayElements; nArrayElement++) {	// Stream in the flat array and store in memArray for sharing with C++ side
	//		_memEdges.L[nArrayElement] = BitConverter.ToUInt16(oBA, nPosBA); nPosBA += 2;
	//	}

	//	//=== Receive the 'aVertToVerts' flat array that exists on _BodyCol meshes ===
	//	nArrayElements = BitConverter.ToInt32(oBA, nPosBA) / 2; nPosBA += 4;
	//	_memVertToVerts = new CMemAlloc<ushort>(nArrayElements);
	//	for (int nArrayElement = 0; nArrayElement < nArrayElements; nArrayElement++) {
	//		_memVertToVerts.L[nArrayElement] = BitConverter.ToUInt16(oBA, nPosBA); nPosBA += 2;
	//	}

	//	CheckMagicNumber(ref oBA, ref nPosBA, true);				// Read the 'end magic number' that always follows a stream.
	//}

	public override void OnDeserializeFromBlender() {
		base.OnDeserializeFromBlender();
		GetComponent<Renderer>().enabled = false;
		base.Baking_UpdateBakedMesh();
		_hBodyCol = ErosEngine.BodyCol_Init(_memVerts.L.Length, _memVerts.P, _memNormals.P, _memTris.L.Length / 3, _memTris.P, _memEdges.L.Length / 2, _memEdges.P, _memVertToVerts.P);
		Debug.Log("+ CBBodyCol PhysX creation: " + _hBodyCol);
	}

	public override void OnDestroy() {
		ErosEngine.BodyCol_Destroy(_hBodyCol);
		base.OnDestroy();
	}

	public override void OnSimulatePre() {
		base.OnSimulatePre();			//###OPT!!!  Don't update radius at all??  (or occasionally??)  ###IMPROVE: Redo passing in of flags to this expensive function to tune!
		ErosEngine.BodyCol_Update(_hBodyCol, false, CGame.INSTANCE._bPenisInVagina);		// Update the body collider mesh in C++ for speed (done from copying verts from source body with maps setup in Blender)
	}
}
