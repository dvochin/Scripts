using UnityEngine;


public class CJointDriver : MonoBehaviour {         // CJointDriver: Encapsulates common usage of the important configurable joint used for ragdoll-style physics movement of body bones  ###MOVE? To own file?
    [HideInInspector] public CActor				_oActor;			                // The actor who owns us
	[HideInInspector] public CJointDriver		_oJointDrvParent;                   // The parent joint driver (and bone) we connect to
	[HideInInspector] public Rigidbody			_oRigidBody;                        // Our rigid body (also a component of our same game object)
	[HideInInspector] public ConfigurableJoint	_oConfJoint;                        // Our D6 configurable joint.  Responsibly for PhysX processing to keep our two bone extremities at their proper rotation
	                  Vector3					_vecStartingPos;					// Pose and rotation stored so we can return to 'configure' game mode at any time
	                  Quaternion 				_quatStartingRotation;              // The starting rotation.  Used to return joint to its starting position.
                      public float              _X, _Y, _Z;                         // Our current raw rotation in degrees.  Fed directly into joint
    [HideInInspector] public float              _X2, _Y2, _Z2;                      // 'Old' version of rotation.  Used to auto-update bone rotation in 'C_BoneDebugMode' debug mode 
                      public float              _XP, _YP, _ZP;                      // Our 'percentage' version of the _X, _Y, _Z rotation.  Used for bone debugging only
    [HideInInspector] public float              _XP2, _YP2, _ZP2;                   // 'Old' version of bone percentage rotation.  Used for bone debugging
                      public float              _XL, _XH, _YHL, _ZHL;               // Our configuration parameters.  X = main bone bend (has a low and high), Y = twist, Z = 'side-to-side' bend (Y&Z only have Low/High combined)
                      public float              _nDriveStrengthMult;

    public static CJointDriver Create(CActor oActor, CJointDriver oJointDrvParent, string sNameBone, float nDriveStrengthMult, float nMass, float XL, float XH, float YHL, float ZHL, int nFinalized=0) {
        Transform oTransform;
        if (oJointDrvParent == null)
            oTransform = CUtility.FindChild(oActor._oBody._oBodyBase._oBonesT, sNameBone);           // Finding bone when root is different.  ###IMPROVE: Can be simplified to just always top bone?  (e.g. Why does 'Bones' have a single top bone 'chestUpper' when the could be merged?)
        else
            oTransform = CUtility.FindChild(oJointDrvParent.transform, sNameBone);

        if (oTransform == null)
            CUtility.ThrowException("CJointDriver.Create() cannot find bone " + sNameBone);
        CJointDriver oJointDriver = CUtility.FindOrCreateComponent(oTransform.gameObject, typeof(CJointDriver)) as CJointDriver;
        oJointDriver.Initialize(oActor, oJointDrvParent, nDriveStrengthMult, nMass, XL, XH, YHL, ZHL, nFinalized!=0);
        return oJointDriver;
    }


    public void Initialize(CActor oActor, CJointDriver oJointDrvParent, float nDriveStrengthMult, float nMass, float XL, float XH, float YHL, float ZHL, bool bFinalized) {
		_oActor = oActor;
		_oJointDrvParent = oJointDrvParent;
        _nDriveStrengthMult = nDriveStrengthMult;
        _vecStartingPos         = transform.localPosition;
        _quatStartingRotation   = transform.localRotation;
		if (CGame.INSTANCE.BoneDebugMode && bFinalized == false) {			// Set debug limits if in debug mode (so we can fully rotate along all axis for game-time tuning) unless bone is 'finalized' in which do apply the supplied limits
			_XH = _YHL = _ZHL = 177f;				// Set limit angles to maximum value in bone debug mode (so runtime bone debugger can move all bones and all axis all the way)
			_XL = -_XH;
		} else {
			_XL = XL;
			_XH = XH;
			_YHL = YHL;
			_ZHL = ZHL;
		}

		//=== Create the rigid body for our bone ===
		_oRigidBody = (Rigidbody)CUtility.FindOrCreateComponent(gameObject, typeof(Rigidbody));     //###TODO: Add a "CRigidBodyWake"???
        _oRigidBody.mass = nMass;
        _oRigidBody.drag = 1.5f;                            //###TODO!! //###DESIGN: Which drag??		//###TUNE!!!!!	###IMPROVE: Different settings for arms & legs???
        _oRigidBody.angularDrag = 1.5f;                     //###TUNE!!!!
        _oRigidBody.sleepThreshold = 0;                     // Ensure the rigid body never sleeps!


        //=== Process special handling needed when we are root (we are kinematic and we have no joint to parent) ===
        if (_oJointDrvParent == null) {             // If we have a null parent then we're the root and we're kinematic with no joint to anyone!

            _oRigidBody.isKinematic = true;

        }
        else {

            //=== If we have a parent then we are not kinematic and rotate by PhysX simulation ===
            _oRigidBody.isKinematic = false;

            //=== Create the D6 configurable joint between our parent and us ===
		    _oConfJoint = (ConfigurableJoint)CUtility.FindOrCreateComponent(gameObject, typeof(ConfigurableJoint));		//###TODO: Add a "CRigidBodyWake"???
		    _oConfJoint.connectedBody = _oJointDrvParent._oRigidBody;

            //=== Set the joint limits as per our arguments ===
            bool bInvertX = (_XL > _XH);                    //###OBS??? If the logical range is inverted we can't send this to PhysX as lowAngularXLimit MUST be < than highAngularXLimit!
			if (bInvertX)
				CUtility.ThrowException("Inverted XL / XH in bone " + gameObject.name);		//###CHECK
		    SoftJointLimit oJL = new SoftJointLimit();              //###IMPROVE: Has other fields that could be of use?
		    oJL.limit = bInvertX ? _XH : _XL;	_oConfJoint. lowAngularXLimit = oJL;		// X is the high-functionality axis with separately-defined Xmin and Xmax... Y and Z only have a +/- range around zero, so we are forced to raise the lower half to match the other side
		    oJL.limit = bInvertX ? _XL : _XH;	_oConfJoint.highAngularXLimit = oJL;
		    oJL.limit = _YHL;	                _oConfJoint.    angularYLimit = oJL;        //###NOTE! Hugely inconvenient feature of D6 joint is Y & Z must be symmetrical!!  Make sure bone is oriented so X is used for the assymetrical rotation!!
		    oJL.limit = _ZHL;	                _oConfJoint.    angularZLimit = oJL;

            //=== Set joint axis defaults (before overriding some of them) ===
            _oConfJoint.xMotion = _oConfJoint.yMotion = _oConfJoint.zMotion = ConfigurableJointMotion.Locked;
		    _oConfJoint.angularXMotion = _oConfJoint.angularYMotion = _oConfJoint.angularZMotion = ConfigurableJointMotion.Limited;		//###DESIGN? Limited vs Free?

            //=== Free the axis that don't need driving ===  ###CHECK: Safe???
		    if (_XL == 0f && _XH == 0f) _oConfJoint.angularXMotion = ConfigurableJointMotion.Free;		// If an axis is unused set it //free to reduce PhysX workload  ###CHECK: Is this ever invoked?  Does it make joint fail if not all three axis driven??
		    if (_YHL == 0f)             _oConfJoint.angularYMotion = ConfigurableJointMotion.Free;		//###DESIGN: Verify unsetting!
		    if (_ZHL == 0f)             _oConfJoint.angularZMotion = ConfigurableJointMotion.Free;		//###NOTE: SLERP needs all three axis by definition... But Limited of little / no use if we drive all the time (less PhysX overhead))
		
            //=== Set the configurable joint drive strength ===
		    JointDrive oDrive = new JointDrive();
		    oDrive.positionSpring = _nDriveStrengthMult * CGame.INSTANCE.BoneDriveStrength;   // Final spring strength is the global constant multiplied by the provided multiplier... makes it easy to adjust whole-body drive strength
            oDrive.positionDamper = 0;							//###TODO!!!!! ###TUNE?
		    oDrive.maximumForce = float.MaxValue;               //###IMPROVE: Some reasonable force to prevent explosions??
		    //oDrive.mode = JointDriveMode.Position;
		    _oConfJoint.slerpDrive = oDrive;
		    _oConfJoint.rotationDriveMode = RotationDriveMode.Slerp;        // Slerp is really the only useful option for bone driving.  (Many other features of D6 joint!!!)

            //=== If we're a node on the right side, copy the collider defined on our twin node on the left side ===
            if (_oActor._eBodySide == EBodySide.Right) {
			    Transform oNodeSrc = CUtility.FindSymmetricalBodyNode(transform.gameObject);
                //Debug.Log("Collider copy " + oNodeSrc.name);
                Collider oColBaseSrc = oNodeSrc.GetComponent<Collider>();
                if (oColBaseSrc.GetType() == typeof(CapsuleCollider)) {
				    CapsuleCollider oColSrc = (CapsuleCollider)oColBaseSrc;
				    CapsuleCollider oColDst = (CapsuleCollider)CUtility.FindOrCreateComponent(transform, typeof(CapsuleCollider));
				    oColDst.center 		= oColSrc.center;
				    oColDst.radius 		= oColSrc.radius;
				    oColDst.height 		= oColSrc.height;
				    oColDst.direction 	= oColSrc.direction;
                } else if (oColBaseSrc.GetType() == typeof(BoxCollider)) {
				    BoxCollider oColSrc = (BoxCollider)oColBaseSrc;
				    BoxCollider oColDst = (BoxCollider)CUtility.FindOrCreateComponent(transform, typeof(BoxCollider));
				    oColDst.center 		= oColSrc.center;
				    oColDst.size 		= oColSrc.size;
                }
            }
        }
    }

	public void OnChangeGameMode(EGameModes eGameModeNew, EGameModes eGameModeOld) {
		// Joint becomes kinematic and reverts to starting position upon configure mode, becomes PhysX-simulated during gameplay
		switch (eGameModeNew) {
			case EGameModes.Configure:
                _X = _Y = _Z = 0;
                UpdateRotation();
                _oRigidBody.isKinematic = true;
				transform.localPosition = _vecStartingPos;				// Restore the joint to its startup position / orientation
				transform.localRotation = _quatStartingRotation;
				break;
			case EGameModes.Play:
                if (_oConfJoint != null) { 
                    JointDrive oDrive = _oConfJoint.slerpDrive;
                    oDrive.positionSpring = _nDriveStrengthMult * CGame.INSTANCE.BoneDriveStrength;   // Final spring strength is the global constant multiplied by the provided multiplier... makes it easy to adjust whole-body drive strength
                    _oConfJoint.slerpDrive = oDrive;
                }
                _oRigidBody.isKinematic = false;
                _X = _Y = _Z = 0;
                UpdateRotation();
                break;                  //###IMPROVE: Add a new game mode for kinematic but 'reset pose to T'?
		}
	}

    void Update() {
        if (CGame.INSTANCE.BoneDebugMode) {                           // In 'bone debug mode' trap any change of our rotation values to update rotation right away.  (Makes it possible to quickly tune at gametime realistic bone rotations)
            if (_X != _X2 || _Y != _Y2 || _Z != _Z2) {
                UpdateRotation();           //_oRigidBody.WakeUp();
				_X2 = _X;  _Y2 = _Y;  _Z2 = _Z;
            }
            if (_XP != _XP2) {
				_XP = Mathf.Clamp(_XP, -100f, 100f);
				RotateX2(_XP);
                _XP2 = _XP;
            }
            if (_YP != _YP2) {
				_YP = Mathf.Clamp(_YP, -100f, 100f);
				RotateY2(_YP);
                _YP2 = _YP;
            }
            if (_ZP != _ZP2) {
				_ZP = Mathf.Clamp(_ZP, -100f, 100f);
				RotateZ2(_ZP);
                _ZP2 = _ZP;
            }
        }
    }

	//public Rigidbody GetRB() { return _oConfJoint.GetComponent<Rigidbody>(); /* GetComponent<Rigidbody>();*/ }
	
	void UpdateRotation() {     // Update X, Y, Z rotation
        if (_oConfJoint != null)
		    _oConfJoint.targetRotation = Quaternion.Euler(_X, _Y, _Z);      // Rotate as specified by X,Y,Z  (z, x, y in that order) (Joint already starts from its starting rotation)
    }

	public void DumpBonePos_DEV() {		//###NOTE: For development... Enables to dump the position of a joint as it is now to enable hard-coding of desirable states (useful to properly position arm)
		//float nXP = 100.0f * (_X - _XL) / (_XH - _XL);		// Not working as we have two formulas for single/dual range
		//float nYP = 100.0f * (_Y - _YL) / (_YH - _YL);
		//float nZP = 100.0f * (_Z - _ZL) / (_ZH - _ZL);
		//Debug.Log(string.Format("{0}.{1} at {2:F0},{3:F0},{4:F0}", _oActor.transform.name, _oTransform.name, nXP, nYP, nZP));
		//Vector3 vecRot = _oTransform.localRotation.eulerAngles;
		//Debug.Log(string.Format("{0}.{1} at {2:F0},{3:F0},{4:F0}", _oActor.transform.name, _oTransform.name, vecRot.x, vecRot.y, vecRot.z));
		Quaternion quatRot = transform.localRotation;
		Debug.Log(string.Format("{0}.{1} at {2:F3},{3:F3},{4:F3},{5:F3}", _oActor.transform.name, transform.name, quatRot.x, quatRot.y, quatRot.z, quatRot.w));
	}

    //=== Rotation where source value goes from 0% to 100% ===
    //public void RotateX1(float nAnglePercent) { _X = _X2 = _XL + (nAnglePercent/100f) * (_XH-_XL);  UpdateRotation(); }
    //public void RotateY1(float nAnglePercent) { _Y = _Y2 = (nAnglePercent/100f) * _YHL;             UpdateRotation(); }
	//public void RotateZ1(float nAnglePercent) { _Z = _Z2 = (nAnglePercent/100f) * _ZHL;             UpdateRotation(); }

    //=== Rotation where source value goes from -100% to 100% ===
    public void RotateX2(float nAnglePercent) { nAnglePercent = Mathf.Clamp(nAnglePercent, -100f, 100f); _X = _X2 = (nAnglePercent/100f) * ((nAnglePercent<0) ? -_XL : _XH); UpdateRotation(); }       //###DESIGN!!!: Not linear if low and high are not opposite... the desired behavior??
	public void RotateY2(float nAnglePercent) { nAnglePercent = Mathf.Clamp(nAnglePercent, -100f, 100f); _Y = _Y2 = (nAnglePercent/100f) * _YHL; UpdateRotation(); }       // Low and high are symmetrical, so simpler than X
	public void RotateZ2(float nAnglePercent) { nAnglePercent = Mathf.Clamp(nAnglePercent, -100f, 100f); _Z = _Z2 = (nAnglePercent/100f) * _ZHL; UpdateRotation(); }

	//public void SetRotationRaw_HACK(float X, float Y, float Z) { _X = X; _Y = Y; _Z = Z; UpdateRotation(); }		// Hack call to bypass our calibration setting and set in direct angles (extracted by observing body limbs in scene for e.g. arm placement)
	//public void SetRotationRaw_HACK(Quaternion quatRot) { _oConfJoint.targetRotation = _quatStartingRotation; }		// Hack call to bypass our calibration setting and set in direct angles (extracted by observing body limbs in scene for e.g. arm placement)
	//public void SetRotationDefault_HACK() { _oConfJoint.targetRotation = _quatStartingRotation; }

	//public void Enable() {				//###DESIGN!!!!??? ###BROKEN?? Not working...redo enable / disable design of actor??
	//	_oRigidBody.isKinematic = false;
	//}
	//public void Disable() {
	//	_oRigidBody.isKinematic = true;
	//	_oTransform.localPosition	= _vecStartingPos;
	//	_oTransform.localRotation 	= _quatStartingRotation;		// Restore rotation the way it was when we got created...  should return body to T-pose.
	//}
};
