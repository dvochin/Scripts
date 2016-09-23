/*# New DAZ3D mesh based on G3F:
- Bones are inverted!  Invert verts in gBlender C?  (Set vert in Blender to see)
- Actors are pulling from wrong bones... redo the whole lot
- Broke head look, head, hair
- Missing texture on eyes.
-###LEARN: Unity is a 'left handed coordinate system' while Blender is 'right handed' = unfortunate!!


- IDEA: Have colliders be defined in Blender and serialized during

=== TODO ===
- Blender breast needs to be separated during original import method?
- What to do about Flex collider?

- Need to create Unity bones in Editor from scratch.
- Q: Do we define colliders in Unity or Blender??
- Need to change the actors to add D6 joints that are appropriate
- We need to create a BodyA, BodyB, BodyC collider groups
- Previous implementation reversed bones to have chest as root.  Can we adapt DAZ's bone structure?
- We'll need to re-calibrate each bone movement again
- Q: How about hands: Do we go with intricate bones or softbody the whole hand?
- Need to remove existing face implementation and re-implement facial expressions by driving bones!
- Need to centralize all modifications applied to incoming DAZ body in one function:
    - We need to keep a vanilla DAZ body in main Blender file.  This is where our custom vertex groups go... morphed body inputs have no custom groups!
    - Glue vagina to base mesh
    - Add geometry to breasts
    - Move penis to position and morph base for seamless connection.


- PROBLEM: Right thigh twist is out of position!
- PROBLEM: Vagina mesh is shipped as 2nd mesh!  Also it does not coincide perfectly on some morphs!

- AFTER: One new DAZ body works perfectly, go back to vagina creation and CSoftBodySkin!

*/
/*###DISCUSSION: Panels


=== LAST ===
- Would be nice to have SB auto-reset during teleports!


- Problem with depth of cursor and GUI... when far we get greater depth... Is this a problem between the overlay cam and main one?  (Or just for VR / Space Navigator??)
-###IMPROVE: Add various 'cursor depth traps' around major body parts so cursor has a max depth?
   - Or... use body PhysX colliders?  BETTER! -> map to cursors??


- Rethink anchor points now that we can clip a little bit.
- Can't select a widget without unselecting first one
- Move canvas center or owning pin?
   - Finalize pin
- Cleanup panel creation
- Panel center mesh
- Panel divider sucks
- Slider grabber bigger


- Mesh show / hide shows extra softbodies, rim, collider body, sb skinned, etc
- Why are pins so small now?
- Cursor hit buggy as shit... redo!

=== NEXT ===

=== TODO ===

=== LATER ===

=== IMPROVE ===

=== DESIGN ===

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===

=== PROBLEMS??? ===

=== WISHLIST ===
- Have button to zero VR camera
- Panel auto-stacking is 'slots'

*/


using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using UnityEngine.UI;


//---------------------------------------------------------------------------	
public class CGame : MonoBehaviour, IObject, IHotSpotMgr {	// The singleton game object.  Accessable via anywhere via 'INSTANCE'.  Provides top-level game init/shutdown and initiates the 'heartbeat' calls that make the game progres frame by frame

    //---------------------------------------------------------------------------	TEMP Flex
    public bool ShowPresentation = true;            //###IMPROVE: Add softbody shapes!
    public bool ShowPhysxColliders = false;
    public bool ShowMeshStartup = false;
    public bool ShowPinningRims = false;
    public bool ShowFlexSkinned = false;
    public bool ShowFlexColliders = false;
    public bool ShowFlexParticles = false;
    public bool EnableIdlePoseMovement = true;          // Small adjustments applied to all poses (to promote a less robotic still appearance)

    //public bool showFlexParticles = true;
    public float particleSpacing = 0.02f;               // 20mm = thickness of a finger = smallest object we need to collide with.
    public float volumeSampling = 1;                    // 1 = Volume spacing at particle spacing, 2 = rich inner volume defintion, 0.01 = very sparse inner definition.  Huge influence!
    public float surfaceSampling = 1;                   // Not much effect on surface defintion.  .001 = 132 par 5 = 183 par
    public float clusterSpacingMult = 1.6f;             // Hugely important and dictates # of bones.  Need to keep this yield desired bone numbers and adjust RadiusMult for desired effect  1=175 particles fall apart  1.3=93 Almost holds together  1.5 mostly holds together  2=41 Mostly together with some breaks  2.5 about right  3=12  5.0 looks stiff  8 crashes  (The spacing for shape-matching clusters, should be at least the particle spacing)
    public float clusterRadiusMult = 2.0f;              // Hugely important.  Once SpacingMult set for # of bones we want we adjust this to keep things together!  Starts looking very good at 2.0  Stiffens as we increase and probably more expensive  (Controls the overall size of the clusters, this controls how much overlap the clusters have which affects how smooth the final deformation is, if parts of the body are detaching then it means the clusters are not overlapping sufficiently to form a fully connected set of clusters)
    public float clusterStiffness = 0.10f;              // Hugely important.  0.1 = Very slow restitution  0.5 = Still too slow  0.75 = Stiff as possible no shimmer 0.8 = Minor shimmer  0.89 = Major shimmer 1.0 blows up
    public float linkRadiusMult = 0;                    // Creates extra springs... they don't appear to do much and just add overhead! (Any particles below this distance will have additional distance constraints created between them)
    public float linkStiffness = 0.0f;                  // (The stiffness of distance links)
    public float skinFalloff = 1.0f;                    // (The speed at which the bone's influence on a vertex falls off with distance)
    public float skinMaxDistMult = 6.0f;                // (The maximum distance a bone can be from a vertex before it will not influence it any more)
    public float nDistParticlesFromBackmeshMult = 0.8f;
    public float nSoftBodyFlexColliderShrinkRatio = 0.10f;    // Percentage of particle spacing that will 'shrink' softbody flex colliders so resultant appearance mesh appears to collide closer than technology allows.
    public float nMassSoftBody = 2.0f;              //###TUNE
    public float nMassCloth = 1.0f;

    public float stretchStiffness = 1.0f;
    public float bendStiffness = 1.0f;
    public float tetherStiffness = 0.0f;
    public float tetherGive = 0.0f;

    public bool BoneDebugMode = true;                   // All joint drivers limits are set to be as big as possible when this is set.  Used for runtime bone angle tuning
    public float BoneDriveStrength = 0.1f;				// The default angular drive for all bones.  Multiplied by a per-bone multiplier.  Hugely important.  ###TUNE

    //---------------------------------------------------------------------------	VISIBLE GAME OPTIONS
    public bool			_DemoVersion;		// When defined, the app builds for demo mode (no penetration)		###MOVE!
						public	EGameModes	_GameModeAtStartup = EGameModes.Configure;
						public	EGameModes	_GameMode = EGameModes.None;
						public 	int 		_TargetFrameRate = 25;
						public float		_nAnimMult_Time = 1;
						public float		_nAnimMult_Pos = 1;
						public AnimationCurve CurveEjaculateMan;	// Designer-adjustable curves to adjust per-frame fluid properties to simulate man/woman ejaculation
						public AnimationCurve CurveEjaculateWoman;
						public	bool		_ShowSysInfo;			// When true shows the system info messages at upper left of screen
						public	bool		_ShowFPS;				// When true shows the frame per second stats

	//---------------------------------------------------------------------------	IMPORTANT MEMBERS

	[HideInInspector]	public static CGame	INSTANCE = null;			// The one and only game INSTANCE.. The only way to access most global objects from the entire app
	[HideInInspector]	public System.Diagnostics.Process	_oProcessBlender;		// The very important Blender process instance we create / manage / destroy.  Must run for game to be functional!!
	
	[HideInInspector] 	public	CCursor			_oCursor;			// The one-and-only CCursor INSTANCE for the game.
	[HideInInspector] 	public	List<WeakReference>	_aKeyHooks = new List<WeakReference>();		// The list of globally-registered keyboard hooks.  ###IMPROVE?  Change to a map by keycode so we can trap multiple assignemnts??
	//[HideInInspector]	public	CFluid			_oFluid;			// The one and only Fluid object the app can have.  (Multiple fluids can't collide with one another)

	[HideInInspector]	public	CCamTarget		_oCamTarget;					// The one global camera target focus point.  Camera position determined by PhysX through configurable CCamMagnet spring joints pulling toward configurable points of interest

	[HideInInspector]	public 	CScriptRecord	_oScriptRecordUserActions;

	[HideInInspector]	public 	CPoseRoot		_oPoseRoot;

	[HideInInspector]	public 	static CCollider[]	s_aColliders_Scene;		// List of CCollider components that are children of the 'SceneColliders' top node.  Pulled in at init

	//---------------------------------------------------------------------------	CONFIGURATION
	
	[HideInInspector] 	public 	float			_DefaultJointSpring  = 2.0f;		//###OBS??
	[HideInInspector] 	public 	float			_DefaultJointSpringOld;
	[HideInInspector] 	public 	float			_DefaultJointDamping = 0.15f;
	[HideInInspector] 	public 	float			_DefaultJointDampingOld;
	
	//---------------------------------------------------------------------------	MISC
	[HideInInspector]	public	string[]		_aGuiMessages;
	[HideInInspector]	public	bool			_bGameModeBasicInteractions;		// When in basic interaction mode game is not showing the hotspots and editing is limited (to be more 'play like' = normal play mode)

	[HideInInspector]	public	int				_nSelectedBody;
	[HideInInspector]	public	int				_nNumPenisInScene_BROKEN;
	[HideInInspector]	public	bool			_GameIsRunning = false;			// When true all FixedUpdate() functions of any object can / should update themselves with FastPhysics (e.g. kinematic colliders)  //###DESIGN: Can get rid of this flag and just test for CGame.enabled??
	[HideInInspector]	public	int				_nFluidParticleRemovedByOverflow_HACK;
	[HideInInspector]	public	uint			_nFrameCount_MainUpdate;		// Number of calls to 'FixedUpdate()'  Used to efficiently perform tasks that don't need to run every frame.  ###DESIGN: Keep???
	[HideInInspector]	public 	System.Random _oRnd = new System.Random(1234);
	[HideInInspector]	public	float			_nTimeStartOfCumming;
	[HideInInspector]	public	bool			_bKeyShift, _bKeyControl, _bKeyAlt;
	[HideInInspector]	public	bool			_bMouseBtnRight;		//###IMPROVE: Other buttons?
	[HideInInspector]	public	bool			_bCursorEnabled_HACK = true;	//###HACK Temp disabling of our 3D cursor in situations where we display a full screen GUI (pic laoders)
	[HideInInspector]	public	bool			_bRunningInEditor;				//###HACK Game running in editor is configured for development
	[HideInInspector]	public	GameObject		_oSceneMeshesGO;                // The 'game scene' that is the current 'eye candy' room purely for visuals.  Stored to hide / show
    [HideInInspector]	public	Text            _oTextUL, _oTextUC, _oTextUR;       // Access members to GUI text fields ###MOVE???
    //---------------------------------------------------------------------------	FPS CALC
    const float         C_TimeBetweenMinCalc = 5.0f;
    float               _nFpsUpdateInterval = 0.5F;
    float               _nFpsAccum = 0;				// FPS accumulated over the interval
    int                 _nFpsFrames = 0;				// Frames drawn over the interval
    float               _nFpsTimeLeft;				// Left time for current interval
    float               _nFpsTimeLeftUntilNextReset;
    float               _nMinFpsPrevious = 25, _nMinFpsNow;
    //int                 _nFpsCollectionCount;							//###IMPROVE: Use / display this to decrease GC calls!
    string              _sFPS;
	public 	CHotSpot			_oHotSpot;				// Hotspot at the head of the character.  Enables user to change the important body properties by right-clicking on the head

    //---------------------------------------------------------------------------	CONSTANTS		###MOVE???

    [HideInInspector]	public const string		C_RelPath_Textures = "/Unity/Assets/Resources/";
	[HideInInspector]	public const int		C_PropAutoUpdatePeriod	= 100;			// Number of _nFpsFrames between CObject property auto refresh.
	[HideInInspector]	public const float		C_BodySeparationAtStart = 0.0f;		//###TUNE ####DISABLED ####DESIGN: Revisit this?
	
	[HideInInspector]	public static Quaternion s_quatRotOffset90degUp   = Quaternion.FromToRotation(Vector3.forward, Vector3.up);		// Used to rotate capsules as PhysX insists on receiving them with Y-axis their longer side ('height')
	[HideInInspector]	public static Quaternion s_quatRotOffset90degDown = Quaternion.FromToRotation(Vector3.up, Vector3.forward);

	[HideInInspector]	public static Bounds	_oBoundsInfinite = new Bounds(Vector3.zero, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));	// Infinite bounds to prevent having to recalc bounds on dynamically updating meshes (such as softbodies and clothing)

	IntPtr _hWnd_Unity;                 // HWND window handle of the Unity window.  Used to restore activation back to it because of Blender window activation (without this user would have to alt-tab!)

    //---------------------------------------------------------------------------	MOVED FROM CGamePlay.  ###MOVE
	public 	CObject			_oObj;							// The user-configurable object representing the game
	public	CBody[]			_aBodies = new CBody[2];		// Our collection of bodies.  Created from the specs in corresponding entry in _aObjBodyDefs.
	public	float			_nTimeReenableCollisions;		// Time when collisions should be re-enabled (Temporarily disabled during pose load)

	const float				C_TimeToMaxOrgasm_OBSOLETE = 20;			// How many seconds it takes to reach maximum lust if maximum pleasure is always on	###TUNE

	public  bool			_bPenisInVagina;			//###DESIGN!!: ###MOVE?? Belongs here?? Prevents penis from generating dynamic CBodyCol colliders to repell it.  Set when entering vagina

	public string			_sNameScenePose;				// The currently loaded scene pose
	public bool				_bScenePoseFlipped;             // If set the scene pose is 'flipped' (i.e. Pose for body a loaded into body b and vice versa)
    public uFlex.FlexSolver _oFlexSolver;                //###MOD
    public bool             _bBodiesAreKinematic;        // When set propert sets to pose nodes will also directly move the attached PhysX object (used during pose loading)

    [HideInInspector] public float _nTimeAtStart;           // Used to determine how much time it takes to init

    #region === INIT
    public void Awake() {
        StartCoroutine(Coroutine_StartGame());			// Handled by a coroutine so that our 'OnGui' can run to update the 'Please wait' dialog
    }
    public IEnumerator Coroutine_StartGame() {		//####OBS: IEnumerator?? //###NOTE: Game is started by iGUICode_Root once it has completely initialized (so as to present the 'Please Wait...' dialog
		Debug.Log("=== CGame.StartGame() ===");
        INSTANCE = this;
        _nTimeAtStart = Time.time;
        _oFlexSolver = FindObjectOfType<uFlex.FlexSolver>();        //###F
        GameObject oSceneGO = GameObject.Find("SCENE/SceneColliders");
        if (oSceneGO != null) 
            s_aColliders_Scene = oSceneGO.GetComponentsInChildren<CCollider>();

        _bRunningInEditor = true;       //###HACK ####REVA Application.isEditor
        _DemoVersion = (_bRunningInEditor == false);		//###CHECK? If dev has Unity code they are non-demo

        _oCursor = CCursor.Cursor_Create();             //###DESIGN!!!!!: REVISIT!	###CLEANUP!!!!!
        Cursor.visible = Application.isEditor;      // _bRunningInEditor;

        //=== Set rapid-access members to text widgets so we can rapidly update them ===
        _oTextUL = GameObject.Find("/UI/CanvasScreen/UL/Text-UL").GetComponent<Text>();
        _oTextUC = GameObject.Find("/UI/CanvasScreen/UC/Text-UC").GetComponent<Text>();
        _oTextUR = GameObject.Find("/UI/CanvasScreen/UR/Text-UR").GetComponent<Text>();

		//=== Create user-adjustable top-level game options ===
		_oObj = new CObject(this, 0, typeof(EGamePlay), "Erotic9");		//###TEMP!!! Main game name in this low-importance GUI???
		_oObj.PropGroupBegin("", "", true);
		_oObj.PropAdd(EGamePlay.Pleasure,			"Pleasure",		30,		-100,	100,	"Amount of pleasure experienced by game characters.  Influences 'Arousal' (NOTE: Temporary game mechanism)", CProp.Local);	//###BUG with first setting
		_oObj.PropAdd(EGamePlay.Arousal,			"Arousal",		0,		0,		100,	"Current state of arousal from game characters.  Currently influence penis size.  (NOTE: Temporary game mechanism)", CProp.Local);
		_oObj.PropAdd(EGamePlay.PoseRootPos,		"Pose Root Position",typeof(EPoseRootPos), 0,	"Base location of pose root.  (e.g. on bed, by bedside, etc)", CProp.Local);
		_oObj.PropAdd(EGamePlay.PenisSize,			"Penis Size",	0,		0,		100,	"", CProp.Local | CProp.ReadOnly | CProp.Hide);
		_oObj.PropAdd(EGamePlay.PenisErectionMax,	"Erection",		0,		0,		100,	"", CProp.Local | CProp.ReadOnly | CProp.Hide);
		_oObj.PropAdd(EGamePlay.FluidConfig,		"Fluid Configuration", 0, "Display the properties of the Erotic9 fluid simulator.  (Advanced)", CProp.Local | CProp.AsButton);
		_oObj.FinishInitialization();
        _oHotSpot = CHotSpot.CreateHotspot(this, transform, "Game Options", false, new Vector3(0, 0.0f, 0.0f), 1.0f);

        //if (_GameModeAtStartup == EGameModes.None) { 
        //    yield break;
        //}


		float nDelayForGuiCatchup = _bRunningInEditor ? 0.2f : 0.01f;		//###HACK? ###TUNE: Adjustable delay to give iGUI time to update 'Game is Loading' message, with some extra time inserted to make Unity editor appear more responsive during game awake time
		yield return new WaitForSeconds(nDelayForGuiCatchup);

		//=== Send async call to authentication so it is ready by the time game has initialized ===
//		WWW oWWW = null;
//		if (Application.genuine) {		//###CHECK: Has any value against piracy???
//			if (Application.internetReachability != NetworkReachability.NotReachable) {		//###CHECK!!!
//				//####BROKEN?! Why store it if we don't use it? string sMachineID = PlayerPrefs.GetString(G.C_PlayerPref_MachineID);
//				string sMachineID = CGame.GetMachineID();		//###CHECK Can cause problems if switching adaptors frequently?
//				oWWW = new WWW("http://www.erotic9.net/cgi-bin/CheckUser.py?Action=Authenticate&MachineID=" + sMachineID);
//			} else {
//				Debug.LogError("Warning: Could not authenticate because of Internet unreacheability.");
//			}
//		} else {
//			Debug.LogError("Warning: Could not authenticate because of executable image corruption.");
//		}

		//=== Try to load our dll to extract helpful error message if it fails, then release it ===
		//Debug.Log("INIT: Attempting to load ErosEngine.dll");         //###BROKEN!  WTF No longer works loading 64 bit dll??
		//int hLoadLibResult = LoadLibraryEx("ErosEngine.dll", 0, 2);
		//if (hLoadLibResult > 32)
		//	FreeLibrary(hLoadLibResult);			// Free our dll so Unity can load it its way.  Based on code sample at http://support.microsoft.com/kb/142814
		//else 			
		//	throw new CException("ERROR: Failure to load ErosEngine.dll.  Error code = " + hLoadLibResult);		// App unusable.  Study return code to find out what is wrong.
		//Debug.Log("INIT: Succeeded in loading ErosEngine.dll");
				
		//####OBS? GameObject oGuiGO = GameObject.Find("iGUI");			//###TODO!!! Update game load status... ###IMPROVE: Async load so OnGUI gets called???  (Big hassle for that!)
		Debug.Log("0. Game Awake");	yield return new WaitForSeconds(nDelayForGuiCatchup);
		int n123 = ErosEngine.Utility_Test_Return123_HACK();			// Dummy call just to see if DLL will load with Unity
        if (n123 != 123)
            throw new CException("ERROR: Failure to get 123 from ErosEngine.dll call to Utility_Test_Return123_HACK.");
        Debug.Log("INIT: Succeeded in loading ErosEngine.dll");

        //=== Initialize our gBlender direct-memory buffers ===
        Debug.Log("1. Shared Memory Creation.");  //###???  new WaitForSeconds(nDelayForGuiCatchup);
		if (ErosEngine.gBL_Init(CGame.GetFolderPathRuntime()) == false)
			throw new CException("ERROR: Could not start gBlender library!  Game unusable.");
		
		//=== Spawn Blender process ===
		Debug.Log("2. Background Server Start.");  //###???  new WaitForSeconds(nDelayForGuiCatchup);	//###CHECK: Cannot wait long!!
		_hWnd_Unity = (IntPtr)GetActiveWindow();			// Just before we start Blender obtain the HWND of our Unity editor / player window.  We will need this to re-activate our window.  (Starting blender causes it to activate and would require user to alt-tab back to game!!)
		_oProcessBlender = CGame.LaunchProcessBlender("Erotic9.blend");
		if (_oProcessBlender == null)
			throw new CException("ERROR: Could not start Blender!  Game unusable.");
        //_nWnd_Blender_HACK = (IntPtr)GetActiveWindow();

        //=== Start Blender (and our gBlender scripts).  Game cannot run without them ===
        Debug.Log("3. Client / Server Handshake.");  //###???  new WaitForSeconds(nDelayForGuiCatchup);
		if (ErosEngine.gBL_HandshakeBlender() == false)
			throw new CException("ERROR: Could not handshake with Blender!  Game unusable.");

        SetForegroundWindow(_hWnd_Unity);           // Set our editor / player back into focus (away from just-spawned Blender)

        //=== Set Blender global variables ===
        CGame.gBL_SendCmd("G", "CGlobals.SetFlexParticleSpacing(" + CGame.INSTANCE.particleSpacing.ToString() + ")");         //###TODO: Add others?

        //=== Start PhysX ===
        Debug.Log("4. PhysX3 Init.");  //###???  new WaitForSeconds(nDelayForGuiCatchup);
		ErosEngine.PhysX3_Create();						// *Must* occur before any call to physics library...  So make sure this object is listed with high priority in Unity's "Script Execution Order"

		Debug.Log("5. PhysX2 Init.");  //###???  new WaitForSeconds(nDelayForGuiCatchup);
		ErosEngine.PhysX2_Create();						//###IMPROVE!!! Return argument ###NOTROBUST

		Debug.Log("6. OpenCL Init.");  //###???  new WaitForSeconds(nDelayForGuiCatchup);
//		if (System.Environment.CommandLine.Contains("-DisableOpenCL") == false)	//###TODO More / better command line processing?		//###SOON ####BROKEN!!!!! OpenCL breaks cloth GPU!
//			ErosEngine.MCube_Init();		//###IMPROVE: Log message to user!

		SetForegroundWindow(_hWnd_Unity);			//###WEAK: Can get rid of??

		//=== Start misc stuff ===
		Debug.Log("7. CGame globals.");  //###???  new WaitForSeconds(nDelayForGuiCatchup);
		_oSceneMeshesGO = GameObject.Find("SceneMeshes");			// Remember our scene game object so we can hide/show

		_aGuiMessages = new string[(int)EGameGuiMsg.COUNT];
		_ShowFPS = _ShowSysInfo = _bRunningInEditor;
		
		//###IMPROVE: Disabled until we need to save CPU cycles...  Create upon user demand to record script!
		//_oScriptRecordUserActions = new CScriptRecord(GetPathScript("RecordedScript"), "Automatically-generated Erotic9 Scene Interation Script");

		_oPoseRoot = GameObject.Find("CPoseRoot").GetComponent<CPoseRoot>();
		_oPoseRoot.OnStart();

		//_oFluid = gameObject.AddComponent<CFluid>();
		//_oFluid.OnAwake();

		Application.targetFrameRate = _TargetFrameRate;			//###BUG!!! Why no effect????
		_DefaultJointSpringOld = _DefaultJointSpring;		//###OBS
		_DefaultJointDampingOld = _DefaultJointDamping;

		_oCamTarget = GameObject.Find("CCamTarget").GetComponent<CCamTarget>();		//###WEAK!!!
		_oCamTarget.OnStart();

		Debug.Log("8. Body Assembly.");  //###???  new WaitForSeconds(nDelayForGuiCatchup);		//###WEAK!!!

		//_oFluid.OnStart();								//###CHECK: Keep interleave

		SetGameModeBasicInteractions(true);

		//=== Find the static scene colliders in 'SceneColliders' node and initialize them ===
		//Debug.Log("CGame.StartGame() Registering Static Colliders: " + s_aColliders_Scene.Length);
        if (s_aColliders_Scene != null)
		    foreach (CCollider oColStatic in s_aColliders_Scene)		// Colliders that are marked as static registered themselves to us in their Awake() so we can start and destroy them
			    oColStatic.OnStart();

		//StartCoroutine(Coroutine_Update100ms());
		StartCoroutine(Coroutine_Update500ms());

		_GameIsRunning = true;
		enabled = true;
		Debug.Log("+++ GameIsRunning ++");

		Debug.Log("7. Scene settling time.");  //###???  new WaitForSeconds(2.0f);		//###DESIGN??  ###TUNE??
		///_oGui.ShowSceneBlanker(false);
		///_oGui.ShowPanelGameLoad(false);

		//=== Check result of user authentication ===
		/*
		if (oWWW != null) {
			yield return oWWW;
			string sResultAuth = oWWW.text;
			_DemoVersion = sResultAuth.Contains("Result=OK") == false;		//###IMPROVE: Create parse routine and return server errors
			Debug.Log("Auth Results = " + sResultAuth);
			if (_DemoVersion == false)
				Debug.Log("Starting game in non-demo mode.");
		}*/
		//_DemoVersion = false;
		//if (_DemoVersion)
		//	Debug.Log("Starting game in demo mode.");		//###TODO: Temp caption!

///		if (_bRunningInEditor == false)
///			CUtility.WndPopup_Create(EWndPopupType.LearnToPlay, null, "Online Help", 50, 50);	// Show the help dialog at start... ###BUG: Does not change the combo box at top!

		SetForegroundWindow(_hWnd_Unity);			//###WEAK: Can get rid of??


        //if (CGame.INSTANCE._GameMode == EGameModes.None)        //####TEMP
        //    return;

        //=== Create the body publicly-editable body definitions that can construct and reconstruct CBody instances ===
        //CGame.INSTANCE._nNumPenisInScene_BROKEN = 0;

		//###NOTE: For simplification in pose files we always have two bodies in the scene with man/shemale being body 0 and woman body 1
		CreateBody(0);
  //      //CreateBody(1);
        _aBodies[0].SelectBody();

		TemporarilyDisablePhysicsCollision();

		///SetPenisInVagina(false);		// We initialize the penis in vagina state to false so vagina track colliders don't kick in at scene init

		if (CGame.INSTANCE._GameMode == EGameModes.Play)
			ScenePose_Load("Standing", false);          // Load the default scene pose

        //###TODO ##NOW: Init GUI first!!
        //iGUISmartPrefab_WndPopup.WndPopup_Create(new CObject[] { oObj }, "Game Play", 0, 0);		//###TEMP

        //Time.timeScale = 0.05f;		//###REVA ###TEMP   Gives more time for cloth to settle... but fix it some other way (with far stronger params??)

        ChangeGameMode(_GameModeAtStartup);             // Set the initial game mode as statically requested

        Debug.LogFormat("Time at startup end: {0}", Time.time - _nTimeAtStart);
        //ScenePose_Load("Standing", false);          // Load the default scene pose
    }






    public void OnDestroy() {			//####REV
		Debug.Log("--- CGame.OnDestroy() ---");
		if (INSTANCE != null)					//####CHECK ###TEMP???  ###DESIGN!!!! W
			DestroyGame();
		//Application.Quit();				//###CHECK
		///ErosEngine.Utility_ForceQuit_HACK();			//###HACK!!!!!!	Try to fix app destruction so we can exit without nuking everything!!
	}

	public void DestroyGame() {
		Debug.Log("--- CGame.DestroyGame() ---");

		_GameIsRunning = false;						// Stop running update loop as we're destroying a lot of stuff

        //=== Destory all the static colliders ===
        //####REVA if (s_aColliders_Scene != null)
        //####REVA     foreach (CCollider oColStatic in s_aColliders_Scene)
        //####REVA         DestroyImmediate(oColStatic.gameObject);		//####CHECK

        //=== Destory the PhysX scenes ===
        ErosEngine.PhysX3_Destroy();		//###BUG ###IMPROVE: How can we be sure we're the last thing we do???  Every other object should have destroyed itself and disconnected from FastPhysics first...
		ErosEngine.PhysX2_Destroy();
		
		//=== Destroy Blender ===
        if (_oProcessBlender != null) {
		    try {
			    _oProcessBlender.Kill();				//###HACK!!
		    } catch (Exception e) {
			    Debug.LogException(e);
		    }
		    _oProcessBlender = null;		//###IMPROVE: Save Blender file before exit!  Tell blender to close itself intead of killing it!!!  Wait for its termination??
        }

		INSTANCE = null;
	}

	#endregion	


    #region === UPDATE
    void Update() {
        if (_GameIsRunning == false)                //###OPT: Get check out of update call and run infrequently?	###IMPROVE: Can use enabled/disabled intead???
            return;

        //=== Store global key modifiers & mouse buttons for this game frame for efficiency ===
        _bKeyShift      = Input.GetKey(KeyCode.LeftShift)   || Input.GetKey(KeyCode.RightShift);
        _bKeyControl    = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        _bKeyAlt        = Input.GetKey(KeyCode.LeftAlt)     || Input.GetKey(KeyCode.RightAlt);
        _bMouseBtnRight = Input.GetMouseButton(1);

        //=== Process CKeyHook keys ===		//###WEAK!!: Switch to use input manager??			//###OPT: Possible to do one test before branching out for all keys??		//###LEARN: If in editor, game window *must* have focus for keys to be seen!!
        for (int nKeyHook = _aKeyHooks.Count - 1; nKeyHook >= 0; nKeyHook--) {       // We iterate in reverse order to make it possible to remove while iterating.
            WeakReference oKeyHookRef = _aKeyHooks[nKeyHook];
            if (oKeyHookRef.IsAlive) {
                CKeyHook oKeyHook = oKeyHookRef.Target as CKeyHook;
                oKeyHook.OnUpdate();
            } else {
                _aKeyHooks.RemoveAt(nKeyHook);
            }
        }

        if (Input.GetKeyDown(KeyCode.F1))           //####TEMP?
            ChangeGameMode(EGameModes.Configure);
        if (Input.GetKeyDown(KeyCode.F2))
            ChangeGameMode(EGameModes.Play);

        if (Input.GetKeyDown(KeyCode.F11))			//####TEMP
            HoldSoftBodiesInReset(false);
        if (Input.GetKeyDown(KeyCode.F12))
            HoldSoftBodiesInReset(true);

        if (Input.GetKeyDown(KeyCode.LeftBracket))      //###TEMP
            HideShowMeshes(false);
        if (Input.GetKeyDown(KeyCode.RightBracket))
            HideShowMeshes(true);
        if (Input.GetKeyDown(KeyCode.Backslash))
            HideShowMeshes();

            //=== Process standard keys === ###IMPROVE: Switch them to CKeyHook???
            if (Input.GetKeyDown(KeyCode.CapsLock)) //###IMPROVE?
            SetGameModeBasicInteractions(!_bGameModeBasicInteractions);

        if (Input.GetKeyDown(KeyCode.Alpha1))
            ScenePose_Load("Standing", false);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            ScenePose_Load("Bedfront Fuck", false);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            ScenePose_Load("Bedfront Spread", false);
        if (Input.GetKeyDown(KeyCode.Alpha4))
            ScenePose_Load("Bedside Fuck", false);
        if (Input.GetKeyDown(KeyCode.Alpha5))
            ScenePose_Load("Spreading on bed - backfacing", false);
        if (Input.GetKeyDown(KeyCode.Alpha6))
            ScenePose_Load("Spreading on bed", false);





        if (Input.GetKeyDown(KeyCode.Backslash)) {
            if (_bKeyControl)
                _ShowSysInfo = !_ShowSysInfo;
            else
                _ShowFPS = !_ShowFPS;
        }

        if (Input.GetKeyDown(KeyCode.K) && _bKeyControl)        // Ctrl+K = Disable default-layer colliders, Shift+Ctrl+K = Enable.  Useful for posing calibration
            Physics.IgnoreLayerCollision(0, 0, !_bKeyShift);


        if (Input.GetKeyDown(KeyCode.P)) {   //###IMPROVE: Display notification		###IMPROVE: Auto save in current scene pose with fully usable name!
            string sDateTime = System.DateTime.Now.ToString() + ".png";     //###IMPROVE: Auto hide of GUI
            sDateTime = sDateTime.Replace('/', '-');
            sDateTime = sDateTime.Replace(':', '.');
            string sPathFileCapture = GetPathScreenCaptures() + _sNameScenePose + " - " + sDateTime;
            Debug.Log("Saved screen capture file to " + sPathFileCapture);  //###IMPROVE: Save a .jpg
            Application.CaptureScreenshot(sPathFileCapture);
        }

        if (Input.GetKeyDown(KeyCode.H) && _bKeyControl) {
            //if (_bKeyShift)
            ///iGUICode_Root.INSTANCE._oGuiRoot.enabled = !iGUICode_Root.INSTANCE._oGuiRoot.enabled;	// Shift + Ctrl + H = Hide GUI layer (useful for screen captures)
            //else
            _oSceneMeshesGO.SetActive(!_oSceneMeshesGO.activeSelf);         // Ctrl + H = Hide scene toggle (useful for screen captures)
        }

        //=== User animation speed / position multiplier updates ===
        if (Input.GetKeyDown(KeyCode.W))
            CGame.INSTANCE._nAnimMult_Time += 0.1f;
        if (Input.GetKeyDown(KeyCode.S))
            CGame.INSTANCE._nAnimMult_Time -= 0.1f;
        if (Input.GetKeyDown(KeyCode.A))
            CGame.INSTANCE._nAnimMult_Pos -= 0.1f;
        if (Input.GetKeyDown(KeyCode.D))
            CGame.INSTANCE._nAnimMult_Pos += 0.1f;
        if (Input.GetKeyDown(KeyCode.C)) {                   // C = toggle cum on first character with penis.  Shift+C = toggle cum on first character without penis
            CBody oBodyCum = FindFirstCharacterWithPenis();
            if (CGame.INSTANCE._bKeyShift)      // Toggle the proper ejaculation flag, and always turnning off the other sex
                oBodyCum = GetBodyOther(oBodyCum);
            CBody oBodyOther = GetBodyOther(oBodyCum);
            if (oBodyOther != null)
                oBodyOther.SetIsCumming(false);                 // Always stop other body from cumming...
            if (oBodyCum != null)
                oBodyCum.SetIsCumming(!oBodyCum._bIsCumming);   // And toggle cum on the requested body
            //if (oBodyCum._bIsCumming == false && oBodyOther._bIsCumming == false)   // If both bodies are not cumming stop the flow.
            //    CGame.INSTANCE._oFluid._oObj.PropSet(EFluid.EmitRate, 0);   //###BUG: Will crash if not 2 odies
            CGame.INSTANCE._nTimeStartOfCumming = Time.time;        // Reset the start of cum cycle so we start at the start
        }
        if (Input.GetKeyDown(KeyCode.V))        // V = Stop all cumming and Wipe away cum (reset fluid)
            Cum_Stop();
        //if (Input.GetKeyDown(KeyCode.G))		//###TODO?
        //	_oObj.PropSet(EGamePlay.Pleasure, _oObj.PropGet(EGamePlay.Pleasure) == 0 ? 50 : 0);

        if (Input.GetKeyDown(KeyCode.BackQuote)) {
            ///if (CGame.INSTANCE._bKeyShift)				//###DESIGN: Join under one key?
            ///iGUICode_Root.OnBtnClick_Poses(null);	// Shift+BackQuote = Load Pose
            ///else
            ///iGUICode_Root.OnBtnClick_Scenes(null);	// BackQuote = Load Scene
        }

        if (Input.GetKeyDown(KeyCode.Tab)) {       // Tab = Select other body.  Assumes a 'two body' scene like most of the code.
            if (CGame.INSTANCE._nSelectedBody == 1)
                _aBodies[1].SelectBody();
            else
                _aBodies[0].SelectBody();
        }

        //=== Process scene / pose load and save ===
        if (CGame.INSTANCE._bKeyControl) {
            if (Input.GetKeyDown(KeyCode.Equals)) {
                if (CGame.INSTANCE._bKeyShift)
                    new CDlgPrompter(true, "Scene Save", "Scene Name:", new CDlgPrompter.DelegateOnOk(CDlgPrompter.Delegate_OnDlgPrompterOk_SavePose), _sNameScenePose);
                else
                    new CDlgPrompter(true, "Pose Save", "Pose Name:", new CDlgPrompter.DelegateOnOk(CDlgPrompter.Delegate_OnDlgPrompterOk_SaveScene), CGame.GetSelectedBody()._sNamePose);
            }
        }


        //###BROKEN!!! WTF???  How can this be "null"??? if (_oGamePlay != null)
        //if (_GameMode == EGameModes.Play || _GameMode == EGameModes.Configure)
        //	OnUpdate();

        if (Input.GetKeyDown(KeyCode.G) && _bKeyControl)        // Ctrl+G = Enter game key	//###IMPROVE: Game option!!
            if (_DemoVersion)       //###IMPROVE? This flag the real one for full game?
                new CDlgPrompter(true, "Game Activation", "Enter Game Key:", new CDlgPrompter.DelegateOnOk(CDlgPrompter.Delegate_OnDlgPrompterOk_Activation));  //PlayerPrefs.GetInt(G.C_PlayerPref_GameKey).ToString());		// Retrieve the key from PlayerPref (if set) when dialog control initializes


        //if (Input.GetKeyDown(KeyCode.F3))       //###TEMP!  ###P
        //    SetBodiesAsKinematic(true);
        //if (Input.GetKeyDown(KeyCode.F4))
        //    SetBodiesAsKinematic(false);



        //Screen.showCursor = true;		//###BROKEN: Hide cursor!!			// We hide the hardware cursor at the beginning of every frame.  iGUI-based dialogs that need to show it will show it after (This way we can easily support multiple dialogs)
        if (_bCursorEnabled_HACK)
            _oCursor.OnUpdate_Cursor();                                         // All mouse processing / interactivity handled here




        //=== FPS Calculations ===                                                                                                                                                                //=== FPS calculations ===
        _nFpsTimeLeft -= Time.deltaTime;
        _nFpsTimeLeftUntilNextReset -= Time.deltaTime;
        _nFpsAccum += Time.timeScale / Time.deltaTime;
        ++_nFpsFrames;

        if (_nFpsTimeLeftUntilNextReset < 0.0f) {
        	_nMinFpsPrevious = _nMinFpsNow;
        	_nMinFpsNow = float.MaxValue;
        	_nFpsTimeLeftUntilNextReset = C_TimeBetweenMinCalc;
        }

        // Interval ended - update GUI text and start new interval
        if (_nFpsTimeLeft <= 0.0) {
        	// display two fractional digits (f2 sMsg)
        	float _nFps = _nFpsAccum / _nFpsFrames;
        	//string sMsg = System.string.Format("{0:F1} FPS"/*\nGC Collection Count: {1}"*/, _nFps/*, System.GC.CollectionCount(0) - _nFpsCollectionCount*/);
        	_sFPS = string.Format("FPS: {0:F1} ({1:F1}ms)  MIN: {2:F1} ({3:F1}ms)", _nFps, 1000 / _nFps, _nMinFpsPrevious, 1000 / _nMinFpsPrevious);//, System.GC.CollectionCount(0));
        	//oLabFPS.label.text = sMsg;

        	//if (_nFps < 24)		//####IMPROVE: Use rich text for colors!
        	//	oLabFPS.labelColor = Color.red;
        	//else if (_nFps < 30)
        	//	oLabFPS.labelColor = Color.yellow;
        	//else
        	//	oLabFPS.labelColor = Color.green;
        	_nFpsTimeLeft = _nFpsUpdateInterval;
        	_nFpsAccum = 0.0F;
        	_nFpsFrames = 0;

        	if (_nMinFpsNow > _nFps)
        		_nMinFpsNow = _nFps;
        }

        CGame.INSTANCE._aGuiMessages[(int)EGameGuiMsg.Dev2] = string.Format("Flex: {0:F3}", _oFlexSolver.m_timers.mTotal);       //###IMPROVE: Disable fetch of timers when not displayed!

        DoFixedUpdate();
    }



    void DoFixedUpdate() {				//###F Update or FixedUpdate()??? // The 'heartbeat of the whole game... *Everything* that happens at every frame is directly called from this function in a deterministic manner.  Greatly simplifies the game code!
		if (_GameIsRunning == false)				//###OPT: Get check out of update call and run infrequently?	###IMPROVE: Can use enabled/disabled intead???
			return;

		//=== CGamePlay former OnUpdate ===  ####CLEANUP
		float nTime = Time.time;

		//CGame.SetGuiMessage(EGameGuiMsg.Dev] = Time.deltaTime.ToString();

		////=== Adjust the global lust property.  This will in turn affect most erotic game parameters such as penis size, erection, etc ===
		//float nPleasure = _oObj.PropGet(EGamePlay.Pleasure);
		//if (nPleasure != 0) {
		//	float nOrgasm = _oObj.PropGet(EGamePlay.Arousal);
		//	float nTimeDeltaAsPercentToMaxOrgasm = Time.deltaTime / C_TimeToMaxOrgasm_OBSOLETE;		// How much we can increase lust at this game frame given max pleasure
		//	float nOrgasmIncrease = nTimeDeltaAsPercentToMaxOrgasm * nPleasure;
		//	nOrgasm += nOrgasmIncrease;
		//	_oObj.PropSet(EGamePlay.Arousal, nOrgasm);
		//}

		//=== Re-enable collision temporarily disabled in Pose_Load() if time has elapsed ===
		if (_nTimeReenableCollisions != 0) {				//###IMPROVE: Do this by co-routine instead??
			if (nTime > _nTimeReenableCollisions) {
				Physics.IgnoreLayerCollision(0, 0, false);
				_nTimeReenableCollisions = 0;
			}
		}


   //     //=== Iterate through our PhysX-enabled objects to ask them to prepare themselves for this upcoming frame ===
        foreach (CBody oBody in _aBodies)
			if (oBody != null)
				oBody.OnSimulatePre();

        //=== Simulate PhysX2 for SoftBodies ===
        //ErosEngine.PhysX2_SimulateFrame(Time.deltaTime);		// We simulate PhysX2 scene so that CBreastBase colliders that appear in PhysX3 scene can push away the cloth there with the softbody breast position of this time frame (expensive blocking call) 

        ////=== Iterate through the bodies for any code that must run between PhysX2 and PhysX3 ===
        //foreach (CBody oBody in _aBodies)
        //	if (oBody != null)
        //		oBody.OnSimulateBetweenPhysX23();

        //=== Simulate PhysX3 for everything else (Clothing, Fluid, etc) ===
        //CGame.INSTANCE._nFluidParticleRemovedByOverflow_HACK = ErosEngine.PhysX3_SimulateFrame(Time.deltaTime);		// Simulate PhysX3 scene that includes everything except soft bodies. (expensive blocking call)

        //CGame.INSTANCE._oFluid.OnSimulatePre();			//###MOVE!?!?!

        //###F ###OBS!!!!  ###CLEANUP
        _oFlexSolver.DoFixedUpdate();
        
        ////=== Extract our information from the soft body simulation ===
        foreach (CBody oBody in _aBodies)
            if (oBody != null)
                oBody.OnSimulatePost();

        CGame.INSTANCE._nFrameCount_MainUpdate++;	//###HACK! To stats!
    }

 //   IEnumerator Coroutine_Update100ms() {			// Slow update coroutine to update low-priority stuff.
	//	for (; ; ) {
	//		if (_GameIsRunning) {					//####CHECK?
	//			//if (_oGui != null) {		//###HACK!!!
	//				//////_oGui.oLabGameMode.label.text = _aGuiMessages[(int)EGameGuiMsg.SelectedBody] + " - " + _aGuiMessages[(int)EGameGuiMsg.SelectedBodyAction];
	//			//}
	//		}
	//		yield return new WaitForSeconds(0.1f);
	//	}
	//}
	IEnumerator Coroutine_Update500ms() {			// Very slow update coroutine to update low-priority stuff.
		for (; ; ) {								//###IMPROVE!!! Use these slow calls to update low-priority stuff app-wide!
			if (_GameIsRunning) {					//####CHECK?
				if (_ShowSysInfo) {
				    string sMsgs = _sFPS + "\r\n";              //###IMPROVE: FPS in different text widget
                    sMsgs += Marshal.PtrToStringAnsi(ErosEngine.Dev_GetDevString(0)) + "\r\n";      //###IMPROVE: Combine in C++?
                    sMsgs += Marshal.PtrToStringAnsi(ErosEngine.Dev_GetDevString(1)) + "\r\n";
                    foreach (string sMsg in _aGuiMessages)
						if (sMsg != null && sMsg.Length > 0)
							sMsgs += sMsg + "\r\n";
				    _oTextUL.text = sMsgs;		// Combine all the GUI messages into one string for display in iGUI
                }
				//		SetGuiMessage(EGameGuiMsg.Dev1, Marshal.PtrToStringAnsi(ErosEngine.Dev_GetDevString(0)));
				//		SetGuiMessage(EGameGuiMsg.Dev2, Marshal.PtrToStringAnsi(ErosEngine.Dev_GetDevString(1)));

				//	}
				//	//////_oGui.oTextDevText.enabled = _ShowSysInfo;		//###WEAK!
				//	//////_oGui.oLabFPS.enabled = _ShowFPS;

				//	//if (_oProcessBlender != null) {		//###IMPROVE: Find a way to extract stdout from Blender console!!
				//	//	string sOut = _oProcessBlender.StandardOutput.ReadLine();
				//	//	if (sOut.Length > 0)
				//	//		Debug.Log("B: " + sOut);
				//	//}
				//}
			}
			yield return new WaitForSeconds(0.5f);
		}
	}

	#endregion

	#region === Misc ===
		
	//###LEARN: Coroutine with timed yield.  Use this for update stuff that doesn't need to run everyframe! ###OPT!!!!!
	//IEnumerator Coroutine_DoWindowsMessages() {		//			StartCoroutine(Coroutine_DoWindowsMessages());						//###OPT ###TODO: Many more of these coroutines for low frequency stuff..
	//	while (true) {
	//		yield return new WaitForSeconds(_UpdateGuiPhysX ? 0.04f : 1.0f);
	//	}
	//}

	//###BROKEN! Redo message notification ###DESIGN!!!!
	public delegate IntPtr Phys_OnPhys_Delegate(EPhysDest eDest, EPhysReq eRequest, EPhysType eType, int nID, int nArg, string sMsg);
	public IntPtr Phys_OnPhys(EPhysDest eDest, EPhysReq eRequest, EPhysType eType, int nID, int nArg, string sMsg) {						//###DESIGN!!!!: Callback can have huge problems being called out of context with null or wrong 'this'!!!  Make static???
		//###*OBS??
		Debug.LogError(">OnPhys Req:" + eRequest + "  Type:" + eType + "  ID:" + nID + "  Arg:" + nArg + " for '" + sMsg + "'");		//###TODO: Add 'verbose' debug flag
		
		IntPtr pOut = IntPtr.Zero;

		switch (eDest) {
			case EPhysDest.Manager:
				switch (eRequest) {
					case EPhysReq.Message:
						OnPhys_Message(sMsg);
						break;
					case EPhysReq.Dump:
						Debug.Log("[PhysMsg: '" + sMsg + "']");
						break;
				}
				break;
		}
	
		return pOut;				
	}
	
	void OnPhys_Message(string sMsg) {		//###DESIGN!!: Rethink needs for these messages
		Debug.LogWarning(">OnPhys Msg:" + sMsg);
	}




    //---------------------------------------------------------------------------	HIDE / SHOW

    //void HideOrShowBodyMeshes(bool bShow) {     //###MOVE
    //    foreach (CBody oBody in _aBodies) {
    //        if (oBody == null)
    //            continue;
    //        MeshRenderer[] aMR = oBody._oBodyRootGO.GetComponentsInChildren<MeshRenderer>();
    //        foreach (MeshRenderer oMR in aMR) {
    //            if (oMR.gameObject.name.StartsWith("Hotspot-"))
    //                continue;
    //            if (oMR.gameObject.name.StartsWith("CHandTarget-"))
    //                continue;
    //            oMR.enabled = bShow;
    //        }
    //        SkinnedMeshRenderer[] aSMR = oBody._oBodyRootGO.GetComponentsInChildren<SkinnedMeshRenderer>();
    //        foreach (SkinnedMeshRenderer oSMR in aSMR) {
    //            oSMR.enabled = bShow;
    //        }
    //    }
    //}

    void HideOrShowPhysxColliders(bool bShowPhysxColliders) {      //###MOVE
        ShowPhysxColliders = bShowPhysxColliders;

        foreach (CBody oBody in _aBodies) {
            if (oBody == null)
                continue;
            if (bShowPhysxColliders) {
                Collider[] aCollidersInBody = oBody._oBodyRootGO.GetComponentsInChildren<Collider>();
                foreach (Collider oCollider in aCollidersInBody) {
                    if (oCollider.gameObject.name.StartsWith("Hotspot-"))         //###DUPLICATE ###IMPROVE: To function call or other means?
                        continue;
                    if (oCollider.gameObject.name.StartsWith("CHandTarget-"))       //###IMPROVE: Add another flag for this stuff?
                        continue;
                    if (oCollider.GetType() == typeof(CapsuleCollider)) {
                        CapsuleCollider oColliderCapsule = (CapsuleCollider)oCollider;
                        GameObject oBoneRendererPrefab = Resources.Load("Prefabs/BoneRenderer_Capsule", typeof(GameObject)) as GameObject;
                        GameObject oBoneRendererGO = GameObject.Instantiate(oBoneRendererPrefab) as GameObject;
                        oBoneRendererGO.name = "BoneRenderer";
                        Transform oBoneRendererT = oBoneRendererGO.transform;
                        oBoneRendererT.parent = oCollider.transform;
                        oBoneRendererT.localPosition = new Vector3(oColliderCapsule.center.x, oColliderCapsule.center.y, oColliderCapsule.center.z);
                        oBoneRendererT.localScale = new Vector3(oColliderCapsule.radius * 2, oColliderCapsule.height / 2, oColliderCapsule.radius * 2);
                        switch (oColliderCapsule.direction) {
                            case 0: oBoneRendererT.localRotation = Quaternion.Euler(0, 0, 90); break;
                            case 1: oBoneRendererT.localRotation = Quaternion.Euler(0, 90, 0); break;
                            case 2: oBoneRendererT.localRotation = Quaternion.Euler(90, 0, 0); break;
                            default: throw new CException("Error in bone capsule collider orientation!");
                        }
                    } else if (oCollider.GetType() == typeof(SphereCollider)) {
                        SphereCollider oColliderSphere = (SphereCollider)oCollider;
                        GameObject oBoneRendererPrefab = Resources.Load("Prefabs/BoneRenderer_Sphere", typeof(GameObject)) as GameObject;
                        GameObject oBoneRendererGO = GameObject.Instantiate(oBoneRendererPrefab) as GameObject;
                        oBoneRendererGO.name = "BoneRenderer";
                        Transform oBoneRendererT = oBoneRendererGO.transform;
                        oBoneRendererT.parent = oCollider.transform;
                        oBoneRendererT.localPosition = new Vector3(oColliderSphere.center.x, oColliderSphere.center.y, oColliderSphere.center.z);
                        oBoneRendererT.localScale = new Vector3(oColliderSphere.radius, oColliderSphere.radius, oColliderSphere.radius);
                        oBoneRendererT.localRotation = Quaternion.identity;
                    }
                    else if (oCollider.GetType() == typeof(BoxCollider)) {
                        BoxCollider oColliderBox = (BoxCollider)oCollider;
                        GameObject oBoneRendererPrefab = Resources.Load("Prefabs/BoneRenderer_Box", typeof(GameObject)) as GameObject;
                        GameObject oBoneRendererGO = GameObject.Instantiate(oBoneRendererPrefab) as GameObject;
                        oBoneRendererGO.name = "BoneRenderer";
                        Transform oBoneRendererT = oBoneRendererGO.transform;
                        oBoneRendererT.parent = oCollider.transform;
                        oBoneRendererT.localPosition = new Vector3(oColliderBox.center.x, oColliderBox.center.y, oColliderBox.center.z);
                        oBoneRendererT.localScale = new Vector3(oColliderBox.size.x, oColliderBox.size.y, oColliderBox.size.z);
                        oBoneRendererT.localRotation = Quaternion.identity;
                    } else {
                        Debug.LogWarningFormat("W: BroadcastMessage_HideOrShowBoneRenderers() could not create rendering mesh for collider of type '{0}'", oCollider.GetType().ToString());
                    }
                }
            } else {
                //=== Iterate through all child objects containing a MeshRenderer to find all the objects named 'BoneRenderer' and destroy them ===
                MeshRenderer[] aMeshRenderers = oBody._oBodyRootGO.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer oMR in aMeshRenderers) {
                    if (oMR.gameObject.name == "BoneRenderer")
                        DestroyObject(oMR.gameObject);
                }
            }
        }
    }

    public void HideShowMeshes(bool bShowPresentation, bool bShowPhysxColliders, bool bShowMeshStartup, bool bShowPinningRims, bool bShowFlexSkinned, bool bShowFlexColliders, bool bShowFlexParticles) {
        foreach (CBody oBody in _aBodies)
            if (oBody != null)
                oBody.HideShowMeshes(bShowPresentation, bShowPhysxColliders, bShowMeshStartup, bShowPinningRims, bShowFlexSkinned, bShowFlexColliders, bShowFlexParticles);
        HideOrShowPhysxColliders(bShowPhysxColliders);
    }

    public void HideShowMeshes() {
        HideShowMeshes(ShowPresentation, ShowPhysxColliders, ShowMeshStartup, ShowPinningRims, ShowFlexSkinned, ShowFlexColliders, ShowFlexParticles);
    }


    void HideShowMeshes(bool bShowPresentationMeshes) {
        ShowPresentation = bShowPresentationMeshes;             
        ShowFlexParticles = !bShowPresentationMeshes;
        //ShowPhysxColliders = !bShowPresentationMeshes;
        if (bShowPresentationMeshes)        // Quickly set all the debug rendering off if we want presentation meshes.
            ShowMeshStartup = ShowPinningRims = ShowFlexSkinned = ShowFlexColliders = ShowFlexParticles = false;
        HideShowMeshes();
    }


    //---------------------------------------------------------------------------	LINE MANAGER
    public static LineRenderer Line_Add(string sKey) {
        GameObject oTemplateGO = Resources.Load("Prefabs/LineRenderer", typeof(GameObject)) as GameObject;
        GameObject oLineGO = Instantiate(oTemplateGO);
        oLineGO.name = sKey;
        oLineGO.transform.SetParent(GameObject.Find("Resources/LineManager").transform);
        oLineGO.SetActive(true);
        LineRenderer oLR = oLineGO.GetComponent<LineRenderer>();
        return oLR;
    }


    //---------------------------------------------------------------------------	GAME MODES

    public void ChangeGameMode(EGameModes eGameMode) {
		if (_GameMode == eGameMode)
			return;

        foreach (CBody oBody in _aBodies)
			if (oBody != null)
				oBody.OnChangeGameMode(eGameMode, _GameMode);           //###N ###BUG Problem is that game mode play disables kinematic... and builds soft bodies!

		_GameMode = eGameMode;

        switch (_GameMode) {
        	case EGameModes.Play:
                Update();                               // Manually run the update loop so that Flex delayed-creation gets to run to finalize any softbodies that got created
                ScenePose_Load("Standing", false);      //###G ###DESIGN!!
        		break;
        	case EGameModes.Configure:
                break;
        }
        HideShowMeshes();           // Hide or show meshes as per configured by our (many) global variables.
    }
    public void HoldSoftBodiesInReset(bool bSoftBodyInReset) {                       // Reset softbodies to their startup state.  Essential during pose load / teleportation!
        foreach (CBody oBody in _aBodies)
            if (oBody != null)
                oBody.HoldSoftBodiesInReset(bSoftBodyInReset);
    }

   	public void TemporarilyDisablePhysicsCollision(float nTimeInSec = 2.0f) {	//###TUNE		// Disable collisions on default layers so bones can go through each other for a small amount of time to avoid tangling.
		Physics.IgnoreLayerCollision(0, 0, true);			//###IMPROVE: Consider a coroutine??
		_nTimeReenableCollisions = Time.time + nTimeInSec;
	}

	public void SetGameModeBasicInteractions(bool bGameModeBasicInteractions) {
		_bGameModeBasicInteractions = bGameModeBasicInteractions;
		BroadcastMessageToAllBodies("OnBroadcast_HideOrShowHelperObjects", _bGameModeBasicInteractions == false);
		_oPoseRoot._oHotSpot.GetComponent<Renderer>().enabled = !_bGameModeBasicInteractions;		// Also show / hide pose root
		if (_bGameModeBasicInteractions)			// Force move mode on cursor if going to hidden helper objects (we only fast track move operations)
			_oCursor.SetEditMode(EEditMode.Move);
	}

    public void SetBodiesAsKinematic(bool bBodiesAreKinematic) {       //###KEEP ###P  ###DESIGN: Closely related to game modes... Can be merged in there?
        if (bBodiesAreKinematic)                // If we're going to kinematic this means we'll probably be performing rapid body movements.  Hold softbodies in reset so they can appear to gracefully handle large distance / orientation movement that might have occured when body was kinematic
            HoldSoftBodiesInReset(true);
        foreach (CBody oBody in _aBodies)
            if (oBody != null)
                oBody.SetBodiesAsKinematic(bBodiesAreKinematic);
        _bBodiesAreKinematic = bBodiesAreKinematic;             // Remeber this globally as it influences how CActor.TeleportLinkedPhysxBone() pins bones to pin position for instant & safe teleportation
        if (bBodiesAreKinematic == false)       // Kinematic movement mode ended.  Restore the soft body to Flex simulation.
            HoldSoftBodiesInReset(false);
    }




    //---------------------------------------------------------------------------	BLENDER MESSAGING

    public static string gBL_SendCmd(string sModule, string sCmd) {
		string sCmdFull = "__import__('" + sModule + "')." + sCmd;
		IntPtr hStringIntPtr = ErosEngine.gBL_Cmd(sCmdFull, false);		//###LEARN: Non-crash & non-leak way to obtain strings from C++ is at http://www.mono-project.com/Interop_with_Native_Libraries#Strings_as_Return_Values
		string sResults = Marshal.PtrToStringAnsi(hStringIntPtr);
		if (sResults.StartsWith("ERROR:"))		//###IMPROVE: Internal C code also has numeric error values not exported to Unity... Error strings enough?
			Debug.LogError("ERROR: Cmd_Wrapper error results.  OUT='" + sCmdFull + "'  IN='" + sResults + "'");		//###WEAK: Assume all error strings start with this!  ####DEV!!!!
		//Debug.Log("-Cmd_Wrapper OUT='" + sCmd + "'  IN='" + sResults + "'");
		return sResults;
	}

	public static int gBL_SendCmd_GetMemBuffer(string sModuleList, string sCmd, ref CMemAlloc<byte> mem) {			//####SOON ####IMPROVE: Can get rid of damn import list by importing everything once at game init??
		string sCmdFull = "__import__(" + sModuleList + ")." + sCmd;
		IntPtr pSizeInBuf = ErosEngine.gBL_Cmd(sCmdFull, true);
		int nSizeByteBuf = pSizeInBuf.ToInt32();		
		if (nSizeByteBuf <= 0)
			throw new CException("gBL_SendCmd_GetMemBuffer() could not obtain valid byte buffer from gBL_Cmd()!");
		mem.Allocate(nSizeByteBuf);
		ErosEngine.gBL_Cmd_GetLastInBuf(mem.P, nSizeByteBuf);		
		return nSizeByteBuf;
	}





	//---------------------------------------------------------------------------	MISC

	public void Cum_Stop() {			// Stop all cumming and Wipe away cum (reset fluid)
		foreach (CBody oBody in _aBodies)
			if (oBody != null)
				oBody.SetIsCumming(false);
		//CGame.INSTANCE._oFluid.ResetFluid();
	}
	public void BroadcastMessageToAllBodies(string sFunction, object oArg) {			//###IMPROVE: Redo all broadcasting through this approach!
		foreach (CBody oBody in _aBodies)
			if (oBody != null)
				oBody._oBodyRootGO.BroadcastMessage(sFunction, oArg);
		CGame.INSTANCE._oPoseRoot.gameObject.BroadcastMessage(sFunction, oArg);		// Also broadcast to the pose root which keeps many helper objects
	}

    #endregion


    //---------------------------------------------------------------------------	PROCESS CREATION
    #region PROCESS CREATION
    public static System.Diagnostics.Process LaunchProcess(string sFileProcess, string sArguments, bool bRedirectOutput, System.Diagnostics.ProcessWindowStyle eWinStyle = System.Diagnostics.ProcessWindowStyle.Minimized) {
		System.Diagnostics.Process oProc = new System.Diagnostics.Process();
		oProc.StartInfo.FileName = sFileProcess;
		oProc.StartInfo.Arguments = sArguments;
        //oProc.StartInfo.CreateNoWindow = true;		// Does anything useful??
        oProc.StartInfo.UseShellExecute = false;        //###NEW: ShellExecute gives us a black blender window with splash!!	// Must be false to redirect below.			//###LEARN: Can't seem to get redirct working with Blender (Test with another app)  Could be that we cannot redirect Blender.  use Shell Execute so at least user can see damn console for errors!!!
		if (bRedirectOutput) {
			oProc.StartInfo.RedirectStandardOutput = true;	//###IMPROVE!!!!: Redirect in/out/err and benefit from this to catch Blender errors! ###SOON
			oProc.StartInfo.RedirectStandardError = true;	//###LEARN: Holy crap does .Net ever rule... you'd have to shoot me doing this in C++!
			//oProc.StartInfo.RedirectStandardInput = true;	//###IMPROVE!!!! Possibly to feed commands to Blender via its console-based Python console?  Would be ultra cool!!  ###RESEARCH!!!!!
		}
		oProc.StartInfo.WindowStyle = eWinStyle;
		Debug.Log(string.Format("CGame.LaunchProcess() file='{0}'  args='{1}'", sFileProcess, sArguments));
		oProc.Start();			//###IMPROVE: Many powerful features in this class!!!!
		//ShowWindow(oProc.MainWindowHandle, 2);            //###IMPROVE?
		return oProc;		//###CHECK: Will above throw if there was an error??  We need to throw if error!
	}

	public static System.Diagnostics.Process LaunchProcessBlender(string sNameBlenderFile, System.Diagnostics.ProcessWindowStyle eWinStyle = System.Diagnostics.ProcessWindowStyle.Minimized) {
		string sFileProcess = CGame.GetPathBlenderApp();
        string sArguments = string.Format("\"{0}/{1}\" --Wait-for-Erotic9 --enable-autoexec --start-console", CGame.GetPathBlends(), sNameBlenderFile);       //###IMPROVE: Don't show console during releases!
		System.Diagnostics.Process oProcess = CGame.LaunchProcess(sFileProcess, sArguments, false, eWinStyle);	//###IMPROVE: Try to redirect!
		oProcess.Exited				+= oProcess_Exited;
		oProcess.OutputDataReceived += oProcess_OutputDataReceived;
		oProcess.ErrorDataReceived	+= oProcess_ErrorDataReceived;
		return oProcess;
	}

	static void oProcess_OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e) {
		Debug.Log("B: " + e.ToString());
	}
	static void oProcess_ErrorDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e) {
		Debug.LogError("B.Error! " + e.ToString());
	}

	static void oProcess_Exited(object sender, EventArgs e) {
		DisplayAlert("Blender Error", "Blender Exited!  Game cannot continue.");
		//###IMPROVE: What to do?  Offer to terminate game??
	}


	[DllImport("user32.dll")] static extern uint GetActiveWindow();
	[DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
	[DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
	[DllImport("Kernel32.dll")] static extern int LoadLibraryEx(string sLibFileName, long hFile, long dwFlags);
	[DllImport("Kernel32.dll")] static extern int FreeLibrary(int hLib);
    #endregion


    //---------------------------------------------------------------------------	UTILITY

    public static CBody GetSelectedBody() {
		return CGame.INSTANCE._aBodies[CGame.INSTANCE._nSelectedBody - 1];		//###NOTROBUST
	}

	public static void SetGuiMessage(EGameGuiMsg eGameGuiMsg, string sMsg) {
		CGame.INSTANCE._aGuiMessages[(int)eGameGuiMsg] = sMsg;
	}
	public static void DisplayAlert(string sTitle, string sMsg) {
		///iGUI.iGUIRoot.alert("Blender Error", "Blender Exited!  Game cannot continue.");
	}

	public static string GetMachineID() {		// Returns a MAC id to be used for game authentication
		NetworkInterface[] aNics = NetworkInterface.GetAllNetworkInterfaces();
		foreach (NetworkInterface oNic in aNics) {			// First return the first ethernet adaptor (up or not) ###WEAK: Can return NICs from things like VMWare or are these always last?
			if (oNic.NetworkInterfaceType == NetworkInterfaceType.Ethernet || oNic.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet || oNic.NetworkInterfaceType == NetworkInterfaceType.Ethernet3Megabit || oNic.NetworkInterfaceType == NetworkInterfaceType.FastEthernetFx || oNic.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT) 
				return oNic.GetPhysicalAddress().ToString();
		}
		foreach (NetworkInterface oNic in aNics) {			// Second return the first up adaptor (can change from wifi to ethernet, etc)
			if (oNic.OperationalStatus == OperationalStatus.Up)
				return oNic.GetPhysicalAddress().ToString();
		}
		if (aNics.Length > 0)
			return aNics[0].GetPhysicalAddress().ToString();	// Third return the first one
		return "11:22:33:44:55:66";							// Fourth return a fake ID (Should never get here?)
	}
	public static string GetWebString(string sURL) {
		WWW oWWW = new WWW(sURL);
		while (oWWW.isDone == false && oWWW.error == null) { }			//###HACK!!!!
		if (oWWW.error != null)
			throw new CException("Error connecting to Erotic9 server: " + oWWW.error);
		string sResult = oWWW.text;					//###IMPROVE: Decode what machine ID was previously assigned?
		return sResult;
	}




	//###CLEANUP: From CGamePlay
	//---------------------------------------------------------------------------	UTILITY
	public void CreateBody(int nBodyID) {
		//=== Destroy the body's head look controller as we cannot look at the 'other body' while the bodies are changing in the scene ===
		//if (_aBodies[0] != null)
		//	_aBodies[0]._oHeadLook.AddLookTargetsToOtherBody(null);
		//if (_aBodies[1] != null)
		//	_aBodies[1]._oHeadLook.AddLookTargetsToOtherBody(null);

		//=== Destroy the entire tree of object a body is by simply destroying its root gameObject!  (Needs DestroyImmediate as lazy Destroy causes rebuild problems)
		if (_aBodies[nBodyID] != null)
			_aBodies[nBodyID].Destroy();
		_aBodies[nBodyID] = null;
		_aBodies[nBodyID] = new CBody(nBodyID);
		_aBodies[nBodyID].DoInitialize();

        //####HACK!!!!
        //if (_aBodies[0] != null)
        //    _aBodies[0]._oHeadLook.AddLookTargetsToOtherBody(null);
        //if (_aBodies[1] != null)
        //    _aBodies[1]._oHeadLook.AddLookTargetsToOtherBody(null);

        //=== Reconnect the head look controller to look at the other body only if the scene now has two valid bodies ===
  //      if (_aBodies[0] != null && _aBodies[1] != null) {
		//	_aBodies[0]._oHeadLook.AddLookTargetsToOtherBody(_aBodies[1]);
		//	_aBodies[1]._oHeadLook.AddLookTargetsToOtherBody(_aBodies[0]);
		//}
		//CGame.SetGuiMessage(EGameGuiMsg.GameStatus] = null;				//###BROKEN!!  How to display before lenghty blocking calls below??? CGame.SetGuiMessage(EGameGuiMsg.GameStatus] = "Rebuilding Body.  Please Wait...";
	}

	public void SetPenisInVagina(bool bPenisInVagina) {		// When penis tip meets the vagina trigger collider (runs from entrance to deep inside), vagina guide track is activated, penis ceases to create dynamic colliders in CBodyCol and fluid only collides with vagina guide track
		if (CGame.INSTANCE._DemoVersion)		//###WEAK!!!! ###TEMP!!!
			_bPenisInVagina  = false;		// Demo build does not allow penetration...
		else
			_bPenisInVagina = bPenisInVagina;
		//CGame.INSTANCE._sGuiString_Dev_DEBUG = "_bPenisInVagina = " + _bPenisInVagina;
		//if (_aBodies[1] != null && _aBodies[1]._oVagina != null)
		//	_aBodies[1]._oVagina.VaginaGuideTrack_EnableDisable(_bPenisInVagina);		//###WEAK? Body 0/1 design issues??
	}

	public CBody FindFirstCharacterWithPenis() {
		foreach (CBody oBody in _aBodies) {
			if (oBody._eBodySex != EBodySex.Woman) 
				return oBody;
		}
		return null;
	}
	public CBody GetBodyOther(CBody oBody) {		// Returns the other body.  Assumes only a 2-body scene.
		if (oBody == _aBodies[0])		//###DESIGN: Revisit this once we fix assumptions as to bodies in body 0/1
			return _aBodies[1];
		else
			return _aBodies[0];
	}

	//--------------------------------------------------------------------------	SCENE LOAD / SAVE  ###MOVE??

	public void ScenePose_Load(string sNameScenePose, bool bScenePoseFlipped) {		// Load a 'scene' = The pose & position of each character plus CPoseRoot position.  bInvert loads pose for body 2 into body 1 and vice versa
		string sPathScenePose = CGame.GetPathSceneFile(sNameScenePose);	// Scenes save their name as the folder, with the payload file always called 'Scene.txt'

		if (File.Exists(sPathScenePose)) {
			_sNameScenePose = sNameScenePose;
			_bScenePoseFlipped = bScenePoseFlipped;
            SetBodiesAsKinematic(true);
			Debug.Log("Scene_Load() loading scene " + sPathScenePose);
			StreamReader oStreamRead = new StreamReader(sPathScenePose);
			string sLine = oStreamRead.ReadLine();
			EPoseRootPos ePoseRootPos = (EPoseRootPos)Enum.Parse(typeof(EPoseRootPos), sLine);
			Scene_ApplyPoseRoot(ePoseRootPos);

			//###DESIGN: No longer store pose root pos/rot?  (Only anchor?)
			//string[] aVals = sLine.Split(',');	// First line of scene file contains position / rotation of CPoseRoot in comma separated values
			//Vector3 vecBase;		= new Vector3(float.Parse(aVals[0]), float.Parse(aVals[1]), float.Parse(aVals[2]));
			//Quaternion quatBase;	= new Quaternion(float.Parse(aVals[3]), float.Parse(aVals[4]), float.Parse(aVals[5]), float.Parse(aVals[6]));
			//CGame.INSTANCE._oPoseRoot.transform.position = vecBase;
			//CGame.INSTANCE._oPoseRoot.transform.rotation = quatBase;

			int nBody, nBodyInc;		// If inverted load 1 then 0, else 0 then 1
			if (bScenePoseFlipped) {
				nBody = 1;
				nBodyInc = -1;
			} else {
				nBody = 0;
				nBodyInc = 1;
			}
			while (oStreamRead.Peek() >= 0) {
				sLine = oStreamRead.ReadLine();
				string[] aVals = sLine.Split(',');			// Scene filename is a very simple text file with one line for each character that contains <PoseFileName>,x,y,z
				string sPoseName = aVals[0];
				Vector3 vecBase = new Vector3(float.Parse(aVals[1]), float.Parse(aVals[2]), float.Parse(aVals[3]));
				Quaternion quatBase = new Quaternion(float.Parse(aVals[4]), float.Parse(aVals[5]), float.Parse(aVals[6]), float.Parse(aVals[7]));
                _aBodies[nBody]._oActor_Base.transform.localPosition = vecBase;
                _aBodies[nBody]._oActor_Base.transform.localRotation = quatBase;
                _aBodies[nBody].Pose_Load(sPoseName);
				nBody += nBodyInc;
                if (_aBodies[nBody] == null)        //###G ###TEMP
                    break;
			}
        }
        else {
			Debug.LogError("Scene_Load() cannot find file " + _sNameScenePose);
		}
        SetBodiesAsKinematic(false);      // Resume normal property sets (changes to pose nodes only move the node and not the PhysX bone)
    }

    public void ScenePose_Save(string sNameScenePose) {
		_sNameScenePose = sNameScenePose;
		_bScenePoseFlipped = false;				// By definition when we save a pose it is not-flipped.  (Flipping only occurs during load)

		Directory.CreateDirectory(CGame.GetPathScenes() + _sNameScenePose);							// Make sure that directory path exists
		string sPathScenePose = CGame.GetPathSceneFile(_sNameScenePose);			// Scenes save their name as the folder, with the payload file always called 'Scene.txt'
		Debug.Log("Scene_Save() saving scene " + sPathScenePose);
		StreamWriter oStreamWrite = new StreamWriter(sPathScenePose);
		EPoseRootPos ePoseRootPos = (EPoseRootPos)_oObj.PropGet(EGamePlay.PoseRootPos);
		oStreamWrite.WriteLine(ePoseRootPos.ToString());

		//Vector3 vecBase = CGame.INSTANCE._oPoseRoot.transform.position;
		//Quaternion quatBase = CGame.INSTANCE._oPoseRoot.transform.rotation;
		//string sLine = string.Format("{0},{1},{2},{3},{4},{5},{6}", vecBase.x, vecBase.y, vecBase.z, quatBase.x, quatBase.y, quatBase.z, quatBase.w);
		//oStreamWrite.WriteLine(sLine);
	
		for (int nBody = 0; nBody < 2; nBody++) {			// Iterate through all bodies and save their currently loaded pose and their base position.
			CBody oBody = _aBodies[nBody];
			Vector3 vecBase = oBody._oActor_Base.transform.localPosition;
			Quaternion quatBase = oBody._oActor_Base.transform.localRotation;
			string sLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7}", oBody._sNamePose, vecBase.x, vecBase.y, vecBase.z, quatBase.x, quatBase.y, quatBase.z, quatBase.w);
			oStreamWrite.WriteLine(sLine);
		}
		oStreamWrite.Close();				//###IMPROVE: Save placeholder image?
	}

	public void Scene_Reload() {
		ScenePose_Load(_sNameScenePose, _bScenePoseFlipped);
	}
	public void Scene_ApplyPoseRoot(EPoseRootPos ePoseRootPos) {
		GameObject oPosRootPosGO = GameObject.Find("Resources/EPoseRootPos/" + ePoseRootPos.ToString());
		CGame.INSTANCE._oPoseRoot.transform.position = oPosRootPosGO.transform.position;
		TemporarilyDisablePhysicsCollision();
	}


	//--------------------------------------------------------------------------	COBJECT CALLBACK EVENTS

	public void OnPropSet_Arousal(float nValueOld, float nValueNew) {	//###OBS???
		const int nCutErection	= 0;		//###OBS?
		const int nCutCum		= 100;
		const int nRangeGrowth	= nCutCum - nCutErection;		//###JUNK?

		if (nValueNew < nCutErection) {

			_oObj.PropSet(EGamePlay.PenisErectionMax, 100.0f * nValueNew / nCutErection);
			_oObj.PropSet(EGamePlay.PenisSize, 0);
			//_oObj.PropSet(EGamePlay.Ejaculation, 0);

		} else if (nValueNew < nCutCum) {

			_oObj.PropSet(EGamePlay.PenisErectionMax, 100.0f);
			_oObj.PropSet(EGamePlay.PenisSize, 100.0f * (nValueNew - nCutErection) / nRangeGrowth);
			//_oObj.PropSet(EGamePlay.Ejaculation, 0);

		} else {

			_oObj.PropSet(EGamePlay.PenisErectionMax, 100.0f);		//###IMPROVE: Really needed to be fully determinisitic on all properties or can we assume progression will set correctly?
			_oObj.PropSet(EGamePlay.PenisSize, 100);
			//_oObj.PropSet(EGamePlay.Ejaculation, 1);

		}
	}

	public void OnPropSet_FluidConfig(float nValueOld, float nValueNew) {
		//if (nValueNew == 1)
		//	CUtility.WndPopup_Create(EWndPopupType.PropertyEditor, new CObject[] { CGame.INSTANCE._oFluid._oObj }, "Fluid Configuration", 0, 0);
	}
	public void OnPropSet_PoseRootPos(float nValueOld, float nValueNew) {		// Set the pose root from the user-selected pose root.
		EPoseRootPos ePoseRootPos = (EPoseRootPos)nValueNew;		// Scene 3D positions to act as 'root' for the CPoseRoot.  MUST match content of folder node Resources/EPoseRootPos!
		Scene_ApplyPoseRoot(ePoseRootPos);
	}

	
	//--------------------------------------------------------------------------	IHotspot interface

	public void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) { }
	public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {
		if (eHotSpotEvent == EHotSpotEvent.ContextMenu)
			_oHotSpot.WndPopup_Create(_aBodies[0], new CObject[] { _oObj });        //###DESIGN: What to do??  ###U
	}






	//---------------------------------------------------------------------------	FOLDERS

	public static string GetFolderPathRuntime() {					// Returns the path to the Erotic9 root directory, whether we are an editor build or a player build  ###NOTE: Assumes the editor build and player build are in the same parent directory!!!
		string sNameFolder = Application.dataPath;
		string sPathSuffixToRemove = (Application.isEditor) ? "Unity/Assets" : "Erotic9_Data";			// Application.dataPaty returns a string like "C:/Src/E9/Erotic9/Erotic9_Data" in player but "C:/Src/E9/Unity/Assets" in editor.  We convert this into a string like "D:\Src\E9\Erotic9" for both builds for constant directory access
		int nPosSuffix = sNameFolder.IndexOf(sPathSuffixToRemove);
		if (nPosSuffix == -1)
			throw new CException("CGame.GetFolderPathRuntime() could not recognize dataPath suffix: " + sPathSuffixToRemove);		//###IMPROVE: Do once at start and remember string?
		sNameFolder = sNameFolder.Substring(0, nPosSuffix);
		if (Application.isEditor)
			sNameFolder += "Erotic9/";
		sNameFolder += "Runtime/";
		return sNameFolder;				// This should always return a string like "D:/Src/E9/Erotic9/Runtime/" for both editor and player buids.  This is our 'root directory' where all our assets are based!
	}

	public static string GetPathPoses() { return GetFolderPathRuntime() + "Poses/"; }
	public static string GetPathPoseFile(string sNameFolder) { return GetPathPoses() + sNameFolder + "/Pose.txt"; }		// Poses are stored in their own name directory with payload file always 'Pose.txt'
	public static string GetPathScenes() { return GetFolderPathRuntime() + "Scenes/"; }
	public static string GetPathSceneFile(string sNameFolder) { return GetPathScenes() + sNameFolder + "/Scene.txt"; }	// Scenes are stored in their own name directory with payload file always 'Scene.txt'
	public static string GetPathScript(string sNameFile) { return GetFolderPathRuntime() + "Scripts/" + sNameFile + ".txt"; }
	public static string GetPathScreenCaptures() { return GetFolderPathRuntime() + "ScreenCaptures/"; }
	public static string GetPathBlends() { return GetFolderPathRuntime() + "Blends"; }
    public static string GetPathBlender()				{ return GetFolderPathRuntime() + "Blender"; }
    //public static string GetPathBlender()			    { return "C:/src/E9/Erotic9/Runtime/Blender" }
    //public static string GetPathBlender()			    { return "C:/src/E9/blender-build/bin/Debug"; }	//###HACK!!!
    public static string GetPathBlenderApp()			{ return GetPathBlender() + "/blender.exe"; }
	
	public static float GetRandom(float nFrom, float nTo) {
		return (float)CGame.INSTANCE._oRnd.NextDouble() * (nTo - nFrom) + nFrom;
	}
}

public enum EGameGuiMsg {		//###OBS??  ###TODO: Incomplete!
	Dev1,					// Output from DLL dev 1&2 string for misc info
	Dev2,
	SelectedBody,
	SelectedBodyAction,
	FluidPolygonize,
	FluidSimulation,
	MouseEdit,
    Misc1,
    CursorStat1,
    CursorStat2,
    CursorStat3,
    COUNT						// Not an actual entry, just for sizing
}

public enum EPoseRootPos {		//###MOVE Scene 3D positions to act as 'root' for the CPoseRoot.  MUST match content of folder node Resources/EPoseRootPos!
	BedFront,
	BedFrontFrame,
	BedCorner,
	BedTop,
}


//GameObject oGameGO = GameObject.Find("(CGame)");						//###BUG!!!!!: Not working even though it should!!!  WTF???  Forcing us to use Awake() and 'script execution order'
//if (oGameGO == null)
//	throw new CException("ERROR: CGame.INSTANCE called and could not find (CGame) node!! (BUG!!)");	// This will certainly cause calling function to fail but there is nothing we can do!!  //###IMPROVE: Exception?
//CGame.INSTANCE = oGameGO.GetComponent<CGame>();																			// One and only place where the singleton is set...  *MUST* occur very early on in codebase!!
//if (CGame.INSTANCE == null)
//	throw new CException("ERROR: CGame.INSTANCE not found on CGame node!!  (BUG!!)");
//PhotonNetwork.offlineMode = true;
//PhotonNetwork.ConnectUsingSettings("0.1");



//Time.timeScale = (Time.timeScale == 1.0f) ? 0.05f : 1.0f;		//###REVA ###TEMP
//Debug.Log("TimeScale=" + Time.timeScale.ToString());
