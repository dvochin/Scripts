using UnityEngine;
using System.Collections.Generic;


public class CHoleRaycaster {       // CHoleRaycaster: Responsible for performing raycasting at every opening bone to 'open' at position of nearest collider.  Used to implement super-accurate vagina opening from penis collider
	Transform       _oBone_ParentUnmoveableT;
	Transform       _oBone_ChildMoveableT;
	LineRenderer    _oLineRenderer_Debug;
	static Vector3  s_vecRaycastEndL    = new Vector3(0, -C_SizeMax, 0);
	static int      s_nLineCounter = 0;
	Ray             _oRay = new Ray();
	RaycastHit      _oRayHit;
	int             _nLayerMask = -1;
	Vector3         _vecStartG, _vecEndG;
	const float     C_SizeMax = 0.07f;
	static bool     C_HoleRaycaster_ShowDebugLines = false;


	public CHoleRaycaster(Transform oBone_ChildMoveableT) {
		_oBone_ChildMoveableT = oBone_ChildMoveableT;
		if (CHoleRaycaster.C_HoleRaycaster_ShowDebugLines) {
			_oLineRenderer_Debug = CGame.Line_Add("HoleRay" + CHoleRaycaster.s_nLineCounter.ToString());        //_oBoneT.gameObject.name
			_oLineRenderer_Debug.startWidth = _oLineRenderer_Debug.endWidth = 0.0005f;
		}
		CHoleRaycaster.s_nLineCounter++;
		if (_nLayerMask == -1)
			_nLayerMask = 1 << LayerMask.NameToLayer("Penis");		//###WEAK: Hardcoded name.  Always this layer??
	}

	public void Initialize() {
		_oBone_ParentUnmoveableT = new GameObject(_oBone_ChildMoveableT.name + "-Parent").transform;
		_oBone_ParentUnmoveableT.rotation = _oBone_ChildMoveableT.rotation;
		_oBone_ParentUnmoveableT.position = _oBone_ChildMoveableT.localToWorldMatrix.MultiplyPoint(-s_vecRaycastEndL);
		_oBone_ParentUnmoveableT.SetParent(_oBone_ChildMoveableT.parent);
		_oBone_ChildMoveableT.SetParent(_oBone_ParentUnmoveableT);
		DoUpdate();
	}

	public void DoUpdate() {
		_vecStartG  = _oBone_ParentUnmoveableT.position;
		_oRay.origin = _vecStartG;
		_oRay.direction = -_oBone_ParentUnmoveableT.up;

		bool bHit = Physics.Raycast(_oRay, out _oRayHit, CHoleRaycaster.C_SizeMax, _nLayerMask);
		if (bHit) {
			_vecEndG = _oRayHit.point;
			_oBone_ChildMoveableT.position = _vecEndG;
		} else {
			_oBone_ChildMoveableT.localPosition = s_vecRaycastEndL;
			_vecEndG = _oBone_ChildMoveableT.position;
		}

		if (CHoleRaycaster.C_HoleRaycaster_ShowDebugLines) {
			_oLineRenderer_Debug.transform.position = _vecStartG;
			_oLineRenderer_Debug.SetPosition(0, _vecStartG);
			_oLineRenderer_Debug.SetPosition(1, _vecEndG);
			_oLineRenderer_Debug.material.color = bHit ? Color.red : Color.green;
		}
	}
}


public class CVaginaRaycaster {
	CBody _oBody;
	List<CHoleRaycaster> _aHoleRaycasters = new List<CHoleRaycaster>();

	public CVaginaRaycaster(CBody oBody) {
		_oBody = oBody;

		//=== Add all raycaster objects from properly-named bones in Mesh ===
		Transform oBoneGenitalsT = _oBody._oBodyBase.FindBone("hip/pelvis/Genitals");
		int nBones = oBoneGenitalsT.childCount;
		for (int nBone = 0; nBone < nBones; nBone++) {
			Transform oBoneT = oBoneGenitalsT.GetChild(nBone);
			if (oBoneT.name.StartsWith(CSoftBody.C_Prefix_DynBone_Vagina)) {     // Iterate through only the dynamic vagina bones
				CHoleRaycaster oHoleRaycaster = new CHoleRaycaster(oBoneT);
				_aHoleRaycasters.Add(oHoleRaycaster);
			}
		}
		foreach (CHoleRaycaster oHoleRaycaster in _aHoleRaycasters)
			oHoleRaycaster.Initialize();
	}

	public void DoUpdate() {
		foreach (CHoleRaycaster oHoleRaycaster in _aHoleRaycasters)
			oHoleRaycaster.DoUpdate();
	}
};
