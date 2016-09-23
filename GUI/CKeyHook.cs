using UnityEngine;
using System;
using System.Collections;

public class CKeyHook : IDisposable {

	public KeyCode			_oKeyCode;			// The keyboard code we are designed to trap and process
	public EKeyHookType		_eKeyHookType;
	public CProp			_oProp;				// The CProp property that owns / manages this keyboard hook		
	public string			_sDescription;
	public float			_nRatio;			// Ratio to multiply raw mouse input to convert into a value
	public bool				_bSelectedBodyOnly;	// When true key is active only if this key hook is owned by the selected body
	public WeakReference	_oWeakRef;			// Weak reference to ourself we stuff into CGame (for later removal)

	float	_nPropValueStart;					// Value of our CProp at start of 'quick mouse edit'
	float	_nMouseStartY;						// Y coordinate of the mouse at the start of 'quick mouse edit'

	public CKeyHook(CProp oProp, KeyCode oKeyCode, EKeyHookType eKeyHookType, string sDescription, float nRatio=1.0f, bool bSelectedBodyOnly = true) {
		_oProp			= oProp;
		_oKeyCode		= oKeyCode;
		_eKeyHookType	= eKeyHookType;
		_sDescription	= sDescription;
		_nRatio			= nRatio;
		_bSelectedBodyOnly = bSelectedBodyOnly;
		_oWeakRef		= new WeakReference(this, false);
		CGame.INSTANCE._aKeyHooks.Add(_oWeakRef);		//###NOTE: Add a weak reference to CGame so that it can automatically remove dead objects as the owner of this CKeyHook destroys us.
	}

	public void Dispose() {									//###IMPROVE!!! Possibly to auto dispose without explicit calling??? Annoying!!
		CGame.INSTANCE._aKeyHooks.Remove(_oWeakRef);		//###CHECK: Keep or just autodelete in CGame??      ###NOW### BUG!
	}
	
	public void OnUpdate() {		//###DESIGN: OnUpdate??? From CGame??? Game mode sensitive??
		if (_oProp._oObject._nBodyID == CGame.INSTANCE._nSelectedBody || _bSelectedBodyOnly == false) {		//###DESIGN!!! Only affect selected body on all keyhooks???  ###SOON

			switch (_eKeyHookType) {

				case EKeyHookType.Simple:			//###OBS? Only use key hooks with quick mouse edit??
					if (Input.GetKeyDown(_oKeyCode))
						Debug.Log(string.Format("KeyHook: {0} = '{1}' ({2})", _oKeyCode.ToString(), _sDescription, _oProp.ToString()));
					break;

				case EKeyHookType.QuickMouseEdit:				//###OPT!!!!: A major performance drain?  Can be improved??
					if (Input.GetKeyDown(_oKeyCode)) {
						_nPropValueStart = _oProp.PropGet();
						_nMouseStartY = Input.mousePosition.y / Screen.height;
					} else if (Input.GetKeyUp(_oKeyCode)) {
						CGame.SetGuiMessage(EGameGuiMsg.SelectedBodyAction, null);
					} else if (Input.GetKey(_oKeyCode)) {
						float nMousePosY = Input.mousePosition.y / Screen.height;
						float nMousePosDelta = (_nMouseStartY - nMousePosY) * _nRatio;		// Note inversion here as mouse going up usually mean lowering value...
						float nPropValDelta = nMousePosDelta * _oProp._nMinMaxRange * 3;	// We multiply the power of the full top-to-bottom screen travel so that user doesn't have to travel mouse that far to select all possible property values
						_oProp.PropSet(_nPropValueStart + nPropValDelta);
						CGame.SetGuiMessage(EGameGuiMsg.SelectedBodyAction, string.Format("{0} = {1:F1}", _sDescription, _oProp.PropGet()));
					}
					break;
			}
		}
	}
}

public enum EKeyHookType {
	Simple,
	QuickMouseEdit
};