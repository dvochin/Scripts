/***DISCUSSION: Morphing
 * Redo head split to leave out teeth and back of head (skinned)
 * Add multiple possible 'channels', each controlable with a good API
 * Just specialize to CFace?  (Breast morphing similar enough for base class?)
 * Remove face from main mesh
 * Eyes??
*/


using UnityEngine;
using System.Collections;

public class CFace : CBMesh, IObject {

	[HideInInspector]	public	CObject				_oObj;				// Our object responsible for 'super public' properties exposed to GUI and animation system.  Each CActor-derived subclass fills this with its own properties

	[HideInInspector]	public	CMorphChannel		_oMorphMouthOpen;	// Morph channels controls facial expression
	[HideInInspector]	public	CMorphChannel		_oMorphEyesClosed;
	[HideInInspector]	public	CMorphChannel		_oMorphBrowInner;
	[HideInInspector]	public	CMorphChannel		_oMorphBrowOuter;

	[HideInInspector]	public	bool				_bMorphApplied;			// Set by CMorphChannel when the mesh has changed so we update it

	CProp _oPropEyesClose;

	public override void OnDeserializeFromBlender() {
		base.OnDeserializeFromBlender();

		//=== Reparent this non-skinned mesh to the head bone so it moves with the neck & body ===
		transform.parent = _oBody.FindBone("chest/neck/head");		//###DESIGN???

		//=== Create the morph channels ===
		_oMorphMouthOpen  = new CMorphChannel(this, "MouthOpen");			//###IMPROVE!!! MouthOpen unfortunately doensn't move teeth which are bone-rigged... Affect bone?  Redo head to leave mouth skinned?  Add teeth to morph???
		_oMorphEyesClosed = new CMorphChannel(this, "EyesClosed");
		_oMorphBrowInner  = new CMorphChannel(this, "BrowInner");
		_oMorphBrowOuter  = new CMorphChannel(this, "BrowOuter");

		//=== Init CObject ===
		_oObj = new CObject(this, _oBody._nBodyID, typeof(EFace), "Face", "Face");
		_oObj.PropGroupBegin("", "", true);
		_oObj.PropAdd(EFace.MouthOpen,	"Mouth Open",		0,	-0.3f,	1,	"", CProp.Local);		//###TUNE: Reasonable limits on negative side
		_oObj.PropAdd(EFace.EyesClosed,	"Eyes Closed",		0,	-0.3f,	1,	"", CProp.Local);
		_oObj.PropAdd(EFace.BrowInner,	"Brow Inner",		0,	-0.3f,	1,	"", CProp.Local);
		_oObj.PropAdd(EFace.BrowOuter,	"Brow Outer",		0,	-0.3f,	1,	"", CProp.Local);
		_oObj.FinishInitialization();

		_oPropEyesClose = _oObj.PropFind(EFace.EyesClosed);
		StartCoroutine(Coroutine_BlinkEyes());					//###LEARN: How to start & use a simple coroutine...
		StartCoroutine(Coroutine_RandomFaceExpression());
	}

	public void FixedUpdate() {
		if (_bMorphApplied) {					//###IMPROVE???
			_oMeshNow.vertices = _memVerts.L;
			_bMorphApplied = false;
		}
	}

	IEnumerator Coroutine_BlinkEyes() {					// Simple coroutine to efficiently blink the eyes...
		//###IMPROVE: Add a few intermediate states for smoother animation.
		float nEyesCloseBeforeClose;
		for (;;) {
			nEyesCloseBeforeClose = _oPropEyesClose.PropGet();			// Remember where the eye was at before mandatory blinking so we return it to where it was.
			_oPropEyesClose.PropSet(1);
			yield return new WaitForSeconds(CGame.GetRandom(0.05f, 0.10f));		
			_oPropEyesClose.PropSet(nEyesCloseBeforeClose);
			yield return new WaitForSeconds(CGame.GetRandom(3.0f, 4.0f));		// Average human eye blink is about 6 sec interval but that appeared too slow
		}
	}

	IEnumerator Coroutine_RandomFaceExpression() {			// Apply a very small amout of randomness to each morphing channel to look less like an automaton!
		const float nFrequency = 0.08f;				//###IMPROVE: This efficient / simple randomizer manages to look robotic... improve with more expensive slerp interpolation?
		const float nRandomization = 0.12f;
		for (; ; ) {
			_oMorphEyesClosed.ApplyMorphRandom(CGame.GetRandom(0, nRandomization));
			yield return new WaitForSeconds(nFrequency);
			_oMorphBrowInner.ApplyMorphRandom(CGame.GetRandom(0, nRandomization));
			yield return new WaitForSeconds(nFrequency);
			_oMorphBrowOuter.ApplyMorphRandom(CGame.GetRandom(0, nRandomization));
			yield return new WaitForSeconds(nFrequency);
			_oMorphMouthOpen.ApplyMorphRandom(CGame.GetRandom(0, nRandomization));
			yield return new WaitForSeconds(nFrequency);
		}
	}

	public void OnPropSet_MouthOpen(float nValueOld, float nValueNew) { _oMorphMouthOpen.ApplyMorph(nValueNew); }
	public void OnPropSet_EyesClosed(float nValueOld, float nValueNew) { _oMorphEyesClosed.ApplyMorph(nValueNew); }
	public void OnPropSet_BrowInner(float nValueOld, float nValueNew) { _oMorphBrowInner.ApplyMorph(nValueNew); }
	public void OnPropSet_BrowOuter(float nValueOld, float nValueNew) { _oMorphBrowOuter.ApplyMorph(nValueNew); }
}
