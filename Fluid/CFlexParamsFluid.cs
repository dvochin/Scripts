﻿/*###DOCS23: Jun 2017 - Flex Fluids
=== DEV ===

=== NEXT ===

=== TODO ===

=== LATER ===

=== IMPROVE ===

=== DESIGN ===

=== IDEAS ===
- Can reduce the # of particles from the bank just by setting max at init time... useful at gameplay??
- Can self-generate our own particle bank dynamically instead of relying of Flex editor-time creation!
- Use vertex-lit flex collider mesh to visually indicate what is going on...
- Use particle colors to mark some of them (such as raycaster particle, end-of-life, what emitter, etc)
+ IDEA: Repurpose particle slower to keep reducing velocity when there is high density?
+ IDEA: Fix particles to their collider?  (or bone??)

=== LEARNED ===
- Cum now behaves better with the slower: Tuning will be very difficult.  Proceed in the order below:
	- Set gravity to the lowest 'believable' value.  High value make it difficult to keep cum on body!
	- Set the iteration count to a reasonable value.
	- Determine a reasonable 'fluid rest distance ratio'
	- Clear particle friction and set static and dynamic to 1
	- Set adhesion to the highest value possible (clear cohesion & viscocity to see)
	- Raise cohesion until blowup.
	- Raise viscocity until blowup.
	- Clear damping to zero.  We don't want to slow down the already-minimal gravity.
	- Slowly raise important dissipation until fluid stays on body as much as possible.
- Player much more efficient.  Getting frame time of 24ms in Editor = ~16ms in player!
	- Q: Can remove mirror window (e.g. just in headset for more perf?)
+ Increasing num iterations reduces bubliness, decreases staircase effect and increases viscocity
	- Meaning: keep viscosity as high as it looks good to take advantage of what we have!

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
+ Adhesion is VERY weak and starts misbehaving very early on complex body collider...  (works far better on simple planes!)

=== QUESTIONS ===

=== WISHLIST ===
- Enable emitter to have two rings of periphery particles?  (20+ particles per slice too many no?)
	- Even better would be to have circular appearance for 3,4,5,6 particles per slice etc.
- Would be nice to have 2-sided transparent shader for our debug visualizers!

*/

using System;
using UnityEngine;
using System.Collections.Generic;

public class CFlexParamsFluid : uFlex.FlexParameters, uFlex.IFlexProcessor {

						public	bool		_bUpdateParameters;							// When true this class will recalculate and propagate all the fluid parameters to all pertinent components.
						public	bool		D_DisableEmitters;
						public	bool		D_ShowParticles_Presentation = true;		//###IMPROVE: Finalize these D_ prefix!
	[HideInInspector]	public	bool		D_ShowParticles_Presentation_COMPARE = false;
	[HideInInspector]	public	bool		D_ShowParticles_Debug = false;
						public	bool		D_ShowParticles_ShowPinLines = false;		// Show tri Flex-pins between particles and their pinning verts

						public	float		_nEmitterDiameter = 0.006f;					// Diameter of fluid created by emitter.  Larger than '_nEmitterDiameter' to account for '_nThicknessWastedByShader' shader waste
						public	float		_nFluidRestDistDivInteractionRadius = 0.7f;	// The important ratio between the 'fluid rest distance' and the critically-important 'particle interaction radius'  NVidia recommends about 0.5 for good quality fluids.  Cheaper fluids can go closer to one but appear blocky  //###OPT: Optimize the crap out of this important parameter!
						public	int			_nEmitterNumParticlesAccross = 3;			// Number of particles accross the fluid emitter.  Must be 3 for now as emitter is limited to one central particle and 7 periphery ones to form a spherical-looking slice  ###IMPROVE: Enable emitter to have two rings of periphery particles?
						public	float		_nEmitterVelocityBase = 0.6f;				// Velocity base of particles of particles at emitters. Each emitter can 'scale' this value with its _nEmitterVelocityMult multiplyer.  Maximum possible fluid velocity of fluid is determined from this number (to mitigate damage from 'explosions')
						public	float		_nEmitterParticleCramming = 0.90f;			// How 'crammed' emitter particles are over the natural fluid rest distance '_nFluidParticleDiameter'.  (Fluid cohesion brings fluid particles somewhat closer than their rest distance)  Used to create at the emitter the tightest possible stream of particles that won't explode.  
						public	float		_nParticleMass = 1.0f;                      // Super-important particle mass.  influeces everything!!
						public	float		_nShaderParticleSizeMult = 2.5f;			// The multiplier applied to the shader's particle size.  Very important to 'smooth out' and blend particles.  higher settings make each particle more 'foggy' (Currently disabled?)

	[HideInInspector]	public	float		_nFluidParticleDiameter;					// The fluid particle diameter = distance between any two fluid particles at rest.
	[HideInInspector]	public	float		_nFluidParticleRadius;						// The fluid particle radius = half the diameter.

	[HideInInspector]	public	float		_nParticleInteractionRadius;				// The super-important interaction radius between particles.  Particles further than that are not 'seen' by the solver (thereby not participating in fluid forces like viscosity or fluid cohesion)
	[HideInInspector]	public	float		_nParticleSolidRestDistance;				// The distance non-fluid particles attempt to maintain between each other.
	[HideInInspector]	public	float		_nParticleCollisionMargins;					// Extra margins given to both extra radius when searching for shapes and extra radius when colliding against kinematic shapes

	[HideInInspector]	public	float		_nEmitterDistanceBetweenParticles;			// The calculated distance particles are spread apart by emitter.  Basically the _nFluidParticleDiameter * cramming ratio parameter _nEmitterParticleCramming
	[HideInInspector]	public int			_nParticlesPerSlice = 8;					// Number of particles per emitter slice.  Pretty much has to be 8 now... one central particle and 7 periphery ones to form a spherish-looking slice

						public	uint		_nParticleNext;								// The 'next' particle to be emitted from this simple static counter.  This is also equals to the # of particles ever emitted.  Note that we 'wrap around' the limited pool of actual particles
						public	uint		_nParticleCount;							// The number of particles in the particle bank
						public	uint		_nParticleAvailable;                        // The number of 'available particles' in the pool that are currently inactive.
						public  uint        _nParticleNext_FluidPinBank;                    // ID of the next particle in the (reserved) fluid pin bank.

						public	float		_nFluidGrp_CollisionDetech_AngleChange = 20.0f;		// How much direction vector has to change in a single frame for us to slow down all particles in our emit group
						public	float		_nFluidGrp_SlowdownOnInitialCollision = 0.5f;		// How much we slow down particles in this emit group when our direction vector changes too much.
						public	float		_nFluidGrp_PlaneDetect_MinDistToParticleSqr = 0.1f * 0.1f;			// How far to look for collision verts in coarse-search for collision vertices.  Anything further is ignored!
						public  float		_nFluidGrp_PlaneDetect_MinDistToEmitterSqr = 0.1f * 0.1f;   // The square of the minimum distance collision verts must clear to be considered for selection.  (Prevents cum sticking to the emitting penis)
						public	ushort      _nFluidGrp_NumFramesStabilizingToFinalPos = 20;

						public	float		_nFluidPin_SpringStrength = 0.5f;			// How strong the 'triple springs' are to glue each fluid particle pinned to a body plane collider.
						public	float		_nPinPlane_VertPullAmount = 0.005f;			// How far 'in' the body each of our pinning particles is in relation to the collider vert.  (Going inside the body enables the springs to pull fluid particles toward the collision plane and appear to 'stick' to the body

						public	float		_nParticleCulling_ThresholdDensity = 1e-16f;// Cutoff of particle density where a particul will be de-activated. ###CHECK: Such a small value!
						public	ushort		_nParticleCulling_LowDensityParticles_FrameCutoff = 10;    // How many frames it takes for a particle with low density to be flagged for destruction

	[HideInInspector]	public	bool		D_FlagForCompleteFluidDestruction;
						public	int			D_FluidEmitter_NumFramesEmitting = 0;		// How many emitter frames to emit.  0 = unlimited (for debugging)
						public	bool		D_FluidEmitter_StopAfterOneCycle = true;

						public uint			STAT_FluidParticlesCulled;
						public uint			STAT_FindPlaneFail_CannotFindThreeVerts;
						public uint         STAT_FindPlaneFail_CannotFindOnSameBody;
						public uint         STAT_FindPlaneFail_NotPinnedToCollider;

	public AnimationCurve CurveEjaculation;					// Designer-adjustable curves to adjust per-frame fluid properties to simulate man/woman ejaculation
						public	float		_nEmitter_CumCycleTimeSpan = 6f;			// How many seconds a cum squirt lasts.
						public float        _nEmitter_DistForPreviousFrameParticles_HACK = 50;        //###TUNE: Can reduce?  ###IMPROVE: Autotune or calculate a better value from max fluid speed?

	[HideInInspector]	public static uint	C_NumFluidPinParticles = 1000;      // How many particles to allocate for the (separate) fluid particle pin bank
						public uint			_nNumPinningSprings;

	[HideInInspector]	public	uFlex.FlexParticles _oFlexParamsFluidParticleBank;          // The bank of particles reserved for Fluid dispensation.  (Generated by uFlex design-time tool and neatly 'parked' in a cube so we can see how many are left)
	[HideInInspector]	public  uFlex.FlexSprings	_oFlexSprings;

	[HideInInspector]	public	Queue<CFluidParticleGroup> _queueFluidParticleGroups = new Queue<CFluidParticleGroup>();	// Queue of particle observers: responsible for observing fluid particles and performing ray casting and turning on fluid colliders	//###INFO: C# Sets
	[HideInInspector]	public	HashSet<CFlexEmitter> _setFlexEmitters = new HashSet<CFlexEmitter>();   // List of emitters pulling particles from this pool.



	public void OnStart () {
		_oFlexParamsFluidParticleBank = GameObject.Find("FlexFluidParticleBank").GetComponent<uFlex.FlexParticles>();
		_bUpdateParameters = true;
		D_ShowParticles_Presentation_COMPARE = !D_ShowParticles_Presentation;		//###WEAK (Guarantee first update)
		uFlex.FlexProcessorFluid oFlexProc = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexProcessorFluid)) as uFlex.FlexProcessorFluid;
		oFlexProc._oFlexProcessor = this;
		_oFlexSprings   = CUtility.FindOrCreateComponent(_oFlexParamsFluidParticleBank.gameObject, typeof(uFlex.FlexSprings))   as uFlex.FlexSprings;   //###CHECK: On bank?? (Needed by FlexContainer)

		DoFixedUpdate();
	}

	public void DoFixedUpdate() {
		if (_bUpdateParameters) {
			Debug.LogFormat("=== FlexFluid updating fluid parameters ===");

			_nFluidParticleDiameter = _nEmitterDiameter / _nEmitterNumParticlesAccross;       // Determine the fluid particle-to-particle rest distance.
			_nFluidParticleRadius	= _nFluidParticleDiameter / 2;

			_nParticleInteractionRadius	= _nFluidParticleDiameter / _nFluidRestDistDivInteractionRadius;
			_nParticleSolidRestDistance = _nParticleInteractionRadius * 0.90f;						// Rest distance from solids a bit less than interaction radius for more robust collisions.
			_nParticleCollisionMargins	= _nParticleInteractionRadius * 0.15f;                       // Provide extra margins for robust collision against kinematic shapes and fast-moving particles

			m_radius					= _nParticleInteractionRadius;
			m_solidRestDistance			= _nParticleSolidRestDistance;
			m_fluidRestDistance			= _nFluidParticleDiameter;
			m_CollisionDistance			= m_solidRestDistance;
			m_ParticleCollisionMargin	= _nParticleCollisionMargins;
			m_ShapeCollisionMargin		= _nParticleCollisionMargins;
			m_MaxSpeed					= _nEmitterVelocityBase * 2.0f;			// Provide a bit more max speed than emitter speed in case fluid falls down (for gravity acceleration)
			m_Fluid						= true;                                 // Yes... we are a fluid solver!
			m_inertiaBias				= 0;
			m_buoyancy					= 1;									// Set this to 1!  0 = turns off gravity!!  -1 = inverse gravity!  WTF 'bouyancy' have to do with gravity???

			//###INFO: All these important parameters are now set in Unity editor GUI
			//m_numIterations             = 3;									//###OPT:!!!!: Can go down to two? ###IMPROVE: Study the influence of this on real performance as higher iterations (e.g. 10) look nicer!
			//m_cohesion					= 1.0f;								//###TUNE:!!! Critical and heavily influcned by mass... Turn this up until fluid explodes!
			//m_viscosity					= 25f;									//###TUNE:!!! As high as possible before fluid explodes
			//m_adhesion					= 0.0f;									//###TUNE!!! Tune until fluid rolls a bit on body then stops.  As low as possible
			//m_dissipation				= 0.03f;								//###INFO: Super important: Slows down particles by # of contacts.  Really helps forming puddles but sensitive!
			//m_damping					= 0.1f;									//###INFO: Greatly slows down particles over time facilitating a 'puddle'
			//m_surfaceTension			= 1.0f;
			//_nEmitterVelocityBase		= 0.6f;
			//m_restitution				= 0;                                    // Very bad!!  Bounces fluid off body!  Keep at 0!!
			//m_dynamicFriction			= 0;									//###INFO: Gets particles going in unreasonable directions (with necessary adhesion?)  ###INFO: A high setting prevents particles from accelerating with gravity!  WTF?
			//m_staticFriction			= 1;									// Very beneficial: Slows down fluid once it reaches body!
			//m_particleFriction		= 1;                                    //###TUNE?
			//m_gravity					= new Vector3(0, -0.1f, 0);             //###TUNE: Gravity!!!!  Why does such a low setting look good??

			//=== Obtain the number of particles and related counters ===
			if (_nParticleCount == 0)
				_nParticleCount = (uint)_oFlexParamsFluidParticleBank.m_particlesCount - C_NumFluidPinParticles;
			_nParticleAvailable = _nParticleCount;
			_nParticleNext_FluidPinBank = _nParticleCount;		// First particle ID in the fluid pin bank is the count of normal particles. (They are last in array)
			_nEmitterDistanceBetweenParticles = _nFluidParticleDiameter * _nEmitterParticleCramming;

			//=== Assign the pinning spring arrays ===
			_nNumPinningSprings = _nParticleCount * 3;			//###OPT:!! Worse case scenario.  Safe but EXPENSIVE in memory!  A scheme where we dynamically allocate according to load would be much more frugal but take more CPU cycles per frame.
			_oFlexSprings.m_springIndices       = new int  [_nNumPinningSprings * 2];
			_oFlexSprings.m_springRestLengths   = new float[_nNumPinningSprings];
			_oFlexSprings.m_springCoefficients  = new float[_nNumPinningSprings];
			_oFlexSprings.m_springsCount = (int)_nNumPinningSprings;

			//=== Notify our connected emitters to update their data structures ===
			foreach (CFlexEmitter oFlexEmitter in _setFlexEmitters)
				oFlexEmitter.OnEmitterParametersChanged();

			//=== Set the particle radius in the FlexFluidRenderer ===
			uFlex.FlexFluidRenderer oFlexParamsFluidRenderer = _oFlexParamsFluidParticleBank.GetComponent<uFlex.FlexFluidRenderer>();
			if (oFlexParamsFluidRenderer != null) {
				oFlexParamsFluidRenderer.m_pointScale  = _nFluidParticleDiameter * _nShaderParticleSizeMult;
				oFlexParamsFluidRenderer.m_pointRadius = oFlexParamsFluidRenderer.m_pointScale / 2;
				oFlexParamsFluidRenderer.m_flexParticles = _oFlexParamsFluidParticleBank;		// Set a component on the fluid bank to a component on the same gameObject = dumb.
			}

			//=== Set the particle radius in the debug FlexParticlesRenderer ===
			uFlex.FlexParticlesRenderer oPartRend = _oFlexParamsFluidParticleBank.GetComponent<uFlex.FlexParticlesRenderer>();
			if (oPartRend != null) {
				oPartRend.m_size = _nFluidParticleDiameter;
				oPartRend.m_radius = _nFluidParticleRadius;
			}

			_bUpdateParameters = false;
		}

		if (Input.GetKeyDown(KeyCode.KeypadPeriod))
			D_ShowParticles_Presentation = !D_ShowParticles_Presentation;

		if (D_ShowParticles_Presentation_COMPARE != D_ShowParticles_Presentation) {
			D_ShowParticles_Presentation_COMPARE = D_ShowParticles_Presentation;
			//###IMPROVE: Cannot re-enable SSF image effect! Camera.main.GetComponent<SSF.SSFPro_ComposeFluid>().enabled = D_ShowParticles_Presentation;
			D_ShowParticles_Debug = !D_ShowParticles_Presentation;
			_oFlexParamsFluidParticleBank.GetComponent<uFlex.FlexParticlesRenderer>()	.enabled = D_ShowParticles_Debug;
			_oFlexParamsFluidParticleBank.GetComponent<uFlex.FlexFluidRenderer>()		.enabled = D_ShowParticles_Presentation;
			Camera.main.GetComponent<SSF.SSFPro_ComposeFluid>().enabled = D_ShowParticles_Presentation;
		}

		CGame.INSTANCE._aGuiMessages[(int)EGameGuiMsg.Fluid1] = string.Format("Fluid:  On={0}  Off={1}  Grps={2}  Springs={3}  Cull={4}", _nParticleCount-_nParticleAvailable, _nParticleAvailable, _queueFluidParticleGroups.Count, _oFlexSprings.m_springsCount, STAT_FluidParticlesCulled);
	}

	public void Emitter_Register(CFlexEmitter oFlexEmitter) {		// Register an additional emitter to our set
		_setFlexEmitters.Add(oFlexEmitter);
	}

	public void Emitter_Unregister(CFlexEmitter oFlexEmitter) {		// Unregister an additional emitter from our set
		_setFlexEmitters.Remove(oFlexEmitter);
	}



	//=========================================================================	PARTICLE BANK
	public void ParticleBank_ReserveParticles(uint nNumParticlesRequested) {
		while (nNumParticlesRequested > _nParticleAvailable) {
			CFluidParticleGroup oFluidParticleGroup_Oldest = _queueFluidParticleGroups.Dequeue();
			_nParticleAvailable += oFluidParticleGroup_Oldest._nNumParticlesInGroup;
			oFluidParticleGroup_Oldest.DoDestroy();
		}
		_nParticleAvailable -= nNumParticlesRequested;
	}

	public void ParticleBank_DestroyAll() {
		while (_queueFluidParticleGroups.Count > 0) {
			CFluidParticleGroup oFluidParticleGroup_Oldest = _queueFluidParticleGroups.Dequeue();
			_nParticleAvailable += oFluidParticleGroup_Oldest._nNumParticlesInGroup;
			oFluidParticleGroup_Oldest.DoDestroy();
		}
		_nParticleNext = 0;
		STAT_FluidParticlesCulled = 0;
		STAT_FindPlaneFail_CannotFindOnSameBody = 0;
		STAT_FindPlaneFail_CannotFindThreeVerts = 0;
		STAT_FindPlaneFail_NotPinnedToCollider = 0;
	}

	public uint ParticleBank_GetNextParticle(bool bFromParticlePinBank = false) {         // Called by emitter to get the next / oldest particle.  Caller is responsible to activate & fully quality (array still contains old garbage values)
		if (bFromParticlePinBank) {     // From reserve 'fluid pin particles' needed by CFluidParticlePin
			if (_nParticleNext_FluidPinBank >= _oFlexParamsFluidParticleBank.m_particlesCount)
				CUtility.ThrowException("###EXCEPTION: ParticleBank_GetNextParticle() could not allocate a particle in the (limited) bank of fluid particles!");
			return _nParticleNext_FluidPinBank++;
		} else {                        // Regular particle
			return _nParticleNext++ % _nParticleCount;			// Wrap around as we're a re-usable circular buffer
		}
	}

	
	//=========================================================================	FLUID PARTICLE GROUPS
	public CFluidParticleGroup FluidParticleGroup_Add(CFlexEmitter oFlexEmitter) {
		CFluidParticleGroup oFluidParticleGroup = CFluidParticleGroup.Create(oFlexEmitter);
		_queueFluidParticleGroups.Enqueue(oFluidParticleGroup);
		return oFluidParticleGroup;
	}

	public void FluidParticleGroup_UpdateParticles() {
		if (D_FlagForCompleteFluidDestruction) {
			ParticleBank_DestroyAll();
			D_FlagForCompleteFluidDestruction = false;
		} else {
			//=== Iterate through all bodies to update the fluid pinning particles ===
			foreach (CBodyBase oBodyBase in CGame.INSTANCE._aBodyBases) {		//###MOVE:??  ###CHECK:!!! Is this the right timing to guarantee baked mesh as recent as possible??
				if (oBodyBase._oBody != null && oBodyBase._oBody._oFlexTriCol_BodyFluid)
					oBodyBase._oBody._oFlexTriCol_BodyFluid.OnSimulate();
			}

			//=== Iterate through our particle groups to process them for this frame ===
			foreach (CFluidParticleGroup oFluidParticleGroup in _queueFluidParticleGroups)
				oFluidParticleGroup.FluidParticleGroup_UpdateParticles();

			//=== Update the springs to pin the 'pinned' fluid particles to where they should be ===
			SpringBank_DynamicBuildSprings();
		}

	}


	//=========================================================================	DYNAMIC SPRING CONSTRUCTION
	public void SpringBank_DynamicBuildSprings() {		// Dynamically fully re-create the spring arrays at each frame.  Not cheap but we don't have a choice given the considerable changes between frames.  KEEP FAST!!
		//=== Dynamically create our springs at each frame by iterating through all particle groups in pinning mode to query all their pinned particles to add three springs for each of the plane verts ===
		int nSpring = 0;
		int nSpringX2 = nSpring * 2;
		foreach (CFluidParticleGroup oFluidParticleGroup in _queueFluidParticleGroups) {
			if (oFluidParticleGroup._eFluidParticleGroupMode >= CFluidParticleGroup.EFluidParticleGroupMode.S3_PinnedToCollisionPlane) {
				foreach (CFluidParticle oFluidParticle in oFluidParticleGroup._aFluidParticles) {
					if (oFluidParticle._bIsDrivenByTripleFlexSprings) {
						for (int nVertPinPlane = 0; nVertPinPlane < 3; nVertPinPlane++) {
							CFluidParticlePin oFluidParticlePin = oFluidParticleGroup._aFluidParticlePins[nVertPinPlane];
							_oFlexSprings.m_springIndices[nSpringX2 /*+ 0*/] = (int)oFluidParticle._nParticleID;
							_oFlexSprings.m_springIndices[nSpringX2   + 1  ] = (int)oFluidParticlePin._nParticleID;
							_oFlexSprings.m_springRestLengths[nSpring] = oFluidParticle._aSpringLengths[nVertPinPlane];
							_oFlexSprings.m_springCoefficients[nSpring]  = _nFluidPin_SpringStrength;
							nSpring++;
							nSpringX2 = nSpring * 2;
						}
					}
				}
			}
		}
		if (nSpring >= _nNumPinningSprings)
			CUtility.ThrowException("###EXCEPTION: Ran out of springs in static spring array.");
		_oFlexSprings.m_springsCount = nSpring;
	}


	//=========================================================================	PER-FRAME FLEX UPDATE
	public void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {		// Iterate through some of our particle to remove those that are isolated
		FluidParticleGroup_UpdateParticles();		// Iterate through particle groups so all particles needing processing are processed.
	}
}
