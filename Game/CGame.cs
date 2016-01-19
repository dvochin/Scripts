/*###DISCUSSION: CGame
=== STRATEGY ===
- Play a bit with breast col density
- Add more breast sliders
- Add full body morph capacity for breasts up/down
- Apply SlaveMesh functionality to cloth collider (half, separate, pair, fix after morph, etc)
- Idea about source body storing its original verts in a layer of any use??

- Work to have the two game modes go seamlessly from one to other
	- Rethink the game mode enums and the flag in CBody
	- Once fully working do we remove the old mode crap?

=== REVIVE ===
- Why are bones below hip copied twice at runtime??
- Sleep of bones reoccuring?
- Can enhance cloth reset with delta pos/rot








=== TODO ===
 * OpenCL out of resources after a while...  FUCK!!
   * Looks like a complete re-init needed FUCK!!
 * Stop fluid when loading pose
 * ++ Have to implement event notification for client... with color codes for common errors such as overstretch, opencl error, low perf, etc
   * ADD TOOLTIPS to explain
 * Change in hand keys???
 * Multiple panels!
 * Redo hand implementation
 * Reset pins not really working!  Torso == Chest??
 * Need stronger pins when pinning arms?
 * +++ Pose should store arm target!  (Pinned currently a nightmare!)
 * Arms damped too much (undamp during load only?)
 * New hand keys?
 * Stop cum when init body
 * Place hands on head would be really nice
 * Save blender when a body is created? (or before closing game??)
 * Get rid of penis tip menu and reroute to sex?  (same with vagina?)
 * Tooltips on all properties
 * ++ Properties don't change for man/woman
 * Have split bone tree... right decision?  Trim extra junk, revisit colliders 
 * Color and shape code hotpots soon!
 * New combo box settings for CProp GUI
 *? Have early blender float properties... convert to string for the clothing?
   * Better would be an enum for the clothes from directory.
 * Improve female ejaculation... set set properties like gravity, emitter size?
 * PhysX33
 * Should we move torso hotspot to neck??  (hard to pick)
 * Auto number precision from bounds
 * Have single property change from holding key
 * Need to square up the many panels!  Dock or keys!
 * Breast collision with arms and rough colliders
 * Blend in fixed animations with slerp
   * Need limiting angles for fixed pins and not for raycast?
 * Will need hand on head, hand brace on bed
   * Raycast on PhysX3 scene?

=== PROBLEMS ===
 * Cum collider still to be disabled if penis in vagina??
 * Two dicks in scene all influenced by keys!
 * Problem with dick init if pleasure event never occurs
 * Scene reload during body init forgets 'inverted'
 * WTF log file in Unity folder????
 * HandleD3DDeviceLost upon cum and game crash in built game!
 * Sex bone super low now!
 * Problem with hand pins loading a new pose if hands were pinned 
 *???? Hand position corrupted on load!!
 * Gizmo hard to select on 2nd time an issue
 * Hand default position way off: Init?
 * Face deleted during body init!
 * Pose loader can be 180 rotated and appear confusing... store rotation in char pose??? (Disabled button)
 * Reload body can cause hotspot exception... last hovered one?
 * Penis tip poke through!
 * Crash entering PhysX2, improve logging
   * Add regular flushes to logging
 * //###BUG: Cursor text size grow up / down with zoom
 * //###BUG!!!!! Problem with CActor and rotation. Euler conversion can't take all angles!!
 * +++ Dlg action click through a big problem!
 * PoseLoad and Save: Watch out for man / woman diff!!
 * Why offset when reset pins???
 * PenisScaleDampCenter off
 * During rebuild hovering hotspots cause trouble.
 *+++ Why is Woman_Face or Woman top node getting deleted???
 * Vagina SB not destroying during rebuild!!
 * ERROR PhysX2: CUDA not available   File(D:\sw\physx\PhysXSDK\2.8.4\trunk\SDKs\Core\Device\RegistryHardwareSelection.cpp) Line(71)
 * BUG: pin cube shows an orientation different than hotspot!
 * Have seen bug not being able to right click on a hotspot... lost in some mode in pose mode??
 * Extra hotspots for obsolete hand targets annoying... remove or fix??
 * Penis softbody appears out of sync with penis... tip goes into body?
 * Single intances of windows!!!
 * Stats dont go to zero when not used over time
 * GUI message will not all show in some res
 * Missing stats
 * PhysX complains about shape set to invalid geometry... Body colliders when idle??
 * Top of breasts for softbody getting an issue... adjust sphere??
 *++++ Penis shake on close to self body a showstopper
 * 888-771-5803 Catherine

=== PROBLEMS: ASSETS ===
 * Man texture too 'yellow'
 * Improve penis texture blend on man
 * Man head texture problem?
 * Chest up/down on man flawed!
 * Problems with man colliders running into each other... really need layers!
 * Improve room with black roof
 * Man and women bone trees have chest collider colliding against arms... need to set to own layer?

=== PROBLEMS??? ===
 * Test sex change with blender init.  that flag still of use???
 * Blender script protection a problem for install??
 * Vagina track not hidden at start?
 * Guide catching cum?

=== IDEAS ===
 * Add pose categories and ordering??
 * Add 'flip' hotkey!
 * A 'special mode' to move some hard-to-get-at hotspots (with them drawn in x-ray)
   * Hide them most of the time (in non pose mode?)
 * Add frame stamp to log entries
 * Hand pose load position... should be remembered so user can load it back!
 * F1-F4 for hand control, F5-F12 for poses
 * Save log files in diff folder?
 * Screen capture feature with output folder... with contest!
 * When cumming in pussy cap cum near entrance!
 * Map mouse button 4-5??
 * sw = Stopwatch.StartNew();
 * Do some body bends with a hotkey pressed!
 * Add additional keys for 1st person cameras!
 * iGUI supports tooltips per combo box entry!
 * Move torso with a quick key?? (Or raise hotspot of chest?
 * ++++ Add extra props from scene options!  And move them with hotspots!!
 * Have Shift+F spread all thighs??
 * Have a 'randomizer' key to enable user to dicate how 'free' character is
 * Feet separation key?
 * Cum guide in vagina & vagina cum?
 * A 'pleasure indicator' when caressing like in the meet & fuck games
 * 'Skip frame' functionality for cum!!
 * Vagina should have 'inverse cum funnel' to guide cum out of opening (doesn't collide with penis) Also put cap
 * Adjust separation between the bodies with one easy value (like penis angle?)
 * Reset pin positions to current with a hotkey (to solve 'stretching problem')
 * Move both legs in one go in 2D (pinned to floor?)

=== INVESTIGATE FEATURES ===
 * Usage for Mesh.Optimize()?
 * Usage for Mesh.Clear()? http://docs.unity3d.com/Documentation/ScriptReference/Mesh.html
 * Mesh.Topology has quads, lines, linestrip, points!
 * Mesh.MarkDynamic http://docs.unity3d.com/Documentation/ScriptReference/Mesh.MarkDynamic.html
+++ http://www.starscenesoftware.com/vectrosity.html for drawing lines!  Has awesome beziers!!

=== WISHLIST ===
 * User lighting control multiplier?
 * Cum in vagina!
 *? Usable full menu for both hands / both bodies, avoid trapping pins, smooth?
 *
=== NExT: BodyCol ===
 * Breast collider oriented 90 degrees out???
 * Fluid grid size out of wack critical issue???  (Why stiffness all broken now??)
 * Good stats will become critical going forward... create a new super class on top of CProp & new GUI with link to profiler
 * 
=== PROBLEMS ===
 * FPS appears at start in player
 * WTF is fucking size of box in PhysX2/3!!!
 * When cum falls on penis base colliders make penis shake!
 * Exit when Blender is not there!
 * Gizmo mostly broken: Move will move toward camera!  (Bad rotation at init?)
 * Penis collision exposions... 

 * === TODO ===
 * Very quickly set colliders owned by cloth for faster OnUpdate... check again!!
 * Breasts not skinned!
 * Can't anim at start because cloth flies off!!  Reposition root higher??
 * Bad latency problem... gone with reposition of colliders???
 * 
 * 
 * === JUNK? ===
 * POSING FAST TRACK 
	 * DETERMINE IF WE GROUP PINS!!!
	 * How about this idea with anim curves????  Should it save our info???
	-URGENT!: Now having to photonize CBody but can't have multiple base classes!  what to do?

 * Posing load and save...
	 * Do we have any hierarchy with the pins??  (like ArmL/R belonging to Arm and it to torso...) so we save relative positions... important!!  TEST
	 * We'll need the ability to load a single character... takes quite a bit of time to do one right!
	 * OR... do we store poses as individuals and rely on the pose designer posing the couple / threesome / quad to load and place posed individuals??  BETTER!!
		 * Then the 'point of insertion (3d position where genital goes (
 * Raising body so feet are on the ground...
	 * Is that even an issue?  Do we just snap to pins and everything ok like our pose?
 * Revise decisions on posing:
	 * For first demo, do we create anims from Unity editor or with our gizmos???
	 * Collision groups ok?
	 * Weight of feet?
	 * Soften drive of pins... appear stiff
	 * Drive on thigh open not enough
	 * Knee folding?  Is that to autocrouch?
	 * Need to allow arms to drop...
 * Finger bones might cause more trouble than worth for now...  Just drive finger bones directly?
	 * Or do we abstract all four fingers as two boxes and thumb as one capsule?
	 * 'doing cool things with hands' is going to be difficult... pick first poses where they are tied up.
 * Arm behind head can be achieved with existing hand driving!!
 * Bad bending around the thighs might make cool poses impossible for now!
	 * Possible to iterate through those verts to smooth them out?
		 * Or is it better to add a bone to push them out?
 * Add show/hide pins again.

=== TODO ===
 * Have to harmonize PhysX properties for SoftBody & cloth soon...
	 * Gravity should be applied to some and not others...
	 * Reconnect a GUI to send these properties via reflection like before??
		 * Use our previous GUI slider or adopt iGUI?

=== LATER ===
 * Morphing now much simpler and more powerful with the rewrite for breast needed...

=== CURRENT PLAN TO REVENUE ===
 * Quick load and save of poses: in files through photon or anim curves???
 * Design a few ultra-hot poses by placing pins in Unity editor and saving them.
 * Implement hot animations from them... in anim curves???
 * PENETRATION!!!!

=== DESIGN ===
 * Max allowed time changed to 0.04 from .3333!!!

=== IDEAS ===
 * blendShapes and http://www.faceshift.com/unity/  Better then our solution??  How to import tho? (http://answers.unity3d.com/questions/574775/how-do-i-get-started-with-blend-shapes.html)
 * Properties really working well with client/server, GUI and scripting... worth enhancing with randomization, smooth adjust, animation, GUI control, etc.
 * Reducing density of penis softbody might make it stay in its cage more...
+++ Autofit of what pose is compatible with what other: have pose designer identify vagina angle at idle and range of motion and height...
	+ When user places a dick somewhere, code attempts to find woman poses made for that angle!  Like placing two a capsule in a sort of cone
		+ What to do about the feet tho... place invisible body first and see what collides?
 * Profiler can output to log file and accept external data (hint: C++ timing stats!)
 * IDEA: Constantly sending verts, tris and counts from different contexts... create a 'CMesh' in c++??
 * //Profiler.BeginSample("StatName");		//###LEARN: Custom stats!
 * ++++ Placing all our important objects as 'Update When Offscreen' prevents having to recalc bounds!!

=== LEARNED ===
 * Setting Unity time setting to .01 from .04 makes strobing effect of Fluid much less noticeable!  (But really slows down system!)
	 * However... setting corresponding number in C++ dll had terrible effects... what gives??

=== PROBLEMS ===
+++++ WTF body disapearing after 20 sec sometimes?? (PhysX window frozen when it happens)
	 * Log says CBSkinBaked lost its skinned mesh, but entire body node is gone!!
	 * Probably related to fluid crash... (was with SPH @ 10K)
	 * Seems to happen after 30 sec always!
	 * Could trap on destroy!
+ PhysX screwed up when we exit game and restart: Game doesn't cleanup!!
+++ Remember hack in PhotonHandler!!
 * Weird bug now with right breast more resistant to gravity???
++++ TRY to not scale penis at all frame... what happens?  Can do once in a while??
++++ Bones were all fucked...  reset sex to be less shitty but not exact...  rethink its 15 deg-off ownership of rest of bones!!
 * WTF happened with breasts & vagina being so soft now??
 * BodyA/BodyB getting a bit of pain in the ass in args everywhere... see if we can simplify?
 * Non-full game init missing meshes!
+ Unity needs to know what meshes Blender creates!  (Like panties, etc) for body to build with proper meshes!

=== PROBLEMS: ASSETS ===
 * Seam appears between breasts and armpits now

=== PROBLEMS??? ===
 * Once I saw performance drop to 23fps while the camera movement looked more like 5fps.   Checking profiler, things like CPinSkinned started taking 14ms and skin rim baked 11ms!
	 * After much testing I did a full rebuild all of the C++ dll and performance got back to 84fps?  WTF??  Why would a bad compile of DLL make C# code run much slower????
 * I think physx clock is ticking while game initializes... verify!
 * Massive rename / reorg around "BodyA' has broken tons of stuff... Many gBL calls now require full qualification!
 * Note that PhysX PVD viewer has X inverted!!!

=== WISHLIST ===
 *-- Disable gravity on some softbodies (to increase performance??)
=== WISHLIST ===
 * Desirability of a 'coarse body collider' concept (with legs, arms, etc being approximated with large capsules...
	 * Implications for accurate breasts & penis collisions
	 * 700 capsule collider limit... on any machine??  (Test on laptop)
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
public class CGame : MonoBehaviour, IObject {	// The singleton game object.  Accessable via anywhere via 'INSTANCE'.  Provides top-level game init/shutdown and initiates the 'heartbeat' calls that make the game progres frame by frame

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
	[HideInInspector]	public	CFluid			_oFluid;			// The one and only Fluid object the app can have.  (Multiple fluids can't collide with one another)

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
    int                 _nFpsCollectionCount;							//###IMPROVE: Use / display this to decrease GC calls!
    string              _sFPS;
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



	#region === INIT
    public void Awake() {
        StartCoroutine(Coroutine_StartGame());			// Handled by a coroutine so that our 'OnGui' can run to update the 'Please wait' dialog
    }
    public IEnumerator Coroutine_StartGame() {		//####OBS: IEnumerator?? //###NOTE: Game is started by iGUICode_Root once it has completely initialized (so as to present the 'Please Wait...' dialog
		Debug.Log("=== CGame.StartGame() ===");

		INSTANCE = this;
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

        //if (_GameMode == EGameModes.None) { 
        //    _oGamePlay = new CGamePlay();			//###TODO: Obtain identities of what character, what cloth we're editing from GUI!
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
		Debug.Log("INIT: Attempting to load ErosEngine.dll");
		int hLoadLibResult = LoadLibraryEx("ErosEngine.dll", 0, 2);
		if (hLoadLibResult > 32)
			FreeLibrary(hLoadLibResult);			// Free our dll so Unity can load it its way.  Based on code sample at http://support.microsoft.com/kb/142814
		else 			
			throw new CException("ERROR: Failure to load ErosEngine.dll.  Error code = " + hLoadLibResult);		// App unusable.  Study return code to find out what is wrong.
		Debug.Log("INIT: Succeeded in loading ErosEngine.dll");
				
		//####OBS? GameObject oGuiGO = GameObject.Find("iGUI");			//###TODO!!! Update game load status... ###IMPROVE: Async load so OnGUI gets called???  (Big hassle for that!)
		Debug.Log("0. Game Awake");	yield return new WaitForSeconds(nDelayForGuiCatchup);
		ErosEngine.Utility_Test_Return123_HACK();			// Dummy call just to see if DLL will load with Unity

		//=== Initialize our gBlender direct-memory buffers ===
		Debug.Log("1. Shared Memory Creation.");  //###???  new WaitForSeconds(nDelayForGuiCatchup);
		if (ErosEngine.gBL_Init(CGame.GetFolderPathRuntime()) == false)
			throw new CException("ERROR: Could not start gBlender library!  Game unusable.");
		
		//=== Spawn Blender process ===
		Debug.Log("2. Background Server Start.");  //###???  new WaitForSeconds(nDelayForGuiCatchup);	//###CHECK: Cannot wait long!!
		_hWnd_Unity = (IntPtr)GetActiveWindow();			// Just before we start Blender obtain the HWND of our Unity editor / player window.  We will need this to re-activate our window.  (Starting blender causes it to activate and would require user to alt-tab back to game!!)
		_oProcessBlender = CGame.LaunchProcessBlender();
		if (_oProcessBlender == null)
			throw new CException("ERROR: Could not start Blender!  Game unusable.");
		//_nWnd_Blender_HACK = (IntPtr)GetActiveWindow();

		//=== Start Blender (and our gBlender scripts).  Game cannot run without them ===
		Debug.Log("3. Client / Server Handshake.");  //###???  new WaitForSeconds(nDelayForGuiCatchup);
		if (ErosEngine.gBL_HandshakeBlender() == false)
			throw new CException("ERROR: Could not handshake with Blender!  Game unusable.");

		SetForegroundWindow(_hWnd_Unity);			// Set our editor / player back into focus (away from just-spawned Blender)
		
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

		_oFluid = gameObject.AddComponent<CFluid>();
		_oFluid.OnAwake();

		Application.targetFrameRate = _TargetFrameRate;			//###BUG!!! Why no effect????
		_DefaultJointSpringOld = _DefaultJointSpring;		//###OBS
		_DefaultJointDampingOld = _DefaultJointDamping;

		_oCamTarget = GameObject.Find("CCamTarget").GetComponent<CCamTarget>();		//###WEAK!!!
		_oCamTarget.OnStart();

		Debug.Log("8. Body Assembly.");  //###???  new WaitForSeconds(nDelayForGuiCatchup);		//###WEAK!!!

		_oFluid.OnStart();								//###CHECK: Keep interleave

		SetGameModeBasicInteractions(true);

		//=== Find the static scene colliders in 'SceneColliders' node and initialize them ===
		//Debug.Log("CGame.StartGame() Registering Static Colliders: " + s_aColliders_Scene.Length);
        if (s_aColliders_Scene != null)
		    foreach (CCollider oColStatic in s_aColliders_Scene)		// Colliders that are marked as static registered themselves to us in their Awake() so we can start and destroy them
			    oColStatic.OnStart();

		StartCoroutine(Coroutine_Update100ms());
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
		_DemoVersion = false;
		if (_DemoVersion)
			Debug.Log("Starting game in demo mode.");		//###TODO: Temp caption!

///		if (_bRunningInEditor == false)
///			CUtility.WndPopup_Create(EWndPopupType.LearnToPlay, null, "Online Help", 50, 50);	// Show the help dialog at start... ###BUG: Does not change the combo box at top!

		SetForegroundWindow(_hWnd_Unity);			//###WEAK: Can get rid of??


		//--- CGamePlay old init... ####CLEANUP
		//=== Create our publicly-editable41 properties for gameplay ===
		_oObj = new CObject(this, 0, typeof(EGamePlay), "Erotic9");		//###TEMP!!! Main game name in this low-importance GUI???
		_oObj.PropGroupBegin("", "", true);
		_oObj.PropAdd(EGamePlay.Pleasure,			"Pleasure",		30,		-100,	100,	"Amount of pleasure experienced by game characters.  Influences 'Arousal' (NOTE: Temporary game mechanism)", CProp.Local);	//###BUG with first setting
		_oObj.PropAdd(EGamePlay.Arousal,			"Arousal",		0,		0,		100,	"Current state of arousal from game characters.  Currently influence penis size.  (NOTE: Temporary game mechanism)", CProp.Local);
		_oObj.PropAdd(EGamePlay.PoseRootPos,		"Pose Root Position",typeof(EPoseRootPos), 0,	"Base location of pose root.  (e.g. on bed, by bedside, etc)", CProp.Local);
		_oObj.PropAdd(EGamePlay.PenisSize,			"Penis Size",	0,		0,		100,	"", CProp.Local | CProp.ReadOnly | CProp.Hide);
		_oObj.PropAdd(EGamePlay.PenisErectionMax,	"Erection",		0,		0,		100,	"", CProp.Local | CProp.ReadOnly | CProp.Hide);
		_oObj.PropAdd(EGamePlay.FluidConfig,		"Fluid Configuration", 0, "Display the properties of the Erotic9 fluid simulator.  (Advanced)", CProp.Local | CProp.AsButton);
		_oObj.FinishInitialization();

        //if (CGame.INSTANCE._GameMode == EGameModes.None)        //####TEMP
        //    return;

        //=== Create the body publicly-editable body definitions that can construct and reconstruct CBody instances ===
        CGame.INSTANCE._nNumPenisInScene_BROKEN = 0;

		//###NOTE: For simplification in pose files we always have two bodies in the scene with man/shemale being body 0 and woman body 1
		//CreateBody(1);		//###BUG!!!!!! Creating woman after man results in vagina softbody disconnecting because of init-time collision with penis!
		CreateBody(0);
		_aBodies[0].SelectBody();

		TemporarilyDisablePhysicsCollision();

		SetPenisInVagina(false);		// We initialize the penis in vagina state to false so vagina track colliders don't kick in at scene init

		if (CGame.INSTANCE._GameMode == EGameModes.Play)
			ScenePose_Load("Standing", false);          // Load the default scene pose

		//###TODO ##NOW: Init GUI first!!
		//iGUISmartPrefab_WndPopup.WndPopup_Create(new CObject[] { oObj }, "Game Play", 0, 0);		//###TEMP

		//Time.timeScale = 0.05f;		//###REVA ###TEMP   Gives more time for cloth to settle... but fix it some other way (with far stronger params??)

		ChangeGameMode(_GameModeAtStartup);				// Set the initial game mode as statically requested
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
	public virtual void Update() {														// The 'heartbeat of the whole game... *Everything* that happens at every frame is directly called from this function in a deterministic manner.  Greatly simplifies the game code!
		if (_GameIsRunning == false)				//###OPT: Get check out of update call and run infrequently?	###IMPROVE: Can use enabled/disabled intead???
			return;

		//Screen.showCursor = true;		//###BROKEN: Hide cursor!!			// We hide the hardware cursor at the beginning of every frame.  iGUI-based dialogs that need to show it will show it after (This way we can easily support multiple dialogs)
		if (_bCursorEnabled_HACK)
			_oCursor.OnUpdate_Cursor();											// All mouse processing / interactivity handled here

		//=== Store global key modifiers & mouse buttons for this game frame for efficiency ===
		_bKeyShift		= Input.GetKey(KeyCode.LeftShift)		|| Input.GetKey(KeyCode.RightShift);
		_bKeyControl	= Input.GetKey(KeyCode.LeftControl)		|| Input.GetKey(KeyCode.RightControl);
		_bKeyAlt		= Input.GetKey(KeyCode.LeftAlt)			|| Input.GetKey(KeyCode.RightAlt);
		_bMouseBtnRight = Input.GetMouseButton(1);


		//=== Process CKeyHook keys ===		//###WEAK!!: Switch to use input manager??			//###OPT: Possible to do one test before branching out for all keys??		//###LEARN: If in editor, game window *must* have focus for keys to be seen!!
		for (int nKeyHook = _aKeyHooks.Count - 1; nKeyHook >=0; nKeyHook--) {		// We iterate in reverse order to make it possible to remove while iterating.
			WeakReference oKeyHookRef = _aKeyHooks[nKeyHook];
			if (oKeyHookRef.IsAlive) {
				CKeyHook oKeyHook = oKeyHookRef.Target as CKeyHook;
				oKeyHook.OnUpdate();
			} else {
				_aKeyHooks.RemoveAt(nKeyHook);
			}
		}

		if (Input.GetKeyDown(KeyCode.F1))			//####TEMP?
			ChangeGameMode(EGameModes.Configure);
		if (Input.GetKeyDown(KeyCode.F2))
			ChangeGameMode(EGameModes.Play);

		//=== Process standard keys === ###IMPROVE: Switch them to CKeyHook???
		if (Input.GetKeyDown(KeyCode.CapsLock)) //###IMPROVE?
			SetGameModeBasicInteractions(!_bGameModeBasicInteractions);
		
		if (Input.GetKeyDown(KeyCode.Alpha0)) {
			TestCode1();
		}
		if (Input.GetKeyDown(KeyCode.Alpha9)) {
			TestCode2();
		}
		if (Input.GetKeyDown(KeyCode.Alpha8)) {
			TestCode3();
		}
		if (Input.GetKeyDown(KeyCode.Backslash)) {
			if (_bKeyControl)
				_ShowSysInfo = !_ShowSysInfo;
			else
				_ShowFPS = !_ShowFPS;
		}

		if (Input.GetKeyDown(KeyCode.K) && _bKeyControl)		// Ctrl+K = Disable default-layer colliders, Shift+Ctrl+K = Enable.  Useful for posing calibration
			Physics.IgnoreLayerCollision(0, 0, !_bKeyShift);


		if (Input.GetKeyDown(KeyCode.P)) {	//###IMPROVE: Display notification		###IMPROVE: Auto save in current scene pose with fully usable name!
			string sDateTime = System.DateTime.Now.ToString() + ".png";		//###IMPROVE: Auto hide of GUI
			sDateTime = sDateTime.Replace('/', '-');
			sDateTime = sDateTime.Replace(':', '.');
			string sPathFileCapture = GetPathScreenCaptures() + _sNameScenePose + " - " + sDateTime;
			Debug.Log("Saved screen capture file to " + sPathFileCapture);	//###IMPROVE: Save a .jpg
			Application.CaptureScreenshot(sPathFileCapture);
		}

		if (Input.GetKeyDown(KeyCode.H) && _bKeyControl) {		
			//if (_bKeyShift)
				///iGUICode_Root.INSTANCE._oGuiRoot.enabled = !iGUICode_Root.INSTANCE._oGuiRoot.enabled;	// Shift + Ctrl + H = Hide GUI layer (useful for screen captures)
			//else
				_oSceneMeshesGO.SetActive(!_oSceneMeshesGO.activeSelf);			// Ctrl + H = Hide scene toggle (useful for screen captures)
		}



		//=== CGamePlay former OnUpdate ===  ####CLEANUP
		float nTime = Time.time;

		//CGame.SetGuiMessage(EGameGuiMsg.Dev] = Time.deltaTime.ToString();

		//=== Adjust the global lust property.  This will in turn affect most erotic game parameters such as penis size, erection, etc ===
		float nPleasure = _oObj.PropGet(EGamePlay.Pleasure);
		if (nPleasure != 0) {
			float nOrgasm = _oObj.PropGet(EGamePlay.Arousal);
			float nTimeDeltaAsPercentToMaxOrgasm = Time.deltaTime / C_TimeToMaxOrgasm_OBSOLETE;		// How much we can increase lust at this game frame given max pleasure
			float nOrgasmIncrease = nTimeDeltaAsPercentToMaxOrgasm * nPleasure;
			nOrgasm += nOrgasmIncrease;
			_oObj.PropSet(EGamePlay.Arousal, nOrgasm);
		}

		//=== Re-enable collision temporarily disabled in Pose_Load() if time has elapsed ===
		if (_nTimeReenableCollisions != 0) {				//###IMPROVE: Do this by co-routine instead??
			if (nTime > _nTimeReenableCollisions) {
				Physics.IgnoreLayerCollision(0, 0, false);
				_nTimeReenableCollisions = 0;
			}
		}

		//=== Iterate through our PhysX-enabled objects to ask them to prepare themselves for this upcoming frame ===
		foreach (CBody oBody in _aBodies)
			if (oBody != null)
				oBody.OnSimulatePre();

		//=== Simulate PhysX2 for SoftBodies ===
		ErosEngine.PhysX2_SimulateFrame(Time.deltaTime);		// We simulate PhysX2 scene so that CBreastBase colliders that appear in PhysX3 scene can push away the cloth there with the softbody breast position of this time frame (expensive blocking call) 

		//=== Iterate through the bodies for any code that must run between PhysX2 and PhysX3 ===
		foreach (CBody oBody in _aBodies)
			if (oBody != null)
				oBody.OnSimulateBetweenPhysX23();

		//=== Simulate PhysX3 for everything else (Clothing, Fluid, etc) ===
		CGame.INSTANCE._nFluidParticleRemovedByOverflow_HACK = ErosEngine.PhysX3_SimulateFrame(Time.deltaTime);		// Simulate PhysX3 scene that includes everything except soft bodies. (expensive blocking call)

		CGame.INSTANCE._oFluid.OnSimulatePre();			//###MOVE!?!?!

		//=== Extract our information from the soft body simulation ===
		foreach (CBody oBody in _aBodies)
			if (oBody != null)
				oBody.OnSimulatePost();

		CGame.INSTANCE._nFrameCount_MainUpdate++;	//###HACK! To stats!

		//=== User animation speed / position multiplier updates ===
		if (Input.GetKeyDown(KeyCode.W))
			CGame.INSTANCE._nAnimMult_Time += 0.1f;
		if (Input.GetKeyDown(KeyCode.S))
			CGame.INSTANCE._nAnimMult_Time -= 0.1f;
		if (Input.GetKeyDown(KeyCode.A))
			CGame.INSTANCE._nAnimMult_Pos -= 0.1f;
		if (Input.GetKeyDown(KeyCode.D))
			CGame.INSTANCE._nAnimMult_Pos += 0.1f;
		if (Input.GetKeyDown(KeyCode.C)) {					// C = toggle cum on first character with penis.  Shift+C = toggle cum on first character without penis
			CBody oBodyCum = FindFirstCharacterWithPenis();
			if (CGame.INSTANCE._bKeyShift) 		// Toggle the proper ejaculation flag, and always turnning off the other sex
				oBodyCum = GetBodyOther(oBodyCum);
			CBody oBodyOther = GetBodyOther(oBodyCum);
			if (oBodyOther != null)
				oBodyOther.SetIsCumming(false);					// Always stop other body from cumming...
			if (oBodyCum != null)
				oBodyCum.SetIsCumming(!oBodyCum._bIsCumming);	// And toggle cum on the requested body
			if (oBodyCum._bIsCumming == false && oBodyOther._bIsCumming == false)	// If both bodies are not cumming stop the flow.
				CGame.INSTANCE._oFluid._oObj.PropSet(EFluid.EmitRate, 0);	//###BUG: Will crash if not 2 odies
			CGame.INSTANCE._nTimeStartOfCumming = Time.time;		// Reset the start of cum cycle so we start at the start
		}
		if (Input.GetKeyDown(KeyCode.V))  		// V = Stop all cumming and Wipe away cum (reset fluid)
			Cum_Stop();
		//if (Input.GetKeyDown(KeyCode.G))		//###TODO?
		//	_oObj.PropSet(EGamePlay.Pleasure, _oObj.PropGet(EGamePlay.Pleasure) == 0 ? 50 : 0);

		if (Input.GetKeyDown(KeyCode.BackQuote)) {		
			///if (CGame.INSTANCE._bKeyShift)				//###DESIGN: Join under one key?
				///iGUICode_Root.OnBtnClick_Poses(null);	// Shift+BackQuote = Load Pose
			///else
				///iGUICode_Root.OnBtnClick_Scenes(null);	// BackQuote = Load Scene
		}

		if (Input.GetKeyDown(KeyCode.Tab)) {		// Tab = Select other body.  Assumes a 'two body' scene like most of the code.
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

		if (Input.GetKeyDown(KeyCode.G) && _bKeyControl)		// Ctrl+G = Enter game key	//###IMPROVE: Game option!!
			if (_DemoVersion)		//###IMPROVE? This flag the real one for full game?
				new CDlgPrompter(true, "Game Activation", "Enter Game Key:", new CDlgPrompter.DelegateOnOk(CDlgPrompter.Delegate_OnDlgPrompterOk_Activation));  //PlayerPrefs.GetInt(G.C_PlayerPref_GameKey).ToString());		// Retrieve the key from PlayerPref (if set) when dialog control initializes
                                                                                                                                                                //else
                                                                                                                                                                //iGUI.iGUIRoot.alert("Message", "This computer is already activated.");
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
    }

    IEnumerator Coroutine_Update100ms() {			// Slow update coroutine to update low-priority stuff.
		for (; ; ) {
			if (_GameIsRunning) {					//####CHECK?
				//if (_oGui != null) {		//###HACK!!!
					//////_oGui.oLabGameMode.label.text = _aGuiMessages[(int)EGameGuiMsg.SelectedBody] + " - " + _aGuiMessages[(int)EGameGuiMsg.SelectedBodyAction];
				//}
			}
			yield return new WaitForSeconds(0.1f);
		}
	}
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

	public void SetGameModeBasicInteractions(bool bGameModeBasicInteractions) {
		_bGameModeBasicInteractions = bGameModeBasicInteractions;
		BroadcastMessageToAllBodies("OnBroadcast_HideOrShowHelperObjects", _bGameModeBasicInteractions == false);
		_oPoseRoot._oHotSpot.GetComponent<Renderer>().enabled = !_bGameModeBasicInteractions;		// Also show / hide pose root
		if (_bGameModeBasicInteractions)			// Force move mode on cursor if going to hidden helper objects (we only fast track move operations)
			_oCursor.SetEditMode(EEditMode.Move);
	}

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
	
	public void ChangeGameMode(EGameModes eGameMode) {
		if (_GameMode == eGameMode)
			return;
		
		foreach (CBody oBody in _aBodies)
			if (oBody != null)
				oBody.OnChangeGameMode(eGameMode, _GameMode);

		_GameMode = eGameMode;
		
		//switch (_GameMode) {
		//	case EGameModes.Play:
		//		break;
		//	case EGameModes.Configure:
		//		break;
		//}
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
		CGame.INSTANCE._oFluid.ResetFluid();
	}
	public void BroadcastMessageToAllBodies(string sFunction, object oArg) {			//###IMPROVE: Redo all broadcasting through this approach!
		foreach (CBody oBody in _aBodies)
			if (oBody != null)
				oBody._oBodyRootGO.BroadcastMessage(sFunction, oArg);
		CGame.INSTANCE._oPoseRoot.gameObject.BroadcastMessage(sFunction, oArg);		// Also broadcast to the pose root which keeps many helper objects
	}

	public void TestCode1() {			//####IMPROVE: To helper keys... more scales
		Time.timeScale = (Time.timeScale == 1.0f) ? 0.05f : 1.0f;		//###REVA ###TEMP
		Debug.Log("TimeScale=" + Time.timeScale.ToString());
	}		
	public void TestCode2() {}		
	public void TestCode3() {}
	#endregion



	//---------------------------------------------------------------------------	PROCESS CREATION

	public static System.Diagnostics.Process LaunchProcess(string sFileProcess, string sArguments, bool bRedirectOutput, System.Diagnostics.ProcessWindowStyle eWinStyle = System.Diagnostics.ProcessWindowStyle.Minimized) {
		System.Diagnostics.Process oProc = new System.Diagnostics.Process();
		oProc.StartInfo.FileName = sFileProcess;
		oProc.StartInfo.Arguments = sArguments;
		//oProc.StartInfo.CreateNoWindow = true;		// Does anything useful??
		oProc.StartInfo.UseShellExecute = true;		// Must be false to redirect below.			//###LEARN: Can't seem to get redirct working with Blender (Test with another app)  Could be that we cannot redirect Blender.  use Shell Execute so at least user can see damn console for errors!!!
		if (bRedirectOutput) {
			oProc.StartInfo.RedirectStandardOutput = true;	//###IMPROVE!!!!: Redirect in/out/err and benefit from this to catch Blender errors! ###SOON
			oProc.StartInfo.RedirectStandardError = true;	//###LEARN: Holy crap does .Net ever rule... you'd have to shoot me doing this in C++!
			//oProc.StartInfo.RedirectStandardInput = true;	//###IMPROVE!!!! Possibly to feed commands to Blender via its console-based Python console?  Would be ultra cool!!  ###RESEARCH!!!!!
		}
		oProc.StartInfo.WindowStyle = eWinStyle;
		Debug.Log(string.Format("CGame.LaunchProcess() file='{0}'  args='{1}'", sFileProcess, sArguments));
		oProc.Start();			//###IMPROVE: Many powerful features in this class!!!!
		//ShowWindow(oProc.MainWindowHandle, 2);
		return oProc;		//###CHECK: Will above throw if there was an error??  We need to throw if error!
	}

	public static System.Diagnostics.Process LaunchProcessBlender(System.Diagnostics.ProcessWindowStyle eWinStyle = System.Diagnostics.ProcessWindowStyle.Minimized) {
		string sFileProcess = CGame.GetPathBlenderApp();
		string sArguments = string.Format("\"{0}/Main.blend\" --enable-autoexec", CGame.GetPathBlends());	//--start-console
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
		if (_aBodies[0] != null)
			_aBodies[0]._oHeadLook.AddLookTargetsToOtherBody(null);
		if (_aBodies[1] != null)
			_aBodies[1]._oHeadLook.AddLookTargetsToOtherBody(null);

		//=== Destroy the entire tree of object a body is by simply destroying its root gameObject!  (Needs DestroyImmediate as lazy Destroy causes rebuild problems)
		if (_aBodies[nBodyID] != null)
			_aBodies[nBodyID].Destroy();
		_aBodies[nBodyID] = null;
		_aBodies[nBodyID] = new CBody(nBodyID);
		_aBodies[nBodyID].DoInitialize();

        //####HACK!!!!
        if (_aBodies[0] != null)
            _aBodies[0]._oHeadLook.AddLookTargetsToOtherBody(null);
        if (_aBodies[1] != null)
            _aBodies[1]._oHeadLook.AddLookTargetsToOtherBody(null);

        //=== Reconnect the head look controller to look at the other body only if the scene now has two valid bodies ===
        if (_aBodies[0] != null && _aBodies[1] != null) {
			_aBodies[0]._oHeadLook.AddLookTargetsToOtherBody(_aBodies[1]);
			_aBodies[1]._oHeadLook.AddLookTargetsToOtherBody(_aBodies[0]);
		}
		//CGame.SetGuiMessage(EGameGuiMsg.GameStatus] = null;				//###BROKEN!!  How to display before lenghty blocking calls below??? CGame.SetGuiMessage(EGameGuiMsg.GameStatus] = "Rebuilding Body.  Please Wait...";
	}

	public void TemporarilyDisablePhysicsCollision(float nTimeInSec = 2.0f) {	//###TUNE		// Disable collisions on default layers so bones can go through each other for a small amount of time to avoid tangling.
		Physics.IgnoreLayerCollision(0, 0, true);			//###IMPROVE: Consider a coroutine??
		_nTimeReenableCollisions = Time.time + nTimeInSec;
	}

	public void SetPenisInVagina(bool bPenisInVagina) {		// When penis tip meets the vagina trigger collider (runs from entrance to deep inside), vagina guide track is activated, penis ceases to create dynamic colliders in CBodyCol and fluid only collides with vagina guide track
		if (CGame.INSTANCE._DemoVersion)		//###WEAK!!!! ###TEMP!!!
			_bPenisInVagina  = false;		// Demo build does not allow penetration...
		else
			_bPenisInVagina = bPenisInVagina;
		//CGame.INSTANCE._sGuiString_Dev_DEBUG = "_bPenisInVagina = " + _bPenisInVagina;
		if (_aBodies[1] != null && _aBodies[1]._oVagina != null)
			_aBodies[1]._oVagina.VaginaGuideTrack_EnableDisable(_bPenisInVagina);		//###WEAK? Body 0/1 design issues??
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
				_aBodies[nBody].Pose_Load(sPoseName);
				_aBodies[nBody]._oActor_Base.transform.localPosition = vecBase;
				_aBodies[nBody]._oActor_Base.transform.localRotation = quatBase;
				nBody += nBodyInc;
				return;  // Only load first body			//####REVB  ####TEMP: FOr single body load
			}
			oStreamRead.Close();
		} else {
			Debug.LogError("Scene_Load() cannot find file " + _sNameScenePose);
		}
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
        //####TEMP
		//GameObject oPosRootPosGO = GameObject.Find("Resources/EPoseRootPos/" + ePoseRootPos.ToString());
		//CGame.INSTANCE._oPoseRoot.transform.position = oPosRootPosGO.transform.position;
		//TemporarilyDisablePhysicsCollision();
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
		if (nValueNew == 1)
			CUtility.WndPopup_Create(EWndPopupType.PropertyEditor, new CObject[] { CGame.INSTANCE._oFluid._oObj }, "Fluid Configuration", 0, 0);
	}
	public void OnPropSet_PoseRootPos(float nValueOld, float nValueNew) {		// Set the pose root from the user-selected pose root.
		EPoseRootPos ePoseRootPos = (EPoseRootPos)nValueNew;		// Scene 3D positions to act as 'root' for the CPoseRoot.  MUST match content of folder node Resources/EPoseRootPos!
		Scene_ApplyPoseRoot(ePoseRootPos);
	}

	//--------------------------------------------------------------------------	IObject interface

	public void OnPropSet_NeedReset(CProp oProp, float nValueOld, float nValueNew) { }			//###DESIGN!!!!: GET RID OF THIS! Called when a property created with the 'NeedReset' flag gets changed so owning object can adjust its global state







	//---------------------------------------------------------------------------	FOLDERS

	public static string GetFolderPathRuntime() {					// Returns the path to the Erotic9 root directory, whether we are an editor build or a player build  ###NOTE: Assumes the editor build and player build are in the same parent directory!!!
		string sNameFolder = Application.dataPath;
		string sPathSuffixToRemove = (Application.isEditor) ? "Unity/Assets" : "Erotic9_Data";			// Application.dataPaty returns a string like "D:/Src/E9/Erotic9/Erotic9_Data" in player but "D:/Src/E9/Unity/Assets" in editor.  We convert this into a string like "D:\Src\E9\Erotic9" for both builds for constant directory access
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
	//public static string GetPathBlenderApp()			{ return "D:/Dev/Blender/blender-build/bin/Release/blender.exe"; }	//###HACK!!!
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

