/*###DISCUSSION: CBody rewrite

=== DEV ===
- Dtor not working!  Lots of resources left!
- Flex lifecyle done quickly!
	- Remove dynamic!

=== NEXT ===

=== TODO ===

=== LATER ===

=== IMPROVE ===

=== DESIGN ===

=== QUESTIONS ===

=== IDEAS ===
- Have VR wands control each body simultaneously!
	- Pressing trigger controls the pelvis node only, pressing grip controls the whole torso (to orient the complete bodies in real time!!0
- Making tease animations and penetration work accross various poses and penis orientation
	- Animation of woman's pelvis is relative to the tip of the penis as oriented toward penis base.

=== LEARNED ===

=== PROBLEMS ===

=== PROBLEMS??? ===

=== WISHLIST ===

*/

using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;



public class CBody : IHotSpotMgr { 		// Manages a 'body':  Does not actually represent a mesh (most of the skinned body represtend by _oBodySkinnedMesh;
	// The main purpose of this class is to act as a bridge between the bone structure used for animation of the main skinned mesh and enable our connected soft-body parts to move along with us via our rim mesh and the pins it updates with PhysX

	public CBodyBase			_oBodyBase;             // Our owning body base.  Responsible to create / destroy us as player nagivates between configure / play mode.
	public bool                 _bEnabled;				// Body is 'enabled'.  Its meshes are visible and it receives CPU cycles.  (Used to disable 'other bodies' when one body is being configured by the player)
	//---------------------------------------------------------------------------	VISIBLE PROPERTIES
	public CObject				_oObj;					// The user-configurable object representing this body.
	public CBSkin				_oBodySkinnedMesh;		// The main skinned mesh representing most of the body (e.g. everything except detached soft body parts, head, hair, clothing, etc)
	public CFlexTriCol			_oFlexTriCol_BodyMain;	//# The 'Flex Triangle Collider' representing this body's shape in Flex's main solver.   This is what repels softbodies and cloth from non-softbody areas.  Separated from the rig in Finalize() from the skinned particles.
	public CFlexTriCol			_oFlexTriCol_BodyFluid;	//# The 'Flex Triangle Collider' representing this body's shape in Flex's fluid solver.  This is what repels fluid particles from the ENTIRE body.  A simple remesh of the source body
    ///public  CHeadLook		_oHeadLook;             // Object in charge of moving head toward points of interest
    public GameObject			_oHairGO;               // Reference to the hair game object we mount on head at init.

	//---------------------------------------------------------------------------	IMPLEMENTATION MEMBERS
	public CHotSpot				_oHotSpot;				// Hotspot at the head of the character.  Enables user to change the important body properties by right-clicking on the head
	public CScriptPlay			_oScriptPlay;			// Scripting interpreter in charge of loading user scripts that programmatically control this body (poses also load this way)
	public string				_sNamePose;             // The name of the current loaded pose (Same as filename in /Poses directory)

	//---------------------------------------------------------------------------	SOFT BODY PARTS
	public CSoftBody_BreastL    _oSoftBody_BreastL;
	public CSoftBody_BreastR    _oSoftBody_BreastR;
	public CSoftBody_Penis		_oSoftBody_Penis;
	public CBSkinBaked_PenisMeshCollider _oPenisColliderPhysX;

	//---------------------------------------------------------------------------	CLOTHING		//###DESIGN: ###CHECK?
	public List<CBCloth>		_aCloths		= new List<CBCloth>();		//###OBS14:???  Belongs to base??	// List of our simulated cloth.  Used to iterate during runtime

	//---------------------------------------------------------------------------	ACTORS
	public CActorGenitals		_oActor_Genitals;					// The smart 'actors' associated with our body.  Adds much intelligent functionality!
	public CActorPelvis			_oActor_Pelvis;
	public CActorChest			_oActor_Chest;
	public CActorArm 			_oActor_ArmL;
	public CActorArm 			_oActor_ArmR;
	public CActorFootCenter		_oActor_FootCenter;
	public CActorLeg 			_oActor_LegL;
	public CActorLeg 			_oActor_LegR;
	public List<CActor>			_aActors = new List<CActor>();		// An array containing all the _oActor_xxx elements above.  Used to simplify iterations.

	//---------------------------------------------------------------------------	SCRIPT ACCESS
	public CObject				 Genitals;		// Flattened references to the oObj CObject member of our actors.  Done to offer scripting runtime simplified access to our scriptable members using friendlier names.
	public CObject				 Pelvis;
	public CObject				 Chest;
	public CObject				 ArmL;
	public CObject				 ArmR;
	public CObject				 FootCenter;
	public CObject				 LegL;
	public CObject				 LegR;
	public CObject               Penis;
	public CObject               Breasts;
	//public CObject               Vagina;
	//public CObject				 Face;

	//---------------------------------------------------------------------------	DEV
	public GameObject _oBodySkinnedMeshGO_HACK;        // Reference to game object containing main skinned body.  Kept as a member because of complex init tree needing this before '_oBodySkinnedMesh' is set!
	public CUICanvas[] _aUICanvas = new CUICanvas[2];           // The UI canvases that display various user interface panels to provide end-user edit capability on this body.  One for each left / right.
	CUICanvas _oCanvas_HACK;
	public Dictionary<string, CBone> _mapBonesFlattened = new Dictionary<string, CBone>();      // Flattened collection of bones extracted from Blender.  Includes dynamic bones.  Used to speed up CBSkin skinning info de-serialization
	CVaginaRaycaster _oVaginaRaycaster;


    public CBody(CBodyBase oBodyBase) {
		_oBodyBase = oBodyBase;
		_oBodyBase._oBody = this;           //###WEAK13: Convenience early-set of our instance into owning parent.  Needed as some of init code needs to access us from our parent! ###DESIGN!
		_oBodySkinnedMeshGO_HACK = new GameObject("RuntimeBody");       // Create the game object that will contain our important CBody component early (complex init tree needs it!)
		_oBodySkinnedMeshGO_HACK.transform.SetParent(_oBodyBase._oBodyRootGO.transform);
		_bEnabled = true;			// Enabled at creation by defnition.

		//bool bForMorphingOnly = false;  //###JUNK (CGame.INSTANCE._GameMode == EGameModes.MorphNew_TEMP);				//####DEV!!!!
		Debug.Log(string.Format("+ Creating body #{0}", _oBodyBase._nBodyID));

		//=== Tell Blender to create our CBody instance for us ===
		CGame.gBL_SendCmd("CBody", _oBodyBase._sBlenderInstancePath_CBodyBase + ".CreateCBody()");

		//===== UPDATE BONE HIERARCHY FROM BLENDER =====
		CBone.BoneUpdate_UpdateFromBlender(_oBodyBase._sBlenderInstancePath_CBodyBase, _oBodyBase._oBoneRootT, ref _mapBonesFlattened);

		//===== MAIN SKINNED BODY PROCESSING =====
		//=== Get the main body skinned mesh (has to be done once all softbody parts have been detached) ===
        _oBodySkinnedMesh = (CBSkin)CBMesh.Create(_oBodySkinnedMeshGO_HACK, _oBodyBase, ".oBody.oMeshBody", typeof(CBSkin)); //###IMPROVE13: Create blender instance string for our CBody?
		_oBodySkinnedMesh.name = _oBodyBase._sBodyPrefix + "-SkinnedBody";
		//_oBodySkinnedMesh.GetComponent<SkinnedMeshRenderer>().enabled = false;

		Body_InitActors();

        //=== Create the left and right canvases on each side of the body so that panels have a place to be pinned for close-to-body editing ===
        _aUICanvas[0] = CUICanvas.Create(Util_CreateCanvasPin("_CanvasPin_Left"));        //###DESIGN22: Canvas pin still our best design for panel starting points??
        _aUICanvas[1] = CUICanvas.Create(Util_CreateCanvasPin("_CanvasPin_Right"));
		 
		//=== Create softbody handler instances ===		       //###IMPROVE: Extract from Blender and expand here
		if (_oBodyBase._eBodySex != EBodySex.Woman) {       // Non-woman (man and shemale) have a penis
			_oSoftBody_Penis = CSoftBody.Create(this, CSoftBody.C_SoftBodyID_Penis, _oBodyBase.FindBone("hip/pelvis/Genitals/#Penis"), typeof(CSoftBody_Penis)) as CSoftBody_Penis;
			_oPenisColliderPhysX = (CBSkinBaked_PenisMeshCollider)CBMesh.Create(null, _oBodyBase, ".oBody.oMeshPenisColliderPhysX", typeof(CBSkinBaked_PenisMeshCollider));
			_oPenisColliderPhysX.name = _oBodyBase._sBodyPrefix + "-PenisColliderPhysX";
		}
		if (_oBodyBase._eBodySex != EBodySex.Man) {         // Non-man (woman and shemale) have breasts and a vagina
			if (CGame.INSTANCE._bSkipLongUnnecessaryOps_HACK == false) {
				_oSoftBody_BreastL = CSoftBody.Create(this, CSoftBody.C_SoftBodyID_BreastL, _oBodyBase.FindBone("hip/abdomenLower/abdomenUpper/chestLower/lPectoral"), typeof(CSoftBody_BreastL)) as CSoftBody_BreastL;
				_oSoftBody_BreastR = CSoftBody.Create(this, CSoftBody.C_SoftBodyID_BreastR, _oBodyBase.FindBone("hip/abdomenLower/abdomenUpper/chestLower/rPectoral"), typeof(CSoftBody_BreastR)) as CSoftBody_BreastR;
			}
			_oVaginaRaycaster = new CVaginaRaycaster(this);         
		}

		//=== Copy references to our actors to our script-friendly CObject variables to provide friendlier access to our scriptable objects ===
		Genitals    = _oActor_Genitals._oObj;                       // CGamePlay passed us the reference to the right (empty) static object.  We fill it here.
		Pelvis      = _oActor_Pelvis._oObj;
		Chest       = _oActor_Chest._oObj;
		ArmL        = _oActor_ArmL._oObj;
		ArmR        = _oActor_ArmR._oObj;
		FootCenter  = _oActor_FootCenter._oObj;
		LegL        = _oActor_LegL._oObj;
		LegR        = _oActor_LegR._oObj;
		Penis       = _oSoftBody_Penis ? _oSoftBody_Penis._oObj : null;
		Breasts     = _oSoftBody_BreastL ? _oSoftBody_BreastL._oObj : null;     // "Breasts" point to left breast as it automatically copies all its commands to the right breast to maintain sync
																				//Face		= null;/// _oFace._oObj;
		_oScriptPlay = CUtility.FindOrCreateComponent(_oBodyBase._oBodyRootGO.transform, typeof(CScriptPlay)) as CScriptPlay;
		_oScriptPlay.OnStart(this);

		//=== Create the important Flex triangle collider body.  It is in charge of repelling all softbodies in the main Flex solver scene ===
		if (true) { 
			_oFlexTriCol_BodyMain = (CFlexTriCol)CBSkin.Create(null, _oBodyBase, ".oBody.oMeshFlexTriCol_BodyMain", typeof(CFlexTriCol));
			_oFlexTriCol_BodyMain.name = _oBodyBase._sBodyPrefix + "-FlexTriCol_BodyMain";
			_oFlexTriCol_BodyMain.transform.SetParent(_oBodyBase._oBodyRootGO.transform);      //###IMPROVE: Put this common re-parenting and re-naming in Create!
			_oFlexTriCol_BodyMain.Initialize(CGame.INSTANCE._oFlexParamsMain.GetComponent<uFlex.FlexColliders>());
		}
		//=== Create the important Flex triangle collider body custom-designed for the fluid scene.  It is in charge of repelling fluid particles from the body ===
		if (true) { 
			_oFlexTriCol_BodyFluid = (CFlexTriCol)CBSkin.Create(null, _oBodyBase, ".oBody.oMeshFlexTriCol_BodyFluid", typeof(CFlexTriCol));
			_oFlexTriCol_BodyFluid.name = _oBodyBase._sBodyPrefix + "-FlexTriCol_BodyFluid";
			_oFlexTriCol_BodyFluid.transform.SetParent(_oBodyBase._oBodyRootGO.transform);
			_oFlexTriCol_BodyFluid.Initialize(CGame.INSTANCE._oFlexParamsFluid.GetComponent<uFlex.FlexColliders>());
		}
		_oBodySkinnedMesh._oSkinMeshRendNow.enabled = (CGame.INSTANCE._bShowFlexFluidColliders_HACK == false);


		if (_oBodyBase._nBodyID == 0)
			CGame.INSTANCE._oVrWandR.AssignToObject_HACK(_oActor_Genitals.transform);
		else
			CGame.INSTANCE._oVrWandL.AssignToObject_HACK(_oActor_Genitals.transform);
	}

	Transform Util_CreateCanvasPin(string sNameCanvas) {            //###DESIGN22: Keep?
		Transform oParentT = _oActor_Chest.transform;           //###WEAK: Parent hardcoding?
		//Transform oParentT = _oBodyBase.FindBone("hip/abdomenLower/abdomenUpper/chestLower/chestUpper");		// Pinning to bones can have some value?
		Transform oCanvasPinT = new GameObject(sNameCanvas).transform;
		oCanvasPinT.SetParent(oParentT);
		return oCanvasPinT;
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

	CActor CreateActor(Transform oParentT, string sNameActor, Type oTypeActor) {
		GameObject oActorGOT = Resources.Load("Prefabs/Actor") as GameObject;
		GameObject oActorGO = GameObject.Instantiate(oActorGOT) as GameObject;
		oActorGO.name = sNameActor;
		oActorGO.transform.parent = oParentT;
		CActor oActor = CUtility.FindOrCreateComponent(oActorGO, oTypeActor) as CActor;
		oActor.OnStart(this);
		return oActor;
	}

	public void Body_InitActors() {
		////=== Fetch the body part components, assign them to easy-to-access variables (and a collection for easy iteration) and initialize them by telling them their 'side' ===
		_aActors.Add(_oActor_Genitals	= CreateActor(_oBodyBase._oBodyRootGO.transform,	"Genitals",		typeof(CActorGenitals))		as CActorGenitals);
		_aActors.Add(_oActor_Pelvis		= CreateActor(_oActor_Genitals.transform,			"Pelvis",		typeof(CActorPelvis))		as CActorPelvis);
		_aActors.Add(_oActor_Chest		= CreateActor(_oActor_Genitals.transform,			"Chest",		typeof(CActorChest))		as CActorChest);
		_aActors.Add(_oActor_ArmL		= CreateActor(_oActor_Chest.transform,				"ArmL",			typeof(CActorArm))			as CActorArm);
		_aActors.Add(_oActor_ArmR		= CreateActor(_oActor_Chest.transform,				"ArmR",			typeof(CActorArm))			as CActorArm);
		_aActors.Add(_oActor_FootCenter = CreateActor(_oActor_Genitals.transform,			"FootCenter",	typeof(CActorFootCenter))	as CActorFootCenter);
		_aActors.Add(_oActor_LegL		= CreateActor(_oActor_FootCenter.transform,			"LegL",			typeof(CActorLeg))			as CActorLeg);
		_aActors.Add(_oActor_LegR		= CreateActor(_oActor_FootCenter.transform,			"LegR",			typeof(CActorLeg))			as CActorLeg);

		//=== Reset all the actors to their default positions ===
		SetActorPosToBonePos();

		if (false) {				//###DEBUG21: Temp code to directly manipulate bones via GUI
			//=== Iterate through all our CBone instances and connect them to this body ===		###MOVE20:?
			//object[] aBones = _oBodyBase._oBoneRootT.GetComponentsInChildren(typeof(CBone), true);
			_oObj = new CObject(_oBodyBase, "Body Direct Bones", "Body Direct Bones");
			_oObj.Event_PropertyValueChanged += Event_PropertyChangedValue;
			CPropGrp oPropGrp = new CPropGrp(_oObj, "Body Direct Bones");
			int nProp = 0;
			foreach (CActor oActor in _aActors) { 
				if (oActor._eBodySide != EBodySide.Right) {			// Only show center and left to save GUI space
					foreach (CBone oBone in oActor._aBones) {
						for (int nBoneRot = 0; nBoneRot < oBone._aBoneRots.Length; nBoneRot++) { 
							CBoneRot oBoneRot = oBone._aBoneRots[nBoneRot];
							if (oBoneRot != null) {			// Our three possbile slots (x,y,z) are not necessarily defined for all bones...
								string sPropName = oBone.gameObject.name + " " + oBoneRot._chAxis + ":" + oBoneRot._sNameRotation;
								CProp oProp = oPropGrp.PropAdd(nProp++, sPropName, sPropName, oBoneRot._nValue, oBoneRot._nMin, oBoneRot._nMax, sPropName);
								oProp._oObjectExtraFunctionality = oBoneRot;			// Store back-reference so OnPropertyChanged can readily adjust the bone
							}
						}
					}
				}
			}
			_oObj.FinishInitialization();			//###TEMP21
			_oCanvas_HACK = CUICanvas.Create(_oBodySkinnedMeshGO_HACK.transform);
			_oCanvas_HACK.transform.position = new Vector3(0.31f, 1.35f, 0.13f);            //###WEAK11: Hardcoded panel placement in code?  Base on a node in a template instead??  ###IMPROVE: Autorotate?
			_oCanvas_HACK.CreatePanel("Body Direct Bone", null, _oObj);
		}
		//oBone.gameObject.name.Contains("hip") ||
		//oBone.gameObject.name.Contains("pelvis") ||
		//oBone.gameObject.name.Contains("abdomen") ||
		//oBone.gameObject.name.Contains("chest") ||
		//oBone.gameObject.name.Contains("Shldr") ||
		//oBone.gameObject.name.Contains("Forearm") ||
		//oBone.gameObject.name.Contains("Hand") ||
		//oBone.gameObject.name.Contains("Thigh") ||
		//oBone.gameObject.name.Contains("lIndex3") ||
		//oBone.gameObject.name.Contains("lBigToe_2") ||
		//oBone.gameObject.name.Contains("Shin") ||
		//oBone.gameObject.name.Contains("Foot") ||
		//oBone.gameObject.name.Contains("Collar")) {
	}

	public void Destroy() {             //###TODO15: ###OBS? Needed still with DoDestory()  Merge!
		//Debug.Log("CBody.OnDestroy(): " + _oBodyBase._sBodyPrefix);
		//foreach (CActor oActor in _aActors)
		//	GameObject.DestroyImmediate(oActor);

		//GameObject.DestroyImmediate(_oActor_Genitals.gameObject);          // Destroy our body's entry in top-level CPoseRoot

		////if (_oKeyHook_PenisBaseUpDown != null) _oKeyHook_PenisBaseUpDown.Dispose();     //###WEAK!!! Try to enable auto-dispose!!
		////if (_oKeyHook_PenisShaftUpDown != null) _oKeyHook_PenisShaftUpDown.Dispose();
		////if (_oKeyHook_PenisDriveStrengthMax != null) _oKeyHook_PenisDriveStrengthMax.Dispose();
		//if (_oKeyHook_ChestUpDown != null) _oKeyHook_ChestUpDown.Dispose();

		//foreach (CBCloth oBCloth in _aCloths)
		//	GameObject.DestroyImmediate(oBCloth.gameObject);
	}

	
	//---------------------------------------------------------------------------	UPDATE

	public void OnUpdate() {       //###DESIGN:!!! Redo fucking update mechanism!! ###CLEANUP
		foreach (CActor oActor in _aActors)
			oActor.OnUpdate();

		if (Input.GetKeyDown(KeyCode.R))		//###DESIGN: Selected only??
			SetActorPosToBonePos();

		if (_oPenisColliderPhysX)
			_oPenisColliderPhysX.OnSimulate();
		//if (_oFlexTriCol_BodyMain)
		//	_oFlexTriCol_BodyMain.OnSimulate();
		//if (_oFlexTriCol_BodyFluid)
		//	_oFlexTriCol_BodyFluid.OnSimulate();

		if (_oVaginaRaycaster != null)
			_oVaginaRaycaster.DoUpdate();

		//if (Input.GetKeyDown(KeyCode.M))
		//	_oBodySkinnedMesh._oSkinMeshRendNow.sharedMesh.RecalculateNormals();
		//if (Input.GetKeyDown(KeyCode.N))
		//	_oBodySkinnedMesh.UpdateNormals();
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
		//###BROKEN
        //foreach (CSoftBody oBody in _aSoftBodies)
        //    oBody.HoldSoftBodiesInReset(bSoftBodyInReset);
    }



	//---------------------------------------------------------------------------	UTILITY

	public void SetActorPosToBonePos() {                    // Places the actor pins to where the 'extremity' of each actor is now.
        //###IMPROVE: Reset doesn't account for the 'drop' that occurs from the influence of gravity on each actor pin... compensate somehow?
		//###CLEANUP22:
        //=== Temporarily remove Chest and Pelvis as child of Torso so we can easily reset Torso to be between Chest and Pelvis below ===
        //_oActor_Chest. transform.SetParent(_oActor_Genitals.transform);
        //_oActor_Pelvis.transform.SetParent(_oActor_Genitals.transform);

        //=== Reset all our actor pins to their extremity's bone position ===
        foreach (CActor oActor in _aActors)
            oActor.SetActorPosToBonePos();

		//=== Manually place Torso to be halfway between Chest and Pelvis ===
		//_oActor_Torso.transform.position = (_oActor_Chest.transform.position + _oActor_Pelvis.transform.position) / 2;
		//_oActor_Torso.transform.rotation = _oActor_Chest.transform.rotation;        // Set Torso to same rotation as chest.
		//Transform oVaginaOpeningT = _oActor_Pelvis._oBoneExtremity.transform.FindChild("VaginaOpening");		//###DESIGN:!!!
		//_oActor_Genitals.transform.position = oVaginaOpeningT.position;
  //      _oActor_Genitals.transform.rotation = oVaginaOpeningT.rotation;

  //      //=== Return Chest and Pelvis to being child of Torso as before ===
  //      _oActor_Chest. transform.SetParent(_oActor_Genitals.transform);
  //      _oActor_Pelvis.transform.SetParent(_oActor_Genitals.transform);
    }
    public void ResetPinToBone(string sPathPin, string sPathBone) {		//###IMPROVE: Fix to run during play-time... Have to re-root from CPoseRoot
		Transform oRootPinT = (_oActor_Genitals != null) ? _oActor_Genitals.transform : CUtility.FindChild(_oBodyBase._oBodyRootGO.transform, "Genitals");	// If we're in game mode we can fetch base actor, if not we have to find Bones node off our prefab tree
		Transform oPinT = CUtility.FindChild(oRootPinT, sPathPin);
		Transform oBoneT = _oBodyBase.FindBone(sPathBone);
		oPinT.position = oBoneT.position;
	}
	public void SelectBody() {			//###MOVE11: To base?
		CGame.INSTANCE._nSelectedBody = _oBodyBase._nBodyID;
		//CGame.SetGuiMessage(EGameGuiMsg.SelectedBody, _oBodyBase._sHumanCharacterName);
	}
	public bool IsBodySelected() {
		return CGame.INSTANCE._nSelectedBody == _oBodyBase._nBodyID;
	}
	public CActorArm FindCloseOrFarArmFromCamera(bool bInvert) {		//###OBS? Used by hand placement functionality to auto-select the hand to move without user selection
		if (IsBodySelected()) {		// Only the closest arm of the selected body responds
			float nDistL = (_oActor_ArmL._oBoneShoulderBend.transform.position - Camera.main.transform.position).magnitude;
			float nDistR = (_oActor_ArmR._oBoneShoulderBend.transform.position - Camera.main.transform.position).magnitude;
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
        if (_oHairGO != null)
            _oHairGO.GetComponent<MeshRenderer>().enabled = CGame.INSTANCE.ShowPresentation;
        if (_oSoftBody_Penis != null) { 
            _oSoftBody_Penis._oSkinMeshRendNow.enabled = CGame.INSTANCE.ShowFlexColliders;
            if (_oSoftBody_Penis.GetComponent<uFlex.FlexParticlesRenderer>() != null)
                _oSoftBody_Penis.GetComponent<uFlex.FlexParticlesRenderer>().enabled = CGame.INSTANCE.ShowFlexParticles;
        }
        foreach (CBCloth oCloth in _aCloths)
            oCloth.HideShowMeshes();
    }



    //---------------------------------------------------------------------------	LOAD / SAVE
    public bool Pose_Load(string sNamePose) {
		string sPathPose = CGame.GetPath_PoseFile(sNamePose);
		if (File.Exists(sPathPose) == false) {
			Debug.LogError("CBody.Pose_Load() could not find file " + sPathPose);
			return false;
		}
		_sNamePose = sNamePose;
		_oActor_Pelvis._eAnimMode = EAnimMode.Stopped;		// Cancel animation on sex bone on pose load
		CGame.INSTANCE.Cum_Stop();			// Stop & clear cum upon a pose loading on any body.

		_oScriptPlay.LoadScript(sPathPose);
		_oScriptPlay.ExecuteAll();			// Execute all statements in file without pausing

		CGame.INSTANCE.TemporarilyDisablePhysicsCollision();

		Debug.Log(string.Format("Pose_Load() on body '{0}' loaded '{1}'", _oBodyBase._sBodyPrefix, _sNamePose));
		return true;
	}
	public void Pose_Save(string sNamePose) {
		_sNamePose = sNamePose;
		Directory.CreateDirectory(CGame.GetPath_Poses() + _sNamePose);					// Make sure that directory path exists
		string sPathPose = CGame.GetPath_PoseFile(_sNamePose);
		CScriptRecord oScriptRec = new CScriptRecord(sPathPose, "Body Pose " + sPathPose);
		foreach (CActor oActor in _aActors)
			oScriptRec.WriteObject(oActor._oObj);
		//###DESIGN: Avoid root actor serialization?			if (oActor != _oActor_Genitals)						// We do not save the base actor.  User orients this to position the body in the scene.
		oScriptRec.CloseFile();
		Debug.Log(string.Format("Pose_Save() on body '{0}' saved '{1}'", _oBodyBase._sBodyPrefix, _sNamePose));
	}
	public void Pose_Reload() {
		Pose_Load(_sNamePose);
	}
	
	public void Serialize_Actors_OBS(FileStream oStream) {
		foreach (CActor oActor in _aActors)
			oActor.Serialize_OBS(oStream);
	}


	public void OnChangeGameMode(EGameModes eGameModeNew, EGameModes eGameModeOld) {
		//foreach (CSoftBody oSoftBody in _aSoftBodies)
		//	oSoftBody.OnChangeGameMode(eGameModeNew, eGameModeOld);
		foreach (CActor oActor in _aActors)
			oActor.OnChangeGameMode(eGameModeNew, eGameModeOld);
	}


	//--------------------------------------------------------------------------	IHotspot interface

	public void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) { }
	public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {
		//###IMPROVE: Make this work by clicking on head? SelectBody();			// Doing anything with a body's hotspot (head) selects the body
		if (eHotSpotEvent == EHotSpotEvent.ContextMenu)
			_oHotSpot.WndPopup_Create(FindClosestCanvas(), new CObject[] { _oObj /*, _oFace._oObj*/ });		//###TEMP: Face??
	}





	public CBody DoDestroy() {	//#@
		//=== Tell Blender's CBodyBase to destroy its CBody instance (and clear up a ton of meshes and memory) ===
		CGame.gBL_SendCmd("CBody", _oBodyBase._sBlenderInstancePath_CBodyBase + ".DestroyCBody()");

		//=== Destroy the soft bodies ===		###IMPROVE: Use some sort of destruction interface to group those up?
		if (_oSoftBody_Penis)
			_oSoftBody_Penis.DoDestroy();
		if (_oSoftBody_BreastL)
			_oSoftBody_BreastL.DoDestroy();
		if (_oSoftBody_BreastR)
			_oSoftBody_BreastR.DoDestroy();
		if (_oFlexTriCol_BodyMain)
			_oFlexTriCol_BodyMain.DoDestroy();
		if (_oFlexTriCol_BodyFluid)
			_oFlexTriCol_BodyFluid.DoDestroy();


		//=== Reparent base nodes to where it originated before init moved it to global pose node ===
		_oActor_Genitals.transform.SetParent(_oBodyBase._oBodyRootGO.transform);      //###DESIGN15:! Causes problems with regular init/destory of CBody?  Do we really want to keep reparenting for easy full pose movement??>
		_oActor_Genitals.gameObject.name = "Base";				//###WEAK18: Kind of shitty to name differently during init / shutdown...  Do we really need this??
		GameObject.Destroy(_oBodySkinnedMesh.gameObject);		// Destroys *everything*  Every mesh we've created and every one of our components

		return null;						// Return convenience null so DoDestroy() can also nullify CBodyBase's reference... which makes this instance reference-less and flagged for garbage collection
	}

	public void DoEnable(bool bEnabled) {
		_bEnabled = bEnabled;
	}



	void Event_PropertyChangedValue(object sender, EventArgs_PropertyValueChanged oArgs) {      // Fired everytime user adjusts a property.
		// 'Bake' the morphing mesh as per the player's morphing parameters into a 'MorphResult' mesh that can be serialized to Unity.  Matches Blender's CBodyBase.UpdateMorphResultMesh()
		//###MOVE22: Super important functionality.  Probably needs to be moved and invoked from different contexts
		Debug.LogFormat("CBody property property '{0}' of group '{1}' changed to value '{2}'", oArgs.PropertyName, oArgs.PropertyGroup._sNamePropGrp, oArgs.ValueNew);

		CBoneRot oBoneRotChanged = oArgs.Property._oObjectExtraFunctionality as CBoneRot;
		oBoneRotChanged._nValue = oArgs.ValueNew;
		CBone oBone = oBoneRotChanged._oBone;
		//Quaternion quatRot = new Quaternion(oBoneRotChanged._oBone._quatBoneRotationStartup.x, oBoneRotChanged._oBone._quatBoneRotationStartup.y, oBoneRotChanged._oBone._quatBoneRotationStartup.z, oBoneRotChanged._oBone._quatBoneRotationStartup.w);
		Quaternion quatRot = new Quaternion(0,0,0,1);		//###IMPROVE: Act upon straight bones versus D6.  ###DEV21: Startup rotation???????

		//=== Iterate *in DAZ-provided order* through out three possible rotations.  (Not all may be defined) ===
		foreach (char chAxisThisRotation in oBone._sRotOrder) {             //###INFO: How to access characters as ascii from a string
			int nAxisThisRotation = chAxisThisRotation - 'X';				// Obtain 0 for 'X', 1 for 'Y', 2 for 'Z' so we can access proper rotation in '_aBoneRots'
			CBoneRot oBoneRot = oBone._aBoneRots[nAxisThisRotation];
			if (oBoneRot != null) {                     // If not defined no big deal... just nothing to do for this rotation order...
				float nBoneRotValue = oBoneRot._nValue;
				if (oBoneRot._bAxisNegated)			// Invert final Quaternion rotation for this axis if bone rotation is negated
					nBoneRotValue = -nBoneRotValue;
				Quaternion quatRotThisAxis = Quaternion.AngleAxis(nBoneRotValue, CGame.INSTANCE._aCardinalAxis[nAxisThisRotation]);		//###DEV20:
				quatRot *= quatRotThisAxis;			// "Rotating by the product lhs * rhs is the same as applying the two rotations in sequence: lhs first and then rhs, relative to the reference frame resulting from lhs rotation"
			}
		}

		//oBone.transform.localRotation = quatRot;					//###IMPROVE21:!!! Enhance so it can directly set bone rotation in kinematic mode and D6 join in gametime mode
		if (oBone._oConfJoint != null)			//###IMPROVE22: Hip has not conf joint given that it's root bone... route to pelvis!
			oBone._oConfJoint.targetRotation = quatRot;
	}
}







//------------------------------- old CBody init stuff
//_oObj = new CObject(oBodyBase, "Body", "Body");										//###BROKEN21:!! CBody GUI!!!
//CPropGrpEnum oPropGrp = new CPropGrpEnum(_oObj, "Body", typeof(EBodyDef));
//oPropGrp.PropAdd(EBodyDef.BreastSize,		"Breast Size BROKEN",		1.0f, 0.5f, 2.5f, "");		
//_oObj.FinishInitialization();
//oPropGrp.PropAdd(0, "Test", "Test", 50, 0, 100, "Test Description");
//_oObj.FinishInitialization();
//_oCanvas_HACK = CUICanvas.Create(_oBodySkinnedMeshGO_HACK.transform);
//_oCanvas_HACK.transform.position = new Vector3(0.31f, 1.35f, 0.13f);            //###WEAK11: Hardcoded panel placement in code?  Base on a node in a template instead??  ###IMPROVE: Autorotate?
//_oCanvas_HACK.CreatePanel("Body Direct Bone", null, _oObj);

//###DESIGN18: Flaw in auto-deletion means cloths must be named differently between cut-time versus play time!
//_aCloths.Add(CBCloth.Create(_oBodyBase, "MyShirtPLAY", "Shirt", "BodySuit", "_ClothSkinnedArea_ShoulderTop"));    //###HACK18:!!!: Choose what cloth to edit from GUI choice  ###DESIGN: Recutting from scratch??  Use what design time did or not??

//=== Create a hotspot at the character's head the user can use to invoke our (important) context menu ===
//###BROKENN
//      Transform oHeadT = FindBone("chest/neck/head");         // Our hotspot is located on the forehead of the character.
//_oHotSpot = CHotSpot.CreateHotspot(oBodyBase, oHeadT, this._sHumanCharacterName, false, new Vector3(0, 0.09f, 0.03f), 2.0f);     //###IMPROVE: Get these offsets from pre-placed nodes on bone structure??

////=== Create the head look controller to look at parts of the other body ===
//###BROKENN
//      _oHeadLook = _oBodyRootGO.gameObject.AddComponent<CHeadLook>();			//####DESIGN: Keep for morph mode??
//_oHeadLook.OnStart(this);


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
///_oActor_Genitals.transform.SetParent(CGame.INSTANCE._oPoseRoot.transform);		//###DESIGN15:! Causes problems with regular init/destory of CBody?  Do we really want to keep reparenting for easy full pose movement??>
///_oActor_Genitals.gameObject.name = _oBodyBase._sBodyPrefix + "_Base";

//=== Create the Flex collider in Blender and serialize it to Unity ===
//CGame.INSTANCE.nDistFlexColliderShrinkMult = 0;         //###TEMP23: Temporary disabling of 'shkrinking' flex collider pending softbody rewrite in 24


//=== Rotate the 2nd body toward the first and separate slightly loading poses doesn't pile up one on another ===	####MOVE? ####OBS? (Depend on pose?)
//###DESIGN:!!! Hack to artifially separate bodies before posing becomes available
//if (_oBodyBase._nBodyID == 0) {
//	_oActor_Genitals.transform.position += new Vector3(0, 0, -CGame.C_BodySeparationAtStart);        //###DESIGN: Don't load base actor instead??  ###BUG Overwrites user setting of base!!!!
//} else {
//	_oActor_Genitals.transform.position += new Vector3(0, 0, CGame.C_BodySeparationAtStart);
//	_oActor_Genitals.transform.rotation *= Quaternion.Euler(0, 180, 0);      // Rotate the 2nd body 180 degrees
//}

///_oClothEdit_HACK = new CClothEdit(oBodyBase, "Shirt");

///CGame.gBL_SendCmd("CBody", "CBodyBase_GetBodyBase(" + _oBodyBase._nBodyID.ToString() + ").Breasts_ApplyMorph('RESIZE', 'Nipple', 'Center', 'Wide', (1.6, 1.6, 1.6, 0), None)");       //###F ###HACK!!!

//  _oMeshPinnedParticles = (CBSkinBaked)CBMesh.Create(null, _oBodyBase, _sBlenderInstancePath_CSoftBody + ".oMeshPinnedParticles", typeof(CBSkinBaked));           // Retrieve the skinned softbody rim mesh Blender just created so we can pin softbody at runtime
//_oMeshPinnedParticles.transform.SetParent(transform);

//public void Util_AdjustMaterialTransparency(string sNameMaterial, float nTransparency, bool bAffectOthers) {       //###MOVE: Pretty handy for development.  Should be callsed from body context??
//###BROKEN: Transparency broken by dynamic-creation of materials and complexity of 'enabling' transparency
//SkinnedMeshRenderer oSMR = _oBodySkinnedMesh._oSkinMeshRendNow;
//bool bTransparentMaterial = (nTransparency != 0);
//for (int nMat = 0; nMat < _oBodySkinnedMesh._aMatsCurrent.Length; nMat++) {
//	Material oMat = _oBodySkinnedMesh._aMatsCurrent[nMat];
//	string sNameMat = oMat.name;
//	bool bIsRightMaterial = sNameMat.Contains(sNameMaterial);
//	if (bIsRightMaterial == !bAffectOthers) {                                           // If we're the right material and we're meant to change it then change it.  If we're the wrong material and we're meant to change others change it too.
//		if (bTransparentMaterial) {
//			_oBodySkinnedMesh._aMatsCurrent[nMat] = _oBodySkinnedMesh._aMatsTransparent[nMat];
//			_oBodySkinnedMesh._aMatsCurrent[nMat].color = new Color32(255, 255, 255, (byte)((1 - nTransparency) * 255f));	// Setting alpha channel to transparency we need
//		} else {
//			_oBodySkinnedMesh._aMatsCurrent[nMat] = _oBodySkinnedMesh._aMatsOpaque[nMat];
//		}
//	}
//}
//oSMR.materials = _oBodySkinnedMesh._aMatsCurrent;
//}

//--------------------------------------------------------------------------	IOBJECT INTERFACE
//public void OnPropSet_BreastSize(float nValueOld, float nValueNew) {		//####DEV ####TEMP: Abstract code for all sliders
//	CGame.gBL_SendCmd("CBody", _oBodyBase._sBlenderInstancePath_CBodyBase + ".Breasts_ApplyMorph('RESIZE', 'Nipple', 'Center', 'Wide', (" + nValueNew.ToString() + "," + nValueNew.ToString() + "," + nValueNew.ToString() + ",0), None)");
//	//UpdateVertsFromBlenderMesh(false);						// Update Unity's copy of the morphing body's verts.
//	//_oBreastL.UpdateVertsFromBlenderMesh(false);		//####DEV ###TEMP
//	//_oBodyColBreast.UpdateVertsFromBlenderMesh(true);       // Update Unity's copy of the breast collider mesh
//}







//     if (_oBSkinRim != null)
//_oBSkinRim.OnUpdate();					// The very first thing we do is to get our rim to update itself so all pin positions for this frame will be refreshed...

///_oHeadLook.OnUpdate();

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
//if (_oArm_SettingRaycastPin != null && (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.LeftAlt))) {	//###INFO: When GetKeyUp is true GetKey is false!
//	_oArm_SettingRaycastPin.ArmRaycastPin_End();
//	_oArm_SettingRaycastPin = null;
//}

//if (_oActor_ArmL != null)
//	_oActor_ArmL.OnUpdate();		//###OBS? Arms need per-frame update to handle pinning to bodycol verts
//if (_oActor_ArmR != null)
//	_oActor_ArmR.OnUpdate();


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


//---------------------------------------------------------------------------	KEYBOARD HOOKS
//CKeyHook _oKeyHook_PenisBaseUpDown;
//CKeyHook _oKeyHook_PenisShaftUpDown;
//CKeyHook _oKeyHook_PenisDriveStrengthMax;
//CKeyHook _oKeyHook_ChestUpDown;
//public Dictionary<string, CBone> _mapBones = new Dictionary<string, CBone>();

