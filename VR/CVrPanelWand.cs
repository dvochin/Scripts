/*###DISCUSSION: Wand GUI panel ###DEV23:
=== LAST ===

=== NEXT ===
- Create a tiny 'aiming dot' at center of headset to visually help the player to quickly point to hotspots.
- Make panel dissapear when wand too far.
- Need a selection framework working on each wand:
	- 1. User gazes upon a hotspot with the headset and presses action button.
	- 2. Wand displays GUI for that hotspot
- Button assignments:
	- Button 1/A/X: "left click" = Invoke default action, open panel, activate a pin, etc
	- Button 2/B/Y: "context menu" = Invoke a 'context menu' that offers all possible action on the object under headset gaze
- Fix gaze working on CPanels!
- Clean up old panel shit!

=== TODO ===
- Anchor point rotates a small amount to try to keep panel levelled and pointed toward headset (without intersecting wand)

=== LATER ===

=== IMPROVE ===

=== DESIGN ===
- Panels are creating at gazing at a body part and clicking action button.  It appears on wand owning action button
	- Player shows by bringing close to face and dismisses by removing from face.  Procedure can be repeated at will
	- Panel can be 'pinned': in 3D space (where Wand currently is) or at a 'pinning position' such as body left, right, couple left / right, etc

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===

=== QUESTIONS ===

=== WISHLIST ===

*/

using UnityEngine;

public class CVrPanelWand {
	CVrObjControl	_oVrObjControl;
	CUICanvas		_oCanvas;		

	public CVrPanelWand(CVrObjControl oVrObjControl, Transform oModelAttachParentT) {
		_oVrObjControl = oVrObjControl;
		_oCanvas = CUICanvas.Create(_oVrObjControl.transform);
		_oCanvas.transform.SetParent(oModelAttachParentT);					// Attach the canvas to the provided attach point on the wand model...
		_oCanvas.transform.localPosition = new Vector3(0, 0.015f, 0.07f);		//... and give it a small offset so it doesn't collider with wand 3D model.
		_oCanvas.transform.localRotation = Quaternion.Euler(90, 0, 0);
		//CObject oObj = CGame.INSTANCE._aBodyBases[0]._oBody._oObj;
		CObject oObj = CGame.INSTANCE._oObj;		//###HACK:!!!!
		CUtility.WndPopup_Create(_oCanvas, EWndPopupType.PropertyEditor, new CObject[] { oObj }, "Wand Menu");	//###IMPROVE: Menu name based on wand name
	}
}
