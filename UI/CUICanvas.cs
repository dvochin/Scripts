/*###DEVLIST: GUI
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
using System.Collections;

public class CUICanvas : MonoBehaviour {

    public static CUICanvas Create() {
        GameObject oCanvasResGO = Resources.Load("UI/CUICanvas") as GameObject;                 //####IMPROVE: Cache
        GameObject oCanvasGO = Instantiate(oCanvasResGO) as GameObject;
        //oCanvasGO.transform.SetParent(oCanvas.transform, false);      //####SOON: Reposition / reparent??
        CUICanvas oUICanvas = oCanvasGO.GetComponent<CUICanvas>();
        //oUICanvas.Init();
        return oUICanvas;
    }

    public void OnButtonClose() {
        Destroy(gameObject);                // Destroy the entire canvas to remove it from the scene.  //####SOON: Notify CProp
    }
}
