using UnityEngine;
using System.Collections;

public class CCamTarget : MonoBehaviour {		// CCamTarget: The one global camera focus point that the camera always orbits around.  Pose is determined by PhysX through a collection of adjustatble 'CCamMagnet' spring joints pulling camera toward points of interest
	//###IMPROVE: Integrate this code into a rewritten MorbitMOD??
	//###IMPROVE??  GUI to select the drive strength of each magnet for custom camera position?? 

	public float CamMagnetPull_Unselected	= 1;		// Strength of spring joint of this magnet when selected/unselected
	public float CamMagnetPull_Selected		= 10;

	public void OnStart() {		
		CCamOrbit oCamOrbiter = Camera.main.GetComponent<CCamOrbit>();
		oCamOrbiter.target = gameObject;

		CUtility.FindOrCreateComponent(gameObject, typeof(Rigidbody));
		GetComponent<Rigidbody>().isKinematic = false;				// We are dynamic.  We move by the many CCamMagnet variable-strenght spring joints pulling us toward points of interest
		GetComponent<Rigidbody>().useGravity = false;
		GetComponent<Rigidbody>().freezeRotation = true;
		GetComponent<Rigidbody>().drag = 200;							//###TUNE
	}
}
