using UnityEngine;
using System;
using System.Collections;

public class CActorChest : CActor {

	[HideInInspector]	public 	CJointDriver 		_oJointChestLower;
	[HideInInspector]	public 	CJointDriver 		_oJointAbdomenUpper;
	[HideInInspector]	public 	CJointDriver 		_oJointAbdomenLower;
    [HideInInspector]	public 	CJointDriver 		_oJointChestUpper;
	//[HideInInspector]	public 	CJointDriver 		_oJointPelvis;

	const float C_RndPose_ValShift = 10.0f;			// Randomization applied to important chest properties to avoid a still character		//###TUNE
	const float C_RndPos_TimeBetweenShifts = 3.0f;
	const int	C_RndPose_SmoothSteps = 20;

	//---------------------------------------------------------------------------	CREATE / DESTROY

	public override void OnStart_DefineLimb() {
		//=== Init Bones and Joints ===
		_aJoints.Add(_oJointExtremity	    = CJointDriver.Create(this, null,					"hip",				15,  8, -000,  000,  000,  000, 1));		//### The body's root bone... is kinematic and has no joint to a parent bone so all zeros
		_aJoints.Add(_oJointAbdomenLower    = CJointDriver.Create(this, _oJointExtremity,		"abdomenLower",	    15,  6, -040,  025,  024,  020, 1));
		_aJoints.Add(_oJointAbdomenUpper	= CJointDriver.Create(this, _oJointAbdomenLower,    "abdomenUpper",	    15,  6, -035,  025,  012,  020, 1));
		_aJoints.Add(_oJointChestLower      = CJointDriver.Create(this, _oJointAbdomenUpper,	"chestLower",		15,  8, -015,  015,  010,  010, 1));
		_aJoints.Add(_oJointChestUpper		= CJointDriver.Create(this, _oJointChestLower,		"chestUpper",	    15,  6, -035,  020,  015,  015, 1));

		//_aJoints.Add(_oJointExtremity	    = CJointDriver.Create(this, null,					"chestUpper",		15,  8, -000,  000,  000,  000, 1));		//### The body's root bone... is kinematic and has no joint to a parent bone so all zeros
		//_aJoints.Add(_oJointChestLower      = CJointDriver.Create(this, _oJointExtremity,		"chestLower",		15,  8, -015,  015,  010,  010, 1));
		//_aJoints.Add(_oJointAbdomenUpper	= CJointDriver.Create(this, _oJointChestLower,      "abdomenUpper",	    15,  6, -035,  025,  012,  020, 1));
		//_aJoints.Add(_oJointAbdomenLower    = CJointDriver.Create(this, _oJointAbdomenUpper,    "abdomenLower",	    15,  6, -040,  025,  024,  020, 1));
		//_aJoints.Add(_oJointHip			    = CJointDriver.Create(this, _oJointAbdomenLower,	"hip",			    15,  6, -035,  020,  015,  015, 1));
		//###NOTE: Pelvis bone is in CActorPelvis for extra processing there.

		//###CHECK: Keep? _oJointExtremity._oRigidBody.isKinematic = (CGame.INSTANCE._GameMode == EGameModes.Configure);		//###HACK ###TEMP ####REVA If play no anim just set everything to kinematic... for temp cloth exploration
		//###OBS? _oJointExtremity._oRigidBody.isKinematic = false;				// We need to disable kinematic on bone as we're driving it with our pin!

		//=== Init Hotspot ===
		_oHotSpot = CHotSpot.CreateHotspot(this, transform, "Chest", true, new Vector3(0, 0, 0), C_SizeHotSpot_BodyNodes);
		//_oHotSpot = CHotSpot.CreateHotspot(this, _oBody.FindBone("chestUpper"), "Chest", true, new Vector3(0, 0, 0), C_SizeHotSpot_TorsoNodes);

		//=== Init CObject ===
		_oObj = new CObject(this, "Chest", "Chest");
		CPropGrpEnum oPropGrp = new CPropGrpEnum(_oObj, "Arm", typeof(EActorChest));
		AddBaseActorProperties();						// The first properties of every CActor subclass are Pinned, pos & rot
		oPropGrp.PropAdd(EActorChest.Torso_LeftRight,	"Torso_LeftRight",	0,	-100,	100,	"");		//###BUG: Have to recalibrate to make new abdoment-centric bones look good.
		oPropGrp.PropAdd(EActorChest.Torso_UpDown,		"Torso_UpDown",		0,	-100,	100,	"");		//###NOW###: Forced to enter crappy name because of new CProp restrictions with naming!
		oPropGrp.PropAdd(EActorChest.Torso_Twist,		"Torso_Twist",		0,	-100,	100,	"");
		_oObj.FinishInitialization();
		//###PROBLEM: Base actor adds 'pinned' but chest is always pinned?

        //###CHECK??
		_oObj.PropSet(0, EActorChest.Pinned, 1);           // Manually set pinned to 1 on torso so body doesn't float in space when no pose is loaded (Weak that we can't set in PropAdd() due to init-time problems)

        if (CGame.INSTANCE.EnableIdlePoseMovement)
            StartCoroutine(Coroutine_ChangeRandomPose());			//###CHECK: When destroyed OK??
	}

	IEnumerator Coroutine_ChangeRandomPose() {	// Simple coroutine to efficiently apply randomization to some of our property values	###MOVE: Move to utility?
		float nTimeBetweenSmoothUpdates = C_RndPos_TimeBetweenShifts / (float)C_RndPose_SmoothSteps;
        //###BROKEN
		for (; ; ) {
			_oObj.PropFind(0, EActorChest.Torso_LeftRight)	.Randomize_SetNewRandomTarget(-C_RndPose_ValShift/2, C_RndPose_ValShift/2);		//###TUNE!!
			_oObj.PropFind(0, EActorChest.Torso_UpDown)		.Randomize_SetNewRandomTarget(-C_RndPose_ValShift, C_RndPose_ValShift);
			_oObj.PropFind(0, EActorChest.Torso_Twist)		.Randomize_SetNewRandomTarget(-C_RndPose_ValShift, C_RndPose_ValShift);

			for (int nSmoothStep = 0; nSmoothStep <= C_RndPose_SmoothSteps; nSmoothStep++) {
				float nPlaceInCycle = (float)nSmoothStep / (float)C_RndPose_SmoothSteps;

				_oObj.PropFind(0, EActorChest.Torso_LeftRight)	.Randomize_SmoothToTarget(nPlaceInCycle);
				_oObj.PropFind(0, EActorChest.Torso_UpDown)		.Randomize_SmoothToTarget(nPlaceInCycle);
				_oObj.PropFind(0, EActorChest.Torso_Twist)		.Randomize_SmoothToTarget(nPlaceInCycle);

				yield return new WaitForSeconds(nTimeBetweenSmoothUpdates);
			}
		}
	}

	
	//---------------------------------------------------------------------------	COBJECT EVENTS

	public void OnPropSet_Torso_LeftRight(float nValueOld, float nValueNew) {
        _oJointChestLower.  RotateZ2(nValueNew);
		_oJointAbdomenUpper.RotateZ2(nValueNew);
        _oJointAbdomenLower.RotateZ2(nValueNew);
		_oJointChestUpper.         RotateZ2(nValueNew);
		_oBody._oActor_Pelvis._oJointExtremity.RotateZ2(nValueNew);         //###CHECK: Cross-reach to pelvis bone??
    }
    public void OnPropSet_Torso_UpDown   (float nValueOld, float nValueNew) {
        _oJointChestLower.  RotateX2(nValueNew);
        _oJointAbdomenUpper.RotateX2(nValueNew);
        _oJointAbdomenLower.RotateX2(nValueNew);
		_oJointChestUpper.         RotateX2(nValueNew);
		_oBody._oActor_Pelvis._oJointExtremity.RotateX2(nValueNew);         //###CHECK: Cross-reach to pelvis bone??
	}
    public void OnPropSet_Torso_Twist    (float nValueOld, float nValueNew) {
        _oJointChestLower.  RotateY2(nValueNew);
        _oJointAbdomenUpper.RotateY2(nValueNew);
        _oJointAbdomenLower.RotateY2(nValueNew);
		_oJointChestUpper.         RotateY2(nValueNew);
		_oBody._oActor_Pelvis._oJointExtremity.RotateY2(nValueNew);			//###CHECK: Cross-reach to pelvis bone??
	}
}
