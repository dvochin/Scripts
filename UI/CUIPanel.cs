/*###DISCUSSION: GUI
=== REVIVE ===
- Remove crappy gasket in CUtility
- Remove multi-object capacity.
- Fix prop groups (rendered in GUI)
- Add capacity for a panel to render an optional 'chooser' between multi-objects
- Every object can be rendered as a 'choice' in our 'multi-choice' property viewer
- Objects receive the OnEditingBegin() and OnEditingEnd() messages.
- Add the capacity of the panel to have 'action buttons' rerouted to owner objects
- Use interfaces throughtout for easy portability.
- Damn title!

=== NEXT ===
- Remove link to CProp when destroying!
- Fully remove iGUI

=== OLD? ===
- Now almost with drag... but size all screwed up cuz panel not centered in canvas... 
- Position Z no clip
- Depth on dialog box
- Prevent click behind dialog box
- Rethink CUIWidget base class, creation, events, etc

=== DESIGN ===

=== APPEARANCE ===
- Panel background
- X stretches

=== PROBLEMS ===

=== PROBLEMS??? ===

=== WISHLIST ===
- Top separator ugly
- Tooltips!!

*/

using UnityEngine;
using UnityEngine.EventSystems;

public class CUIPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

	public CObject _oObj;				// The CObject we view and edit.  When it gets destroyed it calls us for destruction.

    public static CUIPanel Create(CUICanvas oCanvas) {
        GameObject oPanelResGO = Resources.Load("UI/CUIPanel") as GameObject;
        GameObject oPanelGO = Instantiate(oPanelResGO) as GameObject;
        oPanelGO.transform.SetParent(oCanvas.transform, false);
        oPanelGO.transform.localPosition = Vector3.zero;
        oPanelGO.transform.localRotation = Quaternion.identity;
        CUIPanel oPanel = oPanelGO.GetComponent<CUIPanel>();
        //oPanel.Init();
        return oPanel;
    }

	//public static CUIPanel Create(CUICanvas oCanvas, CObject oObj) {
	//	_oObj = oObj;
	//	CUIPanel oPanel = CUIPanel.Create(oCanvas);			//###
	//	_oObj._oPanel = this;
	//}


    public void OnButtonClose() {
        Destroy(gameObject);                // Destroy the entire canvas to remove it from the scene.  //####SOON: Notify CProp
    }

    public void OnPointerEnter(PointerEventData eventData) {
        //Debug.LogFormat("UI Enter: " + eventData.ToString());
        CGame.INSTANCE._oCursor._oCurrentGuiObject_HACK = transform;        //###HACK: Let cursor know user is over this panel (needed for depth adjustments)  ###IMPROVE: Can find a way to get this info in CCursor??
    }
    public void OnPointerExit(PointerEventData eventData) {
		//Debug.LogFormat("UI Exit: " + eventData.ToString());
		CGame.INSTANCE._oCursor._oCurrentGuiObject_HACK = null;
    }
}
