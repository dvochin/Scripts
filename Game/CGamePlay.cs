using UnityEngine;
using System;
using System.IO;


public class CGamePlay : MonoBehaviour, IObject {
	public 	CObject			_oObj;							// The user-configurable object representing the game
	public	CBody[]			_aBodies = new CBody[2];		// Our collection of bodies.  Created from the specs in corresponding entry in _aObjBodyDefs.
	public	float			_nTimeReenableCollisions;		// Time when collisions should be re-enabled (Temporarily disabled during pose load)

	const float				C_TimeToMaxOrgasm_OBSOLETE = 20;			// How many seconds it takes to reach maximum lust if maximum pleasure is always on	###TUNE

	public  bool			_bPenisInVagina;			//###DESIGN!!: ###MOVE?? Belongs here?? Prevents penis from generating dynamic CBodyCol colliders to repell it.  Set when entering vagina

	public string			_sNameScenePose;				// The currently loaded scene pose
	public bool				_bScenePoseFlipped;             // If set the scene pose is 'flipped' (i.e. Pose for body a loaded into body b and vice versa)

	public CGamePlay() {		//###IMPROVE: Merge with CGame coroutine to display updates as body are constructed??
		Debug.Log("=== Entering Game Play Mode ===");
		CGame.INSTANCE._oGamePlay = this;		//###WEAK: We must fill in our pointer in owning CGame so code below can find us from global CGame

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

        if (CGame.INSTANCE._GameMode == EGameModes.None)        //####TEMP
            return;

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
	}

	public void OnDestroy() {
		Debug.Log("--- Leaving Game Play Mode ---");
		foreach (CBody oBody in _aBodies) {
			if (oBody != null) {
				oBody.Destroy();
				//####CHECK: Any other node to destroy??
			}
		}
		//_aBodies = null;				//####CHECK: 
	}

	public void OnUpdate() {		//###DESIGN!!!! ###WEAK: Important function that other game modes would need to replicate!!
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
	}

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
}

public enum EPoseRootPos {		// Scene 3D positions to act as 'root' for the CPoseRoot.  MUST match content of folder node Resources/EPoseRootPos!
	BedFront,
	BedFrontFrame,
	BedCorner,
	BedTop,
}
