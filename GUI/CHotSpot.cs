using UnityEngine;
using System;
using System.Collections;

public class CHotSpot : MonoBehaviour {				// Represents a 'hotspot' object that visually or invisibly indicates to user an action can be done with this object under the mouse.  Supports both 3D editing (moving / rotate / scale) activation and context-menu activation
	
						public 	EHotSpotType _HotspotType;				// What we do when the user activates us.
	[HideInInspector] 	public 	Component	_oEditingObject;			// The optional 'payload' object we edit through a context menu / dialog through reflection.  This object obtains notification of our editing actions as well as gets analyzed through reflection for context editing through iGUICode_RootPanel.
	[HideInInspector] 	public 	IHotSpotMgr _iHotSpotMgr;				// The object we notify upon hotspot modifications
	[HideInInspector] 	public 	string		_sNameHotspot;
	[HideInInspector] 	public 	Transform	_oParentT;					// The transform of our owning parent (usually same object as _iHotSpotMgr
	[HideInInspector] 	public 	Vector3		_vecPosAtStartup;			// Our position as it was set during initialization.  Used to return delta position in GetPosChangeSinceStartup()
	[HideInInspector] 	public 	float		_nScaleAtStartup;			// Our scale at startup.  Used to calculate change in scale in GetScaleChangeSinceStartup()
	[HideInInspector] 	public 	Color		_oColorUnselected;
	[HideInInspector] 	public 	Color		_oColorSelected;
	[HideInInspector] 	public 	Color		_oColorActivated;
	
	[HideInInspector] 	public 	bool		_bEnableEditing = true;		// When true a left click on hotspot reveals the gizmo for 3D editing (move / rotate / scale)

	const float C_RatioColor_Highlight	= 1.25f;		// Ratio to multiply r,g,b when hotspot is highlighted
	const float C_RatioColor_Activated	= 1.50f;		// Ratio to multiply r,g,b when hotspot is activated
	const float C_TransparencyLevel		= 0.4f;		// How transparent to make hotspots


	//---------------------------------------------------------------------------	STATIC CREATION
	public static CHotSpot CreateHotspot(IHotSpotMgr iHotSpotMgr, Transform oParentT, string sNameHotspot, bool bEnableEditing, int nLayer, float nScaleMult = 1.0f, Vector3 vecPosLocalOffset = new Vector3()) {
		float nScale = CCursor.C_HotSpot_DefaultSize * nScaleMult;
        CHotSpot oHotSpot = CUtility.InstantiatePrefab<CHotSpot>("Gizmo/Prefabs/CHotSpot", sNameHotspot, oParentT);     //###WEAK: Scale constants everwhere as absolute values?  Make relative to some well known size?
		oHotSpot.Initialize(iHotSpotMgr, oParentT, sNameHotspot, bEnableEditing, nLayer, nScale, vecPosLocalOffset);
		return oHotSpot;
	}

	//---------------------------------------------------------------------------	INIT
	public void Initialize(IHotSpotMgr iHotSpotMgr, Transform oParentT, string sNameHotspot, bool bEnableEditing, int nLayer, float nScale, Vector3 vecPosLocalOffset) {
		_iHotSpotMgr	= iHotSpotMgr;
		_oParentT		= oParentT;
		_sNameHotspot	= sNameHotspot;
		transform.name = "Hotspot-" + sNameHotspot;
		transform.SetParent(_oParentT);
		transform.localPosition = vecPosLocalOffset;
		transform.localRotation = Quaternion.identity;
		_bEnableEditing = bEnableEditing;
		_vecPosAtStartup = transform.position;
		_nScaleAtStartup = nScale;
		transform.localScale = new Vector3(_nScaleAtStartup, _nScaleAtStartup, _nScaleAtStartup);
		
		gameObject.layer = nLayer;	// All hotspots have to exist in the hotspot layer in order for CCursor to detect them.
		GetComponent<Collider>().isTrigger = true;
		GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Transparent/Diffuse"));
		_oColorUnselected = new Color(UnityEngine.Random.Range(0.1f,0.6f), UnityEngine.Random.Range(0.1f,0.6f), UnityEngine.Random.Range(0.1f,0.6f), C_TransparencyLevel);
		_oColorSelected   = new Color(_oColorUnselected.r*C_RatioColor_Highlight, _oColorUnselected.g*C_RatioColor_Highlight, _oColorUnselected.b*C_RatioColor_Highlight, _oColorUnselected.a*C_RatioColor_Highlight);
		_oColorActivated  = new Color(_oColorUnselected.r*C_RatioColor_Activated, _oColorUnselected.g*C_RatioColor_Activated, _oColorUnselected.b*C_RatioColor_Activated, _oColorUnselected.a*C_RatioColor_Activated);
		GetComponent<Renderer>().sharedMaterial.color = _oColorUnselected;
	}
	
	public void OnDestroy() {
		Destroy(gameObject);		//###CHECK!!
	}

	void OnTriggerEnter(Collider oCol) { /*Debug.Log("TriggerI: " + gameObject.name + " - " + oCol);*/ _iHotSpotMgr.OnHotspotEvent(EHotSpotEvent.TriggerEnter, oCol); }	//###OPT!!!! Check how frequently these are called!
	void OnTriggerExit (Collider oCol) { /*Debug.Log("TriggerO: " + gameObject.name + " - " + oCol);*/ _iHotSpotMgr.OnHotspotEvent(EHotSpotEvent.TriggerExit , oCol); }

	//---------------------------------------------------------------------------	HOVERING
	public void OnHoverBegin() {
		GetComponent<Renderer>().sharedMaterial.color = _oColorSelected;
		CGame._oCursor.SetCursorText(_sNameHotspot);
		CGame._oCursor.SetCursorColor(new Color(0,128,0));		// Change the inner part of the arrow to indicate a 'hotspot' is currently under the mouse cursor.			//###DESIGN: Change cursor text based on what is under cursor now?
	}
	public void OnHoverEnd() {
		GetComponent<Renderer>().sharedMaterial.color = _oColorUnselected;
		CGame._oCursor.SetCursorText("");
		CGame._oCursor.SetCursorColor(Color.white);
	}


	//---------------------------------------------------------------------------	ACTIVATION
	public void OnActivate(bool bClickLeft, bool bClickRight, bool bClickMiddle) {						// The user has left-clicked or right-click activated us!  We activate our 'payload' by performing its default action
		GetComponent<Renderer>().sharedMaterial.color = _oColorActivated;
		CGame._oCursor.SetCursorText("");
		CGame._oCursor.SetCursorColor(new Color(0,255,0));		// Change the inner part of the arrow to indicate a 'hotspot' is currently under the mouse cursor.			//###DESIGN: Change cursor text based on what is under cursor now?

		OnHotspotEvent(EHotSpotEvent.Activation, null);					// Send activation event

		if (bClickLeft || bClickMiddle) {
			if (_bEnableEditing) {		//###CHECK
				CGame._oCursor._oGizmo = CGizmo.CreateGizmo(this, bClickMiddle);
			}
		} else if (bClickRight) {			// The user has right-clicked us!  We activate our 'payload' for context sensitive (typically display a context sensitive menu / editing panel)
			OnHotspotEvent(EHotSpotEvent.ContextMenu, null);
		}
	}
	
	public void OnDeactivate() {						// The user has left-clicked or right-click activated us!  We activate our 'payload' by performing its default action
		OnHotspotEvent(EHotSpotEvent.Deactivation, null);
		if (CGame._oCursor._oGizmo)
			Destroy(CGame._oCursor._oGizmo.gameObject);
	}


	//---------------------------------------------------------------------------	UPDATE
	public void OnUpdate_ActiveHotspot() {		// Called only on the active hotspot: typically to reroute event processing to the gizmos we created/manage...
		if (CGame._oCursor._oGizmo)
			CGame._oCursor._oGizmo.OnUpdateGizmo();
	}

	//---------------------------------------------------------------------------	EVENTS
	public virtual void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) {
		if (_iHotSpotMgr != null)
			_iHotSpotMgr.OnHotspotChanged(oGizmo, eEditMode, eHotSpotOp);		// Notify our manager that we have changed.
	}

	public virtual void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {		//###DESIGN!!: Do these stubs have any value???
		if (_iHotSpotMgr != null)
			_iHotSpotMgr.OnHotspotEvent(eHotSpotEvent, o);
	}

	//---------------------------------------------------------------------------	UTILITY
	public void OnBroadcast_HideOrShowHelperObjects(bool bShow) {
		GetComponent<Renderer>().enabled = bShow;
	}
	
	public Vector3 GetPositionChangeSinceStartup() {
		return transform.position - _vecPosAtStartup;
	}
	public Vector3 GetScaleChangeSinceStartup() {
		return new Vector3(transform.localScale.x / _nScaleAtStartup, transform.localScale.y / _nScaleAtStartup, transform.localScale.z / _nScaleAtStartup);		
	}

	//---------------------------------------------------------------------------	CREATION HELPERS

	//public void WndPopup_Create(CUICanvas oCanvas, CObj[] aObjects, float nX = -1, float nY = -1) {			// Create a popup window capable of end-user editing of the public properties of this object.
	//	CUtility.WndPopup_Create(oCanvas, EWndPopupType.PropertyEditor, aObjects, _sNameHotspot, nX, nY);
	//}
}

public enum EHotSpotType {
	ShowGizmo,					// When activated hotspot will activate the move, rotate or scale gizmo the user can use to move/rotate/scale the current object
	PopupPanel					// When activated hotspot will draw the 'popup panel' that presents user sliders to interactively control the 'super public' members of current object
}

public enum EHotSpotOp {	// First, middle or last operation argument provided in OnHotspotChanged
	First,
	Middle,
	Last
}
public enum EHotSpotEvent {
	Activation,				// User clicked on the hotspot.
	Deactivation,			// User deactivated the hotspot.
	ContextMenu,			// User right clicked on hotspot... typically show context menu.
	TriggerEnter,			// A collider has entered the hotspot trigger
	TriggerExit				// A collider has left the hotspot trigger
}

public interface IHotSpotMgr {
	void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp);	// Hotspot has been moved / rotated / scale
	void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o);															// User invoked context menu (right click) on object.
}


//public static void ChangePropByHotSpotMove(CObj oObj, CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp, float nScale) {
//	// Change our property value by user moving hotspot up / down.  (Example: changing penis base up/down)
//	if (eEditMode == EEditMode.Move) {
//		switch (eHotSpotOp) {
//			case EHotSpotOp.First:
//				oObj._nHotSpotEdit_BeginPosY = oGizmo.transform.position.y;
//				oObj._nHotSpotEdit_BeginValue = oObj._nValue;
//				break;
//			case EHotSpotOp.Middle:
//				float nValue = oObj._nHotSpotEdit_BeginValue + (oGizmo.transform.position.y - oObj._nHotSpotEdit_BeginPosY) * nScale;
//				oObj.Set(nValue);
//				SetGuiMessage(EMsg.MouseEdit, string.Format("{0} = {1:F1}", oObj._sName, oObj.Get()));
//				break;
//			case EHotSpotOp.Last:
//				SetGuiMessage(EMsg.MouseEdit, null);		//###DESIGN: Consider resetting these OnGUI strings at the start of each frame???
//				break;
//		}
//	}
//}
