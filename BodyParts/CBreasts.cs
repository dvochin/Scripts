/*###DISCUSSION: Breasts
=== NEXT ===
(New morphing stuff)
- Have now created 'fake' -> turn into _bNotRealSoftBody
- Where to put menu?  Put new morph ops into our alternate menu?
	- Are we really morphing only breasts or mechanism for entire body?  DECIDE!
- Have to send proper args to Breast morph target... will work with separated breasts??

=== TODO ===
- Nipple sphere collider off??  Too much protrusion!
- Add more properties to GUI: sphere base, protrusion, etc
* Custom-defined properties!
 * Growth!!!


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
using System;

public class CBreasts : CBSoft, IHotSpotMgr {

	CHotSpot	_oHotSpot;

	public CBreasts() {
		_nRangeTetraPinHunt = 0.022f;		//###TUNE!!  (Changes with _SoftBodyDetailLevel!!)
		_SoftBodyDetailLevel = 15;          //###TUNE
	}

	public override void OnDeserializeFromBlender() {
		base.OnDeserializeFromBlender();

		_eColGroup = (EColGroups)(EColGroups.eLayerBodyNoCollisionWithSelfStart + _oBody._nBodyID);		// Breast softbody (and their associated rigid body kinematic colliders to repel other breasts) each get their own group for their own body so each breast softbody doesn't collide with its own colliders (designed to repel other breasts)

		if (CGame.INSTANCE._GameMode == EGameModes.PlayNoAnim) {			// If we're in cloth setup mode we set the softbody breasts as stiff as possible with no gravity so they look as close to rigid as possible
			_oObj.PropSet(ESoftBody.VolumeStiffness, 1.0f);
			_oObj.PropSet(ESoftBody.StretchingStiffness, 1.0f);
			_oObj.PropSet(ESoftBody.SoftBody_Gravity, 0.0f);
		} else { 
			_oObj.PropSet(ESoftBody.VolumeStiffness, 0.4f);             //####MOD: Was .9, .6
			_oObj.PropSet(ESoftBody.StretchingStiffness, 0.35f);        //###TUNE!!
		}
		_oObj.PropSet(ESoftBody.ParticleRadius, 0.03f);             //###TUNE!!! Hugely important to be repelled by hands as strongly as possible
		_oHotSpot = CHotSpot.CreateHotspot(this, _oBody.FindBone("chest"), "Breasts", false, new Vector3(0, 0.09f, 0.2f));
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
