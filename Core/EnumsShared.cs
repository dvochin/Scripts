//###NOTE ###IMPORTANT: This file is loaded & used by BOTH the Unity C# project and the C++ dll.  This is done to ensure both builds cannot get out of sync.
//###NOTE: Do not touch / remove / reorder existing enum entries.  You can only append at the end.  Editing this file requires rebuild C++ dll.


public enum EColFlags {				// Collider flags.  Keep synced between C++ and Unity!
	ePhysX2			= 1 << 0,		// Collider is created / updated in PhysX2 scene (SoftBodies only)
	ePhysX3			= 1 << 1,		// Collider is created / updated in PhysX3 scene (Everything else)
	eDynamic		= 1 << 2,		// Collider is dynamic (not the usual kinematic)
	eStatic			= 1 << 3,		// Collider is static.  It must not move after creation!
	eDrain			= 1 << 4,		// Collider is a drain that destroys fluid particles that touch it (PhysX3 only)
	eMatFriction000	= 1 << 5,		// Collider materials.  Both PhysX2 & PhysX3 have their corresponding materials for these
	eMatFriction050	= 1 << 6,		//###WEAK: Few options for friction but does simplify design accross PhysX2/3
	eMatFriction100	= 1 << 7
};

public enum EColGroups {					// PhysX collider groups.  Keep synced with C++
	eLayerDefault = 0,
	eLayerNoCollisions = 1,
	eLayerPenisI = 2,								// Penis inner chain doesn't collide with anything but is attached to penis softbody to drive it.
	eLayerPenisO = 3,								// Penis outer chain only collides against eLayerVagina vagina soft body in Physx2
	eLayerVagina = 4,								// Vagina only collides against eLayerPenisO
	eLayerBodyNoCollisionWithSelfStart = 15,		// Starting & ending point of 'no collision with self' collision groups for 1-based body ID  Each body gets one (used so breasts softbody won't collider with their own colliders but can collide against colliders from other breasts)
	eLayerBodyNoCollisionWithSelfEnd   = 31
};





public enum ESoftBody {
	VolumeStiffness,
	StretchingStiffness,
	SoftBody_Damping,
	Friction,
	SoftBody_Gravity,
	ParticleRadius,
	SolverIterations,
	SoftBody_GPU,						//###TODO!!!: Reset when changed
};

public enum EPenis {
	PenisScale,
	BaseUpDown,
	BaseLeftRight,
	ShaftUpDown,
	ShaftLeftRight,
	DriveStrength,
	DriveStrengthMax,
	DriveDamping,				//###LEARN: Setting drive damping reduces drive!!  Far better to reduce actor angular damping instead!!
	Mass,
	Density,					//###OBS: Useless??
	AngularDamping,
	LinearDamping
};

public enum EPenisTip {
	//FlagEjaculate,				// Now by CGamePlay for easier global sync
	CycleTime,						//###IMPROVE: Change curve to only active ejaculation and have 'time on' and 'time off'??
	MaxVelocity,
};

public enum ECloth {
	Cloth_GPU,
	SolverFrequency,		//###CHECK: Only 30x values have effect???
	Cloth_Stiffness,
	Bending,				//###BUG??: Bending & Shearing has an effect if stiffness lowered!  (Not orthogonal!!)
	Shearing,
	GraceAreaStrength,		//###BUG?: Only has an effect when zero!
	GraceAreaLow,
	GraceAreaHigh,
	StiffnessFrequency,
	Cloth_Damping,
	Cloth_Gravity,
	FrictionCoefficient,
};

public enum EFluid {				
	Reset,
	About,
	Help,

	Simulate,
	Fluid_GPU,

	FluidMode,
	MaxFluidParticles,
	FluidMeshOpacity,				// The presence / opacity of the polygonized fluid mesh.  ###WEAK: The one property that controls mesh polygonization!!
	ParticlesOpacity,

	ParticleSize,
	ParticleLifetime,
	Viscosity,
	Fluid_Stiffness,

	DynamicFriction,
	StaticFriction,
	Damping,
	Restitution,
	Fluid_Gravity,

	MaxMotionDistance,
	GridSize,
	RestOffsetRatio,
	ContactOffsetRatio,
	ParticleSizeRenderRatio,

	VoxelSizeRatio,
	VoxelThreshold,
	VoxelInfluence,
	VoxelSmooth,
	VoxelSmoothIterations,

	VertsNow,				//###DESIGN!!!: Remove these half-baked stats into a useful new super stat system that covers everything and that connects with profiler
	TrisNow,
	ErrorOutOfVoxels,

	EmitType,
	EmitShape,
	EmitRate,
	EmitVelocity,
	EmitRandomAngle,
	EmitRandomPosRatio,

	EmitWidthRatio,
	EmitHeightRatio,
	EmitSpacingRatio,

	FluidPolygonizerPresent
};

public enum EChoice_FluidMode {
	SPH,
	Basic
};

public enum EChoice_EmitType {
	Pressure,
	Rate
};

public enum EChoice_EmitShape {
	_Ellipse,				// Equivalent to ParticleEmitter::Shape::eELLIPSE   = 0
	_Rectangle				// Equivalent to ParticleEmitter::Shape::eRECTANGLE = 1
};
