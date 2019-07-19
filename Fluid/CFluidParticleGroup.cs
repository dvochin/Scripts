/*###DOCS24: Sept 2017 - Fluid particle group: 
=== DEV ===

=== NEXT ===
- Reject collision planes that are too far?
- Have gradual strengthening of springs?
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
- Particles fighting against one another... implement gradual strengthening of springs so newer particles mold against older ones!
	- Perform gradual updates of the spring length as particle ages (to reduce particle fighting)
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
- Fluid reaching ground dissipates right away!  Can improve some setting?  Can reset velocity so it sticks together?
- Fluid particles can start 'dancing' when body moves... how come not culled??  (Because of consecutive requirement??)

=== WISHLIST ===

*/

//#define D_ShowDebugVisualization

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;

public class CFluidParticleGroup : MonoBehaviour {                  // CFluidParticleGroup: Responsible for 'grouping' particles around an 'Master particle' to 1) greatly reducing their velocity upon first collision, 2) move these particles toward the collision plane for a while and 3) set the particles as kinematic and move them with the collider plane
	public CFlexEmitter             _oFlexEmitter;
	public CFlexParamsFluid			_oFlexParamsFluid;
	public CFlexTriCol              _oFlexTriCol_BodyWePinTo;

	public List<CFluidParticle>     _aFluidParticles	= new List<CFluidParticle>();	// All our particles.  The 'master particle' is always at index 0 (relevant when detecting collisions only)
	public CFluidParticlePin[]		_aFluidParticlePins = new CFluidParticlePin[3];		// Identifies the three verts forming a 'pinning plane' on the body collider we stick to.
	List<CFluidParticle>			_aParticlesToCull	= new List<CFluidParticle>();	// Particles to cull at each frame (kept so we don't allocate at each frame)

    public Vector3[]                _aVertsCollisionTri = new Vector3[4];				// Array of pin positions (used for debug visualization only)
	public CFluidParticle			_oFluidParticle_LastCenterParticle;     // The last emitted center particle (center one in each slice) of the last slice emitted.  Needed by emitter to seamlessly connect particle groups together

	Vector3                         _vecParticleVelocity_Now;		// The observed particle's velocity vector in this frame.
	Vector3                         _vecParticleVelocity_Prev;      // The observed particle's velocity vector in the previous frame.  Radical changes in direction will cause every particle in this emit group to be greatly slowed-down to prevent fluid bouncing off collider!
    Vector3                         _vecParticleCollisionPoint;     // The point of collision where the master particle collided with the mesh collider.  Used to set spring lengths so all particles converge toward that spot
    
	public Vector3                  _vecPlaneCenter;				// The center of the plane we're colliding against.		###OBS??
	public Vector3                  _vecPlaneNormal;				// The normal of the plane we're colliding against.  At every frame we set our velocity in the reverse direction of the normal to 'push' our group of particles to 'stick' against the collision plane we selected during the initial collision

	public EFluidParticleGroupMode  _eFluidParticleGroupMode	= EFluidParticleGroupMode.S0_Uninitialized;     // The state of this group of emitter particles
	public ushort					_nNumParticlesInGroup;          // The number of particles in our 'emit group'.  We slow down all particles in our group in the same operation.
            Ray                     _oRay = new Ray();
            RaycastHit              _oRayHit;

    public int                      _nFramesInState;				// Frame counter used to 'delay' progression from some states to their next one.
	static uint                     _nFluidParticleGroupNext;
	Quaternion						_quatRotation = new Quaternion();		// To avoid creating each frame

	LineRenderer					_oLineRenderer_Normal;                  //###IDEA: Consider GL calls to directly draw procedurally-created meshes??
	LineRenderer					_oLineRenderer_CollisionPlane;

    public static Color s_color_SX_Inactive                     = Color.grey;
    public static Color s_color_S0_Uninitialized				= Color.white;
	public static Color s_color_S1_AwaitingFirstCollision		= Color.green;
	public static Color s_color_S3_PinnedToCollisionPlane		= Color.cyan;
	public static Color s_color_S2_NotPinnedToCollider	        = G.C_Color_RedDark;
	public static Color s_color_PinParticle                     = Color.magenta;
	public static Color s_color_MasterParticle                  = Color.red;
	public static Color s_color_BakedToColliders                = Color.blue;
    public static Color s_color_Kinematic                       = G.C_Color_BlueDark;


    public static CFluidParticleGroup Create(CFlexEmitter oFlexEmitter) {
        CFluidParticleGroup oFluidParticleGroup = CUtility.InstantiatePrefab<CFluidParticleGroup>("Prefabs/CFluidParticleGroup", "CFluidParticleGroup" + _nFluidParticleGroupNext++.ToString(), oFlexEmitter._oFlexParamsFluid.transform);
#if D_ShowDebugVisualization
        oFluidParticleGroup.GetComponent<MeshRenderer>().enabled = true;
#else
        oFluidParticleGroup.GetComponent<MeshRenderer>().enabled = false;
#endif
        return oFluidParticleGroup;
	}

	public bool EmitParticles(CFlexEmitter oFlexEmitter, ushort nSlices, Vector3 vecPosEmitterStart, Vector3 vecDistanceBetweenSlices, float nEmitVelocity_IncrementPerParticle, float nEmitVelocity_PreviousFrame) {
		_oFlexEmitter = oFlexEmitter;
		_oFlexParamsFluid = oFlexEmitter._oFlexParamsFluid;
		_nNumParticlesInGroup = (ushort)(nSlices * _oFlexParamsFluid._nParticlesPerSlice);
        if (_nNumParticlesInGroup > _oFlexParamsFluid.ParticleBank_GetNumParticlesAvailable()) {
            Debug.LogWarningFormat("#WARNING: CFluidParticleGroup.EmitParticles() needed {0} particles but only {1} are available!", _nNumParticlesInGroup, _oFlexParamsFluid.ParticleBank_GetNumParticlesAvailable());
            return false;
        }

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
        return true;
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

	public EParticleGroupFrameAction FluidParticleGroup_UpdateParticles() {    // Update all particles from our group.  Depending on our state (un-collided, collided, pinned to pin plane) we modify the state of our particles as appropriate
        //=== Update the position of our slave particles ===
        foreach (CFluidParticle oFluidParticle in _aFluidParticles) {
            bool bShouldBeCulled = oFluidParticle.InspectParticle();
            //if (bShouldBeCulled) {
            //    if (oFluidParticle._nParticleOrdinalInGroup != 0 || _eFluidParticleGroupMode != EFluidParticleGroupMode.S1_AwaitingFirstCollision)      // Avoid culling master particle when detecting collision as it's the one being polled for angle changes!
            //        _aParticlesToCull.Add(oFluidParticle);
            //}
        }

  //      //=== Destroy particles that are flagged for culling (isolated) ===
  //      foreach (CFluidParticle oFluidParticle in _aParticlesToCull) {
  //          _aFluidParticles.Remove(oFluidParticle);
  //          oFluidParticle.DoDestroy();
  //          _oFlexParamsFluid.STAT_FluidParticlesCulled++;
  //      }
  //      _aParticlesToCull.Clear();

		////=== Don't do anything if we have no active particles left ===
		//if (_aFluidParticles.Count == 0)
		//	return EParticleGroupFrameAction.Destroy;           // We have no particles left so we can be destroyed without baking

        //=== Perform state-dependant processing ===
        EParticleGroupFrameAction eParticleGroupFrameAction = EParticleGroupFrameAction.KeepSimulating;     // This particle group will keep sending 'keep simulating' unless it decides to bake itself

        switch (_eFluidParticleGroupMode) {
			case EFluidParticleGroupMode.S0_Uninitialized:              // Particle group is uninitialized (for only one frame).  Record the 'previous frame velocity' and switch to 'awaiting first collision'
				_vecParticleVelocity_Now = _oFlexParamsFluid._oFlexParamsFluidParticleBank.m_velocities[_aFluidParticles[0]._nParticleID];
				_vecParticleVelocity_Prev = _vecParticleVelocity_Now;
				Util_AdjustParticle_Colors(s_color_S1_AwaitingFirstCollision);
				_eFluidParticleGroupMode = EFluidParticleGroupMode.S1_AwaitingFirstCollision;
				break;

			case EFluidParticleGroupMode.S1_AwaitingFirstCollision:     // The 'master particle' (center particle in first slice) is testing for collision by observing sharp direction changes in its velocity vector.  Particle is in emit stream and has yet to collide with anything.  (We detect collisions by changes in direction of the Master particle velocity vector)
				//Vector3 vecPos = _oFlexParamsFluid._oFlexParamsFluidParticleBank.m_particles[_aFluidParticles[0]._nParticleID].pos;
                Vector3 vecPosMasterParticle = _aFluidParticles[0]._vecParticle;
                if (vecPosMasterParticle.y <= 0.0005f)
                    return EParticleGroupFrameAction.Destroy;           // We have no particles left so we can be destroyed without baking
                _vecParticleVelocity_Now = _oFlexParamsFluid._oFlexParamsFluidParticleBank.m_velocities[_aFluidParticles[0]._nParticleID];
                Profiler.BeginSample("X_Fluid_Group_Raycast");
                _oRay.origin     = vecPosMasterParticle;
                _oRay.direction  = _vecParticleVelocity_Now;
                bool bHit = Physics.Raycast(_oRay, out _oRayHit, CGame.INSTANCE._nSomeHackValue1_HACK, G.C_LayerMask_BodySurface);      //###IMPROVE: Add all bodies and hands and static colliders but must add capacity for multiple surface meshes throughout!!
                Profiler.EndSample();
                if (bHit) {
                    Collider oColFound = _oRayHit.collider;
                    if (_oRayHit.triangleIndex != -1) {
                        //_oSurfacePath.AddPoint(oRayHit.triangleIndex, oRayHit.barycentricCoordinate);     //#DEV26: Use barycentric for weights and springs?
					    Util_AdjustParticle_Velocities_Multiply(_oFlexParamsFluid._nFluidGrp_SlowdownOnInitialCollision);   // Greatly slow down our group of particles.  (If we didn't they would probably bounce off the collider as fluid viscosity & adhesion forces are FAR too small!)
                        _oFlexTriCol_BodyWePinTo = oColFound.gameObject.GetComponent<CFlexTriCol>();        //#DEV26: Hack on what fluid pins to
                        if (_oFlexTriCol_BodyWePinTo) { 
                            _vecParticleCollisionPoint = _oRayHit.point;
                            Mesh oMeshBaked = _oFlexTriCol_BodyWePinTo.Baking_GetBakedSkinnedMesh();
                            int[] aTris = oMeshBaked.triangles;
                            int nTriX3 = _oRayHit.triangleIndex * 3;
                            ushort nVert0 = (ushort)aTris[nTriX3 + 0];
                            ushort nVert1 = (ushort)aTris[nTriX3 + 1];
                            ushort nVert2 = (ushort)aTris[nTriX3 + 2];
                            _aFluidParticlePins[0] = _oFlexTriCol_BodyWePinTo.FluidParticlePins_GetPin(_oFlexParamsFluid, nVert0, nVert1);
                            _aFluidParticlePins[1] = _oFlexTriCol_BodyWePinTo.FluidParticlePins_GetPin(_oFlexParamsFluid, nVert1, nVert2);
                            _aFluidParticlePins[2] = _oFlexTriCol_BodyWePinTo.FluidParticlePins_GetPin(_oFlexParamsFluid, nVert2, nVert0);

                            //=== Calculate the collision plane so we can flatten our particles ===
                            UpdateParticleGroupPositionAndRotation();       //#Fluid ParGrp FSM

                            //=== Pin every few particles to the pinning plane so the others are brought closer to our collider in a fluid realistic way ===
                            int nParticle = 0;
                            foreach (CFluidParticle oFluidParticle in _aFluidParticles) {
                                if ((nParticle++ % CGame.INSTANCE._nFluidPin_NumParticlesBetweenPins) == 0)
                                    oFluidParticle.AddSpringsBetweenParticleAndTriplePins();        // Because of the springs between consecutive particles, pinning just a few particles will pin the whole fluid!
                            }

                            Util_AdjustParticle_Colors(s_color_S3_PinnedToCollisionPlane);
                            _eFluidParticleGroupMode = EFluidParticleGroupMode.S3_PinnedToCollisionPlane;
                            _nFramesInState = 0;
                        } else {
                            CUtility.ThrowException("###EXCEPTION: CFluidParticleGroup.UpdateParticle() could not find CFlexTriCol component on collider!!");
                        }
                    } else {
                        CUtility.ThrowException("###EXCEPTION: CFluidParticleGroup.UpdateParticle() got a collision hit with no triangle index!!");
                    }
                }
				_vecParticleVelocity_Prev = _vecParticleVelocity_Now;
				break;

			case EFluidParticleGroupMode.S2_NotPinnedToCollider:      // Particle group could not find a good body collider to pin to and reached a non-body collider like the floor.  Provide culling only.
				// Nothing to do here.  We just provide culling services.       #DEV26: Remove entire group?
				break;

			case EFluidParticleGroupMode.S3_PinnedToCollisionPlane:     // Each particle is 'pinned' to the closest collision triangle it could find on a fluid collider mesh.  Until its destruction it will appear to 'stick' to the body.
				if (CGame.INSTANCE._bFluidBake_DoBakeFluid) { 
                    //if (_nFramesInState > _oFlexParamsFluid._nFluidGrp_NumFramesStabilizingBeforeBaking)      // Allow particles to 'stabilize' to its final pinning position before we re-pin it to where it is at a certain frame threshold
                    //    eParticleGroupFrameAction = EParticleGroupFrameAction.KeepSimulating;
                    if (_nFramesInState >= _oFlexParamsFluid._nFluidGrp_NumFramesStabilizingBeforeBaking)      // Allow particles to 'stabilize' to its final pinning position before we re-pin it to where it is at a certain frame threshold
                        eParticleGroupFrameAction = EParticleGroupFrameAction.BakeAndDestroy;         // We are done compressing particles against our collider plane and are ready to 'bake' our particles so they continue to live a non-simulated particles statically glued to bodies.  (far higher performance!)
                    _nFramesInState++;
                }
                break;
		}


#if D_ShowDebugVisualization
		if (_oFlexParamsFluid.D_ShowParticles_Debug && _eFluidParticleGroupMode >= EFluidParticleGroupMode.S3_PinnedToCollisionPlane) {
			//UpdateParticleGroupTransform();

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
        return eParticleGroupFrameAction;

    }

	void UpdateParticleGroupPositionAndRotation() {
		//=== Calculate our collision plane center & normal from the average of our three points ===	// Note: It is guaranteed that all CFluidParticlePins have been updated just before.  We can reliably just fetch those we need.
		_vecPlaneCenter = (_aFluidParticlePins[0]._vecVertPos    + _aFluidParticlePins[1]._vecVertPos    + _aFluidParticlePins[2]._vecVertPos)    / 3;
		_vecPlaneNormal = (_aFluidParticlePins[0]._vecVertNormal + _aFluidParticlePins[1]._vecVertNormal + _aFluidParticlePins[2]._vecVertNormal) / 3;  //###OPT:!! Just take normal of our first pin for speed?
        _vecPlaneCenter += _vecPlaneNormal * CGame.INSTANCE._nSomeHackValue3_HACK;      // Push the collision plane up by the particle distance so springs don't attempt to drive particles into / past the collision plane

#if D_ShowDebugVisualization        //=== Set the pin position and rotation ===
        transform.position = _vecPlaneCenter;
        Vector3 vecCenterToPt0 = _aFluidParticlePins[0]._vecVertPos - _vecPlaneCenter;
		_quatRotation.SetLookRotation(_vecPlaneNormal, vecCenterToPt0);
		transform.rotation = _quatRotation;
#endif
    }

    void Util_AdjustParticle_Velocities_Multiply(float nVelocityMultiplier) {
		foreach (CFluidParticle oFluidParticle in _aFluidParticles)
			_oFlexParamsFluid._oFlexParamsFluidParticleBank.m_velocities[oFluidParticle._nParticleID] *= nVelocityMultiplier;
	}

	void Util_AdjustParticle_Colors(Color colorBase) {
        //#if D_ShowDebugVisualization
        const int C_ColorRandomization = 128;
        if (CGame._oFlexParamsFluid.D_ShowParticles_Debug) { 
		    Color32 colorRandomized = colorBase;     // Perform small channel randomization so we can tell emit groups apart when visualizing particles during debugging
		    if (colorRandomized.r == 0)
			    colorRandomized.r = (byte)(CGame._oRnd.Next() % C_ColorRandomization);
		    if (colorRandomized.g == 0)
			    colorRandomized.g = (byte)(CGame._oRnd.Next() % C_ColorRandomization);
		    if (colorRandomized.b == 0)
			    colorRandomized.b = (byte)(CGame._oRnd.Next() % C_ColorRandomization);

		    foreach (CFluidParticle oFluidParticle in _aFluidParticles)
			    _oFlexParamsFluid._oFlexParamsFluidParticleBank.m_colours[oFluidParticle._nParticleID] = colorRandomized;
		    _oFlexParamsFluid._oFlexParamsFluidParticleBank.m_colours[_aFluidParticles[0]._nParticleID] = s_color_MasterParticle;		// First particle is the master one and is colored differently
        }
        //#endif
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
		S0_Uninitialized,					// Particle group is initialized and not testing for collision
		S1_AwaitingFirstCollision,          // The 'master particle' (center particle in first slice) is testing for collision by observing sharp direction changes in its velocity vector
		S2_NotPinnedToCollider,		        // Particle group could not find a good body collider to pin to and reached a non-body collider like the floor.  Provide culling only.
		S3_PinnedToCollisionPlane,          // Each particle is 'pinned' to the closest collision triangle it could find on a fluid collider mesh.  Until its destruction it will appear to 'stick' to the body.
		//S4_AwaitingBakingAndDestruction,    // This particle group no longer has a purpose and we are awaiting destruction.  The particles close to bodies may be 'baked' into CFluidBaked and will appear to 'live on' as fake particle via a fluid rendering trick
	}

    public enum EParticleGroupFrameAction {
        KeepSimulating,                     // Particle group should keep simulating (e.g. still need collision processing)
        BakeAndDestroy,                     // Particle group should be 'baked' to skinned bodies / static objects for greatly-enhanced fluid performance
        Destroy,                            // Particle group should be destroyed.  No particle is worth baking
    }
}




//public bool PinToClosestCollisionPlane() {          // Find the closest three verts on the collider, determine its plane and find the closest point on that plane to our Master particle
//	//=== Iterate through the fluid mesh collider to add the neighboring verts ===
//	SortedList<float, CColliderVert> aSortedColliderVerts = new SortedList<float, CColliderVert>();        //###INFO: How to keep a collection sorted = easy!
//	Vector3 vecPosMaster = _aFluidParticles[0]._vecParticle;
//	Vector3 vecPosEmitter = _oFlexEmitter.transform.position;
//	Vector3 vecVertColliderToMasterParticle;
//	//Vector3 vecVertColliderToEmitter;

//	//=== Accumulate in a sorting collection all the collider vertices within a reasonable distance to our master particle.  These will be inspected further to find the collision plane we can stick to ===
//	foreach (CBodyBase oBodyBase in CGame._aBodyBases) {               //###OPT:!!!!! Very expensive to iterate through all collider verts on all body!  Can redesign?  Can reduce frequency of this full check?
//           Mesh oMeshBaked = oBodyBase._oBody._oFlexTriCol_BodySurface.Baking_GetBakedSkinnedMesh();
//           Vector3[] aVerts	= oMeshBaked.vertices;
//		Vector3[] aNormals	= oMeshBaked.normals;

//		//if (aNormals.Length > 0) {			//###BROKEN:!!!! mesh no longer has normals!  WTF??
//		ushort nVerts = (ushort)aVerts.Length;                  //###OPT:!!!! Replace brute search with a fast spacial search tree?
//		for (ushort nVert = 0; nVert < nVerts; nVert++) {       //###IDEA: Use the new 'surface information' to avoid sticking to penis tip?
//			Vector3 vecVert		= aVerts[nVert];
//			Vector3 vecNormal	= aNormals[nVert];
//			vecVertColliderToMasterParticle = vecVert - vecPosMaster;
//			float nDistSqr = vecVertColliderToMasterParticle.sqrMagnitude;
//			if (nDistSqr <= _oFlexParamsFluid._nFluidGrp_PlaneDetect_MinDistToParticleSqr) {			// Only consider collider verts within a reasonable distance (avoids sorting collection becoming too large / slow)
//				//vecVertColliderToEmitter = vecPosEmitter - vecVert;
//				//float nDistEmitterSqr = vecVertColliderToEmitter.sqrMagnitude;        //#DEV26:C
//				//if (nDistEmitterSqr > _oFlexParamsFluid._nFluidGrp_PlaneDetect_MinDistToEmitterSqr)		// Avoid connecting to this collider vert because it is too close to the emitter.  (prevents cum sticking to penis)
//			    aSortedColliderVerts.Add(nDistSqr, new CColliderVert(nVert, vecVert, vecNormal, nDistSqr, oBodyBase._oBody._oFlexTriCol_BodySurface));
//			}
//		}
//		//}
//	}

//	//=== Exit if we could not find enough close vert to form a collision plane (we have hit a non-body collider like the floor or are too far from a body?)
//	if (aSortedColliderVerts.Count < 3) {
//		_oFlexParamsFluid.STAT_FindPlaneFail_CannotFindThreeVerts++;        //Debug.Log("CFluidParticleGroup.Util_FindClosestPointOnCollider() could find a body collider so we exit.");
//		return false;       // Failed finding body collision plane
//	}

//	//=== Obtain access to the three closest collider vertices so we can form a plane (making sure they are from the SAME body!) ===
//	CColliderVert oColliderVert_Closest = aSortedColliderVerts.Values[0];
//	_oFlexTriCol_BodyWePinTo = oColliderVert_Closest._oFlexTriCol;     // We commit to using collider verts all on the same body!
//	//_aFluidParticlePins[0].AssignToColliderMeshVert(_oFlexTriCol_BodyWePinTo, 0, oColliderVert_Closest);
//	_aFluidParticlePins[0] = _oFlexTriCol_BodyWePinTo.FluidParticlePins_GetPin(_oFlexParamsFluid, oColliderVert_Closest._nVertID);
//	ushort nSortedArrayIndex = 0;
//	for (ushort nVertColliderOrdinal = 1; nVertColliderOrdinal < 3; nVertColliderOrdinal++) {
//		do {
//			nSortedArrayIndex++;
//			if (nSortedArrayIndex >= aSortedColliderVerts.Count) {     //###WEAK
//				_oFlexParamsFluid.STAT_FindPlaneFail_CannotFindOnSameBody++;   //Debug.LogErrorFormat("###ERROR: CFluidParticleGroup.Util_FindClosestPointOnCollider() could not find 3 nearby collider verts on the same body!");
//				return false;       // Failed finding collision plane
//			}
//		} while (aSortedColliderVerts.Values[nSortedArrayIndex]._oFlexTriCol != _oFlexTriCol_BodyWePinTo);

//		//_aFluidParticlePins[nVertColliderOrdinal].AssignToColliderMeshVert(_oFlexTriCol_BodyWePinTo, nSortedArrayIndex, aSortedColliderVerts.Values[nSortedArrayIndex]);
//		_aFluidParticlePins[nVertColliderOrdinal] = _oFlexTriCol_BodyWePinTo.FluidParticlePins_GetPin(_oFlexParamsFluid, aSortedColliderVerts.Values[nSortedArrayIndex]._nVertID);
//		nSortedArrayIndex++;
//	}

//       UpdateParticleGroupTransform();

//	//=== Pin each of our particles by forming its spring lengths to each 3 verts of our 'pinning plane' ===
//	foreach (CFluidParticle oFluidParticle in _aFluidParticles)
//		oFluidParticle.AddSpringsBetweenParticleAndTriplePins();

//	return true;       // Succeeded finding collision plane
//}


//} else {
// if (_vecParticleVelocity_Now.y < 0.01f) {       // Particles are very near ground level.  Remove the special processing on them and let them interact with the floor as usual.  ###CHECK: Not providing culling??  To a different mode??
//  Util_AdjustParticle_Colors(s_color_S2_NotPinnedToCollider);
//  _eFluidParticleGroupMode = EFluidParticleGroupMode.S2_NotPinnedToCollider;
//  _oFlexParamsFluid.STAT_FindPlaneFail_NotPinnedToCollider++;
// }
//}




//float nAngleChangeToPreviousFrame = Vector3.Angle(_vecParticleVelocity_Now, _vecParticleVelocity_Prev);
//if (nAngleChangeToPreviousFrame > _oFlexParamsFluid._nFluidGrp_CollisionDetech_AngleChange) {			// Angle of this Master particle has changed too much since last frame = it has hit a collider = greatly reduce the velocity of every particle in this emit group and kill this particle Master (our slow-down work is done)
//	Util_AdjustParticle_Velocities_Multiply(_oFlexParamsFluid._nFluidGrp_SlowdownOnInitialCollision);	// Greatly slow down our group of particles.  (If we didn't they would probably bounce off the collider as fluid viscosity & adhesion forces are FAR too small!)
//	bool bFoundCollisionPlane = PinToClosestCollisionPlane();
//	if (bFoundCollisionPlane) {
//		Util_AdjustParticle_Colors(s_color_S3_PinnedToCollisionPlane);
//		_eFluidParticleGroupMode = EFluidParticleGroupMode.S3_PinnedToCollisionPlane;
//		_nFramesInState = 0;
//	} else {
//		if (_vecParticleVelocity_Now.y < 0.01f) {       // Particles are very near ground level.  Remove the special processing on them and let them interact with the floor as usual.  ###CHECK: Not providing culling??  To a different mode??
//			Util_AdjustParticle_Colors(s_color_S2_NotPinnedToCollider);
//			_eFluidParticleGroupMode = EFluidParticleGroupMode.S2_NotPinnedToCollider;
//			_oFlexParamsFluid.STAT_FindPlaneFail_NotPinnedToCollider++;
//		}
//	}
//}


//foreach (CFluidParticle oFluidParticle in _aFluidParticles)               // Pin our particles to their current position.  This will greatly stabilize the neighboring fluid area!
//    oFluidParticle.ConvertToKinematic();
//oFluidParticle.PinParticles(/*bUseCurrentPosition =*/ true);
//foreach (CFluidParticle oFluidParticle in _aFluidParticles)           // End the dynamic springs keeping these particles as close as possible to their collision plane
//    oFluidParticle._bIsDrivenByTripleFlexSprings = false;
//Util_AdjustParticle_Colors(s_color_S4_AwaitingBakingAndDestruction);
//_eFluidParticleGroupMode = EFluidParticleGroupMode.S4_AwaitingBakingAndDestruction;




//case EFluidParticleGroupMode.S4_AwaitingBakingAndDestruction:      // Each particle is 'pinned' to its 'stabilized pinning position' (The position it was found to be at after a number of frames while in mode 'S3_PinnedToCollisionPlane' trying to converge on collision plane.  This mode exists to prevent contention / fluid instability as many particles attempt to travel to the pinning plane.  This is the last mode a particle group can be on.
//UpdateParticleGroupTransform();     //#DEV26:
//foreach (CFluidParticle oFluidParticle in _aFluidParticles)                // Pin our particles to their current position.  This will greatly stabilize the neighboring fluid area!
//    oFluidParticle.UpdateKinematicPosition();
//if (_nFramesInState++ % _oFlexParamsFluid._nFluidGrp_NumFramesStabilizingToFinalPos == 0) {      // Periodically re-adjust the springs to the current particle position, further stabilizing the fluid as it ages...
//	foreach (CFluidParticle oFluidParticle in _aFluidParticles)			//###IMPROVE: Not working!  Moves some disconnected parts of the fluid for some reason!
//		oFluidParticle.PinParticles(/*bUseCurrentPosition =*/ true);
//}
//break;



