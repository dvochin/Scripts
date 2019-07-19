using UnityEngine;
using UnityEngine.UI;

public abstract class CUIWidget : MonoBehaviour {
    [HideInInspector]   public CUIPanel    _oCanvas;           // Our owning canvas
    [HideInInspector]   public CObj        _oObj;             // The property we represent  ####SOON: to abstract class??
						public Text         _oTextLabel;
    [HideInInspector]   public bool         _bInitialized;      // Exists to prevent calls to Set until fully initialized
						public string       _Label;
    

    public virtual void Init(CUIPanel oCanvas, CObj oObj) {
        _oCanvas = oCanvas;
        _oObj = oObj;
        if (_oObj == null)
            CUtility.ThrowException("EXCEPTION: CUIWidget.Init has a null oObj!");
		_oTextLabel.text = oObj._sName;
		SetValue(_oObj.Get());
        _bInitialized = true;
    }

    public abstract void SetValue(float nValueNew);

    public void OnValueChange(float nValueNew) {
        if (_bInitialized == false)                     // Avoid calling Set() during init flow (all properties set such as slider's min and max)
            return;
        if (_oObj == null)
            CUtility.ThrowException("EXCEPTION: CUIWidget.Init has a null oObj!");
        _oObj.Set(nValueNew);
    }
}
