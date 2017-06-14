using UnityEngine;
using System;


public class CFlexColTri {         // CFlexColTri: Manages a single Flex collider triangle.  This triangle repells fluid in the Flex fluid solver.  It is a component on an object that also has a BoxCol for precise PhysX raycasts (to predict where collisions will occur)

	CFlexColBody				_oFlexColBody;			// Our owning Flex Col body
	public EFlexColTriStatus	_eFlexColTriStatus  = EFlexColTriStatus.Inactive;		// The current 'status' of this collider: inactive, awaiting insertion or inserted (active).
	int							_nTri;						// The triangle in '_oFlexColBody' we represent.
	int							_nFlexColID = -1;			// The ID of this collider in the fluid Flex scene.  Set in InsertIntoFlexScene()
	int							_nTriIndex1, _nTriIndex2, _nTriIndex3;      // The three triangle indices in our source mesh.  Stored so we can update position!
	Vector3						_vecPosition;				// The position...
	Quaternion					_quatRotation;				// ... and rotation of our collider.  As we can't change the position of our three vertices once pushed into Flex we change our entire position / orientation in Flex.
	Vector3						_vecCollisionSpanAABB;      // Span around this collider center.  Used to feed global-space AABB Flex needs to perform first-stage fast culling
	Mesh						_oMesh;						// The mesh we create for Flex.  Becomes null when no longer needed
	Transform					_oDebugVisualizerT;			// Our visualizer (if created)  Visually shows where we are

	static int[]				s_aTrisOneTriangle = new int[3] { 0, 1, 2 };		// Static array of triangle indices.  Every Flex collider we created is a triangle and this is the triangle list (only one triangle)
	//static float				s_nRatioTriangleEnlarge = 1.0f;		// Ratio to 'expand' triangles to help catch particles falling through cracks!		###LEARN: Expanding causes big blobs to fall through = worse!
	

	public CFlexColTri(CFlexColBody oFlexColBody, int nTri, ref Vector3[] aVerts, ref int[] aTris, ref CFlexColSphere[]	aColSpheres) {
		//=== Collect the information we need for this instance ===
		_oFlexColBody = oFlexColBody;
		_nTri = nTri;
		_nTriIndex1 = aTris[nTri * 3 + 0];
		_nTriIndex2 = aTris[nTri * 3 + 1];
		_nTriIndex3 = aTris[nTri * 3 + 2];
		if (_nTriIndex1 == _nTriIndex2 || _nTriIndex1 == _nTriIndex3 || _nTriIndex2 == _nTriIndex3)
			CUtility.ThrowExceptionF("###EXCEPTION: Invalid triangle in CFlexColTri.");

		//=== Connect this triangle to all three verts for fast mesh traversal ===
		aColSpheres[_nTriIndex1].ConnectTriToThisVert(this);
		aColSpheres[_nTriIndex2].ConnectTriToThisVert(this);
		aColSpheres[_nTriIndex3].ConnectTriToThisVert(this);

		//=== Determine the vertex center in global space ===
		Vector3 vecVertCenter	= Vector3.zero;
		Vector3 vecVert1 = aVerts[_nTriIndex1];
		Vector3 vecVert2 = aVerts[_nTriIndex2];
		Vector3 vecVert3 = aVerts[_nTriIndex3];
		vecVertCenter += vecVert1;
		vecVertCenter += vecVert2;
		vecVertCenter += vecVert3;
		vecVertCenter /= 3;

		//=== Subtract center from our verts to convert back to local space ===
		vecVert1 -= vecVertCenter;
		vecVert2 -= vecVertCenter;
		vecVert3 -= vecVertCenter;

		//=== Make triangle slightly larger than it was defined to be to help catch particles between triangles ===
		//vecVert1 *= s_nRatioTriangleEnlarge;	###LEARN: Expanding causes big blobs to fall through = worse!
		//vecVert2 *= s_nRatioTriangleEnlarge;
		//vecVert3 *= s_nRatioTriangleEnlarge;

		//=== Determine the orientation we're going to give our collider===
		Vector3 vec12 = vecVert2 - vecVert1;							// Any reliable two vectors will be fine for LookRotation (as long as we always calculate our 'rotation' the same way)
		Vector3 vec13 = vecVert3 - vecVert1;
		Vector3 vec23 = vecVert3 - vecVert2;
		Quaternion quatRot = Quaternion.LookRotation(vec12, vec13);				// Calculate our collider's orientation.  Use any reliable vector we have and the highest-performance ones are just two edges (faster than normal)
		Quaternion quatRotInv = Quaternion.Inverse(quatRot);

		//=== 'Unrotate' our vertices by the rotation we determined for our collider -> to convert to our 'fake local space' ===
		vecVert1 = quatRotInv * vecVert1;
		vecVert2 = quatRotInv * vecVert2;
		vecVert3 = quatRotInv * vecVert3;			//###CHECK23: At this point all verts should be glued to 0 on one of the axis? (Might help understanding this to generate tighter AABB?)  (And pin them there?)

		//=== Create a visulization mesh so we can see the colliders while we debug this ===
		_oMesh = new Mesh();
		_oMesh.name = "Tri" + _nTri.ToString();
		Vector3[] aVertsThisTri = new Vector3[3];
		aVertsThisTri[0] = vecVert1;
		aVertsThisTri[1] = vecVert2;
		aVertsThisTri[2] = vecVert3;
		_oMesh.vertices = aVertsThisTri;
		_oMesh.triangles = s_aTrisOneTriangle;
		_oMesh.RecalculateNormals();
		_oMesh.RecalculateBounds();

		//=== Determine the collision span of space needed.  Used to feed Flex AABB for first-stage quick culling ===
		float nLenLongestEdge = Mathf.Max(vec12.magnitude, vec13.magnitude, vec23.magnitude);
		float nLenLongestEdgeDiv2 = nLenLongestEdge / 2;
		_vecCollisionSpanAABB = new Vector3(nLenLongestEdgeDiv2, nLenLongestEdgeDiv2, nLenLongestEdgeDiv2);
		Vector3 vecAABB_Min = vecVertCenter - _vecCollisionSpanAABB;
		Vector3 vecAABB_Max = vecVertCenter + _vecCollisionSpanAABB;

		//=== Assign our coordinates
		_vecPosition = vecVertCenter;
		_quatRotation = quatRot;

		//=== Create the debug visualizer if requested ===
		if (CGame.INSTANCE._bShowFlexFluidColliders_HACK) { 
			GameObject oColTriGOT = Resources.Load("Prefabs/CFlexColTri") as GameObject;
			GameObject oTriGO = GameObject.Instantiate(oColTriGOT) as GameObject;
			oTriGO.name = "T" + nTri.ToString();
			_oDebugVisualizerT = oTriGO.transform;
			_oDebugVisualizerT.SetParent(_oFlexColBody.transform);
			_oDebugVisualizerT.position = vecVertCenter;
			_oDebugVisualizerT.rotation = quatRot;
			_oDebugVisualizerT.localScale = Vector3.one;
			MeshFilter oMeshFilter = _oDebugVisualizerT.GetComponent<MeshFilter>();
			oMeshFilter.mesh = _oMesh;
			MeshRenderer oMeshRend = _oDebugVisualizerT.GetComponent<MeshRenderer>();
			oMeshRend.material.color = CFlexColBody.s_color_Inactive;
			oMeshRend.enabled = false;
		}

		_eFlexColTriStatus = EFlexColTriStatus.Inactive;			// This triangle collider has been created but is inactive.  (We don't need to update its position / rotation every frame)
	}

	public void InsertIntoFlexScene(uFlex.FlexColliders oFlexSceneColliders, int nFlexColID, uFlex.Flex.Memory oFlexMemory) {
		//=== Remember our unique ID in our Flex fluid scene ===
		_nFlexColID = nFlexColID;

		//=== Obtain reference to mesh and its bounds ===
		Vector3[] aVerts = _oMesh.vertices;
		_oMesh = null;									// Allow mesh to be garbage collected
		Vector3 vecAABB_Min = _vecPosition - _vecCollisionSpanAABB;
		Vector3 vecAABB_Max = _vecPosition + _vecCollisionSpanAABB;

		//=== Create the mesh in GPUI memory ===
		IntPtr meshPtr = uFlex.Flex.CreateTriangleMesh();
		uFlex.Flex.UpdateTriangleMesh(meshPtr, aVerts, s_aTrisOneTriangle, 3, 1, ref vecAABB_Min, ref vecAABB_Max, oFlexMemory);

		//=== Create the GPU memory for our mesh ===
		uFlex.Flex.CollisionTriangleMesh triCol = new uFlex.Flex.CollisionTriangleMesh();
		triCol.mMesh = meshPtr;
		triCol.mScale = 1;

		//=== Insert this collider into Flex fluid scene ===
		oFlexSceneColliders.m_collidersGeometry		[_nFlexColID] = triCol;
		oFlexSceneColliders.m_collidersPrevPositions[_nFlexColID] = oFlexSceneColliders.m_collidersPositions[_nFlexColID];
		oFlexSceneColliders.m_collidersPositions	[_nFlexColID] = _vecPosition;
		oFlexSceneColliders.m_collidersPrevRotations[_nFlexColID] = oFlexSceneColliders.m_collidersRotations[_nFlexColID];
		oFlexSceneColliders.m_collidersRotations	[_nFlexColID] = _quatRotation;
		oFlexSceneColliders.m_collidersAabbMin		[_nFlexColID] = vecAABB_Min;
		oFlexSceneColliders.m_collidersAabbMax		[_nFlexColID] = vecAABB_Max;
		oFlexSceneColliders.m_collidersStarts		[_nFlexColID] = _nFlexColID;
		oFlexSceneColliders.m_collidersFlags		[_nFlexColID] = uFlex.Flex.MakeShapeFlags(uFlex.Flex.CollisionShapeType.eFlexShapeTriangleMesh, true);

		//=== Update our flags and color ===
		_eFlexColTriStatus = EFlexColTriStatus.Inserted;				// This triangle collider has been inserted into the Flex scene.  We now need to update its position / rotation every frame
		CGame.INSTANCE._oFlexFluid._nStat_NumActiveColliderTris++;
		if (_oDebugVisualizerT != null) 
			_oDebugVisualizerT.GetComponent<MeshRenderer>().material.color = CFlexColBody.s_color_Active;	// Change our visulazation color so we can easily tell active from inactive
	}

	public void UpdateColPositionsInFluidScene(uFlex.FlexColliders oFlexSceneColliders, ref Vector3[] aVerts) {     // Called with updated baked verts.  Update the position of our entire transform so our fixed-triangle is properly positioned for fluid repelling
		//###OPT: Performance-critical.  Keep FAST!!

		//=== Determine position from triangle center ===  
		_vecPosition = Vector3.zero;					//###OPT:!! Can we just take one vert??  (AABB more complex??)
		Vector3 vecVert1 = aVerts[_nTriIndex1];			//###OPT: Can probably speed center calcs a bit...
		Vector3 vecVert2 = aVerts[_nTriIndex2];
		Vector3 vecVert3 = aVerts[_nTriIndex3];
		_vecPosition += vecVert1;
		_vecPosition += vecVert2;
		_vecPosition += vecVert3;
		_vecPosition /= 3;

		//=== Determine our rotation with 'LookRotation' and two vectors ===
		Vector3 vec12 = vecVert2 - vecVert1;
		Vector3 vec13 = vecVert3 - vecVert1;				//###OPT: Can use vertex normal instead?
		_quatRotation = Quaternion.LookRotation(vec12, vec13);				// Calculate our collider's orientation.  Use any reliable vector we have and the highest-performance ones are just two edges (faster than normal)

		//=== Set our collider's position in global 3D space.  This will both repel fluid and service accurate scene ray casting ===
		if (_oDebugVisualizerT != null) { 
			_oDebugVisualizerT.position = _vecPosition;
			_oDebugVisualizerT.rotation = _quatRotation;
		}

		//=== Update our position in our Flex fluid scene (if we've been inserted) ===
		if (_nFlexColID != -1) { 
			Vector3 vecAABB_Min = _vecPosition - _vecCollisionSpanAABB;
			Vector3 vecAABB_Max = _vecPosition + _vecCollisionSpanAABB;
			oFlexSceneColliders.m_collidersPrevPositions[_nFlexColID] = oFlexSceneColliders.m_collidersPositions[_nFlexColID];
			oFlexSceneColliders.m_collidersPositions	[_nFlexColID] = this._vecPosition;
			oFlexSceneColliders.m_collidersPrevRotations[_nFlexColID] = oFlexSceneColliders.m_collidersRotations[_nFlexColID];
			oFlexSceneColliders.m_collidersRotations	[_nFlexColID] = this._quatRotation;
			oFlexSceneColliders.m_collidersAabbMin		[_nFlexColID] = vecAABB_Min;
			oFlexSceneColliders.m_collidersAabbMax		[_nFlexColID] = vecAABB_Max;
		}
	}

	public void Activate() {
		if (_eFlexColTriStatus == EFlexColTriStatus.Inactive) {
			if (CGame.INSTANCE._bShowFlexFluidColliders_HACK && _oDebugVisualizerT != null)
				_oDebugVisualizerT.GetComponent<MeshRenderer>().enabled = true;
			_oFlexColBody.AppendTriangleColliderToAwaitingInsertionList(this);
			_eFlexColTriStatus = EFlexColTriStatus.AwaitingInsertion;
		}
	}
}

public enum EFlexColTriStatus {
	Inactive,				// Tri collider is inactive and doesn't repell anything in the Flex fluid scene.
	AwaitingInsertion,		// Tri collider is awaiting insertion into the Flex fluid scene (must be done when Flex is ready)
	Inserted,				// Tri collider is inserted into the Flex fluid scene, is actively being moved and rotated and is accurately repelling fluid.
}


//oMeshRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;          // Turning this stuff off doesn't appear to help performance very much!
//oMeshRend.receiveShadows = false;
//oMeshRend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
//oMeshRend.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
//oMeshRend.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
//oMeshRend.material = Resources.Load("Materials/BasicColors/TransYellow", typeof(Material)) as Material;

//gameObject.layer = LayerMask.NameToLayer("NoCollision");			//###LEARN: How to set layers by name
//oBoxCol.size = new Vector3(vecAABB_Max.x - vecAABB_Min.x, vecAABB_Max.y - vecAABB_Min.y, vecAABB_Max.z - vecAABB_Min.z);		// AABB by longest edge is safe and less maintenance but doesn't cull as aggressively!  Better to tighten up the AABB in C# even though it's more expensive??  (a trade off)
