using UnityEngine;


public class CActorGuiPin : MonoBehaviour {     // CActorGuiPin: Maintains a GUI panel at a global-space offset from its parent actor.  Used for GUI panel to not render at their actor's coordinate and overlap

	CActor		_oActor;				// The actor that owns / manages us
	Vector3     _vecOffset;             // The offset we maintain from _oParentT
	CUICanvas   _oCanvas;               // The GUI canvas we own / manage
	Vector3		_vecCamToPin;           // Vector from cam to pin.  Kept to avoid excessive GC
	Quaternion  _quatRot;               // Quaternion of canvas to keep oriented toward camera.  Kept to avoid excessive GC

	public static CActorGuiPin Create(CActor oActor) {
        CActorGuiPin oActorGuiPin = CUtility.InstantiatePrefab<CActorGuiPin>("Prefabs/CActorGuiPin", "ActorGuiPin-" + oActor._sName);
		oActorGuiPin.Init(oActor);
		return oActorGuiPin;
	}

	public void Init(CActor oActor) {
		_oActor = oActor;
		transform.name = "ActorGuiPin-" + _oActor.gameObject.name;
		transform.SetParent(_oActor.transform);

		//=== Calculate an offset that makes sense for the given node ===
		float nX = 0, nZ = 0;                       //###TUNE
		switch (oActor._eBodySide) {
			case EBodySide.Left:
				nX = -0.2f;
				break;
			case EBodySide.Right:
				nX = 0.2f;
				break;
			case EBodySide.Center:
				nZ = 0.4f;                          //###WEAK: Could be clipped by some body parts such as large breasts?
				break;
		}
		_vecOffset  = new Vector3(nX, 0, nZ);
		enabled = false;
	}

	public void GUI_Show() {
		if (_oCanvas)
			return;
		//=== Create canvas and panel to render the context-sensitive popup for this actor ===
		_oCanvas = CUICanvas.Create(transform);
		_oCanvas.transform.SetParent(transform);                  // Attach the canvas to the provided attach point on the wand model...
		_oCanvas.transform.localPosition = Vector3.zero;
		_oCanvas.transform.localRotation = Quaternion.identity;
		_oCanvas.gameObject.name = "CUICanvas-" + _oActor.gameObject.name;
		_oCanvas.CreatePanel(_oActor._sName, _oActor._oObj);          //###IMPROVE: Pin canvas to left or right edge?
		enabled = true;
	}

	public void GUI_Hide() {
		if (_oCanvas)
			GameObject.Destroy(_oCanvas.gameObject);        //#DEV26:
		_oCanvas = null;
		enabled = false;
	}

	public void Update() {		//###OPT: Can run occasionally in co-routine?
		transform.position = _oActor.transform.position + _vecOffset;
		_vecCamToPin = transform.position - Camera.main.transform.position;
		_quatRot = Quaternion.LookRotation(_vecCamToPin, Vector3.up);		//###INFO: How to keep an object facing the camera at all times
		transform.rotation = _quatRot;
	}
}
