using UnityEngine;


public class CFluidParticlePin : MonoBehaviour {        // CFluidParticlePin: Wraps a single vertex in a body mesh fluid collider.  Moves a kinematic fluid pin particle along the normal of a mesh vert to enable strong adhesion of fluid particle to move with the body.
	public CFlexTriCol      _oFlexTriCol;
    public CFluidParticle   _oFluildParticle;           // The kinematic particle this pin creates to act as a fixed point to pull the dynamic particles connected to this pin via Flex springs
	public ushort           _nVert;                   // Our vertex in the '_oFlexTriCol' collider mesh.  Determines the position / rotation of our pin each frame.
    public ushort           _nVertNeighbor;           // The vertex ID in the '_oFlexTriCol' collider mesh of a known neighbor.  Makes it possible for us to have consistent rotation via LookRotation() call pointing to our normal and oriented to that local neighbor
    public Vector3          _vecVertPos;
	public Vector3          _vecVertNormal;
	public Vector3          _vecPosParticle;
    public int              _nFluidBaked_BoneOrdinal;               // Every fluid pin must have a 'bone' in the skinned baked fluid.  This is the ordinal of our corresponding bone in the global CFluidBaked
	Quaternion				_quatRotation = new Quaternion();       // To avoid creating each frame

	public static CFluidParticlePin Create(CFlexTriCol oFlexTriCol, ushort nVert, ushort nVertNeighbor) {
        CFluidParticlePin oFluidParticlePin = CUtility.InstantiatePrefab<CFluidParticlePin>("Prefabs/CFluidParticlePin", "FluidParticlePin", CGame._oFlexParamsFluid.transform);
        oFluidParticlePin.gameObject.name = "CFluidParticlePin" + nVert.ToString();
        oFluidParticlePin.Initialize(oFlexTriCol, nVert, nVertNeighbor);
        return oFluidParticlePin;
	}

	public void Initialize(CFlexTriCol oFlexTriCol, ushort nVert, ushort nVertNeighbor) {
        //###NOTE: Pin won't have valid position / orientation until RegisterNeighbor() pushes in a known neighbor
		_oFlexTriCol = oFlexTriCol;
        _nVert          = nVert;
        _nVertNeighbor  = nVertNeighbor;

        //=== Obtain our pinning particle from the reserved bank of pinning particles and configure as kinematic.  (We move it at each game frame) ===
        Matrix4x4 oMatDummy_HACK = transform.localToWorldMatrix;
        _oFluildParticle = new CFluidParticle(null, Vector3.zero, 0, 0, 0, 0, ref oMatDummy_HACK);      //###WEAK: Re-use of CFluidParticle for our static one but much of its functionality is extraneous... create a simpler class?
        CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particlesActivity[_oFluildParticle._nParticleID] = true;   //###MOVE?
        CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_oFluildParticle._nParticleID].invMass = 0;
        if (CGame._oFlexParamsFluid.D_ShowParticles_Debug)
            CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_colours[_oFluildParticle._nParticleID] = CFluidParticleGroup.s_color_PinParticle;

        //=== Manually update our position upon creation so dependant code gets valid positions ===
        Mesh oMeshBaked = _oFlexTriCol.Baking_GetBakedSkinnedMesh();
        Vector3[] aVerts    = oMeshBaked.vertices;
        Vector3[] aNormals  = oMeshBaked.normals;
        UpdatePosition(ref aVerts, ref aNormals);
        _nFluidBaked_BoneOrdinal = CGame._oFluidBaked.RegisterBoneForFluidBaking(transform);     //###DESIGN: Pass in pin instead?
    }

	public void UpdatePosition(ref Vector3[] aVerts, ref Vector3[] aNormals) {  // Updates the position of both our vertex and our particle.  MUST be called from the context of the uFlex callback 
        Debug.Assert(aVerts != null);
        Debug.Assert(aNormals != null);
        _vecVertPos     = aVerts  [_nVert];
		_vecVertNormal	= aNormals[_nVert];
        Vector3 vecVertPosNeighbor = aVerts[_nVertNeighbor];
        Vector3 vecToNeighbor = (vecVertPosNeighbor - _vecVertPos); //.normalized;

        transform.position = _vecVertPos;
        _quatRotation.SetLookRotation(_vecVertNormal, vecToNeighbor);
        transform.rotation = _quatRotation;

        //=== Update our pinning particle position = our vert position and inset by a known amount along our negative normal (inside body) ===
        _vecPosParticle = _vecVertPos - _vecVertNormal * CGame._oFlexParamsFluid._nPinPlane_VertPullAmount;
        CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_oFluildParticle._nParticleID].pos = _vecPosParticle;
	}
}
