using UnityEngine;

public class CRotateTowardCamera : MonoBehaviour {      // CRotateTowardCamera: Automatically rotates this object to face the camera.  Used for GUI panels (that are child of this object) to always face camera.

	void Update () {
        //transform.rotation = Camera.main.transform.rotation;

        //Vector3 vecRotCam = Camera.main.transform.rotation.eulerAngles;     // Orient toward the camera (but ditch the 'camera roll' component (head tilting left / right)
        //vecRotCam.z = 0;        //###LEARN: How to ditch the camera roll.
        //transform.rotation = Quaternion.Euler(vecRotCam);

        transform.LookAt(Camera.main.transform);        // Simply orient toward the camera.  Works great! :)
    }
}
