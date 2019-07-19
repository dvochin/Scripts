using UnityEngine;
using System;
using System.Collections;

//===== CCamMagnet.  A 'magnet' to the one global CCamTarget camera focus point which acts to pull camera toward this point of interest by an adjustable-strength PhysX spring joint
public class CCamMagnet : MonoBehaviour {

	SpringJoint			_oBoneSpring;
	//ECamMagnetType		_eCamMagnetType;

	void Start() {
		//_eCamMagnetType = (ECamMagnetType)Enum.Parse(typeof(ECamMagnetType), gameObject.name.Substring("CCamMagnet-".Length));		// Obtain our type from the last part of our node name

		_oBoneSpring = CUtility.FindOrCreateComponent(gameObject, typeof(SpringJoint)) as SpringJoint;
		_oBoneSpring.maxDistance = 0;
		GetComponent<Rigidbody>().isKinematic = true;			// Our bone moves the kinematic rigid body attached to the spring joint which pulls the one CCamTarget dynamic rigid body toward it
		Vector3 vecPosBackupL = transform.localPosition;						// To properly setup a spring joint we 1) backup our current position, 2) travel to the current position of the moveable object, 3) attach the spring and 4) return to our backed up position = We will pull object toward us.
		if (CGame._oCamTarget != null) {		//###WEAK ###OPT
			transform.position = CGame._oCamTarget.transform.position;
			_oBoneSpring.connectedBody = CGame._oCamTarget.GetComponent<Rigidbody>();
		}
		transform.localPosition = vecPosBackupL;
	}

	void Update() {			//###OPT: Can find a cheaper way?
		// Keys 1-4 run on all CCamMagnets on all bodies.  1 is 'all pull equals', 2 is sex focus, 3 is breast focus, 4 is head focus
		//###IMPROVE?: Take the rest of the numbers for only body 1 & 2?
        //###P: Revive?
		//if      (Input.GetKeyDown(KeyCode.Alpha1))	_oBoneSpring.spring = CGame._oCamTarget.CamMagnetPull_Unselected;
		//else if (Input.GetKeyDown(KeyCode.Alpha2))	_oBoneSpring.spring = (_eCamMagnetType == ECamMagnetType.Pelvis)	? CGame._oCamTarget.CamMagnetPull_Selected : CGame._oCamTarget.CamMagnetPull_Unselected;
		//else if (Input.GetKeyDown(KeyCode.Alpha3)) 	_oBoneSpring.spring = (_eCamMagnetType == ECamMagnetType.Breasts)	? CGame._oCamTarget.CamMagnetPull_Selected : CGame._oCamTarget.CamMagnetPull_Unselected;
		//else if (Input.GetKeyDown(KeyCode.Alpha4)) 	_oBoneSpring.spring = (_eCamMagnetType == ECamMagnetType.Head)		? CGame._oCamTarget.CamMagnetPull_Selected : CGame._oCamTarget.CamMagnetPull_Unselected;
	}
}

enum ECamMagnetType {
	Pelvis,
	Breasts,
	Head
}