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

public class CBreastBase : CSoftBody/*, IHotSpotMgr*/ {    //###OBS!

	//CHotSpot	_oHotSpot;
	///public	CBodyColBreast		_oBodyColBreast;            // The breast collider mesh.  Used in PhysX3 to repell cloth
	public  int                 _nBreastID;					// 0: BreastL  1: BreastR


	public CBreastBase() {
		///_nRangeTetraPinHunt_OBS = 0.017f;		//###TUNE!!  (Changes with _SoftBodyDetailLevel!!)
		///_SoftBodyDetailLevel = 15;          //###TUNE
		_nBreastID = (this.GetType().Name == "CBreastR") ? 1 : 0;
	}

	public override void OnDeserializeFromBlender() {
		base.OnDeserializeFromBlender();
		///_eColGroup = (EColGroups)(EColGroups.eLayerBodyNoCollisionWithSelfStart + _oBody._nBodyID);     // Breast softbody (and their associated rigid body kinematic colliders to repel other breasts) each get their own group for their own body so each breast softbody doesn't collide with its own colliders (designed to repel other breasts)

		//=== Define the breast collider from the datamember of our CSoftBodyBreast instance managing oMeshColBreast collider mesh ===
		///_oBodyColBreast = (CBodyColBreast)CBMesh.Create(null, _oBody, _sBlenderInstancePath_CSoftBody + ".oMeshColBreast", typeof(CBodyColBreast));
}

	//public override void OnChangeGameMode(EGameModes eGameModeNew, EGameModes eGameModeOld) {		//###DEV
	//	base.OnChangeGameMode(eGameModeNew, eGameModeOld);

	//	switch (eGameModeNew) {		//####DEV ####OBS??
	//		case EGameModes.Play:
	//			_oObj.PropSet(ESoftBody.VolumeStiffness, 0.4f);             //####MOD: Was .9, .6
	//			_oObj.PropSet(ESoftBody.StretchingStiffness, 0.35f);        //###TUNE!!
	//			_oObj.PropSet(ESoftBody.ParticleRadius, 0.03f);             //###TUNE!!! Hugely important to be repelled by hands as strongly as possible
	//			//_oHotSpot = CHotSpot.CreateHotspot(this, _oBody.FindBone("chest"), "Breasts", false, new Vector3(0, 0.09f, 0.2f));
	//			break;
	//		case EGameModes.Configure:
	//			//Destroy(_oHotSpot);			//####IMPROVE: 'safe destroy'?
	//			//_oHotSpot = null;
	//			break;
	//	}
	//}

	//public override void OnSimulateBetweenPhysX23() {
	//	base.OnSimulateBetweenPhysX23();
	//	if (_oBodyColBreast != null)                            // Update the breast colliders from PhysX2 breasts so they are available next call for PhysX3
	//		_oBodyColBreast.OnSimulateBetweenPhysX23();
	//}

	public override void OnDestroy() {
		base.OnDestroy();
	}

	public void FinishColliderCreation() {          // Called by CBody after breasts created so we can late-init collider from this instance
		///_oBodyColBreast.FinishColliderCreation(this);
	}

	////--------------------------------------------------------------------------	IHotspot interface

	//public void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) { }

	//public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {		//###DESIGN? Currently an interface call... but if only GUI interface occurs through CObject just have cursor directly invoke the GUI_Create() method??
	//	if (eHotSpotEvent == EHotSpotEvent.ContextMenu)
	//		_oHotSpot.WndPopup_Create(new CObject[] { _oObj });
	//}
}
