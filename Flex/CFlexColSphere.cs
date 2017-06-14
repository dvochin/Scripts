using UnityEngine;
using System.Collections.Generic;


public class CFlexColSphere : MonoBehaviour {
	// CFlexColSphere: Manages a super-efficient sphere collider to provide fast raycast ability for codebase to query upon.  
	// Each vertex of a reduced-geometry flex collider is represented by an efficient PhysX sphere collider to service various raycasts.  Examples include:
	// - Fluid particle querying where triangle colliders should form in the fluid scene.
	// - Hand raycasts attempt to find which vert on breasts or penis to adhere too, etc.
	//###LEARN: Classes *must* be in their own file for them to be insertable as components by Unity's GUI

	CFlexColBody		_oFlexColBody;
	int					_nVert;
	List<CFlexColTri>	_aFlexColTrisConnected = new List<CFlexColTri>();		// List of triangle that use this vert.  Facilitates fast mesh traversal
	bool				_bActivated;			// When true this vert has been 'activated' -> it has activated its connected triangles
	Vector3				_vecNormal;

	const float C_SizeSpheres = 0.05f;			//###TEMP23:!!! Need to feed sphere size from Blender!


	public void Initialize(CFlexColBody oFlexColBody, int nVert, ref Vector3[] aVerts, ref Vector3[] aNormals) {
		_oFlexColBody = oFlexColBody;
		_nVert = nVert;

		Vector3 vecVert		= aVerts[_nVert];
		_vecNormal = aNormals[_nVert];
		Vector3 vecCenter	= vecVert - C_SizeSpheres * _vecNormal;

		//=== Assign our coordinates
		transform.position = vecCenter;
		transform.localScale = new Vector3(C_SizeSpheres, C_SizeSpheres, C_SizeSpheres);

		//=== Create visulization components ===
		MeshRenderer oMeshRend = gameObject.GetComponent<MeshRenderer>();
		oMeshRend.material.color = CFlexColBody.s_color_Inactive;
		oMeshRend.enabled = false;
	}

	public void UpdateColPositionsInFluidScene(uFlex.FlexColliders oFlexSceneColliders, ref Vector3[] aVerts, ref Vector3[] aNormals) {
		//###OPT: Extremely performance-critical.  Keep ULTRA FAST!!
		Vector3 vecVert		= aVerts[_nVert];
		Vector3 vecNormal	= aNormals[_nVert];
		Vector3 vecCenter	= vecVert - C_SizeSpheres * vecNormal;
		transform.position = vecCenter;
	}

	public void ConnectTriToThisVert(CFlexColTri oFlexColTri) {
		_aFlexColTrisConnected.Add(oFlexColTri);
	}

	public void Activate(Vector3 vecParticleVelocity) {
		if (_bActivated == false) {
			if (Vector3.Angle(vecParticleVelocity, _vecNormal) < 180.0f) {	// Avoid activating these colliders if this collider pointing the other way  (Flex would let it through anyway!)
				if (CGame.INSTANCE._bShowFlexFluidColliders_HACK) { 
					MeshRenderer oRend = GetComponent<MeshRenderer>();
					oRend.material.color = CFlexColBody.s_color_RayCast;
					//oRend.enabled = true;
				}

				foreach (CFlexColTri oFlexColTri in _aFlexColTrisConnected)
					oFlexColTri.Activate();

				CGame.INSTANCE._oFlexFluid._nStat_NumActiveColliderSpheres++;
				_bActivated = true;
			}
		}
	}
}
