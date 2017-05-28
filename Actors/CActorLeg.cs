using UnityEngine;
using System;
using System.Collections;

public class CActorLeg : CActor {

	[HideInInspector]	public 	CBone 	_oBoneThighBend;
	[HideInInspector]	public 	CBone 	_oBoneThighTwist;
	[HideInInspector]	public 	CBone 	_oBoneShin;
	[HideInInspector]	public 	CBone 	_oBoneFoot;
	[HideInInspector]	public 	CBone 	_oBoneMetatarsals;									// Toe joint is called _oBoneExtremity and knee joint _oBoneMidLimb in base class as they require base-class processing

	//CKeyHook _oKeyHook_ThighSpread;


	//---------------------------------------------------------------------------	CREATE / DESTROY

	public override void OnStart_DefineLimb() {
		_nDrivePos = 0.5f * C_DrivePos;             // Weaken the leg pin drive over default

        //=== Init Bones and Joints ===
        CBone oBonePelvis = _oBody._oActor_Pelvis._oBoneExtremity;
		_aBones.Add(_oBoneThighBend 	= CBone.Connect(this, oBonePelvis,	    _chSidePrefixL+"ThighBend",      15, 3.0f, -115, 035, 000, 085, 1));	// X = Leg forward (-115) / back (35), Z = Thigh split (85) / together (20)  ###BAD: Super-important thigh split needed same value for thigh together makes no sense!  ###IMPROVE: 2nd joint?
		_aBones.Add(_oBoneThighTwist  = CBone.Connect(this, _oBoneThighBend,	_chSidePrefixL+"ThighTwist",     20, 2.0f, -000, 000, 075, 000, 1));	// Y = Thigh twist -75 to 75 Ok
		_aBones.Add(_oBoneShin	    = CBone.Connect(this, _oBoneThighTwist,	_chSidePrefixL+"Shin",	        10, 2.0f, -011, 155, 015, 005, 1));	// X = Knee bent (+155) or backward (-11),  Y = Knee rotate -25 to 10 with compromise at 15, Z = Knee side-to-side -5 to 5
		_aBones.Add(_oBoneFoot	    = CBone.Connect(this, _oBoneShin,		_chSidePrefixL+"Foot",	        15, 1.0f, -025, 010, 020, 040, 1));	// X = Foot up (-40) and down (+75), inward -25 to +10, twist -35 to 15:			###IMPROVE: FootX not up/down!!  ###IMPROVE: 2 joints?  ###PROBLEM: Foot up down on Z with poor bending along down
		_aBones.Add(_oBoneMetatarsals = CBone.Connect(this, _oBoneFoot,		_chSidePrefixL+"Metatarsals",	20, 0.5f, -000, 000, 020, 012, 1));	// X = Metatarsals left/right = useless, Y = twist -20 to 20, Z = foot bend -12 to 12
		_aBones.Add(_oBoneExtremity   = CBone.Connect(this, _oBoneMetatarsals, _chSidePrefixL+"Toe",	        20, 3.0f, -000, 000, 020, 050, 1));	// X = Toes bend backward (-45) to forward (65),  Y = Toe twist -20/20, Z = meaningless   ###IMPROVE: Rotate axe?  ###CHECK: Toe mass??
		
		//###BROKEN21: _oBoneShin._oConfJoint.targetRotation = Quaternion.Euler(-160, 0, 0);		//###WEAK: Set knee default position so it can easily bend (with very week drive)		//###SOON: Proper bending of knee calculations from height

		//=== Init Hotspot ===
		if (_eBodySide == EBodySide.Left)            //###IMPROVE: Rediculously long path... switch to 'search for bone' (would not be as fast tho)
			_oHotSpot = CHotSpot.CreateHotspot(this, _oBody._oBodyBase.FindBone("hip/pelvis/lThighBend/lThighTwist/lShin/lFoot/lMetatarsals/lToe"), "Left Leg", true, new Vector3(0, 0, 0));
		else
			_oHotSpot = CHotSpot.CreateHotspot(this, _oBody._oBodyBase.FindBone("hip/pelvis/rThighBend/rThighTwist/rShin/rFoot/rMetatarsals/rToe"), "Right Leg", true, new Vector3(0, 0, 0));

		//=== Init CObject ===
		_oObj = new CObject(this, "Leg" + _chSidePrefixU, "Leg" + _chSidePrefixU);		//###PROBLEM19: Name for scripting and label name!
		CPropGrpEnum oPropGrp = new CPropGrpEnum(_oObj, "Leg", typeof(EActorLeg));
		AddBaseActorProperties();						// The first properties of every CActor subclass are Pinned, pos & rot
		oPropGrp.PropAdd(EActorLeg.Thigh_Spread,	"Thigh-Spread",		0,	-100,	100,	"");
		oPropGrp.PropAdd(EActorLeg.Thigh_Rotate,	"Thigh-Rotate",		0,	-100,	100,	"");
		_oObj.FinishInitialization();

		//_oKeyHook_ThighSpread = new CKeyHook(_oObj.PropFind(0, EActorLeg.Thigh_Spread), KeyCode.F, EKeyHookType.QuickMouseEdit, "Thigh Spread", -1);
	}

	public override void OnDestroy() {
		//_oKeyHook_ThighSpread.Dispose();
		base.OnDestroy();
	}

	//---------------------------------------------------------------------------	COBJECT EVENTS

	public void OnPropSet_Thigh_Spread(float nValueOld, float nValueNew) {
        _oBoneThighBend.RotateZ2(nValueNew);          //###BROKEN
    }
	public void OnPropSet_Thigh_Rotate(float nValueOld, float nValueNew) {
        _oBoneThighBend.RotateY2(nValueNew);          //###BROKEN
    }
}
