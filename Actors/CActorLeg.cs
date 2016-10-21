using UnityEngine;
using System;
using System.Collections;

public class CActorLeg : CActor {

	[HideInInspector]	public 	CJointDriver 	_oJointThighBend;
	[HideInInspector]	public 	CJointDriver 	_oJointThighTwist;
	[HideInInspector]	public 	CJointDriver 	_oJointShin;
	[HideInInspector]	public 	CJointDriver 	_oJointFoot;
	[HideInInspector]	public 	CJointDriver 	_oJointMetatarsals;									// Toe joint is called _oJointExtremity and knee joint _oJointMidLimb in base class as they require base-class processing

	//CKeyHook _oKeyHook_ThighSpread;


	//---------------------------------------------------------------------------	CREATE / DESTROY

	public override void OnStart_DefineLimb() {
		_nDrivePos = 0.5f * C_DrivePos;             // Weaken the leg pin drive over default

        //=== Init Bones and Joints ===
        CJointDriver oJointPelvis = _oBody._oActor_Pelvis._oJointExtremity;
		_aJoints.Add(_oJointThighBend 	= CJointDriver.Create(this, oJointPelvis,	    _sSidePrefixL+"ThighBend",      15, 3.0f, -115, 035, 000, 085, 1));	// X = Leg forward (-115) / back (35), Z = Thigh split (85) / together (20)  ###BAD: Super-important thigh split needed same value for thigh together makes no sense!  ###IMPROVE: 2nd joint?
		_aJoints.Add(_oJointThighTwist  = CJointDriver.Create(this, _oJointThighBend,	_sSidePrefixL+"ThighTwist",     20, 2.0f, -000, 000, 075, 000, 1));	// Y = Thigh twist -75 to 75 Ok
		_aJoints.Add(_oJointShin	    = CJointDriver.Create(this, _oJointThighTwist,	_sSidePrefixL+"Shin",	        10, 2.0f, -011, 155, 015, 005, 1));	// X = Knee bent (+155) or backward (-11),  Y = Knee rotate -25 to 10 with compromise at 15, Z = Knee side-to-side -5 to 5
		_aJoints.Add(_oJointFoot	    = CJointDriver.Create(this, _oJointShin,		_sSidePrefixL+"Foot",	        15, 1.0f, -025, 010, 020, 040, 1));	// X = Foot up (-40) and down (+75), inward -25 to +10, twist -35 to 15:			###IMPROVE: FootX not up/down!!  ###IMPROVE: 2 joints?  ###PROBLEM: Foot up down on Z with poor bending along down
		_aJoints.Add(_oJointMetatarsals = CJointDriver.Create(this, _oJointFoot,		_sSidePrefixL+"Metatarsals",	20, 0.5f, -000, 000, 020, 012, 1));	// X = Metatarsals left/right = useless, Y = twist -20 to 20, Z = foot bend -12 to 12
		_aJoints.Add(_oJointExtremity   = CJointDriver.Create(this, _oJointMetatarsals, _sSidePrefixL+"Toe",	        20, 3.0f, -000, 000, 020, 050, 1));	// X = Toes bend backward (-45) to forward (65),  Y = Toe twist -20/20, Z = meaningless   ###IMPROVE: Rotate axe?  ###CHECK: Toe mass??
		
		_oJointShin._oConfJoint.targetRotation = Quaternion.Euler(-160, 0, 0);		//###WEAK: Set knee default position so it can easily bend (with very week drive)		//###SOON: Proper bending of knee calculations from height

		//=== Init Hotspot ===
		if (_eBodySide == 0)            //###IMPROVE: Rediculously long path... switch to 'search for bone' (would not be as fast tho)
			_oHotSpot = CHotSpot.CreateHotspot(this, _oBody._oBodyBase.FindBone("chestUpper/chestLower/abdomenUpper/abdomenLower/hip/pelvis/lThighBend/lThighTwist/lShin/lFoot/lMetatarsals/lToe"), "Left Leg", true, new Vector3(0, 0, 0));
		else
			_oHotSpot = CHotSpot.CreateHotspot(this, _oBody._oBodyBase.FindBone("chestUpper/chestLower/abdomenUpper/abdomenLower/hip/pelvis/rThighBend/rThighTwist/rShin/rFoot/rMetatarsals/rToe"), "Right Leg", true, new Vector3(0, 0, 0));

		//=== Init CObject ===
		_oObj = new CObject(this, _oBody._oBodyBase._nBodyID, typeof(EActorLeg), "Leg", "Leg" + _sSidePrefixU);
		_oObj.PropGroupBegin("", "", true);
		AddBaseActorProperties();						// The first properties of every CActor subclass are Pinned, pos & rot
		_oObj.PropAdd(EActorLeg.Thigh_Spread,	"Thigh-Spread",		0,	-100,	100,	"");
		_oObj.PropAdd(EActorLeg.Thigh_Rotate,	"Thigh-Rotate",		0,	-100,	100,	"");
		_oObj.FinishInitialization();

		//_oKeyHook_ThighSpread = new CKeyHook(_oObj.PropFind(EActorLeg.Thigh_Spread), KeyCode.F, EKeyHookType.QuickMouseEdit, "Thigh Spread", -1);
	}

	public override void OnDestroy() {
		//_oKeyHook_ThighSpread.Dispose();
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
