/*###DISCUSSION: CBone
=== LAST ===

=== NEXT ===

=== TODO ===
- Port the old config joint driver to new code.
- Port the old pin system to new code
- Old code had actors defining proper limits!  No longer there!
	- Maybe a central generated code function is best / safest?

=== LATER ===
- Why the heck does min/max interchange seem to work???
- Check L/R works at every bone
- Verify startup rotation when D6 really Unity quaternion?  no startup angle?
- Revise the kinematic / simulated game modes.  Related to softbody teleport as well?

=== IMPROVE ===

=== NEEDS ===
- Need to automatically configure its D6 configurable joint to parent
- Need to add facilities later on to guild Flex softbody particles (e.g. finger bone guiding finger particles, etc)

=== DESIGN ===

=== IDEAS ===
- Could CBone be of any help for 'fake bones' such as Vagina triangulation pins, etc?

=== LEARNED ===

=== PROBLEMS ===

=== QUESTIONS ===
- Do we still need to drive arm bones to 'suggest poses' or can we find way to make pin do all the work (perhaps taking longer route?)

=== WISHLIST ===
- Have one bone rig for both men and women!
	- Then how do we manage different bone chains? (e.g. penis and vagina)
*/

using UnityEngine;
using System.Collections.Generic;
using System.IO;



public class CBone : MonoBehaviour, CVrWand.IVrWandMoveable {
	static Quaternion C_quatRotBlender2Unity = Quaternion.Euler(-90, 0, 0);             // Quaternion to convert from Blender-domain to Unity domain (so we have our usual Y+ up, Z- Forward from Blender's Z+ up, Y- forward)

						public CBoneRot[]	_aBoneRots;						// Our collection of three-possible rotations where 0 = X, 1 = Y, 2 = Z.  Always of size three if a bone rotation is owned by this bone  (Created at gametime)
	                    public string[]		_aBoneRots_Serialized;			// Serialized form of what is expanded in _aBoneRots.  Stored during static creation of CBone objects during Blender update of bones
						public string		_sRotOrder;						// DAZ-provided order to apply rotation for DAZ pose loading (looks like 'XYZ', 'ZXY', 'YZX', etc)  Essential to apply DAZ poses as DAZ intended but of no consequence to our gameplay as bones are moved via PhysX D6 joints.  We parse through the order of this string at every rotation to apply them in the proper order

    [HideInInspector]	public CActor				_oActor;				// The actor that owns us
	[HideInInspector]	public CBone				_oBoneParent;			// Our parent bone.  Has a 1:1 relationship with the actual transform bone objects we wrap	[HideInInspector] public Rigidbody			_oRigidBody;                        // Our rigid body (also a component of our same game object)
	[HideInInspector]	public ConfigurableJoint	_oJointD6;              // Our D6 configurable joint.  Responsibly for PhysX processing to keep our two bone extremities at their proper rotation
    [HideInInspector]	public Rigidbody			_oRigidBody;            // Our rigid body for this bone.  Essential for PhysX simulation
						public Vector3				_vecStartingPos;		// Pose and rotation stored so we can return to 'configure' game mode at any time
						public Quaternion 			_quatBoneRotationStartup;// The bone rotation at game startup.  Used to rotate from that starting position at each rotation for precision
	                                                                        //###CLEANUP21: All these still needed??
						public float				_X, _Y, _Z;             // Our current raw rotation in degrees.  Fed directly into joint
    [HideInInspector]	public float				_X2, _Y2, _Z2;			// 'Old' version of rotation.
						public float				_XP, _YP, _ZP;          // Our 'percentage' version of the _X, _Y, _Z rotation.  Used for bone debugging only
    [HideInInspector]	public float				_XP2, _YP2, _ZP2;       // 'Old' version of bone percentage rotation.  Used for bone debugging
						public float				_XL, _XH, _YHL, _ZHL;   // Our configuration parameters.  X = main bone bend (has a low and high), Y = twist, Z = 'side-to-side' bend (Y&Z only have Low/High combined)
						public float				_nJointSlerpSpringMult;	// How strongly our configurable joint is driven (as related to the global setting)
                        public EBoneType            _eBoneType;             // The 'bone type' grouping this bone into a 'stiffness setting'  From 'bender' to 'stiff' to 'twister', etc
                        public float                _nMass;                 // Mass of the rigid body

    [HideInInspector]	public  CTrigger            _oTrigger;              // Optional trigger child that creates a small trigger collider sphere close to this bone to 'actuate it' when a collider enters trigger.  (Used for finger to 'grasp objects' entering trigger range)

	

	public static CBone Connect(CActor oActor, CBone oBoneParent, string sNameBone, float nMass, EBoneType eBoneType = EBoneType.Default) {        // Note: Bone has no PhysX components if nMass is 0
        Transform oBoneT;
        if (oBoneParent == null)
            oBoneT = CUtility.FindChild(oActor._oBodyBase._oBoneRootT, sNameBone);           // Finding bone when root is different.  ###IMPROVE: Can be simplified to just always top bone?  (e.g. Why does 'Bones' have a single top bone 'chestUpper' when the could be merged?)
        else
            oBoneT = CUtility.FindChild(oBoneParent.transform, sNameBone);

        if (oBoneT == null)
            CUtility.ThrowExceptionF("CBone.Connect() cannot find bone '{0}'", sNameBone);
		CBone oBone = oBoneT.GetComponent<CBone>();
        oBone.Initialize(oActor, oBoneParent, nMass, eBoneType);
        return oBone;
	}

	float Util_GetLimit(char chAxis, bool bNegative) {          // Bone utility function to facilitate obtaining a quality bone rotation limit on the requested axis.
		//if (_aBoneRots == null)			// Zero bone rotations = automatic 0 limit
		//	return 0;
		CBoneRot oBoneRot = _aBoneRots[chAxis - 'X'];
		if (oBoneRot == null)			// No bone rotation on this axis = 0 limit
			return 0;
		if (chAxis == 'X') {            // X-axis can have a different negative and positive value on the D6 joints we use.  Return the requested one.
			return bNegative ? oBoneRot._nMin : oBoneRot._nMax;
		} else {						// Y and Z axis must have the same value for min and max.  Calculate the average of this value here.
			return (Mathf.Abs(oBoneRot._nMin) + Mathf.Abs(oBoneRot._nMax)) / 2;		//###CHECK21: Returning the average of min / max...  Should return greater value, lesser value??
		}
	}

	//public void SetBoneRotationAngle(Vector3 eulRotIn) {
	//	// Set our value from element from 'quatRot' that is pertinent to this bone rotation.  Mapping of D6 X/Y/Z to Unity transform's X,Y,Z changes from bone to bone preventing a direct angle setting of all bone rotations in one command
	//	//Vector3 eulRotOut = new Vector3(eulRotIn.x, eulRotIn.y, eulRotIn.z);
	//	Vector3 eulRotOut = new Vector3();
	//	foreach (CBoneRot oBoneRot in _aBoneRots)
	//		if (oBoneRot != null)
	//			oBoneRot.SetBoneRotationAngle(eulRotIn, ref eulRotOut);

	//	_oJoint.targetRotation = Quaternion.Euler(eulRotIn);
	//}

	public void BakeCurrentBoneRotationIntoJointRotation() {
        if (_oJointD6 == null)        //###WEAK?
            return;
		Quaternion quatRotRelativeToStartup = Quaternion.Inverse(transform.localRotation) * _quatBoneRotationStartup;       //###INFO: How to calculate the relative rotation
		_oJointD6.targetRotation = quatRotRelativeToStartup;		//###Apply the change in local rotation (since init state) as the target of the D6 joint rotation.  This has the effect of 'baking' the current bend into the bone rotation
	}


	public void Initialize(CActor oActor, CBone oBoneParent, float nMass, EBoneType eBoneType = EBoneType.Default) {       // Note: Bone has no PhysX components if nMass is 0
        _oActor = oActor;
		_oBoneParent = oBoneParent;
        //_nJointSlerpSpringMult = nDriveStrengthMult;
        _nMass = nMass;
        _eBoneType = eBoneType;

        //=== Create our CBoneRot objects from their 'serialized format' (stuffed by Blender into prefab during design-time import procedure) ===
        _aBoneRots = new CBoneRot[3];
		for (int nRotation = 0; nRotation < _aBoneRots_Serialized.Length; nRotation++) {
			string sRotationSerialization = _aBoneRots_Serialized[nRotation];
			if (sRotationSerialization != null && sRotationSerialization.Length > 0) { 
				CBoneRot oBoneRot = new CBoneRot(this, sRotationSerialization);
				_aBoneRots[oBoneRot._nAxis] = oBoneRot;
			}
		}

        //=== Create an debug bone visualizer mesh (to visually show the axis rotations) ===
        //CUtility.InstantiatePrefab<MeshRenderer>("Gizmo/Gizmo-Rotate-Unity", gameObject.name + "-Vis", transform);        //###DEBUG21: Add visual gizmos at bone positions to fully show position / orientation

        //=== Extract the DAZ-provided bone limits ===
        _vecStartingPos             = transform.localPosition;
        _quatBoneRotationStartup	= transform.localRotation;
		_XL  = Util_GetLimit('X', true);		//###DESIGN21: Keep these variables?  Go straight to CBoneRot everytime??
		_XH  = Util_GetLimit('X', false);		//###CHECK21: Some bones (hip) have 180 limits while D6 limit appears to be 177.  Problem??
		_YHL = Util_GetLimit('Y', false);
		_ZHL = Util_GetLimit('Z', false);
        if (_XL > _XH)
            CUtility.ThrowExceptionF("###EXCEPTION in CBone('{0}').  Invalid min {1} and max {2}", name, _XL, _XH);

        //=== Create the rigid body for our bone ===
        if (_nMass > 0) { 
            _oRigidBody = (Rigidbody)CUtility.FindOrCreateComponent(gameObject, typeof(Rigidbody));     //###TODO: Add a "CRigidBodyWake"???
            _oRigidBody.mass = _nMass;
            _oRigidBody.drag = 0;                               // Both these SUPER important settings are pushed in by CGame at startup.
            _oRigidBody.angularDrag = 0;
            _oRigidBody.sleepThreshold = 0;                     // Ensure the rigid body never sleeps!
		    _oRigidBody.isKinematic = false;					// We are NEVER kinematic.  EVERY bone is PhysX-driven!  (Including root bone 'hip')
            _oRigidBody.useGravity = _oActor.GetType().IsSubclassOf(typeof(CActorLimb));        // Only the limbs have gravity turned on.  Torso bones needlessly weight the pins down


            //=== Process special handling needed when we are root (we are kinematic and we have no joint to parent) ===
            if (_oBoneParent == null) {

                //###NOTE: If we have no parent then we are the root bone (hip).  While we have no outgoing joint we will have two D6 joints pointing to us (pelvis and abdomenLower) forming an uninterupted PhysX bone chain).  So nothing to do...

            } else {

                //=== Create the D6 configurable joint between our parent and us ===
		        _oJointD6 = (ConfigurableJoint)CUtility.FindOrCreateComponent(gameObject, typeof(ConfigurableJoint));		//###TODO: Add a "CRigidBodyWake"???
			    //_oJoint.autoConfigureConnectedAnchor = false;
			    //_oJoint.connectedAnchor = new Vector3();
			    _oJointD6.connectedBody = _oBoneParent._oRigidBody;

			    //=== Set joint axis defaults (before overriding some of them) ===
			    _oJointD6.xMotion = _oJointD6.yMotion = _oJointD6.zMotion = ConfigurableJointMotion.Locked;							// Bone joints *never* move from their axis point... they only rotate!
		        _oJointD6.angularXMotion = _oJointD6.angularYMotion = _oJointD6.angularZMotion = ConfigurableJointMotion.Limited;   // Angular motions are limited unless they have zero limits in which case we lock them

                //=== Lock the axis that cannot move ===
                if (_XL == 0f && _XH == 0f) _oJointD6.angularXMotion = ConfigurableJointMotion.Locked;		// If an axis is unused set it //free to reduce PhysX workload  ###CHECK: Is this ever invoked?  Does it make joint fail if not all three axis driven??
		        if (_YHL == 0f)             _oJointD6.angularYMotion = ConfigurableJointMotion.Locked;		//###DESIGN: Verify unsetting!
		        if (_ZHL == 0f)             _oJointD6.angularZMotion = ConfigurableJointMotion.Locked;		//###NOTE: SLERP needs all three axis by definition... But Limited of little / no use if we drive all the time (less PhysX overhead))

                //=== Set the joint limits as per our arguments ===
                SoftJointLimit oJL = new SoftJointLimit();              //###IMPROVE: Has other fields that could be of use?
			    oJL.bounciness = 0;					//###INFO: "When the joint hits the limit, it can be made to bounce off it. Bounciness determines how much to bounce off an limit. range { 0, 1 }."
			    oJL.contactDistance = 0;			//###IMPROVE: Make relative to angle! //###INFO: "Determines how far ahead in space the solver can "see" the joint limit" (in degrees) See https://docs.unity3d.com/510/Documentation/ScriptReference/SoftJointLimit-contactDistance.html
		        oJL.limit = _XL;	_oJointD6. lowAngularXLimit = oJL;        // X is the high-functionality axis with separately-defined Xmin and Xmax... Y and Z only have a +/- range around zero, so we are forced to raise the lower half to match the other side
                oJL.limit = _XH;    _oJointD6.highAngularXLimit = oJL;        //###INFO: Check out http://quaternions.online/ for visualization of quaternions!
                oJL.limit = _YHL;	_oJointD6.    angularYLimit = oJL;        //###NOTE! Hugely inconvenient feature of D6 joint is Y & Z must be symmetrical!!  Make sure bone is oriented so X is used for the assymetrical rotation!!
		        oJL.limit = _ZHL;	_oJointD6.    angularZLimit = oJL;

                //=== Add soft springs to the 'Limited' bone rotations.  Helps to stabilize the bone rig by giving a little extra angular room for conversion (instead of rigid stops that can causes bouncing)
                //SoftJointLimitSpring oLimitSpring = new SoftJointLimitSpring();
                //oLimitSpring.spring = 1000000;            //###TUNE #DEV26: Try again??
                //oLimitSpring.damper = 100;
                //if (_oJointD6.angularXMotion == ConfigurableJointMotion.Limited)
                //    _oJointD6.angularXLimitSpring  = oLimitSpring;
                //if (_oJointD6.angularYMotion == ConfigurableJointMotion.Limited || _oJointD6.angularZMotion == ConfigurableJointMotion.Limited)
                //    _oJointD6.angularYZLimitSpring = oLimitSpring;

                //=== Set the configurable joint drive strength ===
                JointDrive oDrive = new JointDrive();
		        oDrive.positionSpring = 0;                  // Both of these SUPER important settings are pushed in by CGame at startup.
                oDrive.positionDamper = 0;                          
                oDrive.maximumForce = float.MaxValue;               //###IMPROVE: Some reasonable force to prevent explosions??
		        _oJointD6.rotationDriveMode = RotationDriveMode.Slerp;        // Slerp is really the only useful option for bone driving.  (Many other features of D6 joint!!!)
		        _oJointD6.slerpDrive = oDrive;
            }
        }

        //=== If we're a node on the right side, copy the collider defined on our twin node on the left side ===
        if (_oActor._eBodySide == EBodySide.Right) {
			Transform oNodeSrc = CUtility.FindSymmetricalBodyNode(transform.gameObject);
			gameObject.layer = oNodeSrc.gameObject.layer;				// Give this side of the body the same collider layer as the 'source side'
            Collider oColBaseSrc = oNodeSrc.GetComponent<Collider>();
            //oColBaseSrc.material = CGame._oPhysMat_Friction_Lowest;      //###DESIGN: Default PhysX material??

			if (oColBaseSrc != null) {
				if (oColBaseSrc.GetType() == typeof(CapsuleCollider)) {
					CapsuleCollider oColSrc = (CapsuleCollider)oColBaseSrc;
					CapsuleCollider oColDst = (CapsuleCollider)CUtility.FindOrCreateComponent(transform, typeof(CapsuleCollider));
					oColDst.center = new Vector3(-oColSrc.center.x, oColSrc.center.y, oColSrc.center.z);        // Negate the X center
					oColDst.radius = oColSrc.radius;
					oColDst.height = oColSrc.height;
					oColDst.direction = oColSrc.direction;
                    //oColDst.material = CGame._oPhysMat_Friction_Lowest;
                } else if (oColBaseSrc.GetType() == typeof(SphereCollider)) {
                    SphereCollider oColSrc = (SphereCollider)oColBaseSrc;
                    SphereCollider oColDst = (SphereCollider)CUtility.FindOrCreateComponent(transform, typeof(SphereCollider));
                    oColDst.center = new Vector3(-oColSrc.center.x, oColSrc.center.y, oColSrc.center.z);        // Negate the X center
                    oColDst.radius = oColSrc.radius;
                    //oColDst.material = CGame._oPhysMat_Friction_Lowest;
				} else if (oColBaseSrc.GetType() == typeof(BoxCollider)) {
					BoxCollider oColSrc = (BoxCollider)oColBaseSrc;
					BoxCollider oColDst = (BoxCollider)CUtility.FindOrCreateComponent(transform, typeof(BoxCollider));
					oColDst.center = new Vector3(-oColSrc.center.x, oColSrc.center.y, oColSrc.center.z);        // Negate the X center
                    oColDst.size = oColSrc.size;
                    //oColDst.material = CGame._oPhysMat_Friction_Lowest;
                } else if (oColBaseSrc.GetType() == typeof(Collider)) {
					//###CHECK:??? Collider type = No Collider???
				} else {
					CUtility.ThrowExceptionF("###EXCEPTION: CBone.ctor() could not port collider of type '{0}' on bone '{1}'", oColBaseSrc.GetType().Name, gameObject.name);
				}
			//} else {
				//Debug.LogWarningFormat("#WARNING: CBone.Initialize() finds null collider on bone '{0}'", gameObject.name);		//###IMPROVE23: No collider on toe and metatarsals... fix
			}
        }
    }

    void Update() {             //#DEV26: Only in a debug build?
        if (_XP != _XP2) _XP = _XP2 = RotateX(_XP);
        if (_YP != _YP2) _YP = _YP2 = RotateY(_YP);
        if (_ZP != _ZP2) _ZP = _ZP2 = RotateZ(_ZP);
    }



	//---------------------------------------------------------------------------	PROPERTY HANDLERS

	//=== Rotation where source value goes from -100% to 100% ===
	public float RotateX(float nAnglePercent) {
        nAnglePercent = Mathf.Clamp(nAnglePercent, -100f, 100f);
        _X = _X2 = (nAnglePercent/100f) * ((nAnglePercent<0) ? -_XL : _XH);     //###DESIGN!!!: Not linear if low and high are not opposite... the desired behavior??
        UpdateRotation();
        return nAnglePercent;
    }       
	public float RotateY(float nAnglePercent) {
        nAnglePercent = Mathf.Clamp(nAnglePercent, -100f, 100f);
        _Y = _Y2 = (nAnglePercent/100f) * _YHL;
        UpdateRotation();
        return nAnglePercent;
    }
	public float RotateZ(float nAnglePercent) {
        nAnglePercent = Mathf.Clamp(nAnglePercent, -100f, 100f);
        _Z = _Z2 = (nAnglePercent/100f) * _ZHL;
        UpdateRotation();
        return nAnglePercent;
    }

    public void OnChangeGameMode(EGameModes eGameModeNew, EGameModes eGameModeOld) {        //#DEV26: ###OBS?
		// Joint becomes kinematic and reverts to starting position upon configure mode, becomes PhysX-simulated during gameplay
		switch (eGameModeNew) {
			case EGameModes.MorphBody:
                _X = _Y = _Z = 0;
                UpdateRotation();
                _oRigidBody.isKinematic = true;
				transform.localPosition = _vecStartingPos;				// Restore the joint to its startup position / orientation
				transform.localRotation = _quatBoneRotationStartup;
				break;
			case EGameModes.Play:
                //if (_oJointD6 != null) { 
                //    JointDrive oDrive = _oJointD6.slerpDrive;
                //    oDrive.positionSpring = _nJointSlerpSpringMult * CGame.BoneDriveStrength;   // Final spring strength is the global constant multiplied by the provided multiplier... makes it easy to adjust whole-body drive strength
                //    _oJointD6.slerpDrive = oDrive;
                //}
                _oRigidBody.isKinematic = false;
                _X = _Y = _Z = 0;
                UpdateRotation();
                break;                  //###IMPROVE: Add a new game mode for kinematic but 'reset pose to T'?
		}
	}

    public void DEV_ResetToStartupPosRot() {        //#DEV26:???
        transform.localPosition = _vecStartingPos;              // Restore the joint to its startup position / orientation
        transform.localRotation = _quatBoneRotationStartup;
    }

    void UpdateRotation() {     // Update X, Y, Z rotation			//###TODO21:!!!!!
        Quaternion quatRot = Quaternion.Euler(_X, _Y, _Z);
        if (_oJointD6 != null) {
            _oJointD6.targetRotation = quatRot;      //###INFO: Unity's Eulers *always* rotate in z, x, y in that order  (Blender can have all 6 possible Euler permutations)
        } else {            // If we have no joint then we have no rigid body either... so we just set our localRotation straight up.
            transform.localRotation = _quatBoneRotationStartup * quatRot;
        }
    }


	public void DeserializeFromBlenderStaticBoneImportProcedure(ref CByteArray oBA) { 
		// Ship-time utility function statically invoked by CGameEd to update our bone information from Blender
        Vector3 vecBone  = oBA.ReadVector();              // Bone position itself.

		//=== Read the bone angle as an angle-axis in order to easily traverse Blender-domain to Unity-domain ===
		Vector3 vecRotAxis = oBA.ReadVector();
		float nRotAngle = oBA.ReadFloat();
		Quaternion quatBone = Quaternion.AngleAxis(nRotAngle * 180.0f / Mathf.PI, vecRotAxis);	// Blender sends radians and Unity needs degrees!
		Quaternion quatBoneUnity = quatBone * C_quatRotBlender2Unity;							//###NOTE: Apply the Blender-to-Unity global rotation right off the top so we have our usual Y+ up, Z- Forward from Blender's Z+ up, Y- forward

        //Debug.LogFormat("CBone created bone '{0}' under '{1}' with rot:{2:F3},{3:F3},{4:F3},{5:F3} / pos:{6:F3},{7:F3},{8:F3}", gameObject.name, transform.parent.name, quatBoneUnity.x, quatBoneUnity.y, quatBoneUnity.z, quatBoneUnity.w, vecBone.x, vecBone.y, vecBone.z);
        transform.position = vecBone;
        transform.rotation = quatBoneUnity;

		_sRotOrder = oBA.ReadString();

		byte nRotations = oBA.ReadByte();
		if (nRotations > 0) 
			_aBoneRots_Serialized = new string[3];               // We always have this array of size three (0 = X, 1 = Y, 2 = Z) if we have any rotation (have to go in right slot)

		for (int nRotation = 0; nRotation < nRotations; nRotation++) {
			string sRotationSerialization = oBA.ReadString();
			_aBoneRots_Serialized[nRotation] = sRotationSerialization;
		}
	}


	//---------------------------------------------------------------------------	LOAD / SAVE

	public void Load(BinaryReader oBR) {
        if (_oJointD6 == null)            //#DEV26: how come??
            return;     
        //#DEV27: We need to save both fucking angles as they are NOT ALWAYS compatible!!  (Some axis rotation on some of them)  WHY??
        CGame.INSTANCE.s_quatUtility = CUtility.LoadBinary_QuaternionAsEuler(oBR);
        transform.localRotation = CGame.INSTANCE.s_quatUtility;      // Set both the bone angle and the joint angle right away.
        CGame.INSTANCE.s_quatUtility = CUtility.LoadBinary_QuaternionAsEuler(oBR);
        _oJointD6.targetRotation = CGame.INSTANCE.s_quatUtility;     // (Greatly helps pose loading snap to proper position right away without explosion!)
    }

    public void Save(BinaryWriter oBW) {
        if (_oJointD6 == null)            //#DEV26: how come??
            return;
        CUtility.SaveBinary_QuaternionAsEuler(oBW, transform.localRotation);
        CUtility.SaveBinary_QuaternionAsEuler(oBW, _oJointD6.targetRotation);
    }



    //======================================================================	STATIC BONE UPDATERS

    public static void BoneUpdate_UpdateFromBlender(string sSex) {                      // Update our body's bones from the current Blender structure... Launched at edit-time by our helper class CBodyEd
        //StartBlender();
		//###BROKEN: Now part of CBody but we don't have that instance?
  //      GameObject oResourcesGO = GameObject.Find("Resources");
  //      GameObject oPrefabGO = CUtility.FindObject(oResourcesGO, "Prefab" + sSex + "A");
  //      oPrefabGO.SetActive(true);
		//Transform oBoneRootT = CUtility.FindNodeByName(oPrefabGO.transform, "Bones");
		//Dictionary<string, CBone> mapBonesFlattened = new Dictionary<string, CBone>();		// Flattened collection of bones extracted from Blender.  Includes dynamic bones.
  //      string sNameBodySrc = sSex + "A";        // Remove 'Prefab' to obtain Blender body source name (a bit weak)
		//CBone.BoneUpdate_UpdateFromBlender(sNameBodySrc, oBoneRootT, ref mapBonesFlattened);
	}

	public static void BoneUpdate_UpdateFromBlender(string sBlenderAccessString, Transform oBoneRootT, ref Dictionary<string, CBone> mapBonesFlattened) {
		// Called during CBody creation to de-serialize *all* Blender bones on a body (including dynamic ones) and to set / update their position / orientation

		CByteArray oBA = new CByteArray("'CBody'", sBlenderAccessString + ".Unity_GetBones()");

		//=== Read the recursive bone tree.  The mesh is still based on our bone structure which remains authoritative but we need to map the bone IDs from Blender to Unity! ===
		CBone.BoneUpdate_ReadBone_RECURSIVE(ref oBA, oBoneRootT);		//###IMPROVE? Could define bones in Unity from what they are in Blender?  (Big design decision as we have lots of extra stuff on Unity bones!!!)

		oBA.CheckMagicNumber_End();				// Read the 'end magic number' that always follows a stream.

		Debug.Log("+++ BoneUpdate_UpdateFromBlender() OK +++");
	}

	public static void BoneUpdate_ReadBone_RECURSIVE(ref CByteArray oBA, Transform oBoneParent) {                          // Precise opposite of gBlender.Stream_SendBone(): Reads a bone from Blender, finds (or create) equivalent Unity bone and updates position
		string sBoneName = oBA.ReadString();

		Transform oBoneT = oBoneParent.Find(sBoneName);
		if (oBoneT == null) {
			//Debug.LogFormat("- BoneUpdate_ReadBone() creating new bone '{0}' on parent '{1}'", sBoneName, oBoneParent.name);
			oBoneT = new GameObject(sBoneName).transform;
			oBoneT.parent = oBoneParent;
		}

		CBone oBone = CUtility.FindOrCreateComponent(oBoneT.gameObject, typeof(CBone)) as CBone;
		oBone.DeserializeFromBlenderStaticBoneImportProcedure(ref oBA);

		int nBoneChildren = oBA.ReadUShort();
		for (int nBoneChild = 0; nBoneChild < nBoneChildren; nBoneChild++)
			CBone.BoneUpdate_ReadBone_RECURSIVE(ref oBA, oBoneT);
	}



    public void CreateTriggerChild(float nSize, Vector3 vecOffset) {
        _oTrigger = CTrigger.Create(this, nSize, vecOffset);
    }

    public void OnChildTriggerEnter(CTrigger oTrigger, Collider oCol) {     //@#
        float nRot = 40;
        if (_oActor._eBodySide == EBodySide.Left)           //#DEV26: ###HACK:!!!
            nRot = -nRot;
        RotateX(nRot);
    }
    public void OnChildTriggerExit(CTrigger oTrigger, Collider oCol) {
        RotateX(0);
    }



    //---------------------------------------------------------------------------	VR WAND MOVEMENT

    public virtual Transform VrWandMove_Begin(CVrWand oVrWand, bool bStartAction1, bool bStartAction2) {
        return transform;
    }
    public virtual void VrWandMove_End(CVrWand oVrWand) { }
    public virtual void VrWandMove_Update(CVrWand oVrWand) {
        //=== If object being moved is a bone the 'bake' the angles up the bone tree so the current shape 'takes' ===
        CBone oBoneIterator = this;
        if (oBoneIterator != null) {
            Transform oBoneIteratorT = oBoneIterator.transform;
            while (oBoneIterator != null) {         //#DEV26!!!!! Where to stop?  Anything downstream?
                oBoneIterator.BakeCurrentBoneRotationIntoJointRotation();
                oBoneIteratorT = oBoneIteratorT.parent;
                oBoneIterator = oBoneIteratorT.GetComponent<CBone>();
            }
        }
    }
    public virtual void VrWandMove_UpdatePositionAndRotation(Transform oNodeT) {
        transform.position = oNodeT.position;
        transform.rotation = oNodeT.rotation;
    }



    //---------------------------------------------------------------------------	MISC
    public enum EBoneType {
        Default,                // Bone is default with no particular distiction.
        ArmCollar,              // Bone is the arm collar.  This ones has its own type as it is particularly touchy to balance against gravity and still react property to arm pulls
        Bender,                 // Bone is a primary 'bender' that is meant to bend the most (e.g. elbow, knee, shoulder)
        Twister,                // Bone is a 'twister' such as forearm twist, arm twist, tight twist = fairly loose
        Extremity,              // Bone is an 'extremity' such as hand or feet = fairly soft so pin uses entire dynamic range movement without reaching limits too often
        Finger,                 // Bone is a finger
    }
}




public class CBoneRot {				//###MOVE21:??
	public CBone		_oBone;					// Our parent bone.  Manages / owns this bone rotation definition

	public char			_chAxis;				// Axis of rotation about our owning bone.  Must be 'X', 'Y' or 'Z'
	public char			_chAxisDAZ;				// Axis of rotation about our owning bone in the DAZ domain.  (Enables us to test proper bone angles by importing raw DAZ pose dumps that contain DAZ-domain bone rotation axis)
	public bool			_bAxisNegated;			// If true _chAxis is negated instead of positive
	public byte			_nAxis;					// 0 for X, 1 for Y, 2 for Z (Same info as _chAxis)
	public string		_sNameRotation;			// The human-friendly name of this bone rotation
	public float		_nMin;					// The min / max value this bone rotation can have.  Note that only bone rotations assigned to the 'X' D6 joint can have a different min/max!
	public float		_nMax;
	public float        _nValue;				//###DESIGN: Keep??

	public CBoneRot(CBone oBone, string sRotationSerialization) {		// Creates a bone rotation from a Blender-provided 'serialization string' statically stored in CBone
		_oBone = oBone;
		sRotationSerialization = "[" + sRotationSerialization + "]";			//###WEAK20: SplitCommaSeparatedPythonListOutput() expects a string wrapped by Python's '[' / ']'.  Wrap our csv string for it to work without clipping our parameters
		string[] aRotationsParams = CUtility.SplitDelimiterString_Python(sRotationSerialization);
		string sAxisAndDirection = aRotationsParams[0];				// Looks like 'X+', 'Z-', 'Y+', etc: the axis of rotation and wheter it is positive or negated
		_chAxis				= sAxisAndDirection[0];
		_bAxisNegated		= (sAxisAndDirection[1] == '-');	// Flags if axis is negated or not
		_nAxis				= (byte)(_chAxis - 'X');			// Break down X,Y,Z axis character into axis number (0 = X, 1 = Y, 2 = Z)
		_chAxisDAZ			= aRotationsParams[1][0];
		_sNameRotation		= aRotationsParams[2];
        _nMin				= float.Parse(aRotationsParams[3]);
        _nMax				= float.Parse(aRotationsParams[4]);
        if (_nAxis == 0 && _bAxisNegated == false) {                    //#DEV26: ###HACK:!!!!!! Horrible hack with Blender-to-Unity conversion.  Read https://answers.unity.com/questions/503407/need-to-convert-to-right-handed-coordinates.html or https://answers.unity.com/storage/temp/12048-lefthandedtorighthanded.pdf
            _nMin               = -float.Parse(aRotationsParams[4]);
            _nMax               = -float.Parse(aRotationsParams[3]);
        }
        if (_chAxis == 'Z')                                     //###WEAK: It appears from observation that all Z rotations are meant to go the other direction... Verify this!  Is this caused because Blender (& DAZ) are Right-handed while Unity is left-handed??
            _bAxisNegated = !_bAxisNegated;

        if (_nMin > _nMax)
            CUtility.ThrowExceptionF("###EXCEPTION: CBone '{0}' Rot '{1}' has a min of {2} and max of {3}", _oBone.name, _chAxis, _nMin, _nMax);

        ////_nMin				= float.Parse(aRotationsParams[3]);			//###HACK21:!!!!!!! Why the heck does min/max interchange seem to work???
        ////_nMax				= float.Parse(aRotationsParams[4]);
        //_nMin               = -float.Parse(aRotationsParams[4]);
        //_nMax               = -float.Parse(aRotationsParams[3]);
        //if (_chAxis == 'Z')                                     //###WEAK: It appears from observation that all Z rotations are meant to go the other direction... Verify this!  Is this caused because Blender (& DAZ) are Right-handed while Unity is left-handed??
        //    _bAxisNegated = !_bAxisNegated;
        //Debug.LogFormat("-CBoneRot {0}  '{1}'   {2} - {3} ", sAxisAndDirection, _sNameRotation, _nMin, _nMax);
    }

    //---------------------------------------------------------------------------	BONE ANGLE GET / SET

    public float BoneRotationAngle_Get() {			// Get the bone rotation angle (in degrees) straight from the owner's bone D6 joint
		Vector3 eulRot = _oBone._oJointD6.targetRotation.eulerAngles;
		switch (_chAxis) {
			case 'X':	return eulRot.x;
			case 'Y':	return eulRot.y;
			case 'Z':	return eulRot.z;
			default:	return -1;
		}
	}

    public void BoneRotationAngle_Set(float nBoneRotationAngle) {       // Set the bone rotation angle (in degrees) straight into the owner's bone D6 joint
        Vector3 eulRot = _oBone._oJointD6.targetRotation.eulerAngles;
        switch (_chAxis) {
            case 'X': eulRot.x = nBoneRotationAngle; break;
            case 'Y': eulRot.y = nBoneRotationAngle; break;
            case 'Z': eulRot.z = nBoneRotationAngle; break;
        }
        _oBone._oJointD6.targetRotation = Quaternion.Euler(eulRot);
    }
}


//public void SetBoneRotationAngle(Vector3 eulRotIn, ref Vector3 eulRotOut) {
//	// Set our value from element from 'quatRot' that is pertinent to this bone rotation.  Mapping of D6 X/Y/Z to Unity transform's X,Y,Z changes from bone to bone preventing a direct angle setting of all bone rotations in one command
//	switch (_chAxis) {
//		case 'X':	eulRotOut.x = _nValue = Mathf.Clamp(eulRotIn.x, _nMin, _nMax); break;
//		case 'Y':	eulRotOut.y = _nValue = Mathf.Clamp(eulRotIn.y, _nMin, _nMax); break;
//		case 'Z':	eulRotOut.z = _nValue = Mathf.Clamp(eulRotIn.z, _nMin, _nMax); break;
//	}
//}



//---------------------------------------------------------------------------	LOAD / SAVE
//public void Load(System.IO.StreamReader oStreamReader) {
//	string sLine = oStreamReader.ReadLine();
//	string[] aLineFields = sLine.Split('\t');           // Properties file is a very simple text file with 'value' + <TabCharacter> + 'name' format
//	string sPropValue = aLineFields[0];
//	string sName = aLineFields[1];
//	if (sName == _sNameRotation) {
//		float nPropValue = 0;
//		float.TryParse(sPropValue, out nPropValue);
//		BoneRotationAngle_Set(nPropValue);
//	} else {
//		Debug.LogWarningFormat("#Warning: CBone.Load() found bone rotation '{0}' but was expecting '{1}'", _sNameRotation, sName);      //###DESIGN: Keep this verbose entry in our files??
//	}
//}
//public void Save(System.IO.StreamWriter oStreamWrite) {
//	string sLine = string.Format("{0:F6}\t{1}", BoneRotationAngle_Get(), _sNameRotation);       //#DEV26:Keep name?
//	oStreamWrite.WriteLine(sLine);
//}
//foreach (CBoneRot oBoneRot in _aBoneRots) {
//	if (oBoneRot != null)
//		oBoneRot.Save(oStreamWrite);
//}


        //string sLine = oStreamReader.ReadLine();
        //string[] aLineFields = sLine.Split(',');           // Properties file is a very simple text file with x,y,z,name ordering
        //if (aLineFields[3] == transform.name) {
        //	Vector3 eulRot = new Vector3();
        //	float.TryParse(aLineFields[0], out eulRot.x);
        //	float.TryParse(aLineFields[1], out eulRot.y);
        //	float.TryParse(aLineFields[2], out eulRot.z);
        //	_oJoint.targetRotation = Quaternion.Euler(eulRot);
        //} else {
        //	CUtility.ThrowExceptionF("###EXCEPTION in CBone.Load().  Expected bone name '{0}' but found bone '{1}'", transform.name, aLineFields[3]);
        //}

        //Vector3 eulRot = _oJoint.targetRotation.eulerAn   gles;
        //string sLine = string.Format("{0:000.000},{1:000.000},{2:000.000},{3}", eulRot.x, eulRot.y, eulRot.z, transform.name);		//###INFO: How to pad with zero.  Use '#' instead to trim non-needed mantissa
        //oStreamWrite.WriteLine(sLine);
