using UnityEngine;
using System;
using System.Collections;

public class CActorNode : CActor {			// CActorNode: Simple 'node based' actor without joints to enable user to move / rotate other real actors... Currently used for base and torso
	//###DESIGN: Really keep design based on CActor???  Just needs to move a simple node!

    public override void OnStart_DefineLimb() {
        //=== Init Bones and Joints ===
        //_oHotSpot = CHotSpot.CreateHotspot(this, transform, transform.name, true, G.C_Layer_HotSpot, C_SizeHotSpot_BodyNodes);		//###DESIGN!!!!: How can user move actors apart that get too close??

		//=== Init CObj ===
		//_oObj = new CObj(this, transform.name, transform.name, _oBodyBase._oObj);		//###PROBLEM19: Name for scripting and label name!
		//CObjEnum oObjGrp = new CObjEnum(_oObj, transform.name, typeof(EActorNode));
		//AddBaseActorProperties();						// The first properties of every CActor subclass are Pinned, pos & rot
		//###DESIGN? __oObj.Add(EActorNode.Height,	"Height",		0.5f,	0.5f,	1.2f,	"", CObj.Local);		//###DESIGN: Range tuned to standing height
	}

	//public void OnSet_Height(float nValueOld, float nValueNew) {	//###OBS?
	//	Vector3 vecPos = transform.localPosition;
	//	vecPos.y = nValueNew;
	//	transform.localPosition = vecPos;
	//}
}
