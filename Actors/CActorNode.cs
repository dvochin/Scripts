using UnityEngine;
using System;
using System.Collections;

public class CActorNode : CActor {			// CActorNode: Simple 'node based' actor without joints to enable user to move / rotate other real actors... Currently used for base and torso
	//###DESIGN: Really keep design based on CActor???  Just needs to move a simple node!

	public override void OnStart_DefineLimb() {
		//=== Init Bones and Joints ===
		_oHotSpot = CHotSpot.CreateHotspot(this, transform, transform.name, true, Vector3.zero, C_SizeHotSpot_BodyNodes);		//###DESIGN!!!!: How can user move actors apart that get too close??

		//=== Init CObject ===
		_oObj = new CObject(this, _oBody._nBodyID, typeof(EActorNode), transform.name, transform.name);	//###DESIGN: Keep???
		_oObj.PropGroupBegin("", "", true);
		AddBaseActorProperties();						// The first properties of every CActor subclass are Pinned, pos & rot
		//###DESIGN? _oObj.PropAdd(EActorNode.Height,	"Height",		0.5f,	0.5f,	1.2f,	"", CProp.Local);		//###DESIGN: Range tuned to standing height
		_oObj.FinishInitialization();
	}

	//public void OnPropSet_Height(float nValueOld, float nValueNew) {	//###OBS?
	//	Vector3 vecPos = transform.localPosition;
	//	vecPos.y = nValueNew;
	//	transform.localPosition = vecPos;
	//}
}
