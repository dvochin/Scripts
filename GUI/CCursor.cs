/*###DISCUSSION: Gizmos
=== REVIVE ===
 * Need to set cursor to depth of UI when on UI


=== DESIGN ===
 * Need to finalize relationship between hotspot and the object it controls (parent)
 * Need to notify objects upon effect... always notify parent of hotspot? See if we can use .net events and delegates to cleanup the code more.
 * CHotspot routing to create panel through reflection clever... revisit and refresh.
 * Keep highlights the way they are or do we swap materials to give user more options?

=== PROBLEMS ===
 * Rotation problem also makes gizmo no longer reorient itself toward viewer??
 * Rotate broken...  assumes we start at zero angle??
 * Depth/colors still adjusted when moving gizmo.
 * Hotspot scaling again!
 * Design orientation problem with resize when gizmo oriented wrong way... need way to avoid gizmo flipping!
 *? Pins offset when original pin! (even though they appear at same loc)
 * Rotate broken
 * Gizmos too small and not increasing!
 * Have popup appear if on same object for mouse down and up.
 * Can't invoke context menu if object selected!
 * Right click doesn't cancel edit
 * 

=== WISHLIST ===
 * Rotate cursor too small, scaling not uniform with move/scale. Fix in 3dsMax??
 * Symmetry edit triggerable by scroll lock
 * Color code things and stick to it
 * Have local and global move?
 * Redrawing with highlight borders would be nice!
 * ? Centralize the creation/destruction of these hotspots dependent on game mode... with change of game modes automatically flushing the old hotspots
 
=== TODO ===
 * Pinned objects different colors...
 * Move all global cursor / gizmo / hotspot stuff into CCursor master class and own folder and make everything completely standalone for easy sell.

=== NOW ===
 * Make arrows/scale boxes move/scale along x,y,z

*/

using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

//---------------------------------------------------------------------------	CCursor: Singleton-INSTANCE the performs cursor movement, selection, gizmo management and object movement/rotation/scale.
public class CCursor : MonoBehaviour {
	
	//---------------------------------------------------------------------------	MAIN MEMBERS
	[HideInInspector] 	public 	EEditMode 	_EditMode;					// The current edit mode for the game (select, move, rotate, scale)

	[HideInInspector] 	public 	CHotSpot	_oHotSpotCurrent;			// The hotspot the user is currently hovering over / editing
	[HideInInspector] 	public 	CGizmo		_oGizmo;					// The current gizmo we have shown on screen / own.  Null if not editing any object.

	//---------------------------------------------------------------------------	GUI PROPERTIES
						public 	float		_DefaultCursorDepthOnHotspots = 0.90f;			// Distance between the selected Hotspot and camera... how far cursor goes 'into the scene' -> critical for 3D  ###TUNE!

	//---------------------------------------------------------------------------	INTERNAL MEMBERS
								EModeCursor	_eModeCursor  = EModeCursor.S1_SearchingForHotspot;
	
								float 		_nDepth = 2.0f;				//###TEMP:  Default value at startup irrelevant since we have to calculate non-selected depth from distance to closest body
								float		_nTimeAtMouseButtonDown;	// Time.time value when user pressed mouse button.  Used to timeout a 'mouse click' that took too long
								Vector3		_vecPosAtMouseButtonDown;	// Input.mousePosition at button down.  Used to cancel clicks when user moving the mouse too much (e.g. moving camera orbit)
								TextMesh 	_oTextMesh;					// Reference to the textmesh component of our child 'Text' node.  Store for quick change to cursor text
	[HideInInspector] 	public	RaycastHit 	_oRayHit_LayerHotSpot;		// The last hit colliders

	//---------------------------------------------------------------------------	RESOURCES	
	[HideInInspector] 	public 	Transform	_Prefab_GizmoMove;			// All prefabs loaded with Resources.Load during initialization
	[HideInInspector] 	public 	Transform	_Prefab_GizmoRotate;
	[HideInInspector] 	public 	Transform	_Prefab_GizmoScale;
	[HideInInspector] 	public 	Transform 	_Prefab_GizmoPlaneCutter;
	[HideInInspector] 	public 	Transform 	_Prefab_HotSpot;			// Link to a 'HotspotHotSpot' prefab that has a trigger sphere collider on layer Overlay, a kinematic rigid body and a mesh rendering our pointy directional cube

	//---------------------------------------------------------------------------	CONSTANTS

						public const float	C_GizmoScale = 0.8f;						// The base size of all gizmos (move / rotate / size)
						public const float	C_GizmoScale_RotationMultiplier = 3.0f;		// Extra multiplier applied to only the rotate gizmo
						public const float	C_HotSpot_DefaultSize	= 0.1f;

						public const float	C_TimeMaxForMouseClick		= 0.2f;		// Mouse clicks longer then this do not do anything ###TUNE


    public Transform _oCurrentGuiObjOwnerect_HACK = null;                    // Current Unity GUI object under the cursor.  Comes from our CUIPanel and used for cursor 3D depth adjustment.


	
	public static CCursor Cursor_Create() {
		Transform oNodeCursor = CUtility.FindChild(Camera.main.transform, "Cursor");		//###WEAK!!!  Can be simplified!  Create from prefab!
		CCursor oCursor = oNodeCursor.GetComponent<CCursor>();
		oCursor.Initialize();
		return oCursor;
	}

	
	//---------------------------------------------------------------------------	INITIALIZATION
	void Initialize() {
		Debug.Log ("=== CCursor.Initialize() ===");

		//=== Preload all resources we'll need for quick runtime instantiation ===
		_Prefab_GizmoMove			= Resources.Load("Gizmo/Prefabs/CGizmoMove", 			typeof(Transform)) as Transform;
		_Prefab_GizmoRotate			= Resources.Load("Gizmo/Prefabs/CGizmoRotate", 			typeof(Transform)) as Transform;
		_Prefab_GizmoScale			= Resources.Load("Gizmo/Prefabs/CGizmoScale", 			typeof(Transform)) as Transform;
		_Prefab_GizmoPlaneCutter	= Resources.Load("Gizmo/Prefabs/CGizmoPlaneCutter", 	typeof(Transform)) as Transform;
		_Prefab_HotSpot				= Resources.Load("Gizmo/Prefabs/CHotSpot", 				typeof(Transform)) as Transform;
		
		SetEditMode(EEditMode.Move);		// At app-start we are in edit mode

		_oTextMesh = transform.Find("Text").GetComponent<TextMesh>();		//###BUG: Cursor text size grow up / down with zoom
	}
	

	
	//---------------------------------------------------------------------------	UPDATE
	public void OnUpdate_Cursor() {					// Called in CGame.update() to update our cursor
		//=== Switch edit mode if the corresponding key was pressed ===
		//###DESIGN!!!!! How / when to map these important keys???			###TODO!!!!  Only change mode when in pose edit mode, enforce 'play mode' to always move!

		if (CGame._bGameModeBasicInteractions == false) {
			if (Input.GetKeyDown(KeyCode.T)) {					// Toggle between move and rotate mode when not in basic game interaction mode
				if (_EditMode == EEditMode.Move)
					SetEditMode(EEditMode.Rotate);
				else
					SetEditMode(EEditMode.Move);
			}
		}

		//=== Determine which layer we search for hotspot on ===
		int nLayerTarget = G.C_Layer_HotSpot;					//###BUG??? Send this layer mask to derived classes???
		//if (Input.GetKey(KeyCode.Space))					//###CHECK: Proper?  Hunt for hotspots on hand layer if proper key pressed
		//	nLayerTarget = C_Layer_HotSpotHands;
		uint nLayerTargetMask = (uint)1 << nLayerTarget;

		//###WEAK?  The changes above to cursor mode operation are specific to the game and take away some of the generic behavior that may be useful for other purposes
             if (Input.GetKeyDown(KeyCode.W))	SetEditMode(EEditMode.Move);		// W = Move
		else if (Input.GetKeyDown(KeyCode.T))	SetEditMode(EEditMode.Rotate);      // T = roTate               //###WEAK: Conflicts with reset!  Which to keep??
        //else if (Input.GetKeyDown(KeyCode.E)) 	SetEditMode(EEditMode.Scale);		// E = Expand = Scale		//###DESIGN: Any value in scale / select for game??
        //else if (Input.GetKeyDown(KeyCode.Q))	SetEditMode(EEditMode.Select);		// Q = Select

        //=== Test what collider is under the mouse cursor.  This is used to process the various stages of mouse interactivity as well as to adjust the '3D depth' of the cursor ===
        CGame.SetGuiMessage(EMsg.CursorStat1, "Cursor Mode: " + _eModeCursor.ToString());
        _oRayHit_LayerHotSpot = CUtility.RaycastToCameraPoint2D(Input.mousePosition, -1);
        if (_oRayHit_LayerHotSpot.collider != null)
            CGame.SetGuiMessage(EMsg.CursorStat2, "Cursor Collider: " + _oRayHit_LayerHotSpot.transform.name);
        else
            CGame.SetGuiMessage(EMsg.CursorStat2, "Cursor Collider: None");
        _oRayHit_LayerHotSpot = CUtility.RaycastToCameraPoint2D(Input.mousePosition, (int)nLayerTargetMask);
		if (_oRayHit_LayerHotSpot.collider != null)
			_nDepth = _oRayHit_LayerHotSpot.distance * _DefaultCursorDepthOnHotspots;         // We adjust the '3D depth' of mouse cursor only when a collider is found.  ###IMPROVE: Implement slerp to gracefully change depth?



  //      GameObject oSelectedUI = EventSystem.current.currentSelectedGameObject;     // EventSystem.current.IsPointerOverGameObject()) {        // If cursor is over Unity UI widget set cursor directly there and don't process further cursor functionality
  //      if (oSelectedUI != null) { 
		//    CGame.SetGuiMessage(EMsg.CursorStat3, "Cursor GUI: " + oSelectedUI.name);
  //          //transform.position = Camera.main.ScreenToWorldPoint(;
  //          //PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
  //          //eventDataCurrentPosition.position = screenPosition;
  //          //GraphicRaycaster uiRaycaster = canvas.gameObject.GetComponent<GraphicRaycaster>();
  //          //List<RaycastResult> results = new List<RaycastResult>();
  //          //uiRaycaster.Raycast(eventDataCurrentPosition, results);
  //          //return results.Count > 0;
  //          _nDepth = Vector3.Distance(oSelectedUI.transform.position, Camera.main.transform.position);
		//} else {
  //          CGame.SetGuiMessage(EMsg.CursorStat3, "(Cursor: No GUI control)");
  //      }


        if (_oCurrentGuiObjOwnerect_HACK != null)        //###CHECK: Proper transforms for VR??  For SpaceNavigator??
            _nDepth = Vector3.Distance(_oCurrentGuiObjOwnerect_HACK.position, Camera.main.transform.position);     //###DESIGN: Override hotspot depth above??   ###IDEA: Have cursor a child of panel when it is hovering on top??


        Vector3 vecMouse2D = new Vector3(Input.mousePosition.x, Input.mousePosition.y, _nDepth);		//###OPT: Cache localPosition??
		Vector3 vecMouse3D = Camera.main.ScreenToWorldPoint(vecMouse2D);
		transform.position = vecMouse3D;


        if (_oCurrentGuiObjOwnerect_HACK != null)
            return;			//####SOON ####PROBLEM: Detecting what widget for width (solution offered in post)



        bool bClickLeftDown		= Input.GetMouseButtonDown(0);
		bool bClickRightDown	= Input.GetMouseButtonDown(1);
		bool bClickMiddleDown	= Input.GetMouseButtonDown(2);
		bool bClickDown = bClickLeftDown || bClickRightDown || bClickMiddleDown;

		//=== Process the finite state machine that systematically walks through each step of hotspot search, hovering, activation with bridge to hotspot's OnUpdate_HotSpot() for further 'OnUpdate()' processing ===
		switch (_eModeCursor) {
			case EModeCursor.S1_SearchingForHotspot:					// Default/startup mode: We don't have an interactive selected and are looking for one through 'mouse hover'
				if (_oRayHit_LayerHotSpot.collider != null) {
					_oHotSpotCurrent = _oRayHit_LayerHotSpot.collider.GetComponent<CHotSpot>();
					if (_oHotSpotCurrent) {
						_oHotSpotCurrent.OnHoverBegin();
						_eModeCursor = EModeCursor.S2_HoveringOverHotspot;
					}
				} else {
					//if (iGUICode_Root.INSTANCE._oPaneContextMenu != null) {								// We kill panel when we're not hovering over any Gui collider and panel exists. (makes it have a very short lifespan exactly like a 'tooltip')
					//	iGUICode_Root.INSTANCE._oPaneContextMenu.removeAll();
					//	iGUICode_Root.INSTANCE._oPaneContextMenu.setEnabled(false);			//###IMPROVE!!! Implement 'tooltip-like' functionality for popup menu?
					//	iGUICode_Root.INSTANCE._oPaneContextMenu = null;
					//}				
				}
				break;

			case EModeCursor.S2_HoveringOverHotspot:							// The user is now hovering over a valid Hotspot, but has not yet selected it with left mouse button.  (3D cursor displays name of Hotspot for visual feedback)
				if (bClickDown) {
					if (CGame._bGameModeBasicInteractions == false || bClickRightDown) {	// If we're in full edit mode we wait for up-click, activate and display full gizmo.
						_eModeCursor = EModeCursor.S3_WaitingForMouseUp;
					} else {													// If we're in reduced edit mode, we fast-track to immediate movement along Y,Z axis without showing gizmo
						_oHotSpotCurrent.OnActivate(bClickLeftDown, bClickRightDown, bClickMiddleDown);
						_eModeCursor = EModeCursor.S4_ActivatedHotspot;
					}
					_nTimeAtMouseButtonDown = Time.time;			// Remember the start time of mouse button down to prevent acting on very long mouse clicks
					_vecPosAtMouseButtonDown = Input.mousePosition;	// Remember mouse position so we can cancel if too far on mouse up
				} else {			// No click on hovering hotspot, check to see if we're still on a hotspot and cancel if not.
					CHotSpot oHotSpot = _oRayHit_LayerHotSpot.collider ? _oRayHit_LayerHotSpot.collider.GetComponent<CHotSpot>() : null;
					if (oHotSpot != _oHotSpotCurrent) {
						_oHotSpotCurrent.OnHoverEnd();
						_oHotSpotCurrent = null;
						_eModeCursor = EModeCursor.S1_SearchingForHotspot;
					}
				}
				break;

			case EModeCursor.S3_WaitingForMouseUp:							// The user has down-clicked a hotspot.  We await up-click to see if same hotspot is still under the mouse before activating the hotspot
				bool bClickLeftUp		= Input.GetMouseButtonUp(0);
				bool bClickRightUp		= Input.GetMouseButtonUp(1);
				bool bClickMiddleUp		= Input.GetMouseButtonUp(2);
				if (bClickLeftUp|| bClickRightUp) {
					if (_nTimeAtMouseButtonDown + C_TimeMaxForMouseClick >= Time.time && (Input.mousePosition - _vecPosAtMouseButtonDown).magnitude < 10) {
						CHotSpot oHotSpot = _oRayHit_LayerHotSpot.collider ? _oRayHit_LayerHotSpot.collider.GetComponent<CHotSpot>() : null;
						if (oHotSpot == _oHotSpotCurrent) {			// User has released mouse button on the same hotspot as mouse down.  Activate the hotspot
							_oHotSpotCurrent.OnActivate(bClickLeftUp, bClickRightUp, bClickMiddleUp);
							_eModeCursor = EModeCursor.S4_ActivatedHotspot;
						} else {											// User has released button on nothing or on another hotspot.  Return to start state
							_oHotSpotCurrent.OnHoverEnd();
							_oHotSpotCurrent = null;
							_eModeCursor = EModeCursor.S1_SearchingForHotspot;
						}
					} else {		// User took too long, just ignore both mouse down & mouse up and return to search state
						_eModeCursor = EModeCursor.S1_SearchingForHotspot;
					}
				}
				break;

			case EModeCursor.S4_ActivatedHotspot:							// The user is now hovering over a valid Hotspot, but has not yet selected it with left mouse button.  (3D cursor displays name of Hotspot for visual feedback)
				if (bClickDown) {
					RaycastHit oRayHit_LayerHotSpotAndGizmo = CUtility.RaycastToCameraPoint2D(Input.mousePosition, (int)nLayerTargetMask | G.C_LayerMask_Gizmo);	// Test to see if user clicked on nothing in gizmo or hotspot layers (in 'void')

					if (oRayHit_LayerHotSpotAndGizmo.collider == null) {			// If user didn't click on anything we cancel hotspot activation and return to start state.
						_oHotSpotCurrent.OnDeactivate();
						_oHotSpotCurrent.OnHoverEnd();
						_oHotSpotCurrent = null;
						_eModeCursor = EModeCursor.S1_SearchingForHotspot;
					//} else {												// We hit a valid hotspot or gizmo collider...	//###BROKEN! Was creating multiple gizmos!
					//	CHotSpot oHotSpot = _oRayHit_LayerHotSpot.collider ? _oRayHit_LayerHotSpot.collider.GetComponent<CHotSpot>() : null;
					//	if (oHotSpot != null) {
					//		if (oHotSpot == _oHotSpotCurrent) {				// If user clicked same hotspot simply route the 'activate' call to it so it can display menu or start editing
					//			_oHotSpotCurrent.OnActivate(bClickLeftDown, bClickRightDown);	//###IMPROVE: Test and add quick activation??
					//		//} else {										// If user clicked another hotspot we deactivate the old one and activate the new
					//		//	_oHotSpotCurrent.OnDeactivate();
					//		//	_oHotSpotCurrent.OnHoverEnd();
					//		//	_oHotSpotCurrent = oHotSpot;
					//		//	_oHotSpotCurrent.OnHoverBegin();
					//		//	_oHotSpotCurrent.OnActivate(bClickLeftDown, bClickRightDown);		//###CHECK!
					//		}
					//	}													// In this if clause is also the possibility of a left/right click on a gizmo collider.  This is handled below by hotspot
					}
				}
				if (_oHotSpotCurrent != null) {				// Only the active hotspot gets the chance to trap events (typically to reroute to the gizmo it manages) ===
					_oHotSpotCurrent.OnUpdate_ActiveHotspot();
					//=== Handle special case if we're not showing helper object and the left or middle mouse button is not pressed -> destroy everything and returning to start state to complete the highly transient operation
					if (CGame._bGameModeBasicInteractions && Input.GetMouseButton(0) == false && Input.GetMouseButton(2) == false) {	
						_oHotSpotCurrent.OnDeactivate();
						_oHotSpotCurrent.OnHoverEnd();
						_oHotSpotCurrent = null;
						_eModeCursor = EModeCursor.S1_SearchingForHotspot;
					}
				}
				break;
		}
	}

	

	
	//---------------------------------------------------------------------------	PUBLIC MEMBERS
	public void SetEditMode(EEditMode eEditMode) {			// Sets the edit mode safely.  (Needed if a hotspot is currently activated)
		if (_EditMode == eEditMode)
			return;
		if (_oHotSpotCurrent) {								// Temporarily deactivate current hotspot...
			_oHotSpotCurrent.OnDeactivate();
			_oHotSpotCurrent.OnHoverEnd();
		}
		_EditMode = eEditMode;								//... adopt the new mode...
		Debug.Log("Edit mode now set to '" + _EditMode.ToString() + "'");
		if (_oHotSpotCurrent) {								//... and re-activate hotspot with the new mode.  This will re-initialize correctly for gizmo operation with the new mode
			_oHotSpotCurrent.OnHoverBegin();
			_oHotSpotCurrent.OnActivate(true, false, false);
		}
	}

	public void CancelEditing() {			// Cancels editing by deactivating hots pot which will destroy gizmo if it exists.
		if (_oHotSpotCurrent) {				
			_oHotSpotCurrent.OnDeactivate();
			_oHotSpotCurrent.OnHoverEnd();
		}
		_oHotSpotCurrent = null;
		_eModeCursor = EModeCursor.S1_SearchingForHotspot;
	}
	
	public void SetCursorText(string sCursorText) {
		if (_oTextMesh.text != sCursorText) {
			_oTextMesh.text =  sCursorText;
			_oTextMesh.GetComponent<Renderer>().enabled = (sCursorText.Length > 0);
		}
	}

	public void SetCursorColor(Color oColor) {
		GetComponent<Renderer>().sharedMaterials[1].color = oColor;
	}
}

public enum EEditMode { 	// The types of possible cursor edit/select mode.
	Select, 
	Move, 
	Rotate, 
	Scale,
}		

public enum EModeCursor { 							// The cursor modes used in its Finite State Machine:
	S1_SearchingForHotspot,							// Default/startup mode: We are looking for a hotspot
	S2_HoveringOverHotspot,							// The user is hovering over a hotspot, but has not yet activated it with the left or right mouse buttons.  (3D cursor changed color to provide visual feedback to the user that an action is possible (hotspot may not be visibie))
	S3_WaitingForMouseUp,							// The user has down-clicked a hotspot.  We await up-click to see if same hotspot is still under the mouse before activating the hotspot
	S4_ActivatedHotspot,							// A hotspot has been activated and has invoked its 'payload action' (e.g. display gizmo, popup menu, etc)
};



//iGUICode_Root.INSTANCE._oPaneContextMenu = iGUIRoot.INSTANCE.addElement<iGUIPanel>();
//iGUICode_Root.INSTANCE._oPaneContextMenu.setType(iGUIPanelType.Box);
//iGUICode_Root.INSTANCE._oPaneContextMenu.layout = iGUILayout.VerticalDense;

//foreach (CObjGrp oObjGrp in _aPropGrps) {			//###DESIGN: Use property groups??
//	_oObj.CreateWidget(oPanel);
//	foreach (int nPropID in _oObj._aPropIDs) {
//		CObj oObj = Find(nPropID);
//		oObj.CreateWidget(oPanel, oObjGrp);
//	}
//}


		//iGUIPanel oPanel = iGUICode_Root.INSTANCE._oPaneContextMenu;

		//if (oPanel != null)
		//	ContextMenu_Hide();

		//oPanel.setEnabled(true);				//###OBS???  Now every CObj has its own popup window!!
		//oPanel.positionAndSize = new Rect(0.5f, 0.5f, G.C_Gui_WidgetsWidth, oObj._aChildren.Length * G.C_Gui_WidgetsHeight);		//###IMPROVE: Change to moveable window, default pos at side of screen

		////=== Create context menu label ===
		//iGUILabel oLabelTitle = oPanel.addElement<iGUILabel>();						//###IMPROVE: A bit of duplication creating these basic controls into panels... abstract into functions??
		//oLabelTitle.label.text = sContextMenuTitle;
		//oLabelTitle.label.tooltip = sContextMenuTooltip;
		//oLabelTitle.style.fontSize = 13;
		//oLabelTitle.positionAndSize.Set(0, 0, 1, G.C_Gui_WidgetsHeight);
		//oLabelTitle.style.alignment = TextAnchor.MiddleCenter;

		////=== Create context menu CObj widgets ===
		//foreach (CObj oObj in oObj._aChildren)
		//	oObj.CreateWidget(oPanel, null);

		////=== Create context menu close button ===
		//iGUIButton oButton = oPanel.addElement<iGUIButton>();					
		//oButton.label.text = "Close";
		//oButton.clickCallback += OnButtonClicked_ContextMenuClose;
		////oButton.positionAndSize.Set(0, -G.C_Gui_WidgetsHeight, 1, G.C_Gui_WidgetsHeight);			//###IMPROVE!!! Put button inside menu box!!
		//oButton.style.fixedHeight = G.C_Gui_WidgetsHeight;


	//public void ContextMenu_Hide() {								// Hide the app-global context menu
	//	if (iGUICode_Root.INSTANCE._oPaneContextMenu != null) {
	//		iGUICode_Root.INSTANCE._oPaneContextMenu.removeAll();
	//		iGUICode_Root.INSTANCE._oPaneContextMenu.setEnabled(false);
	//	}
	//}

	//void OnButtonClicked_ContextMenuClose(iGUIElement caller) {
	//	ContextMenu_Hide();
	//}
