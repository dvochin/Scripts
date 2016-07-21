///*###DISCUSSION: Cloth

//- Blender no longer splits clothing between skinned and simulated area.  Entire mesh is simulated althought the (formerly skinned) portion now creates additional particles that have springs to the skinned-portion particles.
//    - Verts that are part of the 'pinning area' are simply marked and added to a set.
//    - Pinning area verts are copied from the cloth mesh (which maintains all its verts) to form the skinning mesh as usual
//    - When Unity receives the skinning mesh mapped to the (full) clothing mesh, it creates 'pinning particles' of infinite weight that are moved as per their skinning position.
//        - Each pinning particle has a corresponding 'slave particle' that follows it along through a tightly-defined spring of zero lenght.



//=== NEXT ===
//- Normals
//- Missing vert
//- Redo param increasing during large movements??
//- Continue with softbody two modes?  Simplify to one mode and have some kind of reset / stiffening during sharp movements?

//=== TODO ===

//=== LATER ===

//=== IMPROVE ===

//=== DESIGN ===

//=== IDEAS ===
//- Play with mass (e.g. during large body movement)
//- Skinned part to cover back?
//+++- Have entire skinned part be a Flex mesh with the 'skinned verts' set to infinite mass... that way we can undress!

//=== LEARNED ===

//=== PROBLEMS ===
//- Cloth cutting can be inverted... how do we make certain what side we keep??

//=== PROBLEMS??? ===

//=== WISHLIST ===

//*/

//using UnityEngine;
//using System;
//using System.Collections.Generic;

//public class CBCloth_OldFlexSplit : CBMesh, IObject, IHotSpotMgr, IFlexProcessor {						// CBCloth_OldFlexSplit: Blender-based mesh that is cloth-simulated by our PhysX code.
//	[HideInInspector]	public 	CObject				_oObj;					// The multi-purpose CObject that stores CProp properties  to publicly define our object.  Provides client/server, GUI and scripting access to each of our 'super public' properties.

//						public	float				_nClothInitStretch = 0.9f;	//####REVA ####IMPROVE: Export!			// Amount cloth is stretched (<1) at startup to pre-stretch all cloth triangles to more closely simulate the stretched look that 3dsMax delivered
//						public	float				_nClothInitStretchFirstFrame = 1.0f;	//####REVA			// Amount cloth is stretched (<1) at startup to pre-stretch all cloth triangles to more closely simulate the stretched look that 3dsMax delivered

//    List<ushort>        _aMapClothVertsSimToSkin;
//    //[HideInInspector]	public 	CMemAlloc<ushort>	_memMapClothVertsSimToSkin = new CMemAlloc<ushort>();	// Map of which periphery vert on simulated half of cloth maps to which periphery vert on skinned half of this cloth.  (Pins the simulated cloth to the body)

//	[HideInInspector]	public 	CBMesh				_oBMeshClothAtStartup;			// The 'startup cloth' that won't get simulated.  It is used to reset the simulated cloth to its start position
//	[HideInInspector]	public 	CBSkin				_oBSkin_SkinnedHalf;			// The 'skinned half' of our cloth.  Animates just like any skinned mesh to its body.  We use some of its periphery verts to pin our simulated cloth.  Baked every frame
//	[HideInInspector]	public 	Mesh				_oMeshClothSkinnedBaked;		// The 'baked' version of the skinned-part of our cloth.  We bake at every frame and send the position of the boundary verts to PhysX to 'pin' the simulated part of this cloth
//	[HideInInspector]	public 	SkinnedMeshRenderer	_oSkinMeshRendNow;

//	[HideInInspector]	public 	CBBodyColCloth		_oBBodyColCloth;				//###DESIGN? Belongs here??		// Our very important custom partial-body collider for this cloth: repels cloth our skinned body in just the clothing area
//	                    Transform                   _oWatchBone;				// Bone position we watch from our owning body to 'autotune' cloth simulation parameters so cloth stays on body during rapid movement (e.g. pose changing)
////                        Vector3                     _vecWatchBonePosLast, _vecWatchBonePosNow;       // Last position of watched bone above.  Used to determine speed of bone to autotune cloth simulation parameters


//	CHotSpot			_oHotSpot;                          // The hotspot object that will permit user to left/right click on us in the scene to move/rotate/scale us and invoke our context-sensitive menu.

//	public string       _sBlenderInstancePath_CCloth;				// Blender access string to our instance (form our CBody instance)
//	static string       s_sNameClothSrc_HACK;                   //####HACK!: To pass in name from static creation!

//    uFlex.FlexParticles _oSoftFlexParticles;
//    int _nResetClothFrames_HACK;

//    public static CBCloth_OldFlexSplit Create(CBody oBody, string sNameCloth, string sClothType, string sNameClothSrc, string sVertGrp_ClothSkinArea) {    // Static function override from CBMesh::Create() to route Blender request to BodyCol module and deserialize its additional information for the local creation of a CBBodyColCloth
//        string sBodyID = "CBody_GetBody(" + oBody._nBodyID.ToString() + ").";
//        CBCloth_OldFlexSplit.s_sNameClothSrc_HACK = "aCloths['" + sNameCloth + "']";
//        CGame.gBL_SendCmd("CBody", sBodyID + "CreateCloth('" + sNameCloth + "', '" + sClothType + "', '" + sNameClothSrc + "', '" + sVertGrp_ClothSkinArea + "')");      // Create the Blender-side CCloth entity to service our requests
//        CGame.gBL_SendCmd("CBody", sBodyID + CBCloth_OldFlexSplit.s_sNameClothSrc_HACK + ".UpdateCutterCurves()");
//        CGame.gBL_SendCmd("CBody", sBodyID + CBCloth_OldFlexSplit.s_sNameClothSrc_HACK + ".CutClothWithCutterCurves()");
//        CGame.gBL_SendCmd("CBody", sBodyID + CBCloth_OldFlexSplit.s_sNameClothSrc_HACK + ".PrepareClothForGame()");
//        CBCloth_OldFlexSplit oBCloth = (CBCloth_OldFlexSplit)CBMesh.Create(null, oBody, CBCloth_OldFlexSplit.s_sNameClothSrc_HACK + ".oMeshClothSimulated", typeof(CBCloth_OldFlexSplit));		// Obtain the simulated-part of the cloth that was created in call above
//		//####IDEA: Modify static creation by first creating instance, stuffing it with custom data and feeding instance in Create to be filled in!
//		return oBCloth;
//	}

//	public override void OnDeserializeFromBlender() {
//		base.OnDeserializeFromBlender();

//		_sBlenderInstancePath_CCloth = CBCloth_OldFlexSplit.s_sNameClothSrc_HACK;

//		//=== Create the skinned-half of our simulated cloth.  It will be responsible for pinning us to the body ===
//		_oBSkin_SkinnedHalf = (CBSkin)CBSkin.Create(null, _oBody, _sBlenderInstancePath_CCloth + ".oMeshClothSkinned", typeof(CBSkin));
//		_oSkinMeshRendNow = _oBSkin_SkinnedHalf.GetComponent<SkinnedMeshRenderer>();            // Obtain reference to skinned mesh renderer as it is this object that can 'bake' a skinned mesh.
//		_oMeshClothSkinnedBaked = new Mesh();

//		//=== Receive the _memMapClothVertsSimToSkin Blender created to map sim verts to skinned verts ===
//		CUtility.BlenderSerialize_GetSerializableCollection("'CBody'", _oBody._sBlenderInstancePath_CBody + "." + _sBlenderInstancePath_CCloth + ".SerializeCollection_aMapClothVertsSimToSkin()", out _aMapClothVertsSimToSkin);
//		//_memMapClothVertsSimToSkin.AllocateFromList(aMapClothVertsSimToSkin);

//		//=== Create the simulated part of the cloth ===
//		MeshFilter oMeshFilter = GetComponent<MeshFilter>();
//		MeshRenderer oMeshRend = GetComponent<MeshRenderer>();
//		oMeshRend.sharedMaterial = Resources.Load("Materials/BasicColors/TransWhite25") as Material;        //####SOON? Get mats!
//		_oSkinMeshRendNow.sharedMaterial = oMeshRend.sharedMaterial;        // Skinned part has same material
//		_oMeshNow = oMeshFilter.sharedMesh;
//		_oMeshNow.MarkDynamic();                // Docs say "Call this before assigning vertices to get better performance when continually updating mesh"

//		//=== Create the 'cloth at startup' mesh.  It won't get simulated and is used to reset simulated cloth to its startup position ===
//		_oBMeshClothAtStartup = CBMesh.Create(null, _oBody, _sBlenderInstancePath_CCloth + ".oMeshClothSimulated", typeof(CBMesh));
//		_oBMeshClothAtStartup.transform.SetParent(_oBody.FindBone("chest").transform);      // Reparent this 'backup' mesh to the chest bone so it rotates and moves with the body
//		_oBMeshClothAtStartup.gameObject.SetActive(false);      // De activate it so it takes no cycle.  It merely exists for backup purposes

//        //=== Create the Flex object for our simulated part ===
//        CFlex.CreateFlexObject(gameObject, _oMeshNow, uFlex.FlexBodyType.Cloth, uFlex.FlexInteractionType.None, CGame.INSTANCE.nMassCloth, 0);
//        uFlex.FlexProcessor oFlexProc = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexProcessor)) as uFlex.FlexProcessor;
//        oFlexProc._oFlexProcessor = this;
//        _oSoftFlexParticles = GetComponent<uFlex.FlexParticles>();

//        //=== Create the managing object and related hotspot ===
//        _oObj = new CObject(this, 0, typeof(EFlexCloth), "Cloth " + gameObject.name);        //###IMPROVE: Name of soft body to GUI
//        _oObj.PropGroupBegin("", "", true);
//        _oObj.PropAdd(EFlexCloth.Tightness, "Tightness", 1.0f, 0.01f, 3.0f, "", CProp.Local);
//        _oObj.FinishInitialization();
//        _oWatchBone = _oBody.FindBone("chest");            //####HACK ####DESIGN: Assumes this cloth is a top!
//        _oHotSpot = CHotSpot.CreateHotspot(this, _oWatchBone, "Clothing", false, new Vector3(0, 0.22f, 0.04f));     //###IMPROVE!!! Position offset that makes sense for that piece of clothing (from center of its verts?)

//        //=== Set simulated rim verts to infinite mass so simulation does not move them (we do) ===
//        int nPairs = _aMapClothVertsSimToSkin.Count / 2;
//        for (int nPair = 0; nPair < nPairs; nPair++) {
//            int nPairX2 = nPair * 2;
//            ushort nVertSim = _aMapClothVertsSimToSkin[nPairX2];
//            _oSoftFlexParticles.m_particles[nVertSim].invMass = 0;
//        }

//        //_oObj.PropSet(EFlexCloth.Tightness, 2.0f);          //###TUNE

//		//=== Kludge the bone speed at startup to 'very high' so auto-tune will adjust for high initial movement ===
////		_nBoneSpeedSum = _aBoneSpeeds[_nBoneSpeedSlotNow++] = 1000000;

//        _nResetClothFrames_HACK = 100;
//	}

//	public override void OnDestroy() {
//		///UnityEngine.Object.DestroyImmediate(_oBBodyColCloth.gameObject);
//		///ErosEngine.Cloth_Destroy(_oObj._hObject);
//		base.OnDestroy();
//	}

//	public void Cloth_Reset() {
//        //for (int nVert = 0; nVert < _oBMeshClothAtStartup._memVerts.L.Length; nVert++)		// Copy each vert from the 'backup' mesh to this simulated cloth...
//        //	_memVerts.L[nVert] = _oBMeshClothAtStartup.transform.localToWorldMatrix.MultiplyPoint(_oBMeshClothAtStartup._memVerts.L[nVert]);	//... making sure to convert each vert from the backup mesh's local coordinates to the global coordinates used in PhysX
//        ///ErosEngine.Cloth_Reset(_oObj._hObject);         // Set PhysX simulated verts to the positions of our current verts (just now at start position)
//        //_nClothDampingBackupValue = _oProp_ClothDamping.PropGet();		// Backup the cloth damping value as we manually change it for many frames after reset
//        //_oProp_ClothDamping.PropSet(1.0f);				// Right after reset max out the cloth damping so it gracefully rests on body...
//        //_nNumFramesClothParamsInReset = 30;				//... and return to normal cloth parameter behavior after these many frames
//    }

//	public virtual void OnSimulatePost() {			// Post-process the cloth mesh after PhysX cloth simulation: set our vertex positions to what PhysX has calculated for this frame and weld boundary normals
//		///ErosEngine.Cloth_OnSimulatePost(_oObj._hObject);
//		//_oMeshNow.vertices = _memVerts.L;
//		//_oMeshNow.RecalculateNormals();				//###OPT!!!!!: Get from PhysX!!
//		//if ((Time.frameCount % 120) == 0)			//###IMPROVE!!! ###OPT!!!! Get from PhysX!!!
//		//	_oMeshNow.RecalculateBounds();
//	}

//    public virtual void OnSimulateBetweenPhysX23() { }      //###OBS



//    public void OnPropSet_NeedReset(CProp oProp, float nValueOld, float nValueNew) { }

//	//--------------------------------------------------------------------------	IHotspot interface

//	public void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) { }

//	public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {		//###DESIGN? Currently an interface call... but if only GUI interface occurs through CObject just have cursor directly invoke the GUI_Create() method??
//		if (eHotSpotEvent == EHotSpotEvent.ContextMenu)
//			_oHotSpot.WndPopup_Create(new CObject[] { _oObj });
//	}

//    public void OnPropSet_Tightness(float nValueOld, float nValueNew) {
//        uFlex.FlexSprings oFlexSprings = gameObject.GetComponent<uFlex.FlexSprings>();
//        for (int nSpring = 0; nSpring < oFlexSprings.m_springsCount; nSpring++)
//            oFlexSprings.m_springCoefficients[nSpring] = nValueNew;
//        //oFlexSprings.m_newStiffness = nValueNew;
//        //oFlexSprings.m_overrideStiffness = true;          //###NOTE: Not doing it this way as it iterates every frame!
//        Debug.LogFormat("Cloth Tightness {0}", nValueNew);
//    }

//    public void OnPropSet_Reset_HACK(float nValueOld, float nValueNew) {
//        _nResetClothFrames_HACK = (int)nValueNew;
//    }

//    //---------------------------------------------------------------------------	Flex
//    public void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
//        //=== Bake the baked part of our cloth.  We need its periphery verts to pin our simulated part of the cloth! ===
//        _oSkinMeshRendNow.BakeMesh(_oMeshClothSkinnedBaked);
//        Vector3[] aVertSkinned = _oMeshClothSkinnedBaked.vertices;
//        int nPairs = _aMapClothVertsSimToSkin.Count / 2;
//        //=== Iterate through all 'paired verts' to set the rim vert of the simulated cloth to the position of its equivalent vert in the skinned mesh ===
//        for (int nPair = 0; nPair < nPairs; nPair++) {
//            int nPairX2 = nPair * 2;
//            ushort nVertSim  = _aMapClothVertsSimToSkin[nPairX2 + 0];
//            ushort nVertSkin = _aMapClothVertsSimToSkin[nPairX2 + 1];
//            _oSoftFlexParticles.m_particles[nVertSim].pos = aVertSkinned[nVertSkin];
//        }
//        if (_nResetClothFrames_HACK > 0) {
//            for (int nVert = 0; nVert < _oBMeshClothAtStartup._memVerts.L.Length; nVert++)		// Copy each vert from the 'backup' mesh to this simulated cloth...
//                _oSoftFlexParticles.m_particles[nVert].pos = _oBMeshClothAtStartup.transform.localToWorldMatrix.MultiplyPoint(_oBMeshClothAtStartup._memVerts.L[nVert]);	//... making sure to convert each vert from the backup mesh's local coordinates to the global coordinates used in PhysX
//            _nResetClothFrames_HACK--;
//        }
//    }
//}





////###IMPROVE: Add skinned cloth?  Or keep only fully simulated??
////[HideInInspector]	public 	CMemAlloc<Vector3> 					_memVertsDst		= new CMemAlloc<Vector3>();	// The vertices that PhysX cloth baking returns to us.  Can be different from _memVerts we sent!!
////[HideInInspector]	public 	CMemAlloc<Vector3> 					_memVertsDstReset	= new CMemAlloc<Vector3>();	// The vertices as they were at cloth creation from Blender.  Used to reset the simulated cloth to its start position
////[HideInInspector]	public 	CMemAlloc<int>						_memTrisDst			= new CMemAlloc<int>();		// The triangles that PhysX cloth baking returns to us.  Can be different from _memTris we sent!!
////	[HideInInspector]	public	List<CMapTwinVert>	_aMapTwinVerts = new List<CMapTwinVert>();	// Collection of mapping between our boundary verts and the verts of our BodyRim.  Used to pin the boundary of our PhysX cloth to our rim so cloth doesn't float in space  ####OBS???
//////=== Iterate through the pins to update their position to PhysX ===
////foreach (CMapTwinVert oMapTwinVert in _aMapTwinVerts) {							//###CHECK!!!: From what domain of verts we set this?? what we sent or what PhysX sends us???
////	Vector3 vecPosLocalOffset = _oBody._oBSkinBaked.GetSkinnedVertex(oMapTwinVert.nVertHost);			// The skinned BodyRim should have been setup to skin this vert that Blender has setup for us...
////	CGame.Cloth_PinVert(_hCloth, oMapTwinVert.nVertPart, vecPosLocalOffset);						// Pin this cloth boundary vert to its proper position in 3D space... non-boundary cloth around it will move toward it during next cloth simulation frame
////}

////=== Iterate through the pins to copy the normals from the skin host to our pin verts.  If we don't a 'seam' will show between the two meshes! ===		
////foreach (CMapTwinVert oMapTwinVert in _aMapTwinVerts) {
////	Vector3 vecNormal = _oBody._oBSkinBaked.GetSkinnedNormal(oMapTwinVert.nVertHost);
////	_memNormals.L[oMapTwinVert.nVertPart] = vecNormal;
////}
////=== Set the Unity verts to what PhysX has calculated for us in this frame ===
////_oMeshNow.Clear(false);			//###CHECK!!!!
////_oMeshNow.MarkDynamic();		// Docs say "Call this before assigning vertices to get better performance when continually updating mesh"
////_oMeshNow.vertices = _memVertsDst.L;
////_oMeshNow.normals	= _memNormals.L;
////_oMeshNow.triangles = _memTrisDst.L;				//###HACK!! MATERIALS!!!   ###OPT!!! MOVE OUT!!



//////_oMeshClothSkinnedBaked.RecalculateNormals();			//###LEARN: Weirdly enough this messes up the normals -> reset of pin vert normals if we recalc here will show seams as body moves away from startup T-pose.  I have no idea why!!
//////_oMeshClothSkinnedBaked.RecalculateBounds();			//####SOON: Run this once in a while.
//////////Vector3[] aVerts = _oMeshClothSkinnedBaked.vertices;	//###LEARN!!!!!: Absolutely IMPERATIVE to obtain whole array before loop like the one below... with individual access profiler reveals 7ms per frame if not!!!!!!!		###TODO!!!!!: Insure this is done throughout the game
////_memVertsSkinnedCloth.PinInMemory();
////Vector3[] aNormals = _oMeshClothSkinnedBaked.normals;
////_memVertsSkinnedCloth.L = _oMeshClothSkinnedBaked.vertices; //###LEARN!!!!!: Absolutely IMPERATIVE to obtain whole array before loop like the one below... with individual access profiler reveals 7ms per frame if not!!!!!!!		###TODO!!!!!: Insure this is done throughout the game
////for (int nVert = 0; nVert < _oMeshClothSkinnedBaked.vertexCount; nVert++) {
////	_memVertsSkinnedCloth.L[nVert] = aVerts[nVert];




////public virtual void OnSimulateBetweenPhysX23() {        // Pre-process the cloth mesh before PhysX simulation: set the PhysX pins so cloth doesn't float in space!
////	if (Input.GetKeyDown(KeyCode.Slash))
////		Cloth_Reset();


////	//=== Bake the baked part of our cloth.  We need its periphery verts to pin our simulated part of the cloth! ===
////	_oSkinMeshRendNow.BakeMesh(_oMeshClothSkinnedBaked);
////	CMemAlloc<Vector3> memVertsSkinnedCloth = new CMemAlloc<Vector3>(_oMeshClothSkinnedBaked.vertices.Length);
////	memVertsSkinnedCloth.AssignAndPin(_oMeshClothSkinnedBaked.vertices);

////if (_oBBodyColCloth != null)
////	_oBBodyColCloth.OnSimulatePre();
////ErosEngine.Cloth_OnSimulatePre(_oObj._hObject, _oBBodyColCloth._hBodyColCloth, memVertsSkinnedCloth.P);

////===== CLOTH PARAMATER AUTO-TUNE FUNCTIONALITY =====
///*####TODO: Cloth autotune
//- Now can autoadjust but sucks we decoupled GUI.  Move equivalent to C++
//- Find a way to reduce tiny adjustments everytime?  (cheap if we move to C++?)
//- Tune a bit	*/

////if (_nNumFramesClothParamsInReset > 1) {                // Cloth just came out of reset, don't change anything.

////	_nNumFramesClothParamsInReset--;

////} else if (_nNumFramesClothParamsInReset == 1) {		// Last frame with cloth still set with parameters for reset.  Restore parameter to default

////	_oProp_ClothDamping.PropSet(_nClothDampingBackupValue);		// Restore cloth damping to the way user set it before reset
////	_nNumFramesClothParamsInReset = 0;

////} else {
////	//=== Calculate the speed of the watched bone this frame ===
////	if (_vecWatchBonePosLast != Vector3.zero) {					// Don't do anything on the very first frame
////		_vecWatchBonePosNow = _oWatchBone.position;
////		float nDistMoveMmPerFrame = (_vecWatchBonePosLast - _vecWatchBonePosNow).magnitude * 1000.0f;

////		//=== Calculate the rolling average of bone speed (to smooth out) ===
////		_nBoneSpeedSum -= _aBoneSpeeds[_nBoneSpeedSlotNow];             // Remove oldest history value from sum (before it gets overwritten with newest value)
////		_aBoneSpeeds[_nBoneSpeedSlotNow] = nDistMoveMmPerFrame;                   //... store distance in current (newest) slot...
////		_nBoneSpeedSum += nDistMoveMmPerFrame;                                    //... add newest entry to sum...
////		_nBoneSpeedSlotNow++;                                           //... increment to next slot...
////		if (_nBoneSpeedSlotNow >= C_NumBoneSpeedSlots)                  //... wrapping around if needed...
////			_nBoneSpeedSlotNow = 0;
////		float nBoneSpeedAvg = _nBoneSpeedSum / C_NumBoneSpeedSlots;    // Calculate rolling average for this frame
////		if (nBoneSpeedAvg < nDistMoveMmPerFrame)									// For immediate response to quickly rising movement make sure the average is never less than the movement speed of the current frame
////			nBoneSpeedAvg = nDistMoveMmPerFrame;

////		//if (nBoneSpeedAvg > 4.0f)
////		//	nBoneSpeedAvg = nBoneSpeedAvg;

////		_oProp_ClothDamping			.RuntimeAdjust_SetTargetValue(nBoneSpeedAvg);
////		_oProp_FrictionCoefficient	.RuntimeAdjust_SetTargetValue(nBoneSpeedAvg);
////		_oProp_StiffnessFrequency	.RuntimeAdjust_SetTargetValue(nBoneSpeedAvg);
////		_oProp_SolverFrequency		.RuntimeAdjust_SetTargetValue(nBoneSpeedAvg);

////		CGame.INSTANCE._oTextUR.text = string.Format("Cloth Avg Speed = {0:F4}", nBoneSpeedAvg, nDistMoveMmPerFrame);       //CGame.INSTANCE._aGuiMessages[(int)EGameGuiMsg.Dev1]
////	}
////	_vecWatchBonePosLast = _vecWatchBonePosNow;
////}
////}








////_oObj = new CObject(this, 0, typeof(ECloth), "Cloth");
////_oObj._hObject = ErosEngine.Cloth_Create("Cloth", _oObj.GetNumProps(), _memVerts.L.Length, _memVerts.P, _memTris.L.Length / 3, _memTris.P, _memMapClothVertsSimToSkin.P, _memMapClothVertsSimToSkin.L.Length/2, _nClothInitStretch, _nClothInitStretchFirstFrame);

////_oObj.PropGroupBegin("", "", true);
////_oObj.PropAdd(ECloth.Cloth_GPU,				"GPU",					1, "", CProp.AsCheckbox);
////_oObj.PropAdd(ECloth.SolverFrequency,		"SolverFrequency",		120, 30, 240, ""); //###TEMP ###TUNE!!!!
////_oObj.PropAdd(ECloth.Cloth_Stiffness,		"Stiffness",			0.70f, 0, 1, "");            //####MOD: Was 0.9 x3
////_oObj.PropAdd(ECloth.Bending,				"Bending",				0.70f, 0, 1, "");
////_oObj.PropAdd(ECloth.Shearing,				"Shearing",				0.70f, 0, 1, "");

////_oObj.PropAdd(ECloth.StiffnessFrequency,	"StiffnessFrequency",	20, 1, 100, "ControlAutoTriColDists the power-law nonlinearity of all rate of change parameters.");  //###BUG?: Not recommended to change after cloth creation!
////_oObj.PropAdd(ECloth.Cloth_Damping,			"Damping",				0.0f, 0, 1, "");
////_oObj.PropAdd(ECloth.Cloth_Gravity,			"Gravity",				-1.0f, -3, 3, "");   //###DESIGN!!: How to switch off gravity??
////_oObj.PropAdd(ECloth.FrictionCoefficient,	"FrictionCoefficient",	1.0f, 0.01f, 1, ""); //###NOTE: Only spheres and capsules apply friction, not triangles!  ####REVA: Was .5

//////=== Set runtime adjustment parameters that will push some cloth parameters toward values that can handle much more movement per frame ===	//####TUNE!
////_oProp_ClothDamping = _oObj.PropFind(ECloth.Cloth_Damping);
////_oProp_FrictionCoefficient = _oObj.PropFind(ECloth.FrictionCoefficient);
////_oProp_StiffnessFrequency = _oObj.PropFind(ECloth.StiffnessFrequency);
////_oProp_SolverFrequency = _oObj.PropFind(ECloth.SolverFrequency);

////_oProp_ClothDamping.RuntimeAdjust_SetTargetValue(0.0f, 2.0f);       // The most important: We *must* greatly reduce damping when moving so cloth has the least momentum to collide against body
////_oProp_FrictionCoefficient.RuntimeAdjust_SetTargetValue(1.0f, 3.0f);        // We max the cloth friction to enhance the body's ability to drag it along.
////_oProp_StiffnessFrequency.RuntimeAdjust_SetTargetValue(100.0f, 5.0f);       // We increase the stiffness frequency to greatly stiffen the cloth
////_oProp_SolverFrequency.RuntimeAdjust_SetTargetValue(120.0f, 6.0f);      // We increase cloth solver frequency to raise the repulsion against the body colliders (expensive!!!)

////_oObj.FinishInitialization();

//////=== Connect this cloth to optional breast colliders.   This MUST be done before going online just below ===
////if (_oBody._oBreastL != null)				//####TODO ####SOON: Extract from some new cloth flag if this cloth needs breast colliders!
////	ErosEngine.Cloth_ConnectClothToBreastColliders(_oObj._hObject, _oBody._oBreastL._oBodyColBreast._hBodyColBreast, _oBody._oBreastR._oBodyColBreast._hBodyColBreast);

//////=== Go online and actually create the PhysX objects ===
////ErosEngine.Object_GoOnline(_oObj._hObject, IntPtr.Zero);

//////=== Create the 'skinned body collider' skinned mesh that repells cloth by baking itself at every game frame and updating PhysX colliders to repel cloth in the shape of the body ===
////_oBBodyColCloth = CBBodyColCloth.Create(null, this);   //####DEV: Push choices of which collider here!  // Create the important body collider to repel this cloth from our owning body

//////=== Obtain reference to special breast colliders if body has them for faster collider update at runtime ===
////if (_oBody._oBreastL != null) {
////	_hBodyColBreastL = _oBody._oBreastL._oBodyColBreast._hBodyColBreast;
////	_hBodyColBreastR = _oBody._oBreastR._oBodyColBreast._hBodyColBreast;
////}










////    IntPtr				_hBodyColBreastL, _hBodyColBreastR;	// Convenience reference to our body's (optional) L/R breast collider for faster runtime update.  ###DESIGN: Keep?
////CProp				_oProp_ClothDamping;				// Cached properties for fast runtime accesss
////CProp				_oProp_FrictionCoefficient;
////CProp				_oProp_StiffnessFrequency;
////CProp				_oProp_SolverFrequency;

////    const int			C_NumBoneSpeedSlots = 32;		// Cloth auto-adjust per frame movement parameters
////   float[]				_aBoneSpeeds = new float[C_NumBoneSpeedSlots];
////   int					_nBoneSpeedSlotNow = 0;
////   float				_nBoneSpeedSum = 0.0f;

////int                 _nNumFramesClothParamsInReset = 0;      // Number of remaining frames to adjust cloth parameters for reset (used to maximize chances of cloth sticking to body)
////float				_nClothDampingBackupValue;              // Temporary storage for 'Cloth_Damping' property to be stored during cloth reset procedure





///*
//- Fix seams removal in Blender...only disable for cloth with no UVs?????
//- How to come out of init????
//	- Need to come out of reset with high damping so cloth 'sticks'
//- Will need to copy normals from skinned to simulated

//- Still stuck on seam verts with PhysX cloth.
//- Now have kludged in skinned part of cloth... pin sim verts to it!
//- Clean up cloth architecture and separate in super-clean files!
//- Remove hack in gBL_GetMesh()?

//- Can roll average speed, Implement 'multiplyer' in CProp extension class to gradually set it toward its 'save high-movement value'
//- Implement hysteresis so we don't set every frame.
//- Remove 1 zone and have gradient....then start tuning!
//*/


