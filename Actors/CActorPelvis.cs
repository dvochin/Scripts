/*###DISCUSSION: USER HOTSPOT ANIMATION
=== NEXT ===

=== TODO ===

=== LATER ===

=== IMPROVE ===

=== DESIGN ===
 * Really need to abstract to own class and time-anim any property? (not just vector2?)

=== IDEAS ===
 * Have a 'mirror mode' where the other's body is inverted from this one??
 * Incorporate angle x/y/z on sex bones to allow user to rotate??
 * Any value in animating chest / other actors??

=== LEARNED ===

=== PROBLEMS ===
 * Shift during initial anim entry a pain...
 * Clicking hotspot makes it you can't activate it again
 * Pose of sex bones coincides with penis root -> Merge the two hotspots??  (Or will play mode solve this)
 * Getting sleeping problems again?

=== PROBLEMS??? ===

=== WISHLIST ===
 *? Orienting the woman sex bone for pussy rub and penetration
 *--- Send curve to Blender to trim points?
 * Store a few curves and ship V1 game with them??

*/

using UnityEngine;

public class CActorPelvis : CActor {		// The important 'pelvis driver' that is extensively used by the user to manipulate & animate the woman and man's genitals area toward penetration and teasing animations

	[HideInInspector]	public 	CBone 		_oBoneHip;

	//---------------------------------------------------------------------------	ANIM RELATED
	public EAnimMode	_eAnimMode = EAnimMode.Stopped;		//###DESIGN: Global??
	AnimationCurve		_oCurveAnimY;
	AnimationCurve		_oCurveAnimZ;
	float				_nAnimStartRec, _nAnimStartPlay, _nAnimClipLength;
	float				_nDistTravelled;				// Distance travelled thus far while recording.  Used to insert a 'get back to start point' of reasonable velocity at the end to avoid cyclic glitch during anim repeats
	float				_nTimeRatioToMatchOtherBody;	// Speedup / slowdown of time during playback we inject to match the time of the other body	
	Vector3				_vecPos, _vecPosStart, _vecPosLast;

	//---------------------------------------------------------------------------	CREATE / DESTROY

	public override void OnStart_DefineLimb() {

		_aBones.Add(_oBoneHip		= CBone.Connect(this, null,			"hip",		8));		//###NOTE: While 'hip' is the root bone, it still has a non-kinematic rigid body like any other bone with D6 joints going into and out of it. (upperChest is our *real* root bone so pelvis can move easily)
		_aBones.Add(_oBoneExtremity = CBone.Connect(this, _oBoneHip,	"pelvis",	8));

		//_oHotSpot = CHotSpot.CreateHotspot(this, transform, "Pelvis", true, G.C_Layer_HotSpot, C_SizeHotSpot_BodyNodes);

        _oObj = new CObj("Pelvis", this);
        AddBaseActorProperties();						// The first properties of every CActor subclass are Pinned, pos & rot
        //_oObj.Set(0, EActorPelvis.Pinned, 1);			// Manually set pinned to 1 on chest so body doesn't float in space when no pose is loaded (Weak that we can't set in Add() due to init-time problems)
    }



    //---------------------------------------------------------------------------	HOTSPOT ANIMATION PLAYBACK / RECORD
    void Update() {
		if (_eAnimMode == EAnimMode.Play) {
			if (Input.GetKeyDown(KeyCode.Escape)) {
				_eAnimMode = EAnimMode.Stopped;
				//CGame._sGuiString_Dev_DEBUG = "";
			} else {
				float nAnimTime = (Time.time - _nAnimStartPlay) * CGame._nAnimMult_Time * _nTimeRatioToMatchOtherBody;
				if (nAnimTime >= _nAnimClipLength) {
					nAnimTime = 0;
					_nAnimStartPlay = Time.time;
				}
				_vecPos = new Vector3();
				_vecPos.y = _oCurveAnimY.Evaluate(nAnimTime);		// Obtain the independent Y,Z hotspot positions from the pre-recorded animation curve.
				_vecPos.z = _oCurveAnimZ.Evaluate(nAnimTime);
				transform.localPosition = _vecPosStart + _vecPos * CGame._nAnimMult_Pos;
				//CGame._sGuiString_Dev_DEBUG = string.Format("Play {0:F1} / {1:F1}", nAnimTime, _nAnimClipLength);
			}
		}
	}

	public override void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) {
		if (eEditMode != EEditMode.Move)				//###DESIGN!!!!: Can rotate?
			return;

		base.OnHotspotChanged(oGizmo, eEditMode, eHotSpotOp);		// Will set our position, properties and script record.
		Vector3 vecPosHotspotG = oGizmo.transform.position;
		Vector3 vecPosHotspotL = transform.parent.worldToLocalMatrix.MultiplyPoint(vecPosHotspotG);		// Convert the global hotspot position to the position relative to our parent (ActorEmpty) (Done so our animation is stored relative to our parent so user can drag parent while animating)

		if (oGizmo._bMiddleClickOp) {				// Left mouse button editing moves sex, middle mouse button animates

			switch (eHotSpotOp) {
				case EHotSpotOp.First:
					_oCurveAnimY = new AnimationCurve();				// Create the animation curves to store the (independant) Y & Z coordinates
					_oCurveAnimZ = new AnimationCurve();
					_nAnimStartRec = Time.time;							// Store the time of clip recording so we can determine clip length for 0-based curves.
					_vecPosStart = _vecPosLast = vecPosHotspotL;
					_nDistTravelled = 0;
					CGame._nAnimMult_Pos = 1;				//###CHECK
					CGame._nAnimMult_Time = 1;
					_eAnimMode = EAnimMode.Record;
					break;

				case EHotSpotOp.Middle:
					float nAnimTime = Time.time - _nAnimStartRec;
					Vector3 vecPosFromStart = vecPosHotspotL - _vecPosStart;
					_oCurveAnimY.AddKey(nAnimTime, vecPosFromStart.y);
					_oCurveAnimZ.AddKey(nAnimTime, vecPosFromStart.z);
					_nDistTravelled += (vecPosHotspotL - _vecPosLast).magnitude;		// Add the distance travelled this last frame
					_vecPosLast = vecPosHotspotL;									// Remember where we are now for next distance travelled adder
					//CGame._sGuiString_Dev_DEBUG = string.Format("Rec {0:F1}  Dist: {1:F2}", nAnimTime, _nDistTravelled);
					break;

				case EHotSpotOp.Last:		// End recording by adding one last data point to return to start position at an appropriately calculated time.
					_nAnimStartPlay = Time.time;
					_nAnimClipLength = _nAnimStartPlay - _nAnimStartRec;
					_nAnimStartRec = 0;
					float nVelocityAvg = _nDistTravelled / _nAnimClipLength;				// The average velocity of travel for this curve
					float nDistStartPtToEndPt = (vecPosHotspotL - _vecPosStart).magnitude;	// The distance between the start and end point we must travel to to close the loop
					float nTimeToReturnToStartPt = nDistStartPtToEndPt / nVelocityAvg;		// The time needed to return to starting point at average velocity
					_nAnimStartPlay = Time.time - _nAnimClipLength;							// Set the current pointer to play to where we are at now in the curve.  (This will make us return to starting point smoothly)
					_nAnimClipLength += nTimeToReturnToStartPt;								// Add the time to travel to the last point to the total clip time.
					_oCurveAnimY.AddKey(_nAnimClipLength, 0);								// Add the starting point position at the end-of-clip time.
					_oCurveAnimZ.AddKey(_nAnimClipLength, 0);

					//=== Find the other body (if it exists) and slow-down or speed up our clip if our clip time is nearby the cliptime of the other body
					//###BROKEN11:
					//CBody oBodyOther = null;
					//foreach (CBody oBody in CGame._aBodyBases) {
					//	if (oBody != _oBody) {
					//		oBodyOther = oBody;
					//		break;
					//	}
					//}
					//if (oBodyOther != null && oBodyOther._oActor_Pelvis != null) {			// If another body with a sex actor exists...
					//	if ((_nAnimClipLength <= oBodyOther._oActor_Pelvis._nAnimClipLength * 1.2f) && (_nAnimClipLength >= oBodyOther._oActor_Pelvis._nAnimClipLength * 0.8f)) {		//... and has nearly the same length as ours...	###TUNE
					//		oBodyOther._oActor_Pelvis._nTimeRatioToMatchOtherBody = oBodyOther._oActor_Pelvis._nAnimClipLength / _nAnimClipLength;	// We set the other body's time because we could be facing a long return to first position as inserted above
					//		//Debug.Log("Anim: _nTimeRatioToMatchOtherBody = " + oBodyOther._oActor_Pelvis._nTimeRatioToMatchOtherBody);
					//	}
					//}
					_nTimeRatioToMatchOtherBody = 1;
					//CGame._sGuiString_Dev_DEBUG = "";
					_eAnimMode = EAnimMode.Play;
					break;

			}
		} else {
			_eAnimMode = EAnimMode.Stopped;				// If user moves with left mouse button we stop previously recorded animation
		}
	}
}

public enum EAnimMode {
	Stopped,
	Play,
	Record			//###SIMPLIFY: Currently unused... remove?
}