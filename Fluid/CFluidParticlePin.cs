using UnityEngine;

public class CFluidParticlePin : MonoBehaviour {        // CFluidParticlePin: Wraps a single vertex in a body mesh fluid collider.  Moves a kinematic fluid pin particle along the normal of a mesh vert to enable strong adhesion of fluid particle to move with the body.
	CFlexParamsFluid		_oFlexParamsFluid;
	public CFlexTriCol      _oFlexTriCol;
	public uint				_nParticleID;
	public uint             _nVertID;
	public Vector3          _vecVertPos;
	public Vector3          _vecVertNormal;
	public Vector3          _vecPosParticle;
	Quaternion				_quatRotation = new Quaternion();       // To avoid creating each frame

	public static CFluidParticlePin Create(CFlexParamsFluid oFlexParamsFluid, CFlexTriCol oFlexTriCol, uint nVertID) {
		GameObject oGOT = Resources.Load("Prefabs/CFluidParticlePin") as GameObject;    //###OPT:!! Transform / GameObject only required for debugging.  Turn into non-transform class for higher gametime efficiency?
		GameObject oGO = GameObject.Instantiate(oGOT) as GameObject;
		oGO.transform.SetParent(oFlexParamsFluid.transform);
		CFluidParticlePin oFluidParticlePin = oGO.GetComponent<CFluidParticlePin>();
		oFluidParticlePin.Initialize(oFlexParamsFluid, oFlexTriCol, nVertID);
		oGO.name = "CFluidParticlePin" + oFluidParticlePin._nParticleID.ToString();
		return oFluidParticlePin;
	}

	public void Initialize(CFlexParamsFluid oFlexParamsFluid, CFlexTriCol oFlexTriCol, uint nVertID) {
		_oFlexParamsFluid = oFlexParamsFluid;
		_oFlexTriCol = oFlexTriCol;
		_nVertID = nVertID;
		
		//=== Obtain our pinning particle from the reserved bank of pinning particles and configure as kinematic.  (We move it at each game frame) ===
		_nParticleID = _oFlexParamsFluid.ParticleBank_GetNextParticle(/*bFromParticlePinBank=*/true);
		_oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particlesActivity[_nParticleID] = true;
		_oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].invMass = 0;
		_oFlexParamsFluid._oFlexParamsFluidParticleBank.m_colours[_nParticleID] = CFluidParticleGroup.s_color_PinParticle;

		//=== Manually update our position upon creation so dependant code gets valid positions ===
		Vector3[] aVerts   = _oFlexTriCol._oMeshBaked.vertices;
		Vector3[] aNormals = _oFlexTriCol._oMeshBaked.normals;
		UpdatePosition(ref aVerts, ref aNormals);
	}

	public void UpdatePosition(ref Vector3[] aVerts, ref Vector3[] aNormals) {  // Updates the position of both our vertex and our particle.  MUST be called from the context of the uFlex callback 
		_vecVertPos		= aVerts  [_nVertID];
		_vecVertNormal	= aNormals[_nVertID];
		transform.position = _vecVertPos;
		_quatRotation.SetLookRotation(_vecVertNormal, Vector3.zero);		//###IMPROVE: Add a workable 'known neighbor vert' zero vector for 'up'?
		transform.rotation = _quatRotation;

		//=== Update our pinning particle position = our vert position and inset by a known amount along our negative normal (inside body) ===
		_vecPosParticle = _vecVertPos - _vecVertNormal * _oFlexParamsFluid._nPinPlane_VertPullAmount;
		_oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].pos = _vecPosParticle;
		//_oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].invMass = 0;	//@#??? 
	}
}
