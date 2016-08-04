using UnityEngine;
using UnityEngine.UI;

public abstract class CUIWidget : MonoBehaviour {
    [HideInInspector]   public CUIPanel    _oCanvas;           // Our owning canvas
    [HideInInspector]   public CProp        _oProp;             // The property we represent  ####SOON: to abstract class??
    [HideInInspector]   public Text         _oTextLabel;
    [HideInInspector]   public bool         _bInitialized;      // Exists to prevent calls to PropSet until fully initialized
    public string       _Label;
    

    public virtual void Init(CUIPanel oCanvas, CProp oProp) {
        _oCanvas = oCanvas;
        _oProp = oProp;
        if (_oProp == null)
            Debug.Log("CUIWidget.Init has null property!!");
        _oTextLabel.text = oProp._sLabel;
        SetValue(_oProp.PropGet());
        _bInitialized = true;
    }

    public abstract void SetValue(float nValueNew);

    public void OnValueChange(float nValueNew) {
        if (_bInitialized == false)                     // Avoid calling PropSet() during init flow (all properties set such as slider's min and max)
            return;
        if (_oProp == null)
            Debug.Log("CUIWidget.OnValueChange has null property!!");
        _oProp.PropSet(nValueNew);
    }
}
