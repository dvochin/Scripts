/*###DISCUSSION: GUI
=== REVIVE ===
- Now almost with drag... but size all screwed up cuz panel not centered in canvas... 




=== NEXT ===
- Position Z no clip
- Formatting of slider values
- Depth on dialog box
- Prevent click behind dialog box
- Rethink CUIWidget base class, creation, events, etc

=== TODO ===
- Property groups!
- Remove link to CProp when destroying!
- Fully remove iGUI

=== DESIGN ===

=== APPEARANCE ===
- Panel background
- X stretches

=== PROBLEMS ===
- Soft bodies not GPU????

=== PROBLEMS??? ===

=== WISHLIST ===
- Top separator ugly
- Move bar!!
- Tooltips!!
- Label to control split point adjustable?

*/

using UnityEngine;
using UnityEngine.EventSystems;

public class CUIPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

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

    public void OnButtonClose() {
        Destroy(gameObject);                // Destroy the entire canvas to remove it from the scene.  //####SOON: Notify CProp
    }

    public void OnPointerEnter(PointerEventData eventData) {
        Debug.LogFormat("UI Enter: " + eventData.ToString());
        CGame.INSTANCE._oCursor._oCurrentGuiObject_HACK = transform;        //###HACK: Let cursor know user is over this panel (needed for depth adjustments)  ###IMPROVE: Can find a way to get this info in CCursor??
    }
    public void OnPointerExit(PointerEventData eventData) {
        Debug.LogFormat("UI Exit: " + eventData.ToString());
        CGame.INSTANCE._oCursor._oCurrentGuiObject_HACK = null;
    }
}
