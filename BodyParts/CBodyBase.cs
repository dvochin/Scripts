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

public class CBodyBase : IObject, IFlexProcessor {
	//---------------------------------------------------------------------------	MAIN MEMBERS
	public CBody			_oBody;					// Our most important member: The gametime body object responsible for gameplay functionality (host for softbodies, dynamic clothes, etc)  Created / destroyed as we enter/leave morph or gameplay mode
	public CBMesh           _oMeshMorphResult;		// The non-skinned body morph-baked mesh we're morphing.  Constantly refreshed from equivalent body in Blender as user moves morphing sliders.
	EBodyBaseModes          _eBodyBaseMode = EBodyBaseModes.Startup;      // The current body base mode: (configure, gameplay mode, disabled mode)  Hugely important
	List<ushort>			_aVertsFlexCollider;    // Collection of verts sent from Blender that determine which verts form a morphing-time Flex collider capable of repelling morph-time bodysuit master cloth.
	//---------------------------------------------------------------------------	MAIN PROPERTIES
	public int				_nBodyID;               // The 0-based numeric ID of our body where we're stored in CGame._aBodyBases[]  Extremely important.  Matches Blender's CBodyBase.nBodyID as well
	public EBodySex         _eBodySex;              // The body's sex (man, woman, shemale)
	public string           _sBlenderInstancePath_CBodyBase;                // The Blender fully-qualified instance path where our corresponding CBodyBase is accessed (from Blender global scope)
	public string           _sHumanCharacterName;   // The human first-name given to this character... purely cosmetic
	public string           _sBodyPrefix;           // The 'body prefix' string used to identify Blender & Unity node for this body (Equals to 'BodyA', 'BodyB', 'BodyC', etc)
	public string           _sMeshSource;           //###KEEP<11>
	public string           _sNameSrcGenitals;
	//---------------------------------------------------------------------------	USER INTERFACE
	CObjectBlender          _oObj;					// The Blender-implemented Object that exposes RTTI-like information for change Blender shape keys from Unity UI panels
    //CHotSpot				_oHotSpot;              // The hotspot object that will permit user to left/right click on us in the scene to move/rotate/scale us and invoke our context-sensitive menu.

	//---------------------------------------------------------------------------	###TODO<14> SORT
	CUICanvas				_oCanvas;               // The fixed UI canvas responsible to render the GUI for this game mode.
	uFlex.FlexParticles     _oFlexParticles;        // The morph-time flex collider responsible to repell master bodysuit cloth so it can morph too.  Composed of a subset of our static body mesh
	bool                    _bParticlePositionsUpdated; // When true a user-morph updated the body's vert and we need to update particle positions at the next opportunity. (in PreContainerUpdate())
	CBClothSrc              _oClothSrc;				// The source clothing instance.  This 'bodysuit-type' full cloth will be simulated as the user adjusts morphing sliders so as to set the master cloth to closely match the body's changing shape

	public GameObject           _oBodyRootGO;           // The root game object of every Unity object for this body.  (Created from prefab)
	public Transform            _oBonesT;               // The 'Root' bone node right off of our top-level node with the name of 'Bones' = The body's bone tree
	public Transform            _oBaseT;                // The 'Root' pin node right off of our top-level node with the name of 'Base' = The body's pins (controlling key bones through PhysX joints)

	public CBodyBase(int nBodyID, EBodySex eBodySex) {
		_nBodyID = nBodyID;
		_eBodySex = eBodySex;
		_sBodyPrefix = "Body" + _nBodyID.ToString();

		//=== Create default values for important body parameters from the sex ===
		if (_eBodySex == EBodySex.Man) {
			_sMeshSource = "ManA";
			_sHumanCharacterName = (_nBodyID == 0) ? "Karl" : "Brent";          //###IMPROVE: Database of names?  From user???
		} else {
			_sMeshSource = "WomanA";
			_sHumanCharacterName = (_nBodyID == 0) ? "Emily" : "Eve";
		}

		switch (_eBodySex) {                                 //###CHECK	####TEMP ####DESIGN: Loaded from file or user top-level selection! ####DESIGN: Public properties?
			case EBodySex.Man:
				_sNameSrcGenitals = "PenisM-EroticVR-A-Big";		//###TODO<11>: Cleanup?
				break;
			case EBodySex.Woman:
				_sNameSrcGenitals = "Vagina-EroticVR-A";                  //###DESIGN??? Crotch and not vagina???
				break;
			case EBodySex.Shemale:
				_sNameSrcGenitals = "PenisW-EroticVR-A-Big";              //###TODO: Comes from GUI!
				break;
		}


		//=== Instantiate the proper prefab for our body type (Man or Woman), which defines our bones and colliders ===
		GameObject oBodyTemplateGO = Resources.Load("Prefabs/Prefab" + _sMeshSource, typeof(GameObject)) as GameObject;      //###TODO: Different gender / body types enum that matches Blender	//oBody._sMeshSource + 
		_oBodyRootGO = GameObject.Instantiate(oBodyTemplateGO) as GameObject;
		_oBodyRootGO.name = _sBodyPrefix;
		_oBodyRootGO.SetActive(true);           // Prefab is stored with top object deactivated to ease development... activate it here...

		//=== Obtain references to needed sub-objects of our prefab ===
		_oBonesT    = CUtility.FindChild(_oBodyRootGO.transform, "Bones");            // Set key nodes of Bones and Base we'll need quick access to over and over.
		_oBaseT     = CUtility.FindChild(_oBodyRootGO.transform, "Base");


		//===== CREATE THE BODY PYTHON INSTANCE IN BLENDER =====  
		CGame.gBL_SendCmd("CBody", "CBodyBase_Create(" + _nBodyID.ToString() + ", '" + _sMeshSource + "', '" + eBodySex.ToString() + "','" + _sNameSrcGenitals + "')");       // This new instance is an extension of this Unity CBody instance and contains most instance members
		_sBlenderInstancePath_CBodyBase = "CBodyBase_GetBodyBase(" + _nBodyID.ToString() + ")";                 // Simplify access to Blender CBodyBase instance

		//=== Download our morphing non-skinned body from Blender ===
		GameObject oBodyBaseGO = new GameObject(_sBodyPrefix);        // Create the root Unity node that will keep together all the many subnodes of this body.
		_oMeshMorphResult = CBMesh.Create(oBodyBaseGO, this, ".oMeshMorphResult", typeof(CBMesh), true);     // Get the baked-morph mesh Blender updates for us at every morph update. (And keep Blender share so we can update)
		_oMeshMorphResult.transform.SetParent(_oBodyRootGO.transform);
		_oMeshMorphResult.name = "MorphingBody";


		//=== DEFINE THE FLEX COLLIDER: Read the collection of verts that will from the Flex collider (responsible to repell master Bodysuit from morph-time body) ===
		_aVertsFlexCollider = CByteArray.GetArray_USHORT("'CBody'", _sBlenderInstancePath_CBodyBase + ".aVertsFlexCollider.Unity_GetBytes()");
		FlexObjects_Create();			// Create the Flex objects the first time.They will be destroyed upon going to gametime


		//=== Create the managing object and related hotspot ===
		_oObj = new CObjectBlender(this, _sBlenderInstancePath_CBodyBase + ".oObjectMeshShapeKeys", _nBodyID);
		_oObj.Event_PropertyValueChanged += Event_PropertyChangedValue;
		//_oHotSpot = CHotSpot.CreateHotspot(this, null, "Body Morphing", false, new Vector3(0, 0, 0));
		_oCanvas = CUICanvas.Create(_oMeshMorphResult.transform);
		_oCanvas.transform.position = new Vector3(0.31f, 1.35f, 0.13f);            //###WEAK<11>: Hardcoded panel placement in code?  Base on a node in a template instead??  ###IMPROVE: Autorotate?
		CUtility.WndPopup_Create(_oCanvas, EWndPopupType.PropertyEditor, new CObject[] { _oObj }, "Body Morphing");

		//=== Switch to morphing / configure mode ===
		OnChangeBodyMode(EBodyBaseModes.MorphBody);             // Enter configure mode so we can programmatically apply morphs to customize this body

		//=== Change some morphing channels ===		//###TODO<11>: Programmatic load & adjustments of sliders from file?
		_oObj.PropSet("Breasts-Implants", 1.2f);
		_oObj.PropSet("Breasts-Height", 1.2f);
	}

	void FlexObjects_Create() {
		//=== Define Flex particles from Blender mesh made for Flex ===
		int nParticles = _aVertsFlexCollider.Count;
		_oFlexParticles = CUtility.CreateFlexObjects(_oMeshMorphResult.gameObject, this, nParticles, uFlex.FlexInteractionType.SelfCollideFiltered, Color.green);		//###TODO<14>: Flex Colors!
		for (int nParticle = 0; nParticle < nParticles; nParticle++) {
			//int nVertWholeMesh = _aVertsFlexCollider[nParticle];				// Lookup the vert index in the full mesh from the Blender-supplied array that isolates which verts participate in the Flex collider
			//_oFlexParticles.m_particles[nParticle].pos = _oMeshMorphResult._memVerts.L[nVertWholeMesh];		// Don't position here to insure only one place positions to avoid bugs / confusion. (in PreContainerUpdate()) 
			_oFlexParticles.m_particles[nParticle].invMass = 0;					// We're a static collider with every particle moved by this code at every morph... (e.g. All particles are not Flex simulated)
			_oFlexParticles.m_colours[nParticle] = _oFlexParticles.m_colour;
			_oFlexParticles.m_particlesActivity[nParticle] = true;
		}
		_bParticlePositionsUpdated = true;                          // Flag Flex to perform an update of its particle positions.
	}

	void FlexObjects_Destroy() {
		//=== Destroy all configure-time Flex objects for performance.  They will have to be re-created upon re-entry into configure mode ===
		GameObject.DestroyImmediate(_oMeshMorphResult.gameObject.GetComponent<uFlex.FlexParticlesRenderer>());      //###LEARN: DestroyImmediate is needed to avoid CUDA errors with regular Destroy()!
		GameObject.DestroyImmediate(_oMeshMorphResult.gameObject.GetComponent<uFlex.FlexProcessor>());
		GameObject.DestroyImmediate(_oFlexParticles);
		_oFlexParticles = null;
	}

	public void OnChangeBodyMode(EBodyBaseModes eBodyBaseMode) {
		if (_eBodyBaseMode == eBodyBaseMode)
			return;

		Debug.LogFormat("MODE: Body#{0} going from '{1}' to '{2}'", _nBodyID.ToString(), _eBodyBaseMode.ToString(), eBodyBaseMode.ToString());

		//=== Perform special processing when going from configure to play to update the morph mesh in Blender ===
		if (_eBodyBaseMode == EBodyBaseModes.MorphBody /*&& eBodyBaseMode == EBodyBaseModes.Play*/) {           
			//=== Going from configure to play mode.  Destroy all configure-time Flex objects for performance.  They will have to be re-created upon re-entry into configure mode ===
			FlexObjects_Destroy();
			//=== Send Blender all the sliders values so it updates its morphing body for game-time body generation ===
			foreach (CProp oProp in _oObj._aProps)			// Manually dump all our Blender properties into Blender so UpdateMorphResultMesh() has the user's morph sliders.  (We don't update at every slider value change for performance)
				oProp._nValueLocal = float.Parse(CGame.gBL_SendCmd("CBody", _oObj._sBlenderAccessString + ".PropSetString('" + oProp._sNameProp + "'," + oProp._nValueLocal.ToString() + ")"));
			//=== Ask Blender to update its morphing body from user-selected slider choices ===
			CGame.gBL_SendCmd("CBody", _sBlenderInstancePath_CBodyBase + ".UpdateMorphResultMesh()");
			_oMeshMorphResult.UpdateVertsFromBlenderMesh(true);         // Morphing can radically change normals.  Recompute them. (Accurate normals needed below anyways for Flex collider)
		}

		//=== Set the new game mode ===
		_eBodyBaseMode = eBodyBaseMode;

		//=== Notify Blender of the switch of body mode.  Makes a lot of things happen that lines below depend on! ===
		CGame.gBL_SendCmd("CBody", _sBlenderInstancePath_CBodyBase + ".OnChangeBodyMode('" + _eBodyBaseMode.ToString() + "')");

		//=== Hide or show the body base components depending on new body mode ===
		bool bShowBodyBase = (_eBodyBaseMode == EBodyBaseModes.MorphBody);
		_oMeshMorphResult.GetComponent<MeshRenderer>().enabled = bShowBodyBase;     // Hide / show the morphing mesh
		_oCanvas.gameObject.SetActive(bShowBodyBase);                               // Hide / show the entire morphing UI

		//=== Switch body mode ===
		//###IMPROVE<17>: Enforce from-to transitions?  (Not all from-to permutations make sense!)
		switch (_eBodyBaseMode) {
			case EBodyBaseModes.Startup:
				CUtility.ThrowException("CBodyBase.OnChangeBodyMode() cannot go back to Startup mode!");
				break;
			case EBodyBaseModes.MorphBody:
				if (_oClothSrc == null)
					_oClothSrc = CBClothSrc.Create(this);   // Create bodycloth clothing instance.  It will be simulated as the user adjusts morphing sliders so as to set the master cloth to closely match the body's changing shape ===
				else
					_oClothSrc.FlexObject_Create();
				FlexObjects_Create();                       // Re-create the Flex objects
				if (_oBody != null)
					_oBody = _oBody.DoDestroy();            // Destroys *everthing* related to gametime (softbodies, cloth, flex colliders, etc) on both Unity and Blender side!
				break;
			case EBodyBaseModes.CutCloth:
				if (_oClothSrc)
					_oClothSrc.FlexObject_Destroy();
				FlexObjects_Create();                       // Re-create the Flex objects
				if (_oBody != null)
					_oBody = _oBody.DoDestroy();            // Destroys *everthing* related to gametime (softbodies, cloth, flex colliders, etc) on both Unity and Blender side!
				break;
			case EBodyBaseModes.Play:
				if (_oBody == null)							// If entering play mode (while having null body), create everything game-play related on Unity & Blender side (huge operation)
					_oBody = new CBody(this);
				else
					_oBody.DoEnable(true);
				break;
			case EBodyBaseModes.Disabled:
				if (_oBody == null)
					CUtility.ThrowException("CBodyBase.OnChangeBodyMode() went to disabled mode with no _oBody!");
				_oBody.DoEnable(false);
				break;
		}
	}

	public void OnSimulatePre() {			//###OBS<14>??
		if (_oBody != null && _oBody._bEnabled)
			_oBody.OnSimulatePre();
	}
	public void OnSimulatePost() {
		if (_oBody != null && _oBody._bEnabled)
			_oBody.OnSimulatePost();
	}
	public void HideShowMeshes() {
		_oMeshMorphResult.GetComponent<MeshRenderer>().enabled = CGame.INSTANCE.ShowPresentation && (_eBodyBaseMode == EBodyBaseModes.MorphBody);
		_oClothSrc.GetComponent<MeshRenderer>().enabled = CGame.INSTANCE.ShowPresentation;
		if (_oMeshMorphResult.GetComponent<uFlex.FlexParticlesRenderer>() != null)
			_oMeshMorphResult.GetComponent<uFlex.FlexParticlesRenderer>().enabled = CGame.INSTANCE.ShowFlexParticles;
		if (_oClothSrc.GetComponent<uFlex.FlexParticlesRenderer>() != null)
			_oClothSrc.GetComponent<uFlex.FlexParticlesRenderer>().enabled = CGame.INSTANCE.ShowFlexParticles;
		if (_oBody != null && _oBody._bEnabled) 	//###CHECK?
			_oBody.HideShowMeshes();
	}
	public Transform FindBone(string sBonePath) {
		Transform oBoneT = _oBonesT.FindChild(sBonePath);
		if (oBoneT == null)
			CUtility.ThrowException(string.Format("ERROR: CBody.FindBone() cannot find bone '{0}'", sBonePath));
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
		Debug.LogFormat("CBodyBase updates body '{0}' because property '{1}' changed to value {2}", _sBodyPrefix, oArgs.PropertyName, oArgs.ValueNew);

		if (oArgs.Property._oObjectExtraFunctionality == null)
			oArgs.Property._oObjectExtraFunctionality = new CMorphChannel(this, oArgs.PropertyName);
		CMorphChannel oMorphChannel = oArgs.Property._oObjectExtraFunctionality as CMorphChannel;
		bool bMeshChanged = oMorphChannel.ApplyMorph(oArgs.ValueNew);
		if (bMeshChanged) {
			_oMeshMorphResult._oMeshNow.vertices = _oMeshMorphResult._memVerts.L;       //###IMPROVE: to some helper function?
			_oMeshMorphResult.UpdateNormals();							// Morphing invalidates normals... update
			_bParticlePositionsUpdated = true;                          // Flag Flex to perform an update of its particle positions...
		}
	}

	public void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
		//=== Update the position of our flex particles so bodysuit can be repelled by the updated morph vert positions ===
		if (_bParticlePositionsUpdated) {
			float nDistFlexColliderShrink = CGame.INSTANCE.particleSpacing * CGame.INSTANCE.nDistFlexColliderShrinkMult;
			for (int nParticle = 0; nParticle < _oFlexParticles.m_particlesCount; nParticle++) {
				int nVertWholeMesh = _aVertsFlexCollider[nParticle];                // Lookup the vert index in the full mesh from the Blender-supplied array that isolates which verts participate in the Flex collider
				Vector3 vecVert = _oMeshMorphResult._memVerts.L[nVertWholeMesh];
				vecVert -=  _oMeshMorphResult._memNormals.L[nVertWholeMesh] * nDistFlexColliderShrink;		// 'Shrink' each particle a fraction of the way inverse to its normal so it 'shrinks' inside the body to account for particle-to-particle distance and make it appear that cloth can be right over body skin.
				_oFlexParticles.m_particles[nParticle].pos = vecVert;
			}
			_bParticlePositionsUpdated = false;
		}
	}
}

public enum EBodyBaseModes {
	Startup,						// CBodyBase just got created at the start of the game and has not yet been initialized to any meaningful mode.  (Can't go back to this mode)
	MorphBody,                      // Body is now being morphed by the user via sliders to Blender (along with ClothSrc / Bodysuit).  Game-ready _oBody is null / destroyed.
	CutCloth,						// Cloth is being cut by removing parts from the ClothSrc / Bodysuit.
	Play,							// Body is simulating in gameplay mode.  (_oBody is valid and has created softbodies / cloths / etc)
	Disabled,						// Body is 'disabled'.  Used to hide / disable one game-ready body so the configure-mode body is the only one visible.  (_oBody exists but all meshes are invisible and doesn't receive any cycles)
}

public enum EBodySex {
	Man,
	Woman,
	Shemale
};
