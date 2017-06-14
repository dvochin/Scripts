using UnityEngine;

public class CFlexEmitter : uFlex.FlexProcessor		// CFlexEmitter: Responsible to emit flex particles in a coherent stream from frame to frame.
{
	[HideInInspector]	public CFlexFluid	_oFlexFluid;
						public	bool		_bEmitterOn = false;
						public	float		_nCumCycleTime = 4f;
						public float		_nEmitterVelocityMult = 1;							// The scaling we applied to the _nEmitterVelocityBase of our fluid parameters
						Vector2[]			_aParticleSlicePositions2D;
						int					_nParticleIdOfLastEmittedInPreviousFrame = -1;		// Particle ID of the last-emitted center particle in the previous frame.  Needed so we can extract its one-frame-old real-world position and create follow-on particles that are exactly next to it (for seamless fluid effect)
						Vector3				_vecPosEmitterStart;								// The positing in our local coordinate space of where we place the first particle in this frame.  While this starts at whatever point we need to start to fill first-frame with particles this position becomes the position of the last-emitted particle of the previous frame (so we append particles this frame that are right next to it)
						Vector3				_vecPosEmitterEnd;									// The target position of where we stop emitting particles.  By definition (0,0,0) but we can move if need be
						float				_nEmitVelocity_PreviousFrame;						// The emitter velocity at the previous frame.  Used to smoothly increase or decrease from that level to our current one (and not cause visible breaks in fluid stream when velocity changes)
						public int			_nStatParticleMatchTooFar_TEMP;
						public int			_nStatParticleMatchSucceeded_TEMP;
						bool				_bConfigured;
						public float		_nDistanceToLookForPreviousFrameParticles_HACK = 15;		// Distance to stop trying to glue this frame's stream to the previous frame


	private void Start() {
		_oFlexFluid = CGame.INSTANCE._oFlexFluid;
		if (_oFlexFluid != null) { 
			_oFlexFluid.Emitter_Register(this);
			OnEmitterParametersChanged();			//###CHECK23: Safe to init by ourselves??  Re-think creation and init flow here.
		}
	}

	private void OnDestroy() {
		_oFlexFluid.Emitter_Unregister(this);
	}

	public void OnEmitterParametersChanged() {
		_nParticleIdOfLastEmittedInPreviousFrame = -1;			// Invalidate snap-to-previous-frame
		//=== Ensure our 2D slice position cache is up to date ===		
		_aParticleSlicePositions2D = new Vector2[_oFlexFluid._nParticlesPerSlice];
		_aParticleSlicePositions2D[0] = new Vector2(0, 0);              // First particle is always at center.  Others are around the first particle
		float nAnglePerPeripheryParticle = 2 * Mathf.PI / (_oFlexFluid._nParticlesPerSlice - 1);		// Number of radians per periphery particle
		for (int i = 1; i < _oFlexFluid._nParticlesPerSlice; i++) {                 // Set the positions of the non-first particles around the first particle
			float nAngleThisParticle = (i-1) * nAnglePerPeripheryParticle;
			_aParticleSlicePositions2D[i] = new Vector2(_oFlexFluid._nEmitterDistanceBetweenParticles * Mathf.Cos(nAngleThisParticle), _oFlexFluid._nEmitterDistanceBetweenParticles * Mathf.Sin(nAngleThisParticle));
		}
		_vecPosEmitterEnd = new Vector3(0, 0, 0);           // The end of our 'emitting axis' is by definition the local 0,0,0 of our transform (placed right inside uretra!)
		_bConfigured = true;
	}

	
    public override void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters)
    {
		if (_bConfigured == false || _bEmitterOn == false)
			return;

		//=== Set the emit velocity & rate from the pertinent configuration curve ===
		float nTimeInEjaculationCycle = (Time.time - CGame.INSTANCE._nTimeStartOfCumming) % _nCumCycleTime;
		float nTimeInEjaculationCycle_Normalized = nTimeInEjaculationCycle / _nCumCycleTime;        //###DESIGN!!! Move curve here?  How to persist??
		float nEmitVelocity = _nEmitterVelocityMult * _oFlexFluid._nEmitterVelocityBase;	// Calculate our effective velocity from our base velocity and our multiplyer
		float nEmitVelocityNow = nEmitVelocity * Mathf.Max(CGame.INSTANCE.CurveEjaculateMan.Evaluate(nTimeInEjaculationCycle_Normalized), 0);       //###NOTE: We assume both the ejaculation curve's X (time) value and Y values (strenght) go from 0 to 1

		//====== ADJUST EMITTER TO PREVIOUS FRAME POSITION ====
		//=== Calculate the velocity vector applied to the particles emitted in this frame ===
		Vector3 vecVelocityLastParticle = transform.forward * nEmitVelocityNow;
		Vector3 vecDistanceTravelledThisFrame = vecVelocityLastParticle * Time.fixedDeltaTime;					//###NOTE: Assumes Flex is called from our top-level FixedUpdate()

		//=== Adjust the 'starting point' of our first particle in relation to last particle of previous frame ===
		if (_nParticleIdOfLastEmittedInPreviousFrame != -1) {
			Vector3 vecPosLastParticleOfPreviousFrame_Global = _oFlexFluid._oFlexFluidParticleBank.m_particles[_nParticleIdOfLastEmittedInPreviousFrame].pos;        // See where our last-emitted 'center' particle is now (so we can emit right next to it)...
			Vector3 vecPosLastParticleOfPreviousFrame_Local = transform.worldToLocalMatrix.MultiplyPoint(vecPosLastParticleOfPreviousFrame_Global);                         //... and convert its global position back to our local space...
			if (vecPosLastParticleOfPreviousFrame_Local.magnitude < _nDistanceToLookForPreviousFrameParticles_HACK * _oFlexFluid._nEmitterDistanceBetweenParticles) {                                         //... and check if it's not too far...			//###TUNE
				_vecPosEmitterStart = vecPosLastParticleOfPreviousFrame_Local;																								//... so start at the local position of the last emitted particle...
				_vecPosEmitterStart.z -= _oFlexFluid._nEmitterDistanceBetweenParticles;                                                                           //... and add our spacing so we begin emitting right next to it.	
				_nStatParticleMatchSucceeded_TEMP++;
			} else {
				_vecPosEmitterStart	= vecDistanceTravelledThisFrame;	// Previous-frame particle is too far.  We start emitting where we should start given the distance we need to fill particles in (derived from velocity + time per frame)
				_nStatParticleMatchTooFar_TEMP++;
			}
		} else {
			_vecPosEmitterStart	= vecDistanceTravelledThisFrame;		// No particle in previous frame.  We start emitting where we should start given the distance we need to fill particles in (derived from velocity + time per frame)
		}

		//=== Determine how many slices we need to emit and the vector between slices so we reach our end (close as possible to our x = 0, y = 0 emitter line)
		Vector3 vecEmitterSpanThisFrame = (_vecPosEmitterEnd - _vecPosEmitterStart);
		float nSlices_Float = vecEmitterSpanThisFrame.magnitude / _oFlexFluid._nEmitterDistanceBetweenParticles;
		int nSlices = (int)nSlices_Float;
		Vector3 vecEmitterSpanThisFrame_UnitVector = vecEmitterSpanThisFrame.normalized;
		Vector3 vecDistanceBetweenSlices = vecEmitterSpanThisFrame_UnitVector * _oFlexFluid._nEmitterDistanceBetweenParticles;

		//====== EMIT PARTICLES FOR THIS FRAME =====
		float nInvMass = 1.0f / _oFlexFluid._nParticleMass;
		Matrix4x4 matL2W = transform.localToWorldMatrix;
		Vector3 vecPosThisParticle_Local = new Vector3();
		int nNumParticles = nSlices * _oFlexFluid._nParticlesPerSlice;
		int nNumParticleThisFrame = 0;
		float nEmitVelocity_IncrementPerParticle = (nEmitVelocityNow - _nEmitVelocity_PreviousFrame) / nNumParticles;

		for (int nSlice = 0; nSlice < nSlices; nSlice++) {											// Iterate through the slices that will will be consumed out of our emitting area given our time scale and outgoing velocity.
			Vector3 vecPosCenterOfThisSlice = _vecPosEmitterStart + nSlice * vecDistanceBetweenSlices;

			for (int i = 0; i < _oFlexFluid._nParticlesPerSlice; i++) {                 // Iterate through all the particles to insert them into this outgoing 'emitter slice'
				int nParticleBeingEmitted = _oFlexFluid.GetOldestParticle();
				if (i == 0) {				// The first particle in a slice is always the center one.  Special processing is needed...
					if (nSlice == 0 && CGame.INSTANCE._nFrameCount_MainUpdate % 3 == 0)		//###TODO23: Tune how frequently to create expensive raycasters!	###IMPROVE: First emitted in a while should always emit!
						_oFlexFluid.FluidParticleRaycaster_Add(nParticleBeingEmitted);		// The very first particle emitted in a frame is configured as a 'raycasting particle' (to dynamically create Fluid collider as it goes).  We also remember its ID so we can glue to it next frame
					if (nSlice == (nSlices-1)) 												// The ID of the very last center particle is remembered so the next frame has the chance to place its particles right next to it.
						_nParticleIdOfLastEmittedInPreviousFrame = nParticleBeingEmitted;	
				}
				vecPosThisParticle_Local.x = vecPosCenterOfThisSlice.x + _aParticleSlicePositions2D[i].x;
				vecPosThisParticle_Local.y = vecPosCenterOfThisSlice.y + _aParticleSlicePositions2D[i].y;
				vecPosThisParticle_Local.z = vecPosCenterOfThisSlice.z;
				Vector3 vecPosThisParticle_Global = matL2W.MultiplyPoint(vecPosThisParticle_Local);       //###OPT: Must we really matrix multiply every point?  Can speed up?
				//Debug.LogFormat("-- Emit#{0}/{1}   G={2:F4},{3:F4},{4:F4}", nSlice, i, vecPosThisParticleGlobal.x, vecPosThisParticleGlobal.y, vecPosThisParticleGlobal.z);
				_oFlexFluid._oFlexFluidParticleBank.m_particlesActivity[nParticleBeingEmitted] = true;
				_oFlexFluid._oFlexFluidParticleBank.m_particles[nParticleBeingEmitted].pos = vecPosThisParticle_Global;
				_oFlexFluid._oFlexFluidParticleBank.m_particles[nParticleBeingEmitted].invMass = nInvMass;
				float nEmitVelocityThisParticle = _nEmitVelocity_PreviousFrame + nEmitVelocity_IncrementPerParticle * nNumParticleThisFrame;
				Vector3 vecVelocityThisParticle = transform.forward * nEmitVelocityThisParticle;
				_oFlexFluid._oFlexFluidParticleBank.m_velocities[nParticleBeingEmitted] = vecVelocityThisParticle;
				nParticleBeingEmitted++;
				nNumParticleThisFrame++;
			}
        }
		_nEmitVelocity_PreviousFrame = nEmitVelocityNow;           // Remember our velocity for the next frame to smooth from
    }
}

//Color				_oColorParticles = Color.white;
//_oFlexFluid._oFlexFluidParticles.m_colours[nParticleBeingEmitted] = _oColorParticles;
//KeyCode				_oKeyControl = KeyCode.F;
//if (Input.GetKeyDown(_oKeyControl))
//_oColorParticles = new Color(Random.value, Random.value, Random.value, 1);

