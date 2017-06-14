/*###DISCUSSION: Flex Body Col - June 2017 - ###DOCS23:

=== DEV ===

=== LAST ===
- Need to get fluid flex colliders from ALL bodies!
- Need to redo entire flex collider code base... how classes interoperate, how they are created, etc

=== NEXT ===
- Interleave spheres pos update now very sparse... a problem
	- Might need to split into an 'updated once a frame' group and 'updated when possible' group... Blender needs to tell us!
- Merge with CFlexSkinnedBody?  These two should be aware of each other!!
-Q: Need Rigid Body on sphere for its capacity to accurately repell arms and legs?
- Need tighter integration of flex body collider with fluid colliders!
- Still some occasional punch-through of fluid into body... How to fix without killing performance?
	- Add a once-per-frame raycaster from penis?
- Check: Make sure we insert into Flex the most up-to-date position! (Might have to bake first?)
	+ Can have a 'frame last baked counter' on baker and a global frame count to avoid baking multiple times per frame!
- Sphere colliders now all same size... would be nicer with different radius to 'fill' the mesh better than lowest common denominator?

=== TODO ===

=== LATER ===

=== IMPROVE ===
- Would be nice to have a vertex-lit Flex collider main mesh for us to relay debug info during development!

=== NEEDS ===
- Fluid scene has to accept triangle colliders from multiple 'CFlexColBody'.
	- Given they are just triangles we probably don't need to insert in the middle of its arrays but just append to them (from whatever body needs to activate more colliders)
- We have a need to visualize which collider is active (e.g. create a one-triangle mesh) to show them.

=== DESIGN ===

=== IDEAS ===
- Mesh walking may help performance instead of raycasting so heavily?
- Mesh walking hands to 'caress' body would be awesome!
- Can have two pools: the 'critical colliders' (e.g. genitals, belly, breasts, inner legs) and the rest.  Non-critical colliders can get interleave-updates over a few frames

=== LEARNED ===

=== PROBLEMS ===

=== OPTIMIZATIONS ===
- AABBs on tris very simple but not as efficient as real ones?
- Sphere.UpdateColPositionsInFluidScene takes 4ms for ONE body -> interleave!!!!!
- Flex.SetShape 2.6 on min shapes = expensive!!
- Flex usage of FindObjectsOfType() = 3ms!!!!!!
	- Process Extra springs = 2.6ms!  what springs?????
+ Keep calculating center?  Expensive!  Can just use a vert?  (AABB would need adjustment)

=== QUESTIONS ===

=== WISHLIST ===

*/

using UnityEngine;
using System.Collections.Generic;



public class CFlexColBody : CBSkin {					// CFlexColBody: Manages a skinned reduced-geometry body.  Being a collection of CFlexColTri, it is responsible for 1) PhysX ray casting, 2) Repelling cloth in main Flex solver and 3) Repelling fluid in Flex fluid solver
	uFlex.FlexColliders		_oFlexColliders;			
	CFlexSkinnedBody		_oFlexSkinnedBody;			// Our skinned body component.  Responsible to bake the verts at each frame
	int						_nVerts;					// Number of verts in the source mesh
	int						_nTris;						// Number of triangles in the source mesh
	CFlexColSphere[]		_aColSpheres;				// All our collider spheres.
	CFlexColTri[]			_aColTris;					// All our collider triangles.
	List<CFlexColTri>		_aListTrisAwaitingInsertion	= new List<CFlexColTri>();			// List of triangle colliders awaiting insertion into Flex Fluid scene.
	List<CFlexColTri>		_aListTrisActivated			= new List<CFlexColTri>();          // List of triangle that have been activated (i.e. Are moved at every frame and repell fluid in Flex fluid solver)
	CFlexFluid				_oFlexFluid;

	public static Color32	s_color_Inactive			= new Color32(255, 255, 0, 64);		//###TODO23: Move these to some global class like G
	public static Color32	s_color_Active				= new Color32(0, 255, 0, 96);
	public static Color32	s_color_RayCast				= new Color32(255, 0, 0, 255);

	public int _nInterleaveUpdateCount_HACK = 4;
	public int _nInterleaveUpdateNow;


	public void Initialize() {
		//=== Obtain access to the components we need ===
		_oFlexFluid = CGame.INSTANCE._oFlexFluid;

		if (_oFlexFluid != null) {			//###KEEP23:?
			_oFlexColliders = _oFlexFluid.GetComponent<uFlex.FlexColliders>();
			_oFlexColliders.RegisterFlexColliderBody(this);
		} else {
			return;
		}
		_oFlexSkinnedBody = GetComponent<CFlexSkinnedBody>();					//###WEAK23: ???

		//=== Obtain access to the source mesh verts, normals and triangles ===
		Mesh oMeshSrc = _oSkinMeshRendNow.sharedMesh;
		Vector3[]	aVerts		= oMeshSrc.vertices;
		Vector3[]	aNormals	= oMeshSrc.normals;
		int[]		aTris		= oMeshSrc.triangles;
		_nVerts		= aVerts.Length;
		_nTris		= aTris.Length / 3;

		//=== Create the collision spheres from source mesh verts ===
		_aColSpheres = new CFlexColSphere[_nVerts];
		GameObject oColSphereGOT = Resources.Load("Prefabs/CFlexColSphere") as GameObject;
		for (int nVert = 0; nVert < _nVerts; nVert++) {
			GameObject oColSphereGO = GameObject.Instantiate(oColSphereGOT) as GameObject;
			oColSphereGO.name = "V" + nVert.ToString();
			oColSphereGO.transform.SetParent(transform);
			CFlexColSphere oFlexColSphere = oColSphereGO.GetComponent<CFlexColSphere>();
			oFlexColSphere.Initialize(this, nVert, ref aVerts, ref aNormals);
			_aColSpheres[nVert] = oFlexColSphere;
		}

		//=== Create the collision triangles from source mesh triangles ===
		_aColTris = new CFlexColTri[_nTris];
		for (int nTri = 0; nTri < _nTris; nTri++)
			_aColTris[nTri] = new CFlexColTri(this, nTri, ref aVerts, ref aTris, ref _aColSpheres);
	}

	public void UpdateColPositionsInFluidScene(uFlex.Flex.Memory oFlexMem) {            //###CHECK23: Timing right?  Not off by one frame??  (Can request baking if need be)
		//###OPT: Performance-critical.  Keep FAST!!

		//=== Update the position of all sphere colliders.  Enables accurate raycasts on complex body shape ===
		Vector3[] aVerts	= _oFlexSkinnedBody._oMeshSkinBaked.vertices;         //###WEAK23: Don't like the relationship with this class... merge?
		Vector3[] aNormals	= _oFlexSkinnedBody._oMeshSkinBaked.normals;
		if (CGame.INSTANCE._bDisableExpensiveFluidCollisions_HACK == false) { 
			//for (int nVert = 0; nVert < _nVerts; nVert++) { 
			for (int nVert = _nInterleaveUpdateNow; nVert < _nVerts; nVert += _nInterleaveUpdateCount_HACK) { 
				CFlexColSphere oFlexColSphere = _aColSpheres[nVert];
				oFlexColSphere.UpdateColPositionsInFluidScene(_oFlexColliders, ref aVerts, ref aNormals);
			}
			_nInterleaveUpdateNow++;
			if (_nInterleaveUpdateNow >= _nInterleaveUpdateCount_HACK)
				_nInterleaveUpdateNow = 0;
		}
		
		//=== Perform raycasts on our 'observer fluid particles' -> Performs just-in-time activation of fluid colliders
		_oFlexFluid.FluidParticleRaycaster_PerformRaycast();

		//=== Add triangles to the Fluid Flex scene that have been marked as 'awaiting insertion'
		if (_aListTrisAwaitingInsertion.Count > 0) { 
			int nFlexColStartID = _oFlexColliders.AddSpaceForAdditionalColliders(_aListTrisAwaitingInsertion.Count);      // Expand our collider's arrays
			int nNewCol = 0;
			foreach (CFlexColTri oFlexColTri in _aListTrisAwaitingInsertion) {
				oFlexColTri.InsertIntoFlexScene(_oFlexColliders, nFlexColStartID + nNewCol, oFlexMem);
				nNewCol++;
				_aListTrisActivated.Add(oFlexColTri);		// All inserted and now active. (we have to maintain its position every frame (expensive))
			}
			_aListTrisAwaitingInsertion.Clear();		// All inserted into scene.  Empty for next batch to insert.
		}

		//=== Update positions of all activated triangles (expensive) ===
		foreach (CFlexColTri oFlexColTri in _aListTrisActivated)
			oFlexColTri.UpdateColPositionsInFluidScene(_oFlexColliders, ref aVerts);
	}

	public void AppendTriangleColliderToAwaitingInsertionList(CFlexColTri oFlexColTri) {
		_aListTrisAwaitingInsertion.Add(oFlexColTri);			// Triangle wants activation.  Append to our list of colliders waiting insertion.  Once that completes it will be inserted in _aListTrisActivated
	}
}

//Vector3 vec23 = vecVert3 - vecVert2;
//vecTriNormal = Vector3.Cross(vec12, vec13).normalized;
//vecTriTangent = vec12;
//float nLenLargestEdge = Mathf.Max(vec12.magnitude, vec13.magnitude, vec23.magnitude);
//Vector3 _vecCollisionSpanAABB = new Vector3(nLenLargestEdge, nLenLargestEdge, nLenLargestEdge);
//Vector3 vecAABB_Min = vecVertCenter - _vecCollisionSpanAABB;
//Vector3 vecAABB_Max = vecVertCenter + _vecCollisionSpanAABB;



//Vector3 vecAABB_Min = transform.localToWorldMatrix.MultiplyPoint(oMesh.bounds.min);
//Vector3 vecAABB_Max = transform.localToWorldMatrix.MultiplyPoint(oMesh.bounds.max);


//if (vecVert1 == vecVert2 || vecVert1 == vecVert3 || vecVert2 == vecVert3)
//	Debug.LogWarningFormat("#WARNING: Invalid triangle {0} in CFlexColTri.", _nTri);
//if (vec12.sqrMagnitude == 0 || vec23.sqrMagnitude == 0)
//	Debug.LogWarningFormat("#WARNING: Invalid vectors {0} in CFlexColTri.", _nTri);
//Vector3 vecNormal = Vector3.Cross(vec12, vec13).normalized;
//if (vecNormal.sqrMagnitude == 0)
//	Debug.LogWarningFormat("#WARNING: Invalid vecNormal {0} in CFlexColTri.", _nTri);
//Quaternion quatRot = Quaternion.LookRotation(vecNormal, vec12);		


//Vector3 vecNormal = Vector3.Cross(vec12, vec13).normalized;		//###LEARN: How to calculate a normal from two edges

//_oRaycaster_HACK = GameObject.Find("Dick-Cum").transform;


//RaycastHit[] aRayHits = new RaycastHit[1];
//aRayHits[0] = oRayHit;
//RaycastHit[] aRayHits = Physics.SphereCastAll(oRay, 0.05f);
//foreach (RaycastHit oRayHit in aRayHits) {
