
using UnityEngine;


public class CActorLeg : CActorLimb {

	[HideInInspector]	public 	CBone 	_oBoneThighBend;
	[HideInInspector]	public 	CBone 	_oBoneThighTwist;
	[HideInInspector]	public 	CBone 	_oBoneShin;
	[HideInInspector]	public 	CBone 	_oBoneFoot;
	[HideInInspector]	public 	CBone 	_oBoneMetatarsals;									// Toe joint is called _oBoneExtremity and knee joint _oBoneMidLimb in base class as they require base-class processing


	//---------------------------------------------------------------------------	CREATE / DESTROY

	public override void OnStart_DefineLimb() {
		_bBakeJointAnglesWhenMovingPin = true;              // Enable this limb to 'bake' each joint angles as the pin is moved.  Greatly stabilizes the limb during gameplay as the hard work of setting joint angles has been done by the pose designer
		///_nDrivePinToBone = 0.5f * C_DrivePinToBone;             // Weaken the leg pin drive over default

		//=== Init Bones and Joints ===
		CBone oBonePelvis = _oBodyBase._oActor_Pelvis._oBoneExtremity;
		float n = 1f;		//###DESIGN:!!!
		_aBones.Add(_oBoneThighBend 	= CBone.Connect(this, oBonePelvis,	    _chSidePrefixL+"ThighBend",     5.0f, CBone.EBoneType.Default));
		_aBones.Add(_oBoneThighTwist	= CBone.Connect(this, _oBoneThighBend,	_chSidePrefixL+"ThighTwist",	4.0f, CBone.EBoneType.Twister));
        _aBones.Add(_oBoneShin			= CBone.Connect(this, _oBoneThighTwist,	_chSidePrefixL+"Shin",	        4.0f, CBone.EBoneType.Bender));
        _aBones.Add(_oBoneFoot			= CBone.Connect(this, _oBoneShin,		_chSidePrefixL+"Foot",	        1.0f, CBone.EBoneType.Bender));     //#DEV26:
        _aBones.Add(_oBoneMetatarsals	= CBone.Connect(this, _oBoneFoot,		_chSidePrefixL+"Metatarsals",	0.5f, CBone.EBoneType.Default));
        _aBones.Add(_oBoneExtremity		= CBone.Connect(this, _oBoneMetatarsals, _chSidePrefixL+"Toe",	        0.1f, CBone.EBoneType.Extremity));

        _oBoneShin.RotateX(-60);                 // Bend knee halfway by default so body 'folds' more easily.

        //float nThighAngle = 30;					//###HACK22: Thigh opening angle so spreading occurs easier during posing ###TUNE #DEV26: Use CBone now?
        //if (_eBodySide == EBodySide.Left)
        //	nThighAngle = -nThighAngle;
        //_oBoneThighBend._oJointD6.targetRotation = Quaternion.Euler(0, 0, nThighAngle);		//###WEAK: Set thigh opening angle so character opens his/her legs more easily :)
        //_oBoneShin.		_oJointD6.targetRotation = Quaternion.Euler(-160, 0, 0);		//###WEAK: Set knee default position so it can easily bend (with very week drive)		//###SOON: Proper bending of knee calculations from height  ###IMPROVE: Use bone rotation value!

        //=== Init Hotspot ===
  //      if (_eBodySide == EBodySide.Left)            //###IMPROVE: Rediculously long path... switch to 'search for bone' (would not be as fast tho)
		//	_oHotSpot = CHotSpot.CreateHotspot(this, _oBodyBase.FindBone("hip/pelvis/lThighBend/lThighTwist/lShin/lFoot/lMetatarsals/lToe"), "Left Leg", true, G.C_Layer_HotSpot);
		//else
		//	_oHotSpot = CHotSpot.CreateHotspot(this, _oBodyBase.FindBone("hip/pelvis/rThighBend/rThighTwist/rShin/rFoot/rMetatarsals/rToe"), "Right Leg", true, G.C_Layer_HotSpot);

		//=== Init CObj ===
		_oObj = new CObj(null, "Leg" + _chSidePrefixU);		//###PROBLEM19: Name for scripting and label name!
		AddBaseActorProperties();						// The first properties of every CActor subclass are Pinned, pos & rot
		//_oObj.Add(EActorLeg.Thigh_Spread,	"Thigh-Spread",		0,	-100,	100,	"");	//###OBS22:??  Rely on toe rotation instead?
		//_oObj.Add(EActorLeg.Thigh_Rotate,	"Thigh-Rotate",		0,	-100,	100,	"");

		//_oKeyHook_ThighSpread = new CKeyHook(_oObj.Find(0, EActorLeg.Thigh_Spread), KeyCode.F, EKeyHookType.QuickMouseEdit, "Thigh Spread", -1);
	}

	public override void OnDestroy() {
		base.OnDestroy();
	}

    protected override void PinToExtremity_ConfigureJoint(JointDrive oJointDriveSlerp) {
        base.PinToExtremity_ConfigureJoint(oJointDriveSlerp);
        oJointDriveSlerp.positionSpring = 100.0f;         //###DESIGN:!!! Make spring stiffness for leg pins much stiffer!
        //_oJoint_Extremity.angularXMotion = _oJoint_Extremity.angularYMotion = _oJoint_Extremity.angularZMotion = ConfigurableJointMotion.Free;      //###DESIGN:!!! Feet pins with no rotation limits??
        ////////_oJoint_Extremity.angularZMotion = ConfigurableJointMotion.Free;            // Limit hand joint to X,Y motion while leaving Z free to rotate like a hinge.  This is essential for PhysX to auto-calculate how to best position entire arm and hand to most easily reach pin point  #DEV26:
    }

    //---------------------------------------------------------------------------	COBJECT EVENTS

    //public void OnSet_Thigh_Spread(float nValueOld, float nValueNew) {		//###OBS?
    //       _oBoneThighBend.RotateZ2(nValueNew);          //###BROKEN
    //   }
    //public void OnSet_Thigh_Rotate(float nValueOld, float nValueNew) {
    //       _oBoneThighBend.RotateY2(nValueNew);          //###BROKEN
    //   }
}
