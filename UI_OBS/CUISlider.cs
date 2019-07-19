using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CUISlider : CUIWidget {
    [HideInInspector]   public Slider       _oSlider;
    [HideInInspector]   public Text         _oTextValue;
    public float _Min = 0;
    public float _Max = 1;

    public static CUISlider Create(CUIPanel oCanvas, CObj oObj) {        //####IMPROVE: Can abstract some of this to base?
        GameObject oSliderResGO = Resources.Load("UI/CUISlider") as GameObject;                 //####IMPROVE: Cache
        GameObject oSliderGO = Instantiate(oSliderResGO) as GameObject;
        oSliderGO.transform.SetParent(oCanvas.transform, false);
        CUISlider oUISlider = oSliderGO.GetComponent<CUISlider>();
        oUISlider.Init(oCanvas, oObj);
        return oUISlider;
    }

    public override void Init(CUIPanel oCanvas, CObj oObj) {
        _oTextLabel = transform.GetChild(0).GetComponent<Text>();           // Label is always first child
        _oSlider = transform.GetChild(1).GetComponent<Slider>();            // Slider always second child
        _oTextValue = _oSlider.transform.GetChild(2).GetChild(0).GetChild(0).GetComponent<Text>();      // Value text at that relative address.  ###WEAK
        _oSlider.minValue = oObj._nMin;
        _oSlider.maxValue = oObj._nMax;
        base.Init(oCanvas, oObj);
    }

    public override void SetValue(float nValueNew) {
        _oSlider.value = nValueNew;
        string sValue;
		if (nValueNew == 0f)
			sValue = "0";
		else if (Mathf.Abs(nValueNew) < 1.0f)                                    // Provide extra precision when the number is small
			sValue = string.Format("{0:F2}", nValueNew);
		else if (Mathf.Abs(nValueNew) < 10.0f)
			sValue = string.Format("{0:F1}", nValueNew);
		else
			sValue = string.Format("{0:F0}", nValueNew);
        _oTextValue.text = sValue;          //####IMPROVE: Connect to format options of CObj
    }
}

public class CSlider : Slider {
	public override void OnPointerEnter(PointerEventData eventData) {
		base.OnPointerEnter(eventData);
	}
}
