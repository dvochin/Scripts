using UnityEngine;
using System.Collections;

public class CPoseRoot : MonoBehaviour, IHotSpotMgr {			// CPoseRoot : Simple node that user can move around to move / rotate all characters ###OBS??

	public CHotSpot	_oHotSpot;
	public CObj	_oObj;

	public void OnStart() {
		_oHotSpot = CHotSpot.CreateHotspot(this, transform, "Pose Root", true, G.C_Layer_HotSpot, 2.0f);

		//_oObj = new CObj(this, "Pose Root", "Pose Root", null);		//#DEV26:???
		//CObjEnum oObjGrp = new CObjEnum(_oObj, "Pose Root", typeof(EPoseRoot));
		//###BROKEN: Need to flip load order, not 180!!  _oObj.Add(EPoseRoot.Flipped,	"Flip Pose",		0,	"", CObj.Local | CObj.AsCheckbox);	//###IMPROVE: Add a separation member with key hook.  A height too??
	}

	public void OnSet_Flipped(float nValueOld, float nValueNew) {
		transform.localRotation = Quaternion.Euler(0, (nValueNew == 1) ? 180 : 0, 0);
		CGame.INSTANCE.TemporarilyDisablePhysicsCollision();		// Flipping the bodies will probably cause them to tangle...
	}

	//---------------------------------------------------------------------------	HOTSPOT EVENTS
	public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {
		//if (eHotSpotEvent == EHotSpotEvent.ContextMenu)		//###BROKEN: Needs canvas!
		//	_oHotSpot.WndPopup_Create(null, new CObj[] { _oObj });
	}

	public virtual void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) {
		switch (eEditMode) {
			case EEditMode.Move:			//###NOTE: We don't process rotation as the game only supports 0 or 180 Y rotation
				transform.position = oGizmo.transform.position;
				break;
		}
	}
}
