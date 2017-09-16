using UnityEngine;


public class CFlexParamsMain : uFlex.FlexParameters /* ,uFlex.IFlexProcessor*/ {

						public	float		_nParticleMass = 1.0f;                      // Super-important particle mass.  influeces everything!!

						public	float		_nFluidParticleDiameter;					// The fluid particle diameter = distance between any two fluid particles at rest.
						public	float		_nFluidParticleRadius;						// The fluid particle radius = half the diameter.

						public	float		_nParticleInteractionRadius;				// The super-important interaction radius between particles.  Particles further than that are not 'seen' by the solver (thereby not participating in fluid forces like viscosity or fluid cohesion)
						public	float		_nParticleSolidRestDistance;				// The distance non-fluid particles attempt to maintain between each other.
						public	float		_nParticleCollisionMargins;                 // Extra margins given to both extra radius when searching for shapes and extra radius when colliding against kinematic shapes

						public	bool		_bUpdateParameters;

	public void OnStart () {
		//uFlex.FlexProcessor oFlexProc = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexProcessor)) as uFlex.FlexProcessor;
		//oFlexProc._oFlexProcessor = this;
		DoFixedUpdate();
	}

	public void DoFixedUpdate() {
		if (_bUpdateParameters) {
			Debug.LogFormat("=== FlexMain updating parameters ===");

			m_cohesion					= 0;
			m_viscosity					= 0;
			m_dissipation				= 0;
			m_adhesion					= 0;
			m_damping					= 0.0f;									//###INFO: Greatly slows down particles over time facilitating a 'puddle'
			m_surfaceTension			= 0f;

			m_numIterations				= 255;
			m_radius					= _nParticleInteractionRadius;
			m_solidRestDistance			= _nParticleSolidRestDistance;
			m_fluidRestDistance			= _nFluidParticleDiameter;
			m_CollisionDistance			= m_solidRestDistance;
			m_ParticleCollisionMargin	= _nParticleCollisionMargins;
			m_ShapeCollisionMargin		= _nParticleCollisionMargins;
			m_MaxSpeed					= 5;
			m_restitution				= 1;
			m_Fluid						= false;
			m_dynamicFriction			= 0.1f;
			m_staticFriction			= 0.1f;
			m_particleFriction			= 0.1f;
			m_buoyancy					= 1;									// Set this to 1!  0 = turns off gravity!!  -1 = inverse gravity!  WTF???
			m_inertiaBias				= 0.001f;                               //###TUNE?
			m_gravity					= new Vector3(0, -0.1f, 0);				//###TUNE: Gravity!!!!  Why does such a low setting look good??
			//m_gravity					= new Vector3(0, -9.82f, 0);

			//=== Set the particle radius in the FlexFluidRenderer ===
			//uFlex.FlexFluidRenderer oFlexParamsMainRenderer = _oFlexParamsMainParticleBank.GetComponent<uFlex.FlexFluidRenderer>();
			//if (oFlexParamsMainRenderer != null) {
			//	oFlexParamsMainRenderer.m_pointScale  = _nFluidParticleDiameter * _nShaderParticelSizeMult;
			//	oFlexParamsMainRenderer.m_pointRadius = oFlexParamsMainRenderer.m_pointScale / 2;
			//	oFlexParamsMainRenderer.m_flexParticles = _oFlexParamsMainParticleBank;		// Set a component on the fluid bank to a component on the same gameObject = dumb.
			//}

			//=== Set the particle radius in the debug FlexParticlesRenderer ===
			//uFlex.FlexParticlesRenderer oPartRend = _oFlexParamsMainParticleBank.GetComponent<uFlex.FlexParticlesRenderer>();
			//if (oPartRend != null) {
			//	oPartRend.m_size = _nFluidParticleDiameter;
			//	oPartRend.m_radius = _nFluidParticleRadius;
			//}

			_bUpdateParameters = false;
		}

		//CGame.INSTANCE._aGuiMessages[(int)EGameGuiMsg.Fluid1] = string.Format("Fluid:  A={0}/{1}  Rays={2}/{3} [#{4}]   Cull={5}", _nStat_NumActiveColliderTris, _nStat_NumActiveColliderSpheres, _nStat_NumFluidRaycasts, _nStat_NumFluidRaycastsAvoided, _setFluidParticleGroups.Count, _nStat_ParticlesCulled);
	}

	//=========================================================================	PER-FRAME FLEX UPDATE
	//public void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {		// Iterate through some of our particle to remove those that are isolated
	//}
}

namespace uFlex {
	public interface IFlexProcessor {
		void PreContainerUpdate(FlexSolver solver, FlexContainer cntr, FlexParameters parameters);
	}
}