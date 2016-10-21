using UnityEngine;
using System;
using System.Collections;

//public class CCollider_OBS : MonoBehaviour {	// Converts a standard Unity collider to a FastPhysics one.  Note that non-centered/resized collider components are not supported.  (Collider component 'Center' must read 0,0,0 and size 1,1,1)

//	public	bool		PhysX2;					// Collider exists in the PhysX2 scene to repell softbody objects (currently only breasts)
//	public	bool		PhysX3;					// Collider exists in the PhysX2 scene to repell fluid (others???)
//	public	bool		IsDrain = false;		// If set any fluid particle that comes in contact with this collider is removed from the simulation and recycled for later emission...
//	public	EColGroups	_eColGroup;				// Our collision group.  Should sync up to the same meaning in PhysX2/PhysX3 (but not Unity PhysX)
//	public	float		DensityOrMass = -1;		//###TODO: Density or mass!  A public member??		###WEAK: Both coupled in one var?  Not very intuitive

//	IntPtr				_hCollider;				// Our memory handle to equivalent kinematic collider actor in PhysX2/PhysX3 scene (abstracted away in CCollider.cpp file between the two scenes)

//	Vector3				_vecOffset;				// Local-coordinate offset user entered into collider 'center' attribute.  We add this vector to local coordinate and convert to global coordinates every frame
//	bool				_bHasLocalOffset;		// True if _vecOffset was set to non-zero at init-time.  Used to speed up per-frame processing

//	BoxCollider			_oColBox;				//###OPT? Too expensive to store these for every collider??
//	SphereCollider		_oColSphere;
//	CapsuleCollider		_oColCapsule;


//	public void OnStart() {
//		OnStart(PhysX2, PhysX3, _eColGroup);
//	}

//	public void OnStart(bool bPhysX2, bool bPhysX3, EColGroups eColGroup = EColGroups.eLayerDefault) {		//###DESIGN: Restore ability to be able to have these autoinit from startup / enabled???
//		PhysX2 = bPhysX2;
//		PhysX3 = bPhysX3;
//		_eColGroup = eColGroup;

//		if (GetComponent<Collider>() == null) {
//			Debug.LogError("ERROR: Object '" + gameObject.name + "' has requested a FastPhysics collider but has no Unity collider!");
//			enabled = false;
//			return;
//		}
//		_oColBox		= GetComponent<Collider>() as BoxCollider;		//###CHECK ###BUG? ###DESIGN: Need to improve all colliders so they work in FastPhysics as closes to possible to Unity.  (e.g. scaling of capsules, offsets on boxes, etc)
//		_oColSphere		= GetComponent<Collider>() as SphereCollider;
//		_oColCapsule	= GetComponent<Collider>() as CapsuleCollider;		//###IMPROVE: Set collider properties to avoid consuming resources?

//		uint nFlags = 0;
//		if (PhysX2)					nFlags |= (uint)EColFlags.ePhysX2;
//		if (PhysX3)					nFlags |= (uint)EColFlags.ePhysX3;
//		if (IsDrain)				nFlags |= (uint)EColFlags.eDrain;
//		if (gameObject.isStatic)	nFlags |= (uint)EColFlags.eStatic;
//		nFlags |= (uint)EColFlags.eMatFriction000;						//###IMPROVE: Expose materials?

//		if (_oColBox != null) {
//			if (transform.localScale.y != 0) {		//###WEAK: We differentiate between a box collider and a plane collider as sizeY=0... can be improved?? (No plane colliders in Unity)
//				//Vector3 vecScale = new Vector3(transform.lossyScale.x * _oColBox.size.x, transform.lossyScale.y * _oColBox.size.y, transform.lossyScale.z * _oColBox.size.z);
//				if (_oColBox.size != new Vector3(1, 1, 1))
//					CUtility.ThrowException("ERROR in CCollider.OnStart(): Found box collider " + gameObject.name + " with non-unity size");		//###DESIGN: Do we really ignore box size??
//				_hCollider = ErosEngine.Collider_Box_Create(gameObject.name, nFlags, transform.position, transform.rotation, transform.lossyScale, DensityOrMass, (int)_eColGroup);									// Center must be at 0,0,0, Size (of collider component) at 1,1,1 and object scale can be anything (inverse than sphere & capsule)	###CHECK!!!! ###DESIGN!!! Box only defined by localScale?? 	###LEARN: box size divided by two for compatibility with PhysX
//			} else {
//				_hCollider = ErosEngine.Collider_Plane_Create(gameObject.name, nFlags, transform.position, transform.rotation, DensityOrMass, (int)_eColGroup);		//###CHECK: Untested... first test didn't appear to work!
//			}
//			_vecOffset = _oColBox.center;
//		} else {
//			float nScale = transform.lossyScale.x;		// We scale spheres and capsules by scale.  Both *must* have scaling x=y=z!!	//###NOTROBUST: Test x=y=z scale?
//			if (_oColSphere != null) {
//				_hCollider = ErosEngine.Collider_Sphere_Create(gameObject.name, nFlags, transform.position, _oColSphere.radius * nScale, DensityOrMass, (int)_eColGroup);									// Center must be at 0,0,0, object scale at 1,1,1
//				_vecOffset = _oColSphere.center;
//			} else if (_oColCapsule != null) {
//				_hCollider = ErosEngine.Collider_Capsule_Create(gameObject.name, nFlags, transform.position, transform.rotation, _oColCapsule.radius * nScale, _oColCapsule.height * nScale, DensityOrMass, 2, (int)_eColGroup);		//###LEARN: To match Unity's capsule mesh we must rotate 90 degrees about Y (Would be nice to be able to match X,Y,Z rotation of capsule collider but mesh would not follow!)
//				_vecOffset = _oColCapsule.center;
//			} else {
//				Debug.LogError("ERROR: Object '" + gameObject.name + "' has an collider of type '" + GetComponent<Collider>().GetType().Name + "' that is unsupported in FastPhysics.");
//				enabled = false;
//				return;
//			}
//		}
//		_bHasLocalOffset = (_vecOffset.magnitude != 0);

//		//###DESIGN!: Shut off if no rigidbody? (Prevents bones from updating though!)
//		if (/*rigidbody == null ||*/ gameObject.isStatic) {	//###NOTE: We update equivalent FastPhysics collider only on Unity colliders that have a 'RigidBody' component and that are not static.  (Colliders without rigidbodies are NOT supposed to move during gameplay and FastPhysics position is set only during initialization)
//			enabled = false;							// Then shut off update		//###BUG??  Test this... didn't do anything once!
//		}
//	}
	
//	public void OnDestroy() {
//		if (_hCollider != IntPtr.Zero)
//			ErosEngine.Collider_Destroy(_hCollider);
//	}

//	//###DESIGN!!!!! Update() or FixedUpdate()???
//	void FixedUpdate() {				// Update ONLY position / rotation at every physics frame.		###DESIGN: Deterministic update like the rest of the app???
//		if (CGame.INSTANCE == null)		//####REVB
//			return;			
//		if (CGame.INSTANCE._GameIsRunning && _hCollider != IntPtr.Zero) {		//###OPT?
//			Vector3 vecPos = transform.position;
//			if (_bHasLocalOffset)
//				vecPos = transform.localToWorldMatrix.MultiplyPoint(_vecOffset);
//			ErosEngine.Collider_SetPositionRotation(_hCollider, vecPos, transform.rotation);		//###IMPROVE: Prevent setting position / orientation if no change?  (But check imposes overhead) ###PROFILE to know how to improve performance throughout
//		}
//	}

//	public void SetBoxSize(Vector3 vecSize) {			//###WEAK: Update for all types?
//		if (_oColBox == null)
//			return;
//		transform.localScale = vecSize;					//###WEAK!!: Assumes ancestor nodes all have scale of 1.  Set by testing with 'lossyScale'? (read only)
//		_oColBox.size = Vector3.one;
//		ErosEngine.Collider_Box_Update(_hCollider, vecSize);
//	}

//	public void EnableDisable(bool bEnable) { ErosEngine.Collider_EnableDisable(_hCollider, bEnable); }
//}
