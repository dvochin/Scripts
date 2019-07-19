#define __VIVE__        //#DEV26:!!!!!!!!!  WTF is going on with defines????????

#region ========================================================================== DOCS
/*###DOCS2: Dec 2017 - Wand object / camera control
=== NEXT ===
- Redesign of property editor window
    - Implement full selection mechanism
    - Remove game setting from that as they are always accessible.
        - Or... same property editor and selection mechanism except game settings never change selection?  (Would save implementation)
    - Property editor functionality still very basic
        - Create complete 'wrapper class' in Unity that handles selection, populating, prop set, etc
        - Once it works add another pane for game properties
    - Both Web and Unity code go through the same Unity property editor calls

- Completely redo the whole dev mode loading the body with its critical settings 
    - Should be done at init of CBodyBase anyways!
    - Idea: Give it its own tab!!

- Fix pose loading so we can start with interesting scene
    - Joint damping greatly stabilizes pose load!  (So does joint stiffening!)
        - When we set can we reset the joint velocity??
    - To simplify we will automatically back up character about 30 cm and flip 2nd 180 degs
    - Fingers should be kinematic to avoid explosion.
    - Pose loading needs to save BOTH joint angle and bone angle!  Why are the two not aligned???
    - Pose loading would probably blend more easily if we temporarily adjust parameters like damping
    - Need to save the pin status as well!
    - Fingers don't need to be saved?
    - Idea! Blend the angles old-to-new over a second or two for smoother transition?
    - Pose save / load 'bakes' the angles... We had a bake routine... lets try it again!
- Previously we saved the dual pose (which pose for each + pose root positioning)
    - Of value for this first deliverable??
- Need to study game mode and interaction between browser codebase and Unity...
    - The crap with the Dev mode PhysX setting needs to go away (perhaps in 'settings' mode?)
- Idea for disapearing toolstrip... have it as a template like CEditor!!

=== RENEWAL ===
- Revive code for body part selection and action...
    -
- Need browser codebase to act on any body.  (e.g. full fledge 'selection / edit' mechanism)
- WTF wand don't work on first body???
- Need init-time wand to connect default to torso of proper body
- Web cursor shows on top of body colliders!!

- Need to study web architecture to clean it up!
- Auto-load interesting bodies
- WTF wrong with fucking Vive mic & speech???

- Game mode a mess!  Study and fix!
- Fix pose load.
- Wand codebase simplification
    - Whole 'select' and 'action' dual range...  Is it a go on Vive or not??
    - Why touchpad down not working all the time on right wand?

- Need to properly populate the PhysX parameters throughout... how to do it given our property editor??
    - What is the deal with the finger processing??

- Start body pos fix!
- Need to project from about 45 degree angle to vive wand (more comfortable on wrists)
    - Re-implement the selection of body area (1:1 mapping with speech commands)
- Auto-assign wands to move torso

--- PROBLEMS ---
- Moving rig reparenting to camera a pain... static is best (user won't be able to move cam + object??
    - Could compute wand delta movement by diff of camera-rig to it (instead of relation to camera)
- Web: eToolstripGrid is defined multiple times with same name and that won't fly... could give different names?
    - Is making a 'bottom toolbar' the same for all editors of any value??
    - We'll need to debug this... show console to see how to unhide properly?
        - Reparent??  Create copies??
        - Fire up IntelliJ or Google debugger!!!


=== DEV ===

=== NEXT ===
- Do something about the damn model we render for the wand!  Hands?  The wand model itself?  Design this!

=== TODO ===
- Remove the unreliable VRTK input access and switch to our own branching code between Oculus / Vive instead of unreliable VRTK!
- Dynamically determine which is the 'left body' and which is the right
- Dynamically determine which is the 'left arm'  and which is the left

=== LATER ===
- Direct mode completely disabled... of any value?  Put back in or ditch?

=== IMPROVE ===
- Headset gaze light color? Distance from skin?
- Need to sync cam and object control between wands?
   - Not sure we need too!  For camera dual-wand control makes it more stable and quicker!  For objects maybe?
- Would be nice to be able to catch the damn 'start menu' buttons on both wands (they normally go to Steam menu / Oculus menu)  We could trap and have an option in our menu to invoke them in a submenu?
- Cursor will briefly flicker in / out when ending ops... time it?
- Wand pointers: Spotlight instead??  Point light??  One for each wand for sure!!  (Needs to belong to headset or CGame to manage collisions between the two wands!)
	- Marker object important.  Turn into its own class to store the full info & 'mode' of what wand is doing
		- Object being manipulated.
		- Safe mode in / out that blocks other user commands until done.
		- Code clarity to move code away from wand
		- Light / spot management, etc
=== DESIGN ===
- What classes will centrally control super-important wand buttons... this one??
- Keep button assignment on mode control?
- Allocate joystick to basic movements when user committed to an op? (turn left/right and move up/down)
   - Joystick used for its four directions during normal gameplay
- Need to design VR panels that take the most space just behind bodies without being occluded by them
- Would be nice to pin panel to the collider that invoked that action but without occluding body.  (e.g for nose to right/left of face, for breasts on body side, etc)
   - If collider is too far then bring panel close to HMD?  Or... make panel bigger / smaller??
- Quickly done colliders: Need an additional bone for foot anchor (so we can use box collider)

=== IDEAS ===
- Oculus raw input calls can reveal extra info such as 'thumb rest', Start, Reserved, etc.  See https://docs.unity3d.com/Documentation/Manual/OculusControllers.html
- Oculus build of the game can use the 'Start Menu' botton on left wand for some debug thing!
- Drive the camera position through a very week PhysX joint and have it have a collider?  (Could push bodies out of the way?)

=== LEARNED ===

=== PROBLEMS ===
?- Marker cube we use heavily has its +X pointing the other way!  

=== ABANDONED? ===
- Need to create new canvas when user brings a wand close to headset: The four most important UI screens are there (triggered by trackpad 4 directions)
   - Canvas UI interaction working!  Just cursor not working... something different with our panel?
       - It creates a 'box collider' which looks good on static one but our dynamic one looks way off!  Trace into its creation!
       - Only affects cursor tho... study why acting works but paint on screen fails!
   - Touchpad touching / pressing works really well... but confusing the control between want and headset... dedicate touchpad to headset?  (Wand gets button a?)
   - Panel creating pretty well... now make them short-lived and interact with them!
       - would be nice to have a nice hand pointer like SteamVR instead of wands!

=== QUESTIONS ===

=== WISHLIST ===

*/
using UnityEngine;
#endregion

public class CVrWand : MonoBehaviour {
#region ====================================================================== MEMBERS
    Transform           _oVrWandParent;                 // The parent to the game object owning this wand controller.  We need it as it's the one that moves / rotates in relation to the VR rig
    Transform			_oVrWandObject;                 // Object being moved and rotated by this wand controller.  Obtained from CGame instance wand grip button press down
	Transform			_oWandTipT;						// Transform pointing to the 'tip' of the wand.   Needed for precision movement rotation about the part of the wand we care about
    Transform           _oObjectBeingMovedT;            // The object currently being moved (when moving)

    Transform           _oRemoteMove_PivotT;            // Reference to our predefined remote move pivot transform.  Essential for proper remote move operations in VR!
    Transform           _oRemoteMove_PivotedT;          // Reference to our predefined remote move pivoted transform.
    Transform           _oRemoteMove_ObjectT;           // Reference to our predefined remote move object transform.  This one is a COPY of the real object being moved so we don't have to re-parent anything.  (The real object gets its position / orientation from this one during moves)

	Vector3				_vecPosStart_Wand;				// Local position / rotation of wand within rig at start of object control
	Quaternion			_quatRotStart_Wand;
	Quaternion			_quatRotStartInv_Wand;          // Quaternion.Inverse() of _quatRotStart_Wand to speed up game loop

	EVrOp               _eVrOp = EVrOp.None;	        // Type of what we control: Camera, a 3D transform (such as a body pin) or none
    EJoyDir             _eJoyDir, _eJoyDir_COMPARE;             // The state of this Vr Wand's joystick: Up, Down, Left, Right, Pressed, ThumbRest.  Sets our top-level operation mode.
    EJoyStrength        _eJoyStrength,  _eJoyStrength_COMPARE;  // How far off center the joystick is (idle, select mode zone, action 1 commit zone)
    //EGripStrength       _eGripStrength, _eGripStrength_COMPARE; // How deep the grip button is pressed down (idle, move/rotate camera zone, action 2 commit zone)

    IVrWandMoveable     _oVrWandMoveable;
    //CVrPanelWand        _oVrPanelWand;					// The (optional) wand GUI.  Capable of rendering most of the game's UI.  Shown my user bringing wand close to front of headset.
	public VRTK.VRTK_ControllerEvents _oVrControlEvents;    // The VRTK object we use to get our information from   //#DEV26

    public CObj		    _oObjDebugJoystickHor_HACK;	    // Debug property that is pushed in by various code-in-development for easy debug property editing by the up/down & right/left axis of this wand's joystick
	public CObj         _oObjDebugJoystickVer_HACK;
	public CObj         _oObjDebugJoystickPress_HACK;

    public string       _sNameWand;                     // "Left" or "Right"
    public char         _chNameWand;                    // 'L' or 'R'
    public int          _nOrdinal;                      // Left=0, Right=1
    public int          _nBodyAssignment;               // What body this wand controls.  Set by speech command 'Lock [Left | Right] Controller to [Left | Right] Body'.  Set to auto by 'Unlock [Left | Right] Controller'
	bool				_bIsLeft;
	uint				_nControllerIndex;				// Our 'controller index'.   Needed to access low-level functions like get grip axis value

    float               _nInputTrigger;
    float               _nInputGrip;
    bool                _bTrigger_Action_COMPARE;
    bool                _bGrip_Action_COMPARE;

    Vector3             _vecLocalToHeadset_WandPos_Start;

    const float         C_CutoffOnButtonAxis = 0.05f;   // Grip and trigger wand buttons must be over this value to be considered 'pressed'.  Anything under = unpressed
    const float         C_CutoffAxis_MinForSelect = C_CutoffOnButtonAxis;       //###IMPROVE: Vive needs 0.06!! Cutoff on joystick value to select the current mode ###NOTE: Vive trigger axis frequently leaves value between 1-4 when user unpresses button!!
    const float         C_CutoffAxis_MinForAction = C_CutoffAxis_MinForSelect;       //#DEV27: Get rid of select / action?? Cutoff on joystick value to select the current mode and perform the 'action 1'
    //###OBS: Dual meaning for joystick axis??  (Can we make it work on all types of VR wands?)
    const bool          C_ShowDebugMarkers = true;      // Show the debug markers to assist in development

    #endregion

#region ======================================================================	INIT
    void Awake() {
        enabled = false;        // Disabled until DoStart() is called (when a real VR Wand is available)
    }

    public void DoStart() {
		_oVrControlEvents = GetComponent<VRTK.VRTK_ControllerEvents>();
		_nControllerIndex = VRTK.VRTK_DeviceFinder.GetControllerIndex(gameObject);

        if (_oVrControlEvents == null)
            CUtility.ThrowExceptionF("###EXCEPTION: CVrWand() could not find VRTK_ControllerEvents component for wand '{0}'", gameObject.name);

		_bIsLeft = gameObject.name.Contains("Left");            // A bit of a weak way to know if this wand is left or right... There must be a better way
		if (_bIsLeft) {
			_sNameWand = "Left";
            _chNameWand = 'L';
            _nOrdinal = 0;
        } else {
			_sNameWand = "Right";
            _chNameWand = 'R';
            _nOrdinal = 1;
        }

        //=== Create the wand GUI panel ===
        //Transform oModelAttachParentT = transform;      //###IMPROVE: When on SteamVR we had a nice 3D model of wand with useful anchors we could use.  Oculus doesn't have that so we're stuck with our plain old transform node.  _oWandSteamT.Find("Model/handgrip/attach").transform;
        //if (_oVrPanelWand == null) {            //###HACK:!!! Panel should connect to us as opposed to the other way around
        //    if (_bIsLeft == false) {            //###HACK:!!!!  Bad hack with left / right VR panels!
        //        _oVrPanelWand = new CVrPanelWand(this, oModelAttachParentT);
        //        _oVrPanelWand._oBrowser.gameObject.SetActive(true);         // Panel hidden until brought close to headset      #DEV26:
        //    }
        //}

        //=== Obtain access to our parent.  This is the one that moves in relation to VR camera rig.  (Our gameObject is always in lock-step with our parent) ===
        _oVrWandParent = transform.parent;

        //=== Obtain references to the necessary resources we need ===
        _oWandTipT = transform;// _oWandSteamT.Find("Model/tip/attach").transform;		// Obtain access to the transform in our model's nodes that reveals the position / orientation of the wand tip ===//###IMPROVE: Can obtain tip position / orientation in a better way through API??  Also would be nicer to have this in Start() but because of VRTK reparenting this is too early

        //=== Find the remote mover transforms under our CGame game object and reparent to the camera rig ===
        //_oRemoteMove_PivotT = CUtility.InstantiatePrefab<Transform>("Prefabs/CVrWand_RemoteMove_Pivot", null, CGame._oCameraRigT);
        _oRemoteMove_PivotT = CUtility.InstantiatePrefab<Transform>("Prefabs/CVrWand_RemoteMove_Pivot");
        _oRemoteMove_PivotedT   = _oRemoteMove_PivotT.  Find("CVrWand_RemoteMove_Pivoted");
        _oRemoteMove_ObjectT    = _oRemoteMove_PivotedT.Find("CVrWand_RemoteMove_Object");
        //_oRemoteMove_PivotT.SetParent(CGame._oCameraRigT);

        //=== Finished initialization.  Enable Update() to run ===
        enabled = true;
    }
    #endregion

#region ======================================================================	UTIL
    Vector3 Util_GetWandLocalPosition() {                       // Returns the wand position local to its owning camera rig.  Used to control camera
        return _oVrWandParent.localPosition;
        //if (_bIsLeft)
        //    return OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTrackedRemote);
        //else
        //    return OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTrackedRemote);
    }
    Quaternion Util_GetWandLocalRotation() {                    // Returns the local wand rotation.  Used to control camera
        return _oVrWandParent.localRotation;
        //if (_bIsLeft)
        //    return OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTrackedRemote);
        //else
        //    return OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTrackedRemote);
    }
    Vector3 Util_GetWandTipLocalPosition() {					// Returns the position of the wand tip as local to the owning camera rig.  Used for controlling objects
		return _oWandTipT.position - CGame._oCameraRigT.position;		
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
    public CBodyBase GetBody() {
        return CGame._aBodyBases[0];           //###TODO: Return body most appropriate to this left or right wand
    }
    public void AssignToObject(Transform oVrWandObject) {
        //=== Assign the object we're controlling.  Set by speech recognition engine ===
        _oVrWandObject = oVrWandObject;
    }
#endregion

#region ======================================================================	BONES
    Transform ObjMover_ScanForBones() {
        Transform oObjectMovedT = null;

        ////=== First check if the wand collides with a moveable object directly.  (This would set up 'direct move' mode) ===
        Collider oColFound = null;         //###IMPROVE: Enumerate through the colliders to find the best one??     //#DEV26: Completely ditch direct mode??
        //Collider[] aColliders = Physics.OverlapSphere(transform.position, 0.01f, G.C_LayerMask_Bones);
        //if (aColliders.Length > 0)
        //    oColFound = aColliders[0];         //###IMPROVE: Enumerate through the colliders to find the best one??

        //=== If not collider was found at the wand position the look for one by forming a ray from the VR headset toward its gazing direction ===
        
        if (oColFound == null) {
            RaycastHit oRayHitBonesOnly;
            bool bHit = Physics.Raycast(CGame._oHeadsetT._oRayHeadsetVR, out oRayHitBonesOnly, float.MaxValue, G.C_LayerMask_Bones);      //###INFO: Perform raycast against bones only    ###IMPROVE: Raycast to body surface and it can remap to which bone for what surface?  (Too complex and not much benefit??)
            oColFound = oRayHitBonesOnly.collider;            // Get the collider from the constantly-running headset raycaster
        }

        //=== Setup a move operation If a collider was found (either right under wand or by raycast from headset) ===
        if (oColFound != null) {
            CBone oBoneFound = oColFound.transform.GetComponent<CBone>();
            if (oBoneFound) {                    // We don't move these bones directly.  Instead we move the actor pinning them in place
                string sNameBone = oBoneFound.transform.name;
                if (sNameBone.Contains("chest")) {
                    oObjectMovedT = CGame._aBodyBases[0]._oActor_Chest.transform;
                } else if (sNameBone.Contains("abdomen")) {
                    oObjectMovedT = CGame._aBodyBases[0]._oActor_Genitals.transform;
                } else if (sNameBone.Contains("hip") || oBoneFound.transform.name.Contains("pelvis")) {
                    oObjectMovedT = CGame._aBodyBases[0]._oActor_Pelvis.transform;
                } else if (sNameBone.Contains("Twist")) {           // Don't move / rotate the twist bones directly (as it bends the limb mid-way!)  Rotate the parent's twisting bone instead... always one level up
                    //Debug.LogErrorFormat("CVrWand selected bone '{0}' instead of '{1}'", oBoneFound.transform.parent.name, oBoneFound.transform.name);
                    oObjectMovedT = oBoneFound.transform.parent;
                } else {
                    oObjectMovedT = oBoneFound.transform;
                }
            }
        }
        return oObjectMovedT;
    }
#endregion

#region ====================================================================== VR CAMERA WAND MOVE
    void Camera_SetupForVrWand() {
        _vecPosStart_Wand       = Util_GetWandLocalPosition();      // Store *local* position / rotation of our wand relative to the camera rig.  We will apply 'delta' position / rotations from this starting point at each frame in Update()
        _quatRotStart_Wand      = Util_GetWandLocalRotation();      // We observe the *local* position of our parent.  (that is what SteamVR pushes in.  We don't use the global position as we move its parent.parent!!)
        _quatRotStartInv_Wand   = Quaternion.Inverse(_quatRotStart_Wand);
    }

    void Camera_UpdateFromVrWand() {
        //=== Obtain camera rig position / rotation (so we can modify more easily) ===
        Vector3 vecCameraRigPos = CGame._oCameraRigT.position;
        Quaternion quatCameraRigRot = CGame._oCameraRigT.rotation;

        //=== Obtain the fresh position and rotation of the wand tip relative to the camera rig ===
        Vector3 vecPosNow_Wand = Util_GetWandLocalPosition();
        Quaternion quatRotNow_Wand = Util_GetWandLocalRotation();

        //=== Obtain the delta position / rotation of the wand tip since the start of this control operation ===
        Vector3 vecPosNowDelta_Wand = vecPosNow_Wand - _vecPosStart_Wand;
        Quaternion quatRotNowDelta_Wand = quatRotNow_Wand * _quatRotStartInv_Wand;     //###INFO: How to determine quaternion difference -> With Quaternion.Inverse()!

        //=== Rotate the delta position about the camera position.  (We need to keep camera rig rotation out of Util_GetWandLocalPosition() so time is not a factor) ===
        vecPosNowDelta_Wand = CGame._oCameraRigT.rotation * vecPosNowDelta_Wand;      //###INFO  vecOut = Quaternion.Inverse(quatRot) * vecIn; // same as vecIn * quatRot

        Quaternion quatRotNowDelta_Wand_ScaledDown = Util_ScaleQuaternion(quatRotNowDelta_Wand, CGame._nVrObjectControlRot);
        quatRotNowDelta_Wand_ScaledDown.x = quatRotNowDelta_Wand_ScaledDown.z = 0;

        //--- Apply the scaled-down delta position and rotation ---
        vecCameraRigPos += vecPosNowDelta_Wand * CGame._nVrObjectControlPos;   // Apply a scaled-down delta vector
        quatCameraRigRot = quatRotNowDelta_Wand_ScaledDown * quatCameraRigRot;          //###INFO: How to apply an angle... Not commutative!!
                                                                                        //###OFF:!!! vecCameraRigPos.y = Mathf.Clamp(vecCameraRigPos.y, -0.8f, 1.0f);        //###TUNE		###IMPROVE: Camera rig CAN go underneath y=0 but camera itself should not!  Clamp camera pos instead of rig!        //=== Make sure we are not trying to go too far up or down ===
        //=== Adjust the camera rig position / rotation ===
        CGame._oCameraRigT.position = vecCameraRigPos;
        CGame._oCameraRigT.rotation = quatCameraRigRot;
    }
#endregion

#region ====================================================================== PANEL CONTROL VIA TRIGGERS
    //   private void OnTriggerEnter(Collider other) {
    //       if (_eVrOp != EVrOp.None)      // Only process trigger if we're not in a operation
    //           return;
    //       //if (_oActorSelected) {
    //       //	CGame._aDebugMsgs[(int)EMsg.VrControlCam] = string.Format("VrCol: {0}", _oActorSelected.gameObject.name);
    //       //}
    //       if (other.gameObject.name == "Trigger-Panel-Show") {                //###WEAK: Test of triggers by their node name.  (Them having a custom object would be more robust)
    //           if (_oVrPanelWand != null && _oVrPanelWand._oBrowser != null)
    //               _oVrPanelWand._oBrowser.gameObject.SetActive(true);
    //           //CGame._aDebugMsgs[(int)EMsg.VrControlCam] = string.Format("VrCol: {0}", other.gameObject.name);
    //       }
    //   }
    //private void OnTriggerExit(Collider other) {
    //       if (_eVrOp != EVrOp.None)      // Only process trigger if we're not in a operation
    //           return;
    //       //CGame._aDebugMsgs[(int)EMsg.VrControlCam] = string.Format("VrCol: none");
    //       if (other.gameObject.name == "Trigger-Panel-Hide") {                //###WEAK: Test of triggers by their node name.  (Them having a custom object would be more robust)
    //           if (_oVrPanelWand != null && _oVrPanelWand._oBrowser != null)
    //               _oVrPanelWand._oBrowser.gameObject.SetActive(false);
    //           //CGame._aDebugMsgs[(int)EMsg.VrControlCam] = string.Format("VrCol: none");		
    //       }
    //   }
#endregion

#region ====================================================================== INPUT
    public Vector2 Input_GetJoystickAxis() {
#if __VIVE__
        return _oVrControlEvents.GetTouchpadAxis();
#else
        if (_bIsLeft)           //###INFO: See mapping at https://docs.unity3d.com/Documentation/Manual/OculusControllers.html
            return OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);      //###INFO: How to obtain reliable data from wands by bypassing (sometime unreliable) VRTK
        else        //###INFO: _oVrControlEvents.GetTouchpadAxis() does NOT work well in our code (but fine in VRTK sample)... Maybe because we keep FPS so low?  Anyways going to native as it's much more accurate / reliable
            return OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);    //###INFO: See docs at https://developer3.oculus.com/doc/0.1.3.0-unity/annotated.html
#endif
    }

    public float Input_GetTrigger() {
#if __VIVE__
        return _oVrControlEvents.GetTriggerAxis();
#else
        if (_bIsLeft)
            return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
        else
            return OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);
#endif
    }

    public float Input_GetGrip() {
#if __VIVE__
        return _oVrControlEvents.gripClicked ? 1 : 0;     //###INFO: How to get grip axis info!  VRTK BROKEN!!!: GetGripAxis() returns zero at source level so have to get it from 3D model!!!  VRTK fundamentally broken at source level... but openvr gets value as it populates wand model properly! (Call             if (!renderModels.GetComponentState(renderModelName, name, ref controllerState, ref controllerModeState, ref componentState))  ###CHECK: Will this work on SteamVR?
        //return VRTK.VRTK_SDK_Bridge.GetGripAxisOnIndex(_nControllerIndex).x;       //###INFO: How to get grip axis info!  VRTK BROKEN!!!: GetGripAxis() returns zero at source level so have to get it from 3D model!!!  VRTK fundamentally broken at source level... but openvr gets value as it populates wand model properly! (Call             if (!renderModels.GetComponentState(renderModelName, name, ref controllerState, ref controllerModeState, ref componentState))  ###CHECK: Will this work on SteamVR?
#else
        if (_bIsLeft)
            return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger);
        else
            return OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger);
#endif
    }

    public bool Input_GetJoystickButtonDown() {
#if __VIVE__
        //return false;
        return _oVrControlEvents.touchpadPressed;
#else
        if (_bIsLeft)
            return OVRInput.Get(OVRInput.Button.PrimaryThumbstick);
        else
            return OVRInput.Get(OVRInput.Button.SecondaryThumbstick);
#endif
    }

    public bool Input_GetJoystickThumbRest() {
#if __VIVE__
        return false;
#else
        if (_bIsLeft)                           // Returns if the user has left his/her thumb on the 'thumb rest' area of the Vr Wand.  ###DESIGN: exists on Vive?
            return OVRInput.Get(OVRInput.RawTouch.LThumbRest);
        else
            return OVRInput.Get(OVRInput.RawTouch.RThumbRest);
#endif
    }
    public bool Input_GetButton_BrowserLeftMouseButton() {            // Returns the status of this wand's 'button one' on Oculus or the 'menu button' on the vive
        return (_eJoyDir == EJoyDir.Up_ClickWebBrowserGUI);
        //return (Input_GetTrigger() > C_CutoffOnButtonAxis);          //###CHECK: Best / only button for vive is trigger??
#if __VIVE__
        return _oVrControlEvents.buttonOnePressed;              //###CHECK
#else
        if (_bIsLeft)
            return OVRInput.Get(OVRInput.RawButton.X);          // 'X' is the 'button one' on the left  wand
        else
            return OVRInput.Get(OVRInput.RawButton.A);          // 'A' is the 'button one' on the right wand
#endif
    }
#endregion

#region ======================================================================	OBJECT MOVER
    void ObjMover_Start() {
        Vector3 vecPosObjectStart = _oObjectBeingMovedT.position;

        //=== Setup the remote of local move by reparenting the mover object to the wand ===
        switch (_eVrOp) {
            case EVrOp.ObjectMove_Remote:       // Collider found through headset raycast hit = setup remote object movement
                _vecLocalToHeadset_WandPos_Start = CGame._oHeadsetT.transform.worldToLocalMatrix.MultiplyPoint(transform.position);     //#@
                _oRemoteMove_PivotT.position    = vecPosObjectStart;                             // Set the pivot position at the object's starting position...
                _oRemoteMove_PivotT.rotation    = CGame._oHeadsetT.transform.rotation;  // Set the pivot rotation at the camera's start rotation.  This is essential to properly interpret the wand movements (local to camera)
                _oRemoteMove_PivotedT.position  = vecPosObjectStart;                             // Set the pivoted position at the object's starting position.  It starts at the same position but will have its 'localPosition' updated while 'pivot' stays fixed
                _oRemoteMove_PivotedT.rotation  = transform.rotation;                            // Set the pivoted rotation to the object's starting rotation.
                _oRemoteMove_ObjectT.position  = _oObjectBeingMovedT.position;                   // Set the copy of the object to the object position and orientation.  At every frame during the move we do the inverse (copy of object to object)
                _oRemoteMove_ObjectT.rotation  = _oObjectBeingMovedT.rotation;
                _oRemoteMove_PivotT  .GetComponent<MeshRenderer>().enabled = C_ShowDebugMarkers;
                _oRemoteMove_PivotedT.GetComponent<MeshRenderer>().enabled = C_ShowDebugMarkers;
                _oRemoteMove_ObjectT .GetComponent<MeshRenderer>().enabled = C_ShowDebugMarkers;
                break;
            case EVrOp.ObjectMove_Direct:       // Collider found right under wand = setup direct object movement
                CUtility.ThrowException("NOT IMPLEMENTED");     //###BROKEN: Since we're no longer re-parenting
                //_oObjectBeingMovedT.SetParent(transform);
                //_oObjectBeingMovedT.localPosition = Vector3.zero;
                break;
        }

        CGame._aDebugMsgs[(int)EMsg.VrWand_ObjectMover] = string.Format("ObjMove:  Mover:{0}  Op:{1}", _oObjectBeingMovedT.name, _eVrOp.ToString());
    }

    void ObjMover_End() {       //###OBS:? Merge with big switch in Update()?
        if (_oObjectBeingMovedT) {
            _oRemoteMove_PivotT  .GetComponent<MeshRenderer>().enabled = false;
            _oRemoteMove_PivotedT.GetComponent<MeshRenderer>().enabled = false;
            _oRemoteMove_ObjectT. GetComponent<MeshRenderer>().enabled = false;
            _oObjectBeingMovedT = null;
            CGame._aDebugMsgs[(int)EMsg.VrWand_ObjectMover] = string.Format("ObjMove: None");
        }
        if (_oVrWandMoveable != null) {
            _oVrWandMoveable.VrWandMove_End(this);
            _oVrWandMoveable = null;
        }
        _eVrOp = EVrOp.None;              // End the control object op
    }
#endregion

#region ====================================================================== UPDATE
    private void FixedUpdate() {            //###CHECK: Update() or FixedUpdate()?
		if (CGame.INSTANCE == null)
			return;

        if (CGame.INSTANCE._bFlag_HACK)
            _oVrControlEvents.ForceUpdate_HACK();

        //###DEV27: ###TEMP
        float nFingerClose = CGame._oObj.Get("DEV_FingerClose_Moving");
        CGame.INSTANCE.GetBodyBase(0)._oActor_ArmL.HandPose_Set(CActorArm.EHandPose.GrabCup, _nInputTrigger * nFingerClose);

        //=== Obtain fully-qualified wand mode from joystick position ===
        if (Input_GetJoystickButtonDown()) {
            //_eJoyDir = EJoyDir.Button_TODO;
            _eJoyStrength = EJoyStrength.Idle;
            //} else if (Input_GetJoystickThumbRest()) {                    //###DESIGN: Should influence top-level mode??
            //    eJoyDir = eJoyDir.ThumbRest_TODO;
        //} else {          //#DEV26S: Huge change for Vive and speech!!
            Vector2 vecTouchpadAxis = Input_GetJoystickAxis();            //###INFO: Oculus can also decode these internally but our implementation we can determine 'cutoff'
            float nX = vecTouchpadAxis.x;
            float nY = vecTouchpadAxis.y;
            float nX_abs = Mathf.Abs(nX);
            float nY_abs = Mathf.Abs(nY);
            float nValMax_abs = Mathf.Max(nX_abs, nY_abs);
            if (nValMax_abs > C_CutoffAxis_MinForSelect) {
                if (nX_abs > 2 * nY_abs) {                  //###NOTE: Only change mode when joystick is close to a cardinal vector. ###TUNE: Joystick angle dead-zone
                    if (nX > 0)
                        _eJoyDir = EJoyDir.Right_HandR;
                    else
                        _eJoyDir = EJoyDir.Left_HandL;
                } else if (nY_abs > 2 * nX_abs) {
                    if (nY > 0)
                        _eJoyDir = EJoyDir.Up_ClickWebBrowserGUI;
                    else
                        _eJoyDir = EJoyDir.Down_ConfigWandModeWithSpeech;
                }
                if (nValMax_abs >= C_CutoffAxis_MinForAction)
                    _eJoyStrength = EJoyStrength.Action1;
                else
                    _eJoyStrength = EJoyStrength.Select;
            } else {
                _eJoyStrength = EJoyStrength.Idle;      //###NOTE: We do NOT clear the joystick direction flag as it is mean to remain selected even when joystick returns to center.  If user wants another mode he/she needs to point joystick in a different direction
            }
        } else {
            _eJoyDir = EJoyDir.None;
            _eJoyStrength = EJoyStrength.Idle;
        }

        _nInputTrigger = Input_GetTrigger();
        _nInputGrip    = Input_GetGrip();

        bool bJoy_Changed       = (_eJoyDir != _eJoyDir_COMPARE);
        bool bJoy_Action        = (_eJoyStrength == EJoyStrength.Action1);
        bool bJoy_Start         = (_eJoyStrength == EJoyStrength.Action1) && (_eJoyStrength_COMPARE != EJoyStrength.Action1);
        bool bShowCursor        = (_eJoyStrength == EJoyStrength.Select);
        bool bTrigger_Action    = (_nInputTrigger > C_CutoffAxis_MinForSelect);             //###INFO: Vive trigger will show values around 1-4 even when finger off!!
        bool bTrigger_Start     = (bTrigger_Action == true ) && (_bTrigger_Action_COMPARE == false);
        bool bTrigger_End       = (bTrigger_Action == false) && (_bTrigger_Action_COMPARE == true );
        bool bGrip_Action       = false;//(_nInputGrip >= C_CutoffAxis_MinForAction);
        bool bGrip_CamMove      = (_nInputGrip > C_CutoffAxis_MinForSelect) /*&& (bGrip_Action == false)*/;
        bool bGrip_Start        = (bGrip_Action == true ) && (_bGrip_Action_COMPARE == false);
        bool bGrip_End          = (bGrip_Action == false) && (_bGrip_Action_COMPARE == true );
        bool bOpStarts          = (bTrigger_Start || bGrip_Start || bJoy_Start || bGrip_CamMove) && (_eVrOp == EVrOp.None);      // New operations only start with either the trigger or the grip button are depressed.
        bool bOpContinues       = (bJoy_Action || bTrigger_Action || bGrip_Action || bGrip_CamMove) && (_eVrOp != EVrOp.None);   // Operations continue as long as the joystick axis, the trigger or grip buttons are depressed
        bool bOpEnds            = (_eVrOp != EVrOp.None) && (bOpContinues == false);            // Operations end when there was an op and it no longer continues

        //=== Only draw the headset gaze cursor when trigger partially pressed ===
        CGame._oHeadsetT._bShowHeadsetGazeCursor = bShowCursor;

        if (bOpStarts) {

            _eVrOp = EVrOp.None;
            CBodyBase oBodyBase = GetBody();

            if (bGrip_CamMove) {            // Grip camera move takes precedent over any joystick mode select.  We must always have quick access to camera move / rotate!

                Camera_SetupForVrWand();
                _eVrOp = EVrOp.Camera;      //###IMPROVE: Create a camera class that implements interface IVrWandMoveable?

            } else {
                switch (_eJoyDir) {
                    case EJoyDir.Up_ClickWebBrowserGUI:
                        break;
                    case EJoyDir.Down_ConfigWandModeWithSpeech:
                        _eVrOp = EVrOp.ConfigWandModeWithSpeech;
                        if (CGame._oSpeechServerProxy)
                            CGame._oSpeechServerProxy.WandConfig_ObjectAssignment_Begin(this);      // Tell speech server to load the grammar to configure this wand.  (Ends when op ends)
                        break;
                    case EJoyDir.Left_HandL:
                    case EJoyDir.Right_HandR:                               //#Wand FSM
                        if (_eJoyDir == EJoyDir.Left_HandL)                 //###TODO: Return the hand most appropriate for this left / right joystick direction.
                            _oVrWandMoveable = oBodyBase._oActor_ArmR;      //###DEV27: ###CHECK
                        else
                            _oVrWandMoveable = oBodyBase._oActor_ArmL;
                        //###DEV27X: _oObjectBeingMovedT = _oVrWandMoveable.VrWandMove_Begin(this, _eJoyStrength == EJoyStrength.Action1, bTrigger_Start);
                        _oObjectBeingMovedT = _oVrWandMoveable.VrWandMove_Begin(this, false, true);
                        if (_eJoyStrength == EJoyStrength.Action1) {
                            _eVrOp = EVrOp.HandsCaressingBody;
                        } else if (bTrigger_Start) {
                            _eVrOp = EVrOp.ObjectMove_Remote;
                        }
                        break;
                    default:
                        break;
                }
                if (bTrigger_Action && _oVrWandObject) {
                    _oObjectBeingMovedT = _oVrWandObject;
                    _eVrOp = EVrOp.ObjectMove_Remote;
                }

                if (_oObjectBeingMovedT)
                    ObjMover_Start();
            }

            Debug.LogFormat("- VrWand: {0} begins new op '{1}' on joystick mode '{2}'", _sNameWand, _eVrOp.ToString(), _eJoyDir.ToString());

        } else if (bOpEnds) {

            ObjMover_End();
            CGame.INSTANCE.CumControl_Stop();
            if (CGame._oSpeechServerProxy)
                CGame._oSpeechServerProxy.WandConfig_ObjectAssignment_End(this);        //###CHECK:27!!!
            CGame._aDebugMsgs[(int)EMsg.Dev4] = "";
            Debug.LogFormat("- VrWand: {0} ends op.", _sNameWand);
                                     
        } else if (bOpContinues) {

            switch (_eVrOp) {
                case EVrOp.Camera:
                    Camera_UpdateFromVrWand();
                    break;

                case EVrOp.ObjectMove_Direct:               //###OBS:?
                    if (_oVrWandMoveable != null)           //###CHECK: Needed?
                        _oVrWandMoveable.VrWandMove_Update(this);
                    break;
                case EVrOp.ObjectMove_Remote:
                    Vector3 vecLocalToHeadset_WandPos_Now = CGame._oHeadsetT.transform.worldToLocalMatrix.MultiplyPoint(transform.position);
                    Vector3 vecLocalToHeadset_WandPos_Diff = vecLocalToHeadset_WandPos_Now - _vecLocalToHeadset_WandPos_Start;
                    _oRemoteMove_PivotedT.localPosition = vecLocalToHeadset_WandPos_Diff;
                    _oRemoteMove_PivotedT.rotation  = transform.rotation;     //#DEV26: WTF bug why null
                    _oObjectBeingMovedT.position = _oRemoteMove_ObjectT.position;        // Set the position and orientation of the object being moved to the position / rotation of its 'copy' in our remove moving rig
                    _oObjectBeingMovedT.rotation = _oRemoteMove_ObjectT.rotation;
                    if (_oVrWandMoveable != null)           //###CHECK: Needed?
                        _oVrWandMoveable.VrWandMove_Update(this);
                    if (_eJoyDir == EJoyDir.Down_ConfigWandModeWithSpeech) {        //#DEV26: ###TEMP
                        if (bTrigger_Action) {
                            //foreach (CFlexEmitter oFlexEmitter in CGame._oFlexParamsFluid._setFlexEmitters)      //###BUG:!!!  Why is direct velocity control not working when curve works???
                            //    oFlexEmitter._nEmitVelocity_DirectControl = _nInputTrigger;
                        } else{
                            CGame.INSTANCE.CumControl_Stop();
                            //foreach (CFlexEmitter oFlexEmitter in CGame._oFlexParamsFluid._setFlexEmitters)
                            //    oFlexEmitter._nEmitVelocity_DirectControl = 0;
                        }
                    }
                    break;

                case EVrOp.HandsCaressingBody:
                    CActorArm oActorArm = CGame.INSTANCE.GetBodyBase(0)._oActor_ArmL;       //###HACK:
                    //if (oActorArm == (object)_oVrWandMoveable) {
                    //    float nFingerClose = CGame._oObj.Get("DEV_FingerClose_Moving");
                    //    CGame.INSTANCE.GetBodyBase(0)._oActor_ArmL.HandPose_Set(CActorArm.EHandPose.GrabCup, _nInputTrigger * nFingerClose);
                    //}
                    _oVrWandMoveable.VrWandMove_UpdatePositionAndRotation(CGame._oHeadsetT._oHeadsetGazeT);      //###NOTE: While caressing body the headset gaze is the transform that stores the position and rotation of where the moved object should go to 'caress' the body.
                    break;

                default:
                    break;
            }
        }

        _eJoyDir_COMPARE            = _eJoyDir;
        _eJoyStrength_COMPARE       = _eJoyStrength;
        _bTrigger_Action_COMPARE    = bTrigger_Action;
        _bGrip_Action_COMPARE       = bGrip_Action;

#if __DEBUG__
        Vector2 vecJoystick = Input_GetJoystickAxis();
        bool bJoystickButtonDown = Input_GetJoystickButtonDown();
        bool bJoystickThumbRest  = Input_GetJoystickThumbRest();
        CGame._aDebugMsgs[(int)EMsg.VrWandInputL+_nOrdinal] = string.Format("Vr{0}:  JD:{1}  JS:{2}  Op:{3}  Tr:{4:F0}  Gr:{5:F0}  X:{6:F0}  Y:{7:F0}  B:{8}  R:{9}",       //  Msg:'{10}'
            _chNameWand, _eJoyDir.ToString(), _eJoyStrength.ToString(), _eVrOp.ToString(), _nInputTrigger*100, _nInputGrip*100, vecJoystick.x*100, vecJoystick.y*100, bJoystickButtonDown ? 1 : 0, bJoystickThumbRest ? 1 : 0);
#endif
    }
#endregion

#region ====================================================================== ENUMS
    public interface IVrWandMoveable {
        Transform VrWandMove_Begin(CVrWand oVrWand, bool bStartAction1, bool bStartAction2);
        void VrWandMove_End(CVrWand oVrWand);
        void VrWandMove_Update(CVrWand oVrWand);            //###OBS? Use other call for everything?
        void VrWandMove_UpdatePositionAndRotation(Transform oNodeT);
    };
    public enum EVrControlAbsRel {
        Absolute,
        Relative,
    }

    public enum EVrOp {     //#DEV27: Revisit!
        None,                       // Nothing is currently being moved / rotated by this wand.
        Camera,                     // The camera is currently being moved / rotated
        ConfigWandModeWithSpeech,   // Wand is being configured with speech
        ObjectMove_Direct,          // Move a limb by creating a D6 joint and baking angles at every frame
        ObjectMove_Remote,          // Moving an object that was selected by the camera gaze hitting a valid collider.
        HandsCaressingBody,         // Special control mode required for moving the hands alongside the body
    }

    public enum EJoyDir {                   // The current joystick mode.  Completely picks what mode this wand is in
        None,                               // Joystick at center and unpressed = Default pelvis control
        Left_HandL,                         // Joystick Left:  Left  hand control
        Right_HandR,                        // Joystick Right: Right hand control
        Up_ClickWebBrowserGUI,              // Joystick Up: Web Browser 'left mouse button'
        Down_ConfigWandModeWithSpeech,      // Joystick down: Configure this wand's mode with a speech command.
        ThumbRest_TODO,                     // Thumb on 'thumb rest' area (aka 'Shoulder')
        Button_TODO,                        // Joystick button pressed: ###TODO
    }

    public enum EJoyStrength {
        Idle,                               // Joystick is in the centered position (idle)
        Select,                             // Joystick is close to center but not at center = changes wand mode and show cursor
        Action1,                            // Joystick is near its travel limit and far away from center = triggers 'modal action 1'
    }

    public enum EGripStrength {
        Idle,                               // Grip is idle and not moving camera or performing an action
        Camera,                             // Grip is partly depressed and moving / rotating the VR Headset camera
        Action2,                            // Grip is fully depressed and performing the modal 'secondary' action
    }
#endregion
}

//------------- Old code when we had dual grip action.  (Vive has button!)
//bool bGrip_Action       = (_nInputGrip >= C_CutoffAxis_MinForAction);
//bool bGrip_CamMove      = (_nInputGrip >= C_CutoffAxis_MinForSelect) && (bGrip_Action == false);
//bool bGrip_Start        = (bGrip_Action == true ) && (_bGripOn_COMPARE == false);
//bool bGrip_End          = (bGrip_Action == false) && (_bGripOn_COMPARE == true );
//bool bOpStarts          = (bTrigger_Start || bGrip_Start || bJoy_Start || bGrip_CamMove) && (_eVrOp == EVrOp.None);      // New operations only start with either the trigger or the grip button are depressed.
//bool bOpContinues       = (bJoy_Action || bTrigger_Action || bGrip_Action || bGrip_CamMove) && (_eVrOp != EVrOp.None);   // Operations continue as long as the joystick axis, the trigger or grip buttons are depressed
//bool bOpEnds            = (_eVrOp != EVrOp.None) && (bOpContinues == false);            // Operations end when there was an op and it no longer continues


//_oRayHeadsetVR.origin    = CGame._oHeadsetT.transform.position;
//_oRayHeadsetVR.direction = CGame._oHeadsetT.transform.forward;
//bHitFromHeadsetRaycast = Physics.Raycast(_oRayHeadsetVR, out _oRayHit, float.MaxValue, G.C_LayerMask_Bones);
//oColFound = _oRayHit.collider;



//switch (_eJoyDir) {
//    case EJoyDir.None:                                       // None = idle joystick direction = Move camera on grip, move pelvis on trigger ###KEEP:??
//        if (bTrigger_Action) {
//            _oObjectBeingMovedT = oBodyBase._oActor_Pelvis.transform;
//            _eVrOp = EVrOp.ObjectMove_Remote;
//        }
//        break;
//    case EJoyDir.Left_HandL:
//    case EJoyDir.Right_HandR:                               //#Wand FSM
//        if (_eJoyDir == EJoyDir.Left_HandL)                 //###TODO: Return the hand most appropriate for this left / right joystick direction.
//            _oVrWandMoveable = oBodyBase._oActor_ArmR;
//        else
//            _oVrWandMoveable = oBodyBase._oActor_ArmL;
//        _oObjectBeingMovedT = _oVrWandMoveable.VrWandMove_Begin(this, _eJoyStrength == EJoyStrength.Action1, bTrigger_Start);
//        if (_eJoyStrength == EJoyStrength.Action1) {
//            _eVrOp = EVrOp.HandsCaressingBody;
//        } else if (bTrigger_Start) {
//            _eVrOp = EVrOp.ObjectMove_Remote;
//        }
//        break;
//    case EJoyDir.Up_ClickWebBrowserGUI:
//        _oObjectBeingMovedT = ObjMover_ScanForBones();
//        _eVrOp = EVrOp.ObjectMove_Remote;
//        break;
//    case EJoyDir.Down_ConfigWandModeWithSpeech:                     //###TODO: Controlling this fucking character through wand.
//        _oObjectBeingMovedT = CGame.INSTANCE.GetBody(0)._oSoftBody_Penis._oCumEmitterT.transform;
//        //_oObjectBeingMovedT = CGame.INSTANCE.GetBodyBase(0).FindBoneByPath("hip/pelvis/Genitals/#Penis");      //#DEV26: ###HACK
//        CGame.INSTANCE.CumControl_ClearCum();
//        CGame.INSTANCE.CumControl_Start();
//        _eVrOp = EVrOp.ObjectMove_Remote;
//        break;
//    case EJoyDir.Button_TODO:                   //###TODO: What to control when button is pressed?
//        _oObjectBeingMovedT = null;
//        break;
//}
