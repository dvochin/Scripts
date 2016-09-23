using UnityEngine;
using System;
using System.Collections;

public class CActorLeg : CActor {

	[HideInInspector]	public 	CJointDriver 	_oJointThighBend;
	[HideInInspector]	public 	CJointDriver 	_oJointThighTwist;
	[HideInInspector]	public 	CJointDriver 	_oJointShin;
	[HideInInspector]	public 	CJointDriver 	_oJointFoot;
	[HideInInspector]	public 	CJointDriver 	_oJointMetatarsals;									// Toe joint is called _oJointExtremity and knee joint _oJointMidLimb in base class as they require base-class processing

	CKeyHook _oKeyHook_ThighSpread;


	//---------------------------------------------------------------------------	CREATE / DESTROY

	public override void OnStart_DefineLimb() {
		_nDrivePos = 0.5f * C_DrivePos;             // Weaken the leg pin drive over default

        //=== Init Bones and Joints ===
        CJointDriver oJointPelvis = _oBody._oActor_Pelvis._oJointExtremity;
		_aJoints.Add(_oJointThighBend 	= CJointDriver.Create(this, oJointPelvis,	    _sSidePrefixL+"ThighBend",      20f, 3f, -000f,  120f, 015f, 020f));
		_aJoints.Add(_oJointThighTwist  = CJointDriver.Create(this, _oJointThighBend,	_sSidePrefixL+"ThighTwist",     20f, 3f,  000f,  000f, 020f, 000f));
		_aJoints.Add(_oJointShin	    = CJointDriver.Create(this, _oJointThighTwist,	_sSidePrefixL+"Shin",	        05f, 2f, -160f,  000f, 001f, 001f));
		_aJoints.Add(_oJointFoot	    = CJointDriver.Create(this, _oJointShin,		_sSidePrefixL+"Foot",	        01f, 1f, -030f,  040f, 020f, 020f));
		_aJoints.Add(_oJointMetatarsals = CJointDriver.Create(this, _oJointFoot,		_sSidePrefixL+"Metatarsals",	01f, 1f, -030f,  040f, 020f, 020f));
		_aJoints.Add(_oJointExtremity   = CJointDriver.Create(this, _oJointMetatarsals, _sSidePrefixL+"Toe",	        01f, 3f, -020f,  060f, 001f, 001f));
		
		_oJointShin._oConfJoint.targetRotation = Quaternion.Euler(-160, 0, 0);		//###WEAK: Set knee default position so it can easily bend (with very week drive)		//###SOON: Proper bending of knee calculations from height

		//=== Init Hotspot ===
		if (_eBodySide == 0)            //###IMPROVE: Rediculously long path... switch to 'search for bone' (would not be as fast tho)
			_oHotSpot = CHotSpot.CreateHotspot(this, _oBody.FindBone("chestUpper/chestLower/abdomenUpper/abdomenLower/hip/pelvis/lThighBend/lThighTwist/lShin/lFoot/lMetatarsals/lToe"), "Left Leg", true, new Vector3(0, 0, 0));
		else
			_oHotSpot = CHotSpot.CreateHotspot(this, _oBody.FindBone("chestUpper/chestLower/abdomenUpper/abdomenLower/hip/pelvis/rThighBend/rThighTwist/rShin/rFoot/rMetatarsals/rToe"), "Right Leg", true, new Vector3(0, 0, 0));

		//=== Init CObject ===
		_oObj = new CObject(this, _oBody._nBodyID, typeof(EActorLeg), "Leg", "Leg" + _sSidePrefixU);
		_oObj.PropGroupBegin("", "", true);
		AddBaseActorProperties();						// The first properties of every CActor subclass are Pinned, pos & rot
		_oObj.PropAdd(EActorLeg.Thigh_Spread,	"Thigh-Spread",		0,	-100,	100,	"", CProp.Local);
		_oObj.PropAdd(EActorLeg.Thigh_Rotate,	"Thigh-Rotate",		0,	-100,	100,	"", CProp.Local);
		_oObj.FinishInitialization();

		_oKeyHook_ThighSpread = new CKeyHook(_oObj.PropFind(EActorLeg.Thigh_Spread), KeyCode.F, EKeyHookType.QuickMouseEdit, "Thigh Spread", -1);
	}

	public override void OnDestroy() {
		_oKeyHook_ThighSpread.Dispose();
		base.OnDestroy();
	}

	//---------------------------------------------------------------------------	COBJECT EVENTS

	public void OnPropSet_Thigh_Spread(float nValueOld, float nValueNew) {
        _oJointThighBend.RotateZ2(nValueNew);          //###BROKEN
    }
	public void OnPropSet_Thigh_Rotate(float nValueOld, float nValueNew) {
        _oJointThighBend.RotateY2(nValueNew);          //###BROKEN
    }
}
