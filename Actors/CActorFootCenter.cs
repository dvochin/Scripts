using UnityEngine;
using System;
using System.Collections;

public class CActorFootCenter : CActor {

	//[HideInInspector]	public 	CBone 	_oBoneThighBend;


	//---------------------------------------------------------------------------	CREATE / DESTROY

	public override void OnStart_DefineLimb() {
		//=== Init Bones and Joints ===
		//CBone oBonePelvis = _oBody._oActor_Pelvis._oBoneExtremity;
		//_aBones.Add(_oBoneThighBend 	= CBone.Connect(this, oBonePelvis,	    _chSidePrefixL+"ThighBend",     10, 3.0f));

		//_oHotSpot = CHotSpot.CreateHotspot(this, _oBody._oBodyBase.FindBone("hip/pelvis/lThighBend/lThighTwist/lShin/lFoot/lMetatarsals/lToe"), "Left Leg", true, new Vector3(0, 0, 0));

		OnUpdate();				// OnStart compute our position right away so our descendents (both feet) are properly positioned relatively to us.

		//=== Init CObject ===
		_oObj = new CObject(this, "FootCenter", "FootCenter");
		CPropGrpEnum oPropGrp = new CPropGrpEnum(_oObj, "FootCenter", typeof(EActorFootCenter));
		AddBaseActorProperties();						// The first properties of every CActor subclass are Pinned, pos & rot
		//oPropGrp.PropAdd(EActorLeg.Thigh_Rotate,	"Thigh-Rotate",		0,	-100,	100,	"");
		_oObj.FinishInitialization();
	}

	public override void OnDestroy() {
		base.OnDestroy();
	}

	public override void OnUpdate() {
		return;         //###BROKEN: Auto foot placement disabled... need a GUI / body option for that


		base.OnUpdate();

		//=== Set the foot center to be the average of the chest and pelvis bone positions flattened on the Y=0 floor.
		Vector3 vecPos = (_oBody._oActor_Pelvis._oBoneExtremity.transform.position + _oBody._oActor_Chest._oBoneExtremity.transform.position) / 2;
		vecPos.y = 0;							// Foot center is ALWAYS on the Y = 0 floor
		transform.position = vecPos;
	}

	//---------------------------------------------------------------------------	COBJECT EVENTS
}
