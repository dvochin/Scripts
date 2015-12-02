/*###DISCUSSION: Breasts
=== NEXT ===

=== TODO ===
- Add more properties to GUI: sphere base, protrusion, etc

=== LATER ===

=== IMPROVE ===

=== DESIGN ===

=== IDEAS ===

=== LEARNED ===
- Low cloth cycles really 'digs into' breast sphere colliders!  (They become highly yielding to cloth pressure = useless!)

=== PROBLEMS ===

=== PROBLEMS??? ===

=== WISHLIST ===
- Inner breast frequently clips with cloth

*/
using UnityEngine;

public class CBreasts : CBSoft, IHotSpotMgr {

	CHotSpot	_oHotSpot;

	public CBreasts() {
		_nRangeTetraPinHunt = 0.015f;		//###TUNE!!  (Changes with _SoftBodyDetailLevel!!)
		_SoftBodyDetailLevel = 15;          //###TUNE
	}

	public override void OnDeserializeFromBlender() {
		base.OnDeserializeFromBlender();
		_eColGroup = (EColGroups)(EColGroups.eLayerBodyNoCollisionWithSelfStart + _oBody._nBodyID);		// Breast softbody (and their associated rigid body kinematic colliders to repel other breasts) each get their own group for their own body so each breast softbody doesn't collide with its own colliders (designed to repel other breasts)
	}

	public override void OnChangeGameMode(EGameModes eGameMode) {
		base.OnChangeGameMode(eGameMode);

		switch (eGameMode) {
			case EGameModes.Play:
				_oObj.PropSet(ESoftBody.VolumeStiffness, 0.4f);             //####MOD: Was .9, .6
				_oObj.PropSet(ESoftBody.StretchingStiffness, 0.35f);        //###TUNE!!
				_oObj.PropSet(ESoftBody.ParticleRadius, 0.03f);             //###TUNE!!! Hugely important to be repelled by hands as strongly as possible
				_oHotSpot = CHotSpot.CreateHotspot(this, _oBody.FindBone("chest"), "Breasts", false, new Vector3(0, 0.09f, 0.2f));
				break;
			case EGameModes.PlayNoAnim:
				Destroy(_oHotSpot);			//####IMPROVE: 'safe destroy'?
				_oHotSpot = null;
				break;
		}
	}

	public override void OnDestroy() {
		base.OnDestroy();
	}

	//--------------------------------------------------------------------------	IHotspot interface

	public void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) { }

	public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {		//###DESIGN? Currently an interface call... but if only GUI interface occurs through CObject just have cursor directly invoke the GUI_Create() method??
		if (eHotSpotEvent == EHotSpotEvent.ContextMenu)
			_oHotSpot.WndPopup_Create(new CObject[] { _oObj });
	}
}
