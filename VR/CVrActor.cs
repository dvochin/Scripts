using UnityEngine;

public class CVrActor : VRTK.VRTK_InteractableObject {		//###DOCS: CVrActor: Simple event sink to override VRTK_InteractableObject on Actor pins: Creates a GUI menu at the actor location when player 'uses' VR object (e.g. presses trigger on either wand)

	CActor _oActor;

	void Start () {
		_oActor = GetComponent<CActor>();
		if (_oActor == null)
			CUtility.ThrowExceptionF("###EXCEPTION: CVrActor could not find CActor component on {0}", gameObject.name);
	}
	
	public override void StartUsing(GameObject usingObject) {
        base.StartUsing(usingObject);
		_oActor.OnVrAction();
	}
}
