/*###DISCUSSION: CBody rewrite
=== TODAY ===

=== NEXT ===

=== TODO ===
- Blender CBody redo
    - Only three sexes: Woman, Man, Shemale.  Remove argument for genitals
    - Now reacts to 'OnChangeGameMode' to properly configure / cleanup
- Changing character sex requires going back to central menu (which cleans everything)
- Rem out the whole breast morph thing now?
- Rename FlexSkin to represent softbody more?
- Shemale (and possibly male if not DAZ penis) needs sophisticated penis vert adjustment after morphs
- Do we still need to map verts between the different bodies??
- Q: Body in configure mode COULD move around and animate!  It just doesn't have softbodies!  +++++
    - Still has cloth, Flex collider (different ones between the two modes), pins, head movement, etc
    - Still a large visual difference to user (scene dissapears, one body only, body in T) but body could be 'tested' in that mode with some animations??
- Separate pose / animated mode versus body state = orthogonal

- What is the same
    - Actors and bones are there, same with scripting objects, same prefab, canvas panels
    - Can still go between pose mode and animated mode

- What changes:
    - Only hotspot in configure is main one
    - Flex colliders are different: 
        - Configure is most body without head and extremities at natural geometry
        - Play is remeshed, does not contain removed softbodies and has head and feet (hands are softbodies?)
    - 


=== LATER ===

=== IMPROVE ===

=== DESIGN ===

=== QUESTIONS ===

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===

=== PROBLEMS??? ===

=== WISHLIST ===

*/

/*###DISCUSSION: Dual-mode game functionality: Morphing and gameplay
=== REWRITE TODO ===
- Finally get fucking mesh in... too many vert groups per bone! (Because bad body morph??)
- Continue CBody ctor to split everything
- Switch to real body... work on other meshes

=== NEXT ===

=== TODO ===

=== LATER ===

=== IMPROVE ===

=== DESIGN ===
- Best possible design would be:
	- Not requring expensive rebuild for anything other than sex change
		- Store fully-realized bodies (man, woman, shemale) in Blender file??
			- Store as a cache and save?  (Could get too many when we have many genitals, bodies)
		- Rebuild of softbody a part of the complexity... could push morph to soft bodies and have them re-create their PhysX2 objects??
	-? Having both morphing and simulated bodies on screen?

- Considerations:
	- How to morph shemale & man because of penis attach and underwear design???
	- Morphing of cloth around cock basically means softbody and penis collider created as usual: Breast different!
	- Keeping CBody separate simplifies hotspot management, game actions, etc.
	- For real morphing flexibility we also have to take body col and cloth collider in account!!

- Ideas:
	- Don't even have our current (limited) morphing body... Apply morphs to real body all the time?
		- Morphs have to traverse detached body parts like breasts and penis
		- In this mode soft bodies would be rigid... coming out they become simulated.
		- Hotspots and actors a problem?  (easy to fix??)

- Strategy:
	- We have a go to reuse CBody current runtime architecture with the softbodies 'disabled' and reloadable from Blender...
		- We need a wholistic way to morph the source body (with penis / vagina) attached and have all the parts update themselves.
		- (This way we can apply morphs that affect several morphed parts)
	- 1. Assemble final 'morph body' -> proper penis, vagina put in.
	- 2. Before parts are detached, create a layer on the morph body so detached parts can update themselves.
	- 3. When a morph is applied to morph body, all detached parts are requested to update themselves.
	- 4. Collider meshes (body col, cloth col, breast col) updates themselves last from their detached part.

=== QUESTIONS ===
- Decide on Blender body storage... fully realized already?  (saves time)

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===

=== PROBLEMS??? ===

=== WISHLIST ===

*/


using UnityEngine;
using System.IO;
using System.Collections.Generic;



public class CBody : IHotSpotMgr { 		// Manages a 'body':  Does not actually represent a mesh (most of the skinned body represtend by _oBodySkinnedMesh;
	// The main purpose of this class is to act as a bridge between the bone structure used for animation of the main skinned mesh and enable our connected soft-body parts to move along with us via our rim mesh and the pins it updates with PhysX

	public CBodyBase			_oBodyBase;             // Our owning body base.  Responsible to create / destroy us as player nagivates between configure / play mode.
	public bool                 _bEnabled;				// Body is 'enabled'.  Its meshes are visible and it receives CPU cycles.  (Used to disable 'other bodies' when one body is being configured by the player)
	//---------------------------------------------------------------------------	VISIBLE PROPERTIES
	public CObject				_oObj;					// The user-configurable object representing this body.
	public CBSkin				_oBodySkinnedMesh;		// The main skinned mesh representing most of the body (e.g. everything except detached soft body parts, head, hair, clothing, etc)
    public CBSkin				_oBodyFlexCollider;     // A reduced-geometry re-mesh of the skinned body generated by Blender that forms the skinned particles responsible to push away other Flex particles from body.
    ///public  CHeadLook		_oHeadLook;             // Object in charge of moving head toward points of interest
	public bool					_bIsCumming;            // Character is currently cumming.  Set globally
    public GameObject			_oHairGO;               // Reference to the hair game object we mount on head at init.

	//---------------------------------------------------------------------------	IMPLEMENTATION MEMBERS
	public CHotSpot				_oHotSpot;				// Hotspot at the head of the character.  Enables user to change the important body properties by right-clicking on the head
	//public CBSkinBaked 		_oBSkinRim;				//###DESIGN!!!! Rename the old 'rim'?
	public CScriptPlay			_oScriptPlay;			// Scripting interpreter in charge of loading user scripts that programmatically control this body (poses also load this way)
	public string				_sNamePose;				// The name of the current loaded pose (Same as filename in /Poses directory)

	//---------------------------------------------------------------------------	SOFT BODY PARTS
	public CBreastL 		    _oBreastL;					// The left and right breasts as softbodies
	public CBreastR 		    _oBreastR;
	public CPenis				_oPenis;
    //public CVagina				_oVagina;
    public List<CSoftBody>	    _aSoftBodies	= new List<CSoftBody>();		// List of all our _oSoftBodiesXXX above... used to simplify iterations.

	//---------------------------------------------------------------------------	CLOTHING		//###DESIGN: ###CHECK?
	public List<CBCloth>		_aCloths		= new List<CBCloth>();	//###OBS14:???  Belongs to base??	// List of our simulated cloth.  Used to iterate during runtime

	//---------------------------------------------------------------------------	ACTORS
	public CActorNode			_oActor_Base;			// The smart 'actors' associated with our body.  Adds much intelligent functionality!
	public CActorNode			_oActor_Torso;
	public CActorPelvis			_oActor_Pelvis;
	public CActorChest			_oActor_Chest;
	public CActorArm 			_oActor_ArmL;
	public CActorArm 			_oActor_ArmR;
	public CActorLeg 			_oActor_LegL;
	public CActorLeg 			_oActor_LegR;
	public List<CActor>			_aActors = new List<CActor>();		// An array containing all the _oActor_xxx elements above.  Used to simplify iterations.

	//---------------------------------------------------------------------------	SCRIPT ACCESS
	public CObject				 Base;		// Flattened references to the oObj CObject member of our actors.  Done to offer scripting runtime simplified access to our scriptable members using friendlier names.
	public CObject				 Torso;
	public CObject				 Chest;
	public CObject				 Pelvis;
	public CObject				 ArmL;
	public CObject				 ArmR;
	public CObject				 LegL;
	public CObject				 LegR;
	public CObject				 Face;
	public CObject				 Penis;

	//---------------------------------------------------------------------------	KEYBOARD HOOKS
	//CKeyHook _oKeyHook_PenisBaseUpDown;
	//CKeyHook _oKeyHook_PenisShaftUpDown;
	//CKeyHook _oKeyHook_PenisDriveStrengthMax;
	//CKeyHook _oKeyHook_ChestUpDown;

	//---------------------------------------------------------------------------	MISC
	public GameObject _oBodySkinnedMeshGO_HACK;        // Reference to game object containing main skinned body.  Kept as a member because of complex init tree needing this before '_oBodySkinnedMesh' is set!
	public CUICanvas[] _aUICanvas = new CUICanvas[2];           // The UI canvases that display various user interface panels to provide end-user edit capability on this body.  One for each left / right.
	///CActorArm _oArm_SettingRaycastPin;      // The arm we are currently searching for raycasting hand target (when user placing hands)


    public CBody(CBodyBase oBodyBase) {		//###NOW11:
		_oBodyBase = oBodyBase;
		_oBodyBase._oBody = this;           //###WEAK13: Convenience early-set of our instance into owning parent.  Needed as some of init code needs to access us from our parent! ###DESIGN!
		_oBodySkinnedMeshGO_HACK = new GameObject("RuntimeBody");       // Create the game object that will contain our important CBody component early (complex init tree needs it!)
		_oBodySkinnedMeshGO_HACK.transform.SetParent(_oBodyBase._oBodyRootGO.transform);
		_bEnabled = true;			// Enabled at creation by defnition.

		//bool bForMorphingOnly = false;  //###JUNK (CGame.INSTANCE._GameMode == EGameModes.MorphNew_TEMP);				//####DEV!!!!
		Debug.Log(string.Format("+ Creating body #{0}", _oBodyBase._nBodyID));

		_oObj = new CObject(this, "Body", "Body");
		CPropGrpEnum oPropGrp = new CPropGrpEnum(_oObj, "Body", typeof(EBodyDef));
		oPropGrp.PropAdd(EBodyDef.BreastSize,		"Breast Size BROKEN",		1.0f, 0.5f, 2.5f, "");		//###BROKEN11:
		_oObj.FinishInitialization();


		//=== Tell Blender to create our CBody instance for us ===
		CGame.gBL_SendCmd("CBody", _oBodyBase._sBlenderInstancePath_CBodyBase + ".CreateCBody()");


        //===== DETACHED SOFTBODY PARTS PROCESSING =====
        if (_oBodyBase._eBodySex != EBodySex.Man) {
			// CSoftBodySkin.Create(this, typeof(CVagina), "chestUpper/chestLower");		//###DEV19: bone?
			//_aSoftBodies.Add(_oBreastL = (CBreastL)CSoftBody.Create(this, typeof(CBreastL), "chestUpper"));
			//_aSoftBodies.Add(_oBreastR = (CBreastR)CSoftBody.Create(this, typeof(CBreastR), "chestUpper"));
			COrificeRig oOR = new COrificeRig(this);
        }
        if (_oBodyBase._eBodySex == EBodySex.Woman) {
            //_aSoftBodies.Add(_oVagina = (CVagina)CSoftBody.Create(this, typeof(CVagina), "chest/abdomen/hip"));
        } else {
            //_aSoftBodies.Add(_oPenis = (CPenis)CSoftBody.Create(this, typeof(CPenis), "chest/abdomen/hip"));
        }

		////####TEMP ####DESIGN ####TEMP ####MOVE		###DESIGN18: Flaw in auto-deletion means cloths must be named differently between cut-time versus play time!
		//_aCloths.Add(CBCloth.Create(_oBodyBase, "MyShirtPLAY", "Shirt", "BodySuit", "_ClothSkinnedArea_ShoulderTop"));    //###HACK18:!!!: Choose what cloth to edit from GUI choice  ###DESIGN: Recutting from scratch??  Use what design time did or not??
		



		//===== MAIN SKINNED BODY PROCESSING =====
		//=== Get the main body skinned mesh (has to be done once all softbody parts have been detached) ===
        _oBodySkinnedMesh = (CBSkin)CBMesh.Create(_oBodySkinnedMeshGO_HACK, _oBodyBase, ".oBody.oMeshBody", typeof(CBSkin)); //###IMPROVE13: Create blender instance string for our CBody?
		_oBodySkinnedMesh.name = _oBodyBase._sBodyPrefix + "-SkinnedBody";
		//_oBodySkinnedMesh.GetComponent<SkinnedMeshRenderer>().enabled = false;

		//=== Create a hotspot at the character's head the user can use to invoke our (important) context menu ===
		//###BROKENN
		//      Transform oHeadT = FindBone("chest/neck/head");         // Our hotspot is located on the forehead of the character.
		//_oHotSpot = CHotSpot.CreateHotspot(this, oHeadT, this._sHumanCharacterName, false, new Vector3(0, 0.09f, 0.03f), 2.0f);     //###IMPROVE: Get these offsets from pre-placed nodes on bone structure??

		////=== Create the head look controller to look at parts of the other body ===
		//###BROKENN
		//      _oHeadLook = _oBodyRootGO.gameObject.AddComponent<CHeadLook>();			//####DESIGN: Keep for morph mode??
		//_oHeadLook.OnStart(this);


		//=== Instantiate the requested hair and pin as child of the head bone ===
		//###BROKENN
		//EBodyHair eBodyHair = (EBodyHair)_oObj.PropGet(EBodyDef.Hair);
		//if (eBodyHair != EBodyHair.None) {
		//	string sNameHair = "HairW-" + eBodyHair.ToString();         //###HACK!!! W extension!		###HACK!!!! Man support!!
		//	GameObject oHairTemplateGO = Resources.Load("Models/Characters/Woman/Hair/" + sNameHair + "/" + sNameHair, typeof(GameObject)) as GameObject;   // Hair has name of folder and filename the same.	//###HACK: Path to hair, selection control, enumeration, etc
		//	_oHairGO = GameObject.Instantiate(oHairTemplateGO) as GameObject;
		//	Transform oBoneHead = FindBone("chest/neck/head");
		//	_oHairGO.transform.parent = oBoneHead;
		//	_oHairGO.transform.localPosition = Vector3.zero;
		//	_oHairGO.transform.localRotation = Quaternion.identity;
		//	if (_eBodySex == EBodySex.Man) {            //###HACK!!!!!! To reuse messy hair for man!!
		//		_oHairGO.transform.localPosition = new Vector3(0, 0.0f, 0.0f);
		//		_oHairGO.transform.localScale = new Vector3(1.07f, 1.07f, 1.08f);
		//	}
		//}

		Body_InitActors();

		////=== Setup the keys to handle penis control on non-woman bodies ===
		//if (_eBodySex != EBodySex.Woman) {      //###MOVE?        ###OBS ###F
		//	bool bSelectedBodyOnly = (CGame.INSTANCE._nNumPenisInScene_BROKEN > 1);    // Keys below are active if this body is the selected body ONLY if we have more than one man in the scene
		//	if (_oPenis != null) {
		//		_oKeyHook_PenisBaseUpDown = new CKeyHook(_oPenis._oObjDriver.PropFind(EPenis.BaseUpDown), KeyCode.Q, EKeyHookType.QuickMouseEdit, "Penis up/down", 1, bSelectedBodyOnly);
		//		_oKeyHook_PenisShaftUpDown = new CKeyHook(_oPenis._oObjDriver.PropFind(EPenis.ShaftUpDown), KeyCode.E, EKeyHookType.QuickMouseEdit, "Penis bend up/down", 1, bSelectedBodyOnly);
		//		_oKeyHook_PenisDriveStrengthMax = new CKeyHook(_oPenis._oObjDriver.PropFind(EPenis.DriveStrengthMax), KeyCode.G, EKeyHookType.QuickMouseEdit, "Penis erection", -1, bSelectedBodyOnly);
		//	}
		//}
		//_oKeyHook_ChestUpDown = new CKeyHook(_oActor_Chest._oObj.PropFind(0, EActorChest.Torso_UpDown), KeyCode.T, EKeyHookType.QuickMouseEdit, "Chest forward/back");

		//=== Reparent our actor base to the pose root so that user can move / rotate all bodies at once ===
		_oActor_Base.transform.SetParent(CGame.INSTANCE._oPoseRoot.transform);		//###DESIGN15:! Causes problems with regular init/destory of CBody?  Do we really want to keep reparenting for easy full pose movement??>
		_oActor_Base.gameObject.name = _oBodyBase._sBodyPrefix + "_Base";

		//=== Copy references to our actors to our script-friendly CObject variables to provide friendlier access to our scriptable objects ===
		Base    = _oActor_Base._oObj;              // CGamePlay passed us the reference to the right (empty) static object.  We fill it here.
		Chest   = _oActor_Chest._oObj;
		Torso   = _oActor_Torso._oObj;
		Pelvis  = _oActor_Pelvis._oObj;
		ArmL    = _oActor_ArmL._oObj;
		ArmR    = _oActor_ArmR._oObj;
		LegL    = _oActor_LegL._oObj;
		LegR    = _oActor_LegR._oObj;
        Face = null;/// _oFace._oObj;
		///###BROKEN ###DEVO Penis = (_oPenis == null) ? null : _oPenis._oObjDriver;

		_oScriptPlay = CUtility.FindOrCreateComponent(_oBodyBase._oBodyRootGO.transform, typeof(CScriptPlay)) as CScriptPlay;
		_oScriptPlay.OnStart(this);

		//=== Rotate the 2nd body toward the first and separate slightly loading poses doesn't pile up one on another ===	####MOVE? ####OBS? (Depend on pose?)
		//####PROBLEM: Set separation and don't do in all game modes
		if (_oBodyBase._nBodyID == 0) {
			_oActor_Base.transform.position = new Vector3(0, 0, -CGame.C_BodySeparationAtStart);        //###DESIGN: Don't load base actor instead??  ###BUG Overwrites user setting of base!!!!
		} else {
			_oActor_Base.transform.position = new Vector3(0, 0, CGame.C_BodySeparationAtStart);
			_oActor_Base.transform.rotation = Quaternion.Euler(0, 180, 0);      // Rotate the 2nd body 180 degrees
		}

        ///_oClothEdit_HACK = new CClothEdit(this, "Shirt");

        ///CGame.gBL_SendCmd("CBody", "CBodyBase_GetBodyBase(" + _oBodyBase._nBodyID.ToString() + ").Breasts_ApplyMorph('RESIZE', 'Nipple', 'Center', 'Wide', (1.6, 1.6, 1.6, 0), None)");       //###F ###HACK!!!

        //=== Create the left and right canvases on each side of the body so that panels have a place to be pinned for close-to-body editing ===
        _aUICanvas[0] = CUICanvas.Create(CUtility.FindChild(_oActor_Chest.transform, "_CanvasPin_Left"));        //###HACK
        _aUICanvas[1] = CUICanvas.Create(CUtility.FindChild(_oActor_Chest.transform, "_CanvasPin_Right"));        //###DESIGN: Which pin?  Torso or chest?


		//=== Create the Flex collider in Blender and serialize it to Unity ===
		CGame.gBL_SendCmd("CBody", _oBodyBase._sBlenderInstancePath_CBodyBase + ".oBody.CreateFlexCollider(" + CGame.INSTANCE.nDistFlexColliderShrinkMult + ")");
		_oBodyFlexCollider = (CBSkin)CBMesh.Create(null, _oBodyBase, ".oBody.oMeshFlexCollider", typeof(CBSkin));
		_oBodyFlexCollider.name = _oBodyBase._sBodyPrefix + "-BodyFlexCollider";
		_oBodyFlexCollider.transform.SetParent(_oBodySkinnedMesh.transform);		//###IMPROVE15: Put this common re-parenting and re-naming in Create!
		_oBodyFlexCollider.gameObject.AddComponent<CFlexSkinnedBody>();
		_oBodyFlexCollider.GetComponent<SkinnedMeshRenderer>().enabled = false;     //###IMPROVE: Move into CFlexSkinnedBody??
		_oBodyFlexCollider.UpdateNormals();			//###NOW15:


		//_oBodySkinnedMesh._oSkinMeshRendNow.enabled = false;		//###TEMP19:
	}

	public CUICanvas FindClosestCanvas() {				// Find the closest body canvas to the camera.  Used to insert new GUI panel at most appropriate editing spot left of right of body.
		float nDistToCamClosest = float.MaxValue;
		CUICanvas oCanvasClosest = null;
		foreach (CUICanvas oCanvas in _aUICanvas) {
			float nDistToCam = Vector2.Distance(oCanvas.transform.position, Camera.main.transform.position);
			if (nDistToCamClosest > nDistToCam) {
				nDistToCamClosest = nDistToCam;
				oCanvasClosest = oCanvas;
			}
		}
		return oCanvasClosest;
	}

	public void Body_InitActors() {
		//=== Fetch the body part components, assign them to easy-to-access variables (and a collection for easy iteration) and initialize them by telling them their 'side' ===
		_aActors.Add(_oActor_Base	= CUtility.FindChild(_oBodyBase._oBodyRootGO.transform, "Base")					.GetComponent<CActorNode>());
		_aActors.Add(_oActor_Torso	= CUtility.FindChild(_oBodyBase._oBodyRootGO.transform, "Base/Torso")			.GetComponent<CActorNode>());
		_aActors.Add(_oActor_Chest	= CUtility.FindChild(_oBodyBase._oBodyRootGO.transform, "Base/Torso/Chest")		.GetComponent<CActorChest>());
		_aActors.Add(_oActor_Pelvis	= CUtility.FindChild(_oBodyBase._oBodyRootGO.transform, "Base/Torso/Pelvis")	.GetComponent<CActorPelvis>());
		_aActors.Add(_oActor_ArmL	= CUtility.FindChild(_oBodyBase._oBodyRootGO.transform, "Base/ArmL")			.GetComponent<CActorArm>());
		_aActors.Add(_oActor_ArmR	= CUtility.FindChild(_oBodyBase._oBodyRootGO.transform, "Base/ArmR")			.GetComponent<CActorArm>());
		_aActors.Add(_oActor_LegL	= CUtility.FindChild(_oBodyBase._oBodyRootGO.transform, "Base/LegL")			.GetComponent<CActorLeg>());
		_aActors.Add(_oActor_LegR	= CUtility.FindChild(_oBodyBase._oBodyRootGO.transform, "Base/LegR")			.GetComponent<CActorLeg>());

		foreach (CActor oActor in _aActors)
			oActor.OnStart(this);
	}

	public void Destroy() {             //###TODO15: ###OBS? Needed still with DoDestory()  Merge!
		//Debug.Log("CBody.OnDestroy(): " + _oBodyBase._sBodyPrefix);
		//foreach (CActor oActor in _aActors)
		//	GameObject.DestroyImmediate(oActor);

		//GameObject.DestroyImmediate(_oActor_Base.gameObject);          // Destroy our body's entry in top-level CPoseRoot

		////if (_oKeyHook_PenisBaseUpDown != null) _oKeyHook_PenisBaseUpDown.Dispose();     //###WEAK!!! Try to enable auto-dispose!!
		////if (_oKeyHook_PenisShaftUpDown != null) _oKeyHook_PenisShaftUpDown.Dispose();
		////if (_oKeyHook_PenisDriveStrengthMax != null) _oKeyHook_PenisDriveStrengthMax.Dispose();
		//if (_oKeyHook_ChestUpDown != null) _oKeyHook_ChestUpDown.Dispose();

		//foreach (CBCloth oBCloth in _aCloths)
		//	GameObject.DestroyImmediate(oBCloth.gameObject);
	}

	
	//---------------------------------------------------------------------------	UPDATE

	public void OnSimulatePre() {
   //     if (_oBSkinRim != null)
			//_oBSkinRim.OnSimulatePre();					// The very first thing we do is to get our rim to update itself so all pin positions for this frame will be refreshed...

		///_oHeadLook.OnSimulatePre();

		////=== Arm pinning key state preocessing ===  Space = set close to camera arm, LeftAlt = faraway from camera arm
		//if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftAlt)) {		// If we begin press to its key we begin the mode, optionally getting an arm to update.  we keep updating while the key is pressed and end the state on key up
		//	if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftAlt)) {
		//		CActorArm oArm = FindCloseOrFarArmFromCamera(Input.GetKeyDown(KeyCode.LeftAlt));	
		//		if (oArm != null) {
		//			_oArm_SettingRaycastPin = oArm;
		//			_oArm_SettingRaycastPin.ArmRaycastPin_Begin();
		//		}
		//	}
		//	if (_oArm_SettingRaycastPin != null)
		//		_oArm_SettingRaycastPin.ArmRaycastPin_Update();
		//}
		//if (_oArm_SettingRaycastPin != null && (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.LeftAlt))) {	//###LEARN: When GetKeyUp is true GetKey is false!
		//	_oArm_SettingRaycastPin.ArmRaycastPin_End();
		//	_oArm_SettingRaycastPin = null;
		//}

		_oActor_ArmL.OnSimulatePre();		//###OBS? Arms need per-frame update to handle pinning to bodycol verts
		_oActor_ArmR.OnSimulatePre();

		//=== Execute ejaculation if selected body and the pertinent global property is set ===//###BUG!!!!: Won't correctly process two of the same sex!!!
		//if (_bIsCumming) {        ###DEVO ###OBS: Old Fluid
		//	if (_eBodySex == EBodySex.Woman) {		//###IMPROVE!!!!!: Woman cum can be a lot better!

		//		//=== Set the position of the one fluid emitter instance to transform position of vagina entrance bone ===
		//		ErosEngine.Object_SetPositionOrientation(CGame.INSTANCE._oFluid._oObj._hObject, 0, _oVagina._oVaginaCumEmitterT.position, _oVagina._oVaginaCumEmitterT.rotation);	// Update the position of our fluid.  Penis tip controls the location of this global object // If woman body is selected and we're cumming place the one fluid emitter at the 'Vagina-CumEmitter' bone

		//		float nCycleTime = 8;		//###TUNE	###IMPROVE: Give user GUI access to these??
		//		float nMaxRate = 300;		//###TUNE!!!
		//		CGame.INSTANCE._oFluid._oObj.PropSet(0, EFluid.EmitVelocity, 0.00f);		//###TUNE

		//		//=== Set the emit velocity & rate from the pertinent configuration curve ===
		//		float nTimeInEjaculationCycle = (Time.time - CGame.INSTANCE._nTimeStartOfCumming) % nCycleTime;
		//		float nTimeInEjaculationCycle_Normalized = nTimeInEjaculationCycle / nCycleTime;		//###DESIGN!!! Move curve here?  How to persist??
		//		float nEmitRate = nMaxRate * Mathf.Max(CGame.INSTANCE.CurveEjaculateWoman.Evaluate(nTimeInEjaculationCycle_Normalized), 0);		//###NOTE: We assume both the ejaculation curve's X (time) value and Y values (strenght) go from 0 to 1

		//		CGame.INSTANCE._oFluid._oObj.PropSet(0, EFluid.EmitRate, nEmitRate);		// Woman ejaculation is a rate-based emitter where we control the rate over time	###DESIGN: Would going pressure-base like man have any sense here??

		//	} else {

		//		//=== Set the position of the one fluid emitter instance to transform position of penis tip ===
		//		ErosEngine.Object_SetPositionOrientation(CGame.INSTANCE._oFluid._oObj._hObject, 0, _oPenis._oPenisTip._oHotSpot.transform.position, _oPenis._oPenisTip._oHotSpot.transform.rotation);	// Update the position of our fluid.  Penis tip controls the location of this global object	// If man's body is selected place at hotspot of penis tip conveniently located at uretra.

		//		float nCycleTime	= _oPenis._oPenisTip._oObj.PropGet(EPenisTip.CycleTime);
		//		float nMaxVelocity	= _oPenis._oPenisTip._oObj.PropGet(EPenisTip.MaxVelocity);

		//		//=== Set the emit velocity & rate from the pertinent configuration curve ===
		//		float nTimeInEjaculationCycle = (Time.time - CGame.INSTANCE._nTimeStartOfCumming) % nCycleTime;
		//		float nTimeInEjaculationCycle_Normalized = nTimeInEjaculationCycle / nCycleTime;		//###DESIGN!!! Move curve here?  How to persist??
		//		float nEmitVelocity = nMaxVelocity * Mathf.Max(CGame.INSTANCE.CurveEjaculateMan.Evaluate(nTimeInEjaculationCycle_Normalized), 0);		//###NOTE: We assume both the ejaculation curve's X (time) value and Y values (strenght) go from 0 to 1

		//		CGame.INSTANCE._oFluid._oObj.PropSet(0, EFluid.EmitVelocity, nEmitVelocity);	// Man ejaculation is a pressure-based emitter where we control the emit velocity over time
		//		float nEmitRate = (nEmitVelocity != 0) ? 10000 : 0;		//###WEAK: What max??  ramp up??  threshold point??	// Set a very large rate as we're a pressure-based emitter... (we are limited by neighborhood particles)
		//		CGame.INSTANCE._oFluid._oObj.PropSet(0, EFluid.EmitRate, nEmitRate);
		//	}
		//}
		if (Input.GetKeyDown(KeyCode.R))		//###DESIGN: Selected only??
			SetActorPosToBonePos();
    }

    public void SetBodiesAsKinematic(bool bBodiesAreKinematic) {       // Set body as kinematic or not (used for pose loading / teleportation)
        EGameModes eGameModeSrc = EGameModes.MorphBody;
        EGameModes eGameModeDst = EGameModes.Play;
        if (bBodiesAreKinematic) {
            eGameModeSrc = EGameModes.Play;
            eGameModeDst = EGameModes.MorphBody;
        }
        foreach (CActor oActor in _aActors)
            oActor.OnChangeGameMode(eGameModeDst, eGameModeSrc);        //###DESIGN Closely related to game mode... merge in within a new game mode??
    }

    public void HoldSoftBodiesInReset(bool bSoftBodyInReset) {                       // Reset softbodies to their startup state.  Essential during pose load / teleportation!
        foreach (CSoftBody oBody in _aSoftBodies)
            oBody.HoldSoftBodiesInReset(bSoftBodyInReset);
    }



	//---------------------------------------------------------------------------	UTILITY

	public void SetActorPosToBonePos() {                    // Places the actor pins to where the 'extremity' of each actor is now.
        //###IMPROVE: Reset doesn't account for the 'drop' that occurs from the influence of gravity on each actor pin... compensate somehow?
        //=== Temporarily remove Chest and Pelvis as child of Torso so we can easily reset Torso to be between Chest and Pelvis below ===
        _oActor_Chest. transform.SetParent(_oActor_Base.transform);
        _oActor_Pelvis.transform.SetParent(_oActor_Base.transform);

        //=== Reset all our actor pins to their extremity's bone position ===
        foreach (CActor oActor in _aActors)
            oActor.SetActorPosToBonePos();

        //=== Manually place Torso to be halfway between Chest and Pelvis ===
        _oActor_Torso.transform.position = (_oActor_Chest.transform.position + _oActor_Pelvis.transform.position) / 2;
        _oActor_Torso.transform.rotation = _oActor_Chest.transform.rotation;        // Set Torso to same rotation as chest.

        //=== Return Chest and Pelvis to being child of Torso as before ===
        _oActor_Chest. transform.SetParent(_oActor_Torso.transform);
        _oActor_Pelvis.transform.SetParent(_oActor_Torso.transform);
    }
    public void ResetPinToBone(string sPathPin, string sPathBone) {		//###IMPROVE: Fix to run during play-time... Have to re-root from CPoseRoot
		Transform oRootPinT = (_oActor_Base != null) ? _oActor_Base.transform : CUtility.FindChild(_oBodyBase._oBodyRootGO.transform, "Base");	// If we're in game mode we can fetch base actor, if not we have to find Bones node off our prefab tree
		Transform oPinT = CUtility.FindChild(oRootPinT, sPathPin);
		Transform oBoneT = _oBodyBase.FindBone(sPathBone);
		oPinT.position = oBoneT.position;
	}
	public void SelectBody() {			//###MOVE11: To base?
		CGame.INSTANCE._nSelectedBody = _oBodyBase._nBodyID;
		CGame.SetGuiMessage(EGameGuiMsg.SelectedBody, _oBodyBase._sHumanCharacterName);
	}
	public bool IsBodySelected() {
		return CGame.INSTANCE._nSelectedBody == _oBodyBase._nBodyID;
	}
	public CActorArm FindCloseOrFarArmFromCamera(bool bInvert) {		//###OBS? Used by hand placement functionality to auto-select the hand to move without user selection
		if (IsBodySelected()) {		// Only the closest arm of the selected body responds
			float nDistL = (_oActor_ArmL._oJointShoulderBend.transform.position - Camera.main.transform.position).magnitude;
			float nDistR = (_oActor_ArmR._oJointShoulderBend.transform.position - Camera.main.transform.position).magnitude;
			if (bInvert)
				return (nDistL < nDistR) ? _oActor_ArmR : _oActor_ArmL;
			else
				return (nDistL < nDistR) ? _oActor_ArmL : _oActor_ArmR;
		} else {
			return null;
		}
	}
	public static string GetNameGameBodyFromPrefix(int nBodyID) {		// Returns the important prefix to many Blender-side objects / meshes related to constucting this CBody... 
		char chBodyID = (char)(65 + nBodyID);			// Convert the 0-based numeric ID into A,B,C,D,E...
		return "Body" + chBodyID;						// Body prefixes look like 'BodyA', 'BodyB', etc
	}

    public void HideShowMeshes() {
        _oBodySkinnedMesh._oSkinMeshRendNow.enabled = CGame.INSTANCE.ShowPresentation;
        ///_oFace.GetComponent<MeshRenderer>().enabled = CGame.INSTANCE.ShowPresentation;
        if (_oHairGO != null)
            _oHairGO.GetComponent<MeshRenderer>().enabled = CGame.INSTANCE.ShowPresentation;
        if (_oBodyFlexCollider != null) { 
            _oBodyFlexCollider._oSkinMeshRendNow.enabled = CGame.INSTANCE.ShowFlexColliders;
            if (_oBodyFlexCollider.GetComponent<uFlex.FlexParticlesRenderer>() != null)
                _oBodyFlexCollider.GetComponent<uFlex.FlexParticlesRenderer>().enabled = CGame.INSTANCE.ShowFlexParticles;
        }
        foreach (CSoftBody oSoftBody in _aSoftBodies)
            oSoftBody.HideShowMeshes();
        foreach (CBCloth oCloth in _aCloths)
            oCloth.HideShowMeshes();
        //if (_oVagina != null)
        //    _oVagina.HideShowMeshes();
    }



    //---------------------------------------------------------------------------	LOAD / SAVE
    public bool Pose_Load(string sNamePose) {
        return false;       //###NOW### ###BROKEN Poses!


		//string sPathPose = CGame.GetPathPoseFile(sNamePose);
		//if (File.Exists(sPathPose) == false) {
		//	Debug.LogError("CBody.Pose_Load() could not find file " + sPathPose);
		//	return false;
		//}
		//_sNamePose = sNamePose;
		//_oActor_Pelvis._eAnimMode = EAnimMode.Stopped;		// Cancel animation on sex bone on pose load
		//CGame.INSTANCE.Cum_Stop();			// Stop & clear cum upon a pose loading on any body.

		//_oScriptPlay.LoadScript(sPathPose);
		//_oScriptPlay.ExecuteAll();			// Execute all statements in file without pausing

		//CGame.INSTANCE.TemporarilyDisablePhysicsCollision();

		//Debug.Log(string.Format("Pose_Load() on body '{0}' loaded '{1}'", _oBodyBase._sBodyPrefix, _sNamePose));
		//return true;
	}
	public void Pose_Save(string sNamePose) {
		_sNamePose = sNamePose;
		Directory.CreateDirectory(CGame.GetPathPoses() + _sNamePose);					// Make sure that directory path exists
		string sPathPose = CGame.GetPathPoseFile(_sNamePose);
		CScriptRecord oScriptRec = new CScriptRecord(sPathPose, "Body Pose " + sPathPose);
		foreach (CActor oActor in _aActors)
			if (oActor != _oActor_Base)						// We do not save the base actor.  User orients this to position the body in the scene.
				oScriptRec.WriteObject(oActor._oObj);
		Debug.Log(string.Format("Pose_Save() on body '{0}' saved '{1}'", _oBodyBase._sBodyPrefix, _sNamePose));
	}
	public void Pose_Reload() {
		Pose_Load(_sNamePose);
	}
	
	public void Serialize_Actors_OBS(FileStream oStream) {
		foreach (CActor oActor in _aActors)
			oActor.Serialize_OBS(oStream);
	}


	public void OnChangeGameMode(EGameModes eGameModeNew, EGameModes eGameModeOld) {        //###DEV
		//foreach (CSoftBody oSoftBody in _aSoftBodies)
		//	oSoftBody.OnChangeGameMode(eGameModeNew, eGameModeOld);
		foreach (CActor oActor in _aActors)
			oActor.OnChangeGameMode(eGameModeNew, eGameModeOld);
	}


	//--------------------------------------------------------------------------	IOBJECT INTERFACE
	public void OnPropSet_BreastSize(float nValueOld, float nValueNew) {		//####DEV ####TEMP: Abstract code for all sliders
		CGame.gBL_SendCmd("CBody", _oBodyBase._sBlenderInstancePath_CBodyBase + ".Breasts_ApplyMorph('RESIZE', 'Nipple', 'Center', 'Wide', (" + nValueNew.ToString() + "," + nValueNew.ToString() + "," + nValueNew.ToString() + ",0), None)");
		//UpdateVertsFromBlenderMesh(false);						// Update Unity's copy of the morphing body's verts.
		_oBreastL.UpdateVertsFromBlenderMesh(false);		//####DEV ###TEMP
		//_oBodyColBreast.UpdateVertsFromBlenderMesh(true);       // Update Unity's copy of the breast collider mesh
	}


	//--------------------------------------------------------------------------	IHotspot interface

	public void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) { }
	public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {
		//###IMPROVE: Make this work by clicking on head? _oBody.SelectBody();			// Doing anything with a body's hotspot (head) selects the body
		if (eHotSpotEvent == EHotSpotEvent.ContextMenu)
			_oHotSpot.WndPopup_Create(FindClosestCanvas(), new CObject[] { _oObj /*, _oFace._oObj*/ });		//###TEMP: Face??
	}









	public CBody DoDestroy() {
		//=== Tell Blender's CBodyBase to destroy its CBody instance (and clear up a ton of meshes and memory) ===
		CGame.gBL_SendCmd("CBody", _oBodyBase._sBlenderInstancePath_CBodyBase + ".DestroyCBody()");

		//=== Reparent base nodes to where it originated before init moved it to global pose node ===
		_oActor_Base.transform.SetParent(_oBodyBase._oBodyRootGO.transform);      //###DESIGN15:! Causes problems with regular init/destory of CBody?  Do we really want to keep reparenting for easy full pose movement??>
		_oActor_Base.gameObject.name = "Base";				//###WEAK18: Kind of shitty to name differently during init / shutdown...  Do we really need this??
		GameObject.Destroy(_oBodySkinnedMesh.gameObject);   // Destroys *everything*  Every mesh we've created and every one of our components
		return null;						// Return convenience null so DoDestroy() can also nullify CBodyBase's reference... which makes this instance reference-less and flagged for garbage collection
	}

	public void DoEnable(bool bEnabled) {
		_bEnabled = bEnabled;
	}
}

public enum EBodySide {
	Left,
	Right,
}
