using UnityEngine;
using System;
using System.Collections;

public class CActorChest : CActor {

	[HideInInspector]	public 	CJointDriver 		_oJointChestLower;
	[HideInInspector]	public 	CJointDriver 		_oJointAbdomenUpper;
	[HideInInspector]	public 	CJointDriver 		_oJointAbdomenLower;
    [HideInInspector]	public 	CJointDriver 		_oJointHip;
	//[HideInInspector]	public 	CJointDriver 		_oJointPelvis;

	const float C_RndPose_ValShift = 10.0f;			// Randomization applied to important chest properties to avoid a still character		//###TUNE
	const float C_RndPos_TimeBetweenShifts = 3.0f;
	const int	C_RndPose_SmoothSteps = 20;

	//---------------------------------------------------------------------------	CREATE / DESTROY

	public override void OnStart_DefineLimb() {
		//=== Init Bones and Joints ===
		_aJoints.Add(_oJointExtremity	    = CJointDriver.Create(this, null,					"chestUpper",		50f,  10f, -000f,  000f,  010f,  020f));
		_aJoints.Add(_oJointChestLower      = CJointDriver.Create(this, _oJointExtremity,		"chestLower",		50f,  10f, -000f,  000f,  010f,  020f));
		_aJoints.Add(_oJointAbdomenUpper	= CJointDriver.Create(this, _oJointChestLower,      "abdomenUpper",	    50f,  06f, -020f,  020f,  010f,  020f));
		_aJoints.Add(_oJointAbdomenLower    = CJointDriver.Create(this, _oJointAbdomenUpper,    "abdomenLower",	    50f,  06f, -020f,  020f,  010f,  020f));
		_aJoints.Add(_oJointHip			    = CJointDriver.Create(this, _oJointAbdomenLower,	"hip",			    50f,  07f, -020f,  020f,  010f,  020f));
		//_aJoints.Add(_oJointPelvis		    = CJointDriver.Create(this, _oJointHip,			"pelvis",			50.0f, 10.0f, -020f,  020f, -030f,  030f, -030f,  030f));

		_oJointExtremity._oRigidBody.isKinematic = (CGame.INSTANCE._GameMode == EGameModes.Configure);		//###HACK ###TEMP ####REVA If play no anim just set everything to kinematic... for temp cloth exploration
		//_oJointExtremity._oRigidBody.isKinematic = false;				// We need to disable kinematic on bone as we're driving it with our pin!

		////transform.localPosition = new Vector3(0, 2.0f, 0);		//###HACK?  ###IMPROVE?? How to deal with the annoying body center not at feet?? (Not working anyways)

		//=== Init Hotspot ===
		_oHotSpot = CHotSpot.CreateHotspot(this, transform, "Chest", true, new Vector3(0, 0, 0), C_SizeHotSpot_BodyNodes);
		//_oHotSpot = CHotSpot.CreateHotspot(this, _oBody.FindBone("chest"), "Chest", true, new Vector3(0, 0, 0), C_SizeHotSpot_TorsoNodes);

		//=== Init CObject ===
		_oObj = new CObject(this, _oBody._nBodyID, typeof(EActorChest), "Chest", "Chest");
		_oObj.PropGroupBegin("", "", true);
		AddBaseActorProperties();						// The first properties of every CActor subclass are Pinned, pos & rot
		_oObj.PropAdd(EActorChest.Chest_LeftRight,	"Chest-LeftRight",	0,	-100,	100,	"", CProp.Local);		//###BUG: Have to recalibrate to make new abdoment-centric bones look good.
		_oObj.PropAdd(EActorChest.Chest_UpDown,		"Chest-UpDown",		0,	-100,	100,	"", CProp.Local);
		_oObj.PropAdd(EActorChest.Chest_Twist,		"Chest-Twist",		0,	-100,	100,	"", CProp.Local);
		_oObj.FinishInitialization();

        //###CHECK??
		_oObj.PropSet(EActorChest.Pinned, 1);           // Manually set pinned to 1 on torso so body doesn't float in space when no pose is loaded (Weak that we can't set in PropAdd() due to init-time problems)

        if (CGame.INSTANCE.EnableIdlePoseMovement)
            StartCoroutine(Coroutine_ChangeRandomPose());			//###CHECK: When destroyed OK??
	}

	IEnumerator Coroutine_ChangeRandomPose() {	// Simple coroutine to efficiently apply randomization to some of our property values	###MOVE: Move to utility?
		float nTimeBetweenSmoothUpdates = C_RndPos_TimeBetweenShifts / (float)C_RndPose_SmoothSteps;
        //###BROKEN
		for (; ; ) {
			_oObj.PropFind(EActorChest.Chest_LeftRight)	.Randomize_SetNewRandomTarget(-C_RndPose_ValShift/2, C_RndPose_ValShift/2);		//###TUNE!!
			_oObj.PropFind(EActorChest.Chest_UpDown)	.Randomize_SetNewRandomTarget(-C_RndPose_ValShift, C_RndPose_ValShift);
			_oObj.PropFind(EActorChest.Chest_Twist)		.Randomize_SetNewRandomTarget(-C_RndPose_ValShift, C_RndPose_ValShift);

			for (int nSmoothStep = 0; nSmoothStep <= C_RndPose_SmoothSteps; nSmoothStep++) {
				float nPlaceInCycle = (float)nSmoothStep / (float)C_RndPose_SmoothSteps;

				_oObj.PropFind(EActorChest.Chest_LeftRight)	.Randomize_SmoothToTarget(nPlaceInCycle);
				_oObj.PropFind(EActorChest.Chest_UpDown)	.Randomize_SmoothToTarget(nPlaceInCycle);
				_oObj.PropFind(EActorChest.Chest_Twist)		.Randomize_SmoothToTarget(nPlaceInCycle);

				yield return new WaitForSeconds(nTimeBetweenSmoothUpdates);
			}
		}
	}

	
	//---------------------------------------------------------------------------	COBJECT EVENTS

	public void OnPropSet_Chest_LeftRight(float nValueOld, float nValueNew) {
        _oJointChestLower.  RotateY2(nValueNew);
        _oJointAbdomenUpper.RotateY2(nValueNew);
        _oJointAbdomenLower.RotateY2(nValueNew);
		_oJointHip.         RotateY2(nValueNew);
		//_oJointPelvis.      RotateY_DualRange(nValueNew);
    }
    public void OnPropSet_Chest_UpDown   (float nValueOld, float nValueNew) {
        _oJointChestLower.  RotateX2(nValueNew);
        _oJointAbdomenUpper.RotateX2(nValueNew);
        _oJointAbdomenLower.RotateX2(nValueNew);
		_oJointHip.         RotateX2(nValueNew);
        //_oJointPelvis.      RotateX_DualRange(nValueNew);
    }
    public void OnPropSet_Chest_Twist    (float nValueOld, float nValueNew) {
        _oJointChestLower.  RotateZ2(nValueNew);
		_oJointAbdomenUpper.RotateZ2(nValueNew);
        _oJointAbdomenLower.RotateZ2(nValueNew);
		_oJointHip.         RotateZ2(nValueNew);
        //_oJointPelvis.      RotateZ_DualRange(nValueNew);
    }
}
