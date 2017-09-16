/*###DOCS24: Aug 2017 - Flex Emitter
=== DEV ===

=== NEXT ===

=== TODO ===

=== LATER ===

=== OPTIMIZATIONS ===

=== REMINDERS ===

=== IMPROVE ===

=== NEEDS ===

=== DESIGN ===

=== QUESTIONS ===

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===
- Fluid does not emit where emitter when scene has more than one penis!
?- Emitter particle 'explode' when velocity is very low... Need to avoid emitting even one slice when velocity very low.

=== WISHLIST ===

*/

using UnityEngine;

public class CFlexEmitter : uFlex.FlexProcessorFluid		// CFlexEmitter: Responsible to emit flex particles in a coherent stream from frame to frame.
{
	[HideInInspector]	public CFlexParamsFluid	_oFlexParamsFluid;
						public	bool		_bEmitterOn = false;
						public	float		_nEmitterVelocityMult = 1;							// The scaling we applied to the _nEmitterVelocityBase of our fluid parameters
	[HideInInspector]	public	Vector2[]	_aParticleSlicePositions2D;
						CFluidParticleGroup	_oFluidParGrp_LastOfPrevFrame;                      // The last of the last-emitted fluid particle group in the previous frame.  Needed so we can extract its one-frame-old real-world position and create follow-on particles that are exactly next to it (for seamless fluid effect)
						Vector3				_vecPosEmitterStart;								// The positing in our local coordinate space of where we place the first particle in this frame.  While this starts at whatever point we need to start to fill first-frame with particles this position becomes the position of the last-emitted particle of the previous frame (so we append particles this frame that are right next to it)
						Vector3				_vecPosEmitterEnd;									// The target position of where we stop emitting particles.  By definition (0,0,0) but we can move if need be
						float				_nEmitVelocity_PreviousFrame;						// The emitter velocity at the previous frame.  Used to smoothly increase or decrease from that level to our current one (and not cause visible breaks in fluid stream when velocity changes)
						public int			_nStatParticleMatchTooFar_TEMP;
						public int			_nStatParticleMatchSucceeded_TEMP;
						bool				_bConfigured;
						int					_nFramesEmitted;

	private void Start() {
		_oFlexParamsFluid = CGame.INSTANCE._oFlexParamsFluid;
		if (_oFlexParamsFluid != null) { 
			_oFlexParamsFluid.Emitter_Register(this);
			OnEmitterParametersChanged();			//###CHECK23: Safe to init by ourselves??  Re-think creation and init flow here.
		}
	}

	private void OnDestroy() {
		if (_oFlexParamsFluid)
			_oFlexParamsFluid.Emitter_Unregister(this);
	}

	public void OnEmitterParametersChanged() {
		_oFluidParGrp_LastOfPrevFrame = null;				// Invalidate last particle group of last frame to guarantee we don't attempt to 'glue' the particles for this frame to the previous frame (for continuity)
		//=== Ensure our 2D slice position cache is up to date ===		
		_aParticleSlicePositions2D = new Vector2[_oFlexParamsFluid._nParticlesPerSlice];
		_aParticleSlicePositions2D[0] = new Vector2(0, 0);              // First particle is always at center.  Others are around the first particle
		float nAnglePerPeripheryParticle = 2 * Mathf.PI / (_oFlexParamsFluid._nParticlesPerSlice - 1);		// Number of radians per periphery particle
		for (int i = 1; i < _oFlexParamsFluid._nParticlesPerSlice; i++) {                 // Set the positions of the non-first particles around the first particle
			float nAngleThisParticle = (i-1) * nAnglePerPeripheryParticle;
			_aParticleSlicePositions2D[i] = new Vector2(_oFlexParamsFluid._nEmitterDistanceBetweenParticles * Mathf.Cos(nAngleThisParticle), _oFlexParamsFluid._nEmitterDistanceBetweenParticles * Mathf.Sin(nAngleThisParticle));
		}
		_vecPosEmitterEnd = new Vector3(0, 0, 0);           // The end of our 'emitting axis' is by definition the local 0,0,0 of our transform (placed right inside uretra!)
		_bConfigured = true;
	}
	
    public override void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
		if (_bConfigured == false || _bEmitterOn == false)
			return;
		if (_oFlexParamsFluid.D_DisableEmitters)
			return;

		//=== Set the emit velocity & rate from the pertinent configuration curve ===
		float nTimeInEjaculationCycle = (Time.time - CGame.INSTANCE._nTimeStartOfCumming) % _oFlexParamsFluid._nEmitter_CumCycleTimeSpan;
		float nTimeInEjaculationCycle_Normalized = nTimeInEjaculationCycle / _oFlexParamsFluid._nEmitter_CumCycleTimeSpan;        //###DESIGN!!! Move curve here?  How to persist??
		if (_oFlexParamsFluid.D_FluidEmitter_StopAfterOneCycle && nTimeInEjaculationCycle_Normalized >= 0.95f)
			DisableEmitter();
		float nEmitVelocity = _nEmitterVelocityMult * _oFlexParamsFluid._nEmitterVelocityBase;  // Calculate our effective velocity from our base velocity and our multiplyer
		float nEmitVelocityNow = nEmitVelocity * Mathf.Max(_oFlexParamsFluid.CurveEjaculation.Evaluate(nTimeInEjaculationCycle_Normalized), 0);       //###NOTE: We assume both the ejaculation curve's X (time) value and Y values (strenght) go from 0 to 1

		//=== ADJUST EMITTER TO PREVIOUS FRAME POSITION: Calculate the velocity vector applied to the particles emitted in this frame ===
		Vector3 vecVelocityLastParticle = transform.forward * nEmitVelocityNow;
		Vector3 vecDistanceTravelledThisFrame = vecVelocityLastParticle * Time.fixedDeltaTime;                  //###NOTE: Assumes Flex is called from our top-level FixedUpdate()

		//=== Adjust the 'starting point' of our first particle in relation to last particle of previous frame ===
		if (_oFluidParGrp_LastOfPrevFrame != null) {
			Vector3 vecPosLastParticleOfPreviousFrameG = _oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_oFluidParGrp_LastOfPrevFrame._oFluidParticle_LastCenterParticle._nParticleID].pos;			// See where our last-emitted 'center' particle is now (so we can emit right next to it)...
			Vector3 vecPosLastParticleOfPreviousFrameL = transform.worldToLocalMatrix.MultiplyPoint(vecPosLastParticleOfPreviousFrameG);									//... and convert its global position back to our local space...
			if (vecPosLastParticleOfPreviousFrameL.magnitude < _oFlexParamsFluid._nEmitter_DistForPreviousFrameParticles_HACK * _oFlexParamsFluid._nEmitterDistanceBetweenParticles) {		//... and check if it's not too far...			//###TUNE
				_vecPosEmitterStart = vecPosLastParticleOfPreviousFrameL;																									//... so start at the local position of the last emitted particle...
				_vecPosEmitterStart.z -= _oFlexParamsFluid._nEmitterDistanceBetweenParticles;																				//... and add our spacing so we begin emitting right next to it.	
				_nStatParticleMatchSucceeded_TEMP++;
			} else {
				_vecPosEmitterStart	= vecDistanceTravelledThisFrame;	// Previous-frame particle is too far.  We start emitting where we should start given the distance we need to fill particles in (derived from velocity + time per frame)
				_nStatParticleMatchTooFar_TEMP++;
			}
		} else {
			_vecPosEmitterStart	= vecDistanceTravelledThisFrame;		// No particle in previous frame.  We start emitting where we should start given the distance we need to fill particles in (derived from velocity + time per frame)
		}

		//=== Determine how many 'slices' we need to emit and the vector between slices so we reach our end (close as possible to our x = 0, y = 0 emitter line)
		Vector3 vecEmitterSpanThisFrame = (_vecPosEmitterEnd - _vecPosEmitterStart);
		float nSlices_Float = vecEmitterSpanThisFrame.magnitude / _oFlexParamsFluid._nEmitterDistanceBetweenParticles;
		ushort nSlices = (ushort)nSlices_Float;       //CGame.INSTANCE._nFluidEmitter_NumSlices;
		if (nSlices > 0) { 
			Vector3 vecEmitterSpanThisFrame_UnitVector = vecEmitterSpanThisFrame.normalized;
			Vector3 vecDistanceBetweenSlices = vecEmitterSpanThisFrame_UnitVector * _oFlexParamsFluid._nEmitterDistanceBetweenParticles;

			uint nNumParticlesInGroup = (uint)(nSlices * _oFlexParamsFluid._nParticlesPerSlice);
			float nEmitVelocity_IncrementPerParticle = (nEmitVelocityNow - _nEmitVelocity_PreviousFrame) / nNumParticlesInGroup;

			CFluidParticleGroup oFluidParGrp = _oFlexParamsFluid.FluidParticleGroup_Add(this);       // The very first particle emitted in a frame is configured as a 'raycasting particle' (to dynamically create Fluid collider as it goes).  We also remember its ID so we can glue to it next frame
			oFluidParGrp.EmitParticles(this, nSlices, _vecPosEmitterStart, vecDistanceBetweenSlices, nEmitVelocity_IncrementPerParticle, _nEmitVelocity_PreviousFrame);       // The very first particle emitted in a frame is configured as a 'raycasting particle' (to dynamically create Fluid collider as it goes).  We also remember its ID so we can glue to it next frame
			_oFluidParGrp_LastOfPrevFrame = oFluidParGrp;				// Remember this particle group so we can seamlessly join the next frame's particles with this one.

			_nEmitVelocity_PreviousFrame = nEmitVelocityNow;            // Remember our velocity for the next frame to smooth from

			_nFramesEmitted++;

			if (_oFlexParamsFluid.D_FluidEmitter_NumFramesEmitting != 0 && _nFramesEmitted >= _oFlexParamsFluid.D_FluidEmitter_NumFramesEmitting)
				DisableEmitter();		//###TEMP: Debug disable of emitter after # of frames.  (Disabled with 0)
		}
	}

	public void DisableEmitter() {
		_oFlexParamsFluid.D_DisableEmitters = true;
		_oFluidParGrp_LastOfPrevFrame = null;
		_nFramesEmitted = 0;
		_nEmitVelocity_PreviousFrame = 0;
	}
}
