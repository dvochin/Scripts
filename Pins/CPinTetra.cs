using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class CPinTetra : CPin {					// CPinTetra: A 3D location in space where a softbody tetrahedron vertex is fixed.  These are slaves to CPinSkinned which themselves move with body to fix a soft body body part to a skinned body.
	public  CPinSkinned 		_oPinSkinned;				// The skinned pin that owns us.  It moves with skinned body and this tetra pin in turn moves softbody tetravertex
	public	CBSoft				_oBSoft;					// The 'soft body' this pin belongs to.
	public 	int					_nVertTetra;
	public 	float				_nForceSpring;
			Transform			_oTran;
	
	public static CPinTetra CreatePinTetra(CPinSkinned oPinSkinned, CBSoft oBSoft, int nVertTetra, Vector3 oVectTetra, float nForceSpring) {
		GameObject oGoPinTemplate = Resources.Load("GUI-Pins/PinTetra") as GameObject;
		GameObject oGO = GameObject.Instantiate(oGoPinTemplate) as GameObject;
		CPinTetra oPinTetra = oGO.GetComponent<CPinTetra>();
		oPinTetra.InitializePinTetra(oPinSkinned, oBSoft, nVertTetra, oVectTetra, nForceSpring);
		return oPinTetra;
	}
	
	public void InitializePinTetra(CPinSkinned oPinSkinned, CBSoft oBSoft, int nVertTetra, Vector3 oVectTetra, float nForceSpring) {
		_oTran = transform;
		_oTran.name 	= nVertTetra.ToString();			//###DESIGN: Decorate the name for Unity GUI? or keep it just ID?
		_oPinSkinned 	= oPinSkinned;
		_oTran.parent 	= _oPinSkinned.transform;
		_oBSoft		= oBSoft;
		
		_nVertTetra 	= nVertTetra;
 		_oTran.position = oVectTetra;						// Specify our start offset.  As we are a child of CPin and it has already-set global coordinate, us setting 'position' will automatically calculate our rest state offset (that gets moved & rotated per frame as our parent moves)
		_nForceSpring 	= nForceSpring;

		GetComponent<Renderer>().sharedMaterial = _oPinSkinned._oMatPinTetra;				// Specify the same material for all tetra pins under this mesh part pin.
		GetComponent<Renderer>().enabled = false;											// Hide renderer for pin.  Power users can show with a hotkey
	}
	
	public override void OnSimulatePre() {							// Our CPinPart parent just changed position so our position has changed as well.  Send Phys our new coordinates!
		ErosEngine.PinTetra_AttachTetraVertToPos(_oBSoft._oObj._hObject, _nVertTetra, _oTran.position);				// If we're a simple fix we just update to the latest position
		base.OnSimulatePre();		//###TODO: Needed as we don't have children?
	}
};
