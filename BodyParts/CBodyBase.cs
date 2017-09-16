/*###DISCUSSION: CBodyBase: Basis of game-time CBody and center of body modification and morphing.
=== LAST ===
- Just got body collider!  Was done quickly... review!
	- Fillin of breast plate!!
	- Simplify extra bits!
- Get chest bone working!  wtf?  Get anim working and poses!
- Base owning bodysuit done quickly... where does it go?
	- Next branch with clothing is either skinned clothing ontop of bodysuit or cutting!
- Support for multiple bodysuits... all simulated simultaneously... need to show only one tho!

- Better nodes... need to deactivate properly morph-time including particles when going to game time!

- Why is pelvis moving so poorly now?  Bones too stiff?

- Integration of Flex collider and cloth:
- Morphing now much faster... clean up the old shit!

- GUI from play mode will persist if up.
	-? GUI morph cannot re-appear?

=== NEXT ===
- Work on gametime Flex collider?

=== TODO ===
- Move all the old CreateBody() and body configuration crap
- Fix show meshes
- 'Panel Title'
- Can close morph menu!

=== LATER ===

=== IMPROVE ===

=== DESIGN ===
- CBodyBase owned by CGame.  It begins existance when user selects which sex at game starts and persists through in/out of gameplay / body configuration cycles.
- CBodyBase owns and manages lifecycle of important CBody object... Entering morphing mode destroys CBody and all its derived objects (softbodies, cloths, etc) while entering game mode re-creates it.
- CBodyBase also exists in Blender and both sides have a 1:1 relationship.  CBodyBase Blender instance also owns its one CBody Blender-side instance which creates / destroys many Blender objects as user navigates between body morphing and game mode.

=== IDEAS ===
- Explore these new VR modes!

=== LEARNED ===

=== PROBLEMS ===
- Normal problem around breasts?  (Caused by material split?)
	- What to do about missing maps of softbody???

=== QUESTIONS ===
- Should BodyBase have the only hotspot switching to other objects?

=== WISHLIST ===

*/

using UnityEngine;
using System.Collections.Generic;

public class CBodyBase : uFlex.IFlexProcessor {
	//---------------------------------------------------------------------------	MAIN MEMBERS
	public CBody			_oBody;					// Our most important member: The gametime body object responsible for gameplay functionality (host for softbodies, dynamic clothes, etc)  Created / destroyed as we enter/leave morph or gameplay mode
	public CBMesh           _oMeshStaticCollider;		//###OBS: Is particle based!  Go for new tri mesh?  The non-skinned body morph-baked mesh we're morphing.  Constantly refreshed from equivalent body in Blender as user moves morphing sliders.
	EBodyBaseModes          _eBodyBaseMode = EBodyBaseModes.Uninitialized;      // The current body base mode: (BodyMorph, CutCloth, Play mode)  Hugely important
	bool					_bIsDisabled;			// Body has been 'disabled' (invisible and not colliding in the scene in any way).  Used when another body is being edited.  (Set by Disable())
	List<ushort>			_aVertsFlexCollider;    // Collection of verts sent from Blender that determine which verts form a morphing-time Flex collider capable of repelling morph-time bodysuit master cloth.
	//---------------------------------------------------------------------------	MAIN PROPERTIES
	public int				_nBodyID;               // The 0-based numeric ID of our body where we're stored in CGame._aBodyBases[]  Extremely important.  Matches Blender's CBodyBase.nBodyID as well
	public EBodySex         _eBodySex;              // The body's sex (man, woman, shemale)
	public string           _sBlenderInstancePath_CBodyBase;                // The Blender fully-qualified instance path where our corresponding CBodyBase is accessed (from Blender global scope)
	public string           _sHumanCharacterName;   // The human first-name given to this character... purely cosmetic
	public string           _sBodyPrefix;           // The 'body prefix' string used to identify Blender & Unity node for this body (Equals to 'BodyA', 'BodyB', 'BodyC', etc)
	public string           _sMeshSource;           // The 'mesh source' in Blender such as "WomanA", "ManA", etc
	public string           _sNameSrcGenitals_OBSOLETE;
	//---------------------------------------------------------------------------	USER INTERFACE
	CObject					_oObj;					// The Blender-implemented Object that exposes RTTI-like information for change Blender shape keys from Unity UI panels
	//---------------------------------------------------------------------------	###TODO14: SORT
	CUICanvas				_oCanvas;               // The fixed UI canvas responsible to render the GUI for this game mode.
	uFlex.FlexParticles     _oFlexParticles;        // The morph-time flex collider responsible to repell master bodysuit cloth so it can morph too.  Composed of a subset of our static body mesh
	bool                    _bFlexBodyCollider_ParticlesUpdated; // When true a user-morph updated the body's vert and we need to update particle positions at the next opportunity. (in PreContainerUpdate())
	public CBMeshFlex       _oClothSrc;				// The source clothing instance.  This 'bodysuit-type' full cloth will be simulated as the user adjusts morphing sliders so as to set the master cloth to closely match the body's changing shape
	public CClothEdit		_oClothEdit;				// The one-and-only cutting (editing) cloth.  Only valid during cloth cutting game mode.

	public GameObject           _oBodyRootGO;           // The root game object of every Unity object for this body.  (Created from prefab)
	public Transform            _oBoneRootT;            // The 'Root' bone node right off of our top-level node with the name of 'Bones' = The body's bone tree
	public Transform            _oGenitalsT;                // The 'Root' pin node right off of our top-level node with the name of 'Base' = The body's pins (controlling key bones through PhysX joints)



	public CBodyBase(int nBodyID, EBodySex eBodySex) {
		_nBodyID = nBodyID;
		_eBodySex = eBodySex;
		_sBodyPrefix = "Body" + _nBodyID.ToString();

		//=== Create default values for important body parameters from the sex ===
		if (_eBodySex == EBodySex.Man) {
			_sHumanCharacterName = (_nBodyID == 0) ? "Karl" : "Brent";          //###IMPROVE: Database of names?  From user???
		} else {
			//_sMeshSource = "WomanA";
			_sHumanCharacterName = (_nBodyID == 0) ? "Emily" : "Eve";
		}

		switch (_eBodySex) {                                 //###CHECK	####TEMP ####DESIGN: Loaded from file or user top-level selection! ####DESIGN: Public properties?
			case EBodySex.Man:
				_sMeshSource = "Man";
				_sNameSrcGenitals_OBSOLETE = "PenisM-EroticVR-A-Big";		//###TODO11: Cleanup?
				break;
			case EBodySex.Woman:
				_sMeshSource = "Woman";
				_sNameSrcGenitals_OBSOLETE = "Vagina-EroticVR-A";                  //###DESIGN??? Crotch and not vagina???
				break;
			case EBodySex.Shemale:
				_sMeshSource = "Shemale";
				_sNameSrcGenitals_OBSOLETE = "PenisW-EroticVR-A-Big";              //###TODO: Comes from GUI!
				break;
		}
		_sMeshSource += "A";				// Eventually meshes will have different versions but for now we only have the "A" version in Blender.


		//=== Instantiate the proper prefab for our body type (Man or Woman), which defines our bones and colliders ===
		GameObject oBodyTemplateGO = Resources.Load("Prefabs/PrefabWomanA", typeof(GameObject)) as GameObject;      //###TODO: Different gender / body types enum that matches Blender	//oBody._sMeshSource + 
		_oBodyRootGO = GameObject.Instantiate(oBodyTemplateGO) as GameObject;
		_oBodyRootGO.name = _sBodyPrefix;
		_oBodyRootGO.SetActive(true);           // Prefab is stored with top object deactivated to ease development... activate it here...

		//=== Obtain references to needed sub-objects of our prefab ===
		_oBoneRootT	= CUtility.FindChild(_oBodyRootGO.transform, "Bones");            // Set key nodes of Bones and Base we'll need quick access to over and over.
		//_oGenitalsT	= new GameObject("Genitals").transform;						// Create root actor / pin Genitals
		//_oGenitalsT.SetParent(_oBodyRootGO.transform);

		//===== CREATE THE BODY PYTHON INSTANCE IN BLENDER =====		###DESIGN21: Reconsider argument passing here... would be better to have a 'mesh version' (e.g. A, B, C) of each sex and separate 'Shemale' to 'Woman'
		CGame.gBL_SendCmd("CBody", "CBodyBase_Create(" + _nBodyID.ToString() + ", '" + eBodySex.ToString() + "', '" + _sMeshSource + "','" + _sNameSrcGenitals_OBSOLETE + "')");       // This new instance is an extension of this Unity CBody instance and contains most instance members
		_sBlenderInstancePath_CBodyBase = "CBodyBase_GetBodyBase(" + _nBodyID.ToString() + ")";                 // Simplify access to Blender CBodyBase instance

		//=== Download our morphing non-skinned body from Blender ===
		GameObject oBodyBaseGO = new GameObject(_sBodyPrefix);        // Create the root Unity node that will keep together all the many subnodes of this body.
		_oMeshStaticCollider = CBMesh.Create(oBodyBaseGO, this, ".oMeshMorphResult", typeof(CBMesh), true);     // Get the baked-morph mesh Blender updates for us at every morph update. (And keep Blender share so we can update)
		_oMeshStaticCollider.transform.SetParent(_oBodyRootGO.transform);
		_oMeshStaticCollider.name = _sBodyPrefix + "-StaticCollider";


		//=== DEFINE THE FLEX COLLIDER: Read the collection of verts that will from the Flex collider (responsible to repell master Bodysuit from morph-time body) ===
		_aVertsFlexCollider = CByteArray.GetArray_USHORT("'CBody'", _sBlenderInstancePath_CBodyBase + ".aVertsFlexCollider.Unity_GetBytes()");

		//=== Create Canvas for GUI for this mode ===		###CHECK19:
		_oCanvas = CUICanvas.Create(_oMeshStaticCollider.transform);
		_oCanvas.transform.position = new Vector3(0.31f, 1.35f, 0.13f);            //###WEAK11: Hardcoded panel placement in code?  Base on a node in a template instead??  ###IMPROVE: Autorotate?

		//=== Create the managing object and related hotspot ===
		_oObj = new CObject(this, "Body Morphing", "Body Morphing");
		_oObj.Event_PropertyValueChanged += Event_PropertyChangedValue;
		CPropGrpBlender oPropGrpBlender = new CPropGrpBlender(_oObj, "Body Morphing", _sBlenderInstancePath_CBodyBase + ".oObjectMeshShapeKeys");
		//###BROKEN21: _oCanvas.CreatePanel("Body Morphing", null, _oObj);			###IMPROVE:21!!!: Find way for GUI to not crash when props are null!!

		//=== Change some morphing channels ===		//###TEMP18:		###BROKEN19:
		//_oObj.PropSet(0, "Breasts-Implants", 1.2f);			//###TODO: Load these from a 'body file'
		//_oObj.PropSet(0, "Breasts-Height", 1.2f);

		//=== Switch to morphing / configure mode ===			//###CHECK: Do we always do here in construction or do we let game tell us?  This will get important as we auto-load bodies for immediate play
		//OnChangeBodyMode(EBodyBaseModes.MorphBody);             // Enter configure mode so we can programmatically apply morphs to customize this body
	}

	void FlexObject_BodyStaticCollider_Enable() {
		//=== Define Flex particles from Blender mesh made for Flex ===
		if (_oFlexParticles == null) { 
			int nParticles = _aVertsFlexCollider.Count;
			_oFlexParticles = CUtility.CreateFlexObjects(_oMeshStaticCollider.gameObject, this, nParticles, uFlex.FlexInteractionType.SelfCollideFiltered, Color.grey);		//###TODO14: Flex Colors!
			for (int nParticle = 0; nParticle < nParticles; nParticle++) {
				//int nVertWholeMesh = _aVertsFlexCollider[nParticle];				// Lookup the vert index in the full mesh from the Blender-supplied array that isolates which verts participate in the Flex collider
				//_oFlexParticles.m_particles[nParticle].pos = _oMeshMorphResult._memVerts.L[nVertWholeMesh];		// Don't position here to insure only one place positions to avoid bugs / confusion. (in PreContainerUpdate()) 
				_oFlexParticles.m_restParticles[nParticle].invMass = _oFlexParticles.m_particles[nParticle].invMass = 0;					// We're a static collider with every particle moved by this code at every morph... (e.g. All particles are not Flex simulated)
				_oFlexParticles.m_colours[nParticle] = _oFlexParticles.m_colour;
				_oFlexParticles.m_particlesActivity[nParticle] = true;
			}
			_bFlexBodyCollider_ParticlesUpdated = true;                          // Flag Flex to perform an update of its particle positions.
		} else { 
			_oFlexParticles.gameObject.SetActive(true);					// Just re-activate if we were de-activated before in FlexObject_Deactivate()
		}
	}

	void FlexObject_BodyStaticCollider_Disable() {
		//=== Destroy all configure-time Flex objects for performance.  They will have to be re-created upon re-entry into configure mode ===
		//GameObject.DestroyImmediate(_oMeshMorphResult.gameObject.GetComponent<uFlex.FlexParticlesRenderer>());      //###INFO: DestroyImmediate is needed to avoid CUDA errors with regular Destroy()!
		//GameObject.DestroyImmediate(_oMeshMorphResult.gameObject.GetComponent<uFlex.FlexProcessor>());
		//GameObject.DestroyImmediate(_oFlexParticles);
		//_oFlexParticles = null;

		_oFlexParticles.gameObject.SetActive(false);			//####DESIGN18: Don't have to clear everything (although this step saves memory).  Keep de-activation instead??
	}

	public void OnChangeBodyMode(EBodyBaseModes eBodyBaseModeNew) {
		if (_eBodyBaseMode == eBodyBaseModeNew)
			return;

		Debug.LogFormat("MODE: Body#{0} going from '{1}' to '{2}'", _nBodyID.ToString(), _eBodyBaseMode.ToString(), eBodyBaseModeNew.ToString());

		if (_eBodyBaseMode == EBodyBaseModes.Uninitialized && eBodyBaseModeNew == EBodyBaseModes.MorphBody) {

			FlexObject_BodyStaticCollider_Enable();							// Create / re-create the Flex objects for this static body collider
			//###DISABLED19: _oClothSrc = CBMeshFlex.CreateForClothSrc(this, "BodySuit");	// Create bodycloth clothing instance.  It will be simulated as the user adjusts morphing sliders so as to set the master cloth to closely match the body's changing shape ===  ###TODO18: Obtain what cloth source from GUI

		} else if (_eBodyBaseMode == EBodyBaseModes.MorphBody && eBodyBaseModeNew == EBodyBaseModes.CutCloth) {

			if (_oClothSrc != null)
				_oClothSrc.FlexObject_ClothSrc_Disable();
			_oCanvas.gameObject.SetActive(false);                               // Hide the entire morphing UI

			//###BROKEN21:!!!!
			////=== Perform special processing when user has done morphing the body to update the morph mesh in Blender. Send Blender all the sliders values so it updates its morphing body for game-time body generation (This because we were morphing locally for much better performance) ===
			//CPropGrpBlender oPropGrpBlender = _oObj._aPropGrps[0] as CPropGrpBlender;
			//foreach (CProp oProp in oPropGrpBlender._aProps)			// Manually dump all our Blender properties into Blender so UpdateMorphResultMesh() has the user's morph sliders.  (We don't update at every slider value change for performance)
			//	oProp._nValueLocal = float.Parse(CGame.gBL_SendCmd("CBody", oPropGrpBlender._sBlenderAccessString + ".PropSetString('" + oProp._sNameProp + "'," + oProp._nValueLocal.ToString() + ")"));
			////=== Ask Blender to update its morphing body from user-selected slider choices ===
			//CGame.gBL_SendCmd("CBody", _sBlenderInstancePath_CBodyBase + ".UpdateMorphResultMesh()");
			////###BROKEN18:!!!: Can no longer re-enter cloth cutting as gBlender loses access?? _oMeshStaticCollider.UpdateVertsFromBlenderMesh(true);         // Morphing can radically change normals.  Recompute them. (Accurate normals needed below anyways for Flex collider)

			//###TEMP19:!!!! _oClothEdit = new CClothEdit(this, "MyShirt", "Shirt", "BodySuit", "_ClothSkinnedArea_ShoulderTop");    //###HACK18:!!!: Choose what cloth to edit from GUI choice  ###DESIGN!!!

		} else if (_eBodyBaseMode == EBodyBaseModes.CutCloth && eBodyBaseModeNew == EBodyBaseModes.Play) {
			FlexObject_BodyStaticCollider_Disable();		// Going from CutCloth to play mode.  Destroy all configure-time Flex objects for performance.  They will have to be re-created upon re-entry into configure mode ===

			_oBody = new CBody(this);						// Create the runtime body.  Expensive op!

			if (_oClothEdit != null)
				_oClothEdit.GameMode_EnterMode_Play();

		} else if (_eBodyBaseMode == EBodyBaseModes.Play && eBodyBaseModeNew == EBodyBaseModes.CutCloth) {

			_oBody = _oBody.DoDestroy();					// Destroys *everthing* related to gametime (softbodies, cloth, flex colliders, etc) on both Unity and Blender side!
			//_oMeshMorphResult.GetComponent<MeshRenderer>().enabled = true;     // Show the morphing mesh
			FlexObject_BodyStaticCollider_Enable();             // Make body base / morphing body visible and active again.
			if (_oClothEdit != null)
				_oClothEdit.GameMode_EnterMode_EditCloth();

		} else if (_eBodyBaseMode == EBodyBaseModes.CutCloth && eBodyBaseModeNew == EBodyBaseModes.MorphBody) {

			if (_oClothEdit != null)
				_oClothEdit = _oClothEdit.DoDestroy();
			if (_oClothSrc != null)
				_oClothSrc.FlexObject_ClothSrc_Enable();
			_oCanvas.gameObject.SetActive(true);                               // Show the entire morphing UI

		} else {
			CUtility.ThrowExceptionF("Exception in CBodyBase.OnChangeBodyMode(): Cannot change from body mode '{0}' to '{1}'.", _eBodyBaseMode.ToString(), eBodyBaseModeNew.ToString());
		}

		//=== Set the new game mode ===
		_eBodyBaseMode = eBodyBaseModeNew;
	}

	public void Disable() {
		//###BROKEN22: CUtility.ThrowException("TODO: Implement disable!");			//###TODO18:
		_bIsDisabled = true;
	}

	public void OnSimulatePre() {			//###OBS14:??
		if (_oBody != null && _oBody._bEnabled)
			_oBody.OnSimulatePre();
	}
	public void HideShowMeshes() {
		//###DESIGN18: Hide / show all options given the many game modes too complex... ditch?
		//_oMeshMorphResult.GetComponent<MeshRenderer>().enabled = CGame.INSTANCE.ShowPresentation && (_eBodyBaseMode != EBodyBaseModes.Play);
		//_oClothEdit.GetComponent<MeshRenderer>().enabled = CGame.INSTANCE.ShowPresentation && (_eBodyBaseMode == EBodyBaseModes.Play);
		//_oClothSrc.GetComponent<MeshRenderer>().enabled = CGame.INSTANCE.ShowPresentation;
		if (_oMeshStaticCollider.GetComponent<uFlex.FlexParticlesRenderer>() != null)
			_oMeshStaticCollider.GetComponent<uFlex.FlexParticlesRenderer>().enabled = CGame.INSTANCE.ShowFlexParticles;
		if (_oClothSrc.GetComponent<uFlex.FlexParticlesRenderer>() != null)
			_oClothSrc.GetComponent<uFlex.FlexParticlesRenderer>().enabled = CGame.INSTANCE.ShowFlexParticles;
		if (_oBody != null && _oBody._bEnabled) 	//###CHECK?
			_oBody.HideShowMeshes();
	}
	public Transform FindBone(string sBonePath) {
		Transform oBoneT = _oBoneRootT.Find(sBonePath);
		if (oBoneT == null)
			CUtility.ThrowExceptionF("ERROR: CBody.FindBone() cannot find bone '{0}'", sBonePath);
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

	void Event_PropertyChangedValue(object sender, EventArgs_PropertyValueChanged oArgs) {      // Fired everytime user adjusts a property.
		// 'Bake' the morphing mesh as per the player's morphing parameters into a 'MorphResult' mesh that can be serialized to Unity.  Matches Blender's CBodyBase.UpdateMorphResultMesh()
		Debug.LogFormat("CBodyBase updates body base '{0}' because property '{1}' of group '{2}' changed to value '{3}'", _sBodyPrefix, oArgs.PropertyName, oArgs.PropertyGroup._sNamePropGrp, oArgs.ValueNew);

		//=== Process morph channel property changes ===
		if (oArgs.PropertyGroup.GetType() == typeof(CPropGrpBlender)) { 
			if (oArgs.Property._oObjectExtraFunctionality == null)				// Create 'Extra functionality object' for this property if this is the first time it is invoked
				oArgs.Property._oObjectExtraFunctionality = new CMorphChannel(this, oArgs.PropertyName);
			CMorphChannel oMorphChannel = oArgs.Property._oObjectExtraFunctionality as CMorphChannel;		// Extract the 'extra functionality object' from this property:  It is a cache for morph channel to (greatly) speed up in-Unity morphing
			bool bMeshChanged = oMorphChannel.ApplyMorph(oArgs.ValueNew);
			if (bMeshChanged) {
				_oMeshStaticCollider._oMeshNow.vertices = _oMeshStaticCollider._memVerts.L;       //###IMPROVE: to some helper function?
				_oMeshStaticCollider.UpdateNormals();							// Morphing invalidates normals... update
				_bFlexBodyCollider_ParticlesUpdated = true;                          // Flag Flex to perform an update of its particle positions...
			}
		}
	}

	public void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
		//=== Update the position of our flex particles so bodysuit can be repelled by the updated morph vert positions ===
		if (_bFlexBodyCollider_ParticlesUpdated) {
			float nDistFlexColliderShrink = CGame.INSTANCE.particleSpacing * CGame.INSTANCE.nDistFlexColliderShrinkMult;
			for (int nParticle = 0; nParticle < _oFlexParticles.m_particlesCount; nParticle++) {
				int nVertWholeMesh = _aVertsFlexCollider[nParticle];                // Lookup the vert index in the full mesh from the Blender-supplied array that isolates which verts participate in the Flex collider
				Vector3 vecVert = _oMeshStaticCollider._memVerts.L[nVertWholeMesh];
				vecVert -=  _oMeshStaticCollider._memNormals.L[nVertWholeMesh] * nDistFlexColliderShrink;		// 'Shrink' each particle a fraction of the way inverse to its normal so it 'shrinks' inside the body to account for particle-to-particle distance and make it appear that cloth can be right over body skin.
				_oFlexParticles.m_particles[nParticle].pos = vecVert;
			}
			_bFlexBodyCollider_ParticlesUpdated = false;
		}
	}
}


public enum EBodyBaseModes {
	Uninitialized,					// CBodyBase just got created at the start of the game and has not yet been initialized to any meaningful mode.  (Can't go back to this mode)
	MorphBody,                      // Body is now being morphed by the user via sliders to Blender (along with ClothSrc / Bodysuit).  Game-ready _oBody is null / destroyed.
	CutCloth,						// Cloth is being cut by removing parts from the ClothSrc / Bodysuit.
	Play,							// Body is simulating in gameplay mode.  (_oBody is valid and has created softbodies / cloths / etc)
}

public enum EBodySex {
	Man,
	Woman,
	Shemale
};

//###TEST: Unit-test for multi-object property viewer
//CPropGrpEnum oPropGrpEnum = new CPropGrpEnum(_oObj, "Test Enum 1-1", typeof(EPenisTip));		
//oPropGrpEnum.PropAdd(EPenisTip.CycleTime,   "Cycle Time",         1.0f, 0.0001f, 1000.0f, "");
//oPropGrpEnum.PropAdd(EPenisTip.MaxVelocity,   "MaxVelocity",         1.0f, 0.0001f, 1000.0f, "");

//CPropGrp oPropGrpMisc = new CPropGrp(_oObj, "Test Misc 1-3");
//oPropGrpMisc.PropAdd(0, "1stProp", "First Prop",         1.0f, 0.0001f, 1000.0f, "");
//oPropGrpMisc.PropAdd(1, "2ndProp", "Second Prop",         1.0f, 0.0001f, 1000.0f, "");
//oPropGrpMisc.PropAdd(2, "3rdProp", "Third Prop",         1.0f, 0.0001f, 1000.0f, "");

//CObject oObj2 = new CObject(this, "Test-Object2", "Test-Object2-Label");;
//CPropGrp oPropGrpMisc2 = new CPropGrp(oObj2, "Test Enum 2-1");
//oPropGrpMisc2.PropAdd(0, "1stProp2", "First Prop2",         1.0f, 0.0001f, 1000.0f, "");
//oPropGrpMisc2.PropAdd(1, "2ndProp2", "Second Prop2",         1.0f, 0.0001f, 1000.0f, "");
//oPropGrpMisc2.PropAdd(2, "3rdProp2", "Third Prop2",         1.0f, 0.0001f, 1000.0f, "");
