///*###DISCUSSION: Breast Colliders
//=== NEXT ===

//=== TODO ===

//=== LATER ===

//=== IMPROVE ===
//- We could create a CSlaveMesh to auto-pair a mesh and have breast col and cloth col fix themslves this way
////####IMPROVE!!! Breast collider specs dynamic!  Insert into a menu!!!

//=== DESIGN ===

//=== IDEAS ===

//=== LEARNED ===

//=== PROBLEMS ===

//=== PROBLEMS??? ===

//=== WISHLIST ===

//*/

//using UnityEngine;
//using System;
//using System.Collections.Generic;

//public class CBodyColBreast : CBMesh {      // CBodyColBreast: A simple mesh (about 25 verts) that is turned into a collection of sphere colliders in the shape of breasts.  The objective is to repell cloth and other character's breasts away from the each breasts of this body

//	CBreastBase     _oBreast;			// The softbody breast that 'owns us'
//	public IntPtr	_hBodyColBreast;

//	public 	CMemAlloc<ushort>	_memColBreastVertSphereRadiusRatio		= new CMemAlloc<ushort>();	// Sores a number from 0-255 to scale the sphere radius (kept in Unity only) used by this collider mesh.  (0 means no sphere created for that vertex)  A maximum of 32 spheres can be defined
//	public 	CMemAlloc<ushort>	_memColBreastCapsuleSpheres				= new CMemAlloc<ushort>();	// Stores the two vertex IDs of each vertex / sphere that represends the end of each tapered capsule.  These are marked by 'sharp edges' for each capsule
//	public 	CMemAlloc<ushort>	_memColBreastMapSlaveMeshSlaveToMaster	= new CMemAlloc<ushort>();	// Stores the the mapping of slave-vert to master-vert that was established by SlaveMesh_DoPairing() so slave mesh can be repositioned to master mesh quickly at gametime

//	public float _nRadiusSphereBase = 0.070f;			//###TODO: Expose as CProps		####MOD was .065
//	public float _nOutsideProtusion = 0.007f;           //###IMPROVE: Autoscale when we have breast growth!

//	const bool C_EnableRenderer = false;

//	public CBodyColBreast() {}

//	public void FinishColliderCreation(CBreastBase oBreast) {	// Collider finalization is handled after normal CMesh-based static creation so breast / breast collider relationship can be established
//		_oBreast = oBreast;

//		GetComponent<Renderer>().enabled = C_EnableRenderer;             // Disable the renderer unless debug mode.  We don't need to show our mesh in Unity (only for PhysX)

//		//=== Receive the 'ColBreastVertSphereRadiusRatio' flat array that contain the relative radius of each of our spheres / vertices for each breast ===
//		List<ushort> aColBreastVertSphereRadiusRatio;
//		CUtility.BlenderSerialize_GetSerializableCollection("'CBody'", _oBreast._sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aColBreastVertSphereRadiusRatio()", out aColBreastVertSphereRadiusRatio);
//		_memColBreastVertSphereRadiusRatio.AllocateFromList(aColBreastVertSphereRadiusRatio);

//		//=== Receive the 'CapsuleSpheres' flat array that tells PhysX how to form tapered capsules from two linked spheres ===
//		List<ushort> aColBreastCapsuleSpheres;
//		CUtility.BlenderSerialize_GetSerializableCollection("'CBody'", _oBreast._sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aColBreastCapsuleSpheres()", out aColBreastCapsuleSpheres);
//		_memColBreastCapsuleSpheres.AllocateFromList(aColBreastCapsuleSpheres);

//		//===== Obtain the 'slave vert to master vert' constructed by SlaveMesh_DefineMasterSlaveRelationship() during body construction =====  ####IMPROVE: Create subclass of CBMesh called CBSlaveMesh?? ####DESIGN ####SOON
//		List<ushort> aColBreastMapSlaveMeshSlaveToMaster;
//		CUtility.BlenderSerialize_GetSerializableCollection("'CBody'", _oBreast._sBlenderInstancePath_CSoftBody_FullyQualfied + ".SerializeCollection_aColBreastMapSlaveMeshSlaveToMaster()", out aColBreastMapSlaveMeshSlaveToMaster);
//		_memColBreastMapSlaveMeshSlaveToMaster.AllocateFromList(aColBreastMapSlaveMeshSlaveToMaster);

//		////=== Create the CBodyColBreast PhysX3 object that will absorb the location of the PhysX2 softbody vert position to convert to an approximation of the breast position in PhysX3 ===
//		_hBodyColBreast = ErosEngine.BodyColBreast_Create(_oBreast._nBreastID, _memVerts.L.Length, _memVerts.P, _memNormals.P, _memColBreastVertSphereRadiusRatio.L.Length, _memColBreastVertSphereRadiusRatio.P, _memColBreastCapsuleSpheres.L.Length / 2, _memColBreastCapsuleSpheres.P);
//		// Breast collider now half-defined.  Cloth will need to attach to it when it's ready to expose its collider info to each of the (optional) two breast colliders
//	}

//	public override void UpdateVertsFromBlenderMesh(bool bUpdateNormals) {			// Only called from the context when we're a slave to body... ####DESIGN: Update verts outselves??
//		base.UpdateVertsFromBlenderMesh(bUpdateNormals);							// First update our verts from Blender, then update PhysX3 with these new vert positions
//		ErosEngine.SoftBody_Breasts_UpdateCBodyColBreast(_hBodyColBreast, _memVerts.P, _memNormals.P,  _nRadiusSphereBase, _nOutsideProtusion, (int)(EColGroups.eLayerBodyNoCollisionWithSelfStart + _oBody._oBodyBase._nBodyID));
//	}

//	public override void OnDestroy() {
//		ErosEngine.BodyColBreast_Destroy(_hBodyColBreast);
//		base.OnDestroy();
//	}

//	public void OnSimulateBetweenPhysX23() {
//		//	//=== Right after PhysX has update the softbody position of our breasts, we need to now update the related colliders in PhysX3 scene so it repells cloth for this time frame ===
//		int nMapEntries = _memColBreastMapSlaveMeshSlaveToMaster.L.Length / 2;                        // Each map entry takes two slots in array in order slave vert 1, master vert 1, slave vert 2, master vert 2, etc.
//		for (int nMapEntry = 0; nMapEntry < nMapEntries; nMapEntry++) {                     //####IMPROVE: Do this in C++!!  ####OPT
//			int nVertSlave  = _memColBreastMapSlaveMeshSlaveToMaster.L[2*nMapEntry + 0];
//			int nVertMaster = _memColBreastMapSlaveMeshSlaveToMaster.L[2*nMapEntry + 1];
//			_memVerts.L[nVertSlave] = _oBreast._memVerts.L[nVertMaster];
//		}
//		//=== Update PhysX3 collider ===
//		ErosEngine.SoftBody_Breasts_UpdateCBodyColBreast(_hBodyColBreast, _memVerts.P, _memNormals.P, _nRadiusSphereBase, _nOutsideProtusion, (int)(EColGroups.eLayerBodyNoCollisionWithSelfStart + _oBody._oBodyBase._nBodyID));

//		//=== Update optional rendered mesh (for debugging of collider) ===
//		if (C_EnableRenderer) {
//			_oMeshNow.vertices = _memVerts.L;           // Optional: Update the invisible mesh (no triangles) to visualize in Unity debugger
//			MeshFilter oMeshFilter  = (MeshFilter)  CUtility.FindOrCreateComponent(transform, typeof(MeshFilter));
//			oMeshFilter.mesh = _oMeshNow;
//		}
//	}
//}
