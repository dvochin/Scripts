using UnityEngine;

public class CUICanvas : MonoBehaviour {			// CUICanvas: Encapsulates the native canvas primitive and facilitates the creation of CUIPanel objects
    public static CUICanvas Create(Transform oParentT) {
        GameObject oCanvasResGO = Resources.Load("UI/CUICanvas") as GameObject;
        GameObject oCanvasGO = Instantiate(oCanvasResGO) as GameObject;
        oCanvasGO.transform.SetParent(oParentT, false);
        oCanvasGO.transform.localPosition = Vector3.zero;
        //oCanvasGO.transform.localRotation = Quaternion.identity;
        oCanvasGO.transform.localRotation = Quaternion.Euler(0, 180, 0);        //###WEAK:!!! Entire canvas inverted to properly face camera. (Don't know why this is needed!)
        CUICanvas oCanvas = oCanvasGO.GetComponent<CUICanvas>();
        oCanvas.Init();
        return oCanvas;
    }

	void Init() {}

	public void UpdateCanvasSize() {		// We need to set the size of our box collider to the new panel size so that VRTK's raycasting will work and VR can control our panels ===
		RectTransform oCanvasRT = GetComponent<RectTransform>();			//###IMPROVE: This 
		Canvas.ForceUpdateCanvases();           //###INFO: Helps getting the values that have been updated this frame but because of our 'ContentSizeFitter' we still get zero height!
		Vector2 vecCanvasSize = new Vector2(oCanvasRT.rect.width, oCanvasRT.rect.height);
		for (int nChild = 0; nChild < transform.childCount; nChild++) {		// Iterate through our immediate children to add their heights (they should all be CUIPanels)
			Transform oChildT = transform.GetChild(nChild);
			RectTransform oChildRT = oChildT as RectTransform;
			vecCanvasSize.y += (int)oChildRT.rect.height;
		}

        Vector2 vecPivot = oCanvasRT.pivot;
		vecPivot.y = 1;			//###HACK: Logic for center below fails.  Even though we have pivot at 0.5 for x,y possibly because of our fitter the right value are computed if we set y to 1
		float zScale = 1;       //###MOD: Was much too thick!   zSize / oCanvasRT.localScale.z;  0, -135, 0 / 232, 270, 0

		BoxCollider oBoxCol = CUtility.FindOrCreateComponent(gameObject, typeof(BoxCollider)) as BoxCollider;
        oBoxCol.size = new Vector3(vecCanvasSize.x, vecCanvasSize.y, zScale);
        oBoxCol.center = new Vector3(vecCanvasSize.x / 2 - vecCanvasSize.x * vecPivot.x, vecCanvasSize.y / 2 - vecCanvasSize.y * vecPivot.y, zScale / 2f);
        oBoxCol.isTrigger = true;
	}

	public CUIPanel CreatePanel(string sNameDialogLabel, string sNameChooserLabel = null, params object[] aObjects) {     //###DESIGN19: Keep?
		CUIPanel oPanel = CUIPanel.Create(this, sNameDialogLabel, sNameChooserLabel, aObjects);
		UpdateCanvasSize();				// Created a new panel.  Need to update our size
		return oPanel;
	}
}
