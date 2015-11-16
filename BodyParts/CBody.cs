/*###DISCUSSION: Dual-mode game functionality: Morphing and gameplay
=== REWRITE TODO ===
- Finally get fucking mesh in... too many vert groups per bone! (Because bad body morph??)
- Continue CBody ctor to split everything
- Swtich to real body... work on other meshes

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
using System;
using System.IO;
using System.Collections.Generic;



public class CBody : IObject, IHotSpotMgr { 		// Manages a 'body':  Does not actually represent a mesh (most of the skinned body represtend by _oBodySkinnedMesh;
	// The main purpose of this class is to act as a bridge between the bone structure used for animation of the main skinned mesh and enable our connected soft-body parts to move along with us via our rim mesh and the pins it updates with PhysX

	//---------------------------------------------------------------------------	VISIBLE PROPERTIES
						public 	int					_nBodyID;				// The 0-based numeric ID of our body.  Extremely important.  Matches Blender's CBody.nBodyID as well
						public	string				_sBodyPrefix;			// The 'body prefix' string used to identify Blender & Unity node for this body (Equals to 'BodyA', 'BodyB', 'BodyC', etc)
						public 	string				_sHumanCharacterName;	// The human first-name given to this character... purely cosmetic
	[HideInInspector]	public 	CObject				_oObj;					// The user-configurable object representing this body.
	[HideInInspector]	public 	CBSkin				_oBodySkinnedMesh;		// The main skinned mesh representing most of the body (e.g. everything except detached soft body parts, head, hair, clothing, etc)
	[HideInInspector]	public 	CHeadLook			_oHeadLook;             // Object in charge of moving head toward points of interest
	[HideInInspector]	public 	EBodySex			_eBodySex;				// The body's sex (man, woman, shemale)
	[HideInInspector]	public 	bool				_bIsCumming;			// Character is currently cumming.  Set globally

	//---------------------------------------------------------------------------	IMPLEMENTATION MEMBERS
	[HideInInspector]	public 	CObject				_oObjBodyDef;			// The body object definition that contains the important properties needed to initialize this body. (body sex, what clothing, etc)
	[HideInInspector]	public 	CHotSpot			_oHotSpot;				// Hotspot at the head of the character.  Enables user to change the important body properties by right-clicking on the head
	[HideInInspector]	public 	CBSkinBaked 		_oBSkinRim;			//###DESIGN!!!! Rename the old 'rim'?
	[HideInInspector]	public 	CBBodyCol			_oBodyCol;				// The full 'Body Collider' that repells fluid, hands, softbody breasts, etc from this human body.	###OPT!!!: Cut into 'body parts' that activate/deactivate when something is near
	[HideInInspector]	public 	Transform			_oBonesT;				// The 'Root' bone node right off of our top-level node with the name of 'Bones' = The body's bone tree
	[HideInInspector]	public 	Transform			_oBaseT;				// The 'Root' pin node right off of our top-level node with the name of 'Base' = The body's pins (controlling key bones through PhysX joints)
	[HideInInspector]	public 	CScriptPlay			_oScriptPlay;			// Scripting interpreter in charge of loading user scripts that programmatically control this body (poses also load this way)
	[HideInInspector]	public 	CFace				_oFace;
	[HideInInspector]	public 	string				_sNamePose;					// The name of the current loaded pose (Same as filename in /Poses directory)

	//---------------------------------------------------------------------------	SOFT BODY PARTS
	[HideInInspector]	public	CBreasts 			_oBreasts;					// The possible soft-body + related clothing parts.  What is filled in depends on what kind of character we are (man, woman, shemale)
	[HideInInspector]	public	CBodyColBreasts		_oBodyColBreasts;			// The breast collider mesh.  Used in PhysX3 to repell cloth
	[HideInInspector]	public	CPenis				_oPenis;
	[HideInInspector]	public	CVagina				_oVagina;
	[HideInInspector]	public	List<CBSoft>		_aSoftBodies	= new List<CBSoft>();		// List of all our _oSoftBodiesXXX above... used to simplify iterations.

	//---------------------------------------------------------------------------	CLOTHING		//###DESIGN: ###CHECK?
	[HideInInspector]	public	List<CBCloth>		_aCloths		= new List<CBCloth>();		// List of our simulated cloth.  Used to iterate during runtime

	//---------------------------------------------------------------------------	ACTORS
	[HideInInspector]	public 	CActorNode			_oActor_Base;			// The smart 'actors' associated with our body.  Adds much intelligent functionality!
	[HideInInspector]	public 	CActorNode			_oActor_Torso;
	[HideInInspector]	public 	CActorPelvis		_oActor_Pelvis;
	[HideInInspector]	public 	CActorChest			_oActor_Chest;
	[HideInInspector]	public 	CActorArm 			_oActor_ArmL;
	[HideInInspector]	public 	CActorArm 			_oActor_ArmR;
	[HideInInspector]	public 	CActorLeg 			_oActor_LegL;
	[HideInInspector]	public 	CActorLeg 			_oActor_LegR;
	[HideInInspector]	public 	List<CActor>		_aActors;				// An array containing all the _oActor_xxx elements above.  Used to simplify iterations.

	//---------------------------------------------------------------------------	SCRIPT ACCESS
	[HideInInspector]	public 	CObject				 Base;		// Flattened references to the oObj CObject member of our actors.  Done to offer scripting runtime simplified access to our scriptable members using friendlier names.
	[HideInInspector]	public 	CObject				 Torso;
	[HideInInspector]	public 	CObject				 Chest;
	[HideInInspector]	public 	CObject				 Pelvis;
	[HideInInspector]	public 	CObject				 ArmL;
	[HideInInspector]	public 	CObject				 ArmR;
	[HideInInspector]	public 	CObject				 LegL;
	[HideInInspector]	public 	CObject				 LegR;
	[HideInInspector]	public 	CObject				 Face;
	[HideInInspector]	public 	CObject				 Penis;

	//---------------------------------------------------------------------------	KEYBOARD HOOKS
	CKeyHook _oKeyHook_PenisBaseUpDown;
	CKeyHook _oKeyHook_PenisShaftUpDown;
	CKeyHook _oKeyHook_PenisDriveStrengthMax;
	CKeyHook _oKeyHook_ChestUpDown;

	//---------------------------------------------------------------------------	MISC
	CActorArm _oArm_SettingRaycastPin;      // The arm we are currently searching for raycasting hand target (when user placing hands)

	//---------------------------------------------------------------------------
	public bool _bForMorphingOnly;								// Body is currently in 'morphing mode' only (not gameplay) ###DEV To enum?


	public CBody(int nBodyID) {
		_nBodyID = nBodyID;
		bool bForMorphingOnly = (CGame.INSTANCE._GameMode == EGameModes.MorphNew_TEMP);				//####DEV!!!!
		Debug.Log(string.Format("+ Creating body #{0}", _nBodyID));


		_sBodyPrefix = CBody.GetNameGameBodyFromPrefix(_nBodyID);		// The name of the Blender-side object that will store our property is the base mesh named like 'BodyA', 'BodyB', etc
		_oObj = new CObject(this, _nBodyID, typeof(EBodyDef), "Body", null, _sBodyPrefix);	//###NOTE: Note that this is a 'Blender-enabled' CObject with some properties existing in Blender!
		_oObj.PropGroupBegin("", "", true);
		_oObj.PropAdd(EBodyDef.Sex,				"Sex",				typeof(EBodySex), (int)EBodySex.Woman,	"", CProp.Local);
		_oObj.PropAdd(EBodyDef.ClothingTop,		"Top Clothing",		typeof(EBodyClothingTop_HACK), 0, "", CProp.Local);		//###HACK!!!!
		_oObj.PropAdd(EBodyDef.ClothingBottom,	"Bottom Clothing",	typeof(EBodyClothingBottom_HACK), 0, "", CProp.Local + CProp.Hide);	//###BROKEN: Need to switch off vagina soft body!!
		_oObj.PropAdd(EBodyDef.Hair,			"Hair",				typeof(EBodyHair), 0,	"", CProp.Local);
		_oObj.PropAdd(EBodyDef.BtnUpdateBody,	"Update Body",		0,	"", CProp.Local | CProp.AsButton);
		_oObj.PropAdd(EBodyDef.BreastSize_OBSOLETE,		"Breast Size",		1.0f, 0.5f, 2.5f, "", CProp.Local);
		_oObj.FinishInitialization();

		//=== Give some reasonable defaults to use when game loads ===		###TODO: Load these from the user's last used body definitions!		####TEMP ####DESIGN: Load from user pref or file?  NOT IN CODE!!
		if (_nBodyID == 0) {
			_oObj.PropSet(EBodyDef.Sex,				(int)EBodySex.Woman);
			_oObj.PropSet(EBodyDef.Hair, (int)EBodyHair.TiedUp);
//			oObj.PropSet(EBodyDef.Hair, (int)EBodyHair.Messy);
//			oObj.PropFind(EBodyDef.ClothingTop)._nPropFlags |= CProp.Hide;		//###HACK!!!!
//			oObj.PropFind(EBodyDef.BreastSize)._nPropFlags |= CProp.Hide;
//			oObj.PropFind(EBodyDef.Hair)._nPropFlags |= CProp.Hide;
		} else {
			//_oObj.PropSet(EBodyDef.Sex,				(int)EBodySex.Man);		####REVB
			_oObj.PropSet(EBodyDef.Sex,				(int)EBodySex.Woman);
			//oObj.PropSet(EBodyDef.ClothingTop, (int)EBodyClothingTop_HACK.TiedTop);
			_oObj.PropSet(EBodyDef.Hair, (int)EBodyHair.TiedUp);
			//if (CGame.INSTANCE._bRunningInEditor)		//###TEMP! :)
			//	oObj.PropSet(EBodyDef.BreastSize, 1.3f);
		}
	}

	public void DoInitialize() { 
		//=== Create default values for important body parameters from the sex ===
		string sMeshSource = "";
		string sNameSrcGenitals = "";
		EBodySex eBodySex = (EBodySex)_oObj.PropGet((int)EBodyDef.Sex);

		if (eBodySex == EBodySex.Man) {
			sMeshSource = "ManA";
			_sHumanCharacterName = (_nBodyID == 0) ? "Karl" : "Brent";          //###IMPROVE: Database of names?  From user???
		} else {
			sMeshSource = "WomanA";
			_sHumanCharacterName = (_nBodyID == 0) ? "Emily" : "Eve";
		}

		switch (eBodySex) {									//###CHECK	####TEMP ####DESIGN: Loaded from file or user top-level selection! ####DESIGN: Public properties?
			case EBodySex.Man:
				sNameSrcGenitals = "PenisM-Erotic9-A-Big";
				break;
			case EBodySex.Woman:
				sNameSrcGenitals = "Vagina-Erotic9-A";					//###DESIGN??? Crotch and not vagina???
				break;
			case EBodySex.Shemale:
				sNameSrcGenitals = "PenisW-Erotic9-A-Big";				//###TODO: Comes from GUI!
				break;
		}


		//===== CREATE THE BODY IN BLENDER =====  
		CGame.gBL_SendCmd("CBody", "CBody_Create(" + _nBodyID.ToString() + ", '" + sMeshSource + "', '" + eBodySex.ToString() + "','" + sNameSrcGenitals + "')");		// This new instance is an extension of this Unity CBody instance and contains most instance members

		
		//=== Instantiate the proper prefab for our body type (Man or Woman), which defines our bones and colliders ===
		GameObject oBodyTemplateGO = Resources.Load("Prefabs/Prefab" + sMeshSource, typeof(GameObject)) as GameObject;		//###TODO: Different gender / body types enum that matches Blender	//oBody._sMeshSource + 
		GameObject oBodyGO = GameObject.Instantiate(oBodyTemplateGO) as GameObject;
		oBodyGO.SetActive(true);			// Prefab is stored with top object deactivated to ease development... activate it here...
		_oBodySkinnedMesh = (CBSkin)CBMesh.Create(oBodyGO, this, "oMeshMorph", "CBody", "GetMesh", "'SkinInfo'", typeof(CBSkin));       // Get the prepared skinned-version of the body Blender CBody constructed for us

		//=== Obtain references to needed sub-objects of our prefab ===
		_oBonesT	= _oBodySkinnedMesh.transform.FindChild("Bones");			// Set key nodes of Bones and Base we'll need quick access to over and over.
		_oBaseT		= _oBodySkinnedMesh.transform.FindChild("Base");

		//=== Create a hotspot at the character's head the user can use to invoke our (important) context menu ===
		Transform oHeadT = FindBone("chest/neck/head");         // Our hotspot is located on the forehead of the character.
		_oHotSpot = CHotSpot.CreateHotspot(this, oHeadT, this._sHumanCharacterName, false, new Vector3(0, 0.09f, 0.03f), 2.0f);		//###IMPROVE: Get these offsets from pre-placed nodes on bone structure??

		////=== Create the 'skinned rim' skinned mesh that is baked at every frame to enable all softbody bits and dynamic clothing items to attach to the current shape of the main skinned body ===
		//if (_bForMorphingOnly == false)
		//	_oBSkinRim = (CBSkinBaked)CBMesh.Create(null, this, _sNameGameBody, G.C_NameSuffix_BodyRim, "Client", "gBL_GetMesh", "'SkinInfo'", typeof(CBSkinBaked));

		////=== Create the various soft-body mesh parts that are dependant on the body sex ===
		////###IMPROVE!!!! Parse array of Blender-pushed chunks into our parts (instead of pulling like below?)
		//if (_bForMorphingOnly == false) {
		//	if (_eBodySex == EBodySex.Woman || _eBodySex == EBodySex.Shemale) {
		//		_aSoftBodies.Add(_oBreasts = (CBreasts)CBMesh.Create(null, this, _sNameGameBody, "_Detach_Breasts", "Client", "gBL_GetMesh", "'SkinInfo'", typeof(CBreasts)));           //###WEAK: Create utility function like before???
		//		_oBodyColBreasts = (CBodyColBreasts)CBMesh.Create(null, this, _sNameGameBody, "-BreastCol-ToBreasts", "Client", "gBL_GetMesh", "'NoSkinInfo'", typeof(CBodyColBreasts));		//###NOTE: Note the '-ToBreasts' suffix this mesh has been paired to detached softbody breasts
		//	}
		//	if (_eBodySex == EBodySex.Shemale || _eBodySex == EBodySex.Man)
		//		_aSoftBodies.Add(_oPenis = (CPenis)CBMesh.Create(null, this, _sNameGameBody, "_Detach_Penis", "Client", "gBL_GetMesh", "'NoSkinInfo'", typeof(CPenis)));
		//	if (_eBodySex == EBodySex.Woman)
		//		_oVagina = new CVagina(this);

		//	//=== Create the important body collider that will repel fluid in the scene ===
		//	_oBodyCol = (CBBodyCol)CBBodyCol.Create(null, this, _sNameGameBody);
		//} else {
		//	if (_eBodySex == EBodySex.Woman || _eBodySex == EBodySex.Shemale)
		//		_oBodyColBreasts = (CBodyColBreasts)CBMesh.Create(null, this, _sNameGameBody, "-BreastCol-ToBody", "Client", "gBL_GetMesh", "'NoSkinInfo'", typeof(CBodyColBreasts));	//###NOTE: Note the '-ToBody' suffix this mesh has been paired to the main body mesh
		//}

		////####TEMP ####DESIGN ####TEMP ####MOVE
		////_aCloths.Add(CBCloth.Create(this, "BodySuit-Top2"));
		////_aCloths.Add(CBCloth.Create(this, "Rough1-Holds"));
		////_aCloths.Add(CBCloth.Create(this, "Rough2-Spreads"));
		////_aCloths.Add(CBCloth.Create(this, "BodySuit-Top-Trimmed"));
		//_aCloths.Add(CBCloth.Create(this, "FullShirt"));

		////=== Create the head look controller to look at parts of the other body ===
		_oHeadLook = _oBodySkinnedMesh.gameObject.AddComponent<CHeadLook>();			//####DESIGN: Keep for morph mode??
		_oHeadLook.OnStart(this);


		//=== Instantiate the requested hair and pin as child of the head bone ===
		EBodyHair eBodyHair = (EBodyHair)_oObj.PropGet(EBodyDef.Hair);
		if (eBodyHair != EBodyHair.None) {
			string sNameHair = "HairW-" + eBodyHair.ToString();         //###HACK!!! W extension!		###HACK!!!! Man support!!
			GameObject oHairTemplateGO = Resources.Load("Models/Characters/Woman/Hair/" + sNameHair + "/" + sNameHair, typeof(GameObject)) as GameObject;   // Hair has name of folder and filename the same.	//###HACK: Path to hair, selection control, enumeration, etc
			GameObject oHairGO = GameObject.Instantiate(oHairTemplateGO) as GameObject;
			Transform oBoneHead = FindBone("chest/neck/head");
			oHairGO.transform.parent = oBoneHead;
			oHairGO.transform.localPosition = Vector3.zero;
			oHairGO.transform.localRotation = Quaternion.identity;
			if (_eBodySex == EBodySex.Man) {            //###HACK!!!!!! To reuse messy hair for man!!
				oHairGO.transform.localPosition = new Vector3(0, 0.0f, 0.0f);
				oHairGO.transform.localScale = new Vector3(1.07f, 1.07f, 1.08f);
			}
		}

		//###HACK???  ####MOVE?
		_oBodySkinnedMesh._oSkinMeshRendNow.updateWhenOffscreen = true;           //###NOTE: Prevents from having to do expensive recalc bounds as pins move body far away from starting point...
		//####DEV
		//Body_InitActors();

		////=== Setup the keys to handle penis control on non-woman bodies ===
		//if (_eBodySex != EBodySex.Woman) {      //###MOVE?
		//	bool bSelectedBodyOnly = (CGame.INSTANCE._nNumPenisInScene_BROKEN > 1);    // Keys below are active if this body is the selected body ONLY if we have more than one man in the scene
		//	if (_oPenis != null) {
		//		_oKeyHook_PenisBaseUpDown = new CKeyHook(_oPenis._oObjDriver.PropFind(EPenis.BaseUpDown), KeyCode.Q, EKeyHookType.QuickMouseEdit, "Penis up/down", 1, bSelectedBodyOnly);
		//		_oKeyHook_PenisShaftUpDown = new CKeyHook(_oPenis._oObjDriver.PropFind(EPenis.ShaftUpDown), KeyCode.E, EKeyHookType.QuickMouseEdit, "Penis bend up/down", 1, bSelectedBodyOnly);
		//		_oKeyHook_PenisDriveStrengthMax = new CKeyHook(_oPenis._oObjDriver.PropFind(EPenis.DriveStrengthMax), KeyCode.G, EKeyHookType.QuickMouseEdit, "Penis erection", -1, bSelectedBodyOnly);
		//	}
		//}
		//_oKeyHook_ChestUpDown = new CKeyHook(_oActor_Chest._oObj.PropFind(EActorChest.Chest_UpDown), KeyCode.T, EKeyHookType.QuickMouseEdit, "Chest forward/back");

		////=== Create the face and its associated morph channels ===
		////####DEV _oFace = (CFace)CBMesh.Create(null, this, _sMeshSource, G.C_NameSuffix_Face, "Client", "gBL_GetMesh", "'NoSkinInfo'", typeof(CFace));

		////=== Reparent our actor base to the pose root so that user can move / rotate all bodies at once ===
		//_oActor_Base.transform.parent = CGame.INSTANCE._oPoseRoot.transform;
		//_oActor_Base.gameObject.name = _sBodyPrefix + "_Base";

		////=== Copy references to our actors to our script-friendly CObject variables to provide friendlier access to our scriptable objects ===
		//Base =		_oActor_Base._oObj;              // CGamePlay passed us the reference to the right (empty) static object.  We fill it here.
		//Chest =		_oActor_Chest._oObj;
		//Torso =		_oActor_Torso._oObj;
		//Pelvis =	_oActor_Pelvis._oObj;
		//ArmL =		_oActor_ArmL._oObj;
		//ArmR =		_oActor_ArmR._oObj;
		//LegL =		_oActor_LegL._oObj;
		//LegR =		_oActor_LegR._oObj;
		//Face =		_oFace._oObj;
		//Penis =		(_oPenis == null) ? null : _oPenis._oObjDriver;

		//_oScriptPlay = CUtility.FindOrCreateComponent(_oBodySkinnedMesh.transform, typeof(CScriptPlay)) as CScriptPlay;
		//_oScriptPlay.OnStart(this);

		////=== Rotate the 2nd body toward the first and separate slightly loading poses doesn't pile up one on another ===	####MOVE? ####OBS? (Depend on pose?)
		////####PROBLEM: Set separation and don't do in all game modes
		//if (_nBodyID == 0) {
		//	_oActor_Base.transform.position = new Vector3(0, 0, -CGame.C_BodySeparationAtStart);        //###DESIGN: Don't load base actor instead??  ###BUG Overwrites user setting of base!!!!
		//} else {
		//	_oActor_Base.transform.position = new Vector3(0, 0, CGame.C_BodySeparationAtStart);
		//	_oActor_Base.transform.rotation = Quaternion.Euler(0, 180, 0);      // Rotate the 2nd body 180 degrees
		//}
	}


	public void Body_InitActors() {
		//=== Fetch the body part components, assign them to easy-to-access variables (and a collection for easy iteration) and initialize them by telling them their 'side' ===
		_aActors.Add(_oActor_Base	= _oBodySkinnedMesh.transform.FindChild("Base")				.GetComponent<CActorNode>());
		_aActors.Add(_oActor_Torso	= _oBodySkinnedMesh.transform.FindChild("Base/Torso")		.GetComponent<CActorNode>());
		_aActors.Add(_oActor_Chest	= _oBodySkinnedMesh.transform.FindChild("Base/Torso/Chest")	.GetComponent<CActorChest>());
		_aActors.Add(_oActor_Pelvis	= _oBodySkinnedMesh.transform.FindChild("Base/Torso/Pelvis").GetComponent<CActorPelvis>());
		_aActors.Add(_oActor_ArmL	= _oBodySkinnedMesh.transform.FindChild("Base/ArmL")		.GetComponent<CActorArm>());
		_aActors.Add(_oActor_ArmR	= _oBodySkinnedMesh.transform.FindChild("Base/ArmR")		.GetComponent<CActorArm>());
		_aActors.Add(_oActor_LegL	= _oBodySkinnedMesh.transform.FindChild("Base/LegL")		.GetComponent<CActorLeg>());
		_aActors.Add(_oActor_LegR	= _oBodySkinnedMesh.transform.FindChild("Base/LegR")		.GetComponent<CActorLeg>());

		foreach (CActor oActor in _aActors)
			oActor.OnStart(this);
	}

	public void Destroy() {             //###BUG: We sure as heck don't destroy everything... ###IMPROVE!
		Debug.Log("CBody.OnDestroy(): " + _sBodyPrefix);
		foreach (CActor oActor in _aActors)
			GameObject.DestroyImmediate(oActor);

		GameObject.DestroyImmediate(_oActor_Base.gameObject);          // Destroy our body's entry in top-level CPoseRoot

		if (_oKeyHook_PenisBaseUpDown != null) _oKeyHook_PenisBaseUpDown.Dispose();     //###WEAK!!! Try to enable auto-dispose!!
		if (_oKeyHook_PenisShaftUpDown != null) _oKeyHook_PenisShaftUpDown.Dispose();
		if (_oKeyHook_PenisDriveStrengthMax != null) _oKeyHook_PenisDriveStrengthMax.Dispose();
		if (_oKeyHook_ChestUpDown != null) _oKeyHook_ChestUpDown.Dispose();

		foreach (CBCloth oBCloth in _aCloths)
			GameObject.DestroyImmediate(oBCloth.gameObject);
		if (_oBSkinRim != null)
			GameObject.DestroyImmediate(_oBSkinRim);
	}

	
	//---------------------------------------------------------------------------	UPDATE

	public void OnSimulatePre() {
		//		if (Input.GetKeyDown(KeyCode.F12))			//####TEMP
		//			gBL_UpdateClientVerts();

		if (_oBSkinRim != null)
			_oBSkinRim.OnSimulatePre();					// The very first thing we do is to get our rim to update itself so all pin positions for this frame will be refreshed...

		if (_oVagina != null)
			_oVagina.OnSimulatePre();

		foreach (CBSoft oBSoft in _aSoftBodies)
			oBSoft.OnSimulatePre();

		_oHeadLook.OnSimulatePre();

		//=== Arm pinning key state preocessing ===  Space = set close to camera arm, LeftAlt = faraway from camera arm
		if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftAlt)) {		// If we begin press to its key we begin the mode, optionally getting an arm to update.  we keep updating while the key is pressed and end the state on key up
			if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftAlt)) {
				CActorArm oArm = FindCloseOrFarArmFromCamera(Input.GetKeyDown(KeyCode.LeftAlt));	
				if (oArm != null) {
					_oArm_SettingRaycastPin = oArm;
					_oArm_SettingRaycastPin.ArmRaycastPin_Begin();
				}
			}
			if (_oArm_SettingRaycastPin != null)
				_oArm_SettingRaycastPin.ArmRaycastPin_Update();
		}
		if (_oArm_SettingRaycastPin != null && (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.LeftAlt))) {	//###LEARN: When GetKeyUp is true GetKey is false!
			_oArm_SettingRaycastPin.ArmRaycastPin_End();
			_oArm_SettingRaycastPin = null;
		}

		_oActor_ArmL.OnSimulatePre();		// Arms need per-frame update to handle pinning to bodycol verts
		_oActor_ArmR.OnSimulatePre();

		//=== Execute ejaculation if selected body and the pertinent global property is set ===//###BUG!!!!: Won't correctly process two of the same sex!!!
		if (_bIsCumming) {
			if (_eBodySex == EBodySex.Woman) {		//###IMPROVE!!!!!: Woman cum can be a lot better!

				//=== Set the position of the one fluid emitter instance to transform position of vagina entrance bone ===
				ErosEngine.Object_SetPositionOrientation(CGame.INSTANCE._oFluid._oObj._hObject, 0, _oVagina._oVaginaCumEmitterT.position, _oVagina._oVaginaCumEmitterT.rotation);	// Update the position of our fluid.  Penis tip controls the location of this global object // If woman body is selected and we're cumming place the one fluid emitter at the 'Vagina-CumEmitter' bone

				float nCycleTime = 8;		//###TUNE	###IMPROVE: Give user GUI access to these??
				float nMaxRate = 300;		//###TUNE!!!
				CGame.INSTANCE._oFluid._oObj.PropSet(EFluid.EmitVelocity, 0.00f);		//###TUNE

				//=== Set the emit velocity & rate from the pertinent configuration curve ===
				float nTimeInEjaculationCycle = (Time.time - CGame.INSTANCE._nTimeStartOfCumming) % nCycleTime;
				float nTimeInEjaculationCycle_Normalized = nTimeInEjaculationCycle / nCycleTime;		//###DESIGN!!! Move curve here?  How to persist??
				float nEmitRate = nMaxRate * Mathf.Max(CGame.INSTANCE.CurveEjaculateWoman.Evaluate(nTimeInEjaculationCycle_Normalized), 0);		//###NOTE: We assume both the ejaculation curve's X (time) value and Y values (strenght) go from 0 to 1

				CGame.INSTANCE._oFluid._oObj.PropSet(EFluid.EmitRate, nEmitRate);		// Woman ejaculation is a rate-based emitter where we control the rate over time	###DESIGN: Would going pressure-base like man have any sense here??

			} else {

				//=== Set the position of the one fluid emitter instance to transform position of penis tip ===
				ErosEngine.Object_SetPositionOrientation(CGame.INSTANCE._oFluid._oObj._hObject, 0, _oPenis._oPenisTip._oHotSpot.transform.position, _oPenis._oPenisTip._oHotSpot.transform.rotation);	// Update the position of our fluid.  Penis tip controls the location of this global object	// If man's body is selected place at hotspot of penis tip conveniently located at uretra.

				float nCycleTime	= _oPenis._oPenisTip._oObj.PropGet(EPenisTip.CycleTime);
				float nMaxVelocity	= _oPenis._oPenisTip._oObj.PropGet(EPenisTip.MaxVelocity);

				//=== Set the emit velocity & rate from the pertinent configuration curve ===
				float nTimeInEjaculationCycle = (Time.time - CGame.INSTANCE._nTimeStartOfCumming) % nCycleTime;
				float nTimeInEjaculationCycle_Normalized = nTimeInEjaculationCycle / nCycleTime;		//###DESIGN!!! Move curve here?  How to persist??
				float nEmitVelocity = nMaxVelocity * Mathf.Max(CGame.INSTANCE.CurveEjaculateMan.Evaluate(nTimeInEjaculationCycle_Normalized), 0);		//###NOTE: We assume both the ejaculation curve's X (time) value and Y values (strenght) go from 0 to 1

				CGame.INSTANCE._oFluid._oObj.PropSet(EFluid.EmitVelocity, nEmitVelocity);	// Man ejaculation is a pressure-based emitter where we control the emit velocity over time
				float nEmitRate = (nEmitVelocity != 0) ? 10000 : 0;		//###WEAK: What max??  ramp up??  threshold point??	// Set a very large rate as we're a pressure-based emitter... (we are limited by neighborhood particles)
				CGame.INSTANCE._oFluid._oObj.PropSet(EFluid.EmitRate, nEmitRate);
			}
		}
		if (Input.GetKeyDown(KeyCode.R))		//###DESIGN: Selected only??
			ResetPinsToBones();
	}

	public void OnSimulateBetweenPhysX23() {
		foreach (CBSoft oBSoft in _aSoftBodies)					// First simulate the PhysX2 soft bodies
			oBSoft.OnSimulateBetweenPhysX23();
		if (_oBodyColBreasts != null)							// Update the breast colliders from PhysX2 breasts so they are available next call for PhysX3
			_oBodyColBreasts.OnSimulateBetweenPhysX23();
		foreach (CBCloth oBCloth in _aCloths)					// Simulate the PhysX3 cloths from the just-updated colliders above
			oBCloth.OnSimulateBetweenPhysX23();
		if (_oBodyCol != null)			//###NOTE: CBodyCol needs to simulate AFTER breasts as these have to push their global colliders for per-frame CBodyCol code to pick them up for this frame.
			_oBodyCol.OnSimulatePre();		//###CHECK: Move to later breaks anything??
	}

	public void OnSimulatePost() {
		foreach (CBSoft oBSoft in _aSoftBodies)			//###CHECK: Cloth fitting mode needs breast to run, but others???
			oBSoft.OnSimulatePost();
		foreach (CBCloth oBCloth in _aCloths)
			oBCloth.OnSimulatePost();
	}


	//---------------------------------------------------------------------------	EDIT-TIME BONE UPDATE FROM BLENDER

	public void UpdateBonesFromBlender() {						// Update our body's bones from the current Blender structure... Launched at edit-time by our helper class CBodyEd
		_oBonesT = _oBodySkinnedMesh.transform.FindChild("Bones");

		string sNameBodySrc = _sBodyPrefix;			//####BROKEN?
		if (sNameBodySrc.StartsWith("Prefab") == false)
			throw new CException("UpdateBonesFromBlender could not recognize PrefabXXX game object name " + sNameBodySrc);
		sNameBodySrc = sNameBodySrc.Substring(6);			// Remove 'Prefab' to obtain Blender body source name (a bit weak)
		CMemAlloc<byte> memBA = new CMemAlloc<byte>();
		CGame.gBL_SendCmd_GetMemBuffer("Client", "gBL_GetBones('" + sNameBodySrc + "')", ref memBA);		//###TODO: get body type from enum in body plus type!	//oBody._sMeshSource + 
		byte[] oBA = (byte[])memBA.L;
		int nPosBA = 0;

		//=== Decrypt the bytes array that Blender Python prepared for us in 'gBL_GetBones()' ===
		ushort nMagicBegin = BitConverter.ToUInt16(oBA, nPosBA); nPosBA += 2;		// Read basic sanity check magic number at start
		if (nMagicBegin != G.C_MagicNo_TranBegin)
			throw new CException("ERROR in CBodyEd.UpdateBonesFromBlender().  Invalid transaction begin magic number!");

		//=== Read the recursive bone tree.  The mesh is still based on our bone structure which remains authoritative but we need to map the bone IDs from Blender to Unity! ===
		ReadBone(ref oBA, ref nPosBA, _oBonesT);		//###IMPROVE? Could define bones in Unity from what they are in Blender?  (Big design decision as we have lots of extra stuff on Unity bones!!!)

		CBMesh.CheckMagicNumber(ref oBA, ref nPosBA, true);				// Read the 'end magic number' that always follows a stream.

		Debug.Log("+++ UpdateBonesFromBlender() OK +++");
	}

	void ReadBone(ref byte[] oBA, ref int nPosBA, Transform oBoneParent) {							// Precise opposite of gBlender.Stream_SendBone(): Reads a bone from Blender, finds (or create) equivalent Unity bone and updates position
		string sBoneName = CUtility.BlenderStream_ReadStringPascal(ref oBA, ref nPosBA);
		Vector3	vecBone = CUtility.ByteArray_ReadVector(ref oBA, ref nPosBA);
		Transform oBone = oBoneParent.FindChild(sBoneName);
		if (oBone == null) {
			oBone = new GameObject(sBoneName).transform;
			oBone.parent = oBoneParent;
			Debug.LogWarning(string.Format("ReadBone created new bone '{0}' under parent '{1}' and set to position {2:F3},{3:F3},{4:F3}", sBoneName, oBoneParent.name, vecBone.x, vecBone.y, vecBone.z));
		} else {
			Debug.Log(string.Format("ReadBone updated bone '{0}' under parent '{1}' and set position {2:F3},{3:F3},{4:F3}", sBoneName, oBoneParent.name, vecBone.x, vecBone.y, vecBone.z));
		}
		oBone.position = vecBone;
		int nBones = oBA[nPosBA++];
		for (int nBone = 0; nBone < nBones; nBone++)
			ReadBone(ref oBA, ref nPosBA, oBone);
	}


	//---------------------------------------------------------------------------	UTILITY

	public void ResetPinsToBones() {
		ResetPinToBone("Torso", "chest/abdomen");
		ResetPinToBone("Torso/Chest", "chest");
		ResetPinToBone("Torso/Pelvis", "chest/abdomen/hip");		//###NOTE: DAZ's hip bone = our pelvis
		ResetPinToBone("Torso", "chest/abdomen");					//###WEAK: ###CHECK???  Do it twice because of different parent child relationhips between the two trees... needed???
		ResetPinToBone("Torso/Chest", "chest");
		ResetPinToBone("Torso/Pelvis", "chest/abdomen/hip");		
	}
	public void ResetPinToBone(string sPathPin, string sPathBone) {		//###IMPROVE: Fix to run during play-time... Have to re-root from CPoseRoot
		Transform oRootPinT = (_oActor_Base != null) ? _oActor_Base.transform : _oBodySkinnedMesh.transform.FindChild("Base");	// If we're in game mode we can fetch base actor, if not we have to find Bones node off our prefab tree
		Transform oRootBontT = _oBodySkinnedMesh.transform.FindChild("Bones");
		Transform oPinT = oRootPinT.FindChild(sPathPin);
		Transform oBoneT = oRootBontT.FindChild(sPathBone);
		oPinT.position = oBoneT.position;
	}
	public Transform FindBone(string sBonePath) {
		Transform oBoneT = _oBonesT.FindChild(sBonePath);
		if (oBoneT == null)
			throw new CException(string.Format("ERROR: CBody.FindBone() cannot find bone '{0}'", sBonePath));
		return oBoneT;
	}
	public Transform SearchBone(Transform oParentT, string sBoneName) {		// Recursive function that searches our entire bone tree for node name (slow!!)
		if (oParentT.name == sBoneName)
			return oParentT;
		for (int nChild = 0; nChild < oParentT.childCount; nChild++) {
			Transform oChild = oParentT.GetChild(nChild);
			Transform oFoundBone = SearchBone(oChild, sBoneName);
			if (oFoundBone != null)
				return oFoundBone;
		}
		return null;
	}
	public void SelectBody() {
		CGame.INSTANCE._nSelectedBody = _nBodyID;
		CGame.SetGuiMessage(EGameGuiMsg.SelectedBody, _sHumanCharacterName);
	}
	public bool IsBodySelected() {
		return CGame.INSTANCE._nSelectedBody == _nBodyID;
	}
	public CActorArm FindCloseOrFarArmFromCamera(bool bInvert) {		// Used by hand placement functionality to auto-select the hand to move without user selection
		if (IsBodySelected()) {		// Only the closest arm of the selected body responds
			float nDistL = (_oActor_ArmL._oJointShoulder._oTransform.position - Camera.main.transform.position).magnitude;
			float nDistR = (_oActor_ArmR._oJointShoulder._oTransform.position - Camera.main.transform.position).magnitude;
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
	public void SetIsCumming(bool bIsCumming) {	// Only one character can cum at a time as there is only one fluid.  Configure the fluid for our usage
		_bIsCumming = bIsCumming;

		if (_bIsCumming) {
			if (_eBodySex == EBodySex.Woman) {
				CGame.INSTANCE._oFluid._oObj.PropSet(EFluid.EmitType, (int)EChoice_EmitType.Rate);
				//_oObj.PropSet(EFluid.Fluid_Gravity, 0.2f);
				//_oObj.PropSet(EFluid.EmitHeightRatio, 4);			//###TODO: Much greater randomness, etc

			} else {
				CGame.INSTANCE._oFluid._oObj.PropSet(EFluid.EmitType, (int)EChoice_EmitType.Pressure);
			}
		}
	}

	//---------------------------------------------------------------------------	LOAD / SAVE
	public bool Pose_Load(string sNamePose) {
		string sPathPose = CGame.GetPathPoseFile(sNamePose);
		if (File.Exists(sPathPose) == false) {
			Debug.LogError("CBody.Pose_Load() could not find file " + sPathPose);
			return false;
		}
		_sNamePose = sNamePose;
		_oActor_Pelvis._eAnimMode = EAnimMode.Stopped;		// Cancel animation on sex bone on pose load
		CGame.INSTANCE._oGamePlay.Cum_Stop();			// Stop & clear cum upon a pose loading on any body.

		_oScriptPlay.LoadScript(sPathPose);
		_oScriptPlay.ExecuteAll();			// Execute all statements in file without pausing

		CGame.INSTANCE._oGamePlay.TemporarilyDisablePhysicsCollision();

		Debug.Log(string.Format("Pose_Load() on body '{0}' loaded '{1}'", _sBodyPrefix, _sNamePose));
		return true;
	}
	public void Pose_Save(string sNamePose) {
		_sNamePose = sNamePose;
		Directory.CreateDirectory(CGame.GetPathPoses() + _sNamePose);					// Make sure that directory path exists
		string sPathPose = CGame.GetPathPoseFile(_sNamePose);
		CScriptRecord oScriptRec = new CScriptRecord(sPathPose, "Body Pose " + sPathPose);
		foreach (CActor oActor in _aActors)
			if (oActor != _oActor_Base)						// We do not save the base actor.  User orients this to position the body in the scene.
				oScriptRec.WriteObject(oActor._oObj);
		Debug.Log(string.Format("Pose_Save() on body '{0}' saved '{1}'", _sBodyPrefix, _sNamePose));
	}
	public void Pose_Reload() {
		Pose_Load(_sNamePose);
	}
	
	public void Serialize_Actors_OBS(FileStream oStream) {
		foreach (CActor oActor in _aActors)
			oActor.Serialize_OBS(oStream);
	}

	//--------------------------------------------------------------------------	IOBJECT INTERFACE
	public void OnPropSet_BreastSize(float nValueOld, float nValueNew) {		//####DEV ####TEMP: Abstract code for all sliders
		//####BROKEN
		//CGame.gBL_SendCmd("Breasts", "Breasts_ApplyOp('" + _sNameCBodyDataMember + G.C_NameSuffix_BodyMorph + "', '" + _sMeshSource + "', 'RESIZE', 'Nipple', 'Center', 'Wide', (" + nValueNew.ToString() + "," + nValueNew.ToString() + "," + nValueNew.ToString() + ",0), None)");
		//UpdateVertsFromBlenderMesh(false);						// Update Unity's copy of the morphing body's verts.
		//CGame.gBL_SendCmd("CBBodyCol", "PairMesh_Apply('BodyA-BreastCol-ToBody', 'BodyA_BodyMorph')");			//####DEV ####MOVE??
		//_oBodyColBreasts.UpdateVertsFromBlenderMesh(true);       // Update Unity's copy of the breast collider mesh
	}

	public void OnPropSet_BtnUpdateBody(float nValueOld, float nValueNew) {
		Debug.Log("CBody: Rebuilding body " + _sBodyPrefix);
		//Pose_Save("TEMP");				// Save the current pose to a temp file so we can restore body as it was right after rebuild
		CGame.INSTANCE._oGamePlay.CreateBody(_nBodyID);		// Will destroy 'this' and rebuild entire new tree of objects & meshes all the way from Blender
		//Pose_Load("TEMP");				// Restore pose saved earlier
		CGame.INSTANCE._oGamePlay.Scene_Reload();		//###HACK?  ###DESIGN: Reload whole scene to re-init position of newly created body
	}
	public void OnPropSet_NeedReset(CProp oProp, float nValueOld, float nValueNew) {}		//###DESIGN!!! Damn this near-useless function getting a pain in the ass...


	//--------------------------------------------------------------------------	IHotspot interface

	public void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) { }
	public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {
		//###IMPROVE: Make this work by clicking on head? _oBody.SelectBody();			// Doing anything with a body's hotspot (head) selects the body
		if (eHotSpotEvent == EHotSpotEvent.ContextMenu)
			_oHotSpot.WndPopup_Create(new CObject[] { _oObjBodyDef, _oFace._oObj });		//###TEMP: Face??
	}
}

public enum EBodySide {
	Left,
	Right,
}
