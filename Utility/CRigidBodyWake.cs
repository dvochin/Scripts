using UnityEngine;
using System.Collections;

//=== Simple script to never allow a rigid body to sleep ===
public class CRigidBodyWake : MonoBehaviour {
	
	Rigidbody _oRB = null;
	
	void FixedUpdate() {
		if (_oRB == null) {
			_oRB = GetComponent<Rigidbody>();
			if (_oRB == null) 
				return;
            //####MOD _oRB.sleepVelocity = 0;
            //_oRB.sleepAngularVelocity = 0;
            _oRB.sleepThreshold = 0;
		}		
		_oRB.WakeUp();
	}
}
