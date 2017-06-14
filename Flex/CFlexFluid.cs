/*###DISCUSSION: Flex Fluids - Jun 2017 - ###DOCS23:
=== DEV ===

=== NEXT ===
- CFlexFluid should be central to any fluid.  Check it has ALL references and be our go-to for anything fluid
- Connect the old body flex collider better with the fluid-scene equivalent

=== TODO ===

=== LATER ===
- Calibrate adhesion, viscocity, cohesion, etc
+ Increasing num iterations reduces bubliness, decreases staircase effect and increases viscocity
	- Meaning: keep viscosity as high as it looks good to take advantage of what we have!
- We will need to eventually pick a damn setting for gravity (for softbody and fluid) and make everyhting look good!
	- Why is a decent looking gravity only -0.1 instead of -9.82??

=== IMPROVE ===
- Cum is much too bright in low-light settings... sense?  Increase transparency??
- Improve Cum color choices of SSF pro... including specular!

=== DESIGN ===

=== IDEAS ===
- Can reduce the # of particles from the bank just by setting max at init time... useful at gameplay??
- Use vertex-lit flex collider mesh to visually indicate what is going on...
- Use particle colors to mark some of them (such as raycaster particle, end-of-life, what emitter, etc)

=== LEARNED ===
- Player much more efficient.  Getting frame time of 24ms in Editor = ~16ms in player!
	- Q: Can remove mirror window (e.g. just in headset for more perf?)

=== PERFORMANCE ===
- Flex uses super-dumb 'FindOjectsOfType<>' taking 1.7ms!
- SSF Blur quite expensive at 6ms!!.  If we must turn it on make the most of it (e.g. adjust blur by cam distance?)
- Reduce some of the important fluid param like the ratio to next particles 0.6666!
- Need to measure performance of ssfpro shader
- Where is this much performance lost??  Find out!!!!!
	- about 7ms in 'GetParticles' -> reduce particle count
	- about 6.1ms in SetShapes.  Keep colliding tris low!
		- Q: Can we group a few tris so we get less shapes??
- Need to start thinking about optimization... how much cum volume do we give user / how many bursts? -> How many particles
	- Can emit less particles in a slice?  (5-7 intead of 8?)
		- Take into account ssf cutout!

=== PROBLEMS ===
- Changing flex time step breaks emitter stream!
- A fair amount of cum penetration still... make parameters more aggresive?
- A lot of 'splashing bubbles' now... reduce this while not increasing fluid spacing
- Fluid transparency has a visual appearance cost: transparent cum further into Z buffer draws parts that should be occluded!  (Need to give a different renderer for each cum burst?)
- We're exposing public component properties like mass but we're setting these in code!  (Misleading)
- uFlex' fluid generator crashes when fluid density smaller than 0.05...  Because of its visualizer?
- We get a 'staircase effect' when velocity expanding later in fluid stream... velocity blending working well??  Increasing iteration runs mostly fixes this but is more expensive
- Damping reduces the effect of gravity... weaken it?

=== QUESTIONS ===

=== WISHLIST ===
- Enable emitter to have two rings of periphery particles?  (20+ particles per slice too many no?)
	- Even better would be to have circular appearance for 3,4,5,6 particles per slice etc.
- Would be nice to have 2-sided transparent shader for our debug visualizers!
- Particle culling: cull those on the ground?  Cull those with zero velocity?  Cull super-low density like 1?

*/

using UnityEngine;
using System.Collections.Generic;


public class CFlexFluid : uFlex.FlexParameters, uFlex.IFlexProcessor {

						public	bool		_bUpdateFluidParameters;					// When true this class will recalculate and propagate all the fluid parameters to all pertinent components.
						public	float		_nEmitterDiameter = 0.005f;					// Diameter of fluid created by emitter.  Larger than '_nEmitterDiameter' to account for '_nThicknessWastedByShader' shader waste
						public	float		_nFluidRestDistDivInteractionRadius = 0.7f;	// The important ratio between the 'fluid rest distance' and the critically-important 'particle interaction radius'  NVidia recommends about 0.5 for good quality fluids.  Cheaper fluids can go closer to one but appear blocky  //###OPT: Optimize the crap out of this important parameter!
						public	int			_nEmitterNumParticlesAccross = 3;			// Number of particles accross the fluid emitter.  Must be 3 for now as emitter is limited to one central particle and 7 periphery ones to form a spherical-looking slice  ###IMPROVE: Enable emitter to have two rings of periphery particles?
						public	float		_nEmitterVelocityBase = 0.5f;				// Velocity base of particles of particles at emitters. Each emitter can 'scale' this value with its _nEmitterVelocityMult multiplyer.  Maximum possible fluid velocity of fluid is determined from this number (to mitigate damage from 'explosions')
						public	float		_nEmitterParticleCramming = 1.0f;			// How 'crammed' emitter particles are over the natural fluid rest distance '_nFluidParticleDiameter'.  (Fluid cohesion brings fluid particles somewhat closer than their rest distance)  Used to create at the emitter the tightest possible stream of particles that won't explode.  
						public	float		_nParticleMass = 1.0f;                      // Super-important particle mass.  influeces everything!!
						public	float		_nShaderParticelSizeMult = 2.5f;			// The multiplier applied to the shader's particle size.  Very important to 'smooth out' and blend particles.  higer settings make each particle more 'foggy'

						public	float		_nFluidParticleDiameter;					// The fluid particle diameter = distance between any two fluid particles at rest.
						public	float		_nFluidParticleRadius;						// The fluid particle radius = half the diameter.

						public	float		_nParticleInteractionRadius;				// The super-important interaction radius between particles.  Particles further than that are not 'seen' by the solver (thereby not participating in fluid forces like viscosity or fluid cohesion)
						public	float		_nParticleSolidRestDistance;				// The distance non-fluid particles attempt to maintain between each other.
						public	float		_nParticleCollisionMargins;					// Extra margins given to both extra radius when searching for shapes and extra radius when colliding against kinematic shapes

						public	float		_nEmitterDistanceBetweenParticles;			// The calculated distance particles are spread apart by emitter.  Basically the _nFluidParticleDiameter * cramming ratio parameter _nEmitterParticleCramming
	[HideInInspector]	public int			_nParticlesPerSlice = 8;					// Number of particles per emitter slice.  Pretty much has to be 8 now... one central particle and 7 periphery ones to form a spherish-looking slice

	[HideInInspector]	public	uFlex.FlexParticles _oFlexFluidParticleBank;			// The bank of particles reserved for Fluid dispensation.  (Generated by uFlex design-time tool and neatly 'parked' in a cube so we can see how many are left)


	[HideInInspector]	public	float		_nEmitterVelocitySqr_Slow, _nEmitterVelocitySqr_Medium, _nEmitterVelocitySqr_Fast;		// Square of velocity that is understood to be slow, medium or fast.  Used to trim down particle raycasting on slow particles.  A ratio of top emitter speed  (These are kept as squared values to prevent sqrt() at runtime)

						public	float		_nParticleCulling_Threshold = 0.1f;			// Cutoff of particle density where a particul will be de-activated.
						public	int			_nParticleCulling_NumFrameToCullAll = 15;	// Number of frames to inspect each particle for culling (spreads culling to avoid major performance hit)
								int			_nParticleCulling_NumToCullPerFrame;		// How many particles to inspect for culling each frame = Num particles / _nParticleCulling_NumFrameToCullAll
								int			_nParticleCulling_CurrentGroup;				// The current group (from 0 to _nParticleCulling_NumFrameToCullAll-1) that is being culled

						public	int			_nStat_ParticlesCulled;						// Number of culled particles
						public	int			_nStat_NumFluidRaycasts;					// Number of raycasts performed by CFluidParticleRaycaster particles.  Expensive
						public	int			_nStat_NumFluidRaycastsAvoided;				// Number of raycasts avoided by CFluidParticleRaycaster particles = Yay!
						public	int			_nStat_NumActiveColliderSpheres;
						public	int			_nStat_NumActiveColliderTris;

	[HideInInspector]	public HashSet<CFluidParticleRaycaster> _setFluidParticleRaycasters	= new HashSet<CFluidParticleRaycaster>();	// Set of particle observers: responsible for observing fluid particles and performing ray casting and turning on fluid colliders	//###LEARN: C# Sets
								int			_nParticleOldest;							// The 'oldest particle' = the next one to be re-cycled the next time an emitter requests a particle ID through GetOldestParticle()
	[HideInInspector]	public	int			_nParticlesEmitted;							// Number of particles emitted.  Used to destroy expensive raycasting particles when they get too old. (get recycled)
	[HideInInspector]	public	HashSet<CFlexEmitter> _setFlexEmitters = new HashSet<CFlexEmitter>();	// List of emitters pulling particles from this pool.

	public void OnStart () {
		_oFlexFluidParticleBank = GameObject.Find("FlexFluidParticleBank").GetComponent<uFlex.FlexParticles>();
		_bUpdateFluidParameters = true;
		uFlex.FlexProcessor oFlexProc = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexProcessor)) as uFlex.FlexProcessor;
		oFlexProc._oFlexProcessor = this;
		DoFixedUpdate();
	}

	public void DoFixedUpdate() {
		if (_bUpdateFluidParameters) {
			Debug.LogFormat("=== FlexFluid updating fluid parameters ===");

			_nFluidParticleDiameter = _nEmitterDiameter / _nEmitterNumParticlesAccross;       // Determine the fluid particle-to-particle rest distance.
			_nFluidParticleRadius	= _nFluidParticleDiameter / 2;

			_nParticleInteractionRadius	= _nFluidParticleDiameter / _nFluidRestDistDivInteractionRadius;
			_nParticleSolidRestDistance = _nParticleInteractionRadius * 0.90f;						// Rest distance from solids a bit less than interaction radius for more robust collisions.
			_nParticleCollisionMargins	= _nParticleInteractionRadius * 0.15f;                       // Provide extra margins for robust collision against kinematic shapes and fast-moving particles

			m_cohesion					= 1.25f;								//###TUNE:!!! Critical and heavily influcned by mass... Turn this up until fluid explodes!
			m_viscosity					= 15f;									//###TUNE:!!! As high as possible before fluid explodes
			m_dissipation				= 0.03f;								//###LEARN: Super important: Slows down particles by # of contacts.  Really helps forming puddles but sensitive!
			m_adhesion					= 0.1f;									//###TUNE!!! Tune until fluid rolls a bit on body then stops.  As low as possible
			m_damping					= 0.0f;									//###LEARN: Greatly slows down particles over time facilitating a 'puddle'
			m_surfaceTension			= 1.0f;
			_nEmitterVelocityBase		= 0.4f;


			m_numIterations				= 3;									//###OPT:!!!!: Can go down to two? ###IMPROVE: Study the influence of this on real performance as higher iterations (e.g. 10) look nicer!
			m_radius					= _nParticleInteractionRadius;
			m_solidRestDistance			= _nParticleSolidRestDistance;
			m_fluidRestDistance			= _nFluidParticleDiameter;
			m_CollisionDistance			= m_solidRestDistance;
			m_ParticleCollisionMargin	= _nParticleCollisionMargins;
			m_ShapeCollisionMargin		= _nParticleCollisionMargins;
			m_MaxSpeed					= _nEmitterVelocityBase * 2.0f;			// Provide a bit more max speed than emitter speed in case fluid falls down (for gravity acceleration)
			m_restitution				= 0;                                    // Very bad!!  Bounces fluid off body!  Keep at 0!!
			m_Fluid						= true;
			m_dynamicFriction			= 1;
			m_staticFriction			= 1;									// Very beneficial: Slows down fluid once it reaches body!
			m_particleFriction			= 1;                                    //###TUNE?
			m_buoyancy					= 1;									// Set this to 1!  0 = turns off gravity!!  -1 = inverse gravity!  WTF???
			m_inertiaBias				= 0.001f;                               //###TUNE?
			m_gravity					= new Vector3(0, -0.1f, 0);				//###TUNE: Gravity!!!!  Why does such a low setting look good??
			//m_gravity					= new Vector3(0, -9.82f, 0);

			_nEmitterDistanceBetweenParticles = _nFluidParticleDiameter * _nEmitterParticleCramming;
			_nParticleCulling_NumToCullPerFrame = _oFlexFluidParticleBank.m_particlesCount / _nParticleCulling_NumFrameToCullAll;

			//=== Expand the particle velocities we consider fast, medium and slow.  Used to trim down (expensive) raycasting to form just-in-time Fluid colliders ===
			_nEmitterVelocitySqr_Fast	= Mathf.Pow(0.60f * _nEmitterVelocityBase, 2);			// Must be over this (square of) speed to be considered a 'fast fluid particle' = raycasts often!
			_nEmitterVelocitySqr_Medium	= Mathf.Pow(0.20f * _nEmitterVelocityBase, 2);			// Must be over this (square of) speed to be considered a 'medium speed fluid particle' = raycasts occasionally!
			_nEmitterVelocitySqr_Slow	= Mathf.Pow(0.05f * _nEmitterVelocityBase, 2);			// Must be over this (square of) speed to be considered a 'slow fluid particle' = raycast infrequently.  (Slower = rarely)

			//=== Notify our connected emitters to update their data structures ===
			foreach (CFlexEmitter oFlexEmitter in _setFlexEmitters)
				oFlexEmitter.OnEmitterParametersChanged();

			//=== Set the particle radius in the FlexFluidRenderer ===
			uFlex.FlexFluidRenderer oFlexFluidRenderer = _oFlexFluidParticleBank.GetComponent<uFlex.FlexFluidRenderer>();
			if (oFlexFluidRenderer != null) {
				oFlexFluidRenderer.m_pointScale  = _nFluidParticleDiameter * _nShaderParticelSizeMult;
				oFlexFluidRenderer.m_pointRadius = oFlexFluidRenderer.m_pointScale / 2;
				oFlexFluidRenderer.m_flexParticles = _oFlexFluidParticleBank;		// Set a component on the fluid bank to a component on the same gameObject = dumb.
			}

			//=== Set the particle radius in the debug FlexParticlesRenderer ===
			uFlex.FlexParticlesRenderer oPartRend = _oFlexFluidParticleBank.GetComponent<uFlex.FlexParticlesRenderer>();
			if (oPartRend != null) {
				oPartRend.m_size = _nFluidParticleDiameter;
				oPartRend.m_radius = _nFluidParticleRadius;
			}

			_bUpdateFluidParameters = false;
		}

		CGame.INSTANCE._aGuiMessages[(int)EGameGuiMsg.Fluid1] = string.Format("Fluid:  A={0}/{1}  Rays={2}/{3} [#{4}]   Cull={5}", _nStat_NumActiveColliderTris, _nStat_NumActiveColliderSpheres, _nStat_NumFluidRaycasts, _nStat_NumFluidRaycastsAvoided, _setFluidParticleRaycasters.Count, _nStat_ParticlesCulled);
	}

	public void Emitter_Register(CFlexEmitter oFlexEmitter) {		// Register an additional emitter to our set
		_setFlexEmitters.Add(oFlexEmitter);
	}

	public void Emitter_Unregister(CFlexEmitter oFlexEmitter) {		// Unregister an additional emitter to our set
		_setFlexEmitters.Remove(oFlexEmitter);
	}

	public int GetOldestParticle() {			// Called by emitter to get the 'oldest particle' (so it can re-cycle it into its current stream)
		int nParticleCurrent = _nParticleOldest++;
		_nParticlesEmitted++;
		if (_nParticleOldest >= _oFlexFluidParticleBank.m_particlesCount)
			_nParticleOldest = 0;
		return nParticleCurrent;
	}

	public void FluidParticleRaycaster_Add(int nParticleID) {
		_setFluidParticleRaycasters.Add(new CFluidParticleRaycaster(nParticleID, _nParticlesEmitted + _oFlexFluidParticleBank.m_particlesCount));		// Add a new raycast particle and tell it to destroy itself when our global counter reaches one particle pool into the future...
	}

	public void FluidParticleRaycaster_PerformRaycast() {
		List<CFluidParticleRaycaster> aFluidParticleRaycaster_ToDestroy = new List<CFluidParticleRaycaster>();
		foreach (CFluidParticleRaycaster oFluidParticleRaycaster in _setFluidParticleRaycasters) { 
			bool bShouldBeDestroyed = oFluidParticleRaycaster.FluidParticleRaycaster_PerformRaycast(_oFlexFluidParticleBank);
			if (bShouldBeDestroyed)
				aFluidParticleRaycaster_ToDestroy.Add(oFluidParticleRaycaster);
		}
		foreach (CFluidParticleRaycaster oFluidParticleRaycaster in aFluidParticleRaycaster_ToDestroy) {
			_setFluidParticleRaycasters.Remove(oFluidParticleRaycaster);		//###CHECK: Will get picked up by garbage collector?
		}
	}

	//=========================================================================	PER-FRAME FLEX UPDATE
	public void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {		// Iterate through some of our particle to remove those that are isolated
		//=== Cull particles that are isolated (zero density) (they look horrible with SSFPro shader)  MUST be called from a Flex.PreContainerUpdate() function ===
		for (int nTest = 0; nTest < _nParticleCulling_NumToCullPerFrame; nTest++) {					// Interleave particle culling (e.g. not all next to each other so removal effect is not so sudden on 'groups' and looks more like 'ageing' instead)
			int nParticle = _nParticleCulling_CurrentGroup + nTest * _nParticleCulling_NumFrameToCullAll;
			if (_oFlexFluidParticleBank.m_particlesActivity[nParticle]) { 
				if (_oFlexFluidParticleBank.m_densities[nParticle] <= _nParticleCulling_Threshold) {					// De-activate isolated particles (zero density) as they look bad in fancy shader!
					_oFlexFluidParticleBank.m_particlesActivity[nParticle] = false;
					_oFlexFluidParticleBank.m_particles[nParticle].pos = CGame.s_vecFaraway;	// Place de-activated fluid particles faraway where they won't show too much
					_nStat_ParticlesCulled++;
				}
			}
		}
		_nParticleCulling_CurrentGroup++;											// Increment group to look at next frame.
		if (_nParticleCulling_CurrentGroup >= _nParticleCulling_NumFrameToCullAll)
			_nParticleCulling_CurrentGroup = 0;
	}
}






public class CFluidParticleRaycaster {		// CFluidParticleRaycaster: Responsible for observing fluid particles and performing ray casting and turning on fluid colliders
	int		_nParticleID;
	int		_nParticlesEmitted_DestructionTime;	// The 'global number of emitted particles' that should exist when we should destory.  Used to destroy this (expensive) raycasting particle when we get too old (have wrapped around particle pool)
	uint	_nFrameCount_NextCheck;				// Count that CGame.INSTANCE._nFrameCount_MainUpdate must be at for us to look if we should raycast this particle or not.  Slow moving particles get this number higher to not check as much


	public CFluidParticleRaycaster(int nParticleID, int nParticlesEmitted_DestructionTime) {
		_nParticleID = nParticleID;
		_nParticlesEmitted_DestructionTime = nParticlesEmitted_DestructionTime;
	}

	public bool FluidParticleRaycaster_PerformRaycast(uFlex.FlexParticles oFlexFluidParticles) {		// Perform a raycast against PhysX collider to dynamically create those we need in fluid scene.  Returns 'true' if this raycasting particle should be destroyed.
		bool bRaycastWasPerformed = false;
		if (oFlexFluidParticles.m_particlesActivity[_nParticleID]) {							// Don't run raycaster on de-activated particles
			if (oFlexFluidParticles.m_densities[_nParticleID] > 0.1f) {                         // Don't perform expensive raycasting on isolated particles...  (they will soon die anyway from culler)  ###TUNE: Define 'not dense'!
				if (CGame.INSTANCE._nFrameCount_MainUpdate >= _nFrameCount_NextCheck) {			// Don't check if it's too soon
					uFlex.Particle oFlexParticle = oFlexFluidParticles.m_particles[_nParticleID];
					Vector3 vecParticleVelocity = oFlexFluidParticles.m_velocities[_nParticleID];

					Ray oRay = new Ray(oFlexParticle.pos, vecParticleVelocity);
					RaycastHit oRayHit;
					Physics.Raycast(oRay, out oRayHit, Mathf.Infinity, CGame.INSTANCE._nLayerMask_BodyColliders);
					if (oRayHit.distance < 0.1f) {							// Avoid activating colliders too far away ###TUNE
						Transform oRayHitT = oRayHit.transform;
						if (oRayHitT != null) {
							CFlexColSphere oFlexColSphere = oRayHitT.GetComponent<CFlexColSphere>();
							if (oFlexColSphere != null) {
								oFlexColSphere.Activate(vecParticleVelocity);
							} else {
								Debug.LogWarning("#WARNING: CFlexColBody finds a collider that didn't have a CFlexColSphere.");
							}
						}
					}

					//=== Determine if this (expensive) raycasting particle is 'too old' and should be destroyed ===
					if (CGame.INSTANCE._oFlexFluid._nParticlesEmitted > _nParticlesEmitted_DestructionTime)
						return true;		// Raycasting particle should be destroyed

					//=== Determine when we can check again based on our velocity ===
					float nVelocitySqr = vecParticleVelocity.sqrMagnitude;
					int nNumOfFramesToSkip = 0;
					if (nVelocitySqr > CGame.INSTANCE._oFlexFluid._nEmitterVelocitySqr_Fast)
						nNumOfFramesToSkip = 1;
					else if (nVelocitySqr > CGame.INSTANCE._oFlexFluid._nEmitterVelocitySqr_Medium)
						nNumOfFramesToSkip =  3 + CGame.INSTANCE._oRnd.Next() % 2;			// Add a bit of randomness no next checks don't bunch up in one frame (spread the load)
					else if (nVelocitySqr > CGame.INSTANCE._oFlexFluid._nEmitterVelocitySqr_Slow)
						nNumOfFramesToSkip = 10 + CGame.INSTANCE._oRnd.Next() % 3;
					else
						nNumOfFramesToSkip = 20 + CGame.INSTANCE._oRnd.Next() % 5;								//###TUNE
					_nFrameCount_NextCheck = CGame.INSTANCE._nFrameCount_MainUpdate + (uint)nNumOfFramesToSkip;
					CGame.INSTANCE._oFlexFluid._nStat_NumFluidRaycasts++;
					bRaycastWasPerformed = true;
				}
			}
		}
		if (bRaycastWasPerformed == false)
			CGame.INSTANCE._oFlexFluid._nStat_NumFluidRaycastsAvoided++;
		return false;				// Raycasting particle should NOT be destroyed
	}
}


	//public static void FluidParticleRaycaster_ConnectToPartices(ref HashSet<CFluidParticleRaycaster> setFluidParticleRaycasters, ref uFlex.FlexParticles oFlexFluidParticles) {
	//	//=== Create sample of 'fluid particle raycaster' particles from the pool of generic fluid particles.
	//	int nParticles = oFlexFluidParticles.m_particlesCount;
	//	for (int nParticle = 0; nParticle < nParticles; nParticle += CGame.INSTANCE._nFluidParticleRaycasterMod_HACK) {
	//		setFluidParticleRaycasters.Add(new CFluidParticleRaycaster(nParticle));
	//	}
	//}
