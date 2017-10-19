/*###DOCS23?: Jun 2017 - Wand GUI panel

=== DEV ===

=== LAST ===

=== NEXT ===
- Bug with panel not showing up the first time!

=== TODO ===
- Anchor point rotates a small amount to try to keep panel levelled and pointed toward headset (without intersecting wand)
- Both wands can show the same panel!

=== LATER ===
- Clean up old panel shit and multi-layers of old crap!

=== IMPROVE ===
- Have wand panels dissapear when they backface the camera

=== DESIGN ===
- Panels are creating at gazing at a body part and clicking action button.  It appears on wand owning action button
	- Player shows by bringing close to face and dismisses by removing from face.  Procedure can be repeated at will
	- Panel can be 'pinned': in 3D space (where Wand currently is) or at a 'pinning position' such as body left, right, couple left / right, etc
- Make panel dissapear when wand too far.
- Need a selection framework working on each wand:
	- 1. User gazes upon a hotspot with the headset and presses action button.
	- 2. Wand displays GUI for that hotspot
- Button assignments:
	- Button 1/A/X: "left click" = Invoke default action, open panel, activate a pin, etc
	- Button 2/B/Y: "context menu" = Invoke a 'context menu' that offers all possible action on the object under headset gaze
- Fix gaze working on CPanels!
- Need a clear design on the 'modes' of each wand to avoid collision of purpose:
	- e.g. Having a panel shown should reroute the buttons to control the panel (e.g. no camera movement, scene iterations)
	- Being in middle of a camera move / object move should not show panel if near headset.

=== IDEAS ===
- Add a button to 'pin' the panel from the wand to the 3d space
	- Add feature to scale up / down as user gets further?
- Make panels 3D objects that participate in collisions and their pins based on D6 joints with a stiff damping.  Should prevent clipping?

=== LEARNED ===

=== PROBLEMS ===

=== QUESTIONS ===

=== WISHLIST ===

*/

using UnityEngine;

public class CVrPanelWand {
	public CVrWand	_oVrWand;
	public CUICanvas		_oCanvas;		

	public CVrPanelWand(CVrWand oVrWand, Transform oModelAttachParentT) {
		_oVrWand = oVrWand;
		_oCanvas = CUICanvas.Create(_oVrWand.transform);
		_oCanvas.transform.SetParent(oModelAttachParentT);					// Attach the canvas to the provided attach point on the wand model...
		_oCanvas.transform.localPosition = new Vector3(0, 0.015f, 0.07f);		//... and give it a small offset so it doesn't collider with wand 3D model.
		_oCanvas.transform.localRotation = Quaternion.Euler(90, 0, 0);
		_oCanvas.gameObject.name = "CUICanvas-VrWand";

		//###HACK:!!!!!! While waiting for our object selection functionality we hardcode direct access
		//CObject oObj = CGame.INSTANCE._aBodyBases[0]._oBody.Breasts;
		//CObject oObj = CGame.INSTANCE._aBodyBases[0]._oBody._oObj;
		CObject oObj = null;
		if (CGame.INSTANCE._aBodyBases[0] != null && CGame.INSTANCE._aBodyBases[0]._oBody != null)
			oObj = CGame.INSTANCE._aBodyBases[0]._oBody.Penis;
		if (oObj != null)
			CUtility.WndPopup_Create(_oCanvas, EWndPopupType.PropertyEditor, new CObject[] { oObj }, "Wand Menu");	//###IMPROVE: Menu name based on wand name
	}
}
