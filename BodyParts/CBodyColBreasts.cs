/*###DISCUSSION: Breast Colliders
=== NEXT ===

=== TODO ===

=== LATER ===

=== IMPROVE ===
- We could create a CPairMesh to auto-pair a mesh and have breast col and cloth col fix themslves this way
//####IMPROVE!!! Breast collider specs dynamic!  Insert into a menu!!!

=== DESIGN ===

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===

=== PROBLEMS??? ===

=== WISHLIST ===

*/

using System;
using UnityEngine;

public class CBodyColBreasts : CBMesh {		// CBodyColBreasts: A simple mesh (about 50 verts) that is turned into a collection of sphere colliders in the shape of breasts.  The objective is to repell cloth and other character's breasts away from the each breasts of this body

	public CBodyColBreasts_PairMesh _oPairMesh;
	public IntPtr	_hBodyColBreasts;

	public 	CMemAlloc<ushort>	_memVertSphereRadiusRatio;		// Sores a number from 0-255 to scale the sphere radius (kept in Unity only) used by this collider mesh.  (0 means no sphere created for that vertex)  A maximum of 32 spheres can be defined
	public 	CMemAlloc<ushort>	_memCapsuleSpheres;				// Stores the two vertex IDs of each vertex / sphere that represends the end of each tapered capsule.  These are marked by 'sharp edges' for each capsule
	public 	CMemAlloc<ushort>	_memMapPairMeshSlaveToMaster;	// Stores the the mapping of slave-vert to master-vert that was established by PairMesh_DoPairing() so slave mesh can be repositioned to master mesh quickly at gametime

	public float _nRadiusSphereBase = 0.070f;			//###TODO: Expose as CProps		####MOD was .065
	public float _nOutsideProtusion = 0.007f;           //###IMPROVE: Autoscale when we have breast growth!

	public CBodyColBreasts() {}

	public override void OnDeserializeFromBlender() {
		base.OnDeserializeFromBlender();

		Destroy(GetComponent<Renderer>());									// Destroy the renderer.  We don't need to show our mesh in Unity (only for PhysX)

		//if (CGame.INSTANCE._GameMode == EGameModes.Play)					// Adjust what mesh this collider mesh maps to (breasts or body) depending on game mode at init
		_oPairMesh = CBodyColBreasts_PairMesh.ToBreasts;		//####DEV ####NOW: Obsolete this dual collider mode I JUST CREATED???
		//else
		//	_oPairMesh = CBodyColBreasts_PairMesh.ToBody;

		//####BROKEN: Also split??
		//CMemAlloc<byte> memBA = new CMemAlloc<byte>();
		//CGame.gBL_SendCmd_GetMemBuffer("'Breasts'", "CBodyColBreasts_GetColliderInfo('" + oBody._sNameGameBody + "-BreastCol-" + _oPairMesh.ToString() + "')", ref memBA);		// Call the Blender-side of our function to retrieve the collider information we require to form breast colliders in PhysX
		//byte[] oBA = (byte[])memBA.L;
		//int nPosBA = 0;

		////=== Receive the 'VertSphereRadiusRatio' flat array that contain the relative radius of each of our spheres / vertices ===
		//int nArrayElements = BitConverter.ToInt32(oBA, nPosBA) / 2; nPosBA += 4;
		//_memVertSphereRadiusRatio = new CMemAlloc<ushort>(nArrayElements);
		//for (int nArrayElement = 0; nArrayElement < nArrayElements; nArrayElement++) {      // Stream in the flat array and store in memArray for sharing with C++ side
		//	_memVertSphereRadiusRatio.L[nArrayElement] = BitConverter.ToUInt16(oBA, nPosBA); nPosBA += 2;
		//}
		//_memVertSphereRadiusRatio.PinInMemory();

		////=== Receive the 'CapsuleSpheres' flat array that tells PhysX how to form tapered capsules from two linked spheres ===
		//nArrayElements = BitConverter.ToInt32(oBA, nPosBA) / 2; nPosBA += 4;
		//_memCapsuleSpheres = new CMemAlloc<ushort>(nArrayElements);
		//for (int nArrayElement = 0; nArrayElement < nArrayElements; nArrayElement++) {
		//	_memCapsuleSpheres.L[nArrayElement] = BitConverter.ToUInt16(oBA, nPosBA); nPosBA += 2;
		//}
		//_memCapsuleSpheres.PinInMemory();

		////=== Read the 'end magic number' that always follows a stream.  Helps catch deserialization errors ===
		//ReadEndMagicNumber(ref oBA, ref nPosBA);


		////===== Obtain the 'slave vert to master vert' constructed by PairMesh_DoPairing() during body construction =====  ####IMPROVE: Create subclass of CBMesh called CBPairedMesh?? ####DESIGN ####SOON
		//CGame.gBL_SendCmd_GetMemBuffer("'CBBodyCol'", "PairMesh_GetVertMapSlaveToMaster('" + oBody._sNameGameBody + "-BreastCol-" + _oPairMesh.ToString() + "')", ref memBA);
		//oBA = (byte[])memBA.L;
		//nPosBA = 0;
		//nArrayElements = BitConverter.ToInt32(oBA, nPosBA) / 2; nPosBA += 4;
		//_memMapPairMeshSlaveToMaster = new CMemAlloc<ushort>(nArrayElements);
		//for (int nArrayElement = 0; nArrayElement < nArrayElements; nArrayElement++) {
		//	_memMapPairMeshSlaveToMaster.L[nArrayElement] = BitConverter.ToUInt16(oBA, nPosBA); nPosBA += 2;
		//}
		//ReadEndMagicNumber(ref oBA, ref nPosBA);
		//_memMapPairMeshSlaveToMaster.PinInMemory();

		////=== Create the CBodyColBreasts PhysX3 object that will absorb the location of the PhysX2 softbody vert position to convert to an approximation of the breast position in PhysX3 ===
		//_hBodyColBreasts = ErosEngine.BodyColBreasts_Create(_memVerts.L.Length, _memVerts.P, _memNormals.P, _memVertSphereRadiusRatio.L.Length, _memVertSphereRadiusRatio.P, _memCapsuleSpheres.L.Length / 2, _memCapsuleSpheres.P);

		//UpdateVertsFromBlenderMesh(true);					// Update the collider verts once to form the colliders for cloth
	}

	public override void UpdateVertsFromBlenderMesh(bool bUpdateNormals) {			// Only called from the context when we're paired to body... ####DESIGN: Update verts outselves??
		base.UpdateVertsFromBlenderMesh(bUpdateNormals);						// First update our verts from Blender, then update PhysX3 with these new vert positions
		ErosEngine.SoftBody_Breasts_UpdateCBodyColBreasts(_hBodyColBreasts, _memVerts.P, _memNormals.P,  _nRadiusSphereBase, _nOutsideProtusion, (int)(EColGroups.eLayerBodyNoCollisionWithSelfStart + _oBody._nBodyID));
	}

	public override void OnDestroy() {
		ErosEngine.BodyColBreasts_Destroy(_hBodyColBreasts);
		base.OnDestroy();
	}

	public void OnSimulateBetweenPhysX23() {
		//=== Right after PhysX has update the softbody position of our breasts, we need to now update the related colliders in PhysX3 scene so it repells cloth for this time frame ===
		if (_oBody._oBreasts != null) { 
			int nMapEntries = _memMapPairMeshSlaveToMaster.L.Length / 2;                        // Each map entry takes two slots in array in order slave vert 1, master vert 1, slave vert 2, master vert 2, etc.
			for (int nMapEntry = 0; nMapEntry < nMapEntries; nMapEntry++) {						//####IMPROVE: Do this in C++!!  ####OPT
				int nVertSlave  = _memMapPairMeshSlaveToMaster.L[2*nMapEntry + 0];
				int nVertMaster = _memMapPairMeshSlaveToMaster.L[2*nMapEntry + 1];
				_memVerts.L[nVertSlave] = _oBody._oBreasts._memVerts.L[nVertMaster];
			}
			//=== Update PhysX3 collider ===
			ErosEngine.SoftBody_Breasts_UpdateCBodyColBreasts(_hBodyColBreasts, _memVerts.P, _memNormals.P,  _nRadiusSphereBase, _nOutsideProtusion, (int)(EColGroups.eLayerBodyNoCollisionWithSelfStart + _oBody._nBodyID));

			//=== Update optional rendered mesh (for debugging of collider) ===
			//_oMeshNow.vertices = _memVerts.L;			// Optional: Update the invisible mesh (no triangles) to visualize in Unity debugger
			//MeshFilter oMeshFilter 	= (MeshFilter)  CUtility.FindOrCreateComponent(transform, typeof(MeshFilter));
			//oMeshFilter.mesh = _oMeshNow;
		}
	}
}

public enum CBodyColBreasts_PairMesh {			// The 'pair mesh' this breast collider sticks its verts to.
	ToBody,										// Collider verts matched to move along body.  (e.g. during body morphing)
	ToBreasts,									// Collider verts matched to move with simulated softbody breasts (e.g. during gameplay)
}
