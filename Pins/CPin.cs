using UnityEngine;
using System.Collections;


public class CPin : MonoBehaviour {
	//public bool					_bShowMarkers, _bShowMarkersGUI;			// Whether we render the debug marker or not...
	//public bool					_bManualControl, _bManualControlGUI;		//###TEMP: Don't update to skin position at runtime (for runtime position change in Unity scene)


	//---------------------------------------------------------------------------	INIT

	public static CPin CreatePin(CPin oPinParent, string sName, string sTemplateName) {		//###LEARN Pins can only be created statically because of we're a MonoBehavior and can only exist as a component!  (Weird that 'this' would be null tho!)
		GameObject oGoPinTemplate = Resources.Load("GUI-Pins/" + sTemplateName) as GameObject;
		GameObject oGO = GameObject.Instantiate(oGoPinTemplate) as GameObject;
		CPin oPin = oGO.GetComponent<CPin>();
		oPin.Initialize(oPinParent, sName);
		return oPin;
	}
	public void Initialize(CPin oPinParent, string sName) {
		transform.name = sName;
		
		if (oPinParent != null)	{							//###CHECK: Do sanity check for parent-type / child-type validity
			transform.parent = oPinParent.transform;
			transform.localPosition = Vector3.zero;				//###CHECK! Do we ALWAYS reset our position to coincide with parent??
		}
	}

	//---------------------------------------------------------------------------	UPDATE

	public virtual void OnSimulatePre() {
		//###IMPROVE: Re-enable if / when we have a 'development' / debug build #if
		//if (_bShowMarkers !=_bShowMarkersGUI)			//###WEAK: Wish there were a way to have properties editable in Unity GUI panel!!!
		//	SetShowMarkers(_bManualControlGUI);
		//if (_bManualControl != _bManualControlGUI)		//###IMPROVE: Replace this crap with a 'BroadcastMessage' approach!!
		//	SetManualControl(_bManualControlGUI);
		
		for (int nChildIndex=0; nChildIndex<transform.childCount; nChildIndex++) {				// We just change our transform position so this will change all CPinTetra we have a sub Unity nodes.  Notify them so they send their new pin location to Phys
			Transform oTran = transform.GetChild(nChildIndex);			//###OPT!?: Can improve performance by caching into a collection instead?
			CPin oPin = (CPin)oTran.GetComponent<CPin>();
			oPin.OnSimulatePre();			
		}
	}

	//---------------------------------------------------------------------------	IMPLEMENTATION

	public void SetPinPosition(Vector3 vecPos) {
		transform.position = vecPos;
	}
	
	//public void SetShowMarkers(bool bShowMarkers) {				// Iterate throught our pins to hide or show their debug visualization (in response to debug key events)
	//	_bShowMarkers = _bShowMarkersGUI = bShowMarkers;		//###IMPROVE: Redo these with 'SendMessage' or 'GetComponentInChildren'??
	//	if (renderer != null)
	//		renderer.enabled = _bShowMarkers;
	//	for (int nChildIndex=0; nChildIndex<transform.childCount; nChildIndex++) {				// We just change our transform position so this will change all CPinTetra we have a sub Unity nodes.  Notify them so they send their new pin location to Phys
	//		Transform oTran = transform.GetChild(nChildIndex);
	//		CPin oPin = (CPin)oTran.GetComponent<CPin>();
	//		oPin.SetShowMarkers(_bShowMarkers);
	//	}
	//}

	//public void SetManualControl(bool bManualControl) {				// Iterate throught our pins to set their manual control tree on/off  //###TODO: Make recursive call with callback to do all this recursive stuff in one call!
	//	_bManualControl = _bManualControlGUI = bManualControl;
	//	for (int nChildIndex=0; nChildIndex<transform.childCount; nChildIndex++) {				// We just change our transform position so this will change all CPinTetra we have a sub Unity nodes.  Notify them so they send their new pin location to Phys
	//		Transform oTran = transform.GetChild(nChildIndex);
	//		CPin oPin = (CPin)oTran.GetComponent<CPin>();
	//		oPin.SetManualControl(_bManualControl);
	//	}
	//}

	public int GetNumChildPins() { return transform.childCount; }
	
	public CPin FindChildPin(int nID) {
		Transform oPinT = transform.FindChild(nID.ToString());
		return oPinT.GetComponent<CPin>();
	}
};
