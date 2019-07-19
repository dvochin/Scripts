/*###DOCS25: Sept 2017 - Rewrite of morphing body functionality

=== DEV ===
- Currently updating Blender at every morph setting... only do after commit?

=== NEXT ===

=== TODO ===
- Fix show/hide meshes
- User can close morph menu!
- Add vagina morphs

=== LATER ===
- DELAYED: Keep the concept of the CFlexCollider
	- Particle base or CFlexTriCol?
	- Show back cloth!

=== IMPROVE ===

=== DESIGN ===
- CBodyBase owned by CGame.  It begins existance when user selects which sex at game starts and persists through in/out of gameplay / body configuration cycles.
- CBodyBase owns and manages lifecycle of important CBody object... Entering morphing mode destroys CBody and all its derived objects (softbodies, cloths, etc) while entering game mode re-creates it.
- CBodyBase also exists in Blender and both sides have a 1:1 relationship.  CBodyBase Blender instance also owns its one CBody Blender-side instance which creates / destroys many Blender objects as user navigates between body morphing and game mode.

=== IDEAS ===
- Support for multiple bodysuits... all simulated simultaneously... need to show only one tho!

=== LEARNED ===

=== PROBLEMS ===

=== QUESTIONS ===

=== WISHLIST ===

*/

using UnityEngine;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

public class CBodyBase : uFlex.IFlexProcessor {
	//---------------------------------------------------------------------------	MAIN MEMBERS
	public CBody			_oBody;					// Our most important member: The gametime body object responsible for gameplay functionality (host for softbodies, dynamic clothes, etc)  Created / destroyed as we enter/leave morph or gameplay mode
	public CBMesh           _oSkinMeshMorphing;		// The skinned body morph-baked mesh we're morphing.  Changes shape as user adjusts morphing sliders.  Does not host any softbodies and can be shown to the user very quickly
	EBodyBaseModes          _eBodyBaseMode = EBodyBaseModes.Uninitialized;      // The current body base mode: (BodyMorph, CutCloth, Play mode)  Hugely important
	bool					_bIsDisabled;			// Body has been 'disabled' (invisible and not colliding in the scene in any way).  Used when another body is being edited.  (Set by Disable())
	List<ushort>			_aVertsFlexCollider;    // Collection of verts sent from Blender that determine which verts form a morphing-time Flex collider capable of repelling morph-time bodysuit master cloth.
	//---------------------------------------------------------------------------	MAIN PROPERTIES
	public int				_nBodyID;               // The 0-based numeric ID of our body where we're stored in CGame._aBodyBases[]  Extremely important.  Matches Blender's CBodyBase.nBodyID as well
	public EBodySex         _eBodySex;              // The body's sex (man, woman, shemale)
	public string           _sBlenderInstancePath_CBodyBase;                // The Blender fully-qualified instance path where our corresponding CBodyBase is accessed (from Blender global scope)
	public string           _sHumanCharacterName;   // The human first-name given to this character... purely cosmetic
	public string           _sBodyPrefix;           // The 'body prefix' string used to identify Blender & Unity node for this body (Equals to 'BodyA', 'BodyB', 'BodyC', etc)
	//---------------------------------------------------------------------------	USER INTERFACE
	public CObjBlender	_oObj;					// The Blender-implemented Object that exposes RTTI-like information for change Blender shape keys from Unity UI panels

	//---------------------------------------------------------------------------	ACTORS
	public List<CActor>			_aActors = new List<CActor>();		// An array containing all the _oActor_xxx elements below.  Used to simplify iterations.
	public CActorGenitals		_oActor_Genitals;					// The smart 'actors' associated with our body.  Adds much intelligent functionality!
	public CActorPelvis			_oActor_Pelvis;
	public CActorChest			_oActor_Chest;
	public CActorArm 			_oActor_ArmL;
	public CActorArm 			_oActor_ArmR;
	public CActorFootCenter		_oActor_FootCenter;
	public CActorLeg 			_oActor_LegL;
	public CActorLeg 			_oActor_LegR;

	//---------------------------------------------------------------------------	SCRIPT ACCESS
	public CObj				 Genitals;		// Flattened references to the oObj CObj member of our actors.  Done to offer scripting runtime simplified access to our scriptable members using friendlier names.
	public CObj				 Pelvis;
	public CObj				 Chest;
	public CObj				 ArmL;
	public CObj				 ArmR;
	public CObj				 FootCenter;
	public CObj				 LegL;
	public CObj				 LegR;
	public CObj               Penis;
	public CObj               Breasts;
	//public CObj				Vagina;
	//public CObj				Face;

	//---------------------------------------------------------------------------	###TODO14: SORT
	CUICanvas					_oCanvas_HACK;          // The fixed UI canvas responsible to render the GUI for this game mode.
	//uFlex.FlexParticles			_oFlexParticles;    // The morph-time flex collider responsible to repell master bodysuit cloth so it can morph too.  Composed of a subset of our static body mesh
	bool						_bFlexBodyCollider_ParticlesUpdated; // When true a user-morph updated the body's vert and we need to update particle positions at the next opportunity. (in PreContainerUpdate())
	//public CBMeshFlex			_oClothSrc;				// The source clothing instance.  This 'bodysuit-type' full cloth will be simulated as the user adjusts morphing sliders so as to set the master cloth to closely match the body's changing shape
	//public CClothEdit			_oClothEdit;            // The one-and-only cutting (editing) cloth.  Only valid during cloth cutting game mode.

	public GameObject           _oBodyRootGO;           // The root game object of every Unity object for this body.  (Created from prefab)
	public Transform            _oBoneRootT;            // The 'Root' bone node right off of our top-level node with the name of 'Bones' = The body's bone tree
	//public Transform            _oGenitalsT;          // The 'Root' pin node right off of our top-level node with the name of 'Base' = The body's pins (controlling key bones through PhysX joints)
	public string               _sNameBodyDef;			// The name of the body definition files currently loaded
	//public string               _sNamePose;           // The name of the current loaded pose (Same as filename in /Poses directory)
    public CGame                _oGame_HACK;            // For CEvalProxy.  ###WEAK
    public float                _nAngleBodyPelvisToCam;  // Angle between this body's pelvis pin to the camera (as last calculated by FindLeftRightBody() to find which body each VR wand controls)

	public CBodyBase(int nBodyID, EBodySex eBodySex, string sNameBodyDef) {
		_nBodyID = nBodyID;
		_eBodySex = eBodySex;
		_sBodyPrefix = "Body" + _nBodyID.ToString();
        _oGame_HACK = CGame.INSTANCE;

        //=== Create default values for important body parameters from the sex ===
        if (_eBodySex == EBodySex.Man) {
			_sHumanCharacterName = (_nBodyID == 0) ? "Karl" : "Brent";          //###IMPROVE: Database of names?  From user???
		} else {
			_sHumanCharacterName = (_nBodyID == 0) ? "Emily" : "Eve";
		}

        //=== Instantiate the proper prefab for our body type (Man or Woman), which defines our bones and colliders ===
        Transform oBodyRootT = CUtility.InstantiatePrefab<Transform>("Prefabs/Woman", _sBodyPrefix);       //###TODO: Different gender / body types enum that matches Blender	//oBody._sMeshSource + 
        _oBodyRootGO = oBodyRootT.gameObject;

        //=== Obtain references to needed sub-objects of our prefab ===
        _oBoneRootT	= CUtility.FindChild(_oBodyRootGO.transform, "Bones");            // Set key nodes of Bones and Base we'll need quick access to over and over.
		//_oGenitalsT	= new GameObject("Genitals").transform;						// Create root actor / pin Genitals
		//_oGenitalsT.SetParent(_oBodyRootGO.transform);

		//===== CREATE THE BODY IN BLENDER PYTHON =====
		CGame.gBL_SendCmd("CBody", "CBodyBase_Create(" + _nBodyID.ToString() + ", '" + eBodySex.ToString() + "')");       // This new instance is an extension of this Unity CBody instance and contains most instance members
		_sBlenderInstancePath_CBodyBase = "CBodyBase_GetBodyBase(" + _nBodyID.ToString() + ")";                 // Simplify access to Blender CBodyBase instance

		//===== UPDATE BONE HIERARCHY FROM BLENDER =====
		//#DEV26:??? Nothing showing!
		Dictionary<string, CBone> mapBonesFlattened = new Dictionary<string, CBone>();      // Flattened collection of bones extracted from Blender.  Includes dynamic bones.  Used to speed up CBSkin skinning info de-serialization
		CBone.BoneUpdate_UpdateFromBlender(_sBlenderInstancePath_CBodyBase, _oBoneRootT, ref mapBonesFlattened);

		//=== Download our morphing skinned body from Blender ===
		GameObject oBodyBaseGO = new GameObject(_sBodyPrefix);        // Create the root Unity node that will keep together all the many sub-nodes of this body.
		_oSkinMeshMorphing = CBMesh.Create(oBodyBaseGO, this, ".oSkinMeshMorph", typeof(CBSkin));
		_oSkinMeshMorphing.transform.SetParent(_oBodyRootGO.transform);
		_oSkinMeshMorphing.name = _sBodyPrefix + "-Morphing";
		_oSkinMeshMorphing.GetComponent<SkinnedMeshRenderer>().enabled = (CGame._eBodyToShow_HACK == CGameBodyToShow.Presentation);

		//=== DEFINE THE FLEX COLLIDER: Read the collection of verts that will from the Flex collider (responsible to repell master Bodysuit from morph-time body) ===
		_aVertsFlexCollider = CByteArray.GetArray_USHORT("'CBody'", _sBlenderInstancePath_CBodyBase + ".aVertsFlexCollider.Unity_GetBytes()");

		//=== Create Canvas for GUI for this mode ===
		_oCanvas_HACK = CUICanvas.Create(_oSkinMeshMorphing.transform);
		_oCanvas_HACK.transform.position = new Vector3(0.31f, 1.35f, 0.13f);            //###WEAK11: Hard-coded panel placement in code?  Base on a node in a template instead??  ###IMPROVE: Autorotate?

		//=== Create the managing object and related hotspot ===
		_oObj = new CObjBlender("Body Morphing", _sBlenderInstancePath_CBodyBase + ".oObjectMeshShapeKeys", this);
		_oObj.Event_PropertyValueChanged += Event_PropertyChangedValue;
		_oObj.ObtainBlenderObjects();
		///_oCanvas_HACK.CreatePanel("Body Morphing", _oObj);         //###IMPROVE:21!!!: Find way for GUI to not crash when props are null!!

		//=== Define the pin actors ===
		Actors_Init();

        //###DEV27: HACK POSITIONING!!      //###TODO: Static move!!
        //if (CGame._nNumBodies_HACK == 2) {
        //    if (_nBodyID == 0) {
	       //     _oActor_Genitals.transform.position += new Vector3(0, 0, -0.3f);
        //    } else {
	       //     _oActor_Genitals.transform.position += new Vector3(0, 0, 0.3f);
	       //     _oActor_Genitals.transform.rotation *= Quaternion.Euler(0, 180, 0);      // Rotate the 2nd body 180 degrees
        //    }
        //}

		//=== Load the specified body off the definition files ===
		//BodyDef_Load(sNameBodyDef);
	}

	//public void BodyDef_Load(string sNameBodyDef = null) {
	//	if (sNameBodyDef != null)
	//		_sNameBodyDef = sNameBodyDef;
	//	_oObj.Load("Body",	_sNameBodyDef);		//###DESIGN: Body always made of these three property group filters?
	//	_oObj.Load("Penis",	_sNameBodyDef);
	//	_oObj.Load("Breasts", _sNameBodyDef);
	//	_oObj.Load("Vagina",	_sNameBodyDef);
	//}

	//public void BodyDef_Save(string sNameBodyDef = null) {
	//	if (sNameBodyDef != null)
	//		_sNameBodyDef = sNameBodyDef;
	//	_oObj.Save("Body",	_sNameBodyDef);
	//	_oObj.Save("Penis",	_sNameBodyDef);
	//	_oObj.Save("Breasts", _sNameBodyDef);
	//	_oObj.Save("Vagina",	_sNameBodyDef);
	//}

	public void Actors_Init() {
		////=== Fetch the body part components, assign them to easy-to-access variables (and a collection for easy iteration) and initialize them by telling them their 'side' ===
		_aActors.Add(_oActor_Genitals	= CreateActor<CActorGenitals>   (_oBodyRootGO.transform,		"Genitals")     as CActorGenitals);
		_aActors.Add(_oActor_Pelvis		= CreateActor<CActorPelvis>     (_oActor_Genitals.transform,	"Pelvis")       as CActorPelvis);
        _aActors.Add(_oActor_Chest		= CreateActor<CActorChest>      (_oActor_Genitals.transform,	"Chest")        as CActorChest);
        _aActors.Add(_oActor_ArmL		= CreateActor<CActorArm>        (_oActor_Chest.transform,		"ArmL")         as CActorArm);
        _aActors.Add(_oActor_ArmR		= CreateActor<CActorArm>        (_oActor_Chest.transform,		"ArmR")         as CActorArm);
        _aActors.Add(_oActor_FootCenter = CreateActor<CActorFootCenter> (_oActor_Genitals.transform,	"FootCenter")   as CActorFootCenter);
        _aActors.Add(_oActor_LegL		= CreateActor<CActorLeg>        (_oActor_FootCenter.transform,	"LegL")         as CActorLeg);
        _aActors.Add(_oActor_LegR		= CreateActor<CActorLeg>        (_oActor_FootCenter.transform,	"LegR")         as CActorLeg);

        ////=== Copy references to our actors to our script-friendly CObj variables to provide friendlier access to our scriptable objects ===
        Genitals    = _oActor_Genitals._oObj;                       // CGamePlay passed us the reference to the right (empty) static object.  We fill it here.
		Pelvis      = _oActor_Pelvis._oObj;
		Chest       = _oActor_Chest._oObj;
		ArmL        = _oActor_ArmL._oObj;
		ArmR        = _oActor_ArmR._oObj;
		FootCenter  = _oActor_FootCenter._oObj;
		LegL        = _oActor_LegL._oObj;
		LegR        = _oActor_LegR._oObj;
		//Penis       = _oSoftBody_Penis ? _oSoftBody_Penis._oObj : null;		#DEV26:????
		//Breasts     = _oSoftBody_BreastL ? _oSoftBody_BreastL._oObj : null;     // "Breasts" point to left breast as it automatically copies all its commands to the right breast to maintain sync
		//Face        = null;/// _oFace._oObj;

		//=== Reset all the actors to their default positions ===
		SetActorPosToBonePos();

		//if (false) {				//###DEBUG21: Temp code to directly manipulate bones via GUI
		//	//=== Iterate through all our CBone instances and connect them to this body ===		###MOVE20:?
		//	//object[] aBones = _oBoneRootT.GetComponentsInChildren(typeof(CBone), true);
		//	_oObj = new CObj(this, "Body Direct Bones", "Body Direct Bones", _oObj);
		//	_oObj.Event_PropertyValueChanged += Event_PropertyChangedValue;
		//	foreach (CActor oActor in _aActors) { 
		//		if (oActor._eBodySide != EBodySide.Right) {			// Only show center and left to save GUI space
		//			foreach (CBone oBone in oActor._aBones) {
		//				for (int nBoneRot = 0; nBoneRot < oBone._aBoneRots.Length; nBoneRot++) { 
		//					CBoneRot oBoneRot = oBone._aBoneRots[nBoneRot];
		//					if (oBoneRot != null) {			// Our three possbile slots (x,y,z) are not necessarily defined for all bones...
		//						string sPropName = oBone.gameObject.name + " " + oBoneRot._chAxis + ":" + oBoneRot._sNameRotation;
		//						CObj oObj = _oObj.Add(null, sPropName, oBoneRot._nValue, oBoneRot._nMin, oBoneRot._nMax, sPropName);
		//						oObj._oObjectExtraFunctionality = oBoneRot;			// Store back-reference so OnPropertyChanged can readily adjust the bone
		//					}
		//				}
		//			}
		//		}
		//	}
		//	//_oCanvas_HACK = CUICanvas.Create(_oBodySkinnedMeshGO_HACK.transform);
		//	//_oCanvas_HACK.transform.position = new Vector3(0.31f, 1.35f, 0.13f);            //###WEAK11: Hardcoded panel placement in code?  Base on a node in a template instead??  ###IMPROVE: Autorotate?
		//	//_oCanvas_HACK.CreatePanel("Body Direct Bone", _oObj);
		//}
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

	CActor CreateActor<T>(Transform oParentT, string sNameActor) where T : CActor {
        T oT = CUtility.InstantiatePrefab<T>("Prefabs/CActor", sNameActor, oParentT);
		oT.OnStart(this);       //#DEV26: CVrActor??
		return oT;
	}

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
		Transform oRootPinT = (_oActor_Genitals != null) ? _oActor_Genitals.transform : CUtility.FindChild(_oBodyRootGO.transform, "Genitals");	// If we're in game mode we can fetch base actor, if not we have to find Bones node off our prefab tree
		Transform oPinT = CUtility.FindChild(oRootPinT, sPathPin);
		Transform oBoneT = FindBoneByPath(sPathBone);
		oPinT.position = oBoneT.position;
	}

    //---------------------------------------------------------------------------	LOAD / SAVE
    public bool Pose_Load(string sPoseData) {       //#
        //return true;//#DEV26: TEMP

        byte[] aBytesCompressed = Convert.FromBase64String(sPoseData);
        using (MemoryStream oMS = new MemoryStream(aBytesCompressed))
        using (DeflateStream oDS = new DeflateStream(oMS, CompressionMode.Decompress))        //###INFO: Using 'using' is great for automatic disposing!
        using (BinaryReader oBR = new BinaryReader(oDS)) {
            foreach (CActor oActor in _aActors)
                oActor.Load(oBR);
        }
        return true;
	}
	public string Pose_Save() {       //#DEV26: Only saves the actor pins and not the bone angles!  REDESIGN??
        MemoryStream oMS = new MemoryStream();                                              //###IMPROVE: Create a helper class that always uses this 'sandwitch' to load and save everything into/from Base64 strings
        using (DeflateStream oDS = new DeflateStream(oMS, CompressionMode.Compress))        //###INFO: Using 'using' is great for automatic disposing!
        using (BinaryWriter oBW = new BinaryWriter(oDS)) {
            foreach (CActor oActor in _aActors)
                oActor.Save(oBW);
        }
        String sPoseData = Convert.ToBase64String(oMS.ToArray());
        oMS.Close();
        Debug.Log(string.Format("Pose_Save() on body '{0}' as data '{1}'", _sBodyPrefix, sPoseData));
        return sPoseData;
        //Pose_Load(sBase64);
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








	void FlexObject_BodySkinnedBody_Enable() {
		//#DEV26:BROKEN
		////=== Define Flex particles from Blender mesh made for Flex ===
		//if (_oFlexParticles == null) { 
		//	int nParticles = _aVertsFlexCollider.Count;
		//	_oFlexParticles = CUtility.CreateFlexObjects(_oMeshSkinnedBody.gameObject, this, nParticles, uFlex.FlexInteractionType.SelfCollideFiltered, Color.grey);		//###TODO14: Flex Colors!
		//	for (int nParticle = 0; nParticle < nParticles; nParticle++) {
		//		//int nVertWholeMesh = _aVertsFlexCollider[nParticle];				// Lookup the vert index in the full mesh from the Blender-supplied array that isolates which verts participate in the Flex collider
		//		//_oFlexParticles.m_particles[nParticle].pos = _oSkinMeshMorph._memVerts.L[nVertWholeMesh];		// Don't position here to insure only one place positions to avoid bugs / confusion. (in PreContainerUpdate()) 
		//		_oFlexParticles.m_restParticles[nParticle].invMass = _oFlexParticles.m_particles[nParticle].invMass = 0;					// We're a static collider with every particle moved by this code at every morph... (e.g. All particles are not Flex simulated)
		//		_oFlexParticles.m_colours[nParticle] = _oFlexParticles.m_colour;
		//		_oFlexParticles.m_particlesActivity[nParticle] = true;
		//	}
		//	_bFlexBodyCollider_ParticlesUpdated = true;                          // Flag Flex to perform an update of its particle positions.
		//} else { 
		//	_oFlexParticles.gameObject.SetActive(true);					// Just re-activate if we were de-activated before in FlexObject_Deactivate()
		//}
	}

	void FlexObject_BodySkinnedBody_Disable() {
		//#DEV26:BROKEN
		////=== Destroy all configure-time Flex objects for performance.  They will have to be re-created upon re-entry into configure mode ===
		//GameObject.DestroyImmediate(_oMeshSkinnedBody.gameObject.GetComponent<uFlex.FlexParticlesRenderer>());      //###INFO: DestroyImmediate is needed to avoid CUDA errors with regular Destroy()!
		//GameObject.DestroyImmediate(_oMeshSkinnedBody.gameObject.GetComponent<uFlex.FlexProcessor>());
		//GameObject.DestroyImmediate(_oFlexParticles);
		//_oFlexParticles = null;

		////_oFlexParticles.gameObject.SetActive(false);			//####DESIGN18: Don't have to clear everything (although this step saves memory).  Keep de-activation instead??
	}

	public void OnChangeBodyMode(EBodyBaseModes eBodyBaseModeNew) {
		if (_eBodyBaseMode == eBodyBaseModeNew)
			return;

		Debug.LogFormat("MODE: Body#{0} going from '{1}' to '{2}'", _nBodyID.ToString(), _eBodyBaseMode.ToString(), eBodyBaseModeNew.ToString());

		if (_eBodyBaseMode == EBodyBaseModes.Uninitialized && eBodyBaseModeNew == EBodyBaseModes.MorphBody) {

			FlexObject_BodySkinnedBody_Enable();                         // Create / re-create the Flex objects for this static body collider

		//###BROKEN:!!!!! } else if (_eBodyBaseMode == EBodyBaseModes.MorphBody && eBodyBaseModeNew == EBodyBaseModes.Play) {

		} else if (eBodyBaseModeNew == EBodyBaseModes.Play) {

			_oSkinMeshMorphing.GetComponent<SkinnedMeshRenderer>().enabled = false;      // Hide the full-body static morphing mesh
			_oCanvas_HACK.gameObject.SetActive(false);                                  // Hide the entire morphing UI

			FlexObject_BodySkinnedBody_Disable();        // Going from CutCloth to play mode.  Destroy all configure-time Flex objects for performance.  They will have to be re-created upon re-entry into configure mode ===

            //foreach (CObj oObjCat2 in _oObj._aChildren)            // Manually dump all our Blender properties into Blender so UpdateMorphResultMesh() has the user's morph sliders.  (We don't update at every slider value change for performance)
            //    foreach (CObj oObj in oObjCat2._aChildren)
            //        oObj.Set(oObj._nValue);
            //    //oObj._nValue = float.Parse(CGame.gBL_SendCmd("CBody", _sBlenderAccessString + ".SetString('" + oObj._sName + "'," + oObj._nValue.ToString() + ")"));

            _oBody = new CBody(this);                       // Create the runtime body.  Expensive op!

		} else if (_eBodyBaseMode == EBodyBaseModes.Play && eBodyBaseModeNew == EBodyBaseModes.MorphBody) {

			_oBody = _oBody.DoDestroy();					// Destroys *everthing* related to gametime (softbodies, cloth, flex colliders, etc) on both Unity and Blender side!
			_oSkinMeshMorphing.GetComponent<SkinnedMeshRenderer>().enabled = true;     // Show the full-body static morphing mesh
            FlexObject_BodySkinnedBody_Enable();             // Make body base / morphing body visible and active again.
			_oCanvas_HACK.gameObject.SetActive(true);                               // Show the entire morphing UI

		} else {
			CUtility.ThrowExceptionF("Exception in CBodyBase.OnChangeBodyMode(): Cannot change from body mode '{0}' to '{1}'.", _eBodyBaseMode.ToString(), eBodyBaseModeNew.ToString());
		}

		//------------- VERSION WHEN WE HAD CLOTH CUTTING MODE... SOME GOOD CODE IN THERE!
		//if (_eBodyBaseMode == EBodyBaseModes.Uninitialized && eBodyBaseModeNew == EBodyBaseModes.MorphBody) {

		//	FlexObject_BodySkinnedBody_Enable();							// Create / re-create the Flex objects for this static body collider
		//	//###DISABLED19: _oClothSrc = CBMeshFlex.CreateForClothSrc(this, "BodySuit");	// Create bodycloth clothing instance.  It will be simulated as the user adjusts morphing sliders so as to set the master cloth to closely match the body's changing shape ===  ###TODO18: Obtain what cloth source from GUI

		//} else if (_eBodyBaseMode == EBodyBaseModes.MorphBody && eBodyBaseModeNew == EBodyBaseModes.CutCloth) {

		//	if (_oClothSrc != null)
		//		_oClothSrc.FlexObject_ClothSrc_Disable();
		//	_oCanvas.gameObject.SetActive(false);                               // Hide the entire morphing UI

		//	//=== Perform special processing when user has done morphing the body to update the morph mesh in Blender. Send Blender all the sliders values so it updates its morphing body for game-time body generation (This because we were morphing locally for much better performance) ===
		//	CObjGrpBlender oObjGrpBlender = _oObj._aPropGrps[0] as CObjGrpBlender;
		//	foreach (CObj oObj in oObjGrpBlender._aChildren)            // Manually dump all our Blender properties into Blender so UpdateMorphResultMesh() has the user's morph sliders.  (We don't update at every slider value change for performance)
		//		oObj._nValue = float.Parse(CGame.gBL_SendCmd("CBody", oObjGrpBlender._sBlenderAccessString + ".SetString('" + oObj._sName + "'," + oObj._nValue.ToString() + ")"));
		//	//=== Ask Blender to update its morphing body from user-selected slider choices ===
		//	CGame.gBL_SendCmd("CBody", _sBlenderInstancePath_CBodyBase + ".UpdateMorphResultMesh()");
		//	////###BROKEN18:!!!: Can no longer re-enter cloth cutting as gBlender loses access?? _oMeshSkinnedBody.UpdateVertsFromBlenderMesh(true);         // Morphing can radically change normals.  Recompute them. (Accurate normals needed below anyways for Flex collider)

		//	//###TEMP19:!!!! _oClothEdit = new CClothEdit(this, "MyShirt", "Shirt", "BodySuit", "_ClothSkinnedArea_ShoulderTop");    //###HACK18:!!!: Choose what cloth to edit from GUI choice  ###DESIGN!!!

		//} else if (_eBodyBaseMode == EBodyBaseModes.CutCloth && eBodyBaseModeNew == EBodyBaseModes.Play) {
		//	FlexObject_BodySkinnedBody_Disable();		// Going from CutCloth to play mode.  Destroy all configure-time Flex objects for performance.  They will have to be re-created upon re-entry into configure mode ===

		//	_oBody = new CBody(this);						// Create the runtime body.  Expensive op!

		//	if (_oClothEdit != null)
		//		_oClothEdit.GameMode_EnterMode_Play();

		//} else if (_eBodyBaseMode == EBodyBaseModes.Play && eBodyBaseModeNew == EBodyBaseModes.CutCloth) {

		//	_oBody = _oBody.DoDestroy();					// Destroys *everthing* related to gametime (softbodies, cloth, flex colliders, etc) on both Unity and Blender side!
		//	//_oSkinMeshMorph.GetComponent<MeshRenderer>().enabled = true;     // Show the morphing mesh
		//	FlexObject_BodySkinnedBody_Enable();             // Make body base / morphing body visible and active again.
		//	if (_oClothEdit != null)
		//		_oClothEdit.GameMode_EnterMode_EditCloth();

		//} else if (_eBodyBaseMode == EBodyBaseModes.CutCloth && eBodyBaseModeNew == EBodyBaseModes.MorphBody) {

		//	if (_oClothEdit != null)
		//		_oClothEdit = _oClothEdit.DoDestroy();
		//	if (_oClothSrc != null)
		//		_oClothSrc.FlexObject_ClothSrc_Enable();
		//	_oCanvas.gameObject.SetActive(true);                               // Show the entire morphing UI

		//} else {
		//	CUtility.ThrowExceptionF("Exception in CBodyBase.OnChangeBodyMode(): Cannot change from body mode '{0}' to '{1}'.", _eBodyBaseMode.ToString(), eBodyBaseModeNew.ToString());
		//}

		//=== Set the new game mode ===
		_eBodyBaseMode = eBodyBaseModeNew;
	}

	public void Disable() {
		//###BROKEN22: CUtility.ThrowException("TODO: Implement disable!");			//###TODO18:
		_bIsDisabled = true;
	}

	public void OnUpdate() {
		foreach (CActor oActor in _aActors)
			oActor.OnUpdate();

		//if (Input.GetKeyDown(KeyCode.R))		//###DESIGN: Selected only??
		//	SetActorPosToBonePos();

		if (_oBody != null && _oBody._bEnabled)
			_oBody.OnUpdate();
	}
	public void HideShowMeshes() {
		//###DESIGN18: Hide / show all options given the many game modes too complex... ditch?
		//_oSkinMeshMorph.GetComponent<MeshRenderer>().enabled = CGame.ShowPresentation && (_eBodyBaseMode != EBodyBaseModes.Play);
		//_oClothEdit.GetComponent<MeshRenderer>().enabled = CGame.ShowPresentation && (_eBodyBaseMode == EBodyBaseModes.Play);
		//_oClothSrc.GetComponent<MeshRenderer>().enabled = CGame.ShowPresentation;
		if (_oSkinMeshMorphing.GetComponent<uFlex.FlexParticlesRenderer>() != null)
			_oSkinMeshMorphing.GetComponent<uFlex.FlexParticlesRenderer>().enabled = CGame.ShowFlexParticles;
		//if (_oClothSrc.GetComponent<uFlex.FlexParticlesRenderer>() != null)
		//	_oClothSrc.GetComponent<uFlex.FlexParticlesRenderer>().enabled = CGame.ShowFlexParticles;
		if (_oBody != null && _oBody._bEnabled) 	//###CHECK?
			_oBody.HideShowMeshes();
	}
	public Transform FindBoneByPath(string sBonePath) {
		Transform oBoneT = _oBoneRootT.Find(sBonePath);
		if (oBoneT == null)
			CUtility.ThrowExceptionF("ERROR: CBody.FindBone() cannot find bone '{0}'", sBonePath);
		return oBoneT;
	}
	public Transform SearchBone_RECURSIVE(Transform oParentT, string sBoneName) {		// Recursive function that searches our entire bone tree for node name (slow!!)
		if (oParentT.name == sBoneName)
			return oParentT;
		for (int nChild = 0; nChild < oParentT.childCount; nChild++) {
			Transform oChild = oParentT.GetChild(nChild);
			Transform oFoundBone = SearchBone_RECURSIVE(oChild, sBoneName);
			if (oFoundBone != null)
				return oFoundBone;
		}
		return null;
	}

	void Event_PropertyChangedValue(object sender, EventArgs_PropertyValueChanged oArgs) {      // Fired everytime user adjusts a property.
		// 'Bake' the morphing mesh as per the player's morphing parameters into a 'MorphResult' mesh that can be serialized to Unity.  Matches Blender's CBodyBase.UpdateMorphResultMesh()
		Debug.LogFormat("CBodyBase updates body base '{0}' because property '{1}' changed to value '{2}'", _sBodyPrefix, oArgs.CObj._sNameInCodebase, oArgs.CObj._nValue);
		//#DEV26:OPT!!!!!!!!  Gets everything?

		//=== Process morph channel property changes ===
		if (oArgs.CObj._oObjectExtraFunctionality == null)				// Create 'Extra functionality object' for this property if this is the first time it is invoked
			oArgs.CObj._oObjectExtraFunctionality = new CMorphChannel(this, oArgs.CObj._sNameInCodebase);
		CMorphChannel oMorphChannel = oArgs.CObj._oObjectExtraFunctionality as CMorphChannel;		// Extract the 'extra functionality object' from this property:  It is a cache for morph channel to (greatly) speed up in-Unity morphing
		bool bMeshChanged = oMorphChannel.ApplyMorph(oArgs.CObj._nValue);
		if (bMeshChanged) {
			_oSkinMeshMorphing._oMeshNow.vertices = _oSkinMeshMorphing._memVerts.L;       //###IMPROVE: to some helper function?
			_oSkinMeshMorphing.UpdateNormals();						// Morphing invalidates normals... update       //###OPT:!!!! #DEV26: Do at end of all morph channel apply?
			_bFlexBodyCollider_ParticlesUpdated = true;					// Flag Flex to perform an update of its particle positions...
		}
	}

	public void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
		//#DEV26:DISABLED: Won't be needed until we re-implement cloth fitting & baking
		////=== Update the position of our flex particles so bodysuit can be repelled by the updated morph vert positions ===
		//if (_bFlexBodyCollider_ParticlesUpdated) {
		//	float nDistFlexColliderShrink = CGame.particleSpacing * CGame.nDistFlexColliderShrinkMult;
		//	for (int nParticle = 0; nParticle < _oFlexParticles.m_particlesCount; nParticle++) {
		//		int nVertWholeMesh = _aVertsFlexCollider[nParticle];                // Lookup the vert index in the full mesh from the Blender-supplied array that isolates which verts participate in the Flex collider
		//		Vector3 vecVert = _oMeshSkinnedBody._memVerts.L[nVertWholeMesh];
		//		vecVert -=  _oMeshSkinnedBody._memNormals.L[nVertWholeMesh] * nDistFlexColliderShrink;		// 'Shrink' each particle a fraction of the way inverse to its normal so it 'shrinks' inside the body to account for particle-to-particle distance and make it appear that cloth can be right over body skin.
		//		_oFlexParticles.m_particles[nParticle].pos = vecVert;
		//	}
		//	_bFlexBodyCollider_ParticlesUpdated = false;
		//}
	}

    //public void W2U_PoseUpdate_RequestPoseDataFromUnity() {
    //    Debug.Log("W2U_PoseUpdate_RequestPoseDataFromUnity");
    //    string sPoseData = Pose_Save();
    //    CGame.INSTANCE._oBrowser_HACK.SendEvalToBrowser("eGridPoses.U2W_PoseUpdate_ReturnOfRequestedPoseData('" + sPoseData + "');");       //###WEAK: Global better??
    //}
    //public void W2U_PoseSet_SetPose(string sPoseData) {
    //    Debug.Log("W2U_PoseSet_SetPose() gets PoseData: " + sPoseData);
    //    Pose_Load(sPoseData);
    //}
}


public enum EBodyBaseModes {
	Uninitialized,					// CBodyBase just got created at the start of the game and has not yet been initialized to any meaningful mode.  (Can't go back to this mode)
	MorphBody,                      // Body is now being morphed by the user via sliders to Blender (along with ClothSrc / Bodysuit).  Game-ready _oBody is null / destroyed.
	//CutCloth,						// Cloth is being cut by removing parts from the ClothSrc / Bodysuit.
	Play,							// Body is simulating in gameplay mode.  (_oBody is valid and has created softbodies / cloths / etc)
}

public enum EBodySex {
	Man,
	Woman,
	Shemale
};

//###TEST: Unit-test for multi-object property viewer
//CObjGrpEnum oObjGrpEnum = new CObjGrpEnum(_oObj, "Test Enum 1-1", typeof(EPenisTip));		
//oObjGrpEnum.Add(EPenisTip.CycleTime,   "Cycle Time",         1.0f, 0.0001f, 1000.0f, "");
//oObjGrpEnum.Add(EPenisTip.MaxVelocity,   "MaxVelocity",         1.0f, 0.0001f, 1000.0f, "");

//CObjGrp oObjGrpMisc = new CObjGrp(_oObj, "Test Misc 1-3");
//oObjGrpMisc.Add(0, "1stProp", "First Prop",         1.0f, 0.0001f, 1000.0f, "");
//oObjGrpMisc.Add(1, "2ndProp", "Second Prop",         1.0f, 0.0001f, 1000.0f, "");
//oObjGrpMisc.Add(2, "3rdProp", "Third Prop",         1.0f, 0.0001f, 1000.0f, "");

//CObj oObj2 = new CObj(this, "Test-Object2", "Test-Object2-Label");;
//CObjGrp oObjGrpMisc2 = new CObjGrp(oObj2, "Test Enum 2-1");
//oObjGrpMisc2.Add(0, "1stProp2", "First Prop2",         1.0f, 0.0001f, 1000.0f, "");
//oObjGrpMisc2.Add(1, "2ndProp2", "Second Prop2",         1.0f, 0.0001f, 1000.0f, "");
//oObjGrpMisc2.Add(2, "3rdProp2", "Third Prop2",         1.0f, 0.0001f, 1000.0f, "");




//=== Perform special processing when user has done morphing the body to update the morph mesh in Blender. Send Blender all the sliders values so it updates its morphing body for game-time body generation (This because we were morphing locally for much better performance) ===
//CObjGrpBlender oObjGrpBlender = _oObj._aPropGrps[0] as CObjGrpBlender;
//foreach (CObj oObj in oObjGrpBlender._aChildren)            // Manually dump all our Blender properties into Blender so UpdateMorphResultMesh() has the user's morph sliders.  (We don't update at every slider value change for performance)
//	oObj._nValue = float.Parse(CGame.gBL_SendCmd("CBody", oObjGrpBlender._sBlenderAccessString + ".SetString('" + oObj._sName + "'," + oObj._nValue.ToString() + ")"));
////=== Ask Blender to update its morphing body from user-selected slider choices ===
//CGame.gBL_SendCmd("CBody", _sBlenderInstancePath_CBodyBase + ".Unity_UpdateMorphResultMesh()");






//public void Pose_Reload() {
//	Pose_Load(_sNamePose);
//}
//###DESIGN: Avoid root actor serialization?			if (oActor != _oActor_Genitals)						// We do not save the base actor.  User orients this to position the body in the scene.
//###TODO: Discontinue script recorder / loader to save/load?? CScriptRecord oScriptRec = new CScriptRecord(sPathPose, "Body Pose " + sPathPose);
// oScriptRec.WriteObject(oActor._oObj);
//oScriptRec.CloseFile();

//string sPathPose = CGame.GetPath_PoseFile(sNamePose);
//if (File.Exists(sPathPose) == false) {
//	Debug.LogError("CBody.Pose_Load() could not find file " + sPathPose);
//	return false;
//}
//_sNamePose = sNamePose;
//_oActor_Pelvis._eAnimMode = EAnimMode.Stopped;		// Cancel animation on sex bone on pose load
//CGame.Cum_Stop();			// Stop & clear cum upon a pose loading on any body.

//_oScriptPlay.LoadScript(sPathPose);
//_oScriptPlay.ExecuteAll();			// Execute all statements in file without pausing

//CGame.TemporarilyDisablePhysicsCollision();

//Debug.Log(string.Format("Pose_Load() on body '{0}' loaded '{1}'", _sBodyPrefix, _sNamePose));

