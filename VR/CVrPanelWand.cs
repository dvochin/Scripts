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

public class CVrPanelWand {     //###OBS:??
    public CVrWand  _oVrWand;
    public CBrowser _oBrowser;
    Transform _oModelAttachParentT;
    const float C_SizeBrowser = 0.25f;
    const float C_AspectRatio_WidthDivHeight = 0.5f;
    const int C_BrowserPixelWidth = 300;

    public CVrPanelWand(CVrWand oVrWand, Transform oModelAttachParentT) {       //#DEV26: Need this class??  Have browser owned by wand directly??
        _oVrWand = oVrWand;
        _oModelAttachParentT = oModelAttachParentT;
         CBrowser.Create(out _oBrowser);
        //#DEV27A:??? CGame.INSTANCE._oBrowser_HACK = _oBrowser;
        _oBrowser.transform.localScale = new Vector3(C_SizeBrowser/2, C_SizeBrowser, 0.001f);      //###INFO: Raycasting won't be able to hit the browser if the z axis is zero.
        _oBrowser.transform.localRotation = Quaternion.Euler(60, 0, 0);        // Root wand transform need some rotation to avoid bending the user's wrist.  ###IMPROVE: Can get a sub-node from children?
        _oBrowser.Resize(C_BrowserPixelWidth, (int)(C_BrowserPixelWidth / C_AspectRatio_WidthDivHeight));
    }
}

//public class CVrPanelWand {
//    public CVrWand  _oVrWand;
//    public CBrowser _oBrowser;
//    Transform _oModelAttachParentT;
//    const float C_SizeBrowser = 0.25f;
//    const float C_AspectRatio_WidthDivHeight = 0.5f;
//    const int C_BrowserPixelWidth = 300;


//    public CVrPanelWand(CVrWand oVrWand, Transform oModelAttachParentT) {       //#DEV26: Need this class??  Have browser owned by wand directly??
//        _oVrWand = oVrWand;
//        _oModelAttachParentT = oModelAttachParentT;
//        CreateBrowser();
//    }

//    public void CreateBrowser() {
//        CGame._oBrowser_HACK = _oBrowser = CUtility.InstantiatePrefab<CBrowser>("Prefabs/ZenBrowser/CBrowser", "Browser-VrWand", _oModelAttachParentT);
//        _oBrowser.transform.localScale = new Vector3(C_SizeBrowser/2, C_SizeBrowser, 0.001f);      //###INFO: Raycasting won't be able to hit the browser if the z axis is zero.
//        _oBrowser.transform.localRotation = Quaternion.Euler(60, 0, 0);        // Root wand transform need some rotation to avoid bending the user's wrist.  ###IMPROVE: Can get a sub-node from children?
//        _oBrowser.Initialize();
//        _oBrowser.Resize(C_BrowserPixelWidth, (int)(C_BrowserPixelWidth / C_AspectRatio_WidthDivHeight));
//    }
//}
