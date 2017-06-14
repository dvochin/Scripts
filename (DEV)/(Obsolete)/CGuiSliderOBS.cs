using UnityEngine;
using System;
using System.Reflection;
using System.Collections;

public class iGUICode_RootSliderOBS : MonoBehaviour {			// Encapsulates a '3D slider' that appears at object-depth in 3D scene to change simple float values

	iGUICode_RootPanelOBS	_oPanel;
	int			_nOrdinal;
	FieldInfo 	_oFieldInfo;

	Transform	_oNodeSlide;				// The small 'thumb slide' that slides inside the slider to give visual indication of our value
	TextMesh 	_oTextMesh;
	
	public const float C_SliderMin  	= 0.0025f;		//###WEAK: Values calculated from 'eyeballing in editor' -> won't adjust to other mesh sizes!
	public const float C_SliderMax  	= 0.1650f;
	public const float C_SliderSize 	= (C_SliderMax - C_SliderMin);
	public const float C_SlideSize 		= 0.0240f;
	public const float C_SliderHeight	= 0.03f;
	
	
	public static iGUICode_RootSliderOBS Create_GuiSlider(iGUICode_RootPanelOBS oPanel, int nOrdinal, FieldInfo oFieldInfo) {
		GameObject oSliderResGO =  Resources.Load("GUI-Panel/iGUICode_RootSlider") as GameObject;
		GameObject oNodeSliderGO = GameObject.Instantiate(oSliderResGO) as GameObject;
		iGUICode_RootSliderOBS oSlider = oNodeSliderGO.GetComponent<iGUICode_RootSliderOBS>();
		oSlider.Initialize(oPanel, nOrdinal, oFieldInfo);
		return oSlider;
	}
	
	public void Initialize(iGUICode_RootPanelOBS oPanel, int nOrdinal, FieldInfo oFieldInfo) {
		_oPanel 	= oPanel;
		_nOrdinal 	= nOrdinal;
		_oFieldInfo = oFieldInfo;
		
		transform.name = "iGUICode_RootSlider_" + oFieldInfo.Name;
		transform.parent = _oPanel.transform;
		transform.localPosition = new Vector3(0.01f, -_nOrdinal*C_SliderHeight-iGUICode_RootPanelOBS.C_HeaderTextHeight, 0);
		transform.localRotation = Quaternion.Euler(0, -90f, 0);						//**WEAK: Another annoying init rotation... fix this in 3dsMax??
		_oNodeSlide = transform.Find("Slide");
		_oTextMesh  = transform.Find("Text").GetComponent<TextMesh>();			//###WEAK: A bit of duplication with CCursor... create base class??
		gameObject.layer = CCursor.C_Layer_HotSpot;

		BoxCollider oColBox = (BoxCollider)transform.GetComponent<Collider>();
		oColBox.center  = new Vector3(0, 0, C_SliderSize/2);// + C_SliderMin*2 + C_SlideSize/2);
		oColBox.size	= new Vector3(0.02f, 0.02f, .2f); 							// We give it some 'depth' so slider collider can 'poke through' panel collider and get mouse events
		
		SetSlidePos_FromValue((float)_oFieldInfo.GetValue(_oPanel._oHotSpot._oEditingObject));
	}
	
	public void SetSliderTextFromVarName(string sVarName, float nValue) {
		string sFieldNameHumanReadable = CUtility.ConvertCamelCaseToHumanReadableString(sVarName);
		SetSliderText(sFieldNameHumanReadable, nValue);
	}

	public void SetSliderText(string sFieldNameHumanReadable, float nValue) {
		string sLabelText = string.Format("{0}: {1:F0}", sFieldNameHumanReadable, nValue);
		_oTextMesh.text =  sLabelText;
	}
	
	public void SetSlidePos_FromMouse(RaycastHit oRayHit) {
		Vector3 vecPosLocal = transform.worldToLocalMatrix.MultiplyPoint(oRayHit.point);
		float nSlidePos = Mathf.Clamp(vecPosLocal.z - 0.7f*C_SlideSize, C_SliderMin, C_SliderMax);		//###WEAK: Hack on that multiplyer... eyeballed it until mouse gives centered value on slider
		_oNodeSlide.localPosition = new Vector3(0, 0, nSlidePos);
		float nValuePercent = (200f * (nSlidePos - C_SliderMin) / C_SliderSize) -100f;
		_oFieldInfo.SetValue(_oPanel._oHotSpot._oEditingObject, nValuePercent);
		SetSliderTextFromVarName(_oFieldInfo.Name, nValuePercent);
	}
	
	public void SetSlidePos_FromValue(float nValuePercent) {								//###IMPROVE: Design needs more intuitive like get/set?
		float nPosWorld = ((nValuePercent+100f) * (C_SliderSize/200f)) + C_SliderMin;
		_oNodeSlide.localPosition = new Vector3(0, 0, nPosWorld);
		SetSliderTextFromVarName(_oFieldInfo.Name, nValuePercent);
	}
}

