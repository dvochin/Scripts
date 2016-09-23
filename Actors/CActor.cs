/*###DISCUSSION: ACTORS
=== NEW BODY ===  ###NOW###
- Poses all corrupt now.  Throw away??    (Load disabled)

- WTF wrong with pins now?  Inverted?  Back??  Why at second time?  Reset does something bad?  Pins should be dist only??
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

public abstract class CActor : MonoBehaviour, IObject, IHotSpotMgr {		// Base class to an 'actor': a body part such as arms, legs, chest that has intelligence to interact with the environment.  Subclassed in CActorArm / CActorLeg / CActorChest, etc...

	[HideInInspector]	public	CObject			_oObj;				// Our object responsible for 'super public' properties exposed to GUI and animation system.  Each CActor-derived subclass fills this with its own properties
	[HideInInspector]	public 	CHotSpot		_oHotSpot;			// The hotspot object that will permit user to left/right click on us in the scene to move/rotate/scale us and invoke our context-sensitive menu.
	[HideInInspector]	public 	CBody			_oBody;
	[HideInInspector]	public 	EBodySide		_eBodySide;								// Left side =  0 , right side =  1
	[HideInInspector]	public 	string 			_sSidePrefixL, _sSidePrefixU;		// Left side = 'l", right side = "r".  Used to locate our bone structure by string name

	[HideInInspector]	public 	CJointDriver 	_oJointExtremity;					//###MOVE? This is the 'extremity point' of the limb (hand for arm and foot for leg).  Kept here so CHarnessSphereActorWalker can orient itself to our knee / elbow for more natural hand and toe placement
	[HideInInspector]	public 	List<CJointDriver> _aJoints = new List<CJointDriver>();		// The array of joints defined for this actor.  Duplicated in there for easy enable/disable as we change game mode
	[HideInInspector]	public 	ConfigurableJoint _oConfJoint_Extremity;			// The joint of our extremity.  Stored for faster pin/unpin
	
	[HideInInspector]	public	float			_nDrivePos = C_DrivePos;			// The strength of the spring force pulling actor to its pin position

						public	const float		C_DrivePos = 200f;					// The default positional drive for all pins.  Hugely important! Sets _nDrivePos  ###TUNE
						public	const float		C_SizeHotSpot_BodyNodes = 1.0f;	// Relative hotspot size of the torso nodes
						//public	const float		C_SizeHotSpot_BodyNodes = 2.8f;	// Relative hotspot size of the torso nodes

						Quaternion _quatRotChanged;	// To enable CProp-based rotation setting (which must break quaternions into four floats) we store rotation writes this temp quaternion with the end-result 'taking' only upon set of w.  This means that any rotation change must change w to 'take' (as it should given nature of quaternions)

	//---------------------------------------------------------------------------	CREATE / DESTROY

	public virtual void OnStart(CBody oBody) {
		_oBody 	= oBody;

		_eBodySide = gameObject.name.EndsWith("R") ? EBodySide.Right : EBodySide.Left;		// Extract what side we are from our node name (e.g. ArmL, LegR, etc)
		_sSidePrefixL = (_eBodySide == 0) ? "l" : "r";
		_sSidePrefixU = _sSidePrefixL.ToUpper();
		_oConfJoint_Extremity = GetComponent<ConfigurableJoint>();

		GetComponent<Renderer>().enabled = false;					// Hidden by default.  CGame shows/hides us with call to OnBroadcast_HideOrShowHelperObjects
		
		OnStart_DefineLimb();

        _oObj.PropSet(EActorNode.Pinned, 1);            //###NOW### Pin by default??

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
		foreach (CJointDriver oJoint in _aJoints)							// Propagate the change in game mode to all our joints
			oJoint.OnChangeGameMode(eGameModeNew, eGameModeOld);
		//switch (eGameModeNew) {
		//	case EGameModes.Play:
		//		break;
		//	case EGameModes.Configure:
		//		break;
		//}
	}

    public void SetActorPosToBonePos() {
		if (_oJointExtremity != null) {
            transform.position = _oJointExtremity.transform.position;           // Reset this actor's pin to where the bone extremity is right now
            transform.rotation = _oJointExtremity.transform.rotation;
        }
    }

    //---------------------------------------------------------------------------	COBJECT EVENTS

    public void OnPropSet_Pinned(float nValueOld, float nValueNew) {	// Reflection call to service all the 'Pinned' properties of our derived classes.
		if (_oJointExtremity == null)			//###CHECK
			return;

		if (_oConfJoint_Extremity == null)
			throw new CException("*Err: CActor.PinOrUnpin() called with no extremity joint set!");

		bool bPinned = nValueNew == 1;
        //if (GetType() != typeof(CActorChest))           //###NOW###
        //    bPinned = false;
		if (bPinned) {		// How pinning init works: 1) Remember current location/rotation, 2) set to where extremity is now, 3) activate joint, 4) move back to old pos/rot = extremity's spring will gradually move & rotate toward current position of pin!  (just what we want!)
			//###IMPROVE: When user pins make pin location where limb extremity is (ie. no movement)
			Vector3 vecPosOld = transform.position;
			Quaternion quatRotOld = transform.rotation;
			Transform oAnchorT = _oJointExtremity.transform;			// Joint extremity is used as anchor unless a subnode called 'Anchor' exists (e.g. place outside hand or toes that is used to pull forces)
			Transform oAnchorChildT = oAnchorT.FindChild("Anchor");
			if (oAnchorChildT != null)
				oAnchorT = oAnchorChildT;
			transform.position = oAnchorT.position;			//? Doesn't matter as it gets calculated every frame anyway but just to be cleaner in 3D scene...
			transform.rotation = oAnchorT.rotation;			
			_oConfJoint_Extremity.connectedBody = _oJointExtremity._oRigidBody;
			SoftJointLimit oJointLimit = new SoftJointLimit();                  //###WEAK: Do everytime?? ###TUNE?
            oJointLimit.limit = 0.001f;				//###LEARN: If this is zero there is NO spring functionality!!
			SoftJointLimitSpring oJointLimitSpring = new SoftJointLimitSpring();    //###WEAK: Do everytime??		//####MOD: To Unity5
            oJointLimitSpring.spring = _nDrivePos;
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
		_oObj.PropAdd(EActorNode.Pinned,			"Pinned",			0,	"", CProp.Local + CProp.AsCheckbox);
		_oObj.PropAdd(EActorNode.PosX,				"PosX",				0,	-2,		2,		"", CProp.Local | CProp.Hide);
		_oObj.PropAdd(EActorNode.PosY,				"PosY",				0,	-2,		2,		"", CProp.Local | CProp.Hide);		//###DESIGN!!!  Bounds???
		_oObj.PropAdd(EActorNode.PosZ,				"PosZ",				0,	-2,		2,		"", CProp.Local | CProp.Hide);		//###DESIGN: Have a 'power edit mode' that unhides these properties (shown while pressing control for example??)
		_oObj.PropAdd(EActorNode.RotX,				"RotX",				0,	-9999,	9999,	"", CProp.Local | CProp.Hide);		//###BUG ###DESIGN!!!: Meaningless to export Quaternion to user... Euler instead??
		_oObj.PropAdd(EActorNode.RotY,				"RotY",				0,	-9999,	9999,	"", CProp.Local | CProp.Hide);		//###DESIGN: Limits of quaternions
		_oObj.PropAdd(EActorNode.RotZ,				"RotZ",				0,	-9999,	9999,	"", CProp.Local | CProp.Hide);
		_oObj.PropAdd(EActorNode.RotW,				"RotW",				1,	-9999,	9999,	"", CProp.Local | CProp.Hide);
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
	public void OnPropSet_RotY(float nValueOld, float nValueNew) { _quatRotChanged.y = nValueNew; }
	public void OnPropSet_RotZ(float nValueOld, float nValueNew) { _quatRotChanged.z = nValueNew; }
	public void OnPropSet_RotW(float nValueOld, float nValueNew) { _quatRotChanged.w = nValueNew; transform.localRotation = _quatRotChanged; TeleportLinkedPhysxBone(); }     //###WEAK: Only call OptionallyTeleportLinkedPhysxBone() on one for performance reason but ugly!
	//public void OnPropSet_RotX(float nValueOld, float nValueNew) { Vector3 vecEuler = transform.localRotation.eulerAngles; vecEuler.x = nValueNew; transform.localRotation = Quaternion.Euler(vecEuler); }
	//public void OnPropSet_RotY(float nValueOld, float nValueNew) { Vector3 vecEuler = transform.localRotation.eulerAngles; vecEuler.y = nValueNew; transform.localRotation = Quaternion.Euler(vecEuler); }
	//public void OnPropSet_RotZ(float nValueOld, float nValueNew) { Vector3 vecEuler = transform.localRotation.eulerAngles; vecEuler.z = nValueNew; transform.localRotation = Quaternion.Euler(vecEuler); }



	//---------------------------------------------------------------------------	HOTSPOT EVENTS

	public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {
		//_oBody.SelectBody();			// Manipulating a body's hotspot automatically selects this body.	###CHECK: Hotspot triggers throw this off??
		if (eHotSpotEvent == EHotSpotEvent.ContextMenu)
			_oHotSpot.WndPopup_Create(_oBody, new CObject[] { _oObj });
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
				_oObj.PropSet(EActorNode.PosX, vecPosL.x);		//###NOTE: Setting EActorNode for all actors (all have same enum index for same pin properties)
				_oObj.PropSet(EActorNode.PosY, vecPosL.y);
				_oObj.PropSet(EActorNode.PosZ, vecPosL.z);
				transform.localPosition = vecPosL;
				break;
			case EEditMode.Rotate:
				Quaternion quatRotG = oGizmo.transform.rotation;			//###DESIGN???: Problem with rotation?
				transform.rotation = quatRotG;		//###WEAK!!  ###OPT!  Some work duplication with PropSet below setting our transform!
				Quaternion quatRotL = transform.localRotation;
				_oObj.PropSet(EActorNode.RotX, quatRotL.x);
				_oObj.PropSet(EActorNode.RotY, quatRotL.y);
				_oObj.PropSet(EActorNode.RotZ, quatRotL.z);
				_oObj.PropSet(EActorNode.RotW, quatRotL.w);
				break;
		}
	}
}









public class CJointDriver : MonoBehaviour {         // CJointDriver: Encapsulates common usage of the important configurable joint used for ragdoll-style physics movement of body bones  ###MOVE? To own file?
    //###MOVE?: Proper file for joint driver?
    [HideInInspector] public CActor				_oActor;			                // The actor who owns us
	[HideInInspector] public CJointDriver		_oJointDrvParent;                   // The parent joint driver (and bone) we connect to
	[HideInInspector] public Rigidbody			_oRigidBody;                        // Our rigid body (also a component of our same game object)
	[HideInInspector] public ConfigurableJoint	_oConfJoint;                        // Our D6 configurable joint.  Responsibly for PhysX processing to keep our two bone extremities at their proper rotation
	                  Vector3					_vecStartingPos;					// Pose and rotation stored so we can return to 'configure' game mode at any time
	                  Quaternion 				_quatStartingRotation;              // The starting rotation.  Used to return joint to its starting position.
                      public float              _X, _Y, _Z;                         // Our current rotation
    [HideInInspector] public float              _Xold, _Yold, _Zold;                // 'Old' version of rotation.  Used to auto-update bone rotation in 'C_BoneDebugMode' debug mode 
                      public float              _XL, _XH, _YHL, _ZHL;               // Our configuration parameters.  X = main bone bend (has a low and high), Y = twist, Z = 'side-to-side' bend (Y&Z only have Low/High combined)
                      public float              _nDriveStrengthMult;

    public static CJointDriver Create(CActor oActor, CJointDriver oJointDrvParent, string sNameBone, float nDriveStrengthMult, float nMass, float XL, float XH, float YHL, float ZHL) {
        Transform oTransform;
        if (oJointDrvParent == null)
            oTransform = oActor._oBody._oBonesT.FindChild(sNameBone);           // Finding bone when root is different.  ###IMPROVE: Can be simplified to just always top bone?  (e.g. Why does 'Bones' have a single top bone 'chestUpper' when the could be merged?)
        else
            oTransform = oJointDrvParent.transform.FindChild(sNameBone);

        if (oTransform == null)
            throw new CException("CJointDriver.Create() cannot find bone " + sNameBone);
        CJointDriver oJointDriver = CUtility.FindOrCreateComponent(oTransform.gameObject, typeof(CJointDriver)) as CJointDriver;
        oJointDriver.Initialize(oActor, oJointDrvParent, nDriveStrengthMult, nMass, XL, XH, YHL, ZHL);
        return oJointDriver;
    }


    public void Initialize(CActor oActor, CJointDriver oJointDrvParent, float nDriveStrengthMult, float nMass, float XL, float XH, float YHL, float ZHL) {
		_oActor = oActor;
		_oJointDrvParent = oJointDrvParent;
        _nDriveStrengthMult = nDriveStrengthMult;
        _XL = XL;	_XH = XH;
		_YHL = YHL;
		_ZHL = ZHL;
        _vecStartingPos         = transform.localPosition;
        _quatStartingRotation   = transform.localRotation;

        //=== Create the rigid body for our bone ===
        _oRigidBody = (Rigidbody)CUtility.FindOrCreateComponent(gameObject, typeof(Rigidbody));     //###TODO: Add a "CRigidBodyWake"???
        _oRigidBody.mass = nMass;
        _oRigidBody.drag = 1.5f;                            //###TODO!! //###DESIGN: Which drag??		//###TUNE!!!!!	###IMPROVE: Different settings for arms & legs???
        _oRigidBody.angularDrag = 1.5f;                     //###TUNE!!!!
        _oRigidBody.sleepThreshold = 0;                     // Ensure the rigid body never sleeps!


        //=== Process special handling needed when we are root (we are kinematic and we have no joint to parent) ===
        if (_oJointDrvParent == null) {             // If we have a null parent then we're the root and we're kinematic with no joint to anyone!

            _oRigidBody.isKinematic = true;

        }
        else {

            //=== If we have a parent then we are not kinematic and rotate by PhysX simulation ===
            _oRigidBody.isKinematic = false;

            //=== Create the D6 configurable joint between our parent and us ===
		    _oConfJoint = (ConfigurableJoint)CUtility.FindOrCreateComponent(gameObject, typeof(ConfigurableJoint));		//###TODO: Add a "CRigidBodyWake"???
		    _oConfJoint.connectedBody = _oJointDrvParent._oRigidBody;

            //=== Set the joint limits as per our arguments ===
            bool bInvertX = (_XL > _XH);					// If the logical range is inverted we can't send this to PhysX as lowAngularXLimit MUST be < than highAngularXLimit!
		    SoftJointLimit oJL = new SoftJointLimit();              //###IMPROVE: Has other fields that could be of use?
            if (CGame.INSTANCE.BoneDebugMode) {                   // Set debug limits if in debug mode (so we can fully rotate along all axis for game-time tuning
                oJL.limit = -177;
                _oConfJoint.lowAngularXLimit  = oJL;
                oJL.limit = 177;
                _oConfJoint.highAngularXLimit = oJL;
                _oConfJoint.angularYLimit = oJL;
                _oConfJoint.angularZLimit = oJL;
            } else { 
		        oJL.limit = bInvertX ? _XH : _XL;	_oConfJoint. lowAngularXLimit = oJL;		// X is the high-functionality axis with separately-defined Xmin and Xmax... Y and Z only have a +/- range around zero, so we are forced to raise the lower half to match the other side
		        oJL.limit = bInvertX ? _XL : _XH;	_oConfJoint.highAngularXLimit = oJL;
		        oJL.limit = _YHL;	                _oConfJoint.    angularYLimit = oJL;        //###NOTE! Hugely inconvenient feature of D6 joint is Y & Z must be symmetrical!!  Make sure bone is oriented so X is used for the assymetrical rotation!!
		        oJL.limit = _ZHL;	                _oConfJoint.    angularZLimit = oJL;
            }
            //=== Set joint axis defaults (before overriding some of them) ===
            _oConfJoint.xMotion = _oConfJoint.yMotion = _oConfJoint.zMotion = ConfigurableJointMotion.Locked;
		    _oConfJoint.angularXMotion = _oConfJoint.angularYMotion = _oConfJoint.angularZMotion = ConfigurableJointMotion.Limited;		//###DESIGN? Limited vs Free?

            //=== Free the axis that don't need driving ===  ###CHECK: Safe???
		    if (_XL == 0f && _XH == 0f) _oConfJoint.angularXMotion = ConfigurableJointMotion.Free;		// If an axis is unused set it //free to reduce PhysX workload  ###CHECK: Is this ever invoked?  Does it make joint fail if not all three axis driven??
		    if (_YHL == 0f)             _oConfJoint.angularYMotion = ConfigurableJointMotion.Free;		//###DESIGN: Verify unsetting!
		    if (_ZHL == 0f)             _oConfJoint.angularZMotion = ConfigurableJointMotion.Free;		//###NOTE: SLERP needs all three axis by definition... But Limited of little / no use if we drive all the time (less PhysX overhead))
		
            //=== Set the configurable joint drive strength ===
		    JointDrive oDrive = new JointDrive();
		    oDrive.positionSpring = _nDriveStrengthMult * CGame.INSTANCE.BoneDriveStrength;   // Final spring strength is the global constant multiplied by the provided multiplier... makes it easy to adjust whole-body drive strength
            oDrive.positionDamper = 0;							//###TODO!!!!! ###TUNE?
		    oDrive.maximumForce = float.MaxValue;               //###IMPROVE: Some reasonable force to prevent explosions??
		    //oDrive.mode = JointDriveMode.Position;
		    _oConfJoint.slerpDrive = oDrive;
		    _oConfJoint.rotationDriveMode = RotationDriveMode.Slerp;        // Slerp is really the only useful option for bone driving.  (Many other features of D6 joint!!!)

            //=== If we're a node on the right side, copy the collider defined on our twin node on the left side ===
            if (_oActor._eBodySide == EBodySide.Right) {
			    Transform oNodeSrc = CUtility.FindSymmetricalBodyNode(transform.gameObject);
                //Debug.Log("Collider copy " + oNodeSrc.name);
                Collider oColBaseSrc = oNodeSrc.GetComponent<Collider>();
                if (oColBaseSrc.GetType() == typeof(CapsuleCollider)) {
				    CapsuleCollider oColSrc = (CapsuleCollider)oColBaseSrc;
				    CapsuleCollider oColDst = (CapsuleCollider)CUtility.FindOrCreateComponent(transform, typeof(CapsuleCollider));
				    oColDst.center 		= oColSrc.center;
				    oColDst.radius 		= oColSrc.radius;
				    oColDst.height 		= oColSrc.height;
				    oColDst.direction 	= oColSrc.direction;
                } else if (oColBaseSrc.GetType() == typeof(BoxCollider)) {
				    BoxCollider oColSrc = (BoxCollider)oColBaseSrc;
				    BoxCollider oColDst = (BoxCollider)CUtility.FindOrCreateComponent(transform, typeof(BoxCollider));
				    oColDst.center 		= oColSrc.center;
				    oColDst.size 		= oColSrc.size;
                }
            }
        }
    }

	public void OnChangeGameMode(EGameModes eGameModeNew, EGameModes eGameModeOld) {
		// Joint becomes kinematic and reverts to starting position upon configure mode, becomes PhysX-simulated during gameplay
		switch (eGameModeNew) {
			case EGameModes.Configure:
                _X = _Y = _Z = 0;
                UpdateRotation();
                _oRigidBody.isKinematic = true;
				transform.localPosition = _vecStartingPos;				// Restore the joint to its startup position / orientation
				transform.localRotation = _quatStartingRotation;
				break;
			case EGameModes.Play:
                if (_oConfJoint != null) { 
                    JointDrive oDrive = _oConfJoint.slerpDrive;
                    oDrive.positionSpring = _nDriveStrengthMult * CGame.INSTANCE.BoneDriveStrength;   // Final spring strength is the global constant multiplied by the provided multiplier... makes it easy to adjust whole-body drive strength
                    _oConfJoint.slerpDrive = oDrive;
                }
                _oRigidBody.isKinematic = false;
                _X = _Y = _Z = 0;
                UpdateRotation();
                break;                  //###IMPROVE: Add a new game mode for kinematic but 'reset pose to T'?
		}
	}

    void Update() {
        if (CGame.INSTANCE.BoneDebugMode) {                           // In 'bone debug mode' trap any change of our rotation values to update rotation right away.  (Makes it possible to quickly tune at gametime realistic bone rotations)
            if (_X != _Xold || _Y != _Yold || _Z != _Zold) {
                UpdateRotation();
                _Xold = _X;  _Yold = _Y;  _Zold = _Z;
                //_oRigidBody.WakeUp();           //###CHECK: Needed?
            }
        }
    }

	//public Rigidbody GetRB() { return _oConfJoint.GetComponent<Rigidbody>(); /* GetComponent<Rigidbody>();*/ }
	
	void UpdateRotation() {     // Update X, Y, Z rotation
        if (_oConfJoint != null)
		    _oConfJoint.targetRotation = Quaternion.Euler(_X, _Y, _Z);      // Rotate as specified by X,Y,Z  (z, x, y in that order) (Joint already starts from its starting rotation)
    }

	public void DumpBonePos_DEV() {		//###NOTE: For development... Enables to dump the position of a joint as it is now to enable hard-coding of desirable states (useful to properly position arm)
		//float nXP = 100.0f * (_X - _XL) / (_XH - _XL);		// Not working as we have two formulas for single/dual range
		//float nYP = 100.0f * (_Y - _YL) / (_YH - _YL);
		//float nZP = 100.0f * (_Z - _ZL) / (_ZH - _ZL);
		//Debug.Log(string.Format("{0}.{1} at {2:F0},{3:F0},{4:F0}", _oActor.transform.name, _oTransform.name, nXP, nYP, nZP));
		//Vector3 vecRot = _oTransform.localRotation.eulerAngles;
		//Debug.Log(string.Format("{0}.{1} at {2:F0},{3:F0},{4:F0}", _oActor.transform.name, _oTransform.name, vecRot.x, vecRot.y, vecRot.z));
		Quaternion quatRot = transform.localRotation;
		Debug.Log(string.Format("{0}.{1} at {2:F3},{3:F3},{4:F3},{5:F3}", _oActor.transform.name, transform.name, quatRot.x, quatRot.y, quatRot.z, quatRot.w));
	}

    //=== Rotation where source value goes from 0% to 100% ===
    public void RotateX1(float nAnglePercent) { _X = _XL  + (nAnglePercent/100f) * (_XH-_XL);   UpdateRotation(); }
	public void RotateY1(float nAnglePercent) { _Y = (nAnglePercent/100f) * _YHL;               UpdateRotation(); }
	public void RotateZ1(float nAnglePercent) { _Z = (nAnglePercent/100f) * _ZHL;               UpdateRotation(); }

    //=== Rotation where source value goes from -100% to 100% ===
    public void RotateX2(float nAnglePercent) { _X = (nAnglePercent/100f) * ((nAnglePercent<0) ? -_XL : -_XH); UpdateRotation(); }       //###DESIGN!!!: Not linear if low and high are not opposite... the desired behavior??
	public void RotateY2(float nAnglePercent) { _Y = (nAnglePercent/100f) * _YHL; UpdateRotation(); }       // Low and high are symmetrical, so simpler than X
	public void RotateZ2(float nAnglePercent) { _Z = (nAnglePercent/100f) * _ZHL; UpdateRotation(); }

	//public void SetRotationRaw_HACK(float X, float Y, float Z) { _X = X; _Y = Y; _Z = Z; UpdateRotation(); }		// Hack call to bypass our calibration setting and set in direct angles (extracted by observing body limbs in scene for e.g. arm placement)
	//public void SetRotationRaw_HACK(Quaternion quatRot) { _oConfJoint.targetRotation = _quatStartingRotation; }		// Hack call to bypass our calibration setting and set in direct angles (extracted by observing body limbs in scene for e.g. arm placement)
	//public void SetRotationDefault_HACK() { _oConfJoint.targetRotation = _quatStartingRotation; }

	//public void Enable() {				//###DESIGN!!!!??? ###BROKEN?? Not working...redo enable / disable design of actor??
	//	_oRigidBody.isKinematic = false;
	//}
	//public void Disable() {
	//	_oRigidBody.isKinematic = true;
	//	_oTransform.localPosition	= _vecStartingPos;
	//	_oTransform.localRotation 	= _quatStartingRotation;		// Restore rotation the way it was when we got created...  should return body to T-pose.
	//}
};


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
//		case EActorAnchor_OBS.HandPalmCenter:	oNodeAnchor = _oJointExtremity._oTransform.FindChild("Anchor-HandPalmCenter");					break;
//		case EActorAnchor_OBS.MidFingerMidway:	oNodeAnchor = _oJointExtremity._oTransform.FindChild(_sSidePrefixL+"Mid1/"+_sSidePrefixL+"Mid2/Anchor-MidFingerMidway");	break;
//		case EActorAnchor_OBS.MidFingerTip:	 	oNodeAnchor = _oJointExtremity._oTransform.FindChild(_sSidePrefixL+"Mid1/"+_sSidePrefixL+"Mid2/"+_sSidePrefixL+"Mid3/Anchor-MidFingerTip");	break;
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
	//			_oObj.PropSet(EActorNode.PosX, vecPos.x);		//###WEAK: Setting EActorNode for all actors (all have same index for same pin properties)
	//			_oObj.PropSet(EActorNode.PosY, vecPos.y);		
	//			_oObj.PropSet(EActorNode.PosZ, vecPos.z);
	//			transform.position = vecPos;
	//			break;
	//		case EEditMode.Rotate:
	//			Quaternion quatRot = oGizmo.transform.rotation;
	//			_oObj.PropSet(EActorNode.RotX, quatRot.x);
	//			_oObj.PropSet(EActorNode.RotY, quatRot.y);
	//			_oObj.PropSet(EActorNode.RotZ, quatRot.z);
	//			_oObj.PropSet(EActorNode.RotW, quatRot.w);
	//			transform.rotation = quatRot;		//###DESIGN!!!! Huge work duplication with PropSet below setting our transform!
	//			break;
	//	}
	//}


//Vector3 vecEulerG = oGizmo.transform.rotation.eulerAngles;
//transform.rotation = Quaternion.Euler(vecEulerG);	//###CHECK!!!!!!: Proper way to convert global rotation to local?  ###CHECK: Need to do different for base??
//Vector3 vecEulerL = transform.localRotation.eulerAngles;	//###BUG!!!!! Problem with CActor and rotation. Euler conversion can't take all angles!!
//_oObj.PropFind(EActorNode.RotX)._nValueLocal = vecEulerL.x;		//###HACK!!!!!
//_oObj.PropFind(EActorNode.RotY)._nValueLocal = vecEulerL.y;
//_oObj.PropFind(EActorNode.RotZ)._nValueLocal = vecEulerL.z;
