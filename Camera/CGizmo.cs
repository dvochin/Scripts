using UnityEngine;
using System;
using System.Collections;

public class CGizmo : MonoBehaviour {
	CHotSpot					_oHotSpot;							// The hotspot that created/manage us.
	EEditMode 					_eEditMode;
	Quaternion 					_quatRotStart_Gizmo;
	float 						_nAngleCutterSpace_Start;
	Transform					_oPlaneCutter;						// Temporary cutter cursor that exists when we transform a gizmo
	EModeGizmo					_eModeGizmo;
	Vector3						_vecGizmoPosAtStart;				// 3D world position of gizmo at start.  Needed for scaling
	Vector3						_vecFirstClickOffsetToGizmo;		// Offset between the gizmo center and where the user clicked to invoke gizmo dragging.  Used to avoid snapping object to mouse click and make gizmo function like it does in most 3D programs.
//	Vector3						_vecScaleAtStart;
	RaycastHit					_oRayHit_LayerGizmo;
	Collider					_oHighlightedGizmoCollider;			// The gizmo collider we are highlighting.  We remember which one so we only highlight one
	Vector3						_vecRayHitPoint_Last;				// Last point hit by ray used in Update loop.  Used to avoid doing anything if user hasn't moved mouse cursor in the last Update frame.
	Vector3						_vecRotAxisJoint;

	[HideInInspector]	public bool			_bMiddleClickOp;					// A middle mouse button clicked gave life to this Gizmo.  Used for secondary editing

	[HideInInspector]	public	float		C_GizmoColliderHighlight = 1.25f;		// Ratio by which we multiply the alpha channel of a gizmo collider panel to visually indicate a 'highlight' as user hovers over a collider part.



	public static CGizmo CreateGizmo(CHotSpot oHotSpot, bool bMiddleClickOp) {
		Transform oGizmoTran = null;
		
		switch (CGame.INSTANCE._oCursor._EditMode) {
			case EEditMode.Select:	return null;			// We don't display a gizmo in select mode...
			case EEditMode.Move:	oGizmoTran = (Transform)GameObject.Instantiate(CGame.INSTANCE._oCursor._Prefab_GizmoMove); 		break;
			case EEditMode.Rotate:	oGizmoTran = (Transform)GameObject.Instantiate(CGame.INSTANCE._oCursor._Prefab_GizmoRotate);	break;
			case EEditMode.Scale:	oGizmoTran = (Transform)GameObject.Instantiate(CGame.INSTANCE._oCursor._Prefab_GizmoScale); 	break;
		}

		CGizmo oGizmoInPrefab = oGizmoTran.GetComponent<CGizmo>();
		if (oGizmoInPrefab == null)
			CUtility.ThrowException("ERROR: CGizmo could not find CGizmo component in Gizmo prefab!");

		oGizmoInPrefab.Initialize(oHotSpot, bMiddleClickOp);
		
		return oGizmoInPrefab;
	}

	void Initialize(CHotSpot oHotSpot, bool bMiddleClickOp) {
		_oHotSpot = oHotSpot;
		_bMiddleClickOp = bMiddleClickOp;
		_eEditMode = CGame.INSTANCE._oCursor._EditMode;		// Our gizmo mode is the mode of the cursor edit mode at startup.
		gameObject.name = "Gizmo-" + _eEditMode.ToString();

		//=== Set the initializing gizmo the position and rotation of the hotspot.  We have to move/rotate both at every frame ===
		transform.SetParent(null);								// Note that we are NOT a child of our owning hotspot...
		transform.position = _oHotSpot.transform.position;
		if (_eEditMode != EEditMode.Move)				// Only start at the object's current location if we're rotate or scale.  we always move with axis-aliged gizmo
			transform.rotation = _oHotSpot.transform.rotation;
		transform.localScale = new Vector3(CCursor.C_GizmoScale, CCursor.C_GizmoScale, CCursor.C_GizmoScale);		// Gizmo is always drawn at the default scale regardless of hotspot scaling.
		if (_eEditMode == EEditMode.Rotate)				// Magnify the rotation cursor a given ratio as it frequently needs a bigger size for easy user manipulation
			transform.localScale *= CCursor.C_GizmoScale_RotationMultiplier;

		//=== Handling of special 'hidden helper object' mode = Hide gizmo and immediately begin move opeation along y,z axis ===
		if (CGame.INSTANCE._bGameModeBasicInteractions) {	
			Renderer[] aRend = transform.GetComponentsInChildren<Renderer>();	//###IMPROVE? Instead of hiding all gizmo elements, would it be too tough to just not instantiate its prefab??
			foreach (Renderer oRend in aRend)
				oRend.enabled = false;
			_eModeGizmo = EModeGizmo.S2_UserDraggingGizmoPart;		// Direct fast-track to immediately drag gizmo...
			GizmoTransform_Begin(true);								// ... by forcing a move operation on YZ plane
		} else {
			_eModeGizmo = EModeGizmo.S1_WaitingLeftClickGizmoPart;
		}
	}

	public void OnUpdateGizmo() {						// Sent by the global cursor from 'Update()' to enable gizmo interactivity by trapping mouse events on various gizmo colliders
		
		//=== Potentially invert the gizmo along x,y,z so that it is always pointing toward the camera (Rotation gizmo is 3D symmetrical and never needs inversion ===
		if (_eEditMode != EEditMode.Rotate) {
			Vector3 vecGizmoToCam = Camera.main.transform.position - transform.position;
			float nAngleForward = Vector3.Angle(vecGizmoToCam, transform.forward);
			float nAngleRight   = Vector3.Angle(vecGizmoToCam, transform.right);
			float nAngleUp      = Vector3.Angle(vecGizmoToCam, transform.up);
			transform.localScale = new Vector3(nAngleRight < 90 ? CCursor.C_GizmoScale : -CCursor.C_GizmoScale, nAngleUp < 90 ? CCursor.C_GizmoScale : -CCursor.C_GizmoScale, nAngleForward < 90 ? CCursor.C_GizmoScale : -CCursor.C_GizmoScale);
		}
		
		//=== Process the gizmo finite state machine as progressed by left mouse click ===
		switch (_eModeGizmo) {

			case EModeGizmo.S1_WaitingLeftClickGizmoPart:	// User has completed the left mouse up & down that resulted in hotspot being activated and gizmo shown.  We now wait until left mouse button on a gizmo part to begin gizmo moving/rotating/scaling the object
				_oRayHit_LayerGizmo = CUtility.RaycastToCameraPoint2D(Input.mousePosition, CCursor.C_LayerMask_Gizmo);		// Obtain the user's hit on the gizmo collider layers
				CGizmo oGizmo = FindGizmoFromCollider(_oRayHit_LayerGizmo.collider);		//... See if the user clicked on a part of our gizmo...
				if (oGizmo != null) {
					if (_oHighlightedGizmoCollider != _oRayHit_LayerGizmo.collider) {
						if (_oHighlightedGizmoCollider) {
							if (_oHighlightedGizmoCollider.GetComponent<Renderer>() != null) {		//###CHECK21: Why does this fail??
								Color oColorGizmoCollider = _oHighlightedGizmoCollider.GetComponent<Renderer>().sharedMaterial.color;
								oColorGizmoCollider.a = oColorGizmoCollider.a / C_GizmoColliderHighlight;
								_oHighlightedGizmoCollider.GetComponent<Renderer>().sharedMaterial.color = oColorGizmoCollider;
							}
						}
						_oHighlightedGizmoCollider = _oRayHit_LayerGizmo.collider;
						if (_oHighlightedGizmoCollider) {
							if (_oHighlightedGizmoCollider.GetComponent<Renderer>() != null) {		//###CHECK21: Why does this fail??
								Color oColorGizmoCollider = _oHighlightedGizmoCollider.GetComponent<Renderer>().sharedMaterial.color;
								oColorGizmoCollider.a = oColorGizmoCollider.a * C_GizmoColliderHighlight;
								_oHighlightedGizmoCollider.GetComponent<Renderer>().sharedMaterial.color = oColorGizmoCollider;
							}
						}
					}
				}

				if (Input.GetMouseButtonDown(0)) {			// Trap left mouse down...
					if (oGizmo != null) {																	//... There should only be one gizmo globally but throw if we find another one!
						if (oGizmo != this)
							CUtility.ThrowException("ERROR: Found a gizmo collider that wasn't 'this' Gizmo");		// If this occurs there is a serious error in the flow that creates CGizmo objects and another one of is created & never removed on screen... dual creation during the same event??
						GizmoTransform_Begin();
						_eModeGizmo = EModeGizmo.S2_UserDraggingGizmoPart;
					}				
				}
				break;
			
			case EModeGizmo.S2_UserDraggingGizmoPart:			// The user is now left-clicking one of the three x,y,z colliders for the move or rotate gizmo for the currently editing Bone.  While the user keeps LMB down we move/rotate the selected Bone...
				if (Input.GetMouseButton(0) || Input.GetMouseButton(2)) {				// User is still holding LMB down on a collider of the current gizmo: Continue moving/rotating current Bone on previously select x,y,z axis
					GizmoTransform_Update();
				} else {									// User is no longer holding LMB: We are done transforming this x,y,z and we return to looking for the next gizmo x,y,z collider for further action
					GizmoTransform_End();
					_eModeGizmo = EModeGizmo.S1_WaitingLeftClickGizmoPart;
				}
				break;
		}
	}
	
	
	
	
	public void GizmoTransform_Begin(bool bForceMoveYZ = false) {		// Invoked at the beginning of gizmo operation when user first clicked on one of our x,y,z gizmo collider part
		//=== Create an INSTANCE of the 'cursor cutter' planar object.  This object is a simple plane collider arranged to cross the gizmo position and aligned parallel to the allowed transformation the current gizmo part allows (either XY, YZ, XZ)
		//###IMPROVE: Offset is visible when beginning animation... offset properly applied??

		_oPlaneCutter = (Transform)GameObject.Instantiate(CGame.INSTANCE._oCursor._Prefab_GizmoPlaneCutter);		// This plane will convert the rays from the camera to the mouse into a 3D position on the cutting plane.
		_oPlaneCutter.gameObject.SetActive(true);				// Prefab might have top object deactivated.  Make sure it is active.
		_oPlaneCutter.SetParent(transform);						// Temporarily assign plane cutter parent to gizmo so we can rotate to the object's local rotation below...
		_oPlaneCutter.localPosition = Vector3.zero;				// Zero our local position / rotation so we're coincident with hotspot / gizmo
		_oPlaneCutter.localRotation = Quaternion.identity;

		//=== Decode what part of the gizmo the user clicked on from the name of the collider part ===
		string sXYZ = bForceMoveYZ ? "X" : _oRayHit_LayerGizmo.collider.gameObject.name.Substring(1, 1);			// Cursor collider sub-parts all have <M/R>+<X,Y,Z> like MX or RZ.  Break down name of collider we just hit to determine x,y,z transformation axis	(Force move about X plane (moving Y,Z) when in forced MoveYZ mode ('hidden helper objects' game mode))
		switch (sXYZ) {
			case "X": _oPlaneCutter.localRotation = Quaternion.Euler(0, 0, 90.0f);	_vecRotAxisJoint = Vector3.left;	break;
			case "Y": _oPlaneCutter.localRotation = Quaternion.identity;			_vecRotAxisJoint = Vector3.up;		break;
			case "Z": _oPlaneCutter.localRotation = Quaternion.Euler(90.0f, 0, 0);	_vecRotAxisJoint = Vector3.forward;	break;
			default: Debug.LogWarning("*W: Invalid cursor XYZ"); break;
		}

		//=== Cutter plane is now correctly positioned & oriented.  We now decouple so cutting plane won't move as object moves/rotates ===
		_oPlaneCutter.SetParent(null);

		RaycastHit oRayHit_LayerCutter = CUtility.RaycastToCameraPoint2D(Input.mousePosition, CCursor.C_LayerMask_Cutter);		// Obtain the user's hit on the cutter to determine gizmo offset to mouse click

		//=== Store important information at start of gizmo operation for our calculations in GizmoTransform_Update() ===
		_vecGizmoPosAtStart = transform.position;									// Remember where the gizmo was at start.  (Needed for scaling)
		_vecFirstClickOffsetToGizmo = oRayHit_LayerCutter.point - _vecGizmoPosAtStart;		// Remember the offset of the point where the user clicked to where gizmo is to avoid 'snapping object' to gizmo upon next update

		//=== Initialize gizmo-dependant variables that will be needed in GizmoTransform_Update() ===
		switch (_eEditMode) {
			case EEditMode.Move:
				break;
			case EEditMode.Rotate:				// For rotation gizmo we need to remember the original rotation quaternion to facilitate applying the delta angle over the rotation plane
				_quatRotStart_Gizmo = transform.rotation;			// All rotation will occur relative to our starting quaternion
				Vector3 vecDiffGizmoPt2Obj_ORIG = _oPlaneCutter.worldToLocalMatrix.MultiplyPoint(oRayHit_LayerCutter.point);		// Map the hit point to the cutting plane coordinates, as it is our reference for AngleAxis conversion
				_nAngleCutterSpace_Start = -Mathf.Rad2Deg*Mathf.Atan2(vecDiffGizmoPt2Obj_ORIG.z, vecDiffGizmoPt2Obj_ORIG.x);	// This angle is the 'starting point' on the cutting plane where user originally clicked.  In later updates we calculate angle the same way and apply diffference
				break;
			case EEditMode.Scale:
				//####OBS?? _vecScaleAtStart = _oHotSpot.transform.localScale;	//***BUG?? Object instead??	// Remember our starting scale so we can multiply scale from this starting value (Otherwise we'd reset scale to 1 at the start of every gizmo scale operation)
				break;
		}
		_oHotSpot.OnHotspotChanged(this, _eEditMode, EHotSpotOp.First);	// Notify hotspot that is the first operation  ###CHECK: Valid coords??
	}
	
	public void GizmoTransform_End() {				// User is no longer dragging a gizmo collider... We simply parent the gizmo to the Bone and reset its position/rotation for automatic position / rotation
		if (_oPlaneCutter) {
			Destroy(_oPlaneCutter.gameObject);		// Delete our temporary cutter cursor.
			_oPlaneCutter = null;
		}
		_oHotSpot.OnHotspotChanged(this, _eEditMode, EHotSpotOp.Last);		// Notify hotspot that is the last operation
	}
	
	public void GizmoTransform_Update() {		// User is actively dragging one of the gizmo parts to move/rotate/scale our hotspot.  Read on collider plane where mouse cursor is and adjust object

		RaycastHit oRayHit = CUtility.RaycastToCameraPoint2D(Input.mousePosition, CCursor.C_LayerMask_Cutter);		// We now only do hit test on cutter plane
		if (_vecRayHitPoint_Last == oRayHit.point)			// If user hasn't move the mouse in the last frame don't do anything
			return;
		_vecRayHitPoint_Last = oRayHit.point;
		
		switch (_eEditMode) {
			case EEditMode.Move:
				transform.position = oRayHit.point - _vecFirstClickOffsetToGizmo;		// We subtract the first-click offset at each update to maintain the start relationship between the user's first click and starting gizmo
				break;
			case EEditMode.Rotate:				// Calculate the new position of the gizmo by rotating around the cutting plane
				Vector3 vecDiffGizmoPt2Obj_NowL = _oPlaneCutter.worldToLocalMatrix.MultiplyPoint(oRayHit.point);	// Map the hit point to the cutting plane coordinates, as it is our reference for AngleAxis conversion
				float nAngleCutterSpace_Now = -Mathf.Rad2Deg*Mathf.Atan2(vecDiffGizmoPt2Obj_NowL.z, vecDiffGizmoPt2Obj_NowL.x);	// Calculate the angle from x and z with atan2
				float nAngleDelta = nAngleCutterSpace_Now-_nAngleCutterSpace_Start;				// When the user first click a x,y,z collider he/she had an original angle around the wheel (probably din't click at angle=0)... subtract this angle here to determine how much the wheel moved
				transform.rotation = _quatRotStart_Gizmo;										// Assign at every frame the staring state...
				transform.Rotate(_vecRotAxisJoint, nAngleDelta);		// And then rotate the starting point around the cutting plane by the delta angle about the rotation axis globally-aligned vector (Not too sure why it needed globally oriented, only one that works!)  ###INFO: Rotate accepts degrees

				break;
			case EEditMode.Scale:
				Vector3 vecMovementSinceStart = oRayHit.point - _vecGizmoPosAtStart;
				float nScaleX = (_vecFirstClickOffsetToGizmo.x != 0f) ? (vecMovementSinceStart.x / _vecFirstClickOffsetToGizmo.x) : 1f;		// Calculate the current scaling while clamping to 1 (no-op scale change) the axis we are currently not operating on
				float nScaleY = (_vecFirstClickOffsetToGizmo.y != 0f) ? (vecMovementSinceStart.y / _vecFirstClickOffsetToGizmo.y) : 1f;
				float nScaleZ = (_vecFirstClickOffsetToGizmo.z != 0f) ? (vecMovementSinceStart.z / _vecFirstClickOffsetToGizmo.z) : 1f;
				nScaleX = Mathf.Max(nScaleX, 0);		// Prevent scale on any axis from going negative. (That would screw up visual appearance of gizmo leading to inverted rotations, etc)
				nScaleY = Mathf.Max(nScaleY, 0);
				nScaleZ = Mathf.Max(nScaleZ, 0);	//###DESIGN!!! What to do during scale???
				//_oHotSpot.transform.localScale = new Vector3(_vecScaleAtStart.x * nScaleX, _vecScaleAtStart.y  * nScaleY, _vecScaleAtStart.z * nScaleZ);	// We only change scale of hotspot, not of gizmo	// OnHotspotChanged() is now responsible for moving/rotate/scale
				break;
		}
		_oHotSpot.OnHotspotChanged(this, _eEditMode, EHotSpotOp.Middle);			// Notify hotspot that is has changed and is not the last operation	//###DESIGN!?!?!  ###BROKEN?? Check that scale is still working
	}
	
	public CGizmo FindGizmoFromCollider(Collider oCollider) {		// Utility function to safely find the gizmo object from the collider user clicked on.
		if (oCollider == null)
			return null;
		if (oCollider.transform.parent == null)
			return null;
		Transform oColParentParent = oCollider.transform.parent.parent;
		if (oColParentParent == null) 
			return null;
		CGizmo oGizmo = oColParentParent.GetComponent<CGizmo>();
		return oGizmo;
	}
}

public enum EModeGizmo {
	S1_WaitingLeftClickGizmoPart,
	S2_UserDraggingGizmoPart,
}
