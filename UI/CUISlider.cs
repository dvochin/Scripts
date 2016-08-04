using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CUISlider : CUIWidget {
    [HideInInspector]   public Slider       _oSlider;
    [HideInInspector]   public Text         _oTextValue;
    public float _Min = 0;
    public float _Max = 1;

    public static CUISlider Create(CUIPanel oCanvas, CProp oProp) {        //####IMPROVE: Can abstract some of this to base?
        GameObject oSliderResGO = Resources.Load("UI/CUISlider") as GameObject;                 //####IMPROVE: Cache
        GameObject oSliderGO = Instantiate(oSliderResGO) as GameObject;
        oSliderGO.transform.SetParent(oCanvas.transform, false);
        CUISlider oUISlider = oSliderGO.GetComponent<CUISlider>();
        oUISlider.Init(oCanvas, oProp);
        return oUISlider;
    }

    public override void Init(CUIPanel oCanvas, CProp oProp) {
        _oTextLabel = transform.GetChild(0).GetComponent<Text>();           // Label is always first child
        _oSlider = transform.GetChild(1).GetComponent<Slider>();            // Slider always second child
        _oTextValue = _oSlider.transform.GetChild(2).GetChild(0).GetChild(0).GetComponent<Text>();      // Value text at that relative address.  ###WEAK
        _oSlider.minValue = oProp._nMin;
        _oSlider.maxValue = oProp._nMax;
        base.Init(oCanvas, oProp);
    }

    public override void SetValue(float nValueNew) {
        _oSlider.value = nValueNew;
        string sValue;
        if (Mathf.Abs(nValueNew) < 1.0f)                                    // Provide extra precision when the number is small
            sValue = string.Format("{0:F2}", nValueNew + 0.005f);
        else if (Mathf.Abs(nValueNew) < 10.0f)
            sValue = string.Format("{0:F1}", nValueNew + 0.05f);
        else
            sValue = string.Format("{0:F0}", nValueNew + 0.5f);
        _oTextValue.text = sValue;          //####IMPROVE: Connect to format options of CProp
    }
}

public class CSlider : Slider {
	public override void OnPointerEnter(PointerEventData eventData) {
		base.OnPointerEnter(eventData);
	}
}
