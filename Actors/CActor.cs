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

- Huge hack with CProp name!!!!!
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
 * Do we support variable-lenght penis?  (e.g. backing up chest pins on longer penis possible, but can't change heigh if penis pointing upward/downward!)
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

public abstract class CActor : MonoBehaviour, IHotSpotMgr {		// Base class to an 'actor': a body part such as arms, legs, chest that has intelligence to interact with the environment.  Subclassed in CActorArm / CActorLeg / CActorChest, etc...

	[HideInInspector]	public	CObject			_oObj;				// Our object responsible for 'super public' properties exposed to GUI and animation system.  Each CActor-derived subclass fills this with its own properties
	[HideInInspector]	public 	CHotSpot		_oHotSpot;			// The hotspot object that will permit user to left/right click on us in the scene to move/rotate/scale us and invoke our context-sensitive menu.
	[HideInInspector]	public 	CBody			_oBody;
	[HideInInspector]	public 	EBodySide		_eBodySide;							// Side of actor: Left, Right or Center
	[HideInInspector]	public 	char			_chSidePrefixL, _chSidePrefixU;		// Left side = 'l", right side = r.  Used to locate our bone structure by string name

	[HideInInspector]	public 	CBone 			_oBoneExtremity;					//###MOVE? This is the 'extremity bone' of the limb (hand for arm and foot for leg).  Kept here so CHarnessSphereActorWalker can orient itself to our knee / elbow for more natural hand and toe placement
	[HideInInspector]	public 	List<CBone>		_aBones = new List<CBone>();		// The array of bones defined for this actor.  Duplicated in there for easy enable/disable as we change game mode
	[HideInInspector]	public 	ConfigurableJoint _oConfJoint_Extremity;			// The joint of our extremity.  Stored for faster pin/unpin
	
	[HideInInspector]	public	float			_nDrivePinToBone = C_DrivePinToBone;			// The strength of the spring force pulling actor to its pin position

						public	const float		C_DrivePinToBone = 5000f;					//###DEV22 The default positional drive for all pins.  Hugely important! Sets _nDrivePos  ###TUNE
						public	const float		C_SizeHotSpot_BodyNodes = 1.0f;	// Relative hotspot size of the torso nodes
						//public	const float		C_SizeHotSpot_BodyNodes = 2.8f;	// Relative hotspot size of the torso nodes

						CUICanvas _oCanvas_HACK;		//###DEV22
						Quaternion _quatRotChanged;	// To enable CProp-based rotation setting (which must break quaternions into four floats) we store rotation writes this temp quaternion with the end-result 'taking' only upon set of w.  This means that any rotation change must change w to 'take' (as it should given nature of quaternions)

	//---------------------------------------------------------------------------	CREATE / DESTROY

	public virtual void OnStart(CBody oBody) {
		_oBody 	= oBody;

		//=== Determine if we're a left, right or 'center' actor (e.g. ArmL, LegR, etc) === (NOTE: Applies to Actors, NOT bones!!)
		if (gameObject.name.EndsWith("L")) { 
			_eBodySide = EBodySide.Left;
			_chSidePrefixL = 'l';
		} else if (gameObject.name.EndsWith("R")) { 
			_eBodySide = EBodySide.Right;		
			_chSidePrefixL = 'r';
		} else { 
			_eBodySide = EBodySide.Center;		
			_chSidePrefixL = 'X';					//###CHECK21: What to do??
		}
		_chSidePrefixU = _chSidePrefixL.ToString().ToUpper()[0];

		//=== Create the configurable joint component and set it with values appropriate for a pin ===
		if (_oConfJoint_Extremity != null) {		//###CHECK22:!! This check ok?  Keep?
		//if (GetType() != typeof(CActorGenitals)) {		//###HACK22:!! Keep this check?  Check differently? (e.g. if has an extremity?)
			_oConfJoint_Extremity = CUtility.FindOrCreateComponent(gameObject, typeof(ConfigurableJoint)) as ConfigurableJoint;
			_oConfJoint_Extremity.xMotion = _oConfJoint_Extremity.yMotion = _oConfJoint_Extremity.zMotion = ConfigurableJointMotion.Limited;
			_oConfJoint_Extremity.angularXMotion = _oConfJoint_Extremity.angularYMotion = _oConfJoint_Extremity.angularZMotion = ConfigurableJointMotion.Limited;

			JointDrive oDrive = new JointDrive();
			oDrive.positionSpring = 1000.0f;			//###DEV22:!!!!  ###TUNE!!!!!!!!!!
			if (GetType() == typeof(CActorLeg)) { 
				oDrive.positionSpring *= 100.0f;			//###DEV22: Make spring stiffness for leg pins much stiffer!
				_oConfJoint_Extremity.angularXMotion = _oConfJoint_Extremity.angularYMotion = _oConfJoint_Extremity.angularZMotion = ConfigurableJointMotion.Free;		//###DEV22: Feet pins with no rotation limits??
			}
			oDrive.positionDamper = 0;                          //###TODO!!!!! ###TUNE?
			oDrive.maximumForce = float.MaxValue;               //###IMPROVE: Some reasonable force to prevent explosions??
			_oConfJoint_Extremity.rotationDriveMode = RotationDriveMode.Slerp;        // Slerp is really the only useful option for bone driving.  (Many other features of D6 joint!!!)
			_oConfJoint_Extremity.slerpDrive = oDrive;

			//###DEV22:!!! Config rigidbody mass and drag?
		}

		GetComponent<Renderer>().enabled = false;					// Hidden by default.  CGame shows/hides us with call to OnBroadcast_HideOrShowHelperObjects
		
		OnStart_DefineLimb();

		_oObj.PropSet(0, EActorNode.Pinned, 1);			//###DEV22: Pin everything??
		//if (GetType() != typeof(CActorArm) && GetType() != typeof(CActorLeg))
		//	_oObj.PropSet(0, EActorNode.Pinned, 1);
		//if (GetType() == typeof(CActorArm) || GetType() == typeof(CActorLeg))
		//	_oObj.PropSet(0, EActorNode.Pinned, 1);

        SetActorPosToBonePos();         //###CHECK
	}

	public virtual void OnDestroy() {}
	
	public abstract void OnStart_DefineLimb();


	//---------------------------------------------------------------------------	LOAD / SAVE

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

    public void OnPropSet_Pinned(float nValueOld, float nValueNew) {	// Reflection call to service all the 'Pinned' properties of our derived classes.
		if (_oBoneExtremity == null)			//###CHECK
			return;

		if (_oConfJoint_Extremity == null)
			CUtility.ThrowException("*Err: CActor.Pinned() called with no extremity joint set!");

		bool bPinned = nValueNew == 1;
        //if (CGame.INSTANCE.BoneDebugMode && GetType() != typeof(CActorChest))           // Disable pinning in bone debug mode on extremities so we can test bone orientation
        //    bPinned = false;
		if (bPinned) {		// How pinning init works: 1) Remember current location/rotation, 2) set to where extremity is now, 3) activate joint, 4) move back to old pos/rot = extremity's spring will gradually move & rotate toward current position of pin!  (just what we want!)
			//###IMPROVE: When user pins make pin location where limb extremity is (ie. no movement)
			Vector3 vecPosOld = transform.position;
			Quaternion quatRotOld = transform.rotation;
			Transform oAnchorT = _oBoneExtremity.transform;			// Joint extremity is used as anchor unless a subnode called 'Anchor' exists (e.g. place outside hand or toes that is used to pull forces)
			Transform oAnchorChildT = oAnchorT.FindChild("Anchor");
			if (oAnchorChildT != null)
				oAnchorT = oAnchorChildT;
			transform.position = oAnchorT.position;			//? Doesn't matter as it gets calculated every frame anyway but just to be cleaner in 3D scene...
			transform.rotation = oAnchorT.rotation;			
			_oConfJoint_Extremity.connectedBody = _oBoneExtremity._oRigidBody;
			SoftJointLimit oJointLimit = new SoftJointLimit();                  //###WEAK: Do everytime?? ###TUNE?
            oJointLimit.limit = 0.001f;				//###LEARN: If this is zero there is NO spring functionality!!
			SoftJointLimitSpring oJointLimitSpring = new SoftJointLimitSpring();    //###WEAK: Do everytime??		//####MOD: To Unity5
            oJointLimitSpring.spring = _nDrivePinToBone;
			_oConfJoint_Extremity.linearLimit = oJointLimit;
			_oConfJoint_Extremity.linearLimitSpring = oJointLimitSpring;
			transform.position = vecPosOld;
			transform.rotation = quatRotOld;
		} else {
			_oConfJoint_Extremity.connectedBody = null;			// Destroy the joint and the actor's limb will float in space as drive by various limb joints.
		}
		if (_oHotSpot != null)									// We can move/rotate/scale by gizmo only if we're pinned
			_oHotSpot._bEnableEditing = bPinned;
	}

	public void AddBaseActorProperties() {
		//###IMPROVE19: Prop add label and prop name a pain
		_oObj.PropAdd(0, EActorNode.Pinned,				"Pinned",			"Pinned",	0,	0,		1,		"", CProp.AsCheckbox);
		_oObj.PropAdd(0, EActorNode.PosX,				"PosX",				"PosX",		0,	-2,		2,		"", CProp.Hide);
		_oObj.PropAdd(0, EActorNode.PosY,				"PosY",				"PosY",		0,	-2,		2,		"", CProp.Hide);		//###DESIGN!!!  Bounds???
		_oObj.PropAdd(0, EActorNode.PosZ,				"PosZ",				"PosZ",		0,	-2,		2,		"", CProp.Hide);		//###DESIGN: Have a 'power edit mode' that unhides these properties (shown while pressing control for example??)
		_oObj.PropAdd(0, EActorNode.RotX,				"RotX",				"RotX",		0,	-9999,	9999,	"", CProp.Hide);		//###BUG ###DESIGN!!!: Meaningless to export Quaternion to user... Euler instead??
		_oObj.PropAdd(0, EActorNode.RotY,				"RotY",				"RotY",		0,	-9999,	9999,	"", CProp.Hide);		//###DESIGN: Limits of quaternions
		_oObj.PropAdd(0, EActorNode.RotZ,				"RotZ",				"RotZ",		0,	-9999,	9999,	"", CProp.Hide);
		_oObj.PropAdd(0, EActorNode.RotW,				"RotW",				"RotW",		1,	-9999,	9999,	"", CProp.Hide);
	}

    void TeleportLinkedPhysxBone() {                // Optionally teleport the attached PhysX bone of the node being moved / rotated.  (This enables pose loads to immediately snap the PhysX body at the right position without jarring PhysX spring problems dragging body parts all around the scene!
        if (CGame.INSTANCE._bBodiesAreKinematic) {
            if (_oConfJoint_Extremity != null && _oConfJoint_Extremity.connectedBody != null) {
                //_oConfJoint_Extremity.connectedBody.isKinematic = true;           // Done body-wide before / after pose loading
                _oConfJoint_Extremity.connectedBody.transform.position = transform.position;
                _oConfJoint_Extremity.connectedBody.transform.rotation = transform.rotation;
            }
        }
    }

	public void OnPropSet_PosX(float nValueOld, float nValueNew) { Vector3 vecPos = transform.localPosition; vecPos.x = nValueNew; transform.localPosition = vecPos; }
	public void OnPropSet_PosY(float nValueOld, float nValueNew) { Vector3 vecPos = transform.localPosition; vecPos.y = nValueNew; transform.localPosition = vecPos; }
	public void OnPropSet_PosZ(float nValueOld, float nValueNew) { Vector3 vecPos = transform.localPosition; vecPos.z = nValueNew; transform.localPosition = vecPos; TeleportLinkedPhysxBone(); }     //###WEAK: Only call OptionallyTeleportLinkedPhysxBone() on one for performance reason but ugly!
    public void OnPropSet_RotX(float nValueOld, float nValueNew) { _quatRotChanged.x = nValueNew; }		//###HACK!!!!: To enable setting of a quaternion from orthogonal 4 properties we store properties in x,y,z and only really set result when w is set
	public void OnPropSet_RotY(float nValueOld, float nValueNew) { _quatRotChanged.y = nValueNew; }		//###NOW### Do we really export quats?  Better with euler because of w?? ###SOON!!!
	public void OnPropSet_RotZ(float nValueOld, float nValueNew) { _quatRotChanged.z = nValueNew; }
	public void OnPropSet_RotW(float nValueOld, float nValueNew) { _quatRotChanged.w = nValueNew; transform.localRotation = _quatRotChanged; TeleportLinkedPhysxBone(); }     //###WEAK: Only call OptionallyTeleportLinkedPhysxBone() on one for performance reason but ugly!
	//public void OnPropSet_RotX(float nValueOld, float nValueNew) { Vector3 vecEuler = transform.localRotation.eulerAngles; vecEuler.x = nValueNew; transform.localRotation = Quaternion.Euler(vecEuler); }
	//public void OnPropSet_RotY(float nValueOld, float nValueNew) { Vector3 vecEuler = transform.localRotation.eulerAngles; vecEuler.y = nValueNew; transform.localRotation = Quaternion.Euler(vecEuler); }
	//public void OnPropSet_RotZ(float nValueOld, float nValueNew) { Vector3 vecEuler = transform.localRotation.eulerAngles; vecEuler.z = nValueNew; transform.localRotation = Quaternion.Euler(vecEuler); }


	//---------------------------------------------------------------------------	MISC EVENTS

	public void OnVrAction() {              // User has 'used' this item via a VR wand.  Invoke our context menu	//'
		if (_oCanvas_HACK == null) {							//###DEV22: Can each actor own its own canvas?  ###TODO:!!!!! Destroy!!!
			_oCanvas_HACK = CUICanvas.Create(transform);
			_oCanvas_HACK.transform.SetParent(transform);
			float nRatioToCam = 0.7f;		//###TUNE
			_oCanvas_HACK.transform.position = (transform.position * nRatioToCam + Camera.main.transform.position * (1-nRatioToCam));
			_oCanvas_HACK.transform.localRotation = Quaternion.identity;
			_oCanvas_HACK.transform.rotation = Camera.main.transform.rotation;
			//###DEV22:!!!! Figure out a better way to guarantee popup panel without occlusion from 3D objects without hacks making it closer to camera.  e.g. use raycast to find first collider
			// then... find way to interact with widgets! (through wand or pointer beam?  see samples!)
			//###IDEA22:!!! Use VR simple pointer code for raycast example with pointer!
		}
		CUtility.WndPopup_Create(_oCanvas_HACK, EWndPopupType.PropertyEditor, new CObject[] { _oObj }, gameObject.name);	//###CHECK: Invoke through hotspot??
		//_oHotSpot.WndPopup_Create(_oCanvas_HACK, new CObject[] { _oObj });
	}

	//---------------------------------------------------------------------------	HOTSPOT EVENTS


	public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {
		//_oBody.SelectBody();			// Manipulating a body's hotspot automatically selects this body.	###CHECK: Hotspot triggers throw this off??
		if (eHotSpotEvent == EHotSpotEvent.ContextMenu)
			_oHotSpot.WndPopup_Create(_oBody.FindClosestCanvas(), new CObject[] { _oObj });
	}

	public virtual void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) {
		_oBody.SelectBody();			// Manipulating a body's hotspot automatically selects this body.

		switch (eEditMode) {
			case EEditMode.Move:
				Vector3 vecPosG = oGizmo.transform.position;		//###NOTE: Upon hotspot movement we have to set not only our transform properties but CProp as well.
				Vector3 vecPosL;
				if (transform.name == "Base")				// If the base actor is moving we set the global position otherwise we move the child actor relatve to the base actor  ###IMPROVE: Test by type isntead of string!
					vecPosL = vecPosG;
				else
					vecPosL = transform.parent.worldToLocalMatrix.MultiplyPoint(vecPosG);		// Convert the global hotspot position to be relative to the actor's parent (Usually 'Base' or 'Torso')
				_oObj.PropSet(0, EActorNode.PosX, vecPosL.x);		//###NOTE: Setting EActorNode for all actors (all have same enum index for same pin properties)
				_oObj.PropSet(0, EActorNode.PosY, vecPosL.y);
				_oObj.PropSet(0, EActorNode.PosZ, vecPosL.z);
				transform.localPosition = vecPosL;
				break;
			case EEditMode.Rotate:
				Quaternion quatRotG = oGizmo.transform.rotation;			//###DESIGN???: Problem with rotation?
				transform.rotation = quatRotG;		//###WEAK!!  ###OPT!  Some work duplication with PropSet below setting our transform!
				Quaternion quatRotL = transform.localRotation;
				_oObj.PropSet(0, EActorNode.RotX, quatRotL.x);
				_oObj.PropSet(0, EActorNode.RotY, quatRotL.y);
				_oObj.PropSet(0, EActorNode.RotZ, quatRotL.z);
				_oObj.PropSet(0, EActorNode.RotW, quatRotL.w);
				break;
		}
	}

	public virtual void OnSimulatePre() {       //###DEV22: Rename?
		//if (GetComponent<MeshRenderer>() != null)	
		//	GetComponent<MeshRenderer>().enabled = true;		//###DEV22: Temp to circumvent VRTK hiding our pins!
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



//	public virtual void OnSimulatePre() {		//###DESIGN: Anything useful here?

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






	//public void OnPropSet_PosX(float nValueOld, float nValueNew) { Vector3 vecPos = transform.position; vecPos.x = nValueNew; transform.position = vecPos; }
	//public void OnPropSet_PosY(float nValueOld, float nValueNew) { Vector3 vecPos = transform.position; vecPos.y = nValueNew; transform.position = vecPos; }
	//public void OnPropSet_PosZ(float nValueOld, float nValueNew) { Vector3 vecPos = transform.position; vecPos.z = nValueNew; transform.position = vecPos; }
	//public void OnPropSet_RotX(float nValueOld, float nValueNew) { Quaternion quatRot = transform.rotation; quatRot.x = nValueNew; transform.rotation = quatRot; }
	//public void OnPropSet_RotY(float nValueOld, float nValueNew) { Quaternion quatRot = transform.rotation; quatRot.y = nValueNew; transform.rotation = quatRot; }
	//public void OnPropSet_RotZ(float nValueOld, float nValueNew) { Quaternion quatRot = transform.rotation; quatRot.z = nValueNew; transform.rotation = quatRot; }
	//public void OnPropSet_RotW(float nValueOld, float nValueNew) { Quaternion quatRot = transform.rotation; quatRot.w = nValueNew; transform.rotation = quatRot; }



	////---------------------------------------------------------------------------	HOTSPOT EVENTS

	//public virtual void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) {
	//	_oBody.SelectBody();			// Manipulating a body's hotspot automatically selects this body.

	//	switch (eEditMode) {
	//		case EEditMode.Move:	
	//			Vector3 vecPos = oGizmo.transform.position;		//###NOTE: Upon hotspot movement we have to set not only our transform properties but CProp as well.
	//			_oObj.PropSet(0, EActorNode.PosX, vecPos.x);		//###WEAK: Setting EActorNode for all actors (all have same index for same pin properties)
	//			_oObj.PropSet(0, EActorNode.PosY, vecPos.y);		
	//			_oObj.PropSet(0, EActorNode.PosZ, vecPos.z);
	//			transform.position = vecPos;
	//			break;
	//		case EEditMode.Rotate:
	//			Quaternion quatRot = oGizmo.transform.rotation;
	//			_oObj.PropSet(0, EActorNode.RotX, quatRot.x);
	//			_oObj.PropSet(0, EActorNode.RotY, quatRot.y);
	//			_oObj.PropSet(0, EActorNode.RotZ, quatRot.z);
	//			_oObj.PropSet(0, EActorNode.RotW, quatRot.w);
	//			transform.rotation = quatRot;		//###DESIGN!!!! Huge work duplication with PropSet below setting our transform!
	//			break;
	//	}
	//}


//Vector3 vecEulerG = oGizmo.transform.rotation.eulerAngles;
//transform.rotation = Quaternion.Euler(vecEulerG);	//###CHECK!!!!!!: Proper way to convert global rotation to local?  ###CHECK: Need to do different for base??
//Vector3 vecEulerL = transform.localRotation.eulerAngles;	//###BUG!!!!! Problem with CActor and rotation. Euler conversion can't take all angles!!
//_oObj.PropFind(0, EActorNode.RotX)._nValueLocal = vecEulerL.x;		//###HACK!!!!!
//_oObj.PropFind(0, EActorNode.RotY)._nValueLocal = vecEulerL.y;
//_oObj.PropFind(0, EActorNode.RotZ)._nValueLocal = vecEulerL.z;
