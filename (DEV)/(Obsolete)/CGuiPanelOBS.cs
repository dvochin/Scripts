using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public class iGUICode_RootPanelOBS : MonoBehaviour {			// Encapsulates a '3D slider' that appears at object-depth in 3D scene to change simple float values

	public CHotSpot		_oHotSpot;
	Transform 			_oNodePanel;
	TextMesh 			_oTextMesh;
	List<iGUICode_RootSliderOBS> 	_aSliders = new List<iGUICode_RootSliderOBS>();							//###IMPROVE: Revisit code to use these!  //###DESIGN

	public const float C_PanelDefaultWidth  = 0.4f;				// The size of the panel as created by 3dsMax  //###WEAK: A bit weak how we crunch up a rounded rect to our size...
	public const float C_PanelDefaultHeight = 0.4f;
	public const float C_HeaderTextHeight	= 0.05f;
	
	public static iGUICode_RootPanelOBS Create_GuiPanel(CHotSpot oHotSpot) {
		GameObject oPanelResGO	= Resources.Load("GUI-Panel/iGUICode_RootPanel") as GameObject;
		GameObject oNodePanelGO = GameObject.Instantiate(oPanelResGO) as GameObject;
		iGUICode_RootPanelOBS oGuiPanel = oNodePanelGO.GetComponent<iGUICode_RootPanelOBS>();
		oGuiPanel.Initialize(oHotSpot);
		return oGuiPanel;
	}
	
	void Initialize(CHotSpot oHotSpot) {
		_oHotSpot = oHotSpot;
		
		gameObject.name = "iGUICode_RootPanel_" + _oHotSpot.gameObject.name;
		transform.position = (4*_oHotSpot.transform.position + Camera.main.transform.position)/5;					// Adopt the position of our hotspot (rotation stays pinned to camera)
		transform.parent = Camera.main.transform;
		transform.localRotation = Quaternion.Euler(0, 180f, 0);				//###WEAK: Panel needs 180 degree rotation to show its right face toward the camera.  Is it right or do we change it?  (being attached to camera and all...)
		
		_oNodePanel = transform.FindChild("Panel");
		_oTextMesh  = transform.FindChild("Text").GetComponent<TextMesh>();
		_oTextMesh.text = _oHotSpot.gameObject.name;			//###*NOW: 
		_oNodePanel.gameObject.layer = CCursor.C_Layer_HotSpot;	//###CHECK

		Type oType = _oHotSpot.GetType();
		foreach (FieldInfo oFieldInfo in oType.GetFields()) {			//###IMPROVE: Enforce definition of 'super public' by placing test in CUtility?
			if (oFieldInfo.Attributes == FieldAttributes.Public && oFieldInfo.FieldType == typeof(float) && oFieldInfo.Name[0] >= 'A' && oFieldInfo.Name[0] <= 'Z') {		// Only create sliders for public floats starting with a capital letter -> our definition of 'super public'
				_aSliders.Add(iGUICode_RootSliderOBS.Create_GuiSlider(this, _aSliders.Count, oFieldInfo));
			}
		}
		
		float nHeightContent = 0.04f + _aSliders.Count * iGUICode_RootSliderOBS.C_SliderHeight;
		_oNodePanel.localScale = new Vector3(1, nHeightContent / C_PanelDefaultHeight, 1);
		_oTextMesh.transform.localPosition = new Vector3(0, -iGUICode_RootSliderOBS.C_SliderHeight/2, 0);

		BoxCollider oColBox = (BoxCollider)_oNodePanel.GetComponent<Collider>();
		oColBox.center  = new Vector3(0, -C_PanelDefaultHeight/2, 0);
		oColBox.size	= new Vector3(C_PanelDefaultWidth, C_PanelDefaultHeight, 0);		// We create the panel collider with no depth and control colliders with depth so they can 'poke through' and get their mouse events
	}
}

