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

	public CFlexEmitter     _oCumEmitterT;
	CPenisSlice[]		    _aPenisSlices;

	public override void Initialize(CBody oBody, int nSoftBodyID, Transform oBoneRootT) {
		base.Initialize(oBody, nSoftBodyID, oBoneRootT);

        //=== Create the managing object and related hotspot ===	//###DESIGN:?? Inheritance with derived classes.  Softbodies repeat same properties..s.
		_oObj = new CObj("Penis", "Penis", null);				// _oBody._oBodyBase._oObj
		_oObj.Event_PropertyValueChanged += Event_PropertyChangedValue;
        _oObj.Add("Erection",			this, 0.03f,  0.03f, 0.25f, "");			//###TUNE:!!!!! Extremely important and sensitive!  Heavily affected by Flex solver iterations!!! 	 ###PROBLEM: Stiffness can 'blow up' depending on many Flex parameters!	  ###NOTE: Stiffness *must* propagate itself through all particles to avoid softbody falling appart!!
        _oObj.Add("Size",				this, 1.0f,   0.85f, 1.25f, "");
        _oObj.Add("Up/Down",			this, 0.0f, -10.0f, 10.0f, "");
        _oObj.Add("Left/Right",		    this, 0.0f, -10.0f, 10.0f, "");
        _oObj.Add("Cum",				this, 0.0f,   0.0f, 1.0f, "");
        //_oObj.Add("TestCenterCurve",	this, 0.0f,   0.0f, 1.0f, "");

        //###DEV27: ###HACK
        CGame.INSTANCE._oBrowser_HACK._oWebViewTree_Cock_HACK = new CWebViewTree(CGame.INSTANCE._oBrowser_HACK, _oObj, "Cock_HACK", "Cock");

		//_oObj.Add(ESoftBodyPenis.ShaftTwistLeftRight,"Twist Left/Right",	0.0f, -5.0f, 5.0f, "");
		//_oObj.Add(ESoftBodyPenis.Reset_HACK,			"Reset",			0.0f, 0.0f, 1.0f, "");
		//_oObj.Add(ESoftBodyPenis.Kinematic_HACK,		"Kinematic",		0.0f, 0.0f, 1.0f, "");

		//CGame._oVrWandL._oObjDebugJoystickHor_HACK = _oObj.Find("Size");         // Test code to connect Vr wand directly to various penis axis
		//CGame._oVrWandL._oObjDebugJoystickVer_HACK = _oObj.Find("Erection");
		//CGame._oVrWandR._oObjDebugJoystickVer_HACK = _oObj.Find("Up/Down");
		//CGame._oVrWandR._oObjDebugJoystickHor_HACK = _oObj.Find("Left/Right");
		//CGame._oVrWandR._oObjDebugJoystickPress_HACK = _oObj.Find("Cum");

		///CGame._oVrWandR._oObjDebugJoystickHor_HACK = _oObj.Find(ESoftBodyPenis.Kinematic_HACK);
		///CGame._oVrWandR._oObjDebugJoystickVer_HACK = _oObj.Find(ESoftBodyPenis.Reset_HACK);

		//=== Find the Uretra particle / bone ===
        Mesh oMeshBaked = Baking_GetBakedSkinnedMesh();
		for (ushort nParticle = 0; nParticle < _aParticleInfo.Count; nParticle++) {					
			int nParticleInfo = _aParticleInfo[nParticle];
			int nParticleFlags = (nParticleInfo & CSoftBody.C_ParticleInfo_Mask_Flags);

            //###DEV27Z: ###CHECK Uretra not always defined from Blender??
			if (_oCumEmitterT == null && (nParticleFlags & CSoftBody.C_ParticleInfo_BitFlag_Uretra) != 0) {
				Debug.LogFormat("-> CSoftBody_Penis() found uretra particle #{0}", nParticle);

				//=== Find the bone associated with this uretra particle ===
				ushort nBoneUretra = (ushort)((nParticleInfo & CSoftBody.C_ParticleInfo_Mask_BoneID) >> CSoftBody.C_ParticleInfo_BitShift_BoneID);
				Transform oBoneUretraT = _mapBonesDynamic[nBoneUretra];

                //=== Instantiate an emitter and reparent / reorient to the uretra bone ===
                _oCumEmitterT = CUtility.InstantiatePrefab<CFlexEmitter>("Prefabs/CFlexEmitter", "CFlexEmitter", oBoneUretraT);
                _oCumEmitterT.DoStart();
                _oCumEmitterT.transform.position = oMeshBaked.vertices[nParticle];		//###NOTE: Feed in the GLOBAL position of where the particle is at init.  As the shape's center gives this bone a position further away from penis tip edge Unity will calculate the proper localPosition to keep the emitter exactly where the uretra is even as bone / shape moves.
				_oCumEmitterT.transform.localRotation = Quaternion.Euler(-90, 0, 0);                  //###WEAK: Blender bones oriented 90 degrees off.  This re-orientation was determined by observation

                break;      //###CHECK:
			}
		}

		//===== CREATE PENIS 'SLICES' (SUBDIVISIONS) =====
		//=== Subdivide particle / bones into 'slices' that will allow penis bending at game-time ===
		int nSlices = 10;								//###TUNE
		_aPenisSlices = new CPenisSlice[nSlices];

		Vector3 vecPenisStart	= _oBoneRootT.position;
		Vector3 vecPenisEnd		= _oCumEmitterT.transform.position;
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

		//=== Assign each penis particle bones to the appropriate slice (determined by distance from base toward tip) ===
		for (int nArrayIndex = 0; nArrayIndex < _aFlatMapBoneIdToShapeId.Count; nArrayIndex += 2) {
			ushort nBoneID	= _aFlatMapBoneIdToShapeId[nArrayIndex + 0];    //# Serializable array storing what shapeID each bone has.  Flat map is a simple list of <Bone1>, <Shape1>, <Bone2>, <Shape2>, etc.
			ushort nShapeID = _aFlatMapBoneIdToShapeId[nArrayIndex + 1];
			ushort nParticleID = _mapShapesToParticles[nShapeID];
			Transform oBoneParticleT	= _mapBonesDynamic[nBoneID];		// Obtain reference to the particle / bone and its parent (fake shape 'bone').  We set the shape 'bone' the position / rotation of the corresponding Flex shape knowing that the presentation mesh will be updated only from the real particle bone.
			float nPosBoneShapeZ = oBoneParticleT.position.z;
			int nSlice = (int)((nPosBoneShapeZ - vecPenisStart.z) / nLenPenisSlice);
			nSlice = Mathf.Clamp(nSlice, 0, nSlices - 1);
			_aPenisSlices[nSlice].AddChildBone(oBoneParticleT, nParticleID);
		}

        ShapeDef_SetStiffness(1);       //#DEV26:
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


	public override void OnPropChanged(CObj oObj) {               // This *must* be called within context of Flex's 'PreContainerUpdate()'
		base.OnPropChanged(oObj);
		switch (oObj._sName) {
			case "Erection":
				ShapeDef_SetStiffness(oObj._nValue);
				break;

			case "Size":
				ShapeDef_Enter();       // Scale the entire penis by changing the local scale of the root slice and 'baking'
				_aPenisSlices[0]._oSliceT.localScale = new Vector3(oObj._nValue, oObj._nValue, oObj._nValue);
				ShapeDef_Leave();
				break;

			case "UpDown":
			case "LeftRight":       //###BROKEN?
				Util_AdjustPenisSliceRotation(1, _aPenisSlices.Length-1);
				break;

			//case (int)ESoftBodyPenis.ShaftUpDown:
			//case (int)ESoftBodyPenis.ShaftLeftRight:
			//case (int)ESoftBodyPenis.ShaftTwistLeftRight:
			//	Util_AdjustPenisSliceRotation(2, _aPenisSlices.Length-1);
			//	break;

			case "Reset":
				if (oObj._nValue != 0) {
					for (int nParticle = 0; nParticle < _nParticles; nParticle++) {
						_oFlexParticles.m_particles[nParticle].pos = _oFlexParticles.m_restParticles[nParticle].pos;
						_oFlexParticles.m_particles[nParticle].invMass = 0;
					}
				}
				break;

			case "Kinematic":
				for (int nParticle = 0; nParticle < _nParticles; nParticle++) {
					int nParticleType = _aParticleInfo[nParticle] & C_ParticleInfo_Mask_Type;
					if ((nParticleType & C_ParticleInfo_BitTest_IsSimulated) != 0)
						_oFlexParticles.m_particles[nParticle].invMass = (oObj._nValue != 0) ? 0 : 1;
				}
				break;

			case "Cum":
				CGame.INSTANCE.CumControl_Start();
				break;

            //case "TestCenterCurve":
            //    PenisCenterCurve_Get3dPosAtLength(oObj._nValue);
            //    break;
		}
	}
    public Vector3 PenisCenterCurve_Get3dPosAtLength(float nLenRatio) {
        // Returns where in 3D space where along the line running from base to tip at the 'nLenRatio' position.
        // (e.g. nLenRatio = 0 returns at base, 1 returns at tip, 0.5 at mid-shaft, etc)
        // Used to guide hand to 'masturbate' penis regardless of curvature or size.  Not mathematically precise but good enoug
        nLenRatio = Mathf.Clamp(nLenRatio, 0, 0.99f);                   // Remove 1 as code below would get out of bounds.
        float nSliceSplit = nLenRatio * (_aPenisSlices.Length - 2) + 1;     //###WEAK: Ignore first slice.  We need the shaft.  ###IMPROVE: Find exact ratio?
        int nSlice1 = (int)nSliceSplit;
        int nSlice2 = nSlice1 + 1;
        float nSliceRemains = nSliceSplit - nSlice1;
        CPenisSlice oSlice1 = _aPenisSlices[nSlice1];
        CPenisSlice oSlice2 = _aPenisSlices[nSlice2];
        Vector3 vecSlice1 = oSlice1.CalcApproxSliceCenter();
        Vector3 vecSlice2 = oSlice2.CalcApproxSliceCenter();
        GameObject.Find("DEV_Cock1").transform.position = vecSlice1;
        GameObject.Find("DEV_Cock2").transform.position = vecSlice2;
        Vector3 vecPosAtRatio = (nSliceRemains * vecSlice2) + ((1.0f - nSliceRemains) * vecSlice1);     // Interpolate between the two slices by the remains ratio
        //Transform oMarker = CUtility.InstantiatePrefab<Transform>("Prefabs/MarkerS", $"PenisCenterCurve-{nLenRatio}", CGame.INSTANCE.transform);
        //oMarker.position = vecPosAtRatio;
        //oMarker.GetComponent<MeshRenderer>().enabled = true;
        //CGame.INSTANCE.GetBodyBase(0)._oActor_ArmL.transform.position = vecPosAtRatio;      //###DEV27: ###TEMP: To test placing hand on cock (need spring)
        return vecPosAtRatio;
    }

	void Util_AdjustPenisSliceRotation(int nSliceStart, int nSliceEnd) {
		Quaternion quatRot = Quaternion.Euler(_oObj.Get("Up/Down"), _oObj.Get("Left/Right"), 0);
		ShapeDef_Enter();
		for (int nSlice = nSliceStart; nSlice < nSliceEnd; nSlice++)
			_aPenisSlices[nSlice]._oSliceT.localRotation = quatRot;
		ShapeDef_Leave();
	}

	public class CPenisSliceBone {

		public CPenisSlice  _oPenisSlice;
		public Transform    _oBoneParticleT;            //###DESIGN: No longer required now that we directly modify rest particles... but can be useful later??
		public Transform    _oBoneShapeDefT;
		public Transform    _oMarkerT;
		Vector3             _vecPosBackupG;
		Quaternion          _quatRotBackupG;
		public int          _nParticleID;

		public CPenisSliceBone(CPenisSlice oPenisSlice, int nParticleID, Transform oParticleBoneT) {
			_oPenisSlice        = oPenisSlice;
			_nParticleID        = nParticleID;
			_oBoneParticleT     = oParticleBoneT;
		    _oBoneShapeDefT = new GameObject().transform;
			_oBoneShapeDefT.name = _oBoneParticleT.name + "-ShapeDef";
			_oBoneShapeDefT.position    = _oBoneParticleT.position;
			_oBoneShapeDefT.rotation    = _oBoneParticleT.rotation;
			_oBoneShapeDefT.localScale  = _oBoneParticleT.localScale;
			_oBoneShapeDefT.SetParent(_oPenisSlice._oSliceT);
			if (CPenisSlice.C_DEBUG_CreateMarkers) {
                _oMarkerT = CUtility.InstantiatePrefab<Transform>("Prefabs/MarkerS", "PenisSliceBone-" + nParticleID.ToString(), _oBoneParticleT);
				_oMarkerT.GetComponent<MeshRenderer>().material.color = Color.blue;
			}
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
		public Transform                _oMarkerT;
		public List<CPenisSliceBone>    _aSliceBones = new List<CPenisSliceBone>();
		public static bool              C_DEBUG_CreateMarkers = false;          //###NOTE: Use this debug technique throughout the codebase!
        public CPenisSliceBone          _oSliceBoneL;            // Leftmost, rightmost particles.  Used to find the approximate 'center' in CalcApproxSliceCenter() (needed to determine the 'PenisCenterCurve' regardless of penis run-time bend
        public CPenisSliceBone          _oSliceBoneR;


		public CPenisSlice(CSoftBody_Penis oSoftBodyPenis, int nSliceID, Vector3 vecPos) {
			_oSoftBodyPenis = oSoftBodyPenis;
			_nSliceID = nSliceID;
			_oSliceT = new GameObject().transform;
			_oSliceT.position = vecPos;
            _oSliceT.rotation = Quaternion.identity;
            _oSliceT.name = "+PenisSlice-" + _nSliceID.ToString();
			if (CPenisSlice.C_DEBUG_CreateMarkers) {
                _oMarkerT = CUtility.InstantiatePrefab<Transform>("Prefabs/MarkerS", "PenisSlice" + _nSliceID.ToString(), _oSliceT);
				_oMarkerT.GetComponent<MeshRenderer>().material.color = Color.cyan;
            }
		}

		public void AddChildBone(Transform oParticleBoneT, int nParticleID) {
            CPenisSliceBone oPenisSliceBone = new CPenisSliceBone(this, nParticleID, oParticleBoneT);
			_aSliceBones.Add(oPenisSliceBone);
            if (_oSliceBoneL == null) {
                _oSliceBoneL = oPenisSliceBone;
            } else {
                if (_oSliceBoneL._oBoneParticleT.position.x > oPenisSliceBone._oBoneParticleT.position.x)
                    _oSliceBoneL = oPenisSliceBone;
            }
            if (_oSliceBoneR == null) {
                _oSliceBoneR = oPenisSliceBone;
            } else {
                if (_oSliceBoneR._oBoneParticleT.position.x < oPenisSliceBone._oBoneParticleT.position.x)
                    _oSliceBoneR = oPenisSliceBone;
            }
		}

		public void ShapeDef_Enter() {
			foreach (CPenisSliceBone oSliceBone in _aSliceBones)
				oSliceBone.ShapeDef_Enter();
		}

		public void ShapeDef_Leave() {
			foreach (CPenisSliceBone oSliceBone in _aSliceBones)
				oSliceBone.ShapeDef_Leave();
		}

        public Vector3 CalcApproxSliceCenter() {
            // Return the approximate run-time center of this penis slice as calculated by the midpoint of the left-most and right-most particle (determined at init time)
            return (_oSliceBoneL._oBoneParticleT.position + _oSliceBoneR._oBoneParticleT.position) / 2;
        }
	}
};



//_oObj.Add(ESoftBodyPenis.Transparency,		"Transparency",			0.0f, 0.0f, 100.0f, "");
//_oObj.Add(ESoftBodyPenis.TransparencyBody_HACK,	"Body Transparency",	0.0f, 0.0f, 100.0f, "");		//###MOVE
			//case (int)ESoftBodyPenis.Transparency:
			//	_oBody.Util_AdjustMaterialTransparency("Penis", oObj._nValue / 100, false);
			//	break;
			//case (int)ESoftBodyPenis.TransparencyBody_HACK:
			//	_oBody.Util_AdjustMaterialTransparency("Penis", oObj._nValue / 100, true);
			//	break;

