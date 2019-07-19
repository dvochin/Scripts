/*###DISCUSSION: ACTORS
=== NEW BODY ===  ###NOW###
-LAST: Fixed shitload of bugs now we have SB again...  Would be fun to have big tits, so re-insert bodies with morphing and dynamically set to a morphed shape?

-LAST: Did arm and legs with some problems on some axes.   Now torso, neck, head then colliders!

- Finalize bone drive strength overall and multipliers... all relative to gravity!
	- How to finalize gravity!?!?!

- Poses all corrupt now.  Throw away??    (Load disabled)

- WTF wrong with pins now?  Inverted?  Back??  Why at second time?  Reset does something bad?
    - Review which pins should be just spring versus angles too.
- Missing textures!  Improve in Blender too!  (Genitals change all the time!)
- PROBLEM: Blender now showing extra armatures!!

- Changed joint behavior to no longer invert Y & Z!  Poses now fucked!  Can fix?
- Go through Daz and extract the damn angles!
- Do the colliders

- Huge hack with CObj name!!!!!
- Add warning when clipping y,z in joint driver!
- How to handle joint axis rotation?  What about L/R of body??
- How to handle different y,z rotations??  Switch axis??






=== NEXT ===
 * Reparenting of hands a big issue now for good pose creation... have to make them relative to something!!
 * Add more hand targets all over to test the simple design's viability...
 * Animate the hands!;
=== TODO? ===
 * Remove limited on pins... only strong slerp??
 * BUG!  Moving chest sets it at start pos??  WTF?
 *? Hand twists can be done with rotation!
 *
=== LATER ===

=== IMPROVE ===
 * Change friction so dick glides

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===
 * Resettopos always??
- Collider problems: toes and hands not symmetrical!
- Why chest, thumb and hand not show??
- Thigh split requires +Z one one and -Z the other... an issue?

=== PROBLEMS??? ===
 * We only display hotspot at bone, never where pin is (causes non-intuitive behavior when pinning and extremity snaps to some location user doesn't see
 * Pins oriented along X of extremities but this looks weird for most bones... orient towards Y+?
=== WISHLIST ===
 * Need to pin actors to bones, etc (e.g. hands to breasts, hands to back of head, etc)


===== DESIGN REQUIREMENTS: POSE DEFINITION SINGLE BODY AND COUPLE =====
--- NEEDS ---
 * Really need to clarify the concept of the pose root type: flat on floor, on bed ledge, etc.
   * Will need to filter in a 'pose slot' what can load?
 * Force some of the pins on some axis?  (e.g. base always at floor level)

--- SIMPLIFICATIONS ---
 * 
 * 
===== DESIGN REQUIREMENTS: POSE =====
--- NEEDS ---
 * Interchanging of pose for woman and man... as coupled by man's penis position and female's vagina
 * Find what is compatible for currently-loaded sex with other-sex compatible poses.

--- SIMPLIFICATIONS ---
 * We only support two axis on everything.  (Body left-to-right everywhere is completely ignored)
   * Penis's penis always pointing toward -Z, female vagina opening always pointed toward +Z, +Y is always up for both man/woman, X totally ignored.
 * This design of separating man and woman to facilitate permutations is not supposed to adapt to every possible pose!
   * Poses that need angled bodies, custom orientations, etc will need to be created in a 'baked pose' storing both actors... these cannot interchange and therefore not benefit from design permutations
 * The first version assumes extensive user-interaction needed for working penetration: after first release we can add more AI for less user interaction
   * Loading two compatible man/woman poses gets you 'close enough' for tease animations and penetration (if user correctly orients penis)

 * Pose for man includes penis curve and tip position
--- QUESTIONS ---
 * Do we support variable-length penis?  (e.g. backing up chest pins on longer penis possible, but can't change heigh if penis pointing upward/downward!)
 * Do we force X=0 to central pins while saving?
 * Have 'heavy foot' that remain at 'ground level' = base pin height?

--- DECISIONS ---
 * Each man & woman pose identifies a height and forward/backward range.
   * The non-controlled character will move toward meeting point
 * It falls upon the player to set the right conditions for hot actions like 'pussy rub' and 'penetration'
   * Easily-controllable sliders are provided for penis base angle, penis shaft up/down and vagina angle
   * Easily-accessible capability to move the sex of either character pass the onus on the player to make most starting poses come to life.
     * Easy capacity to set simple two-point animations made possible by middle-mouse-click on sex bone and setting two locations.
   * Lit buttons appear on screen when pussy-rub is possible and penetration
 * The 'meeting point' is the tip of the penis and the entry to the vagina

--- ACTION-STEPS ---
 * Need to serialize penis-based and vagina-based poses seperately.  Owned by CBody now instead of CGamePlay

--- USE CASES ---
 * 
*/
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public abstract class CActor : MonoBehaviour, IHotSpotMgr, CVrWand.IVrWandMoveable {		// Base class to an 'actor': a body part such as arms, legs, chest that has intelligence to interact with the environment.  Subclassed in CActorArm / CActorLeg / CActorChest, etc...

	[HideInInspector]	public	CObj			_oObj;				// Our object responsible for 'super public' properties exposed to GUI and animation system.  Each CActor-derived subclass fills this with its own properties
	//#DEV26: ###OBS? [HideInInspector]	public 	CHotSpot		_oHotSpot;			// The hotspot object that will permit user to left/right click on us in the scene to move/rotate/scale us and invoke our context-sensitive menu.
	[HideInInspector]	public 	CBodyBase		_oBodyBase;
	[HideInInspector]	public 	EBodySide		_eBodySide;							// Side of actor: Left, Right or Center
	[HideInInspector]	public 	char			_chSidePrefixL, _chSidePrefixU;     // Left side = 'l", right side = r.  Used to locate our bone structure by string name
						public	string			_sName;								// Name of actor such as "Left Arm"

	[HideInInspector]	public 	CBone 			_oBoneExtremity;					//###MOVE? This is the 'extremity bone' of the limb (hand for arm and foot for leg).  Kept here so CHarnessSphereActorWalker can orient itself to our knee / elbow for more natural hand and toe placement
	[HideInInspector]	public 	List<CBone>		_aBones = new List<CBone>();		// The array of bones defined for this actor.  Duplicated in there for easy enable/disable as we change game mode
	[HideInInspector]	public ConfigurableJoint _oJoint_ExtremityToPin;            // The joint between our 'extremity' (hand, feet, etc) and our 'pinning pin'.  Stored for faster pin/unpin.  Hinge joint for limb pins and Configurable Joint for all others
    
                        public	CActorGuiPin	_oActorGuiPin;						// The canvas pin mechanism responsible for displaying our popup GUI menu at a controlled distance from our position.  (Used to prevent menu from being clipped by body)
	//[HideInInspector]	public	float			_nDrivePinToBone = C_DrivePinToBone;            // The strength of the spring force pulling actor to its pin position
	[HideInInspector]	public	bool			_bBakeJointAnglesWhenMovingPin;		// When true every joint in this actor will 'bake' their angle as the pin is moved.  Only set for extremities such as arms and legs

//###OBS						public	const float		C_DrivePinToBone = 5000f;					//###CHECK: The default positional drive for all pins.  Hugely important! Sets _nDrivePos  ###TUNE
						public	const float		C_SizeHotSpot_BodyNodes = 1.0f;	// Relative hotspot size of the torso nodes
						//public	const float		C_SizeHotSpot_BodyNodes = 2.8f;	// Relative hotspot size of the torso nodes

						CUICanvas _oCanvas_HACK;		//###TEMP:
						Vector3 _eulRotChanged;	// To enable CObj-based rotation setting (which must break quaternions into four floats) we store rotation writes this temp quaternion with the end-result 'taking' only upon set of w.  This means that any rotation change must change w to 'take' (as it should given nature of quaternions)

	//---------------------------------------------------------------------------	CREATE / DESTROY

	public virtual void OnStart(CBodyBase oBodyBase) {
		_oBodyBase 	= oBodyBase;

		//=== Determine if we're a left, right or 'center' actor (e.g. ArmL, LegR, etc) === (NOTE: Applies to Actors, NOT bones!!)
		if (gameObject.name.EndsWith("L")) { 
			_eBodySide = EBodySide.Left;
			_chSidePrefixL = 'l';
			_sName = "Left " + gameObject.name.Substring(0, gameObject.name.Length - 1);
		} else if (gameObject.name.EndsWith("R")) { 
			_eBodySide = EBodySide.Right;		
			_chSidePrefixL = 'r';
			_sName = "Right " + gameObject.name.Substring(0, gameObject.name.Length - 1);
		} else { 
			_eBodySide = EBodySide.Center;		
			_chSidePrefixL = 'X';                   //###CHECK21: What to do??
			_sName = gameObject.name;
		}
		_chSidePrefixU = _chSidePrefixL.ToString().ToUpper()[0];

        OnStart_DefineLimb();       //#DEV26:!!!

        //#DEV26:
        if (_oObj != null && GetType() != typeof(CActorArm))      //#DEV26: Revisit where default pinning goes!
            _oObj.Set("Pinned", 1);			//###DESIGN:!!! What to pin?  Do it here??
	}

    protected virtual void PinToExtremity_ConfigureJoint(JointDrive oJointDriveSlerp) {
        oJointDriveSlerp.positionSpring = 100.0f;       // Setup quite a stiff slerp rotation drive for the default bone.  Limbs override this!
    }

    void PinToExtremity_CreateJoint() {
        if (_oBoneExtremity == null)
            CUtility.ThrowExceptionF("###EXCEPTION: CActor '{0}' called CreateJointOnExtremity() but no bone extremity is defined!", _sName);

        ConfigurableJoint oJoint_Extremity;
        //if (GetType().IsSubclassOf(typeof(CActorLimb))) {        //#DEV26: Virtual function?

        //    HingeJoint oJointHinge = _oBoneExtremity.gameObject.AddComponent<HingeJoint>();
        //    oJointHinge.anchor = new Vector3(0.012137f, 0.082438f, 0.02f);        //#DEV26: ###IMPROVE: Read from marker in hand?
        //                                                                          //_oJoint_Extremity.anchor = new Vector3(0.012137f, 0.082438f, 0.015285f);        //#DEV26:
        //    oJointHinge.autoConfigureConnectedAnchor = false;
        //    oJointHinge.connectedAnchor = Vector3.zero;
        //    oJointHinge.axis = new Vector3(0, 0, 1);
        //    oJoint_Extremity = oJointHinge;

        //} else {

            _oJoint_ExtremityToPin = _oBoneExtremity.gameObject.AddComponent<ConfigurableJoint>();
            _oJoint_ExtremityToPin.xMotion = _oJoint_ExtremityToPin.yMotion = _oJoint_ExtremityToPin.zMotion = ConfigurableJointMotion.Limited;
            _oJoint_ExtremityToPin.angularXMotion = _oJoint_ExtremityToPin.angularYMotion = _oJoint_ExtremityToPin.angularZMotion = ConfigurableJointMotion.Limited;
            JointDrive oJointDriveSlerp = new JointDrive();
            oJointDriveSlerp.positionDamper = 1.0f;                          //###TODO!!!!! ###TUNE?
            oJointDriveSlerp.maximumForce = float.MaxValue;               //###IMPROVE: Some reasonable force to prevent explosions??

            //=== Call subclass to properly configure the joint for its limb requirements ===
            PinToExtremity_ConfigureJoint(oJointDriveSlerp);

            //=== Only configure Slerp if actor sets its spring to non-zero ===
            if (oJointDriveSlerp.positionSpring != 0) {
                _oJoint_ExtremityToPin.rotationDriveMode = RotationDriveMode.Slerp;        // Slerp is really the only useful option for bone driving.  (Many other features of D6 joint!!!)
                _oJoint_ExtremityToPin.slerpDrive = oJointDriveSlerp;
            }
        //}
        
        //###DESIGN:!!! Config rigidbody mass and drag?
        //#DEV26: GetComponent<Renderer>().enabled = false;					// Hidden by default.  CGame shows/hides us with call to OnBroadcast_HideOrShowHelperObjects
        //SetActorPosToBonePos();         //###CHECK #DEV26:
    }


    public virtual void OnDestroy() {}
	
	public abstract void OnStart_DefineLimb();

    public virtual void OnActorPinned(bool bPinned) { }


	//---------------------------------------------------------------------------	LOAD / SAVE
	public void Load(BinaryReader oBR) {
        transform.localPosition = CUtility.LoadBinary_Vector3(oBR);
        transform.localRotation = CUtility.LoadBinary_QuaternionAsEuler(oBR);
		foreach (CBone oBone in _aBones)
			oBone.Load(oBR);
	}

	public void Save(BinaryWriter oBW) {
        CUtility.SaveBinary_Vector3(oBW, transform.localPosition);
        CUtility.SaveBinary_QuaternionAsEuler(oBW, transform.localRotation);
		foreach (CBone oBone in _aBones)
			oBone.Save(oBW);
	}

	public void Serialize_OBS(FileStream oStream) {
		CUtility.Serialize(oStream, transform, false);
		//_oObj.Serialize_OBS(oStream);
	}
	
	//---------------------------------------------------------------------------	UTILITY

	public void OnBroadcast_HideOrShowHelperObjects(bool bShow) {		// Send by a 'BroadcastMessageToAllBodies" to hide/show the whole subtree of helper objects.
		GetComponent<Renderer>().enabled = bShow;
	}

	public void OnChangeGameMode(EGameModes eGameModeNew, EGameModes eGameModeOld) {
		foreach (CBone oBone in _aBones)							// Propagate the change in game mode to all our joints
			oBone.OnChangeGameMode(eGameModeNew, eGameModeOld);
		//switch (eGameModeNew) {
		//	case EGameModes.Play:
		//		break;
		//	case EGameModes.Configure:
		//		break;
		//}
	}

    public void SetActorPosToBonePos() {
		if (_oBoneExtremity != null) {
            transform.position = _oBoneExtremity.transform.position;           // Reset this actor's pin to where the bone extremity is right now
            transform.rotation = _oBoneExtremity.transform.rotation;
        }
    }

    //---------------------------------------------------------------------------	COBJECT EVENTS
		 

    public void OnSet_Pinned(float nValueOld, float nValueNew) {	// Reflection call to service all the 'Pinned' properties of our derived classes.
		if (_oBoneExtremity == null)			//###CHECK
			return;

		bool bPinned = nValueNew == 1;

        if (bPinned == (_oJoint_ExtremityToPin != null))
            return;                         // Return without doing anything if we're already in the right pinning mode.

		if (bPinned) {		// How pinning init works: 1) Remember current location/rotation, 2) set to where extremity is now, 3) activate joint, 4) move back to old pos/rot = extremity's spring will gradually move & rotate toward current position of pin!  (just what we want!)
			//###IMPROVE: When user pins make pin location where limb extremity is (ie. no movement)
		    if (_oJoint_ExtremityToPin == null)
                PinToExtremity_CreateJoint();
            GetComponent<Rigidbody>().isKinematic = true;           // Make sure our pin is Kinematic!
            Vector3 vecPosOld = transform.position;
			Quaternion quatRotOld = transform.rotation;
			Transform oAnchorT = _oBoneExtremity.transform;			// Joint extremity is used as anchor unless a subnode called 'Anchor' exists (e.g. place outside hand or toes that is used to pull forces)
			Transform oAnchorChildT = oAnchorT.Find("Anchor");      //#DEV26:!!! Use??
			if (oAnchorChildT != null)
				oAnchorT = oAnchorChildT;
			transform.position = oAnchorT.position;			//? Doesn't matter as it gets calculated every frame anyway but just to be cleaner in 3D scene...
			transform.rotation = oAnchorT.rotation;
            _oJoint_ExtremityToPin.connectedBody = GetComponent<Rigidbody>();
            transform.position = vecPosOld;
			transform.rotation = quatRotOld;
		} else {
            if (_oJoint_ExtremityToPin) {
                GameObject.Destroy(_oJoint_ExtremityToPin);      //#DEV26: What to do??
                _oJoint_ExtremityToPin = null;
            }
        }
        OnActorPinned(bPinned);                                 // Notify actor that is has been pinned / unpinned.
   //     if (_oHotSpot != null)									// We can move/rotate/scale by gizmo only if we're pinned       //###OBS:??
			//_oHotSpot._bEnableEditing = bPinned;
	}
    protected void Util_SetJointExtremityToPinStrengths(float nPosSpring, float nPosSpringDamper, float nRotSpring, float nRotSpringDamper) {
        //=== Set the pin pulling power ===
        SoftJointLimitSpring oLimitSpring = _oJoint_ExtremityToPin.linearLimitSpring;
        oLimitSpring.spring = 10*nPosSpring;        //###DEV27Z: ###TEMP
        oLimitSpring.damper = nPosSpringDamper;
        _oJoint_ExtremityToPin.linearLimitSpring = oLimitSpring;

        //=== Set the pin torque power ===
        oLimitSpring = _oJoint_ExtremityToPin.angularXLimitSpring;
        oLimitSpring.spring = 0.0000001f;//nRotSpring;
        oLimitSpring.damper = nRotSpringDamper;
        _oJoint_ExtremityToPin.angularXLimitSpring  = oLimitSpring;
        _oJoint_ExtremityToPin.angularYZLimitSpring = oLimitSpring;
    }

    protected void Util_SetJointExtremityToPinStrengths_ByMode(bool bDirectDriveStrength) {
        if (bDirectDriveStrength) { 
            //=== Greatly strenghten the pulling power of the pin during manual moves ===
            Util_SetJointExtremityToPinStrengths(   CGame._oObj.Get("Pin_Pos_Spring_Direct"),
                                                    CGame._oObj.Get("Pin_Pos_Damper"),
                                                    CGame._oObj.Get("Pin_Rot_Spring_Direct_Mult") * CGame._oObj.Get("Joint_Slerp_Spring"),
                                                    CGame._oObj.Get("Pin_Rot_Damper"));
        } else { 
            //=== Greatly strenghten the pulling power of the pin during manual moves ===
            Util_SetJointExtremityToPinStrengths(   CGame._oObj.Get("Pin_Pos_Spring"),
                                                    CGame._oObj.Get("Pin_Pos_Damper"),
                                                    CGame._oObj.Get("Pin_Rot_Spring_Mult") * CGame._oObj.Get("Joint_Slerp_Spring"),
                                                    CGame._oObj.Get("Pin_Rot_Damper"));
        }
    }



	public virtual void AddBaseActorProperties() {
		//###IMPROVE19: Prop add label and prop name a pain
		_oObj.Add("Pinned", this, 0, 0, 1);
        _oObj.Add3("Pos", this);
        _oObj.Add3("Rot", this);

        //_oObj.Add(null, "PosX",				0,	-2,		2,		"", CObj.Hide);
        //_oObj.Add(null, "PosY",				0,	-2,		2,		"", CObj.Hide);		//###DESIGN!!!  Bounds???
        //_oObj.Add(null, "PosZ",				0,	-2,		2,		"", CObj.Hide);		//###DESIGN: Have a 'power edit mode' that unhides these properties (shown while pressing control for example??)
        //_oObj.Add(null, "RotX",				0,	-9999,	9999,	"", CObj.Hide);		//###BUG ###DESIGN!!!: Meaningless to export Quaternion to user... Euler instead??
        //_oObj.Add(null, "RotY",				0,	-9999,	9999,	"", CObj.Hide);		//###DESIGN: Limits of quaternions
        //_oObj.Add(null, "RotZ",				0,	-9999,	9999,	"", CObj.Hide);
    }

    void TeleportLinkedPhysxBone() {                // Optionally teleport the attached PhysX bone of the node being moved / rotated.  (This enables pose loads to immediately snap the PhysX body at the right position without jarring PhysX spring problems dragging body parts all around the scene!
        if (CGame._bBodiesAreKinematic) {
            if (_oJoint_ExtremityToPin != null && _oJoint_ExtremityToPin.connectedBody != null) {
                //_oJoint_Extremity.connectedBody.isKinematic = true;           // Done body-wide before / after pose loading
                _oJoint_ExtremityToPin.connectedBody.transform.position = transform.position;
                _oJoint_ExtremityToPin.connectedBody.transform.rotation = transform.rotation;
            }
        }
    }

    public Vector3 OnGet_Pos() { return transform.localPosition; }
    public Vector3 OnGet_Rot() { return transform.localRotation.eulerAngles; }
    public void OnSet_Pos(Vector3 vecValue) { transform.localPosition = vecValue; }
    public void OnSet_Rot(Vector3 vecValue) { transform.localRotation = Quaternion.Euler(vecValue); }


    //#DEV26: ###DESIGN: Change to triple-value properties for both position / euler rotation??  (Or both pos / rot in one with flag for disabled elements?)
    //public float OnGet_PosX() { return transform.localPosition.x; }
    //public float OnGet_PosY() { return transform.localPosition.y; }
    //public float OnGet_PosZ() { return transform.localPosition.z; }
    //public float OnGet_RotX() { return transform.localRotation.eulerAngles.x; }
    //public float OnGet_RotY() { return transform.localRotation.eulerAngles.y; }
    //public float OnGet_RotZ() { return transform.localRotation.eulerAngles.z; }

    //public void OnSet_PosX(float nValueOld, float nValueNew) { Vector3 vecPos = transform.localPosition; vecPos.x = nValueNew; transform.localPosition = vecPos; }
    //public void OnSet_PosY(float nValueOld, float nValueNew) { Vector3 vecPos = transform.localPosition; vecPos.y = nValueNew; transform.localPosition = vecPos; }
    //public void OnSet_PosZ(float nValueOld, float nValueNew) { Vector3 vecPos = transform.localPosition; vecPos.z = nValueNew; transform.localPosition = vecPos; TeleportLinkedPhysxBone(); }     //###WEAK: Only call OptionallyTeleportLinkedPhysxBone() on one for performance reason but ugly!
    //   public void OnSet_RotX(float nValueOld, float nValueNew) { _eulRotChanged.x = nValueNew; transform.localRotation = Quaternion.Euler(_eulRotChanged); /*TeleportLinkedPhysxBone();*/ }   //###OPT:!!!! Do three times?  Only commit on one of them?
    //public void OnSet_RotY(float nValueOld, float nValueNew) { _eulRotChanged.y = nValueNew; transform.localRotation = Quaternion.Euler(_eulRotChanged); /*TeleportLinkedPhysxBone();*/ }
    //public void OnSet_RotZ(float nValueOld, float nValueNew) { _eulRotChanged.z = nValueNew; transform.localRotation = Quaternion.Euler(_eulRotChanged); TeleportLinkedPhysxBone(); }


    //---------------------------------------------------------------------------	GUI DISPLAY
    public void GUI_Show() {
		if (_oActorGuiPin)
			return;
		_oActorGuiPin = CActorGuiPin.Create(this);
		_oActorGuiPin.GUI_Show();
	}

	public void GUI_Hide() {
		if (_oActorGuiPin)
			_oActorGuiPin.GUI_Hide();
	}


	//---------------------------------------------------------------------------	MISC EVENTS

	public void OnVrAction() {              // User has 'used' this item via a VR wand.  Invoke our context menu
		if (_oCanvas_HACK == null) {							//###DESIGN:!!! Can each actor own its own canvas?  ###TODO:!!!!! Destroy!!!
			_oCanvas_HACK = CUICanvas.Create(transform);
			_oCanvas_HACK.transform.SetParent(transform);
			float nRatioToCam = 0.7f;		//###TUNE
			_oCanvas_HACK.transform.position = (transform.position * nRatioToCam + Camera.main.transform.position * (1-nRatioToCam));
			_oCanvas_HACK.transform.localRotation = Quaternion.identity;
			_oCanvas_HACK.transform.rotation = Camera.main.transform.rotation;
			//###DESIGN:!!!! Figure out a better way to guarantee popup panel without occlusion from 3D objects without hacks making it closer to camera.  e.g. use raycast to find first collider
			// then... find way to interact with widgets! (through wand or pointer beam?  see samples!)
			//###DESIGN:!!! Use VR simple pointer code for raycast example with pointer!
		}
		_oCanvas_HACK.CreatePanel(gameObject.name, _oObj);  //###CHECK: Invoke through hotspot??
		Debug.LogError("OnVrAction!");
	}

	//---------------------------------------------------------------------------	HOTSPOT EVENTS
	public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {
		//_oBody.SelectBody();			// Manipulating a body's hotspot automatically selects this body.	###CHECK: Hotspot triggers throw this off??
		//#DEV26:BROKEN!
		//if (eHotSpotEvent == EHotSpotEvent.ContextMenu)
		//	_oBodyBase.FindClosestCanvas().CreatePanel("Actor", _oObj);
	}

	public virtual void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) {
		//#DEV26:BROKEN!! _oBodyBase.SelectBody();			// Manipulating a body's hotspot automatically selects this body.

		switch (eEditMode) {
			case EEditMode.Move:
				Vector3 vecPosG = oGizmo.transform.position;		//###NOTE: Upon hotspot movement we have to set not only our transform properties but CObj as well.
				Vector3 vecPosL;
				if (transform.name == "Base")				// If the base actor is moving we set the global position otherwise we move the child actor relatve to the base actor  ###IMPROVE: Test by type isntead of string!
					vecPosL = vecPosG;
				else
					vecPosL = transform.parent.worldToLocalMatrix.MultiplyPoint(vecPosG);		// Convert the global hotspot position to be relative to the actor's parent (Usually 'Base' or 'Torso')
				_oObj.Set("PosX", vecPosL.x);		//###NOTE: Setting EActorNode for all actors (all have same enum index for same pin properties)
				_oObj.Set("PosY", vecPosL.y);
				_oObj.Set("PosZ", vecPosL.z);
				transform.localPosition = vecPosL;
				break;
			case EEditMode.Rotate:
				Quaternion quatRotG = oGizmo.transform.rotation;			//###DESIGN???: Problem with rotation?
				transform.rotation = quatRotG;		//###WEAK!!  ###OPT!  Some work duplication with Set below setting our transform!
				Vector3 eulRotL = transform.localRotation.eulerAngles;
				_oObj.Set("RotX", eulRotL.x);
				_oObj.Set("RotY", eulRotL.y);
				_oObj.Set("RotZ", eulRotL.z);
				break;
		}
	}

	public virtual void OnUpdate() {       //###DESIGN: Rename?
		//if (GetComponent<MeshRenderer>() != null)	
		//	GetComponent<MeshRenderer>().enabled = true;		//###DESIGN: Temp to circumvent VRTK hiding our pins!
	}

	public void BakeCurrentBoneRotationIntoJointRotation() {
		foreach (CBone oBone in _aBones)
			if (oBone._oJointD6 != null)
				oBone.BakeCurrentBoneRotationIntoJointRotation();
		Debug.LogWarningFormat("BakeCurrentBoneRotationIntoJointRotation bake all joints on actor '{0}'", _sName);
	}



    //---------------------------------------------------------------------------	VR WAND MOVEMENT
    public virtual Transform VrWandMove_Begin(CVrWand oVrWand, bool bStartAction1, bool bStartAction2) {
        _oObj.Set("Pinned", 0);
        _oObj.Set("Pinned", 1);          //#DEV26: ###KEEP??
        return transform;
    }
    public virtual void VrWandMove_End(CVrWand oVrWand) { }
    public virtual void VrWandMove_Update(CVrWand oVrWand) { }
    public virtual void VrWandMove_UpdatePositionAndRotation(Transform oNodeT) {
        transform.position = oNodeT.position;
        transform.rotation = oNodeT.rotation;
    }
}

public enum EBodySide {
	Center,
	Left,
	Right,
}

//public enum EActorAnchor_OBS {
//	HandPalmCenter,
//	MidFingerMidway,
//	MidFingerTip
//};



//	public virtual void OnUpdate() {		//###DESIGN: Anything useful here?

//		Vector3 vecLoc = transform.position;			// Ensure our pin position doesn't get invalid (like y < GroundLevel = underground!)
//		if (vecLoc.y < _oBody.GroundLevel) {
//			vecLoc.y = _oBody.GroundLevel;
//			transform.position = vecLoc;
//		}

//		//if (_ActorWalker != ActorWalker) {		//###OBS!
//		//	_ActorWalker = ActorWalker;
//		//	ConnectToWalker(ActorWalker);
//		//}
//		if (_ActorAnchor != ActorAnchor) 				//###BUG: Won't have any influence if not applied *before* ConnectToWalker()
//			_ActorAnchor = ActorAnchor;

//		if (CUtility.CheckIfChanged(PinActive, ref _PinActive, "PinActive"))
//			PinOrUnpin(PinActive);

////		if (_oWalker != null)
////			_oWalker.OnSimulatePre_Walker(_eBodySide);
//	}


//public Transform FetchActorAnchor_OBS(EActorAnchor_OBS eActorAnchor) {		//###TODO: Add more anchors!
//	Transform oNodeAnchor = null;
//	switch (eActorAnchor) {
//		case EActorAnchor_OBS.HandPalmCenter:	oNodeAnchor = _oBoneExtremity._oTransform.FindChild("Anchor-HandPalmCenter");					break;
//		case EActorAnchor_OBS.MidFingerMidway:	oNodeAnchor = _oBoneExtremity._oTransform.FindChild(_sSidePrefixL+"Mid1/"+_sSidePrefixL+"Mid2/Anchor-MidFingerMidway");	break;
//		case EActorAnchor_OBS.MidFingerTip:	 	oNodeAnchor = _oBoneExtremity._oTransform.FindChild(_sSidePrefixL+"Mid1/"+_sSidePrefixL+"Mid2/"+_sSidePrefixL+"Mid3/Anchor-MidFingerTip");	break;
//	}
//	if (oNodeAnchor == null)
//		Debug.LogWarning("CActor.FetchActorAnchor_OBS() could not find actor anchor " + eActorAnchor);	
//	return oNodeAnchor;
//}






//public void OnSet_PosX(float nValueOld, float nValueNew) { Vector3 vecPos = transform.position; vecPos.x = nValueNew; transform.position = vecPos; }
//public void OnSet_PosY(float nValueOld, float nValueNew) { Vector3 vecPos = transform.position; vecPos.y = nValueNew; transform.position = vecPos; }
//public void OnSet_PosZ(float nValueOld, float nValueNew) { Vector3 vecPos = transform.position; vecPos.z = nValueNew; transform.position = vecPos; }
//public void OnSet_RotX(float nValueOld, float nValueNew) { Quaternion quatRot = transform.rotation; quatRot.x = nValueNew; transform.rotation = quatRot; }
//public void OnSet_RotY(float nValueOld, float nValueNew) { Quaternion quatRot = transform.rotation; quatRot.y = nValueNew; transform.rotation = quatRot; }
//public void OnSet_RotZ(float nValueOld, float nValueNew) { Quaternion quatRot = transform.rotation; quatRot.z = nValueNew; transform.rotation = quatRot; }
//public void OnSet_RotW(float nValueOld, float nValueNew) { Quaternion quatRot = transform.rotation; quatRot.w = nValueNew; transform.rotation = quatRot; }



////---------------------------------------------------------------------------	HOTSPOT EVENTS

//public virtual void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) {
//	_oBody.SelectBody();			// Manipulating a body's hotspot automatically selects this body.

//	switch (eEditMode) {
//		case EEditMode.Move:	
//			Vector3 vecPos = oGizmo.transform.position;		//###NOTE: Upon hotspot movement we have to set not only our transform properties but CObj as well.
//			_oObj.Set(0, EActorNode.PosX, vecPos.x);		//###WEAK: Setting EActorNode for all actors (all have same index for same pin properties)
//			_oObj.Set(0, EActorNode.PosY, vecPos.y);		
//			_oObj.Set(0, EActorNode.PosZ, vecPos.z);
//			transform.position = vecPos;
//			break;
//		case EEditMode.Rotate:
//			Quaternion quatRot = oGizmo.transform.rotation;
//			_oObj.Set(0, EActorNode.RotX, quatRot.x);
//			_oObj.Set(0, EActorNode.RotY, quatRot.y);
//			_oObj.Set(0, EActorNode.RotZ, quatRot.z);
//			_oObj.Set(0, EActorNode.RotW, quatRot.w);
//			transform.rotation = quatRot;		//###DESIGN!!!! Huge work duplication with Set below setting our transform!
//			break;
//	}
//}


//Vector3 vecEulerG = oGizmo.transform.rotation.eulerAngles;
//transform.rotation = Quaternion.Euler(vecEulerG);	//###CHECK!!!!!!: Proper way to convert global rotation to local?  ###CHECK: Need to do different for base??
//Vector3 vecEulerL = transform.localRotation.eulerAngles;	//###BUG!!!!! Problem with CActor and rotation. Euler conversion can't take all angles!!
//_oObj.Find(0, EActorNode.RotX)._nValue = vecEulerL.x;		//###HACK!!!!!
//_oObj.Find(0, EActorNode.RotY)._nValue = vecEulerL.y;
//_oObj.Find(0, EActorNode.RotZ)._nValue = vecEulerL.z;



//=== Create the configurable joint component and set it with values appropriate for a pin ===
//if (GetType() == typeof(CActorArm)) {      //###HACK22:!! Keep this check?  Check differently? (e.g. if has an extremity?)
//    _oJoint_Extremity = CUtility.FindOrCreateComponent<HingeJoint>(_oBoneExtremity.gameObject);
//    _oJoint_Extremity.anchor = new Vector3(0.012137f, 0.082438f, 0.02f);        //#DEV26: ###IMPROVE: Read from marker in hand?
//    //_oJoint_Extremity.anchor = new Vector3(0.012137f, 0.082438f, 0.015285f);        //#DEV26:
//    _oJoint_Extremity.autoConfigureConnectedAnchor = false;
//    _oJoint_Extremity.connectedAnchor = Vector3.zero;
//    _oJoint_Extremity.axis = new Vector3(0, 0, 1);
//    CUtility.FindOrCreateComponent<CBone>(gameObject);          //#DEV26: ###HACK:!!  Need to add a CBone so wand can move us!!
//    //GetComponent<Rigidbody>().isKinematic = true;
//} else if (GetType() != typeof(CActorGenitals)) {      //###HACK22:!! Keep this check?  Check differently? (e.g. if has an extremity?)
//    _oJoint_Extremity = CUtility.FindOrCreateComponent<HingeJoint>(gameObject);
//    //GetComponent<Rigidbody>().isKinematic = true;
//}
