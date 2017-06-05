using UnityEngine;


public class CActorLeg : CActor {

	[HideInInspector]	public 	CBone 	_oBoneThighBend;
	[HideInInspector]	public 	CBone 	_oBoneThighTwist;
	[HideInInspector]	public 	CBone 	_oBoneShin;
	[HideInInspector]	public 	CBone 	_oBoneFoot;
	[HideInInspector]	public 	CBone 	_oBoneMetatarsals;									// Toe joint is called _oBoneExtremity and knee joint _oBoneMidLimb in base class as they require base-class processing


	//---------------------------------------------------------------------------	CREATE / DESTROY

	public override void OnStart_DefineLimb() {
		_nDrivePinToBone = 0.5f * C_DrivePinToBone;             // Weaken the leg pin drive over default

        //=== Init Bones and Joints ===
        CBone oBonePelvis = _oBody._oActor_Pelvis._oBoneExtremity;
		float n = 1f;		//###DEV22:???
		_aBones.Add(_oBoneThighBend 	= CBone.Connect(this, oBonePelvis,	    _chSidePrefixL+"ThighBend",     n*10, 3.0f));
		_aBones.Add(_oBoneThighTwist	= CBone.Connect(this, _oBoneThighBend,	_chSidePrefixL+"ThighTwist",	n, 2.0f));
		_aBones.Add(_oBoneShin			= CBone.Connect(this, _oBoneThighTwist,	_chSidePrefixL+"Shin",	        n, 2.0f));
		_aBones.Add(_oBoneFoot			= CBone.Connect(this, _oBoneShin,		_chSidePrefixL+"Foot",	        n, 1.0f));
		_aBones.Add(_oBoneMetatarsals	= CBone.Connect(this, _oBoneFoot,		_chSidePrefixL+"Metatarsals",	n, 0.5f));
		_aBones.Add(_oBoneExtremity		= CBone.Connect(this, _oBoneMetatarsals, _chSidePrefixL+"Toe",	        n, 3.0f));

		float nThighAngle = 30;					//###HACK22: Thigh opening angle so spreading occurs easier during posing ###TUNE
		if (_eBodySide == EBodySide.Left)
			nThighAngle = -nThighAngle;
		_oBoneThighBend._oConfJoint.targetRotation = Quaternion.Euler(0, 0, nThighAngle);		//###WEAK: Set thigh opening angle so character opens his/her legs more easily :)
		_oBoneShin.		_oConfJoint.targetRotation = Quaternion.Euler(-160, 0, 0);		//###WEAK: Set knee default position so it can easily bend (with very week drive)		//###SOON: Proper bending of knee calculations from height  ###IMPROVE: Use bone rotation value!

		//=== Init Hotspot ===
		if (_eBodySide == EBodySide.Left)            //###IMPROVE: Rediculously long path... switch to 'search for bone' (would not be as fast tho)
			_oHotSpot = CHotSpot.CreateHotspot(this, _oBody._oBodyBase.FindBone("hip/pelvis/lThighBend/lThighTwist/lShin/lFoot/lMetatarsals/lToe"), "Left Leg", true, new Vector3(0, 0, 0));
		else
			_oHotSpot = CHotSpot.CreateHotspot(this, _oBody._oBodyBase.FindBone("hip/pelvis/rThighBend/rThighTwist/rShin/rFoot/rMetatarsals/rToe"), "Right Leg", true, new Vector3(0, 0, 0));

		//=== Init CObject ===
		_oObj = new CObject(this, "Leg" + _chSidePrefixU, "Leg" + _chSidePrefixU);		//###PROBLEM19: Name for scripting and label name!
		CPropGrpEnum oPropGrp = new CPropGrpEnum(_oObj, "Leg", typeof(EActorLeg));
		AddBaseActorProperties();						// The first properties of every CActor subclass are Pinned, pos & rot
		//oPropGrp.PropAdd(EActorLeg.Thigh_Spread,	"Thigh-Spread",		0,	-100,	100,	"");	//###OBS22:??  Rely on toe rotation instead?
		//oPropGrp.PropAdd(EActorLeg.Thigh_Rotate,	"Thigh-Rotate",		0,	-100,	100,	"");
		_oObj.FinishInitialization();

		//_oKeyHook_ThighSpread = new CKeyHook(_oObj.PropFind(0, EActorLeg.Thigh_Spread), KeyCode.F, EKeyHookType.QuickMouseEdit, "Thigh Spread", -1);
	}

	public override void OnDestroy() {
		base.OnDestroy();
	}

	//---------------------------------------------------------------------------	COBJECT EVENTS

	//public void OnPropSet_Thigh_Spread(float nValueOld, float nValueNew) {		//###OBS?
 //       _oBoneThighBend.RotateZ2(nValueNew);          //###BROKEN
 //   }
	//public void OnPropSet_Thigh_Rotate(float nValueOld, float nValueNew) {
 //       _oBoneThighBend.RotateY2(nValueNew);          //###BROKEN
 //   }
}
