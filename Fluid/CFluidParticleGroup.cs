/*###DOCS24: Sept 2017 - Fluid particle group: 
=== DEV ===

=== NEXT ===
- Reject collision planes that are too far?
- Have gradual strenghtening of springs?
- Optimize the many parameters like spring strength!

=== TODO ===
- Could re-tune Blender's fluid
- Set to #define some of the non-needed code such as plane center & normal calculations!
- Improve renderer particle mult ratio, SSF pro blending, etc

=== LATER ===

=== OPTIMIZATIONS ===
- Run profiler on this stuff to limit the most expensive stuff!
- CFluidParticleGroup keeps the transform & prefab?  (Tri-springs to 3 plane verts means we no longer need mid)
- Exhaustive search of all collider particles from all bodies expensive!  Can form a spacial search tree for this that is occasionally updated?
- Currently re-building full list of close collider verts at every frame!  Can 'freeze the body' in time and just do this once?

=== REMINDERS ===
- Consider having wrapper class for LineRenderer?

=== IMPROVE ===
- Particle re-pinning to self position moving fluid blobs!  WTF?
- Particles fighting against one another... implement gradual strenghtening of springs so newer particles mold against older ones!
	- Perform gradual updates of the spring lenght as particle ages (to reduce particle fighting)
	- Use density as a helper?  Denser the more we adjust!
		- Or... closer to plane?
- Culling improvements
	- Remove particles that are too far (occasional checks)
- Consider turning D_ShowDebugVisualization into a Unity-wide symbol entered in Unity GUI project property?
- When we reach 0 particles, Destroy, remove us from the fluid queue and add our particles to the bank??

=== NEEDS ===

=== DESIGN ===
- Currently only looking for collision planes when angle changes sharply... much cheaper but can miss some good collider choices!

=== QUESTIONS ===

=== IDEAS ===
- IDEA: Use C# finalize for auto-cleanup more with standard destructor C++ like syntax!  (Cannot unfortunately do everything as some cleanup like accessing gameObject can only be done from main thread)

=== LEARNED ===

=== PROBLEMS ===
- Small collision triangles can have a plane normal that is quite different from its neighbors: this results in perfectly-pinned particles to float out of thin air...
	- We could resolve this by forcing particles to stay within the original triangle but detection would be programmatically expensive!
- Can have blobs of particles forming under collider planes?  WTF?
- Some particles appear are out of place at init?  WTF are they??  (Possible mix-up with main scene??)
- Can't re-enable SSF pro!
- Odd isolated particles that won't cull...  see in SSF pro debug!  Master particles??
- Even low densities 'pick off' the edge particles quickly, gradually reducing the fluid!
- Fluid reaching groud dissipates right away!  Can improve some setting?  Can reset velocity so it sticks together?
- Fluid particles can start 'dancing' when body moves... how come not culled??  (Because of consecutive requirement??)

=== WISHLIST ===

*/

#define D_ShowDebugVisualization

using UnityEngine;
using System.Collections.Generic;


public class CFluidParticleGroup : MonoBehaviour {                  // CFluidParticleGroup: Responsible for 'grouping' particles around an 'Master particle' to 1) greatly reducing their velocity upon first collision, 2) move these particles toward the collision plane for a while and 3) set the particles as kinematic and move them with the collider plane
	public CFlexEmitter             _oFlexEmitter;
	public CFlexParamsFluid			_oFlexParamsFluid;
	public CFlexTriCol              _oFlexTriCol_BodyWePinTo;

	public List<CFluidParticle>     _aFluidParticles	= new List<CFluidParticle>();	// All our particles.  The 'master particle' is always at index 0 (relevant when detecting collisions only)
	public CFluidParticlePin[]		_aFluidParticlePins = new CFluidParticlePin[3];		// Identifies the three verts forming a 'pinning plane' on the body collider we stick to.
	List<CFluidParticle>			_aParticlesToCull	= new List<CFluidParticle>();	// Particles to cull at each frame (kept so we don't allocate at each frame)
	public Vector3[]                _aVertsCollisionTri = new Vector3[4];				// Array of pin positions (used for debug visualization only)
	public CFluidParticle			_oFluidParticle_LastCenterParticle;     // The last emitted center particle (center one in each slice) of the last slice emitted.  Needed by emitter to seamlessly connect particle groups together

	public Vector3                  _vecParticleVelocity_Now;		// The observed particle's velocity vector in this frame.
	public Vector3                  _vecParticleVelocity_Prev;      // The observed particle's velocity vector in the previous frame.  Radical changes in direction will cause every particle in this emit group to be greatly slowed-down to prevent fluid bouncing off collider!
	public Vector3                  _vecPlaneCenter;				// The center of the plane we're colliding against.		###OBS??
	public Vector3                  _vecPlaneNormal;				// The normal of the plane we're colliding against.  At every frame we set our velocity in the reverse direction of the normal to 'push' our group of particles to 'stick' against the collision plane we selected during the initial collision

	public EFluidParticleGroupMode  _eFluidParticleGroupMode	= EFluidParticleGroupMode.S0_Uninitialized;     // The state of this group of emitter particles
	public ushort					_nNumParticlesInGroup;          // The number of particles in our 'emit group'.  We slow down all particles in our group in the same operation.

	public int                      _nFramesInState;				// Frame counter used to 'delay' progression from some states to their next one.
	static uint                     _nFluidParticleGroupNext;
	Quaternion						_quatRotation = new Quaternion();		// To avoid creating each frame

	LineRenderer					_oLineRenderer_Normal;
	LineRenderer					_oLineRenderer_CollisionPlane;

	public static Color s_color_S0_Uninitialized				= Color.white;
	public static Color s_color_S1_AwaitingFirstCollision		= Color.green;
	public static Color s_color_S3_PinnedToCollisionPlane		= Color.cyan;
	public static Color s_color_S4_PinnedToStabilizedPos        = Color.blue;
	public static Color s_color_S2_NotPinnedToCollider	= new Color32(128, 0, 0, 255);		// Dark red
	public static Color s_color_PinParticle                     = Color.magenta;
	public static Color s_color_MasterParticle                  = Color.red;


	public static CFluidParticleGroup Create(CFlexEmitter oFlexEmitter) {
		GameObject oGOT = Resources.Load("Prefabs/CFluidParticleGroup") as GameObject;
		GameObject oGO = GameObject.Instantiate(oGOT) as GameObject;
		oGO.name = "CFluidParticleGroup" + _nFluidParticleGroupNext++.ToString();
		oGO.transform.SetParent(oFlexEmitter._oFlexParamsFluid.transform);
		CFluidParticleGroup oFluidParticleGroup = oGO.GetComponent<CFluidParticleGroup>();
		return oFluidParticleGroup;
	}

	public void EmitParticles(CFlexEmitter oFlexEmitter, ushort nSlices, Vector3 vecPosEmitterStart, Vector3 vecDistanceBetweenSlices, float nEmitVelocity_IncrementPerParticle, float nEmitVelocity_PreviousFrame) {
		_oFlexEmitter = oFlexEmitter;
		_oFlexParamsFluid = oFlexEmitter._oFlexParamsFluid;
		_nNumParticlesInGroup = (ushort)(nSlices * _oFlexParamsFluid._nParticlesPerSlice);
		_oFlexParamsFluid.ParticleBank_ReserveParticles(_nNumParticlesInGroup);      // Tell fluid we will need this many particles.  (It will repurpose the oldest ones for us to use)

		//=== Emit our particles ===
		Matrix4x4 matEmitterL2W = _oFlexEmitter.transform.localToWorldMatrix;
		ushort nNumParticleThisFrame = 0;

		for (ushort nSlice = 0; nSlice < nSlices; nSlice++) {                                          // Iterate through the slices that will will be consumed out of our emitting area given our time scale and outgoing velocity.
			Vector3 vecPosSliceCenter = vecPosEmitterStart + nSlice * vecDistanceBetweenSlices;

			for (ushort nParticleOrdinalInGroup = 0; nParticleOrdinalInGroup < _oFlexParamsFluid._nParticlesPerSlice; nParticleOrdinalInGroup++) {                 // Iterate through all the particles to insert them into this outgoing 'emitter slice'
				CFluidParticle oFluidParticle = new CFluidParticle(this, vecPosSliceCenter, nParticleOrdinalInGroup, nNumParticleThisFrame, nEmitVelocity_PreviousFrame, nEmitVelocity_IncrementPerParticle, ref matEmitterL2W);
				_aFluidParticles.Add(oFluidParticle);
				if (nParticleOrdinalInGroup == 0 && nSlice == nSlices-1)
					_oFluidParticle_LastCenterParticle = oFluidParticle;			// Remember the last center particle of the last slice.  (Emitter needs it for seamless joining of particle groups)
				nNumParticleThisFrame++;
			}
		}
	}

	public void DoDestroy() {
		DestroyAllParticles();
#if D_ShowDebugVisualization
		if (_oLineRenderer_CollisionPlane)
			GameObject.Destroy(_oLineRenderer_CollisionPlane.gameObject);
		if (_oLineRenderer_Normal)
			GameObject.Destroy(_oLineRenderer_Normal.gameObject);
#endif
		GameObject.Destroy(gameObject);		// Destroy our entire gameObject, which also destroys this instance / component.
	}

	public void DestroyAllParticles() {
		foreach (CFluidParticle oFluidParticle in _aFluidParticles)
			oFluidParticle.DoDestroy();
		_aFluidParticles.Clear();
	}

	public void FluidParticleGroup_UpdateParticles() {    // Update all particles from our group.  Depending on our state (uncollided, collided, pinned to pin plane) we modify the state of our particles as appropriate.
		//=== Update the position of our slave particles ===
		foreach (CFluidParticle oFluidParticle in _aFluidParticles) {
			bool bShouldBeCulled = oFluidParticle.InspectParticle();
			if (bShouldBeCulled) {
				if (oFluidParticle._nParticleOrdinalInGroup != 0 || _eFluidParticleGroupMode != EFluidParticleGroupMode.S1_AwaitingFirstCollision)		// Avoid culling master particle when detecting collision as it's the one being polled for angle changes!
					_aParticlesToCull.Add(oFluidParticle);
			}
		}

		//=== Destroy particles that are flagged for culling (isolated) ===
		foreach (CFluidParticle oFluidParticle in _aParticlesToCull) {
			_aFluidParticles.Remove(oFluidParticle);
			oFluidParticle.DoDestroy();
			_oFlexParamsFluid.STAT_FluidParticlesCulled++;
		}
		_aParticlesToCull.Clear();

		//=== Don't do anything if we have no active particles left ===
		if (_aFluidParticles.Count == 0)			//###IMPROVE: Destroy, remove us from the fluid queue and add our particles to the bank??
			return;


		//=== Perform state-dependant processing ===
		switch (_eFluidParticleGroupMode) {
			case EFluidParticleGroupMode.S0_Uninitialized:              // Particle group is uninitialized (for only one frame).  Record the 'previous frame velocity' and switch to 'awaiting first collision'
				_vecParticleVelocity_Now = _oFlexParamsFluid._oFlexParamsFluidParticleBank.m_velocities[_aFluidParticles[0]._nParticleID];
				_vecParticleVelocity_Prev = _vecParticleVelocity_Now;
				Util_AdjustParticle_Colors(s_color_S1_AwaitingFirstCollision);
				_eFluidParticleGroupMode = EFluidParticleGroupMode.S1_AwaitingFirstCollision;
				break;

			case EFluidParticleGroupMode.S1_AwaitingFirstCollision:     // The 'master particle' (center particle in first slice) is testing for collision by observing sharp direction changes in its velocity vector.  Particle is in emit stream and has yet to collide with anything.  (We detect collisions by changes in direction of the Master particle velocity vector)
				_vecParticleVelocity_Now = _oFlexParamsFluid._oFlexParamsFluidParticleBank.m_velocities[_aFluidParticles[0]._nParticleID];
				float nAngleChangeToPreviousFrame = Vector3.Angle(_vecParticleVelocity_Now, _vecParticleVelocity_Prev);
				if (nAngleChangeToPreviousFrame > _oFlexParamsFluid._nFluidGrp_CollisionDetech_AngleChange) {			// Angle of this Master particle has changed too much since last frame = it has hit a collider = greatly reduce the velocity of every particle in this emit group and kill this particle Master (our slow-down work is done)
					Util_AdjustParticle_Velocities_Multiply(_oFlexParamsFluid._nFluidGrp_SlowdownOnInitialCollision);	// Greatly slow down our group of particles.  (If we didn't they would probably bounce off the collider as fluid viscosity & adhesion forces are FAR too small!)
					bool bFoundCollisionPlane = PinToClosestCollisionPlane();
					if (bFoundCollisionPlane) {
						Util_AdjustParticle_Colors(s_color_S3_PinnedToCollisionPlane);
						_eFluidParticleGroupMode = EFluidParticleGroupMode.S3_PinnedToCollisionPlane;
						_nFramesInState = 0;
					} else {
						if (_vecParticleVelocity_Now.y < 0.01f) {       // Particles are very near ground level.  Remove the special processing on them and let them interact with the floor as usual.  ###CHECK: Not providing culling??  To a different mode??
							Util_AdjustParticle_Colors(s_color_S2_NotPinnedToCollider);
							_eFluidParticleGroupMode = EFluidParticleGroupMode.S2_NotPinnedToCollider;
							_oFlexParamsFluid.STAT_FindPlaneFail_NotPinnedToCollider++;
						}
					}
				}
				_vecParticleVelocity_Prev = _vecParticleVelocity_Now;
				break;

			case EFluidParticleGroupMode.S2_NotPinnedToCollider:      // Particle group could not find a good body collider to pin to and reached a non-body collider like the floor.  Provide culling only.
				// Nothing to do here.  We just provide culling services.
				break;

			case EFluidParticleGroupMode.S3_PinnedToCollisionPlane:     // Each particle is 'pinned' to the closest collision triangle it could find on a fluid collider mesh.  Until its destruction it will appear to 'stick' to the body.
				if (_nFramesInState++ > _oFlexParamsFluid._nFluidGrp_NumFramesStabilizingToFinalPos) {		// Allow particles to 'stabilize' to its final pinning position before we re-pin it to where it is at a certain frame threshold
					foreach (CFluidParticle oFluidParticle in _aFluidParticles)				// Pin our particles to their current position.  This will greatly stabilize the neighboring fluid area!
						oFluidParticle.PinParticles(/*bUseCurrentPosition =*/ true);
					Util_AdjustParticle_Colors(s_color_S4_PinnedToStabilizedPos);
					_eFluidParticleGroupMode = EFluidParticleGroupMode.S4_PinnedToStabilizedPos;
				}
				break;

			case EFluidParticleGroupMode.S4_PinnedToStabilizedPos:      // Each particle is 'pinned' to its 'stabilized pinning position' (The position it was found to be at after a number of frames while in mode 'S3_PinnedToCollisionPlane' trying to converge on collision plane.  This mode exists to prevent contention / fluid instability as many particles attempt to travel to the pinning plane.  This is the last mode a particle group can be on.
				//if (_nFramesInState++ % _oFlexParamsFluid._nFluidGrp_NumFramesStabilizingToFinalPos == 0) {      // Periodically re-adjust the springs to the current particle position, further stabilizing the fluid as it ages...
				//	foreach (CFluidParticle oFluidParticle in _aFluidParticles)			//###IMPROVE: Not working!  Moves some disconnected parts of the fluid for some reason!
				//		oFluidParticle.PinParticles(/*bUseCurrentPosition =*/ true);
				//}
				break;

		}


#if D_ShowDebugVisualization
		if (_oFlexParamsFluid.D_ShowParticles_Debug && _eFluidParticleGroupMode >= EFluidParticleGroupMode.S3_PinnedToCollisionPlane) {
			UpdateParticleGroupTransform();

			//=== Draw the useful normal vector.  Important during debugging! ===
			if (_oLineRenderer_Normal == null && _oFlexParamsFluid.D_ShowParticles_Debug)
				_oLineRenderer_Normal = CGame.Line_Add("FPNormal", Color.green, 0.001f, 0.0005f);
			if (_oLineRenderer_Normal) {
				_oLineRenderer_Normal.SetPosition(0, _vecPlaneCenter);
				_oLineRenderer_Normal.SetPosition(1, _vecPlaneCenter + _vecPlaneNormal * 0.02f);
			}

			//=== Draw the collision plane triangle edges ===
			if (_oLineRenderer_CollisionPlane == null)
				_oLineRenderer_CollisionPlane = CGame.Line_Add("FPPlane", Color.blue, 0.002f, 0.002f, 4);
			if (_oLineRenderer_CollisionPlane) { 
				for (int nVertPlane = 0; nVertPlane < 3; nVertPlane++)
					_aVertsCollisionTri[nVertPlane] = _aFluidParticlePins[nVertPlane]._vecVertPos;
				_aVertsCollisionTri[3] = _aFluidParticlePins[0]._vecVertPos;		// Last line vert is the first vert so we get a closed triangle
				_oLineRenderer_CollisionPlane.SetPositions(_aVertsCollisionTri);
			}
		}
#endif
	}

	void UpdateParticleGroupTransform() {			//###OPT:!!! No longer needed for its original purpose of moving / rotating children nodes.  Now just for debugging & visualizing.  Keep?
		//=== Calculate our collision plane center & normal from the average of our three points ===	// Note: It is guaranteed that all CFluidParticlePins have been updated just before.  We can reliably just fetch those we need.
		_vecPlaneCenter = (_aFluidParticlePins[0]._vecVertPos    + _aFluidParticlePins[1]._vecVertPos    + _aFluidParticlePins[2]._vecVertPos)    / 3;
		_vecPlaneNormal = (_aFluidParticlePins[0]._vecVertNormal + _aFluidParticlePins[1]._vecVertNormal + _aFluidParticlePins[2]._vecVertNormal) / 3;

		//=== Set the pin position and rotation ===
		transform.position = _aFluidParticlePins[0]._vecVertPos;
		Vector3 vec1to2 = _aFluidParticlePins[1]._vecVertPos - _aFluidParticlePins[0]._vecVertPos;
		_quatRotation.SetLookRotation(_aFluidParticlePins[0]._vecVertNormal, vec1to2);
		transform.rotation = _quatRotation;
	}

	void Util_AdjustParticle_Velocities_Multiply(float nVelocityMultiplier) {
		foreach (CFluidParticle oFluidParticle in _aFluidParticles)
			_oFlexParamsFluid._oFlexParamsFluidParticleBank.m_velocities[oFluidParticle._nParticleID] *= nVelocityMultiplier;
	}

	void Util_AdjustParticle_Colors(Color colorBase) {
#if D_ShowDebugVisualization
		Color32 colorRandomized = colorBase;     // Perform small channel randomization so we can tell emit groups apart when visualizing particles during debugging
		if (colorRandomized.r == 0)
			colorRandomized.r = (byte)(CGame.INSTANCE._oRnd.Next() % 96);
		if (colorRandomized.g == 0)
			colorRandomized.g = (byte)(CGame.INSTANCE._oRnd.Next() % 96);
		if (colorRandomized.b == 0)
			colorRandomized.b = (byte)(CGame.INSTANCE._oRnd.Next() % 96);

		foreach (CFluidParticle oFluidParticle in _aFluidParticles)
			_oFlexParamsFluid._oFlexParamsFluidParticleBank.m_colours[oFluidParticle._nParticleID] = colorRandomized;
		_oFlexParamsFluid._oFlexParamsFluidParticleBank.m_colours[_aFluidParticles[0]._nParticleID] = s_color_MasterParticle;		// First particle is the master one and is colored differently
#endif
	}

	public bool PinToClosestCollisionPlane() {          // Find the closest three verts on the collider, determine its plane and find the closest point on that plane to our Master particle
		//=== Iterate through the fluid mesh collider to add the neighboring verts ===
		SortedList<float, CColliderVert> aSortedColliderVerts = new SortedList<float, CColliderVert>();        //###INFO: How to keep a collection sorted = easy!
		Vector3 vecPosMaster = _aFluidParticles[0]._vecParticle;
		Vector3 vecPosEmitter = _oFlexEmitter.transform.position;
		Vector3 vecVertColliderToMasterParticle;
		Vector3 vecVertColliderToEmitter;

		//=== Accumulate in a sorting collection all the collider vertices within a reasonable distance to our master particle.  These will be inspected further to find the collision plane we can stick to ===
		foreach (CBodyBase oBodyBase in CGame.INSTANCE._aBodyBases) {				//###OPT:!!!!! Very expensive to iterate through all collider verts on all body!  Can redesign?  Can reduce frequency of this full check?
			Vector3[] aVerts	= oBodyBase._oBody._oFlexTriCol_BodyFluid._oMeshBaked.vertices;
			Vector3[] aNormals	= oBodyBase._oBody._oFlexTriCol_BodyFluid._oMeshBaked.normals;

			if (aNormals.Length > 0) {			//###BROKEN:!!!! mesh no longer has normals!  WTF??

				ushort nVerts = (ushort)aVerts.Length;
				for (ushort nVert = 0; nVert < nVerts; nVert++) {
					Vector3 vecVert		= aVerts[nVert];
					Vector3 vecNormal	= aNormals[nVert];
					vecVertColliderToMasterParticle = vecVert - vecPosMaster;
					float nDistSqr = vecVertColliderToMasterParticle.sqrMagnitude;
					if (nDistSqr <= _oFlexParamsFluid._nFluidGrp_PlaneDetect_MinDistToParticleSqr) {			// Only consider collider verts within a reasonable distance (avoids sorting collection becoming too large / slow)
						vecVertColliderToEmitter = _oFlexEmitter.transform.position - vecVert;
						float nDistEmitterSqr = vecVertColliderToEmitter.sqrMagnitude;
						if (nDistEmitterSqr > _oFlexParamsFluid._nFluidGrp_PlaneDetect_MinDistToEmitterSqr)		// Avoid connecting to this collider vert because it is too close to the emitter.  (prevents cum sticking to penis)
							aSortedColliderVerts.Add(nDistSqr, new CColliderVert(nVert, vecVert, vecNormal, nDistSqr, oBodyBase._oBody._oFlexTriCol_BodyFluid));
					}
				}
			}
		}

		//=== Exit if we could not find enough close vert to form a collision plane (we have hit a non-body collider like the floor or are too far from a body?)
		if (aSortedColliderVerts.Count < 3) {
			_oFlexParamsFluid.STAT_FindPlaneFail_CannotFindThreeVerts++;        //Debug.Log("CFluidParticleGroup.Util_FindClosestPointOnCollider() could find a body collider so we exit.");
			return false;       // Failed finding body collision plane
		}

		//=== Obtain access to the three closest collider vertices so we can form a plane (making sure they are from the SAME body!) ===
		CColliderVert oColliderVert_Closest = aSortedColliderVerts.Values[0];
		_oFlexTriCol_BodyWePinTo = oColliderVert_Closest._oFlexTriCol;     // We commit to using collider verts all on the same body!
		//_aFluidParticlePins[0].AssignToColliderMeshVert(_oFlexTriCol_BodyWePinTo, 0, oColliderVert_Closest);
		_aFluidParticlePins[0] = _oFlexTriCol_BodyWePinTo.GetFluidParticlePin(_oFlexParamsFluid, oColliderVert_Closest._nVertID);
		ushort nSortedArrayIndex = 0;
		for (ushort nVertColliderOrdinal = 1; nVertColliderOrdinal < 3; nVertColliderOrdinal++) {
			do {
				nSortedArrayIndex++;
				if (nSortedArrayIndex >= aSortedColliderVerts.Count) {     //###WEAK
					_oFlexParamsFluid.STAT_FindPlaneFail_CannotFindOnSameBody++;   //Debug.LogErrorFormat("###ERROR: CFluidParticleGroup.Util_FindClosestPointOnCollider() could not find 3 nearby collider verts on the same body!");
					return false;       // Failed finding collision plane
				}
			} while (aSortedColliderVerts.Values[nSortedArrayIndex]._oFlexTriCol != _oFlexTriCol_BodyWePinTo);

			//_aFluidParticlePins[nVertColliderOrdinal].AssignToColliderMeshVert(_oFlexTriCol_BodyWePinTo, nSortedArrayIndex, aSortedColliderVerts.Values[nSortedArrayIndex]);
			_aFluidParticlePins[nVertColliderOrdinal] = _oFlexTriCol_BodyWePinTo.GetFluidParticlePin(_oFlexParamsFluid, aSortedColliderVerts.Values[nSortedArrayIndex]._nVertID);
			nSortedArrayIndex++;
		}

		UpdateParticleGroupTransform();

		//=== Pin each of our particles by forming its spring lengths to each 3 verts of our 'pinning plane' ===
		foreach (CFluidParticle oFluidParticle in _aFluidParticles)
			oFluidParticle.PinParticles();

		return true;       // Succeeded finding collision plane
	}




	public class CColliderVert {               // CColliderVert: Simple short-lived data-storage class storing information on a collider mesh vert used while attempting to find the three closest ones for the 'collision plane'
		public float        _nDistSqr;
		public ushort       _nVertID;
		public Vector3      _vecColliderVert;
		public Vector3      _vecColliderNormal;
		public CFlexTriCol  _oFlexTriCol;

		public CColliderVert(ushort nVertID, Vector3 vecColliderVert, Vector3 vecColliderNormal, float nDistSqr, CFlexTriCol oFlexTriCol) {
			_nVertID            = nVertID;
			_vecColliderVert    = vecColliderVert;
			_vecColliderNormal  = vecColliderNormal;
			_nDistSqr           = nDistSqr;
			_oFlexTriCol        = oFlexTriCol;
		}
	}


	public enum EFluidParticleGroupMode {
		S0_Uninitialized,					// Particle group is unitialized and not testing for collision
		S1_AwaitingFirstCollision,          // The 'master particle' (center particle in first slice) is testing for collision by observing sharp direction changes in its velocity vector
		S2_NotPinnedToCollider,		// Particle group could not find a good body collider to pin to and reached a non-body collider like the floor.  Provide culling only.
		S3_PinnedToCollisionPlane,          // Each particle is 'pinned' to the closest collision triangle it could find on a fluid collider mesh.  Until its destruction it will appear to 'stick' to the body.
		S4_PinnedToStabilizedPos,           // Each particle is 'pinned' to its 'stabilized pinning position' (The position it was found to be at after a number of frames while in mode 'S3_PinnedToCollisionPlane' trying to converge on collision plane.  This mode exists to prevent contention / fluid instability as many particles attempt to travel to the pinning plane.  This is the last mode a particle group can be on.
	}
}



public class CFluidParticle {
	public CFluidParticleGroup		_oFluidParticleGroup;
	public uint						_nParticleID;
	public ushort                   _nParticleOrdinalInGroup;
	public byte                     _nFramesWithLowDensity;                 // How many frames this particle has been flagged as low-density.  When this exceeds 'C_FramesWithLowDensity_Cutoff' it gets flagged for destruction
	public bool                     _bIsDrivenByTripleFlexSprings;			// Important flag that means that three Flex springs have been attached between this fluid particle and the three plane verts of the collision plane we've been assigned to stick to.  Makes this fluid particle efficiently 'stick' to the body
	public Vector3                  _vecParticle;
	public float[]                  _aSpringLengths = new float[3];         // The length of the three springs this particle has to the three verts of the pinning plane.  This information is collected at every frame for dynamic Flex spring construction
#if D_ShowDebugVisualization
	public LineRenderer[]           _aLineRenderers_Pins;
	public LineRenderer             _oLineRenderer_ToMaster;
#endif

	public CFluidParticle(CFluidParticleGroup oFluidParticleGroup, Vector3 vecPosSliceCenter, ushort nParticleOrdinalInGroup, ushort nNumParticleThisFrame, float nEmitVelocity_PreviousFrame, float nEmitVelocity_IncrementPerParticle  , ref Matrix4x4 matEmitterL2W) {
		_oFluidParticleGroup		= oFluidParticleGroup;
		_nParticleID				= _oFluidParticleGroup._oFlexParamsFluid.ParticleBank_GetNextParticle();
		_nParticleOrdinalInGroup	= nParticleOrdinalInGroup;

		//=== Determine the particle position in local (to emitter) and global coordinates ===
		Vector3 vecPosThisParticle_Local = new Vector3();
		vecPosThisParticle_Local.x = vecPosSliceCenter.x + _oFluidParticleGroup._oFlexEmitter._aParticleSlicePositions2D[nParticleOrdinalInGroup].x;
		vecPosThisParticle_Local.y = vecPosSliceCenter.y + _oFluidParticleGroup._oFlexEmitter._aParticleSlicePositions2D[nParticleOrdinalInGroup].y;
		vecPosThisParticle_Local.z = vecPosSliceCenter.z;
		Vector3 vecPosThisParticle_Global = matEmitterL2W.MultiplyPoint(vecPosThisParticle_Local);       //###OPT: Must we really matrix multiply every point?  Can speed up?

		//=== Activate the new particle ===
		_oFluidParticleGroup._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particlesActivity[_nParticleID] = true;
		_oFluidParticleGroup._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].pos = vecPosThisParticle_Global;
		_oFluidParticleGroup._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].invMass = 1.0f / _oFluidParticleGroup._oFlexParamsFluid._nParticleMass;
		_oFluidParticleGroup._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_colours[_nParticleID] = CFluidParticleGroup.s_color_S0_Uninitialized;

		//=== Set the particle velocity ===
		float nEmitVelocityThisParticle = nEmitVelocity_PreviousFrame + nEmitVelocity_IncrementPerParticle * nNumParticleThisFrame;
		Vector3 vecVelocityThisParticle = _oFluidParticleGroup._oFlexEmitter.transform.forward * nEmitVelocityThisParticle;
		_oFluidParticleGroup._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_velocities[_nParticleID] = vecVelocityThisParticle;
	}

	public void DoDestroy() {
		_oFluidParticleGroup._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particlesActivity[_nParticleID] = false;					//###INFO: For some reason not de-activating the particles gives a HUGE performance loss.  Can't explain why difference is unreasonably large!
		_oFluidParticleGroup._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].invMass = 0;                     //###INFO: NOTE: SSF Pro Fluid renderer shader cannot read that this particle is de-activated.  Move it out of the way so it does not render in the scene  So... setting to kinematic so they don't explode in parking space
		_oFluidParticleGroup._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].pos = _oFluidParticleGroup._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_restParticles[_nParticleID].pos;		// Return the particle to its 'parking space'
#if D_ShowDebugVisualization
		_oFluidParticleGroup._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_colours[_nParticleID] = CFluidParticleGroup.s_color_S0_Uninitialized;
		if (_aLineRenderers_Pins != null) {
			for (int nVertPinPlane = 0; nVertPinPlane < 3; nVertPinPlane++)
				GameObject.Destroy(_aLineRenderers_Pins[nVertPinPlane].gameObject);
		}
		if (_oLineRenderer_ToMaster)
			GameObject.Destroy(_oLineRenderer_ToMaster.gameObject);
#endif
	}

	public bool InspectParticle() {        // Returns true if particle should be culled (too isolated for too long)
		_vecParticle = _oFluidParticleGroup._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].pos;
#if D_ShowDebugVisualization
		if (_bIsDrivenByTripleFlexSprings && _oFluidParticleGroup._oFlexParamsFluid.D_ShowParticles_ShowPinLines) {
			if (_aLineRenderers_Pins == null) {
				_aLineRenderers_Pins = new LineRenderer[3];
				for (int nVertPinPlane = 0; nVertPinPlane < 3; nVertPinPlane++)
					_aLineRenderers_Pins[nVertPinPlane] = CGame.Line_Add("FPPin" + _nParticleID.ToString() + "-" + nVertPinPlane.ToString(), Color.yellow, 0.0005f, 0.0005f);
			}
			for (int nVertPinPlane = 0; nVertPinPlane < 3; nVertPinPlane++) {
				_aLineRenderers_Pins[nVertPinPlane].SetPosition(0, _vecParticle);
				_aLineRenderers_Pins[nVertPinPlane].SetPosition(1, _oFluidParticleGroup._aFluidParticlePins[nVertPinPlane]._vecVertPos);
				//_aLineRenderers_Pins[nVertPinPlane].SetPosition(1, _oFluidParticleGroup._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_oFluidParticleGroup._aPinPlaneVerts[nVertPinPlane]._oFluidParticlePin._nParticleID].pos);
			}
		}
#endif
		//=== Analyze particle density to cull those that are isolated for too long ===
		if (_oFluidParticleGroup._eFluidParticleGroupMode != CFluidParticleGroup.EFluidParticleGroupMode.S4_PinnedToStabilizedPos) {
			if (_oFluidParticleGroup._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_densities[_nParticleID] <= _oFluidParticleGroup._oFlexParamsFluid._nParticleCulling_ThresholdDensity) {                 // Destroy isolated particles (zero density) as they look bad in fancy shader!
				if (_nFramesWithLowDensity++ > _oFluidParticleGroup._oFlexParamsFluid._nParticleCulling_LowDensityParticles_FrameCutoff)        // Increment and flag for destruction when flagged too many times.
					return true;
			} else {
				_nFramesWithLowDensity = 0;     // If a particle is not low density at any given frame we reset the counter.  (The low-density frames must be consecutive for culling to occur)
			}
		}
		return false;
	}

	public void PinParticles(bool bUseCurrentPosition = false) {          // Pin this particle by setting the length of its three springs in relation to the three verts of our 'pinning plane'
		_vecParticle = _oFluidParticleGroup._oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_nParticleID].pos;
		Vector3 vecPosPinning;

		if (bUseCurrentPosition == true)
			vecPosPinning = _vecParticle;
		else
			vecPosPinning = Math3d.ProjectPointOnPlane(_oFluidParticleGroup._vecPlaneNormal, _oFluidParticleGroup._aFluidParticlePins[0]._vecVertPos, _vecParticle);        // Calculate the closest point on the collision plane: Where the particle should go to be right on the pinning plane. (the three spring lenghts are set to move it to this position)

		//=== Set the triple spring length to nudge the particle toward its desired point
		for (int nVertPinPlane = 0; nVertPinPlane < 3; nVertPinPlane++) {
			Vector3 vecDelta = vecPosPinning - _oFluidParticleGroup._aFluidParticlePins[nVertPinPlane]._vecVertPos;
			_aSpringLengths[nVertPinPlane] = vecDelta.magnitude;
		}
			
		//=== Flag this particle as 'pinned'.  It requires no further processing to move to where we want it... Flex does the work (much more efficiently than we could by direct kinematic moves at each frame) ===
		_bIsDrivenByTripleFlexSprings = true;			// Flag this particle as 'driven by its triple springs'.  Fluid runtime will add three springs to this particle at every frame to hold it where we want it in relation to the body
	}
}

//--------------- Debug lines showing relationship between the particles (links to 'master particle')
//if (_oLineRenderer == null && _oFluidParticleGroup._oFlexParamsFluid.D_ShowParticles_Debug)
//	_oLineRenderer = CGame.Line_Add("FPM" + _nParticleID.ToString(), Color.grey, 0.0005f, 0.0005f);
//if (_oLineRenderer) {
//	_oLineRenderer.SetPosition(0, _oFluidParticleGroup._oFluidParticle_Master._vecParticle);
//	_oLineRenderer.SetPosition(1, _vecParticle);
//}
