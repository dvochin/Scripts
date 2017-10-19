/*###DOCS25: Aug 2017 - Penis SoftBody
=== DEV ===
- Need to accurately determine the beginning of the shaft to avoid moving ball bones
- Need to create a convincing 'limp to erect' effect... but is stiffness enough??  (base needs to be stronger)
- Need a convincing penis bend up / down morph... (both base and shaft)
-IDEA: Have button combo control other penis attributes

--- Penis sizing architecture ---
	- Based on the penis collider:
		- At init find the left-most and right-most verts
		- Occasionally compute the distance between the two and update collider's _nPenisDiameter
		- When vagina penis sensor detects penis tip is entering vagina it begins a co-routine that occasionally updates its size (to account for penis growth)
			- (Vagina sensor obtains the penis collider instance from the PhysX raycast results)

=== NEXT ===
- Property group setting at startup
- Scrotum slices / bones
- Port more from CPenis: length, scrotum

=== TODO ===
- GUI object relationship with base?
- We'll need penis diameter for vagina collider opening!

=== LATER ===

=== OPTIMIZATIONS ===

=== REMINDERS ===
- Fluid: Collision against ground plane stops fluid right away!  With tri colliders it bounces!!
- Move transparency controls somewhere else

=== IMPROVE ===
- Uretra is a bit high for center of penis
- Improve VR Wand debug property editing

=== NEEDS ===

=== DESIGN ===
- Penis sizing strategy:
	- Don't actually need the complex & problematic particle trim IF we set Flex's 'Inertia Bias' high enough (e.g. 30 with particle friction at .1 and numIterations at 70)
		- Smaller penis make particles too close and causes problem with some Flex forces like particle friction... but maybe with proper tuning
	- So... could ask user to design mid-way penis with a range that is the same for both smaller & bigger?  (Smallest penis gets particles too close and biggest one particles not at optimal spacing)
	- Need to test the tricky parameters such as particle friction and shape inertia for penis / breast size

=== QUESTIONS ===

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===
- Collisions between dynamic particles and pinned ones!  Verify min distance!
- Bad skinned verts in penis collider
- Base L / R looks poor
- ShapeDefs done very quickly.  Refine!

=== WISHLIST ===

*/

using UnityEngine;
using System.Collections.Generic;

public class CSoftBody_Penis : CSoftBody {

	Transform			_oCumEmitterT;
	CPenisSlice[]		_aPenisSlices;

	public override void Initialize(CBody oBody, int nSoftBodyID, Transform oBoneRootT) {
		base.Initialize(oBody, nSoftBodyID, oBoneRootT);

        //=== Create the managing object and related hotspot ===	//###DESIGN:?? Inheritance with derived classes.  Softbodies repeat same properties...
		_oObj = new CObject(this, "Penis", "Penis");				//###WEAK: String names duplication x 3
		_oObj.Event_PropertyValueChanged += Event_PropertyChangedValue;
		CPropGrpEnum oPropGrp = new CPropGrpEnum(_oObj, "Penis", typeof(ESoftBodyPenis));
        oPropGrp.PropAdd(ESoftBodyPenis.Stiffness,			"Stiffness",		0.1f, 0.01f, 0.3f, "");			//###TUNE:!!!	 ###PROBLEM: Stiffness can 'blow up' depending on many Flex parameters!	  ###NOTE: Stiffness *must* propagate itself through all particles to avoid softbody falling appart!!
        oPropGrp.PropAdd(ESoftBodyPenis.Size,				"Size",				1.0f, 0.7f, 1.3f, "");
        oPropGrp.PropAdd(ESoftBodyPenis.BaseUpDown,			"Base Up/Down",		0.0f, -45.0f, 45.0f, "");
        oPropGrp.PropAdd(ESoftBodyPenis.BaseLeftRight,		"Base Left/Right",	0.0f, -45.0f, 45.0f, "");
        oPropGrp.PropAdd(ESoftBodyPenis.ShaftUpDown,		"Curve Up/Down",	0.0f, -25.0f, 25.0f, "");
        oPropGrp.PropAdd(ESoftBodyPenis.ShaftLeftRight,		"Curve Left/Right",	0.0f, -20.0f, 20.0f, "");
        oPropGrp.PropAdd(ESoftBodyPenis.ShaftTwistLeftRight,"Twist Left/Right",	0.0f, -5.0f, 5.0f, "");
        oPropGrp.PropAdd(ESoftBodyPenis.Reset_HACK,			"Reset",			0.0f, 0.0f, 1.0f, "");
        oPropGrp.PropAdd(ESoftBodyPenis.Kinematic_HACK,		"Kinematic",		0.0f, 0.0f, 1.0f, "");
		CGame.INSTANCE._oVrWandR._oPropDebugJoystickVer_HACK = oPropGrp.PropFind(ESoftBodyPenis.BaseUpDown);
		CGame.INSTANCE._oVrWandR._oPropDebugJoystickHor_HACK = oPropGrp.PropFind(ESoftBodyPenis.BaseLeftRight);
		//CGame.INSTANCE._oVrWandL ._oPropDebugJoystickVer_HACK = oPropGrp.PropFind(ESoftBodyPenis.ShaftUpDown);
		//CGame.INSTANCE._oVrWandL ._oPropDebugJoystickHor_HACK = oPropGrp.PropFind(ESoftBodyPenis.ShaftLeftRight);
		CGame.INSTANCE._oVrWandL._oPropDebugJoystickHor_HACK = oPropGrp.PropFind(ESoftBodyPenis.Size);
		CGame.INSTANCE._oVrWandL._oPropDebugJoystickVer_HACK = oPropGrp.PropFind(ESoftBodyPenis.Stiffness);
		///CGame.INSTANCE._oVrWandL ._oPropDebugJoystickHor_HACK = oPropGrp.PropFind(ESoftBodyPenis.TrimFlexParticles_HACK);
		///CGame.INSTANCE._oVrWandR._oPropDebugJoystickHor_HACK = oPropGrp.PropFind(ESoftBodyPenis.Kinematic_HACK);
		///CGame.INSTANCE._oVrWandR._oPropDebugJoystickVer_HACK = oPropGrp.PropFind(ESoftBodyPenis.Reset_HACK);

		_oObj.FinishInitialization();

		//=== Find the Uretra particle / bone ===
		for (ushort nParticle = 0; nParticle < _aParticleInfo.Count; nParticle++) {					
			int nParticleInfo = _aParticleInfo[nParticle];
			int nParticleFlags = (nParticleInfo & CSoftBody.C_ParticleInfo_Mask_Flags);

			if (_oCumEmitterT == null && (nParticleFlags & CSoftBody.C_ParticleInfo_BitFlag_Uretra) != 0) {
				Debug.LogFormat("-> CSoftBody_Penis() found uretra particle #{0}", nParticle);

				//=== Find the bone associated with this uretra particle ===
				ushort nBoneUretra = (ushort)((nParticleInfo & CSoftBody.C_ParticleInfo_Mask_BoneID) >> CSoftBody.C_ParticleInfo_BitShift_BoneID);
				Transform oBoneUretraT = _mapBonesDynamic[nBoneUretra];

				//=== Instantiate an emitter and reparent / reorient to the uretra bone ===
				GameObject oFlexEmitterGOT = Resources.Load("Prefabs/CFlexEmitter") as GameObject;
				GameObject oFlexEmitterGO = GameObject.Instantiate(oFlexEmitterGOT) as GameObject;
				oFlexEmitterGO.name = string.Format("CFlexEmitter");
				_oCumEmitterT = oFlexEmitterGO.transform;
				_oCumEmitterT.SetParent(oBoneUretraT);
				_oCumEmitterT.position = _oMeshBaked.vertices[nParticle];		//###NOTE: Feed in the GLOBAL position of where the particle is at init.  As the shape's center gives this bone a position further away from penis tip edge Unity will calculate the proper localPosition to keep the emitter exactly where the uretra is even as bone / shape moves.
				_oCumEmitterT.localRotation = Quaternion.Euler(-90, 0, 0);                  //###WEAK: Blender bones oriented 90 degrees off.  This re-orientation was determined by observation
				_oCumEmitterT.localScale = Vector3.one;
			}
		}

		//===== CREATE PENIS 'SLICES' (SUBDIVISIONS) =====
		//=== Subdivide particle / bones into 'slices' that will allow penis bending at game-time ===
		int nSlices = 10;								//###TUNE
		_aPenisSlices = new CPenisSlice[nSlices];

		Vector3 vecPenisStart	= _oBoneRootT.position;
		Vector3 vecPenisEnd		= _oCumEmitterT.position;
		vecPenisStart.x = 0;
		vecPenisStart.z += 0.022f;		//###HACK:!!!!!! Perform crappy manual adjustment of 'beginning of penis shaft' so we don't change scrotum bones!  Get this info from a marked Blender vert?  (Improved: don't process bones under a certain vert)
		vecPenisEnd.Set(0, vecPenisStart.y, vecPenisEnd.z);
		float nLenPenis = vecPenisEnd.z - vecPenisStart.z;
		float nLenPenisSlice = nLenPenis / nSlices;

		//=== Create the penis slices ===
		for (int nSlice = 0; nSlice < nSlices; nSlice++) {
			float nPosSliceZ = vecPenisStart.z + (nSlice + 1) * nLenPenisSlice;
			Vector3 vecPos = new Vector3(0, vecPenisEnd.y, nPosSliceZ);
			_aPenisSlices[nSlice] = new CPenisSlice(this, nSlice, vecPos);
			if (nSlice >= 1)
				_aPenisSlices[nSlice]._oSliceT.SetParent(_aPenisSlices[nSlice-1]._oSliceT);
		}
		_aPenisSlices[0]._oSliceT.SetParent(transform);     // Set penis slice chain root to be child of our penis node

		//=== Assign the penis particle bones to the just-created slices ===
		for (int nArrayIndex = 0; nArrayIndex < _aFlatMapBoneIdToShapeId.Count; nArrayIndex += 2) {
			ushort nBoneID	= _aFlatMapBoneIdToShapeId[nArrayIndex + 0];    //# Serialiazable array storing what shapeID each bone has.  Flat map is a simple list of <Bone1>, <Shape1>, <Bone2>, <Shape2>, etc.
			ushort nShapeID = _aFlatMapBoneIdToShapeId[nArrayIndex + 1];
			ushort nParticleID = _mapShapesToParticles[nShapeID];
			Transform oBoneParticleT	= _mapBonesDynamic[nBoneID];		// Obtain reference to the particle / bone and its parent (fake shape 'bone').  We set the shape 'bone' the position / rotation of the corresponding Flex shape knowing that the presentation mesh will be updated only from the real particle bone.
			float nPosBoneShapeZ = oBoneParticleT.position.z;
			int nSlice = (int)((nPosBoneShapeZ - vecPenisStart.z) / nLenPenisSlice);
			nSlice = Mathf.Clamp(nSlice, 0, nSlices - 1);
			_aPenisSlices[nSlice].AddChildBone(oBoneParticleT, nParticleID);
		}
	}
	public override void DoDestroy() {
		//=== Destroy the PARENTS of the dynamic bones we created ===
		foreach (KeyValuePair<ushort, Transform> oPair in _mapBonesDynamic)
			GameObject.Destroy(oPair.Value.parent.gameObject);		// Because we create shapes for every particle / bone, lookup to the parent so we can delete both shape and bone in one call
		GameObject.Destroy(gameObject);
	}


	public override void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) {
		base.PreContainerUpdate(solver, cntr, parameters);
	}

	public override void ShapeDef_Enter() {
		base.ShapeDef_Enter();
		foreach (CPenisSlice oPenisSlice in _aPenisSlices)
			oPenisSlice.ShapeDef_Enter();
	}

	public override void ShapeDef_Leave() {
		foreach (CPenisSlice oPenisSlice in _aPenisSlices)
			oPenisSlice.ShapeDef_Leave();
		base.ShapeDef_Leave();
	}


	public override void OnPropChanged(CProp oProp) {               // This *must* be called within context of Flex's 'PreContainerUpdate()'
		base.OnPropChanged(oProp);
		switch (oProp._nPropOrdinal) {
			case (int)ESoftBodyPenis.Stiffness:
				ShapeDef_SetStiffness(oProp._nValueLocal);
				break;

			case (int)ESoftBodyPenis.Size:
				ShapeDef_Enter();
				_aPenisSlices[0]._oSliceT.localScale = new Vector3(oProp._nValueLocal, oProp._nValueLocal, oProp._nValueLocal);
				ShapeDef_Leave();
				break;

			case (int)ESoftBodyPenis.BaseUpDown:
			case (int)ESoftBodyPenis.BaseLeftRight:
				Util_AdjustPenisSliceRotation(1, 2, true);
				break;

			case (int)ESoftBodyPenis.ShaftUpDown:
			case (int)ESoftBodyPenis.ShaftLeftRight:
			case (int)ESoftBodyPenis.ShaftTwistLeftRight:
				Util_AdjustPenisSliceRotation(2, _aPenisSlices.Length-1, false);
				break;

			case (int)ESoftBodyPenis.Reset_HACK:
				if (oProp._nValueLocal != 0) {
					for (int nParticle = 0; nParticle < _nParticles; nParticle++) {
						_oFlexParticles.m_particles[nParticle].pos = _oFlexParticles.m_restParticles[nParticle].pos;
						_oFlexParticles.m_particles[nParticle].invMass = 0;
					}
				}
				break;

			case (int)ESoftBodyPenis.Kinematic_HACK:
				for (int nParticle = 0; nParticle < _nParticles; nParticle++) {
					int nParticleType = _aParticleInfo[nParticle] & C_ParticleInfo_Mask_Type;
					if ((nParticleType & C_ParticleInfo_BitTest_IsSimulated) != 0)
						_oFlexParticles.m_particles[nParticle].invMass = (oProp._nValueLocal != 0) ? 0 : 1;
				}
				break;

		}
	}

	void Util_AdjustPenisSliceRotation(int nSliceStart, int nSliceEnd, bool bIsBase) {
		Quaternion quatRot;
		if (bIsBase)
			quatRot = Quaternion.Euler(_oObj.PropGet(0, (int)ESoftBodyPenis.BaseUpDown), _oObj.PropGet(0, (int)ESoftBodyPenis.BaseLeftRight), 0);
		else
			quatRot = Quaternion.Euler(_oObj.PropGet(0, (int)ESoftBodyPenis.ShaftUpDown), _oObj.PropGet(0, (int)ESoftBodyPenis.ShaftLeftRight), _oObj.PropGet(0, (int)ESoftBodyPenis.ShaftTwistLeftRight));
		ShapeDef_Enter();
		for (int nSlice = nSliceStart; nSlice < nSliceEnd; nSlice++)
			_aPenisSlices[nSlice]._oSliceT.localRotation = quatRot;
		ShapeDef_Leave();
	}



	public class CPenisSliceBone {

		public CPenisSlice  _oPenisSlice;
		public Transform    _oBoneParticleT;            //###DESIGN: No longer required now that we directly modify rest particles... but can be useful later??
		public Transform    _oBoneShapeDefT;
		Vector3             _vecPosBackupG;
		Quaternion          _quatRotBackupG;
		public int          _nParticleID;

		public CPenisSliceBone(CPenisSlice oPenisSlice, int nParticleID, Transform oParticleBoneT) {
			_oPenisSlice        = oPenisSlice;
			_nParticleID        = nParticleID;
			_oBoneParticleT     = oParticleBoneT;
			if (CPenisSlice.C_DEBUG_CreateMarkers) {
				GameObject oMarkerCubeGOT = Resources.Load("ModelsOLD/Markers/MarkerCubeDir") as GameObject;        //###TODO: Move resource!
				_oBoneShapeDefT = (GameObject.Instantiate(oMarkerCubeGOT) as GameObject).transform;
				_oBoneShapeDefT.GetComponent<MeshRenderer>().material.color = Color.blue;
			} else {
				_oBoneShapeDefT = new GameObject().transform;
			}
			_oBoneShapeDefT.name = _oBoneParticleT.name + "-ShapeDef";
			_oBoneShapeDefT.position    = _oBoneParticleT.position;
			_oBoneShapeDefT.rotation    = _oBoneParticleT.rotation;
			_oBoneShapeDefT.localScale  = _oBoneParticleT.localScale;
			_oBoneShapeDefT.SetParent(_oPenisSlice._oSliceT);
		}

		public void ShapeDef_Enter() {
			_vecPosBackupG      = _oBoneParticleT.position;
			_quatRotBackupG     = _oBoneParticleT.rotation;
		}

		public void ShapeDef_Leave() {
			_oPenisSlice._oSoftBodyPenis._oFlexParticles.m_restParticles[_nParticleID].pos = _oBoneShapeDefT.position;
			_oPenisSlice._oSoftBodyPenis._aQuatParticleRotations[_nParticleID] = _oBoneShapeDefT.rotation;
			_oBoneParticleT.position    = _vecPosBackupG;
			_oBoneParticleT.rotation    = _quatRotBackupG;
		}
	}

	public class CPenisSlice {

		public CSoftBody_Penis          _oSoftBodyPenis;
		public int                      _nSliceID;
		public Transform                _oSliceT;
		public List<CPenisSliceBone>    _aSliceBones = new List<CPenisSliceBone>();
		public static bool              C_DEBUG_CreateMarkers = false;          //###IMPROVE: Use this debug technique throughtout the codebase!


		public CPenisSlice(CSoftBody_Penis oSoftBodyPenis, int nSliceID, Vector3 vecPos) {
			_oSoftBodyPenis = oSoftBodyPenis;
			_nSliceID = nSliceID;
			if (CPenisSlice.C_DEBUG_CreateMarkers) {
				GameObject oMarkerCubeGOT = Resources.Load("ModelsOLD/Markers/MarkerCubeDir") as GameObject;
				_oSliceT = (GameObject.Instantiate(oMarkerCubeGOT) as GameObject).transform;
				_oSliceT.GetComponent<MeshRenderer>().material.color = Color.cyan;
			} else {
				_oSliceT = new GameObject().transform;
			}
			_oSliceT.position = vecPos;
			_oSliceT.name = "+PenisSlice-" + _nSliceID.ToString();
		}

		public void AddChildBone(Transform oParticleBoneT, int nParticleID) {
			_aSliceBones.Add(new CPenisSliceBone(this, nParticleID, oParticleBoneT));
		}

		public void ShapeDef_Enter() {
			foreach (CPenisSliceBone oSliceBone in _aSliceBones)
				oSliceBone.ShapeDef_Enter();
		}

		public void ShapeDef_Leave() {
			foreach (CPenisSliceBone oSliceBone in _aSliceBones)
				oSliceBone.ShapeDef_Leave();
		}
	}
};

public class CBSkinBaked_PenisMeshCollider : CBSkinBaked {      // CBSkinBaked_PenisMeshCollider: A mesh collider created from baked mesh created every frame from a reduced-geometry penis.  Used to open vagina via its raycasting approach to penetration
	MeshCollider _oMeshCollider;

	public override void OnDeserializeFromBlender(params object[] aExtraArgs) {
		base.OnDeserializeFromBlender(aExtraArgs);
		gameObject.layer = LayerMask.NameToLayer("Penis");
		_oMeshCollider = CUtility.FindOrCreateComponent(gameObject, typeof(MeshCollider)) as MeshCollider;
		_oMeshCollider.sharedMesh = _oMeshBaked;
	}

	public override void OnSimulate() {
		base.OnSimulate();
		_oMeshCollider.sharedMesh = _oMeshBaked;
	}
}


//oPropGrp.PropAdd(ESoftBodyPenis.Transparency,		"Transparency",			0.0f, 0.0f, 100.0f, "");
//oPropGrp.PropAdd(ESoftBodyPenis.TransparencyBody_HACK,	"Body Transparency",	0.0f, 0.0f, 100.0f, "");		//###MOVE
			//case (int)ESoftBodyPenis.Transparency:
			//	_oBody.Util_AdjustMaterialTransparency("Penis", oProp._nValueLocal / 100, false);
			//	break;
			//case (int)ESoftBodyPenis.TransparencyBody_HACK:
			//	_oBody.Util_AdjustMaterialTransparency("Penis", oProp._nValueLocal / 100, true);
			//	break;

