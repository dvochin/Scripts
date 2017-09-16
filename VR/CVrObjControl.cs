/*###DOCS22: May 2017 - Wand object / camera control
=== DEV ===

=== NEXT ===
- Do something about the damn model we render for the wand!  Hands?  The wand model itself?  Design this!

=== TODO ===
- Remove the VRTK components we don't use
- Revive panels and its attach point?
- Check: tip and its axis ok??
- Play with 'VRTK_Touchpad' component to extract touchpad info

=== LATER ===
=== IMPROVE ===
- Need to sync cam and object control between wands?
	- Not sure we need too!  For camera dual-wand control makes it more stable and quicker!  For objects maybe?
- Would be nice to be able to catch the damn 'start menu' buttons on both wands (they normally go to Steam menu / Oculus menu)  We could trap and have an option in our menu to invoke them in a submenu?

=== DESIGN ===
- What classes will centrally control super-important wand buttons... this one??
- Keep button assignment on mode control?
- Allocate joystick to basic movements when user committed to an op? (turn left/right and move up/down)
	- Joystick used for its four directions during normal gameplay
- Need to create new canvas when user brings a wand close to headset: The four most important UI screens are there (triggered by trackpad 4 directions)
	- Canvas UI interaction working!  Just cursor not working... something different with our panel?
		- It creates a 'box collider' which looks good on static one but our dynamic one looks way off!  Trace into its creation!
		- Only affects cursor tho... study why acting works but paint on screen fails!
	- Touchpad touching / pressing works really well... but confusing the control between want and headset... dedicate touchpad to headset?  (Wand gets button a?)
	- Panel creating pretty well... now make them short-lived and interact with them!
		- would be nice to have a nice hand pointer like SteamVR instead of wands!
- Need to design VR panels that take the most space just behind bodies without being occluded by them
- Would be nice to pin panel to the collider that invoked that action but without occluding body.  (e.g for nose to right/left of face, for breasts on body side, etc)
	- If collider is too far then bring panel close to HMD?  Or... make panel bigger / smaller??
- Improving PhysX on bones
- Hip lower than Pelvis!  Can skip a bone?  How??
- Quickly done colliders: Need an additional bone for foot anchor (so we can use box collider)
- Incredible confusion between all the kinematic / dynamic modes / pinned or not pinned... SORT THIS OUT FIRST!!!!!!!... eg GUI at startup how can it work?  F4/F4???


=== IDEAS ===
- Oculus build of the game can use the 'Start Menu' botton on left wand for some debug thing!
- Drive the camera position through a very week PhysX joint and have it have a collider?  (Could push bodies out of the way?)

=== LEARNED ===

=== PROBLEMS ===
- Pin cube we use heavily has its +X pointing the other way!  

=== QUESTIONS ===

=== WISHLIST ===

*/

using UnityEngine;



public class CVrObjControl : MonoBehaviour {

	Transform			_oCameraRigT;					// The '[CameraRig]' transform.  Needed to subtract its position / rotation away from global wand tip / position
	Transform			_oHeadsetT;						// The headset transform.  Used for camera rotation
	Transform			_oVrWandObject;                 // Object being moved and rotated by this object controller.  Obtained from CGame instance wand grip button press down
	Transform			_oWandSteamT;					// The 'SteamVR' wand (with SteamVR_TrackedObject component).  Parent of the VRTK one (with VRTK_ControllerEvents component)  This transform has the coordinates wand
	Transform			_oWandTipT;						// Transform pointing to the 'tip' of the wand.   Needed for precision movement rotation about the part of the wand we care about
	//Transform			_oWandGripT;					//###TODO23: Transform pointing to the 'grip' of the wand.  Hack needed to obtain the current grip position as VRTK's grip axis is currently broken (so we extract out of model)	###PROBLEM: What happens if model is not shown??

	Vector3				_vecPosStart_Wand;				// Local position / rotation of wand within rig at start of object control
	Quaternion			_quatRotStart_Wand;
	Quaternion			_quatRotStartInv_Wand;			// Quaternion.Inverse() of _quatRotStart_Wand to speed up game loop

	Vector3				_vecPosStart_Object;			// Global position / rotation of wand within rig at start of object control
	Quaternion			_quatRotStart_Object;
	Quaternion			_quatRotStartInv_Object;        // Quaternion.Inverse() of _quatRotStart_Object to speed up game loop

	bool				_bLateInitialized;				// Flag for delayed initialization.

	EVrControlObject	_eVrControlObject	= EVrControlObject.None;	// Type of what we control: Camera, a 3D transform (such as a body pin) or none
	EVrControlMode		_eVrControlMode		= EVrControlMode.Absolute;	// For controlling object either absolute (user controls translation / rotation directly) or relative (delta added over time)
	CVrPanelWand		_oVrPanelWand;					// The (optional) wand GUI.  Capable of rendering most of the game's UI.  Shown my user bringing wand close to front of headset.
	VRTK.VRTK_ControllerEvents _oVrControlEvents;		// The VRTK object we use to get our information from
	uint				_nControllerIndex;				// Our 'controller index'.   Needed to access low-level functions like get grip axis value
	bool				_bIsLeft;
	public CProp		_oPropDebugJoystickHor_HACK;	// Debug property that is pushed in by various code-in-development for easy debug property editing by the up/down & right/left axis of this wand's joystick
	public CProp        _oPropDebugJoystickVer_HACK;
	CProp               _oPropEditedByJoystick_HACK;    // Debug property actively being edited by this wand's joystick.  Either _oPropDebugJoystickHor_HACK, _oPropDebugJoystickVer_HACK or null when no joystick editing

	public string       _sNameWand;                     // "Left" or "Right"
	EGameGuiMsg         _eGameGuiMsg;                   // VrWandLeft or VrWandRight

	const float C_JoystickPropEdit_SizeDeadzone = 0.15f;				// How wide the deadzone is during debug joystick property editing
	const float C_JoystickPropEdit_PropertyModifyStrength = 0.01f;      // How strong the joystick property editing is per frame as a percentage of its range

	//======================================================================	INIT
	void Start () {
		_oVrControlEvents = GetComponent<VRTK.VRTK_ControllerEvents>();
		_nControllerIndex = VRTK.VRTK_DeviceFinder.GetControllerIndex(gameObject);

        if (_oVrControlEvents == null)
            CUtility.ThrowExceptionF("###EXCEPTION: CVrObjControl() could not find VRTK_ControllerEvents component for wand '{0}'", gameObject.name);

		_bIsLeft = gameObject.name.Contains("Left");            // A bit of a weak way to know if this wand is left or right... There must be a better way
		if (_bIsLeft) {
			_sNameWand = "Left";
			_eGameGuiMsg = EGameGuiMsg.VrWandLeft;
		} else {
			_sNameWand = "Right";
			_eGameGuiMsg = EGameGuiMsg.VrWandRight;
		}

		//=== Setup controller event listeners to trap wand trigger pressed and released to move / rotate controlled object with six degrees of freedom! ===
		_oVrControlEvents.GripPressed			+= new VRTK.ControllerInteractionEventHandler(OnVrWandGrip_Pressed);		//###PROBLEM: VRTK broken around grip functionality. (Axis always returns 0 and hairline and touch events are never called!)
        _oVrControlEvents.TriggerHairlineStart	+= new VRTK.ControllerInteractionEventHandler(OnVrWandTrigger_Pressed);		//###INFO: We trap hairline on way down (about 25% button press) but we end when user leaves button altogether (so that we can use entire axis for 'strength')
        _oVrControlEvents.ButtonTwoPressed		+= new VRTK.ControllerInteractionEventHandler(OnVrWandButton2_Pressed);
		_oVrControlEvents.TouchpadAxisChanged   += new VRTK.ControllerInteractionEventHandler(OnVrWandTouchpadAxisChanged);
		//_oVrControlEvents.GripReleased		+= new VRTK.ControllerInteractionEventHandler(OnVrWandGrip_Released);
		//_oVrControlEvents.TriggerTouchEnd		+= new VRTK.ControllerInteractionEventHandler(OnVrWandTrigger_Released);
		//_oVrControlEvents.ButtonOnePressed	+= new VRTK.ControllerInteractionEventHandler(OnVrWandButton1_Pressed);
		//_oVrControlEvents.ButtonOneReleased	+= new VRTK.ControllerInteractionEventHandler(OnVrWandButton1_Released);
		//_oVrControlEvents.ButtonTwoReleased	+= new VRTK.ControllerInteractionEventHandler(OnVrWandButton2_Released);
		//_oVrControlEvents.StartMenuPressed	+= new VRTK.ControllerInteractionEventHandler(OnVrWandButtonPressedStartMenu);	//###IMPROVE: How to trap the damn 'third button' for ourselves!!

		//=== Create the wand GUI panel ===
		Transform oModelAttachParentT = transform;		//###IMPROVE: When on SteamVR we had a nice 3D model of wand with useful anchors we could use.  Oculus doesn't have that so we're stuck with our plain old transform node.  _oWandSteamT.Find("Model/handgrip/attach").transform;
		if (_bIsLeft == false) {			//###HACK:!!!!  Bad hack with left / right VR panels!
			_oVrPanelWand = new CVrPanelWand(this, oModelAttachParentT);
			_oVrPanelWand._oCanvas.gameObject.SetActive(false);			// Panel hidden until brought close to headset
		}
	}

	void LateInitialization() {					// Late initialization is needed because VRTK reparents itself to SteamVR wand somewhere in initialization
		if (_bLateInitialized == false) {
			Debug.LogFormat("=== Initialize VR Wand '{0}' ===", gameObject.name);
			_oWandSteamT = _oVrControlEvents.transform.parent;

			//=== Obtain references to the necessary VRTK and SteamVR game objects ===	//###IMPROVE: Add safety checks to make sure we have been re-parented by VRTK
			//if (_oWandSteamT.GetComponent<SteamVR_TrackedObject>() == null)			// Make sure we really have the steam wand VR object (VRTK object gets reparented to it and we might get called too early)
			//	CUtility.ThrowExceptionF("###EXCEPTION: CVrObjControl() could not find SteamVR_TrackedObject for wand '{0}'", gameObject.name);
			_oCameraRigT = _oWandSteamT.transform.parent;
			//if (_oCameraRigT.GetComponent<SteamVR_ControllerManager>() == null)     // Make sure we really have the steam wand VR object (VRTK object gets reparented to it and we might get called too early)
			//	CUtility.ThrowExceptionF("###EXCEPTION: CVrObjControl() could not find SteamVR_ControllerManager in camera rig for wand '{0}'", gameObject.name);
			_oHeadsetT = VRTK.VRTK_DeviceFinder.DeviceTransform(VRTK.VRTK_DeviceFinder.Devices.Headset);       //###INFO: How to globally get the objects we need!

			//=== Obtain references to the needed parts of the 3D wand model ===
			_oWandTipT = transform;// _oWandSteamT.Find("Model/tip/attach").transform;		// Obtain access to the transform in our model's nodes that reveals the position / orientation of the wand tip ===//###IMPROVE: Can obtain tip position / orientation in a better way through API??  Also would be nicer to have this in Start() but because of VRTK reparenting this is too early
			//###IMPROVE23: No longer have access to SteamVR anchors on 3D model!  _oWandGripT = _oWandSteamT.Find("Model/grip").transform;           // Obtain access to the transform in our model's nodes that reveals the orientation of the grip.  This is the easiest way we can extrac the grip position as VRTK is currently broken returning grip axis pos.

			// CGame.INSTANCE._aGuiMessages[(int)EGameGuiMsg.VrMode] = string.Format("VrMode: {0}", _eVrControlMode);      // Weak: duplication
			_bLateInitialized = true;
		}
	}


	//======================================================================	UTIL
	Vector3 Util_GetWandLocalPosition() {						// Returns the wand position local to its owning camera rig.  Used to control camera
		return _oWandSteamT.localPosition;
	}
	Quaternion Util_GetWandLocalRotation() {					// Returns the local wand rotation.  Used to control camera
		return _oWandSteamT.localRotation;
	}
	Vector3 Util_GetWandTipLocalPosition() {					// Returns the position of the wand tip as local to the owning camera rig.  Used for controlling objects
		return _oWandTipT.position - _oCameraRigT.position;		
	}
	Quaternion Util_GetWandGlobalRotation() {					// Returns the global rotation of the wand tip.  Used for controlling objects
		return _oWandTipT.rotation;								
	}

	Quaternion Util_ScaleQuaternion(Quaternion quatIn, float nRatio) {	// Reduce the rotation 'power' of the delta quaternion -> convert to angle-axis, scale and convert back to quaternion.
		Vector3 vecAxis;
		float nAngle;
		quatIn.ToAngleAxis(out nAngle, out vecAxis);		//###IMPROVE: Can be done directly on quaternions?  (How to guarantee a valid quaternion?)
		nAngle *= nRatio;
		return Quaternion.AngleAxis(nAngle, vecAxis);
	}


	//======================================================================	BUTTONS
	//private void OnVrWandButton1_Pressed (object sender, VRTK.ControllerInteractionEventArgs e)	{ }		//###DESIGN22: Here??
	//private void OnVrWandButton1_Released(object sender, VRTK.ControllerInteractionEventArgs e)	{ }

	private void OnVrWandButton2_Pressed(object sender, VRTK.ControllerInteractionEventArgs e) {
		LateInitialization();
		_eVrControlMode += 1;
		if (_eVrControlMode == EVrControlMode.COUNT)
			_eVrControlMode = EVrControlMode.Absolute;
		// CGame.INSTANCE._aGuiMessages[(int)EGameGuiMsg.VrMode] = string.Format("VrMode: {0}",  _eVrControlMode);
	}
	//private void OnVrWandButton2_Released(object sender, VRTK.ControllerInteractionEventArgs e) { }
	private void OnVrWandTouchpadAxisChanged(object sender, VRTK.ControllerInteractionEventArgs e) {    //###IMPROVE: Only changes properties when joystick moves?  Change to constant change until return within deadzone?
		float nX_abs = Mathf.Abs(e.touchpadAxis.x);					//###IMPROVE: Add some screen visualization to show user how property is being changed
		float nY_abs = Mathf.Abs(e.touchpadAxis.y);
		float nValMax_abs = Mathf.Max(nX_abs, nY_abs);

		if (_oPropEditedByJoystick_HACK == null) {										// Start joystick debug property editing if not currently editing anything and joystick out of deadzone
			if (nValMax_abs > C_JoystickPropEdit_SizeDeadzone) {						// Ignore joystick movements within our 'deadzone'
				if (nX_abs > nY_abs)                                                    // X movement larger = start horizontal edit
					_oPropEditedByJoystick_HACK = _oPropDebugJoystickHor_HACK;          // Enables update to update this property
				else																	// Y movement larger = start vertical edit
					_oPropEditedByJoystick_HACK = _oPropDebugJoystickVer_HACK;          // Enables update to update this property
			}
		} else {																		// End joystick debug property editing if editing something and joystick entered deadzone
			if (nValMax_abs <= C_JoystickPropEdit_SizeDeadzone) {
				_oPropEditedByJoystick_HACK = null;         // Stops update from editing any property
				CGame.INSTANCE._aGuiMessages[(int)_eGameGuiMsg] = string.Format("{0} Wand:", _sNameWand);
			}
		}
	}

		

	//======================================================================	OBJECT CONTROL

	private void OnVrWandTrigger_Pressed(object sender, VRTK.ControllerInteractionEventArgs e) {
		if (_eVrControlObject != EVrControlObject.None)		// Only start an operation if we're at idle state (old op has to complete)
			return;

		//=== User pressed down on grip.  Obtain reference to the transform we're set to move / rotate ===
		LateInitialization();
		if (_bIsLeft)
			_oVrWandObject = CGame.INSTANCE._oVrWandObjectL;
		else
			_oVrWandObject = CGame.INSTANCE._oVrWandObjectR;

        if (_oVrWandObject == null)
            CUtility.ThrowException("###EXCEPTION: CVrObjControl() could not obtain a valid object to move / rotate!");

		//=== Store *local* position / rotation of our wand relative to the camera rig.  We will apply 'delta' position / rotations from this starting point at each frame in Update() ===
		_vecPosStart_Wand		= Util_GetWandTipLocalPosition();		// We observe the *local* position of our parent.  (that is what SteamVR pushes in.  We don't use the global position as we move its parent.parent!!)
		_quatRotStart_Wand		= Util_GetWandGlobalRotation();
		_quatRotStartInv_Wand	= Quaternion.Inverse(_quatRotStart_Wand);

		//=== Store startup position / rotation of object to control ===
		_vecPosStart_Object		= _oVrWandObject.position;
		_quatRotStart_Object	= _oVrWandObject.rotation;
		_quatRotStartInv_Object = Quaternion.Inverse(_quatRotStart_Object);

		//=== Enable Update() to move / rotate the object below ===
		// CGame.INSTANCE._aGuiMessages[(int)EGameGuiMsg.VrControl1] = string.Format("VR1:  Obj='{0}'  WPos={1}  WRot={2}  Ev={3}", _oVrWandObject.name, _vecPosStart_Wand, _quatRotStart_Wand.eulerAngles, e.ToString());
		// CGame.INSTANCE._aGuiMessages[(int)EGameGuiMsg.VrControl2] = string.Format("VR2:  OPos={0}  ORot={1}", _vecPosStart_Object, _quatRotStart_Object.eulerAngles);

		//=== Enable Update() to control the current object ===
		_eVrControlObject = EVrControlObject.Object;	// Enable Update() to control current object
    }
    //private void OnVrWandTrigger_Released(object sender, VRTK.ControllerInteractionEventArgs e) {}


	//======================================================================	CAMERA CONTROL
	private void OnVrWandGrip_Pressed(object sender, VRTK.ControllerInteractionEventArgs e) {
		if (_eVrControlObject != EVrControlObject.None)		// Only start an operation if we're at idle state (old op has to complete)
			return;

		//=== Store *local* position / rotation of our wand relative to the camera rig.  We will apply 'delta' position / rotations from this starting point at each frame in Update() ===
		LateInitialization();
		_vecPosStart_Wand		= Util_GetWandLocalPosition();		// We observe the *local* position of our parent.  (that is what SteamVR pushes in.  We don't use the global position as we move its parent.parent!!)
		_quatRotStart_Wand		= Util_GetWandLocalRotation();
		_quatRotStartInv_Wand	= Quaternion.Inverse(_quatRotStart_Wand);

		_eVrControlObject = EVrControlObject.Camera;	// Enable Update() to control camera
	}
	//private void OnVrWandGrip_Released(object sender, VRTK.ControllerInteractionEventArgs e) { }

	private void OnTriggerEnter(Collider other) {
		if (other.gameObject.name == "Trigger-Panel-Show") {				//###WEAK: Test of triggers by their node name.  (Them having a custom object would be more robust)
			if (_oVrPanelWand != null && _oVrPanelWand._oCanvas != null)
				_oVrPanelWand._oCanvas.gameObject.SetActive(true);
			//CGame.INSTANCE._aGuiMessages[(int)EGameGuiMsg.VrControlCam] = string.Format("VrCol: {0}", other.gameObject.name);
		}
	}
	private void OnTriggerExit(Collider other) {
		if (other.gameObject.name == "Trigger-Panel-Hide") {				//###WEAK: Test of triggers by their node name.  (Them having a custom object would be more robust)
			if (_oVrPanelWand != null && _oVrPanelWand._oCanvas != null)
				_oVrPanelWand._oCanvas.gameObject.SetActive(false);
			//CGame.INSTANCE._aGuiMessages[(int)EGameGuiMsg.VrControlCam] = string.Format("VrCol: none");		
		}
	}


	//======================================================================	UPDATE
	private void Update() {
		switch (_eVrControlObject) {
			case EVrControlObject.Camera:
				float nButtonStrengthGrip = VRTK.VRTK_SDK_Bridge.GetGripAxisOnIndex(_nControllerIndex).x;		//###INFO: How to get grip axis info!  VRTK BROKEN!!!: GetGripAxis() returns zero at source level so have to get it from 3D model!!!  VRTK fundamentally broken at source level... but openvr gets value as it populates wand model properly! (Call             if (!renderModels.GetComponentState(renderModelName, name, ref controllerState, ref controllerModeState, ref componentState))  ###CHECK: Will this work on SteamVR?

				if (nButtonStrengthGrip > 0.05f) {              //###INFO: GetTriggerAxis() can still return very small values when user has finger completely off!

					//=== Obtain camera rig position / rotation (so we can modify more easily) ===
					Vector3		vecCameraRigPos		= _oCameraRigT.position;
					Quaternion	quatCameraRigRot	= _oCameraRigT.rotation;

					//=== Obtain the fresh position and rotation of the wand tip relative to the camera rig ===
					Vector3		vecPosNow_Wand	= Util_GetWandLocalPosition();
					Quaternion	quatRotNow_Wand	= Util_GetWandLocalRotation();

					//=== Obtain the delta position / rotation of the wand tip since the start of this control operation ===
					Vector3		vecPosNowDelta_Wand		= vecPosNow_Wand - _vecPosStart_Wand;
					Quaternion	quatRotNowDelta_Wand	= quatRotNow_Wand * _quatRotStartInv_Wand;     //###INFO: How to determine quaternion difference -> With Quaternion.Inverse()!

					//=== Rotate the delta position about the camera position.  (We need to keep camera rig rotation out of Util_GetWandLocalPosition() so time is not a factor) ===
					vecPosNowDelta_Wand = _oCameraRigT.rotation * vecPosNowDelta_Wand;		//###INFO  vecOut = Quaternion.Inverse(quatRot) * vecIn; // same as vecIn * quatRot

					Quaternion quatRotNowDelta_Wand_ScaledDown = Util_ScaleQuaternion(quatRotNowDelta_Wand, CGame.INSTANCE._nVrObjectControlRot * nButtonStrengthGrip);
					quatRotNowDelta_Wand_ScaledDown.x = quatRotNowDelta_Wand_ScaledDown.z = 0;
			
					//--- Apply the scaled-down delta position and rotation ---
					vecCameraRigPos += vecPosNowDelta_Wand * CGame.INSTANCE._nVrObjectControlPos * nButtonStrengthGrip;	// Apply a scaled-down delta vector
					quatCameraRigRot = quatRotNowDelta_Wand_ScaledDown * quatCameraRigRot;			//###INFO: How to apply an angle... Not commutative!!

					//=== Make sure we are not trying to go too far up or down ===
					//###BROKEN:!!! vecCameraRigPos.y = Mathf.Clamp(vecCameraRigPos.y, -0.8f, 1.0f);        //###TUNE		###IMPROVE: Camera rig CAN go underneath y=0 but camera itself should not!  Clamp camera pos instead of rig!

					//=== Adjust the camera rig position / rotation ===
					_oCameraRigT.position = vecCameraRigPos;
					_oCameraRigT.rotation = quatCameraRigRot;

				} else {                                            // User no longer pressing down grip.  End the camera move operation.

					_eVrControlObject = EVrControlObject.None;				// End the control camera op
					// CGame.INSTANCE._aGuiMessages[(int)EGameGuiMsg.VrControlCam] = "";	//###IMPROVE: enabled = false too?

				}
				break;

			case EVrControlObject.Object:
				float nButtonStrengthTrigger	= _oVrControlEvents.GetTriggerAxis();

				if (nButtonStrengthTrigger > 0.05) {		//###INFO: GetTriggerAxis() can still return very small values when user has finger completely off!
					
					//=== Obtain the fresh position and rotation of the wand tip relative to the camera rig ===
					Vector3		vecPosNow_Wand	= Util_GetWandTipLocalPosition();
					Quaternion	quatRotNow_Wand	= Util_GetWandGlobalRotation();

					//=== Obtain the delta position / rotation of the wand tip since the start of this control operation ===
					Vector3		vecPosNowDelta_Wand		= vecPosNow_Wand - _vecPosStart_Wand;
					Quaternion	quatRotNowDelta_Wand	= quatRotNow_Wand * _quatRotStartInv_Wand ;		//###INFO: How to determine quaternion difference -> With Quaternion.Inverse()!

					//=== Move / rotate the object being controlled either relatively or absolutely ===
					if (_eVrControlMode == EVrControlMode.Relative) {
						//--- Reduce the influence of the delta quaternion -> convert to angle-axis, scale and convert back to quaternion ---  ###IMPROVE: Can be done directly on quaternions?  (How to guarantee a valid quaternion?)
						Quaternion quatRotNowDelta_Wand_ScaledDown = Util_ScaleQuaternion(quatRotNowDelta_Wand, CGame.INSTANCE._nVrObjectControlRot * nButtonStrengthTrigger);
						//--- Apply the scaled-down delta position and rotation ---
						_oVrWandObject.position += vecPosNowDelta_Wand * CGame.INSTANCE._nVrObjectControlPos * nButtonStrengthTrigger;	// Apply a scaled-down delta vector
						_oVrWandObject.rotation = quatRotNowDelta_Wand_ScaledDown * _oVrWandObject.rotation;			//###INFO: How to apply an angle... Not commutative!!
					} else {
						_oVrWandObject.position = _vecPosStart_Object	+ vecPosNowDelta_Wand;
						_oVrWandObject.rotation = quatRotNowDelta_Wand	* _quatRotStart_Object;
					}

				} else {		//=== User released grip.  End movement / rotation ===

					_eVrControlObject = EVrControlObject.None;				// End the control object op
					_oVrWandObject = null;
					// CGame.INSTANCE._aGuiMessages[(int)EGameGuiMsg.VrControl1] = "";
					// CGame.INSTANCE._aGuiMessages[(int)EGameGuiMsg.VrControl2] = "";
				}
				break;

			default:
				break;
		}

		//=== Perform joystick editing of assigned debug properties if set ===
		if (_oPropEditedByJoystick_HACK != null) {
			Vector2 vecTouchpadAxis = _oVrControlEvents.GetTouchpadAxis();
			float nValJoystickAxis = (_oPropEditedByJoystick_HACK == _oPropDebugJoystickHor_HACK) ? vecTouchpadAxis.x : vecTouchpadAxis.y;
			float nValJoystickAxis_Abs = Mathf.Abs(nValJoystickAxis);
			float nJoystickRatioLessDeadzone = (nValJoystickAxis_Abs - C_JoystickPropEdit_SizeDeadzone) / (1 - C_JoystickPropEdit_SizeDeadzone);
			if (nValJoystickAxis < 0)
				nJoystickRatioLessDeadzone *= -1;
			float nValue = _oPropEditedByJoystick_HACK._nValueLocal;
			nValue += nJoystickRatioLessDeadzone * _oPropEditedByJoystick_HACK._nMinMaxRange * C_JoystickPropEdit_PropertyModifyStrength;
			_oPropEditedByJoystick_HACK.PropSet(nValue);
			CGame.INSTANCE._aGuiMessages[(int)_eGameGuiMsg] = string.Format("{0} Wand: {1} = {2}", _sNameWand, _oPropEditedByJoystick_HACK._sNameProp, _oPropEditedByJoystick_HACK.PropGet());
		}
	}
}

public enum EVrControlMode {
	Absolute,
	Relative,
	COUNT,
}

public enum EVrControlObject {
	None,			// Nothing is currently being moved / rotated by this wand.
	Object,			// An object is currently being moved / rotated (e.g. body pin)
	Camera,			// The camera is currently being moved / rotated
}












//###OBS: Implementation featuring always rotating camera Y rot and up/down = Nauseating so went back to wand controlling camera!
//	private void Update() {
//		//=== Obtain camera rig position / rotation (so we can modify more easily) ===
//		Vector3		vecCameraRigPos		= _oCameraRigT.position;
//		Quaternion	quatCameraRigRot	= _oCameraRigT.rotation;

//		//=== Obtain the current headset position / rotation ===
//		Vector3		vecHeadsetPos	= Util_GetLocalHeadsetPosition();
//		Quaternion	quatHeadsetRot	= _oHeadsetT.localRotation;

//		//=== Calculate the 'delta' position / rotation since start of camera operation ===
//		Vector3		vecHeadsetPosDelta	= vecHeadsetPos  - _vecHeadsetPos_Start;
//		Quaternion	quatHeadsetRotDelta	= quatHeadsetRot;// * Quaternion.Inverse(_quatHeadsetRot_Start);

//		//=== Obtain how far the buttons have been pressed.  Determines the 'strenght' of our action ===
//		float nButtonStrengthGrip		= _oVrControlEvents.GetGripAxis();
//		float nButtonStrengthTrigger	= _oVrControlEvents.GetTriggerAxis();		// Obtain how strongly the buttons have been pressed = influences 'strength' of operation

//		//=== Print debug information about the raw delta inputs we have to work with ===
//		Vector3 eulHeadsetRotDelta = quatHeadsetRotDelta.eulerAngles;
//		// CGame.INSTANCE._aGuiMessages[(int)EGameGuiMsg.VrControlCam] = string.Format("VrCam:  PD:{0:F2},{1:F2},{2:F2}   OD:{3:F0},{4:F0},{5:F0}", vecHeadsetPosDelta.x, vecHeadsetPosDelta.y, vecHeadsetPosDelta.z,  eulHeadsetRotDelta.x, eulHeadsetRotDelta.y, eulHeadsetRotDelta.z);

//		//=== Scale down translation and rotation.  We need to move / rotate the camera a fraction of the delta headset position / rotation ===
//		Quaternion quatHeadsetRotDelta_ScaledDown	= Util_ScaleQuaternion(quatHeadsetRotDelta, CGame.INSTANCE._nVrObjectControlRot * 0.1f);	//###TUNE
//		Vector3 vecHeadsetPosDelta_ScaledDown		= vecHeadsetPosDelta * CGame.INSTANCE._nVrObjectControlPos * nButtonStrengthGrip;
//		vecHeadsetPosDelta_ScaledDown.y = 0;		// Can't ask player to shrink / stretch his/her neck to go up and down so we throw that value right away.

//		//=== Change the camera rig height from the angle the user is lookup up / down ===
//		vecCameraRigPos.y += -quatHeadsetRotDelta_ScaledDown.x / 2;		//###TUNE
//		vecCameraRigPos.y = Mathf.Clamp(vecCameraRigPos.y, -0.8f, 1.0f);			// Make sure we are not trying to go too far up or down  //###TUNE		###IMPROVE: Camera rig CAN go underneath y=0 but camera itself should not!  Clamp camera pos instead of rig!

//		//=== We only control the camera's left / right rotation ===
//		quatHeadsetRotDelta_ScaledDown.x = 0;		// Camera up / down is not touched (the user can look up and down fine by themselves)  (Camera up/down rotate has been fed into camera position up/down above)
//		quatHeadsetRotDelta_ScaledDown.z = 0;       // Camera roll is taboo in VR!  (causes nausea!)  We throw this input away.
//		quatCameraRigRot	= quatHeadsetRotDelta_ScaledDown * quatCameraRigRot;


//		if (_bControllingCameraMovement) {

//			//=== Make sure we don't go too far away from scene center ===
//			if (vecCameraRigPos.magnitude > 4)				// Change with center of where characters are!
//				vecHeadsetPosDelta_ScaledDown = Vector3.zero;

//			//=== Apply the calculated deltas to the temp camera rig variables.  Note that up/down is taken care above.  Here is just camera x,z
//			vecCameraRigPos		+= vecHeadsetPosDelta_ScaledDown;

//		} else if (_bControllingObject) { 

//			//=== Obtain the fresh position and rotation of the wand tip relative to the camera rig ===
//			Vector3		vecPosNow_Wand	= Util_GetLocalWandPosition();
//			Quaternion	quatRotNow_Wand	= Util_GetLocalWandRotation();

//			//=== Obtain the delta position / rotation of the wand tip since the start of this control operation ===
//			Vector3		vecPosNowDelta_Wand		= vecPosNow_Wand - _vecPosStart_Wand;
//			Quaternion	quatRotNowDelta_Wand	= quatRotNow_Wand * _quatRotStartInv_Wand ;		//###INFO: How to determine quaternion difference -> With Quaternion.Inverse()!

//			//=== Move / rotate the object being controlled either relatively or absolutely ===
//			if (_eVrControlMode == EVrControlMode.Relative) {
//				//--- Reduce the influence of the delta quaternion -> convert to angle-axis, scale and convert back to quaternion ---  ###IMPROVE: Can be done directly on quaternions?  (How to guarantee a valid quaternion?)
//				Quaternion quatRotNowDelta_Wand_ScaledDown = Util_ScaleQuaternion(quatRotNowDelta_Wand, CGame.INSTANCE._nVrObjectControlRot * nButtonStrengthTrigger);
//				//--- Apply the scaled-down delta position and rotation ---
//				_oVrWandObject.position += vecPosNowDelta_Wand * CGame.INSTANCE._nVrObjectControlPos * nButtonStrengthTrigger;	// Apply a scaled-down delta vector
//				_oVrWandObject.rotation = quatRotNowDelta_Wand_ScaledDown * _oVrWandObject.rotation;			//###INFO: How to apply an angle... Not commutative!!
//			} else {
//				_oVrWandObject.position = _vecPosStart_Object	+ vecPosNowDelta_Wand;
//				_oVrWandObject.rotation = quatRotNowDelta_Wand	* _quatRotStart_Object;
//			}
//		}

//		//=== Adjust the camera rig position / rotation ===
//		_oCameraRigT.position = vecCameraRigPos;
//		_oCameraRigT.rotation = quatCameraRigRot;
//	}
//}




//GameObject.Find("(DEV)/TEST_VrControl/Wand_Pos").transform.localPosition  = vecPosNowDelta_Wand;
//GameObject.Find("(DEV)/TEST_VrControl/Wand_Rot").transform.localRotation  = quatRotNow_Wand;
//GameObject.Find("(DEV)/TEST_VrControl/Wand_RotD").transform.localRotation = quatRotNowDelta_Wand;
		
// CGame.INSTANCE._aGuiMessages[(int)EGameGuiMsg.VrControl3] = string.Format("VR3:  Pos {0}  Rot {1}  nAngleDiff={2}", transform.parent.localPosition, quatRotNow, quatDiff);


//private void OnVrWandButtonPressedStartMenu(object sender, VRTK.ControllerInteractionEventArgs e) {		//###IMPROVE: What can we do to get these events??
// CGame.INSTANCE._aGuiMessages[(int)EGameGuiMsg.VrControl1] = string.Format("Button Start!");
//}


////=== Apply a small pin cube to tip of wand to visualize its position / orientation ===
//GameObject oVisWandTipGO = GameObject.Find("(DEV)/TEST_VrControl/Wand_Tip");
//if (oVisWandTipGO != null) { 
//	Transform oWandTipT = transform.parent.FindChild("Model/tip/attach").transform;		// Weak duplication
//	Transform oVisWandTipT = oVisWandTipGO.transform;
//	oVisWandTipT.SetParent(oWandTipT);
//	oVisWandTipT.localPosition = Vector3.zero;
//	oVisWandTipT.localRotation = Quaternion.identity;
//	oVisWandTipT.localScale = new Vector3(5,5,5);
//}


//float nAngleGripModel = 0;:  _oWandGripT.localRotation.eulerAngles.y;
//if (nAngleGripModel > 180)						// One of the wand will return this as small angle the other way (e.g. 350 to 360 degrees).  Convert
//	nAngleGripModel = 360 - nAngleGripModel;
//float nButtonStrengthGrip = nAngleGripModel / 10.0f;		//###HACK:!? Somewhat of a hack to get to grip position straight from the 3D model position (VRTK Grip axis returns always zero at source level!)  Value returns from 0 to 10 degrees
// CGame.INSTANCE._aGuiMessages[(int)EGameGuiMsg.VrControlCam] = string.Format("VrCam:  STR={0:F0}%", nButtonStrengthGrip*100.0f);


//bool				_bButtonStrenghtWasNonZero;		// Helper bool to implmenent histeresis.  (Update() takes a while to register non-zero button press values)
//_bButtonStrenghtWasNonZero = true;			// Tell histeresis boolean that we've seen a non-zero value and that a zero value can terminate the op.
//if (_bButtonStrenghtWasNonZero) {				// Only end the op if a non-zero button value was seen at least once!
//_bButtonStrenghtWasNonZero = false;     // Reset the flag checking for non-zero button values.  Makes sure we only terminate op when 'GetButtonAxis()' returns a non-zero value and then zero (implements histeresis)


