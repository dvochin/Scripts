
///*###DISCUSSION: Fluid
//=== NEXT ===
// * Cam control
//	* Automatic focus points that camera can connect to (for game and ff) with emitter, fluid center, organs, etc
// * Gizmo finicky!  Can get lost and stop working!
 
// * Materials not initialized!
// * All shapes appear to be missing a pane from export!
// * Rotate slow??
// * Need safety trap code on some values like grid size, etc
// * Format of property number display, including %
// * Shift currently only applied to z... has no value now?  
// * How the heck are panels linked???
// * Voxel smooth setting wonky when low.
// * Stats and error reporting critical
// * Need to trim integer properties
//-- Product release ---
// * StdDev problem with new emissions!
// * Crash fix: remove from inner loop!
//	 * Should be invoked only when performance slows!!
// * Choice of what voxel to render done very poorly!  Go around center??  Hook up with stddev??
// * Trimming # of verts???
// * Remove mesh merge on demand
// * Display error conditions clearly!
// * Renderer choice
// * Tighter torus and other shapes for fun
// * Choice between renderers
//	 * Efficient shutdown of unused one
// * Option to disable mesh merge
// * Load and save base configs
// * some args not loading!
// * Get a great camera with good hotkeys & mouse control
// * Figure out if we can ship with iGUI and redo GUI
// * World takes 15ms to render!  Display stats without it!!!!!

//=== TODO ===
// * Would be nice to only have 'good' shuriken particles shown!
// * Forcing users to move stuff in scene views kills performance...
//	 * Include our gizmo to help sell!!
// * Adjusting stiffness from life of particle a MUST for our cause... get it in!
// * Remove parts of scene not working: spiral, torus, and replace with ramp with loop
//	 * Make funnel more angled so we get fluid flow as it moves fast
// * Neat test effect is to set life to infinite and release all particles very quickly
// * Need to draw attention during overflow!!
// * We're leaking too much... revisit colliders


//=== LATER ===

//=== DESIGN ===
// * investigate if mesh.clear helps or not...

//=== IDEAS ===
// * Particles emitted / dying per second a useful stat
// * Particles rest offset enables particles of different sizes!!
// * Instead of setting emitter velocity set max motion distance!!
// * Freaky effect if you also draw particles with glow!


//=== LEARNED ===

//=== PROBLEMS ===
// * Have seen changing max motion cause fluid to split then changing lifetime fixing it!  WTF???
// * Have seen polygonization start taking 4 times longer and disseaper suddenly... one same stream to ground!
// * Watch Grid size get way out of wack!  Add occasional test???
// * Fluid stiffness heavily affected by delta simulation time?
// * Weird bugs with increasing emitter widht yields fewer pressure sites!
// * Quick autoreduction of emitter velocity will clamp speed of existing particles!
// * BUG: Disabling / reenabling colliders creates new PhysX copy!!!!!!
// * Changing particle size gets all ratios changing percentage!! (put back old code??) 
// * Going from non-SPH to SPH loses important settings like stiffness
// * WTF? Why some particles going through colliders and drains sometimes??  Grid size??
// * BUG: Particles going through colliders!  Seems like particles that should be dead are going right through and still rendering.  (Possibly because of grid overflow?)
// * BUG: Setting emitter width/height resets spacing!
// * ParticleSizeRenderRatio no longer set!
// * REVISIT INIT DESIGN OF COLLIDERS! Only one init!!
//-? Emit spacing wonky versus w/h
//	-? WTF wrong with emit width / height changing place... have to rebuid??
// * Changing mode doesn't resend all... external accel lost
// * Definitively a problem with particle lifetime... Does it really affect only emitter??
//	 * getValidParticleRange() never goes down!  WTF??  Particles not dying??
//	 * Appears to go up/down at every 'particle lifetime'... related to emitter object???
//=== PROBLEMS: ASSETS ===

//=== PROBLEMS??? ===
//-? Random pos huge start value
// * Fluid_Stiffness setting change has no effect with CPU mode!!
//	 * Only when changing in Cloth_GPU mode does it take... and switching back to CPU mode new setting takes then!! WTF??
// * Resizing colliders has no effect... but disabling / re-enable ok.
//	 * Implement a global 'update strategy'

//=== WISHLIST ===
// * Dual nature of pushing in painting and taking away with threshold complicates things... Improve paint to really paint its real-life size no matter voxel size!
// * Add a simple torus so we can demonstrate cool flow effect (without gravity)
// * Extra velocity property for tighter MaxMotionDistance
// * Additional fun things to the scene...
//	 * Moving buckets
//	 * triggers & notification?
// * Load world in Unity and keep there in scene??
// * Have a floating draggable iGUI panel that shows # of particles... useful for screen clippings!
// * Expose properties of physics material
// * Better camera!!
//	eCOLLISION_TWOWAY					= (1<<0),
//	eCOLLISION_WITH_DYNAMIC_ACTORS		= (1<<1),
//	ePER_PARTICLE_REST_OFFSET			= (1<<4),
//		 * Enables far greater output of emitter!
// * Better 'pressure emitter' with perfect cylindrical flow!
//*/


//using UnityEngine;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Runtime.InteropServices;


//public class CFluid : MonoBehaviour, IObject {

//	[HideInInspector] public CObject		_oObj;		// The multi-purpose CObject that stores CProp properties  to publicly define our object.  Provides client/server, GUI and scripting access to each of our 'super public' properties.

//	int					_nMaxFluidParticles;			// Meant to save going to server for frequently-used property but ugly nonetheless

//	Mesh				_oMeshRender;
//	CMemAlloc<Vector3> 	_memVertsFluid;		// Each fluid particle is a vertex that is expanded in a billboard equilateral triangle.  So 3 verts in the rendering mesh per particle
//	CMemAlloc<int> 		_memTrisFluid;
//	GameObject			_oFluidMeshGO;
//	MeshRenderer		_oFluidMeshRend;


//	CMemAlloc<UnityEngine.ParticleSystem.Particle> _memParticles = new CMemAlloc<UnityEngine.ParticleSystem.Particle>();		// The Shuriken particles of this fluid... with positions set by our DLL to take advantage of its advanced / fast fluid PhysX simulation.
//	ParticleSystem		_oShuriken;

//	Material			_oMatFluidMesh;				// The shared material that renders the polygonized fluid mesh.
//	Material			_oMatFluidParticles;		// The shared material that renders the Shuriken-based 'billboard particles'

//	//bool				_bWorkerThreadRunning;		// When true the expensive polygonize worker thread is running.  We skip _nFpsFrames until it is done


//	//--------------------------------------------------------------------------	Initialization

//	public void OnAwake() {									//###DESIGN: Call from central game??
//		_oObj = new CObject(this, 0, typeof(EFluid), "Fluid");

//		//=== Fetch materials we programmatically control ===
//		_oMatFluidMesh		= Resources.Load("Materials/Scene/FluidMesh") as Material;
//		_oMatFluidParticles = Resources.Load("Materials/Scene/FluidParticles") as Material;

//		//=== Initialize Shuriken ===
//		Transform oFluidShurikenRenderer = Camera.main.transform.FindChild("FluidShurikenRenderer").transform;
//		if (oFluidShurikenRenderer == null)
//			throw new CException("ERROR: CFluid.ctor() could not find 'FluidShurikenRenderer' child node under main camera object.");		// Main camera *must* contain a child object called "FluidShurikenRenderer" that contains the Shuriken particle system that will always render the fluid regardless of camera angle.  (Done this way as otherwise the entire fluid would dissapear if its origin is not in the camera frustrum)
//		_oShuriken = oFluidShurikenRenderer.GetComponent<ParticleSystem>();
//		if (_oShuriken == null)
//			throw new CException("ERROR: CFluid.ctor() could not find Shuriken particle system in 'FluidShurikenRenderer' camera sub-node");	// FluidShurikenRenderer must contain a properly-configured Shuriken particle system to render the fluid
//		_oShuriken.enableEmission = false;
//		_oShuriken.Stop();
//		_oShuriken.GetComponent<Renderer>().sharedMaterial = _oMatFluidParticles;

//		//=== Initialize polygonized mesh renderer ===								//###DESIGN!!!: What in cases where mesh renderer always off?? Can clean some of this???
//		_oFluidMeshGO = GameObject.Find("FluidPolygonized");						//###DESIGN: Let this be the mesh for our node instead of external one??
//		MeshFilter oMeshFilter	= _oFluidMeshGO.GetComponent<MeshFilter>();
//		_oFluidMeshRend = _oFluidMeshGO.GetComponent<MeshRenderer>();
//		_oMeshRender = new Mesh();				// Create the actual mesh the object will use.  Will persist throughout...
//		_oMeshRender.MarkDynamic();				// Docs say "Call this before assigning vertices to get better performance when continually updating mesh"
//		_memVertsFluid = new CMemAlloc<Vector3>();
//		_memTrisFluid = new CMemAlloc<int>();
//		oMeshFilter.mesh = _oMeshRender;
//		_oFluidMeshRend.sharedMaterial = _oMatFluidMesh;

//		//=== Create the server-side fluid object.  Essential before defining / connecting to server-side properties below ===
//		_oObj._hObject = ErosEngine.Fluid_Create(_oObj._sNameFull, _oObj.GetNumProps());		//###DESIGN: Write base function to accept & process handle???

//		//===== Create / connect the client / server properties that will be used to define and manage this Fluid =====
//		_oObj.PropGroupBegin("", "Buttons", false, 3);
//		//oObj.PropAdd(EFluid.Help,					"Help",		0, "Display the help panel.", CProp.Local | CProp.AsButton);
//		//oObj.PropAdd(EFluid.About,				"About",	0, "Display the application information screen.", CProp.Local | CProp.AsButton);
//		_oObj.PropAdd(EFluid.Reset,					"Reset",	0, "Reset the simulation.", CProp.NeedReset | CProp.Local | CProp.AsButton);

//		_oObj.PropGroupBegin("", "", false, 2);
//		_oObj.PropAdd(EFluid.Simulate,				"Simulate", 1, "Enable / Disable PhysX Fluid simulation.", CProp.AsCheckbox);
//		_oObj.PropAdd(EFluid.Fluid_GPU,				"GPU",		1, "Run the fluid simulation on the Cloth_GPU.\nRequires a CUDA-enabled NVidia video card.", CProp.AsCheckbox);

//		_oObj.PropGroupBegin("Simulation Control", "Options that control the global state of the fluid simulation.");
//		_oObj.PropAdd(EFluid.FluidMode,				"Mode:",		typeof(EChoice_FluidMode), (int)EChoice_FluidMode.SPH, "The fluid simulation mode.\nSPH stands for 'Smoothed Particles Hydrodynamics' and closely simulates\nreal-world fluids by simulation inter-particle forces.\nBasic fluid mode does not simulate inter-particle forces and\nsimply colliders fluid particles against its environment.", CProp.NeedReset + CProp.Hide);		//###BROKEN: Crashes the game, not sure why... hiding as it's useless in game anyways
//		_oObj.PropAdd(EFluid.MaxFluidParticles,		"#Particles",	2000,	500,	10000,	"The maximum number of fluid particles that can be\ncreated / managed by this Fluid.\nA fraction of this number may be active / rendered at any given time.", CProp.NeedReset);

//		_oObj.PropGroupBegin("Base Properties", "Core fluid properties:\nThe most important settings that have the most visible effects on fluid simulation.");
//		_oObj.PropAdd(EFluid.ParticleSize,			"Size",			.006f,	0.003f,	0.01f,	"Size of particles in meters.\nHas considerable influence on the simulation.\nWhile FastFluid internally rescales most PhysX parameters to\nreduce fluid scaling efforts note that fluids have to be 'retuned'\neverytime this is changed.\nKeep particle size as large as possible to grealy increase fluid efficiency.", CProp.NeedReset);
//		_oObj.PropAdd(EFluid.ParticleLifetime,		"Lifetime",		16,		0,		30,		"The lifetime of particles in seconds.  Set to zero for infinite life.", CProp.NeedReset);		//###IMPROVE: Help text once more is known on what is going on with changing particle life!!	###CHECK: Reset needed??	###DESIGN!!: Consider driving this with ejaculation cycle time??
//		_oObj.PropAdd(EFluid.Viscosity,				"Viscosity",	75,		5,		150,	"The viscosity of the fluid (SPH mode only)\nDetermines how 'thick' or 'runny' the fluid is.\nFluid will become unstable and 'explode' if this value is set too high");		//###CHECK: Min?
//		_oObj.PropAdd(EFluid.Fluid_Stiffness,		"Stiffness",	20f,	0.1f,	30,		"The strength of the force that aims to return fluid particles at\ntheir at-rest density (SPH mode only)\nRaise this value as much as fluid stability allows to\ngreatly increase the visible volume of your fluid and obtain better benefit/performance.\nNOTE: Changing this setting in CPU-mode requires a fluid reset (bug?)");	//###NOTE: Fluid_Stiffness appears to have a PhysX bug on CPU-mode and needs reset!  (Cloth_GPU is fine!)

//		_oObj.PropGroupBegin("Physical Properties", "Fluid properties related to the laws of Physics.");
//		_oObj.PropAdd(EFluid.DynamicFriction,		"Dyn.Friction",	1.0f,	0,		1,		"Dynamic friction determines the force needed to keep\na particle sliding along a colliding shape.");
//		_oObj.PropAdd(EFluid.StaticFriction,		"Sta.Friction",	1.0f,	0,		1,		"Static friction determines the force needed to get a\nparticle to begin to slide on a colliding shape.");
//		_oObj.PropAdd(EFluid.Damping,				"Damping",		0.5f,	0,		5,		"The damping force that is applied to each particle.\nIf this is set too high the particle will eventually stop in mid-air regardless of gravity.");	//###TEMP??
//		_oObj.PropAdd(EFluid.Restitution,			"Restitution",	0.0f,	0,		1,		"The strength of the reflective force applied to particles as\nthey collide with shapes in the scene.\nDetermines how 'bouncy' the fluid behaves as it collides.");
//		_oObj.PropAdd(EFluid.Fluid_Gravity,			"Local Gravity",0.7f,	-3,		3,		"Acceleration applied to fluid particles along up/down (y) negated direction in m/s^2");

//		_oObj.PropGroupBegin("Advanced Properties", "Advanced fluid properties that require in-depth knowledge and careful tuning.");
//		_oObj.PropAdd(EFluid.MaxMotionDistance,		"Max Motion",	.030f,	0.01f,	.1f,	"The maximum distance a particle can travel in one simulation frame in meters.\nHas *major* impact on performance.\nKeep this as *low as possible* for maximum efficiency.");		//###TUNE!!!!!  ###IMPORTANT!!!
//		_oObj.PropAdd(EFluid.GridSize,				"Grid Size",	.02f,	.01f,	1,		"Sets the particle grid size used for internal spatial data structures.\nIf 'OverflowErrors' occur (removing particles because there are too many in a grid cell)\nraise this value just high enough the errors no longer occur.");
//		_oObj.PropAdd(EFluid.RestOffsetRatio,		"Rest Offset",	0.3f,	0,		1,		"Sets the distance between particles and collision geometry maintained during\nsimulation as a ratio of particle diameter.\nMust be lesser or equal to ContactOffsetRatio.\nSetting this to zero will cause fluid particles to rest exactly on the surface of colliders while\nhigher value will help the 'glide' over irregular obstancles and\ngreatly improve the fluidity of the simulation.\nA value around 30% is suggested as higher values can greatly reduce performance.");
//		_oObj.PropAdd(EFluid.ContactOffsetRatio,	"Contact Offset",0.6f,	0,		1,		"Sets the distance at which contacts are generated between particles and\ncollision geometry as a ratio of particle diameter.\nMust be greater or equal to ContactOffsetRatio.\nA reasonable value of 60% is suggested as higher values can greatly reduce performance.");

//		_oObj.PropGroupBegin("Voxel Mesh Creation", "Properties that direct how voxelization of the\nfluid particles occur and how the resultant mesh is created.");
//		_oObj.PropAdd(EFluid.VoxelSizeRatio,		"Size",			1.0f,	.5f,	2,		"The size of the 3D voxel as a ratio to particle size.\nUsed during mesh creation by our OpenCL Marching Cubes algorithm to polygonize the\nfluid particle cloud into a renderable mesh.\nSmall voxel sizes produce finer fluid meshes but are computationally expensive\nand reduce the usable volume of the polygonizer.");
//		_oObj.PropAdd(EFluid.VoxelThreshold,		"Threshold",	0.27f,	0.05f,	1f,		"The minimum weight a voxel must have to be included in the fluid rendering mesh.\nLow values produce more fluid volume at the cost of visible spheres at the fluid boundaries.\nHigh values smooth the fluid particle spheres but reduce the fluid volume.");		//###BUG: Bad geometry appears if we go too low!
//		_oObj.PropAdd(EFluid.VoxelInfluence,		"Influence",	1.0f,	0.25f,	3f,		"Important ratio that affects the 'area of influence' of a particle during fluid voxelization.\nHigher values produce a smoother fluid mesh but greatly decrease performance.\nLower values run faster but create meshes that have individual particles more evident.\nValues below one will introduce invisible geometry inside larger fluid volumes that decreases performance.\nKeep around one for best performance.");
//		_oObj.PropAdd(EFluid.VoxelSmooth,			"Smooth",		1,		0f,		1f,		"Smoothing applied to voxels before mesh creation.\nSet to near 1 to smooth the particle cloud field to smooth out the\nappearance of the resultant mesh.\nSet to lower values for more visible particle spheres in the voxel mesh.\nA value of zero disables smoothing and slightly increases performance.");
//		_oObj.PropAdd(EFluid.VoxelSmoothIterations,"Smooth #",		1,		0,		5,		"Number of times smoothing algorithm is invoked to smooth the mesh.\nWhile each iteration decreases performance, the resultant mesh contains less geometry and renders faster.\nNote that excessive smoothing may result in holes in the mesh,\nbut a simple increase in the 'Threshold' value will fix this.\n1 iterations is recommended.");

//		_oObj.PropGroupBegin("Emitter", "Misc. emitter properties.");
//		_oObj.PropAdd(EFluid.EmitType,				"Type:",		typeof(EChoice_EmitType),	(int)EChoice_EmitType.Pressure, "'Pressure-type' emitters attempt to maintain a constant volume of particles in\nemit area with the maximum pressure set by 'EmitRate'.\n'Rate' type emitters simply create the 'EmitRate' number of particles at each frame.", CProp.NeedReset);
//		_oObj.PropAdd(EFluid.EmitShape,				"Shape:",		typeof(EChoice_EmitShape),	(int)EChoice_EmitShape._Ellipse, "Selects the shape of the emitter opening.  Ellipse and Rectangle are\navailable and both their dimensions are set by EmitWidth and EmitHeight.", CProp.NeedReset);
//		_oObj.PropAdd(EFluid.EmitRate,				"Rate",			0,		0,		10000,	"The number of particles created by this emitter per frame.");
//		_oObj.PropAdd(EFluid.EmitVelocity,			"Velocity",		0.8f,	0,		1.5f,	"The output velocity of the emitter particles.  Note that this is capped by the fluid's\n'MaxMotionDistance' which should be set as low as possible for\nperformance.");
//		_oObj.PropAdd(EFluid.EmitRandomAngle,		"Angle",		0.1f,	0,		.5f,	"Random angle applied to emitted particles in radians.\nA small angle deviation greatly helps fluids behave more realistically to\nbreak the mathematical precision imposed during emission.");
//		_oObj.PropAdd(EFluid.EmitRandomPosRatio,	"Shift",		2,		0,		10,		"Random position shift applied to emitted particles as a ratio of particle size.\nA small deviation greatly helps fluids behave more realistically to\nbreak the mathematical precision imposed during emission.");
//		_oObj.PropAdd(EFluid.EmitWidthRatio,		"Width",		3,		0,		3,		"Width of the emitter opening as a ratio of particle size.");		//####MOD: Was 1,1,1
//		_oObj.PropAdd(EFluid.EmitHeightRatio,		"Height",		3,		0,		3,		"Height of the emitter opening as a ratio of particle size.");
//		_oObj.PropAdd(EFluid.EmitSpacingRatio,		"Spacing",		2,		0,		3,		"X,Y,Z Distance between emitted particles are from one-another\nas a ratio of particle size.  (Affects both the x,y distance between emission sites and the z 'time-axis' emission of particles over time.");

//		_oObj.PropGroupBegin("Rendering Control", "Properties influencing how the simulation output is rendered.");
//		_oObj.PropAdd(EFluid.FluidMeshOpacity,		"Mesh Opacity", 0.7f,	0,		1,		"Opacity of the polygonized fluid mesh.  Set to zero to disable the expensive polygonization\nstep and one to render with a solid material.\nFluid polygonization is a feature requiring FastFluid Pro");
//		_oObj.PropAdd(EFluid.ParticlesOpacity,		"Par. Opacity", 0.0f,	0,		1,		"Opacity of the 'billboard rendered' particles using Shuriken particle rendering system.\nSet to zero to hide the relatively-inexpensive step of rendering individual particles by\ndrawing a 'billboard sprite' for each one.", CProp.Local);
//		_oObj.PropAdd(EFluid.ParticleSizeRenderRatio,"Render Size",	7.0f,	0.5f,	10.0f,	"Size of rendered billboard particles versus their actual simulated size as\ndrawn by the Shuriken particle system.\nKeep this as large as possible to maximize the visual appearance of costly\nfluid particles.  Note that obtaining quality fluid effects is difficult with this rendering technique.\nExtensive experimentation with shaders is needed to improve the\nappearance of this simple-yet-inexpensive rendering technique.", CProp.Local);

//		_oObj.PropGroupBegin("Statistics", "Important runtime statistics that provide feedback on important simulation parameters.");	//###OBS???
//		_oObj.PropAdd(EFluid.VertsNow,				"# Verts",		0,		0,		65532,	"Number of vertices currently used to render the mesh.", CProp.ReadOnly);		//###DESIGN: Remove??  To new stat system??
//		_oObj.PropAdd(EFluid.TrisNow,				"# Triangles",	0,		0,		65532,	"Number of triangles currently used to render the mesh.", CProp.ReadOnly);
//		_oObj.PropAdd(EFluid.ErrorOutOfVoxels,		"# Vox Over",	0,		0,		1000000,"ERROR: Out of voxels. Fluid mesh renderer will not be able to scan the fluid over its bounds.\n(Mesh will be incomplete)  To prevent this, keep all fluid particles in a smaller area, increase the ability to destroy faraway particles, or increase 'VoxelSize'.  Number gives the number of voxels that had to be skipped.", CProp.ReadOnly);

//		_oObj.PropAdd(EFluid.FluidPolygonizerPresent,	"Polygonizer Present",	0, "Set to true when our advanced Fluid polygonizer is able to run.  Disabled if your computer could not compile our OpenCL polygonization code.  If set to false please send the developers the 'Log-ErosEngineDll.txt file so we can fix the code to work on your particular video card.", CProp.ReadOnly);
//		_oObj.FinishInitialization();
//	}

//	public void OnStart() {									//###DESIGN: Call from central game??
//		Object_GoOnline();
//	}

//	public void OnDestroy() {					//###CHECK?  Destroying object multiple times. Don't use DestroyImmediate on the same object in OnDisable or OnDestroy.
//		Object_GoOffline();
//		if (CGame.INSTANCE._oFluid != null)				//###IMPROVE: Consider using enable / disable instead?
//			CGame.INSTANCE._oFluid = null;
//		if (_oObj._hObject != IntPtr.Zero)
//			ErosEngine.Fluid_Destroy(_oObj._hObject);
//		_oObj.OnDestroy();			//###CHECK: Sufficient late for destroying emitters??
//	}


//	//--------------------------------------------------------------------------	Server Object Init / Destroy

//	void Object_GoOnline() {		//###WEAK: Always called twice at init... (because of 'OnReset' and OnStart())

//		_nMaxFluidParticles = (int)_oObj.PropGet(EFluid.MaxFluidParticles);

//		_memParticles.Allocate(_nMaxFluidParticles);
//		for (int nPar = 0; nPar < _nMaxFluidParticles; nPar++) {
//			UnityEngine.ParticleSystem.Particle oPar = _memParticles.L[nPar];		//###LEARN: Color must be set or we see nothing... we can 'hide' end-of-life particles gradually by setting these alphas to zeros!
//			oPar.color = Color.white;			//###IMPROVE!!!: Set particle size relative to PhysX!!!
//			oPar.size = .5f;					// Important particle size set by CProp later in init cycle.
//			//oPar.size = 0.05f;				//###TEMP!  _nSizeParticles * 3;				// Must be set or we see nothing... Setting very high so we just set all particles with renderer max particle size
//			_memParticles.L[nPar] = oPar;		//###LEARN: Needed (would not have set array data otherwise)
//		}

//		ErosEngine.Object_GoOnline(_oObj._hObject, _memParticles.P);

//		if (_oObj.PropGet(EFluid.FluidPolygonizerPresent) == 0) {			// Disable polygonization renderer if C++ reports it could not initialize (expensive) OpenCL-based fluid renderer
//			//###IMPROVE: Alert user? Debug.LogError("Fluid polygonization is disabled because of OpenCL error: switching to low-quality fluid renderer.  Please send 'Log-ErosEngineDll.txt'");
//			_oObj.PropSet(EFluid.FluidMeshOpacity, 0.0f);		// Disable polygonization
//			_oObj.PropSet(EFluid.ParticlesOpacity, 0.5f);		// Enable low-quality Shuriken renderer
//		}
//		UpdateParticleSize();			// Manually update particle size at init because notification is disabled during property addition.
//	}

//	void Object_GoOffline() {
//		ErosEngine.Object_GoOffline(_oObj._hObject);
//		_memParticles.ReleaseGlobalHandles();
//	}





//	//--------------------------------------------------------------------------	Update

//	public void OnSimulatePre() {	//###DESIGN!!!!: Sim time in C++!!!!!		//###DESIGN!!! ###BUG!!!: Should not invoke Polygonize if fluid paused??? (Left on for debugging...)
//		_oObj.OnSimulatePre();

//		//=== Update Shuriken billboard particles from the shared memory arrays that C++ dll updated for us this physics time step ===
//		if (_oObj.PropGet(EFluid.ParticlesOpacity) != 0.0f) {
//			_oShuriken.GetComponent<Renderer>().enabled = true;
//			_oShuriken.SetParticles(_memParticles.L, _memParticles.L.Length);//, _nMaxFluidParticles);	//###CHECK?
//		} else {
//			_oShuriken.GetComponent<Renderer>().enabled = false;
//		}

//		//=== Update the polygonized mesh by calling its (expensive) mesh creation functions ===
//		if (_oObj.PropGet(EFluid.FluidMeshOpacity) != 0.0f) {

//			int nNumParticles = ErosEngine.Fluid_Polygonize(_oObj._hObject);		//###IMPROVE!!! Multi-threaded!!!

//			if (nNumParticles < 0) {
//				Debug.LogError(string.Format("ERROR {0} with OpenCL Fluid Polygonization.  Forcing fluid reset to prevent crash.", nNumParticles));
//				CGame.INSTANCE.Cum_Stop();
//				return;
//			}

//			//if (_bWorkerThreadRunning == false) {				// In one frame we either setup a new polygonize or we process the result of one that has finished.  This way we remain non blocking and very demanding polygonization doesn't slow down the game (some _nFpsFrames show the last polygonization results)
//			//	int nErrorVal = CGame.FluidRenderer_Polygonize_ThreadStart(_oObj._hObject);
//			//	if (nErrorVal == 0) {
//			//		_bWorkerThreadRunning = true;
//			//		Debug.Log("Started");
//			//	}
//			//	nNumParticles = 0;
//			//} else {
//			//	int nErrorVal = CGame.FluidRenderer_Polygonize_IsWorkerThreadDone(_oObj._hObject);
//			//	if (nErrorVal >= 0) {				// Greater or equal to zero means worker thread is done thread with num particles in value
//			//		nNumParticles = (uint)nErrorVal;
//			//		_bWorkerThreadRunning = false;
//			//		Debug.Log("Ended");
//			//	}
//			//}
			
//			if (nNumParticles > 0) {										// Mesh exists.  Fluid_Polygonize has therefore returned # of triangles to allocate in public property 'TrisNow'

//				//=== Obtain properly-sized triangle array ===
//				_memTrisFluid.ReleaseGlobalHandles();
//				int nTriIndices = (int)_oObj.PropGet(EFluid.TrisNow) * 3;

//				if (nTriIndices > 0) {
//					_memTrisFluid.Allocate(nTriIndices);
//					IntPtr hStringIntPtr = ErosEngine.Fluid_Polygonize_GetTriangles(_oObj._hObject, _memTrisFluid.P, nTriIndices);
//					CGame.SetGuiMessage(EGameGuiMsg.FluidPolygonize, Marshal.PtrToStringAnsi(hStringIntPtr));

//					//=== Obtain properly-sized vert array ===
//					_memVertsFluid.ReleaseGlobalHandles();
//					int nVerts = (int)_oObj.PropGet(EFluid.VertsNow);
//					_memVertsFluid.Allocate(nVerts);
//					ErosEngine.Fluid_Polygonize_GetVertices(_oObj._hObject, _memVertsFluid.P, nVerts);

//					//=== Sets the polygonized fluid mesh from the just-updated mesh from Marching Cubes OpenCL algorithm ===
//					_oMeshRender.Clear(false);						//###CHECK: This helping or not??
//					_oMeshRender.vertices = _memVertsFluid.L;		//###LEARN: Needed every frame...  ###LEARN: Reducing vert count very important!  Reduces cost of nearly everthing!
//					_oMeshRender.uv = new Vector2[_memVertsFluid.L.Length];		//###LEARN: Some cost to that!  Not free!		###HACK!!!!!
//					_oMeshRender.triangles = _memTrisFluid.L;		//###OPT!!! ###CHECK: Faster to 'trim' the array everyframe???
//					_oMeshRender.RecalculateNormals();		//###NOTE: Very important service Unity renders in this case as throughout the OpenCL voxelization code we stripped out normal calculations to be done at the very end... here!
//					//if ((Time.frameCount % 90) == 0)		//###OPT!!!!! Do extra verts slow down recalc normals???  FIND OUT!!!!
//					//_oMeshRender.RecalculateBounds();		//###LEARN: Very expensive call!  Always fetch bounds when we have them!  Gets really slow with lots of verts!

//					_oFluidMeshRend.enabled = true;
//				}

//			} else {
//				_oFluidMeshRend.enabled = false;			//###CHECK: Unhide when content is there!
//			}
//			CGame.SetGuiMessage(EGameGuiMsg.FluidSimulation, string.Format("Particles: {0} / {1} = {2:F1}%  OverflowErrors: {3}", nNumParticles, _nMaxFluidParticles, 100 * nNumParticles / _nMaxFluidParticles, CGame.INSTANCE._nFluidParticleRemovedByOverflow_HACK));
//		} else {
//			_oFluidMeshRend.enabled = false;
//		}
//	}



//	//--------------------------------------------------------------------------	Property OnPropSet_ notification events: Automatically called by CProp implementation when properties of the same name change.

//	public void OnPropSet_NeedReset(CProp oProp, float nValueOld, float nValueNew) {			// Called when a property created with the 'NeedReset' flag gets changed so owning object can adjust its global state
//		//Debug.Log("FLUID RESET");
//		ResetFluid();
//	}

//	public void OnPropSet_FluidMeshOpacity(float nValueOld, float nValueNew) {
//        //###BROKEN
//		//_oMatFluidMesh.shader = Shader.Find((nValueNew == 1.0f) ? "Diffuse" : "Transparent/Diffuse");			// Switch shader to diffuse if opacity is 1 (Transparent shader at 1 opacity still has that bad rendering problem with additive render of back parts of the mesh)
//		//Color col = _oMatFluidMesh.color;								// Also change the mesh opacity on demand.
//		//col.a = nValueNew;												//###IMPROVE: Solid material when 1!
//		//_oMatFluidMesh.color = col;
//		//if ((nValueOld == 0 && nValueNew != 0) || (nValueOld != 0 && nValueNew == 0)) 							// If fluid mesh polygonization has just become enabled or disabled, perform a full reset to allow server to initialize or destroy the large amout of mesh renderer code
//		//	ResetFluid();
//	}

//	public void OnPropSet_ParticlesOpacity(float nValueOld, float nValueNew) {		//###TODO
//	}

//	public void OnPropSet_Stiffness(float nValueOld, float nValueNew) {				// There appears to be a bug with PhysX that when setting stiffness with CPU-simulated fluid that setting doesn't take without reset...  Trap and reroute here
//		if (_oObj.PropGet(EFluid.Fluid_GPU) == 0)
//			ResetFluid();
//	}

//	public void OnPropSet_ParticleSize(float nValueOld, float nValueNew) { UpdateParticleSize(); }
//	public void OnPropSet_ParticleSizeRenderRatio(float nValueOld, float nValueNew) { UpdateParticleSize(); }

	

//	//--------------------------------------------------------------------------	Utility

//	void UpdateParticleSize() {							// Iterate through the Shuriken particle array to set the particle size		//###IMPROVE: Consider doing in C++??
//		if (_memParticles.L == null)					// This function is called when properties changed that effect particle rendering.  Ignore notification that have come too early (when properties are added at init)
//			return;
//		float nSizeSimulated	= _oObj.PropGet(EFluid.ParticleSize);
//		float nSizeRenderOffset = _oObj.PropGet(EFluid.ParticleSizeRenderRatio);
//		float nSizeRender = nSizeSimulated * nSizeRenderOffset;
//		for (int nPar = 0; nPar < _nMaxFluidParticles; nPar++)
//			_memParticles.L[nPar].size = nSizeRender;
//	}

//	public void ResetFluid() {			// Changing particle count requires a full shutdown and rebuild.
//		int nMaxFluidParticles = (int)_oObj.PropGet(EFluid.MaxFluidParticles);	//###WEAK!!!!: Fully teardown and rebuild called from all kinds of contexts like pose load... Make more efficient!
//		Debug.Log("Reseting fluid with max particles = " + nMaxFluidParticles);
//		Object_GoOffline();
//		_oObj.PropSet(EFluid.EmitRate, 0);				//###CHECK: Breaks anything??
//		_nMaxFluidParticles = nMaxFluidParticles;
//		Object_GoOnline();												//###IMPROVE: Test some properties that are known to get out of wack and adjust them here??
//	}
//}


////Transform oFluidShurikenRenderer = Camera.main.transform.FindChild("FluidShurikenRenderer");
////if ((CGame.INSTANCE._nFrameCount_MainUpdate % 30) == 0) {		//###IMPROVE: Provide service by global app to service these 'not-every-frame' functionality.
