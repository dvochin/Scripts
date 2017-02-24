using UnityEngine;

public class CUICanvas : MonoBehaviour {			// CUICanvas: Encapsulates the native canvas primitive and facilitates the creation of CUIPanel objects
    public static CUICanvas Create(Transform oParentT) {
        GameObject oCanvasResGO = Resources.Load("UI/CUICanvas") as GameObject;
        GameObject oCanvasGO = Instantiate(oCanvasResGO) as GameObject;
        oCanvasGO.transform.SetParent(oParentT, false);
        oCanvasGO.transform.localPosition = Vector3.zero;
        //oCanvasGO.transform.localRotation = Quaternion.identity;
        oCanvasGO.transform.localRotation = Quaternion.Euler(0, 180, 0);        // Entire canvas inverted to properly face camera. (Don't know why this is needed!)
        CUICanvas oCanvas = oCanvasGO.GetComponent<CUICanvas>();
        oCanvas.Init();
        return oCanvas;
    }

	void Init() {

	}

	public void CreatePanel() {

	}
}
