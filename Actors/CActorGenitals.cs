using UnityEngine;
using System;
using System.Collections;

public class CActorGenitals : CActor {

	//---------------------------------------------------------------------------	CREATE / DESTROY

	public override void OnStart_DefineLimb() {
		//=== Init Bones and Joints ===

		//=== Initialize position of our pin to the startup position / orientation of our basis bone in body ===		//###BROKEN:???
		Transform oGenitalsT = CUtility.FindChild(_oBodyBase._oBoneRootT, "hip/pelvis/Genitals");		// Because we're first to be created we must fetch our reference bone directly
		//###TODO22: Have extra bone for man rig to point to penis start
		transform.position = oGenitalsT.position;
		transform.rotation = oGenitalsT.rotation;

		//=== Init CObj ===
		_oObj = new CObj("Genitals", this);
		AddBaseActorProperties();						// The first properties of every CActor subclass are Pinned, pos & rot
		//_oObj.Add(EActorLeg.Thigh_Rotate,	"Thigh-Rotate",		0,	-100,	100,	"");
	}

	public override void OnDestroy() {
		base.OnDestroy();
	}

	//---------------------------------------------------------------------------	COBJECT EVENTS
}
