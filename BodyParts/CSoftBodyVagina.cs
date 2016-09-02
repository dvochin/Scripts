using UnityEngine;
using System.Collections;


public class CSoftBodyVagina : CSoftBody {		// Class to abstract away complexity of managing vagina softbody

	public CSoftBodyVagina() {
		_nRangeTetraPinHunt_OBS = 0.007f;
		_SoftBodyDetailLevel = 20;				//###TUNE
	}

	public override void OnDeserializeFromBlender() {
		base.OnDeserializeFromBlender();
		_eColGroup = EColGroups.eLayerVagina;

		_oObj.PropSet(ESoftBody.SolverIterations, 2);		//###OPT!!!! Expensive but shakes otherwise!
		_oObj.PropSet(ESoftBody.VolumeStiffness, 1);			//###IMPROVE: Make properties publicly available? (like penis?)
		_oObj.PropSet(ESoftBody.StretchingStiffness, 0.5f);	//###TUNE!!!
		_oObj.PropSet(ESoftBody.SoftBody_Damping, 1);
		_oObj.PropSet(ESoftBody.ParticleRadius, 0.01f);		//###TUNE!!!
		_oObj.PropSet(ESoftBody.Friction, 0);
		_oObj.PropSet(ESoftBody.SoftBody_Gravity, 0);
	}
}
