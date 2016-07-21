using UnityEngine;
using System.Collections;

public class CClothEdit : MonoBehaviour {

	public CBody		_oBody;
	public string		_sClothType;
	public string       _sBlenderInstancePath_CClothEdit;
	public CBMesh       _oClothSource;

	public CClothEdit(CBody oBody, string sClothType) {
		_oBody			= oBody;
		_sClothType		= sClothType;

		//        CBody.CBody._aBodies[0].CreateCloth("BodySuit", "_ClothSkinnedArea_Top", "Shirt")      ###One of teh body suits?
		string sCmd = _oBody._sBlenderInstancePath_CBody + ".CreateCloth('BodySuit', '_ClothSkinnedArea_Top', '" + _sClothType + "')";          //###DESIGN: Pass in all args from Unity?  Blender determines?
		CGame.gBL_SendCmd("CBody", sCmd);
		_sBlenderInstancePath_CClothEdit = "aCloths['" + _sClothType + "']";

		_oClothSource = CBMesh.Create(null, _oBody, _sBlenderInstancePath_CClothEdit + ".oMeshClothSource", typeof(CBMesh));
	}
}
