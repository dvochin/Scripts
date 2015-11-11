using UnityEngine;
using System;
using System.Collections;

public class CActorLeg : CActor {

	[HideInInspector]	public 	CJointDriver 	_oJointThigh;
	[HideInInspector]	public 	CJointDriver 	_oJointFoot;									// Toe joint is called _oJointExtremity and knee joint _oJointMidLimb in base class as they require base-class processing

	CKeyHook _oKeyHook_ThighSpread;


	//---------------------------------------------------------------------------	CREATE / DESTROY

	public override void OnStart_DefineLimb() {
		_nDrivePos = 0.5f * C_DrivePos;				// Weaken the leg pin drive over default

		//=== Init Bones and Joints ===
		_aJoints.Add(_oJointThigh 	 = new CJointDriver(this, _oBody._oActor_Chest._oJointHip,	_sSidePrefixL+"Thigh", 20.0f*C_DriveAng, 3.0f, -000f,  120f, -015f,  015f, -020f,  090));	//###CHECK: Reduced y by half as rotated thighs look really bad!	###CHECK: Why need for so much strenght??
		_aJoints.Add(_oJointMidLimb	 = new CJointDriver(this, _oJointThigh,						_sSidePrefixL+"Shin",	5.0f*C_DriveAng, 2.0f, -160f,  000f, -001f,  001f, -001f,  001f));		//###TODO: Knee twist??
		_aJoints.Add(_oJointFoot	 = new CJointDriver(this, _oJointMidLimb,					_sSidePrefixL+"Foot",	1.0f*C_DriveAng, 1.0f, -030f,  040f, -020f,  020f, -020f,  020f));		//###TODO: Food doesn't look very good going up
		_aJoints.Add(_oJointExtremity= new CJointDriver(this, _oJointFoot,						_sSidePrefixL+"Toe",	1.0f*C_DriveAng, 3.0f, -020f,  060f, -001f,  001f, -001f,  001f));		//###WEAK: 1 rot on.	###TODO!!! High mass on toes to stop shake... 
		
		_oJointMidLimb._oConfJoint.targetRotation = Quaternion.Euler(-160, 0, 0);		//###WEAK: Set knee default position so it can easily bend (with very week drive)		//###SOON: Proper bending of knee calculations from height

		//=== Init Hotspot ===
		if (_eBodySide == 0)
			_oHotSpot = CHotSpot.CreateHotspot(this, _oBody.FindBone("chest/abdomen/hip/lThigh/lShin/lFoot/lToe"), "Left Leg", true, new Vector3(0, 0, 0));
		else
			_oHotSpot = CHotSpot.CreateHotspot(this, _oBody.FindBone("chest/abdomen/hip/rThigh/rShin/rFoot/rToe"), "Right Leg", true, new Vector3(0, 0, 0));

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

	public void OnPropSet_Thigh_Spread(float nValueOld, float nValueNew) { _oJointThigh.RotateZ_DualRange(nValueNew); }
	public void OnPropSet_Thigh_Rotate(float nValueOld, float nValueNew) { _oJointThigh.RotateY_DualRange(nValueNew); }
}
