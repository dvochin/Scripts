/*###DISCUSSION: CHeadLook
=== NEXT ===

=== TODO ===

=== LATER ===

=== IMPROVE ===
 * Prevent looking at a look target if outside reasonable vision cone?
 * Look at camera once in a while?

=== DESIGN ===

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===

=== PROBLEMS??? ===

=== WISHLIST ===

*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class CHeadLook : MonoBehaviour {		//###INFO: Based on code from http://wiki.unity3d.com/index.php?title=CHeadLook.  (Modified to look at different bones of parter's body at random intervals)

	CBody				_oBody;									//###MOD: Extra members to autolook different bones of other body over time.
	List<Transform>		_aLookTargets = new List<Transform>();	// List of transforms made of bones from other body we look at at random
	Vector3				_vecRandomShift;						// Small shift applied to a look target to prevent staring at same spot too long
	float				_nTimeNextChangeLookTarget = -1;		// Time when a next look target will be selected at random
	float				_nTimeNextChangeRandomShift = -1;		// Time when _vecRandomShift changed to a next shift

	const int			C_TimeBetweenLookChangeLow	= 5;		// Time range to choose a different look target
	const int			C_TimeBetweenLookChangeHigh = 10;		//###TUNE
	const float			C_RandomShiftAmount = 0.02f;			// Amound of shift on x,y,z to _vecRandomShift	###TUNE


	public Transform rootNode;
	public BendingSegment[] segments = new BendingSegment[1];
	public NonAffectedJoints[] nonAffectedJoints = new NonAffectedJoints[0];
	public Vector3 headLookVector = Vector3.forward;
	public Vector3 headUpVector = Vector3.up;
	//public Vector3 target = Vector3.zero;			//###MOD: Substituded global vector for a global node that is moved around
	public Transform _oNodeLookTarget;
	public float effect = 1;
	public bool overrideAnimation = true;			//###CHECK!!!!


	public void OnStart(CBody oBody) {
		_oBody = oBody;						//###MOD: Init segment from code as our component is added at runtime now
		BendingSegment oSeg = new BendingSegment();

		oSeg.firstTransform		= _oBody._oBodyBase.FindBone("chest/neck");			//###IMPROVE?  Can spine be added (or would interfere with our anim too much?)
		oSeg.lastTransform		= _oBody._oBodyBase.FindBone("chest/neck/head");
		oSeg.thresholdAngleDifference = 0;			//###???
		oSeg.bendingMultiplier = 1.0f;				//###IMPROVE!!!: Add 'eyes' and get them to move part of the way
		oSeg.maxAngleDifference = 90;
		oSeg.maxBendingAngle = 40;					//###IMPROVE: Reduce angle for head up / down and wider for head left/right
		oSeg.responsiveness = 3.0f;					//###TUNE!!!
		segments[0] = oSeg;

		if (rootNode == null)
			rootNode = transform;

		// Setup segments
		foreach (BendingSegment segment in segments) {
			Quaternion parentRot = segment.firstTransform.parent.rotation;
			Quaternion parentRotInv = Quaternion.Inverse(parentRot);
			segment.referenceLookDir =
				parentRotInv * rootNode.rotation * headLookVector.normalized;
			segment.referenceUpDir =
				parentRotInv * rootNode.rotation * headUpVector.normalized;
			segment.angleH = 0;
			segment.angleV = 0;
			segment.dirUp = segment.referenceUpDir;

			segment.chainLength = 1;
			Transform t = segment.lastTransform;
			while (t != segment.firstTransform && t != t.root) {
				segment.chainLength++;
				t = t.parent;
			}

			segment.origRotations = new Quaternion[segment.chainLength];
			t = segment.lastTransform;
			for (int i=segment.chainLength - 1; i >= 0; i--) {
				segment.origRotations[i] = t.localRotation;
				t = t.parent;
			}
		}
	}

	public void AddLookTargetsToOtherBody(CBody oBodyOther) {			// Send by CGamePlay when scene gets a body added / removed
		//###IMPROVE: Have a special 'object of interest' object that overrides... such as end of penis when cumming

		_aLookTargets.Clear();

		_aLookTargets.Add(Camera.main.transform);			// We always add the camera		//###DESIGN???

		if (oBodyOther != null) {			//###IMPROVE?? Add parts of self body to look at like penis tip?
			_aLookTargets.Add(oBodyOther._oBodyBase.FindBone("chest/neck/head"));
			_aLookTargets.Add(oBodyOther._oBodyBase.FindBone("chest/abdomen"));
			_aLookTargets.Add(oBodyOther._oBodyBase.FindBone("chest/abdomen/hip"));
			_aLookTargets.Add(oBodyOther._oBodyBase.FindBone("chest/abdomen/hip/sex"));
			//_aLookTargets.Add(oBodyOther.FindBone("chest/lCollar/lShldr/lForeArm/lHand"));		//###DESIGN???
			//_aLookTargets.Add(oBodyOther.FindBone("chest/rCollar/rShldr/rForeArm/rHand"));
		}
		_nTimeNextChangeLookTarget = -1;		// Force an immediate refresh of node to look at
	}

	public void OnSimulate() {		//###OPT!!!: Not needed to run every frame!!
		//if (Time.deltaTime == 0)
		//	return;

		if (_aLookTargets.Count == 0) 		// Done here for late initialization... can be improved?
			return;							//###CHECK: Reset head look to zero??

		//=== Change look target if the time has come to do so ===
		float nTime = Time.time;
		if (nTime > _nTimeNextChangeLookTarget) {
			_nTimeNextChangeLookTarget = nTime + CGame.INSTANCE._oRnd.Next(C_TimeBetweenLookChangeLow, C_TimeBetweenLookChangeHigh);
			int nNextLookTarget = CGame.INSTANCE._oRnd.Next(_aLookTargets.Count-1);		//###CHECK: Can get last item??
			_oNodeLookTarget = _aLookTargets[nNextLookTarget];		//###IMPROVE: Prevent going back to same?	//###IMPROVE: Don't select next look targets that are currently not within a reasonable angle
		}

		//=== Change random shift if the time has come to do so ===
		if (nTime > _nTimeNextChangeRandomShift) {
			_nTimeNextChangeRandomShift = nTime + CGame.INSTANCE._oRnd.Next(1, 2);
			_vecRandomShift = new Vector3((float)CGame.INSTANCE._oRnd.NextDouble() * C_RandomShiftAmount, (float)CGame.INSTANCE._oRnd.NextDouble() * C_RandomShiftAmount, (float)CGame.INSTANCE._oRnd.NextDouble() * C_RandomShiftAmount);
		}


		//###MOD: Rest of code from Unity's head look controller...
		// Remember initial directions of joints that should not be affected
		Vector3[] jointDirections = new Vector3[nonAffectedJoints.Length];
		for (int i=0; i < nonAffectedJoints.Length; i++) {
			foreach (Transform child in nonAffectedJoints[i].joint) {
				jointDirections[i] = child.position - nonAffectedJoints[i].joint.position;
				break;
			}
		}

		// Handle each segment
		foreach (BendingSegment segment in segments) {
			Transform t = segment.lastTransform;
			if (overrideAnimation) {
				for (int i=segment.chainLength - 1; i >= 0; i--) {
					t.localRotation = segment.origRotations[i];
					t = t.parent;
				}
			}

			Quaternion parentRot = segment.firstTransform.parent.rotation;
			Quaternion parentRotInv = Quaternion.Inverse(parentRot);

			// Desired look direction in world space
			//###MOD Vector3 lookDirWorld = (target - segment.lastTransform.position).normalized;
			Vector3 lookDirWorld = (_oNodeLookTarget.position + _vecRandomShift - segment.lastTransform.position).normalized;
			
			// Desired look directions in neck parent space
			Vector3 lookDirGoal = (parentRotInv * lookDirWorld);

			// Get the horizontal and vertical rotation angle to look at the target
			float hAngle = AngleAroundAxis(
				segment.referenceLookDir, lookDirGoal, segment.referenceUpDir
			);

			Vector3 rightOfTarget = Vector3.Cross(segment.referenceUpDir, lookDirGoal);

			Vector3 lookDirGoalinHPlane =
                lookDirGoal - Vector3.Project(lookDirGoal, segment.referenceUpDir);

			float vAngle = AngleAroundAxis(
				lookDirGoalinHPlane, lookDirGoal, rightOfTarget
			);

			// Handle threshold angle difference, bending multiplier,
			// and max angle difference here
			float hAngleThr = Mathf.Max(
				0, Mathf.Abs(hAngle) - segment.thresholdAngleDifference
			) * Mathf.Sign(hAngle);

			float vAngleThr = Mathf.Max(
				0, Mathf.Abs(vAngle) - segment.thresholdAngleDifference
			) * Mathf.Sign(vAngle);

			hAngle = Mathf.Max(
				Mathf.Abs(hAngleThr) * Mathf.Abs(segment.bendingMultiplier),
				Mathf.Abs(hAngle) - segment.maxAngleDifference
			) * Mathf.Sign(hAngle) * Mathf.Sign(segment.bendingMultiplier);

			vAngle = Mathf.Max(
				Mathf.Abs(vAngleThr) * Mathf.Abs(segment.bendingMultiplier),
				Mathf.Abs(vAngle) - segment.maxAngleDifference
			) * Mathf.Sign(vAngle) * Mathf.Sign(segment.bendingMultiplier);

			// Handle max bending angle here
			hAngle = Mathf.Clamp(hAngle, -segment.maxBendingAngle, segment.maxBendingAngle);
			vAngle = Mathf.Clamp(vAngle, -segment.maxBendingAngle, segment.maxBendingAngle);

			Vector3 referenceRightDir =
                Vector3.Cross(segment.referenceUpDir, segment.referenceLookDir);

			// Lerp angles
			segment.angleH = Mathf.Lerp(
				segment.angleH, hAngle, Time.deltaTime * segment.responsiveness
			);
			segment.angleV = Mathf.Lerp(
				segment.angleV, vAngle, Time.deltaTime * segment.responsiveness
			);

			// Get direction
			lookDirGoal = Quaternion.AngleAxis(segment.angleH, segment.referenceUpDir)
				* Quaternion.AngleAxis(segment.angleV, referenceRightDir)
				* segment.referenceLookDir;

			// Make look and up perpendicular
			Vector3 upDirGoal = segment.referenceUpDir;
			Vector3.OrthoNormalize(ref lookDirGoal, ref upDirGoal);

			// Interpolated look and up directions in neck parent space
			Vector3 lookDir = lookDirGoal;
			segment.dirUp = Vector3.Slerp(segment.dirUp, upDirGoal, Time.deltaTime * 5);
			Vector3.OrthoNormalize(ref lookDir, ref segment.dirUp);

			// Look rotation in world space
			Quaternion lookRot = (
				(parentRot * Quaternion.LookRotation(lookDir, segment.dirUp))
				* Quaternion.Inverse(
					parentRot * Quaternion.LookRotation(
						segment.referenceLookDir, segment.referenceUpDir
					)
				)
			);

			// Distribute rotation over all joints in segment
			Quaternion dividedRotation =
                Quaternion.Slerp(Quaternion.identity, lookRot, effect / segment.chainLength);
			t = segment.lastTransform;
			for (int i=0; i < segment.chainLength; i++) {
				t.rotation = dividedRotation * t.rotation;
				t = t.parent;
			}
		}

		// Handle non affected joints
		for (int i=0; i < nonAffectedJoints.Length; i++) {
			Vector3 newJointDirection = Vector3.zero;

			foreach (Transform child in nonAffectedJoints[i].joint) {
				newJointDirection = child.position - nonAffectedJoints[i].joint.position;
				break;
			}

			Vector3 combinedJointDirection = Vector3.Slerp(
				jointDirections[i], newJointDirection, nonAffectedJoints[i].effect
			);

			nonAffectedJoints[i].joint.rotation = Quaternion.FromToRotation(
				newJointDirection, combinedJointDirection
			) * nonAffectedJoints[i].joint.rotation;
		}
	}

	// The angle between dirA and dirB around axis
	public static float AngleAroundAxis(Vector3 dirA, Vector3 dirB, Vector3 axis) {
		// Project A and B onto the plane orthogonal target axis
		dirA = dirA - Vector3.Project(dirA, axis);
		dirB = dirB - Vector3.Project(dirB, axis);

		// Find (positive) angle between A and B
		float angle = Vector3.Angle(dirA, dirB);

		// Return angle multiplied with 1 or -1
		return angle * (Vector3.Dot(axis, Vector3.Cross(dirA, dirB)) < 0 ? -1 : 1);
	}
}

[System.Serializable]
public class BendingSegment {
	public Transform firstTransform;
	public Transform lastTransform;
	public float thresholdAngleDifference = 0;
	public float bendingMultiplier = 0.6f;
	public float maxAngleDifference = 30;
	public float maxBendingAngle = 80;
	public float responsiveness = 5;
	internal float angleH;
	internal float angleV;
	internal Vector3 dirUp;
	internal Vector3 referenceLookDir;
	internal Vector3 referenceUpDir;
	internal int chainLength;
	internal Quaternion[] origRotations;
}

[System.Serializable]
public class NonAffectedJoints {
	public Transform joint;
	public float effect = 0;
}
