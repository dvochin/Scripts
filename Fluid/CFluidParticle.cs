using UnityEngine;


public class CFluidParticle {
	public CFluidParticleGroup		_oFluidParticleGroup;                   // The particle group we belong to.  Manages us.  Exception is when we're a pin particle
	public int						_nParticleID;                           // Our particle ID.  -1 when we don't have a real Flex-simulated particle
	public ushort                   _nParticleOrdinalInGroup;
	public byte                     _nFramesWithLowDensity;                 // How many frames this particle has been flagged as low-density.  When this exceeds 'C_FramesWithLowDensity_Cutoff' it gets flagged for destruction
	public bool                     _bIsPinnedWithTripleFlexSprings;		// Important flag that means that three Flex springs have been attached between this fluid particle and the three plane verts of the collision plane we've been assigned to stick to.  Makes this fluid particle efficiently 'stick' to the body
	public Vector3                  _vecParticle;
	public float[]                  _aSpringLengths = new float[3];         // The length of the three springs this particle has to the three verts of the pinning plane.  This information is collected at every frame for dynamic Flex spring construction
    public int                      _nVert_BakedMeshKinematic = -1;         // The vertex that 'moves' this particle when this particle is kinematic.  Makes it possible for baked fluid particles to continue repelling real simulated Flex particles.
#if D_ShowDebugVisualization
    public LineRenderer[]           _aLineRenderers_Pins;
	public LineRenderer             _oLineRenderer_ToMaster;
#endif

	public CFluidParticle(CFluidParticleGroup oFluidParticleGroup, Vector3 vecPosSliceCenter, ushort nParticleOrdinalInGroup, ushort nNumParticleThisFrame, float nEmitVelocity_PreviousFrame, float nEmitVelocity_IncrementPerParticle  , ref Matrix4x4 matEmitterL2W) {
		_oFluidParticleGroup		= oFluidParticleGroup;
        _nParticleID                = ParticleBank_GetFlexParticle();
        _nParticleOrdinalInGroup	= nParticleOrdinalInGroup;

        //=== Determine the particle position in local (to emitter) and global coordinates ===
        if (_oFluidParticleGroup != null) {
            Vector3 vecPosThisParticle_Local = new Vector3 {        //###INFO: Interesting new way to initialize!
                x = vecPosSliceCenter.x + _oFluidParticleGroup._oFlexEmitter._aParticleSlicePositions2D[nParticleOrdinalInGroup].x,
                y = vecPosSliceCenter.y + _oFluidParticleGroup._oFlexEmitter._aParticleSlicePositions2D[nParticleOrdinalInGroup].y,
                z = vecPosSliceCenter.z
            };
            Vector3 vecPosThisParticle_Global = matEmitterL2W.MultiplyPoint(vecPosThisParticle_Local);       //###OPT: Must we really matrix multiply every point?  Can speed up by doing it once per emitter frame?

            //=== Activate the new particle ===
            CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particlesActivity[_nParticleID] = true;
		    CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].pos = vecPosThisParticle_Global;
		    CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].invMass = 1.0f / CGame._oFlexParamsFluid._nParticleMass;
            if (CGame._oFlexParamsFluid.D_ShowParticles_Debug)
		        CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_colours[_nParticleID] = CFluidParticleGroup.s_color_S0_Uninitialized;

		    //=== Set the particle velocity ===
		    float nEmitVelocityThisParticle = nEmitVelocity_PreviousFrame + nEmitVelocity_IncrementPerParticle * nNumParticleThisFrame;
		    Vector3 vecVelocityThisParticle = _oFluidParticleGroup._oFlexEmitter.transform.forward * nEmitVelocityThisParticle;
		    CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_velocities[_nParticleID] = vecVelocityThisParticle;
        }
	}

	public void DoDestroy() {               // Destroy the particle and returns it to the inactive pool.  'this' can be destroyed after this call.
		CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particlesActivity[_nParticleID] = false;					//###INFO: For some reason not de-activating the particles gives a HUGE performance loss.  Can't explain why difference is unreasonably large!
        CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].invMass = 0;                     //###INFO: NOTE: SSF Pro Fluid renderer shader cannot read that this particle is de-activated.  Move it out of the way so it does not render in the scene  So... setting to kinematic so they don't explode in parking space
        CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].pos = CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_restParticles[_nParticleID].pos;		// Return the particle to its 'parking space' 
        if (CGame._oFlexParamsFluid.D_ShowParticles_Debug)
            CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_colours[_nParticleID] = CFluidParticleGroup.s_color_SX_Inactive;
        ParticleBank_ReleaseFlexParticle();
#if D_ShowDebugVisualization
		CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_colours[_nParticleID] = CFluidParticleGroup.s_color_S0_Uninitialized;
		if (_aLineRenderers_Pins != null) {
			for (int nVertPinPlane = 0; nVertPinPlane < 3; nVertPinPlane++)
				GameObject.Destroy(_aLineRenderers_Pins[nVertPinPlane].gameObject);
		}
		if (_oLineRenderer_ToMaster)
			GameObject.Destroy(_oLineRenderer_ToMaster.gameObject);
#endif
    }

    public int ParticleBank_GetFlexParticle() {
        _nParticleID = CGame._oFlexParamsFluid.ParticleBank_GetNextParticle(this);
        return _nParticleID;
    }
    public void ParticleBank_ReleaseFlexParticle() {
        _nParticleID = CGame._oFlexParamsFluid.ParticleBank_ReleaseFlexParticle(this);
    }



    public bool InspectParticle() {        // Returns true if particle should be culled (too isolated for too long)     //#DEV26:
		_vecParticle = CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].pos;
#if D_ShowDebugVisualization
		if (_bIsDrivenByTripleFlexSprings && CGame._oFlexParamsFluid.D_ShowParticles_ShowPinLines) {
			if (_aLineRenderers_Pins == null) {
				_aLineRenderers_Pins = new LineRenderer[3];
				for (int nVertPinPlane = 0; nVertPinPlane < 3; nVertPinPlane++)
					_aLineRenderers_Pins[nVertPinPlane] = CGame.Line_Add("FPPin" + _nParticleID.ToString() + "-" + nVertPinPlane.ToString(), Color.yellow, 0.0005f, 0.0005f);
			}
			for (int nVertPinPlane = 0; nVertPinPlane < 3; nVertPinPlane++) {
				_aLineRenderers_Pins[nVertPinPlane].SetPosition(0, _vecParticle);
				_aLineRenderers_Pins[nVertPinPlane].SetPosition(1, _oFluidParticleGroup._aFluidParticlePins[nVertPinPlane]._vecVertPos);
				//_aLineRenderers_Pins[nVertPinPlane].SetPosition(1, CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_oFluidParticleGroup._aPinPlaneVerts[nVertPinPlane]._oFluidParticlePin._nParticleID].pos);
			}
		}
#endif
		//=== Analyze particle density to cull those that are isolated for too long ===
		if (CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_densities[_nParticleID] <= CGame._oFlexParamsFluid._nParticleCulling_ThresholdDensity) {                 // Destroy isolated particles (zero density) as they look bad in fancy shader!
			if (_nFramesWithLowDensity++ > CGame._oFlexParamsFluid._nParticleCulling_LowDensityParticles_FrameCutoff)        // Increment and flag for destruction when flagged too many times.
				return true;
		} else {
			_nFramesWithLowDensity = 0;     // If a particle is not low density at any given frame we reset the counter.  (The low-density frames must be consecutive for culling to occur)
		}
		return false;
	}

	public void AddSpringsBetweenParticleAndTriplePins() {          // Pin this particle by setting the length of its three springs in relation to the three verts of our 'pinning plane'
		_vecParticle = CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].pos;

        //=== Before we pin to the plane, bring closer stray particles that are too far from the master particle ===
        Vector3 vecPosMasterParticle = _oFluidParticleGroup._aFluidParticles[0]._vecParticle;
        Vector3 vecDistFromMasterParticle = _vecParticle - vecPosMasterParticle;
        float nDistFromMasterParticle = vecDistFromMasterParticle.magnitude;
        if (nDistFromMasterParticle > CGame._oFlexParamsFluid._nMaxDistFromMasterParticle) {
            float nRatioShrink = nDistFromMasterParticle / CGame._oFlexParamsFluid._nMaxDistFromMasterParticle;
            vecDistFromMasterParticle /= nRatioShrink;
            _vecParticle = vecPosMasterParticle + vecDistFromMasterParticle;
        }

        Vector3 vecPosPinning = Math3d.ProjectPointOnPlane(_oFluidParticleGroup._vecPlaneNormal, _oFluidParticleGroup._aFluidParticlePins[0]._vecVertPos, _vecParticle);        // Calculate the closest point on the collision plane: Where the particle should go to be right on the pinning plane. (the three spring lenghts are set to move it to this position)

		//=== Set the triple spring length to nudge the particle toward its desired point
		for (int nPin = 0; nPin < 3; nPin++) {
            Vector3 vecDelta = vecPosPinning - _oFluidParticleGroup._aFluidParticlePins[nPin]._vecPosParticle;
            _aSpringLengths[nPin] = vecDelta.magnitude * CGame.INSTANCE._nSomeHackValue2_HACK;
		}
			
		//=== Flag this particle as 'driven by springs'.  It requires no further processing to move to where we want it... Flex does the work (much more efficiently than we could by direct kinematic moves at each frame) ===
		_bIsPinnedWithTripleFlexSprings = true;			// Flag this particle as 'driven by its triple springs'.  Fluid runtime will add three springs to this particle at every frame to hold it where we want it in relation to the body
	}

    public void BakedFluid_ConvertToKinematic(int nVert_BakedMeshKinematic) {        //#DEV26:
        _nVert_BakedMeshKinematic = nVert_BakedMeshKinematic;
        CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].invMass = 0;        // We are now kinematic and need to update our position every frame
        _bIsPinnedWithTripleFlexSprings = false;			// Make sure we're not pinned!
        if (CGame._oFlexParamsFluid.D_ShowParticles_Debug)
            CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_colours[_nParticleID] = CFluidParticleGroup.s_color_Kinematic;
    }

    public void BakedFluid_UpdateKinematicPosition(ref Vector3[] aVertsBakedMesh) {
        CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].pos = aVertsBakedMesh[_nVert_BakedMeshKinematic];
    }
}

//--------------- Debug lines showing relationship between the particles (links to 'master particle')
//if (_oLineRenderer == null && CGame._oFlexParamsFluid.D_ShowParticles_Debug)
//	_oLineRenderer = CGame.Line_Add("FPM" + _nParticleID.ToString(), Color.grey, 0.0005f, 0.0005f);
//if (_oLineRenderer) {
//	_oLineRenderer.SetPosition(0, _oFluidParticleGroup._oFluidParticle_Master._vecParticle);
//	_oLineRenderer.SetPosition(1, _vecParticle);
//}

//public void ConvertToKinematic() {
//    _vecParticle = CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].pos;
//    _oFluidParticlePinned = CFluidParticlePinned_OBS.Create(this, _vecParticle, _oFluidParticleGroup.transform.rotation);
//    CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].invMass = 0;     // This particle is now kinematic.  UpdateKinematicPosition() below will be called each frame to set its position in Flex
//    //CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particlesActivity[_nParticleID] = false;
//    _bIsDrivenByTripleFlexSprings = false;			// We now kinematic and no longer requires triple springs to converge to the collision plane...
//}

//public void UpdateKinematicPosition() {
//    if (_oFluidParticlePinned) {
//        CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].pos = _oFluidParticlePinned.transform.position;
//    }
//}


//CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[oFluidParticle._nParticleID].pos = oFluidParticle._vecParticle;
//CGame._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_velocities[oFluidParticle._nParticleID] = _vecParticleVelocity_Now;     // Copy the master particle's velocity vector so this faraway particle gets teleported closer and has goes in the same direction as the master particle
//CGame._oFlexParamsFluid.STAT_ParticlesBroughtCloserBeforeSpringDef++;
