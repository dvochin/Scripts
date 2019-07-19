using System;
using UnityEngine;
using System.Collections.Generic;

public class CFlexTriCol : CSurfaceMesh {        // CFlexTriCol: Converts a reduced-geometry skinned collider mesh into a Flex Triangle Mesh at every frame.  (Responsible to keep cloth, softbodies away in main Flex scene and Fluid particles in Fluid scene)  Owned by CBody
                                                 //###DESIGN: Relationship between CSurfaceMesh and derived CFlexTriCol is awkward and convoluted.  Redo inheritance??
    uFlex.FlexColliders					_oFlexColliders;
	uFlex.Flex.CollisionTriangleMesh	_oFlexTriCol;
	IntPtr								_oTriMeshPtr;
	int									_nFlexColID = -1;
	public Dictionary<ushort, CFluidParticlePin>   _mapFluidParticlePins;		// This body collider collection of fluid particle pins.  Needed to fix fluid particles to body so they don't fly off (provide strong adhesion)

	public void InitializeInFlexScene(uFlex.FlexColliders oFlexColliders) {
		_oFlexColliders = oFlexColliders;
		_oFlexColliders.RegisterFlexTriCol(this);
	}

	public virtual void DoDestroy() {
		_oFlexColliders.UnregisterFlexTriCol(this);
		GameObject.Destroy(gameObject);
	}


    public void InsertIntoFlexScene(uFlex.FlexColliders oFlexSceneColliders, uFlex.Flex.Memory oFlexMemory) {
        //=== Create the mesh in GPU memory ===
        Mesh oMeshBaked = Baking_GetBakedSkinnedMesh();
        Vector3[] aVerts =	oMeshBaked.vertices;
		int[] aTris =		oMeshBaked.triangles;
		Vector3 vecAABB_Min = oMeshBaked.bounds.min;
		Vector3 vecAABB_Max = oMeshBaked.bounds.max;
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
        Mesh oMeshBaked = Baking_GetBakedSkinnedMesh();
        Vector3[] aVerts =	oMeshBaked.vertices;
		int[] aTris =		oMeshBaked.triangles;
		Vector3 vecAABB_Min = oMeshBaked.bounds.min;
		Vector3 vecAABB_Max = oMeshBaked.bounds.max;
		uFlex.Flex.UpdateTriangleMesh(_oTriMeshPtr, aVerts, aTris, aVerts.Length, aTris.Length/3, ref vecAABB_Min, ref vecAABB_Max, oFlexMemory);
		oFlexSceneColliders.m_collidersAabbMin		[_nFlexColID] = vecAABB_Min;
		oFlexSceneColliders.m_collidersAabbMax		[_nFlexColID] = vecAABB_Max;
	}

	public CFluidParticlePin FluidParticlePins_GetPin(CFlexParamsFluid oFlexParamsFluid, ushort nVert, ushort nVertNeighbor) {
        CFluidParticlePin oFluidParticlePin;
        if (_mapFluidParticlePins == null)
			_mapFluidParticlePins = new Dictionary<ushort, CFluidParticlePin>();
		if (_mapFluidParticlePins.ContainsKey(nVert)) {
            oFluidParticlePin = _mapFluidParticlePins[nVert];           //#DEV26: ###CHECK:!!! Need accurate particle pos... Is it up to date???
		} else {
			oFluidParticlePin = CFluidParticlePin.Create(this, nVert, nVertNeighbor);
			_mapFluidParticlePins.Add(nVert, oFluidParticlePin);
		}
        Debug.Assert(oFluidParticlePin);
        return oFluidParticlePin;
	}

	public void FluidParticlePins_UpdatePositions() {
		if (_mapFluidParticlePins != null) {
            Mesh oMeshBaked = Baking_GetBakedSkinnedMesh();
            Vector3[] aVerts	= oMeshBaked.vertices;
			Vector3[] aNormals  = oMeshBaked .normals;
            foreach (KeyValuePair<ushort, CFluidParticlePin> oPair in _mapFluidParticlePins)
				oPair.Value.UpdatePosition(ref aVerts, ref aNormals);
		}
	}

	public override void OnSimulate() {
		base.OnSimulate();						// Will 'bake' the skinned mesh so we can access updated positions & normals
		FluidParticlePins_UpdatePositions();
	}
}
