/*###DISCUSSION: Anim hands

=== FINISH ===
 * Cleanup old crap!
   * Cleanup old bone tree & code??  Old layer for hands?
 * Vert snapping on closest sphere found, but we need closest vert!  (can simplify C++ now that we don't need as much accuracy??)
 * Make a few hand poses flow... enhance with bone settings?
 * Reduce pin strength??  Increase damping??
 * 
=== DESIGN ===
- Important the 'drag' of the rigid body!  Don't forget that one!!
- Pose spring is very important parameter and needs tweaking for all bones!
- We want to work on the in-game experience of easy interaction to the lovemaking experience.
	- All actions the user does control public variables that are recorded in animation tracks.
		- The user can go back in time with these, setup loops, save poses, create sequences, share them etc.
		- The only thing he/she can't do is do advanced editing & adjust timing -> that requires Unity editor and the Animation GUI panels

- Now operating on configurable joint, but duplicate the code so that it can act on the straight bones...
	- Add editor-based editing for quick posing!!  This can facilitate animation curves... (without all PhysX IK help of course)
- Concept of 'anchor' on the hands that shifts to different pre-defined transforms: such as tip of index for vagina insertion, palm of hand for under breast, mid-index finger for around cock, etc
- Some bones (like collar) need much stiffer drive to avoid looking stupid when changing... how to merge that in??
- Create CWalkerActor and CWalkerArea class?
	- CWalkerArea is derives from our skinned collider and add an 'area of interest' perpendicular to the normal it calculates... :)

=== HAND BREAST SIMULATION ===
 * A big collider really helps... need less cycles to simulate and get breasts out of the way... so diff colliders for PhysX2 & 3?
   * PhysX2 assumes flat hands
 * increase particle radius helps too
 * The big question... move by CKeyHook or go the distance to animate hands like we do sex bone??
=== IDEAS ===
- Caress zones of differenty types... that are invoked occasionally according to how 'turned on' the character is (e.g. stroking the arms during early foreplay... then the breasts... finally vagina penetration!)

=== PROBLEMS ===
- TEMP: Set drag of left arm for test!

=== TODO ===
- Give shapes and colors to springs and targets!
- To clean up what is shown in animator GUI, consider moving all 'super public' properties to own component??
- AnimEvents: need to extract the playing animation context...
	- Make tests to perform PingPong, looping, wait for other event, etc
	

- Develop 'autofind' ability of a limb to detect an interesting magnet in its area of interest.  (Use boxe triggers for this??
- Reduce strenght of slerp and start blending in effects of magnets
- Find how we're going to dampen... with rigidbody drag, angle drag?  Joint drag?  There are so many!!
- Develop hand poses
- NEXT: Bring back magnets, reduce drive and follow the tits!!
- Caress of other body... find hotspot area through trigger boxes??
 
- Now have working actor magnets and anchors!!!
	- Work on the proper 'twist'... impossible with spring joint?  Config Joint on hands then??
		- Or might rely on simply the colliders doing their job??  (Assuming a proper bone drive hint that is weakly driven)
		- Idea: Set the magnet INSIDE the body and have the collider keep on surface!!!!!
	- With magnets and anchors, do we create a layer of abstraction on top of that that connects the two in easy-to-invoke one enum?
		- This would of course have to be sensitive to the relative position of the actor with the target... e.g. stroking penis while standing up has the hand position differently then kneeling!  (Might be too complex... best left to animator??)

=== NOW ===
- Right hand side is getting a pain... subclass?
- Number of bind poses shutdown problem a pain...
- Stronger drive of collar a must...
- Remove most drives??
- Clarify naming between 'walker' and 'target' and update vars!
- Some targets (BackOfHead) need special 'hints' to look good...  as they are the exception postpone these for now?
- It would be nice to be able to save target positions at gametime?
- Have a 'cut hand' to place with would make things more intuitive!
	- Have that hand accept params to properly visualize fingers?
	- Do we place finger position hints in target??
- Have code pull possible choices out of nodes at init?
- Conclusions on early experiments:
	- Target has to drive collar strongly and shoulder weakly... elbow and hand can be solved by PhysX
	- So to design these we need to have a 'test mode' where magnet can be quickly be turned on and off (so collar & shoulder drive can show their results)
	- Therefore... is our old concept of 'pose' dead?  Will we ever have to drive?
	- Possible to have lockups in certain transitions... will the collar and shoulder drives be sufficient to resolve?
	- Will we ever have to drive elbow and hand???  If no then why have them as anim parameters??  TEST!!

- Continue exploring arms and modes.  The mode with drive and targets is the right idea
	- Create an in game menu that appears when user presses Q or E to enter that mode
	- Record these actions in an animation clip that is created at the beginning of every game.

*/

using UnityEngine;
using System.Collections;
using System;

public class CActorArm : CActor {

	[HideInInspector]	public 	CJointDriver 	_oJointCollar;
	[HideInInspector]	public 	CJointDriver 	_oJointShoulder;							// Hand joint is called _oJointExtremity and elbow joint _oJointMidLimb in base class as they require base-class processing

	[HideInInspector]	public 	CJointDriver[,] _aaFingers = new CJointDriver[5,3];			// Array of the three bones for each five fingers of this hand

	[HideInInspector]	public 	CHandTarget		_oHandTarget;			// Our connected hand target (if any) responsible for pinning and animate the hand...

	//####OBS? float[]			_aFingerSpreadMultiplier = new float[] { 0, 1.3f, 0.3f, -0.5f, -1.1f };		// These multiply the Fingers_Spread to cause fingers Y to spread fingers away from each other in different ration (e.g. not all rotate in same direction!)

	//--- Ray-pinning of hands variables ---
	//CMemAlloc<Vector3>	_memVecRayHitInfo;				// HandTarget of the last raycast hit as computed by DLL raycasting.  Used for arm pinning
	CHandTarget			_oHandTarget_RaycastPin;		// Commonly used hand target used during commonly-used raycast to position hands.
	//int					_nArmPinBodyColVert;			// Body collider vert that pinned arm is following
	CBody				_oBodyArmPin;					// Body owning body collider vert _nArmPinBodyColVert
	//float				_nTimeStartPinSet;				// Time.time value when pin was set.  Used to gracefully slerp to new position to avoid sharp hand movements.
	//bool				_bPinByClosestVert;				// When true we pin to the closest body collider vert.  When false we pin by ray position (much more accurate)


	const float			C_TimeToMoveToNewPinPos = 9.0f;	// Time to slerp to new hand pin position		###TUNE

	//---------------------------------------------------------------------------	CREATE / DESTROY
	public override void OnStart_DefineLimb() {
		_nDrivePos = 0.1f * C_DrivePos;				// Weaken the hand drive so hand doesn't fly from pin to pin		//###TUNE
		//_memVecRayHitInfo = new CMemAlloc<Vector3>(2);

		//=== Init Bones and Joints ===
		_aJoints.Add(_oJointCollar	 = new CJointDriver(this, _oBody._oActor_Chest._oJointExtremity,	_sSidePrefixL+"Collar",	50.0f*C_DriveAng, 1.5f, -000f,  070f,  030f, -030f,  030f, -015f));		//###IMPROVE: Could go to -10 on x for getting hands lower still...  ####MOD: Stiffened, was 5*
		_aJoints.Add(_oJointShoulder = new CJointDriver(this, _oJointCollar,		_sSidePrefixL+"Shldr",			1.0f*C_DriveAng, 2.5f, -082f,  020f,  030f, -080f,  040f, -045f));		//###BUG: Shoulder Y goes forward much more than back... leaving the Y negative range very large if we don't slerp drive!!   What to do with no drive to prevent this??
		_aJoints.Add(_oJointMidLimb	 = new CJointDriver(this, _oJointShoulder,		_sSidePrefixL+"ForeArm",		1.0f*C_DriveAng, 1.5f,  020f, -130f,  000f,  000f, -030f,  030f));
		_aJoints.Add(_oJointExtremity= new CJointDriver(this, _oJointMidLimb,		_sSidePrefixL+"Hand",			1.0f*C_DriveAng, 0.5f, -080f,  070f, -030f,  030f, -050f,  050f));		//###TODO: Might have to negate some axis
		
		//ConfigFingerRoot(1, "Index");			// Thumb is handled below		###DESIGN!!! Fingers in Unity PhysX is just a no-go because of extremely poor latency... what to do?????
		//ConfigFingerRoot(2, "Mid");
		//ConfigFingerRoot(3, "Ring");			//??? ###HACK!! Disabled man's carpal1&2 in 3dsMax so we can use same code as women's!
		//ConfigFingerRoot(4, "Pinky");

		//CJointDriver oJointFingerBoneParent = _oJointExtremity;
		//oJointFingerBoneParent = ConfigFingerBone(oJointFingerBoneParent, 0, 0, "Thumb", 005f, -030f,  005f,  025f);
		//oJointFingerBoneParent = ConfigFingerBone(oJointFingerBoneParent, 0, 1, "Thumb", 010f, -015f,  000f,  000f);
		//oJointFingerBoneParent = ConfigFingerBone(oJointFingerBoneParent, 0, 2, "Thumb", 010f, -015f,  000f,  000f);

		//=== Init Hotspot ===
		if (_eBodySide == 0)	//###IMPROVE: Find one side bone from the other's string?
			_oHotSpot = CHotSpot.CreateHotspot(this, _oBody.FindBone("chest/lCollar/lShldr/lForeArm/lHand"), "Left Hand", true, new Vector3(0, 0, 0));
		else
			_oHotSpot = CHotSpot.CreateHotspot(this, _oBody.FindBone("chest/rCollar/rShldr/rForeArm/rHand"), "Right Hand", true, new Vector3(0, 0, 0));

		//=== Init CObject ===
		_oObj = new CObject(this, _oBody._nBodyID, typeof(EActorArm), "Arm", "Arm" + _sSidePrefixU);
		_oObj.PropGroupBegin("", "", true);
		AddBaseActorProperties();						// The first properties of every CActor subclass are Pinned, pos & rot
		_oObj.PropAdd(EActorArm.HandTarget,			"HandTarget",			typeof(EHandTargets), (int)EHandTargets.ManualPosition, "", CProp.Local);
		_oObj.PropAdd(EActorArm.Hand_UpDown,		"Hand-UpDown",			0,	-100,	100, "", CProp.Local);
		_oObj.PropAdd(EActorArm.Hand_LeftRight,		"Hand-LeftRight",		0,	-100,	100, "", CProp.Local);
		_oObj.PropAdd(EActorArm.Hand_Twist,			"Hand-Twist",			0,	-100,	100, "", CProp.Local);
		_oObj.PropAdd(EActorArm.Fingers_Close,		"Fingers-Close",		0,	-100,	100, "", CProp.Local + CProp.Hide);		//###BROKEN
		_oObj.PropAdd(EActorArm.Fingers_Spread,		"Fingers-Spread",		0,	-100,	100, "", CProp.Local + CProp.Hide);
		_oObj.PropAdd(EActorArm.Fingers_ThumbPose,	"Fingers-ThumbPose",	typeof(EThumbPose), (int)EThumbPose.AlongsideFingers, "", CProp.Local + CProp.Hide);
		//_oObj.PropAdd(EActorArm.UserControl,		"User Control",			0,		0,		1, "", CProp.Local);
		_oObj.FinishInitialization();

		_oHandTarget_RaycastPin = CActorArm.FindHandTarget(_oBody, EHandTargets.RaycastPin, _eBodySide == EBodySide.Right);	// Find the raycast pin hand target as we use it heavily and it is reparented
	}

	void ConfigFingerRoot(int nFingerID, string sFingerName) {							//###WEAK: 3dsMax bone rotation for fingers need massive rework... these ranges kept limited to compensate... FIX MODEL!
		CJointDriver oJointFingerBoneParent = _oJointExtremity;
		oJointFingerBoneParent = ConfigFingerBone(oJointFingerBoneParent, nFingerID, 0, sFingerName, 020f, -020f, -009f,  000f);		// First  bone: Can twist about y also
		oJointFingerBoneParent = ConfigFingerBone(oJointFingerBoneParent, nFingerID, 1, sFingerName, 010f, -020f,  000f,  000f);		// Second bone: Only X
		oJointFingerBoneParent = ConfigFingerBone(oJointFingerBoneParent, nFingerID, 2, sFingerName, 010f, -020f,  000f,  000f);		// Third  bone: Only X (more limited)		//###CHECK: Joint free vs fixed affecting these??
	}
	
	CJointDriver ConfigFingerBone(CJointDriver oJointFingerBoneParent, int nFingerID, int nFingerBoneID, string sFingerName, float XL, float XH, float YR, float ZR) {
		CJointDriver oJointDrv = new CJointDriver(this, oJointFingerBoneParent, _sSidePrefixL+sFingerName + (nFingerBoneID+1).ToString(), 1.0f*C_DriveAng, 0.01f, XL,  XH, -YR,  YR,  -ZR,  ZR);		//###TODO: Calibrate different fingers specially!
		_aJoints.Add(oJointDrv);
		JointDrive oDrive = oJointDrv._oConfJoint.slerpDrive;
		oDrive.positionSpring = 1;						//###TUNE
		oDrive.positionDamper = 0;// .001f;				//***NOW!
		oJointDrv._oConfJoint.slerpDrive = oDrive;
		
		oJointDrv._oRigidBody.mass = 0.0001f;			//###TODO!
		oJointDrv._oRigidBody.drag = 0;					// Remove all damping so finger joint gets the least stress as possible to avoid 'disjointed rubber fingers'
		oJointDrv._oRigidBody.angularDrag = 0;
		CapsuleCollider oCollCap = (CapsuleCollider)CUtility.FindOrCreateComponent(oJointDrv._oTransform.gameObject, typeof(CapsuleCollider));
		oCollCap.direction = 2;			// z = 2 as per docs at http://docs.unity3d.com/Documentation/ScriptReference/CapsuleCollider-direction.html
		oCollCap.radius = 0.011f;
		oCollCap.height = 0.04f;		
		_aaFingers[nFingerID, nFingerBoneID] = oJointDrv;		
		return oJointDrv;
	}
	public static CHandTarget FindHandTarget(CBody oBody, EHandTargets eHandTarget, bool bRightSideOfBody) {
		string sNameHandTarget = "CHandTarget-" + eHandTarget.ToString();				// The enum name of hand target is prefixed by 'CHandTarget-' in the bone tree
		Transform oHandTargetT = oBody.SearchBone(oBody._oBonesT, sNameHandTarget);
		CHandTarget oHandTarget = oHandTargetT.GetComponent<CHandTarget>();
		if (bRightSideOfBody)		// The right side of body has 'create as needed' hand targets that are mirrored from the left side...
			oHandTarget = CHandTarget.FindOrCreateRightSideHandTarget(oHandTarget);
		return oHandTarget;
	}

	//---------------------------------------------------------------------------	ARM RAYCAST PINNING: User selecting where to place the closest arm of the selected body on a body surface through raycasting

	public void ArmRaycastPin_Begin() {
		_oHandTarget = _oHandTarget_RaycastPin;
		_oHandTarget.ConnectHandToHandTarget(this);
		_oHandTarget.transform.position = _oJointExtremity._oTransform.position;			// Manually set the hand target position to the hand position so slerp in Update doesn't start from some old stale position  ###MOVE?
		_oBodyArmPin = null;
		//_nArmPinBodyColVert = -1;
		//_nTimeStartPinSet = 0;			// Reset our start-of-slerp time to zero so it's initialize at first real pin position
		//_bPinByClosestVert = false;		// We start pinning by ray position as it's much more accurate.  When user releases pinning key we go to vert pinning which will handle changes in body orientation  ###IMPROVE: Always have 'ray precision' pinning by storing offset and rotating to closest vert normal?
		_oConfJoint_Extremity.angularXMotion = _oConfJoint_Extremity.angularYMotion = _oConfJoint_Extremity.angularZMotion = ConfigurableJointMotion.Free;	// We free rotation constraint so hand is just attraced by position (and repelled by colliders)  Greatly simplifies hand raycasting!!
	}
	public void ArmRaycastPin_Update() {
		//_oBodyArmPin = null;          ###F ###OBS ###BROKEN
		//_nArmPinBodyColVert = -1;

		//Ray oRay = Camera.main.ScreenPointToRay(Input.mousePosition);

		//foreach (CBody oBody in CGame.INSTANCE._aBodies) {		//###OPT!!: Expensive call currently and we throwaway most of the precision work!  Rethink!!
		//	if (oBody != null) {
		//		_nArmPinBodyColVert = ErosEngine.BodyCol_RayCast(oBody._oBodyCol._hBodyCol, oRay.origin, oRay.direction, _memVecRayHitInfo.P);		//###SIMPLIFY  ###CLEANUP: Extra stuff!
		//		if (_nArmPinBodyColVert != -1) {		// If a raycast vert was found pin this arm to the raycast-provided bodycol vert...
		//			_oBodyArmPin = oBody;
		//			//Debug.Log("Ray: " + _nArmPinBodyColVert + " = " + _memVecRayHitInfo.L[0] + _memVecRayHitInfo.L[1]);
		//			//GameObject.Find("(CGame)/(JUNK)/PIN_RAY") .transform.position = _memVecRayHitInfo.L[0];
		//			//GameObject.Find("(CGame)/(JUNK)/PIN_VERT").transform.position = _oBodyArmPin._oBodyCol._memVerts.L[_nArmPinBodyColVert];
		//			if (_nTimeStartPinSet == 0)			// Set the start-of-slerp time only once... the first time we have a valid position to pin to...
		//				_nTimeStartPinSet = Time.time;
		//			return;			//###WEAK!! This simple iteration will result in first body with valid raycast to be selected, even if further bodies are actually closer to camera
		//		}
		//	}
		//}
	}
	public void ArmRaycastPin_End() {						// Check at end of raycast procedure to unpin if nothing was found
		//_bPinByClosestVert = true;							// Go to vert-pinning mode now to finalize the pin (much less acurate but survives body movement)
		if (_oBodyArmPin == null) {							//###CHECK!! Correct cancellation??
			_oHandTarget.ConnectHandToHandTarget(null);		// Destroy arm from pin at end of raycast search if we're not connected to anything
			_oHandTarget = null;				//???
			_oObj.PropSet(EActorArm.Pinned, 0);		// Unpin??
			//_oConfJoint_Extremity.angularXMotion = _oConfJoint_Extremity.angularYMotion = _oConfJoint_Extremity.angularZMotion = ConfigurableJointMotion.Limited;	// If raycasting results in no pin to raycast vert we re-enable angular constraint??? ###TODO!!!! ###DESIGN!!
		}
	}

	//---------------------------------------------------------------------------	UPDATE
	public void OnSimulatePre() {			// Arms need per-frame update to handle pinned situations where we constantly set our pin position to a body collider vert
        //###F ###BROKEN (Body col!)
		//if (_oBodyArmPin != null && _oHandTarget != null) {			//###IMPROVE: Add 'walking' the body col mesh to 'caress' the mesh!
		//	Vector3 vecPinPos = _bPinByClosestVert ? _oBodyArmPin._oBodyCol._memVerts.L[_nArmPinBodyColVert] : _memVecRayHitInfo.L[0];		//###TODO!!!! ALWAYS SLERP!
		//	float nPercentSlerp = (Time.time - _nTimeStartPinSet) / C_TimeToMoveToNewPinPos;		//###LEARN: How to setup Vector.Slerp correctly... see E:\EG\Unity\Data\Documentation\Documentation\ScriptReference\Vector3.Slerp.html
		//	_oHandTarget.transform.position = Vector3.Slerp(_oHandTarget.transform.position, vecPinPos, nPercentSlerp);	// Constantly set our pin position to the body collider vertex  found during ArmRaycastPin_Update()
		//}

		////=== Set hand position when some keys are pressed ===
		//if (_oBody.IsBodySelected()) {						//###TODO	###HACK!!!
		//	if (Input.GetKeyDown(KeyCode.Alpha5))				//###IDEA: USE F1-F4?
		//		_oObj.PropSet(EActorArm.HandTarget, (int)EHandTargets.Head_Side);
		//	//if (Input.GetKeyDown(KeyCode.Alpha6))
		//	//	_oObj.PropSet(EActorArm.HandTarget, (int)EHandTargets.Breast_Side);
		//}
	}


	//---------------------------------------------------------------------------	COBJECT EVENTS

	public void OnPropSet_HandTarget(float nValueOld, float nValueNew) {
		if (_oHandTarget != null) {
			_oHandTarget.ConnectHandToHandTarget(null);		// If we were previously connected drop the connection now to let the hand go idle
			_oHandTarget = null;
		}
		EHandTargets eHandTarget = (EHandTargets)nValueNew;
		
		switch (eHandTarget) {
			case EHandTargets.ManualPosition:
				//###BROKEN!!! Conflict during init! _oObj.PropSet(EActorArm.Pinned, 1);				// Manual position means pinned
				_oConfJoint_Extremity.angularXMotion = _oConfJoint_Extremity.angularYMotion = _oConfJoint_Extremity.angularZMotion = ConfigurableJointMotion.Limited;	// Enable angular contraint so user can fully define hand pos/rot
				transform.parent = _oBody._oBaseT;				// Reparent the arm pin to the starting parent of 'Base'
				break;
			case EHandTargets.Idle:
				_oObj.PropSet(EActorArm.Pinned, 0);				// No hand target to go to, unpin to let gravity & arm drive simulate the arm
				transform.parent = _oBody._oBaseT;				// Reparent the arm pin to the starting parent of 'Base'
				break;
			default:
				_oHandTarget = CActorArm.FindHandTarget(_oBody, eHandTarget, _eBodySide == EBodySide.Right);
				_oHandTarget.ConnectHandToHandTarget(this);
				break;
		}

		//} else if (eHandTarget == EHandTargets.DumpPos_TEMP) {		//###OBS? Keep??
		//	_oJointShoulder	.DumpBonePos_DEV();
		//	_oJointMidLimb	.DumpBonePos_DEV();
		//	_oJointExtremity.DumpBonePos_DEV();

		//if (eHandTarget == EHandTargets.Driven_DEV) {			//###WEAK!!!?
		//	_oObj.PropSet(EActorArm.Pinned, 0);				// No hand target to go to, unpin to let gravity & arm drive simulate the arm
		//	transform.parent = _oBody._oBaseT;				// Reparent the arm pin to the starting parent of 'Base'
		//	_oJointShoulder	.SetRotationRaw_HACK(new Quaternion(-0.109f,0.592f,0.133f,0.787f));
		//	_oJointMidLimb	.SetRotationRaw_HACK(new Quaternion(-0.398f,0.818f,0.281f,0.307f));
		//	_oJointExtremity.SetRotationRaw_HACK(new Quaternion(-0.207f,0.124f,-0.200f,0.950f));
		//} else {
		//	_oJointShoulder	.SetRotationDefault_HACK();
		//	_oJointMidLimb	.SetRotationDefault_HACK();
		//	_oJointExtremity.SetRotationDefault_HACK();
		//}
	}

	public void OnPropSet_Hand_UpDown(float nValueOld, float nValueNew) { _oJointExtremity.RotateX_DualRange(nValueNew); }
	public void OnPropSet_Hand_LeftRight(float nValueOld, float nValueNew) { _oJointExtremity.RotateY_DualRange(nValueNew); }
	public void OnPropSet_Hand_Twist	(float nValueOld, float nValueNew) { _oJointExtremity.RotateZ_DualRange(nValueNew); }
	//public void OnPropSet_Fingers_Close	(float nValueOld, float nValueNew) {
	//	for (int nFingerID = 1; nFingerID < 5; nFingerID++)												// Thumb excluded
	//		for (int nFingerBoneID = 0; nFingerBoneID < 3; nFingerBoneID++)
	//			_aaFingers[nFingerID, nFingerBoneID].RotateX_DualRange(nValueNew);
	//}
	//public void OnPropSet_Fingers_Spread(float nValueOld, float nValueNew) {
	//	for (int nFingerID = 1; nFingerID < 5; nFingerID++) {											// Thumb excluded
	//		for (int nFingerBoneID = 0; nFingerBoneID < 3; nFingerBoneID++) {
	//			_aaFingers[nFingerID, nFingerBoneID].RotateY_DualRange(nValueNew * _aFingerSpreadMultiplier[nFingerID]);		// Apply a special 'multiplier' to finger Y to cause them to spread in opposite directions!
	//		}
	//	}
	//}
	//public void OnPropSet_Fingers_ThumbPose(float nValueOld, float nValueNew) {
	//	EThumbPose eThumbPose = (EThumbPose)nValueNew;
	//	switch (eThumbPose) {
	//		case EThumbPose.AlongsideFingers:
	//			_aaFingers[0, 0].RotateX_DualRange(000f);
	//			_aaFingers[0, 0].RotateZ_DualRange(000f);
	//			break;
	//		case EThumbPose.OppositeFingers:
	//			_aaFingers[0, 0].RotateX_DualRange(100f);
	//			_aaFingers[0, 0].RotateZ_DualRange(100f);
	//			break;
	//	}
	//}

	//public void OnPropSet_UserControl(float nValueOld, float nValueNew) {
	//	if (_oHandTarget != null)
	//		_oHandTarget.SetUserControl(nValueNew);
	//}
}

public enum EThumbPose {
	AlongsideFingers,
	OppositeFingers,
};

public enum EHandTargets {	//###OBS??? Extract from CHandTarget nodes??		// Design-time defined hand positions.  Each *must* match a 'CHandTarget-xxx' node somewhere in the bone tree
	ManualPosition,			// Manual position (default at init) is required so pinned arms correctly pin at startup
	Idle,
	Head_Side,
	//Breast_Side,
	//Breast_Under,
	//Stomach,
	//Hip_Side,
	//Crotch_Up,
	//Crotch_Side,
	//Penis,
	//Vagina,
	//Brace_Side,
	//Brace_Back,
	RaycastPin,
	//Driven_DEV,
	//DumpPos_TEMP,
};