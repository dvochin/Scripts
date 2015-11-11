/*###DISCUSSION: CHandTarget
 * What the user needs is credible breast animations at the press of a key (all non-locked hands perform the same mirrored animation)
   * Breast mash, breast up/down, breast caress, etc repeated 3-7 times then to the next...
     * The same needs apply to cock stroking and pussy fingering... a single key to invoke it and that's it!
   * Of course this needs breast dimensions to know where to start and how far to press in
     * Where to get breast dimensions efficiently at runtime:
       * 6 specially-marked verts marked since Blender queried directly in C++ (breast side, bottom, nipple)
 * Breast caress, dick stroking, pussy fingering are all heavy development areas requiring careful design, hand positioning, anchors, good anims, good randomization, etc
 * So... is CKeyControl of hands superfluous?
 * Also... No direct control of hands like we do with Pelvis (with possible animations?)
*/


using UnityEngine;
using System.Collections;

public class CHandTarget : MonoBehaviour, IHotSpotMgr {		// CHandTarget: A destination bone-connected target point for a hand that can randomly animate hand inside an optional bounding box

	public			EHandTravelDir	_HandTravelDir = EHandTravelDir.MinusY;			// GUI-exposed property that sets what direction along X,Y,Z + or - travel will take when user controls hand (pushing hand is going along -Y)
	public			float			_MaxUserTravel = 0.1f;							// Maximum distance user can travel from design-time starting point.

	CActorArm		_oActorArm;						// The arm connected to us.  Null if we're not active
	//Vector3			_vecAnimAreaBounds;				// (Optional) bounds of animation for connected hands.	###CLEANUP!?
	//Vector3			_vecTarget;						// The current target point
	//Vector3			_vecTargetLast;					// The previous target point.  Used for smoothing through 'slerp'
	//Vector3			_vecRandom;						// Current random values used to determine current travel ponit
	//Vector3			_vecRandomLast;					// Last random values used at last time a target point was chosen.  Used to find a new random point in the opposite direction
	//Vector3			_vecDistToFurthestExtremity;	// Distance to furthest extremity of -1 or +1 in our next random range... used to travel the other way for next travel point
	//float			_nTimeNextPosChange;			// Time.time value when we're due for a new random position

	//[HideInInspector] CHotSpot		_oHotSpot;			// Our hotspot that enables user to find our hand target in 3D scene when in 'Hand Connect Mode'

	void Start () {			//###DESIGN: No OnStart()???
		//Transform oAnimAreaT = transform.FindChild("AnimArea");
		//if (oAnimAreaT != null) {				
		//	BoxCollider oBoxCol = oAnimAreaT.collider as BoxCollider;
		//	oBoxCol.enabled = false;				// Box collider is only provided to define our animation range... make sure it is disabled to prevent taking resources
		//	oBoxCol.isTrigger = true;
		//	_vecAnimAreaBounds = oAnimAreaT.localScale;		// Animation area stored in subnode's anim area (box collider kept at size 1,1,1 as usual for proper visualization)
		//} else {
		//	gameObject.SetActive(false);		// If we don't have an animation area we don't need update.
		//}

		//###DESIGN: Hotspots of hands with no name??
		//###BROKEN: _oHotSpot = CHotSpot.CreateHotspot(this, transform, /*transform.name*/ "", true, Vector3.zero, 1.0f, CCursor.C_Layer_HotSpotHands);		// Create our hotspot on the hand layer for special filtering by CCursor
	}

	public static CHandTarget FindOrCreateRightSideHandTarget(CHandTarget oHandTargetL) {	// Find or create a 'right side mirrored copy' of design-time-defined left-side hand target.   Bone structure only has hand targets defined for the left body side to prevent duplication.  Mirror the left node into a mirrored right node for processing of the right body side

		// Create a right side node that will store the mirrored copy of the source left-side node.
		string sNameHandTargetRight = oHandTargetL.gameObject.name + "-Right";				// Right-side hand targets are generated & mirrored as needed.  Their name is suffixed with '-Right'
		CHandTarget oHandTargetR = CUtility.FindOrCreateNode(oHandTargetL.transform.parent, sNameHandTargetRight, typeof(CHandTarget)) as CHandTarget;
		oHandTargetR.transform.parent = oHandTargetL.transform.parent;			// Right side gets same parent as left side

		//=== Calculate the mirrored position of right side ===
		Vector3 vecTarget = oHandTargetL.transform.localPosition;
		vecTarget.x = -vecTarget.x;
		oHandTargetR.transform.localPosition = vecTarget;

		//=== Adjust hand target rotation to requested body side ===
		Vector3 vecRotTarget = oHandTargetL.transform.localRotation.eulerAngles;
		vecRotTarget.y = Mathf.PI - vecRotTarget.y;
		vecRotTarget.z = -vecRotTarget.z;
		oHandTargetR.transform.localRotation = Quaternion.Euler(vecRotTarget);

		//=== Create a copy of the anim area subnode if it exists ===
		//Transform oAnimAreaLT = oHandTargetL.transform.FindChild("AnimArea");
		//if (oAnimAreaLT != null) {
		//	BoxCollider oBoxColR = CUtility.FindOrCreateNode(oHandTargetR.transform, oAnimAreaLT.name, typeof(BoxCollider)) as BoxCollider;
		//	oBoxColR.enabled = false;
		//	oBoxColR.isTrigger = true;
		//	oBoxColR.transform.localScale = oAnimAreaLT.localScale;
		//	oBoxColR.transform.localPosition = Vector3.zero;
		//	oBoxColR.transform.localRotation = Quaternion.identity;
		//}

		return oHandTargetR;
	}

	public void ConnectHandToHandTarget(CActorArm oActorArm) {
		//###IMPROVE: smooth progression to target (sin)

		_oActorArm = oActorArm;
		//_nTimeNextPosChange = -1;
		if (_oActorArm != null) {
			oActorArm.transform.parent = transform;					// Reparent to the hand target's node so we move/rotate along with the node
			oActorArm.transform.localPosition = Vector3.zero;
			oActorArm.transform.localRotation = Quaternion.identity;
			oActorArm._oObj.PropSet(EActorArm.Pinned, 1);			// Hand target now in requested position.  Pin to make it move
			//GetNextRandomVector();
			enabled = true;
		} else {
			enabled = false;
		}
	}

	//void GetNextRandomVector() {		###CLEANUP?
	//	_vecRandomLast = _vecRandom;
	//	_vecRandom.x = ((float)CGame.INSTANCE._oRnd.NextDouble()) * 2.0f - 1.0f;		// Seed the next random anywhere in -1..1 range...
	//	_vecRandom.y = ((float)CGame.INSTANCE._oRnd.NextDouble()) * 2.0f - 1.0f;
	//	_vecRandom.z = ((float)CGame.INSTANCE._oRnd.NextDouble()) * 2.0f - 1.0f;
	//}

	//void FixedUpdate () {		// Hook into global update to animate hand if we're connected  ###DESIGN: hand / arm actor doesn't have a OnUpdate which is why we do from this end... revisit??
	//	if (_oActorArm != null) {
	//		float nTime = Time.time;
	//		if (nTime > _nTimeNextPosChange) {
	//			_nTimeNextPosChange = nTime + 1.0f;// +(float)CGame.INSTANCE._oRnd.NextDouble() * 1.0f;		//###IMPROVE: Export change of pose timeout

	//			//=== Choose a new random target point within our animation area ===
	//			//_vecRandom.x = 1.0f - Mathf.Cos(((float)CGame.INSTANCE._oRnd.NextDouble() - 0.5f) * Mathf.PI);		// Gives a curve that promotes the ends of the -1..1 range
	//			//_vecRandom.y = 1.0f - Mathf.Cos(((float)CGame.INSTANCE._oRnd.NextDouble() - 0.5f) * Mathf.PI);
	//			//_vecRandom.z = 1.0f - Mathf.Cos(((float)CGame.INSTANCE._oRnd.NextDouble() - 0.5f) * Mathf.PI);

				
	//			//_vecDistToFurthestExtremity.x = _vecRandomLast.x > 0 ? -1.0f - _vecRandomLast.x : 1.0f - _vecRandomLast.x;		// Calculate the maximum distance possible to travel to the furthest extremity (we will go in that direction)
	//			//_vecDistToFurthestExtremity.y = _vecRandomLast.y > 0 ? -1.0f - _vecRandomLast.y : 1.0f - _vecRandomLast.y;
	//			//_vecDistToFurthestExtremity.z = _vecRandomLast.z > 0 ? -1.0f - _vecRandomLast.z : 1.0f - _vecRandomLast.z;

	//			//_vecRandom.x = ((float)CGame.INSTANCE._oRnd.NextDouble()) * _vecDistToFurthestExtremity.x;
	//			//_vecRandom.y = ((float)CGame.INSTANCE._oRnd.NextDouble()) * _vecDistToFurthestExtremity.x;
	//			//_vecRandom.z = ((float)CGame.INSTANCE._oRnd.NextDouble()) * _vecDistToFurthestExtremity.x;
	//			//Debug.Log("Rnd: " + _vecRandom.x + "," + _vecRandom.y + "," + _vecRandom.z);

	//			//_vecRandomLast = _vecRandom;

	//			GetNextRandomVector();
	//			_vecTargetLast = _vecTarget;
	//			_vecTarget.x = _vecRandom.x * _vecAnimAreaBounds.x;		//###CHECK: The full size of bounding box??
	//			_vecTarget.y = _vecRandom.y * _vecAnimAreaBounds.y;
	//			_vecTarget.z = _vecRandom.z * _vecAnimAreaBounds.z;
	//			_oActorArm.transform.localPosition = _vecTarget;
	//		}

	//		_vecTargetLast = Vector3.Slerp(_vecTargetLast, _vecTarget, 3.0f);
	//		_oActorArm.transform.localPosition = _vecTargetLast;
	//	}
	//}

	//public void SetUserControl(float nValue) {		//###OBS!!! Anim area??
	//	nValue *= _MaxUserTravel;					// nValue is 0..1 so we obtain max travel distance by multiplying.
	//	Vector3 vecPos = _oActorArm.transform.localPosition;
	//	switch (_HandTravelDir) {
	//		case EHandTravelDir.PlusX:		vecPos.x =  nValue; break;
	//		case EHandTravelDir.PlusY:		vecPos.y =  nValue; break;
	//		case EHandTravelDir.PlusZ:		vecPos.z =  nValue; break;
	//		case EHandTravelDir.MinusX:		vecPos.x = -nValue; break;
	//		case EHandTravelDir.MinusY:		vecPos.y = -nValue; break;
	//		case EHandTravelDir.MinusZ:		vecPos.z = -nValue; break;
	//	}
	//	_oActorArm.transform.localPosition = vecPos;
	//}


	public void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) {

	}
	public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {
		if (eHotSpotEvent == EHotSpotEvent.Activation) {
			ConnectHandToHandTarget(CGame.GetSelectedBody()._oActor_ArmL);
		} else if (eHotSpotEvent == EHotSpotEvent.Deactivation) {
			ConnectHandToHandTarget(null);
		}
	}
}

public enum EHandTravelDir {			// Direction travel occurs when user control moves the hand in a direction.	//###OBS?
	PlusX,
	PlusY,
	PlusZ,
	MinusX,
	MinusY,
	MinusZ
};
