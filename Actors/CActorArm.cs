#region JUNK

//if (CGame._bDevFlag1) {
//    float nLimit = 2;
//    for (int nFinger = 1; nFinger < 5; nFinger++) {
//        for (int nJoint = 0; nJoint < 3; nJoint++) {
//            CBone oBoneFinger = _aaFingers[nFinger, nJoint];
//            Vector3 eulRot = oBoneFinger.transform.localRotation.eulerAngles;
//            if (CGame._bDevFlag2)
//                eulRot.x = Mathf.Clamp(eulRot.x,  oBoneFinger._oJoint.lowAngularXLimit.limit,   oBoneFinger._oJoint.highAngularXLimit.limit);
//            if (CGame._bDevFlag3) {
//                eulRot.y = Mathf.Clamp(eulRot.y, -nLimit, nLimit);
//                eulRot.z = Mathf.Clamp(eulRot.z, -nLimit, nLimit);
//            }
//            //eulRot.y = Mathf.Clamp(eulRot.y, -oBoneFinger._oJoint.angularYLimit.limit, oBoneFinger._oJoint.angularYLimit.limit);
//            //eulRot.x = Mathf.Clamp(eulRot.z, -oBoneFinger._oJoint.angularZLimit.limit, oBoneFinger._oJoint.angularZLimit.limit);
//            oBoneFinger.transform.localRotation = Quaternion.Euler(eulRot);
//        }
//    }
//}

//_oBoneExtremity.gameObject.layer = nLayer;
//for (int nFinger = 0; nFinger < 5; nFinger++) {
//    for (int nJoint = 0; nJoint < 3; nJoint++) {
//        CBone oBoneFinger = _aaFingers[nFinger, nJoint];
//        oBoneFinger.gameObject.layer = nLayer;
//    }
//}
#endregion



using UnityEngine;
using System.Collections.Generic;
using System;

public class CJoint {

}


public class CActorArm : CActorLimb, IVisualizeFlex {

	[HideInInspector]	public 	CBone 	_oBoneCollar;
	[HideInInspector]	public 	CBone 	_oBoneShoulderBend;
	[HideInInspector]	public 	CBone 	_oBoneShoulderTwist;
    [HideInInspector]	public 	CBone 	_oBoneForearmBend;
    [HideInInspector]	public 	CBone 	_oBoneForearmTwist;
	[HideInInspector]	public 	CBone[,] _aaFingers;            // Array of the three bones for each five fingers of this hand
    [HideInInspector]	public 	EHandMode _eHandMode = EHandMode.Normal;
    [HideInInspector]	public 	EHandPose _eHandPose;

    /*###TODO: FlexHand
    - Move to a subclass of CSoftBody (so we can have visualization?)
    - Need to access proper spring so we can close fingers... by calculation or we stuff into array?
    - Particles explode!
    - Rest particles don't appear to have enough fingers!
    - Visualizer doesn't work!  Adopt our own!
        - Currently part of CSoftBody... detach?
    */ 



    [HideInInspector]	public	uFlex.FlexParticles			_oFlexParticles;
    [HideInInspector]	public  uFlex.FlexSprings           _oFlexSprings;              // Custom-created springs to bind the hand particles (over two layers) to give hand a softbody feel.
    [HideInInspector]	public	int                         _nParticles = 2*4*3;        // two layers (one for fingers other for 2nd layer on top of fingers to solidify the hand), four fingers, three joints per finger

    //--- Temporary build-time arrays for Flex Hand creation ---
    List<int>   _aSpringIndices       = new List<int>();
    List<float> _aSpringRestLength    = new List<float>();
    List<float> _aSpringCoef          = new List<float>();

    int FlexHand_CalcParticleID(int nLayer, int nFinger, int nJoint) {
        // Flattens the layer, finger, finger joint into a particle ID
        return (nLayer * 4 * 3) + (nFinger * 3) + nJoint;
    }

    void FlexHand_AddSpring(int nLayer1, int nFinger1, int nJoint1, int nLayer2, int nFinger2, int nJoint2) {
        int nPar1 = FlexHand_CalcParticleID(nLayer1, nFinger1, nJoint1);
        int nPar2 = FlexHand_CalcParticleID(nLayer2, nFinger2, nJoint2);
        _aSpringIndices.Add(nPar1);
        _aSpringIndices.Add(nPar2);
        _aSpringCoef.Add(1.0f);
        Vector3 vecPar1 = _oFlexParticles.m_particles[nPar1].pos;
        Vector3 vecPar2 = _oFlexParticles.m_particles[nPar2].pos;
        float nDistRest = Vector3.Distance(vecPar1, vecPar2);
        _aSpringRestLength.Add(nDistRest);
    }

    public void FlexHand_Create() {     //#Hand
		//=== Define Flex particles that will perform 2-way interaction of hands in the Flex scene ===
		_oFlexParticles = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexParticles)) as uFlex.FlexParticles;
        _oFlexParticles.m_drawDebug = true;
        _oFlexParticles.m_drawRest = true;
		_oFlexParticles.m_particlesCount        = _oFlexParticles.m_maxParticlesCount = _nParticles;
		_oFlexParticles.m_particles				= new uFlex.Particle[_nParticles];
		_oFlexParticles.m_restParticles			= new uFlex.Particle[_nParticles];		//###OPT19: Wasteful?  Remove from uFlex??
		_oFlexParticles.m_colours				= new Color[_nParticles];
		_oFlexParticles.m_velocities			= new Vector3[_nParticles];
		_oFlexParticles.m_densities				= new float[_nParticles];
		_oFlexParticles.m_particlesActivity		= new bool[_nParticles];
		_oFlexParticles.m_colour				= Color.green;
		_oFlexParticles.m_interactionType		= uFlex.FlexInteractionType.None;       //###TEMP
		_oFlexParticles.m_collisionGroup		= -1;
		_oFlexParticles.m_overrideMass			= true;							//###INFO: when m_overrideMass is true ALL particles have invMass set to 1 / m_mass.  So we *MUST* have this always true so we can have kinematic particles
		_oFlexParticles.m_mass					= 1f;							//###INFO: when m_overrideMass is true this has NO influence so we can set to whatever.
		//_oFlexParticles.m_bounds = oMeshBaked.bounds;

        _oFlexSprings   = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexSprings))   as uFlex.FlexSprings;

        //for (int nLayer = 0; nLayer < 2; nLayer++) {
        //    for (int nFinger = 0; nFinger < 4; nFinger++) {
        //        for (int nJoint = 0; nJoint < 3; nJoint++) {
        //        }
        //    }
        //}

        //=== Set the starting particle positions ===
        for (int nLayer = 0; nLayer < 2; nLayer++) {
            for (int nFinger = 0; nFinger < 4; nFinger++) {
                for (int nJoint = 0; nJoint < 3; nJoint++) {
                    int nPar = FlexHand_CalcParticleID(nLayer, nFinger, nJoint);
                    CBone oBoneFinger = _aaFingers[nFinger, nJoint];
                    Vector3 vecPos = oBoneFinger._vecStartingPos;
                    vecPos.y += 1;
                    if (nLayer == 1)
                        vecPos.x += 0.01f;      //###TODO: Add up vector
                    _oFlexParticles.m_particles[nPar].pos = vecPos;
                    _oFlexParticles.m_particles[nPar].invMass = (nJoint == 0) ? 0 : 1;
                    _oFlexParticles.m_restParticles[nPar].pos = vecPos;
                    _oFlexParticles.m_restParticles[nPar].invMass = _oFlexParticles.m_particles[nPar].invMass;
                    _oFlexParticles.m_particlesActivity[nPar] = true;
                }
            }
        }

        //=== Link each finger joint with its subsequent joint ===
        for (int nLayer = 0; nLayer < 2; nLayer++) {
            for (int nFinger = 0; nFinger < 4; nFinger++) {     
                for (int nJoint = 1; nJoint < 2; nJoint++) {      //###NOTE: Ending one joint before the end
                    FlexHand_AddSpring(nLayer, nFinger, nJoint, nLayer, nFinger, nJoint + 1);
                }
            }
        }
        //=== Link the fingers together ===
        for (int nLayer = 0; nLayer < 2; nLayer++) {
            for (int nFinger = 0; nFinger < 3; nFinger++) {                         //###NOTE: Ending one finger before last
                for (int nJoint = 0; nJoint < 3; nJoint++) {
                    FlexHand_AddSpring(nLayer, nFinger, nJoint, nLayer, nFinger + 1, nJoint);
                }
            }
        }
        //=== Link the two layers together ===
        for (int nFinger = 0; nFinger < 4; nFinger++) {
            for (int nJoint = 0; nJoint < 3; nJoint++) {
                FlexHand_AddSpring(0, nFinger, nJoint, 1, nFinger, nJoint);
            }
        }

        //=== Copy our temporary working arrays into the pertinent Flex array ===
        _oFlexSprings.m_springIndices       = _aSpringIndices.ToArray();
        _oFlexSprings.m_springRestLengths   = _aSpringRestLength.ToArray();
        _oFlexSprings.m_springCoefficients  = _aSpringCoef.ToArray();
        _oFlexSprings.m_springsCount = _aSpringRestLength.Count;
        //_oFlexSprings.m_debug = true;
        _aSpringIndices       = null;
        _aSpringRestLength    = null;
        _aSpringCoef          = null;
    }

    //---------------------------------------------------------------------------	CREATE / DESTROY
    public override void OnStart_DefineLimb() {
		_bBakeJointAnglesWhenMovingPin = true;              // Enable this limb to 'bake' each joint angles as the pin is moved.  Greatly stabilizes the limb during gameplay as the hard work of setting joint angles has been done by the pose designer
		///_nDrivePinToBone = 0.1f * C_DrivePinToBone;				// Weaken the hand drive so hand doesn't fly from pin to pin		//###TUNE

		//=== Init Bones and Joints ===
        CBone oBoneChestUpper = _oBodyBase._oActor_Chest._oBoneExtremity;           //###DEV21:!!! Cleanup all the old crap!
		_aBones.Add(_oBoneCollar			= CBone.Connect(this, oBoneChestUpper,		_chSidePrefixL+"Collar",	    3.0f, CBone.EBoneType.ArmCollar));
		_aBones.Add(_oBoneShoulderBend		= CBone.Connect(this, _oBoneCollar,		    _chSidePrefixL+"ShldrBend",     2.0f, CBone.EBoneType.Bender));
		_aBones.Add(_oBoneShoulderTwist		= CBone.Connect(this, _oBoneShoulderBend,   _chSidePrefixL+"ShldrTwist",    1.5f, CBone.EBoneType.Twister));
		_aBones.Add(_oBoneForearmBend		= CBone.Connect(this, _oBoneShoulderTwist,	_chSidePrefixL+"ForearmBend",   1.5f, CBone.EBoneType.Bender));
		_aBones.Add(_oBoneForearmTwist		= CBone.Connect(this, _oBoneForearmBend,	_chSidePrefixL+"ForearmTwist",  1.0f, CBone.EBoneType.Twister));
		_aBones.Add(_oBoneExtremity			= CBone.Connect(this, _oBoneForearmTwist,	_chSidePrefixL+"Hand",		    1.0f, CBone.EBoneType.Extremity));     //###INFO: Mass has a HUGE influence on how strong and stable joints are!  Setting hand mass lower makes in much less stable!  ###TODO: Study why!!

        //=== Manually remove the shoulder's Y rotation as it looks horrible! ===   ###HACK: Bake into bone angles in Blender import??
        _oBoneCollar._oJointD6.angularYMotion = ConfigurableJointMotion.Locked;

        _oBoneForearmBend.RotateX(_eBodySide == EBodySide.Left ? 60 : -60);              // Bend forearm halfway by default so body 'folds' more easily.        //###IMPROVE: Have a function that properly inverts!?!

        //OnSet_Dev_Limb_RigidBody_Drag(0, 15);
        //OnSet_Dev_Limb_RigidBody_AngDrag(0, 0.1f);

        //transform.position = new Vector3(_eBodySide == EBodySide.Left ? -0.22f : 0.22f, 0.90f, 0.00f);       // Position the anchor pin at a reasonable position alongside the thigh

        //=== Init Hotspot ===
  //      if (_eBodySide == EBodySide.Left)
		//	_oHotSpot = CHotSpot.CreateHotspot(this, _oBodyBase.FindBone("hip/abdomenLower/abdomenUpper/chestLower/chestUpper/lCollar/lShldrBend/lShldrTwist/lForearmBend/lForearmTwist/lHand"), "Left Hand", true, G.C_Layer_HotSpot);		//###IMPROVE20: Horrible path!  Shorten by using some var!
		//else
		//	_oHotSpot = CHotSpot.CreateHotspot(this, _oBodyBase.FindBone("hip/abdomenLower/abdomenUpper/chestLower/chestUpper/rCollar/rShldrBend/rShldrTwist/rForearmBend/rForearmTwist/rHand"), "Right Hand", true, G.C_Layer_HotSpot);

        //=== Init CObj ===
        _oObj = new CObj("Arm" + _chSidePrefixU, this);
        _oObj.Event_PropertyValueChanged += Event_PropertyChangedValue;
        AddBaseActorProperties();                       // The first properties of every CActor subclass are Pinned, pos & rot
#if __DEBUG__
        //_oObj.Add("Dev_Arm_Collar_SlerpStrength",       this, 0, 0, 50);
        //_oObj.Add("Dev_Arm_Shoulder_SlerpStrength",     this, 0, 0, 50);
        //_oObj.Add("Dev_Arm_Elbow_SlerpStrength",        this, 0, 0, 50);
        //_oObj.Add("Dev_Arm_Twist_SlerpStrength",        this, 0, 0, 50);
#endif

        _aaFingers = new CBone[5,3];			// Array of the three bones for each five fingers of this hand

        //=== Initialize the fingers ===
        DefineFinger(0, "Thumb",    "");
        DefineFinger(1, "Index",    "Carpal1");
        DefineFinger(2, "Mid",      "Carpal2");
        DefineFinger(3, "Ring",     "Carpal3");
        DefineFinger(4, "Pinky",    "Carpal4");

        //=== Define the PhysX properties for all fingers ===
        for (int nFinger = 0; nFinger < 5; nFinger++) {
            for (int nJoint = 0; nJoint < 3; nJoint++) {
                CBone oBoneFinger = _aaFingers[nFinger, nJoint];
                oBoneFinger.gameObject.layer = G.C_Layer_Hand;
                //oBoneFinger._oRigidBody.mass = 2;
                oBoneFinger._oRigidBody.isKinematic = false;  //#DEV27:??????? _eBodySide == EBodySide.Right;        
                oBoneFinger._oRigidBody.useGravity = false;
                oBoneFinger._oJointD6.angularYMotion = oBoneFinger._oJointD6.angularZMotion = ConfigurableJointMotion.Locked;
                Collider oCol = oBoneFinger.GetComponent<Collider>();
                oCol.material = CGame._oPhysMat_Friction_Lowest;      //###CHECK ###DEV278
            }
        }

        //=== Specialize the PhysX properties for Thumb joints ===
        CBone oBoneThumb0 = _aaFingers[0, 0];
        oBoneThumb0._oJointD6.angularYMotion = oBoneThumb0._oJointD6.angularZMotion = ConfigurableJointMotion.Limited;      // Thumb0 gets all three rotations enabled (it needs that full flexibility to support the hand shapes we need)
        CBone oBoneThumb1 = _aaFingers[0, 1];
        oBoneThumb1._oJointD6.angularYMotion = ConfigurableJointMotion.Limited;
        CBone oBoneThumb2 = _aaFingers[0, 2];
        oBoneThumb2._oJointD6.angularYMotion = ConfigurableJointMotion.Limited;

        //float nSizeFingerTriggerSphere = 0.030f;
        //Vector3 vecOffset = new Vector3(0, 0, _eBodySide == EBodySide.Left ? nSizeFingerTriggerSphere : -nSizeFingerTriggerSphere);
        //for (int nFinger = 1; nFinger < 5; nFinger++) {
        //    for (int nJoint = 0; nJoint < 3; nJoint++) {
        //        CBone oBoneFinger = _aaFingers[nFinger, nJoint];
        //        oBoneFinger.CreateTriggerChild(nSizeFingerTriggerSphere, vecOffset);
        //    }
        //}

        //DEV_SetFingerPhysicsParameters_HACK();

        //_oBoneShoulderBend.RotateX2(this._eBodySide == EBodySide.Left ? -90 : 90);        // Set the default should bend to be alongside body (instead of 'T' pose)

        FlexHand_Create();
    }

    void DefineFinger(int nFinger, string sNameJointPrefix, string sNameIntermediateJointToSkip) {
        //const float nDriveStrength_Fingers = 0.1f;
        sNameIntermediateJointToSkip  = _chSidePrefixL + sNameIntermediateJointToSkip;          // Prefix all our joint names with our prefix side 'l' or 'r'
        sNameJointPrefix              = _chSidePrefixL + sNameJointPrefix;
        string sPathFingerRoot;
        if (sNameIntermediateJointToSkip.Contains("Carpal")) {         // Carpal bones are skipped and we go straight for the finger (Thumb has no carpal ancestor bone)
            sPathFingerRoot = string.Format("{0}/{1}", sNameIntermediateJointToSkip, sNameJointPrefix);
        } else {                        
            sPathFingerRoot = sNameJointPrefix;
        }
        float nMass = 0.015f;            // Creates fingers that are NOT simulated by PhysX if we have zero mass (e.g. straight setting of 'localRotation')
        _aaFingers[nFinger, 0] = CBone.Connect(this, _oBoneExtremity,           sPathFingerRoot        + "1", nMass, CBone.EBoneType.Finger);
        _aaFingers[nFinger, 1] = CBone.Connect(this, _aaFingers[nFinger, 0],    sNameJointPrefix + "2", nMass, CBone.EBoneType.Finger);
        _aaFingers[nFinger, 2] = CBone.Connect(this, _aaFingers[nFinger, 1],    sNameJointPrefix + "3", nMass, CBone.EBoneType.Finger);
    }

    protected override void PinToExtremity_ConfigureJoint(JointDrive oJointDriveSlerp) {    //#DEV26: ###TODO: Move to limb
        base.PinToExtremity_ConfigureJoint(oJointDriveSlerp);

        //_oJoint_ExtremityToPin.anchor = new Vector3(0.012137f, 0.082438f, 0.02f);        //#DEV26: ###IMPROVE: Read from marker in hand?
        //_oJoint_ExtremityToPin.anchor = new Vector3(0.006f, 0.096f, 0.048f);            //###DEV27: Cock anchor ###IMPROVE: Set into prefab?
        //_oJoint_ExtremityToPin.anchor = new Vector3(0.006f, 0.07f, 0.04f);            //###DEV27: Cock anchor ###IMPROVE: Set into prefab?  Z = height, Y = tips
        _oJoint_ExtremityToPin.anchor = new Vector3(0.006f, 0.07f, 0.07f);            //###DEV27: Cock anchor ###IMPROVE: Set into prefab?  Z = height, Y = tips
        _oJoint_ExtremityToPin.autoConfigureConnectedAnchor = false;
        _oJoint_ExtremityToPin.connectedAnchor = Vector3.zero;
        _oJoint_ExtremityToPin.angularZMotion = ConfigurableJointMotion.Free;            // Limit hand joint to X,Y motion while leaving Z free to rotate like a hinge.  This is essential for PhysX to auto-calculate how to best position entire arm and hand to most easily reach pin point
        oJointDriveSlerp.positionSpring = 0.0f;                 // Disable Slerp for arm pin.  Strictly D6 limits and no additional drive!

        const float nTinyLimitToEnableSpring = 0.000001f;
        SoftJointLimit oLimit = new SoftJointLimit();
        oLimit.limit = nTinyLimitToEnableSpring;
        _oJoint_ExtremityToPin.linearLimit = oLimit;

        oLimit.limit = -nTinyLimitToEnableSpring;
        _oJoint_ExtremityToPin.lowAngularXLimit = oLimit;
        oLimit.limit =  nTinyLimitToEnableSpring;
        _oJoint_ExtremityToPin.highAngularXLimit = oLimit;

        oLimit.limit =  nTinyLimitToEnableSpring;               //###CHECK: PhysX has this large value as minimum for Y,Z??
        _oJoint_ExtremityToPin.angularYLimit = oLimit;
        _oJoint_ExtremityToPin.angularYLimit = oLimit;

        Util_SetJointExtremityToPinStrengths_ByMode(/*bDirectDriveStrength=*/false);     // Set the pin driving strength to normal
    }

    public override void OnActorPinned(bool bPinned) {
        _oBoneForearmBend   ._oRigidBody.useGravity = !bPinned;         // Disable gravity when pinned so our pin strenght can be a lot weaker
        _oBoneForearmTwist  ._oRigidBody.useGravity = !bPinned;
        _oBoneExtremity     ._oRigidBody.useGravity = !bPinned;
    }

    //---------------------------------------------------------------------------	UPDATE

    public override void OnUpdate() {          // Arms need per-frame update to handle pinned situations where we constantly set our pin position to a body collider vert
		base.OnUpdate();

        if (_eBodySide == EBodySide.Left) {
            if (Input.GetKeyDown(KeyCode.Alpha9))
                HandPose_Set(EHandPose.Default);
            if (Input.GetKeyDown(KeyCode.Alpha8))
                HandPose_Set(EHandPose.GrabCup);
            if (Input.GetKeyDown(KeyCode.Alpha7))
                HandPose_Set(EHandPose.Planar);
        }

        if (_eHandMode == EHandMode.MasturbatingPenis) {
            float nPointInCycle = (Time.time / 2.0f) * Mathf.PI;
            float nLenRatio = (Mathf.Sin(nPointInCycle) + 1) / 2;
            Vector3 vecPosAtRatio = CGame.INSTANCE.GetBody(0)._oSoftBody_Penis.PenisCenterCurve_Get3dPosAtLength(nLenRatio);
            transform.position = vecPosAtRatio;
            //###DEV27: Set hand pose every frame?  Degree of finger closing?  From what info???
        }

        Visualizer_TestForCreationDestruction();
    }

    //---------------------------------------------------------------------------	COBJECT EVENTS

    void Event_PropertyChangedValue(object sender, EventArgs_PropertyValueChanged oArgs) {}
    
    public void HandPose_Set(EHandPose eHandPose, float nFingersClosed_HACK = 0) {
        _eHandPose = eHandPose;
        switch (_eHandPose) {
            case EHandPose.Default:
                _aaFingers[0, 0].RotateX(0);
                _aaFingers[0, 0].RotateZ(0);
                _aaFingers[0, 2].RotateX(0);
                for (int nFinger = 1; nFinger < 5; nFinger++) {
                    for (int nJoint = 0; nJoint < 3; nJoint++) {
                        _aaFingers[nFinger, nJoint].RotateX(0);
                    }
                }
                break;
            case EHandPose.Planar:
                break;
            case EHandPose.GrabCup:
                //_aaFingers[0, 0].RotateX(CGame.INSTANCE.s_vecFingerThumb0_Grab.x);
                //_aaFingers[0, 0].RotateY(CGame.INSTANCE.s_vecFingerThumb0_Grab.y);
                //_aaFingers[0, 0].RotateZ(CGame.INSTANCE.s_vecFingerThumb0_Grab.z);
                //_aaFingers[0, 1].RotateY(CGame.INSTANCE.s_vecFingerThumb1_Grab.x);
                //_aaFingers[0, 2].RotateY(CGame.INSTANCE.s_vecFingerThumb1_Grab.y);
                //_aaFingers[0, 2].RotateX(CGame.INSTANCE.s_vecFingerThumb1_Grab.z);

                _aaFingers[0, 0]._oJointD6.targetRotation = Quaternion.Euler(CGame.INSTANCE.s_vecFingerThumb0_Grab);

                //float nFingersClose = -CGame._oObj.Get("DEV_FingerClose_Moving");
                for (int nFinger = 1; nFinger < 5; nFinger++) {
                    for (int nJoint = 0; nJoint < 3; nJoint++) {
                        _aaFingers[nFinger, nJoint].RotateX((nJoint == 0) ? -nFingersClosed_HACK * 2 : -nFingersClosed_HACK);
                    }
                }
                break;
        }
    }

    void HandCollider_EnableDisableCollisionWithOtherCollider(Collider oColOther, bool bEnableCollision) {
        Physics.IgnoreCollision(oColOther, _oBoneExtremity.GetComponent<Collider>(), !bEnableCollision);
        for (int nFinger = 0; nFinger < 5; nFinger++) {
            for (int nJoint = 0; nJoint < 3; nJoint++) {
                CBone oBoneFinger = _aaFingers[nFinger, nJoint];
                Collider oCol = oBoneFinger.GetComponent<Collider>();
                if (oCol)
                    Physics.IgnoreCollision(oColOther, oCol, !bEnableCollision);
            }
        }
    }
    public void HandCollider_CollisionsAgainstBodySurface_Enable() {
        if (CGame._aBodyBases[0]._oBody == null)
            return;
        Collider oColBodySurface = CGame._aBodyBases[0]._oBody._oFlexTriCol_BodySurface.GetComponent<MeshCollider>();      //#DEV26: ###HACK!!!!
        HandCollider_EnableDisableCollisionWithOtherCollider(oColBodySurface, true);
        //float nMassMult = CGame._oObj.Get("DEV_RB_MassMultInMove");
        //_oBoneExtremity   ._oRigidBody.mass = _oBoneExtremity   ._nMass * nMassMult;
        //_oBoneForearmTwist._oRigidBody.mass = _oBoneForearmTwist._nMass * nMassMult;
        //foreach (CBone oBone in _aBones)
        //    oBone._oRigidBody.mass = oBone._nMass * nMassMult;
    }
    public void HandCollider_CollisionsAgainstBodySurface_Disable() {
        if (CGame._aBodyBases[0]._oBody == null)
            return;
        Collider oColBodySurface = CGame._aBodyBases[0]._oBody._oFlexTriCol_BodySurface.GetComponent<MeshCollider>();      //#DEV26: ###HACK!!!!
        HandCollider_EnableDisableCollisionWithOtherCollider(oColBodySurface, false);
        //float nMassMult = CGame._oObj.Get("DEV_RB_MassMultInMove");
        //_oBoneExtremity   ._oRigidBody.mass = _oBoneExtremity._nMass;
        //_oBoneForearmTwist._oRigidBody.mass = _oBoneForearmTwist._nMass;
        //foreach (CBone oBone in _aBones)
        //    oBone._oRigidBody.mass = oBone._nMass;
    }



    //---------------------------------------------------------------------------	DEBUG PROPERTIES

#if __DEBUG_
    //public void OnSet_Dev_Arm_Collar_SlerpStrength(float nValueOld, float nValueNew) {
    //    CUtility.Joint_SetSlerpPositionSpring(_oBoneCollar._oJointD6, nValueNew);
    //}
    //public void OnSet_Dev_Arm_Shoulder_SlerpStrength(float nValueOld, float nValueNew) {
    //    CUtility.Joint_SetSlerpPositionSpring(_oBoneShoulderBend._oJointD6, nValueNew);
    //}
    //public void OnSet_Dev_Arm_Elbow_SlerpStrength(float nValueOld, float nValueNew) {
    //    CUtility.Joint_SetSlerpPositionSpring(_oBoneForearmBend._oJointD6, nValueNew);
    //}
    //public void OnSet_Dev_Arm_Twist_SlerpStrength(float nValueOld, float nValueNew) {
    //    CUtility.Joint_SetSlerpPositionSpring(_oBoneShoulderTwist._oJointD6, nValueNew);
    //    CUtility.Joint_SetSlerpPositionSpring(_oBoneForearmTwist ._oJointD6, nValueNew);
    //}
#endif

    //---------------------------------------------------------------------------	VR WAND MOVEMENT
    public override Transform VrWandMove_Begin(CVrWand oVrWand, bool bStartAction1, bool bStartAction2) {       //###DESIGN: Rename to auto move, manual move?
        base.VrWandMove_Begin(oVrWand, bStartAction1, bStartAction2);
        if (bStartAction1) {               //#DEV26: Proper?  Grip too?
            HandPose_Set(CActorArm.EHandPose.Default);
        } else if (bStartAction2) {
            _oJoint_ExtremityToPin.angularZMotion = ConfigurableJointMotion.Limited;        // For the duration of user manual movement, limit the Z-rotation so that player has full 6 degree of freedom control over the hand to properly place in 3D from the VR wand
            Util_SetJointExtremityToPinStrengths_ByMode(/*bDirectDriveStrength=*/true);     // Greatly strengthen the pulling power of the pin during manual moves
            HandPose_Set(CActorArm.EHandPose.GrabCup);              // Set the hand's pose during manual move = closing fingers to hold a 'cup'
            HandCollider_CollisionsAgainstBodySurface_Disable();    // Disable hand's PhysX collider so it can go right into the softbodies ###IMPROVE: Keep collisions against main body!  (Need to separate softbody PhysX mesh colliders into their own layer!!)
        }
        return transform;
    }
    public override void VrWandMove_End(CVrWand oVrWand) {
        base.VrWandMove_End(oVrWand);
        _oJoint_ExtremityToPin.angularZMotion = ConfigurableJointMotion.Free;            // Restore Z motion to the default 'free rotation' so automatic pins self-resolve the hand rotation about the body
        Util_SetJointExtremityToPinStrengths_ByMode(/*bDirectDriveStrength=*/false);     // Return the driving strength to normal
        HandPose_Set(CActorArm.EHandPose.Default);
        HandCollider_CollisionsAgainstBodySurface_Enable();     // Re-enable hand's PhysX collider so so hands are now repelled even by softbodies (through their PhysX static colliders)
    }
    public override void VrWandMove_Update(CVrWand oVrWand) {
        base.VrWandMove_Update(oVrWand);
        VrWandMove_UpdatePositionAndRotation(transform);            // Redirect to this call so rotation can adjust 'up' to be compatible with our joint
    }

    public override void VrWandMove_UpdatePositionAndRotation(Transform oNodeT) {
        transform.position = oNodeT.position;
        transform.rotation = Quaternion.LookRotation(oNodeT.forward, _oBoneForearmTwist.transform.up);      //###INFO: Our configuration joint (used for hands as a hinge joint) becomes unstable if the up vectors are pointing into each other.  Align the hand up vector with the forearm up vector to stabilize
    }


    public void HandMode_Change(EHandMode eHandMode) {
        _eHandMode = eHandMode;
        switch (_eHandMode) {
            case EHandMode.Normal:
                break;
            case EHandMode.MasturbatingPenis:
                VrWandMove_Begin(CGame._oVrWandL, false, true);     //###DEV27: Fix this shit up!!
                HandCollider_CollisionsAgainstBodySurface_Enable();     
                break;
        }
    }

	#region =========================================================================	VISUALIZATION
    [HideInInspector]	public	CVisualizeFlex              _oVisualizeFlex;        //#Vis
						public	bool						_bEnableVisualizer = false;
    [HideInInspector]	public	bool						_bEnableVisualizer_COMPARE = true;
						public	Vector3						_vecVisualiserOffset = new Vector3();//(0.05f, 0.05f, 0.05f);
						public	float						_SizeParticles_Mult	= 0.25f;		//###IMPROVE: Add capacity for dev to change these at runtime via Unity property editor
						public	float						_SizeShapes_Mult	= 0.50f;

    void Visualizer_TestForCreationDestruction() {
		if (_bEnableVisualizer != _bEnableVisualizer_COMPARE) {
            if (_bEnableVisualizer) {
			    _oVisualizeFlex = CVisualizeFlex.Create(gameObject, this);
            } else {
                GameObject.Destroy(_oVisualizeFlex);
                _oVisualizeFlex = null;
            }
			_bEnableVisualizer_COMPARE = _bEnableVisualizer;
		}
    }

    public uFlex.FlexParticles          GetFlexParticles()          { return _oFlexParticles; }     //###IMPROVE: Just make interface the public vars?
    public uFlex.FlexShapeMatching      GetFlexShapeMatching()      { return null; }
    public Vector3                      GetVisualiserOffset()       { return _vecVisualiserOffset; }
	public float					    GetSizeParticles_Mult()     { return _SizeParticles_Mult; }
	public float						GetSizeShapes_Mult()        { return _SizeShapes_Mult; }
    #endregion

    public enum EHandPose {
        Default,
        GrabCup,
        Planar,
    }

    public enum EHandMode {
        Normal,
        MasturbatingPenis,
    }
}
