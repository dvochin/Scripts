using System;
using UnityEngine;
using System.Collections.Generic;

public class CFlexTriCol : CBSkinBaked {        // CFlexTriCol: Converts a reduced-geometry skinned collider mesh into a Flex Triangle Mesh at every frame.  (Responsible to keep cloth, softbodies away in main Flex scene and Fluid particles in Fluid scene)  Owned by CBody

	uFlex.FlexColliders					_oFlexColliders;
	uFlex.Flex.CollisionTriangleMesh	_oFlexTriCol;
	IntPtr								_oTriMeshPtr;
	int									_nFlexColID = -1;
	Dictionary<ushort, CFluidParticlePin>   _mapFluidParticlePins;		// This body collider's collection of fluid particle pins.  Needed to fix fluid particles to body so they don't fly off (provide strong adhesion)

	public void Initialize(uFlex.FlexColliders oFlexColliders) {
		_oFlexColliders = oFlexColliders;
		_oFlexColliders.RegisterFlexTriCol(this);
	}

	public void InsertIntoFlexScene(uFlex.FlexColliders oFlexSceneColliders, uFlex.Flex.Memory oFlexMemory) {
		//=== Create the mesh in GPU memory ===
		Baking_UpdateBakedMesh();
		Vector3[] aVerts =	_oMeshBaked.vertices;
		int[] aTris =		_oMeshBaked.triangles;
		Vector3 vecAABB_Min = _oMeshBaked.bounds.min;
		Vector3 vecAABB_Max = _oMeshBaked.bounds.max;
		_oTriMeshPtr = uFlex.Flex.CreateTriangleMesh();
		uFlex.Flex.UpdateTriangleMesh(_oTriMeshPtr, aVerts, aTris, aVerts.Length, aTris.Length/3, ref vecAABB_Min, ref vecAABB_Max, oFlexMemory);

		//=== Create the Flex structure for our mesh ===
		_oFlexTriCol = new uFlex.Flex.CollisionTriangleMesh();
		_oFlexTriCol.mMesh = _oTriMeshPtr;
		_oFlexTriCol.mScale = 1;

		_nFlexColID = oFlexSceneColliders.AddSpaceForAdditionalColliders(1);

		//=== Insert this collider into Flex fluid scene ===
		oFlexSceneColliders.m_collidersGeometry		[_nFlexColID] = _oFlexTriCol;			//###NOTE: These ask for Vector4!  Vector3 can properly expand into Vector4?
		oFlexSceneColliders.m_collidersPrevPositions[_nFlexColID] = oFlexSceneColliders.m_collidersPositions[_nFlexColID];
		oFlexSceneColliders.m_collidersPositions	[_nFlexColID] = Vector3.zero;
		oFlexSceneColliders.m_collidersPrevRotations[_nFlexColID] = oFlexSceneColliders.m_collidersRotations[_nFlexColID];
		oFlexSceneColliders.m_collidersRotations	[_nFlexColID] = Quaternion.identity;
		oFlexSceneColliders.m_collidersAabbMin		[_nFlexColID] = vecAABB_Min;
		oFlexSceneColliders.m_collidersAabbMax		[_nFlexColID] = vecAABB_Max;
		oFlexSceneColliders.m_collidersStarts		[_nFlexColID] = _nFlexColID;
		oFlexSceneColliders.m_collidersFlags		[_nFlexColID] = uFlex.Flex.MakeShapeFlags(uFlex.Flex.CollisionShapeType.eFlexShapeTriangleMesh, true);		// True as last argument = Dynamic!
	}

	public void UpdateTriColInFlexScene(uFlex.FlexColliders oFlexSceneColliders, uFlex.Flex.Memory oFlexMemory) {     // Called with updated baked verts.  Update the position of our entire transform so our fixed-triangle is properly positioned for fluid repelling
		Baking_UpdateBakedMesh();
		Vector3[] aVerts =	_oMeshBaked.vertices;
		int[] aTris =		_oMeshBaked.triangles;
		Vector3 vecAABB_Min = _oMeshBaked.bounds.min;
		Vector3 vecAABB_Max = _oMeshBaked.bounds.max;
		uFlex.Flex.UpdateTriangleMesh(_oTriMeshPtr, aVerts, aTris, aVerts.Length, aTris.Length/3, ref vecAABB_Min, ref vecAABB_Max, oFlexMemory);
		oFlexSceneColliders.m_collidersAabbMin		[_nFlexColID] = vecAABB_Min;
		oFlexSceneColliders.m_collidersAabbMax		[_nFlexColID] = vecAABB_Max;
	}

	public CFluidParticlePin GetFluidParticlePin(CFlexParamsFluid oFlexParamsFluid, ushort nVert) {
		if (_mapFluidParticlePins == null)
			_mapFluidParticlePins = new Dictionary<ushort, CFluidParticlePin>();
		if (_mapFluidParticlePins.ContainsKey(nVert)) {
			return _mapFluidParticlePins[nVert];
		} else {
			CFluidParticlePin oFluidParticlePin = CFluidParticlePin.Create(oFlexParamsFluid, this, nVert);
			_mapFluidParticlePins.Add(nVert, oFluidParticlePin);
			return oFluidParticlePin;
		} 
	}

	public void FluidParticlePins_UpdatePositions() {
		if (_mapFluidParticlePins != null) {
			Vector3[] aVerts	= _oMeshBaked.vertices;
			Vector3[] aNormals = _oMeshBaked .normals;
			foreach (KeyValuePair<ushort, CFluidParticlePin> oPair in _mapFluidParticlePins)
				oPair.Value.UpdatePosition(ref aVerts, ref aNormals);
		}
	}

	public override void OnSimulate() {
		base.OnSimulate();						// Will 'bake' the skinned mesh so we can access updated positions & normals
		FluidParticlePins_UpdatePositions();
	}
}
