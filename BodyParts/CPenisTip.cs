using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;


//public class CPenisTip_OBS : MonoBehaviour, IObject, IHotSpotMgr {			//###DESIGN: CPenis and tip or just one??			###DESIGN: Make IHostSpotMgr owned by CObject??
//	[HideInInspector] public CPenis_OBS		_oPenis;				// Our owning penis
//	[HideInInspector] public CObject	_oObj;					// The multi-purpose CObject that stores CProp properties  to publicly define our object.  Provides client/server, GUI and scripting access to each of our 'super public' properties.
//	[HideInInspector] public CHotSpot	_oHotSpot;				// Hotspot at the penis tip.  Conventiently positioned at penis opening to be the emitter for fluid cum.

//	public CMemAlloc<float> _memTransformPhysX;		// Dll position of the penis tip

//	//--------------------------------------------------------------------------	INIT

//	public void OnAwake(CPenis_OBS oPenis) {
//		_oPenis = oPenis;
//		_oObj = new CObject(this, _oPenis._oBody._oBodyBase._nBodyID, typeof(EPenisTip), "PenisTip");
//	}

//	public void OnStart() {									//###DESIGN: Call from central game??
//		_oObj.PropGrpBegin("", "", true);
//		_oObj.PropAdd(EPenisTip.MaxVelocity,	"Max Velocity",		1.3f, 0, 2,	"Maximum cum velocity.", CProp.Local);	//###WEAK!!!! Pretty much useless for the user as we limit EFluid.MaxMotionDistance
//		_oObj.PropAdd(EPenisTip.CycleTime,		"Cycle Time",		4, 2, 8,	"Time in seconds between ejaculations.", CProp.Local);
//		_oObj.FinishInitialization();

//		_memTransformPhysX = new CMemAlloc<float>(7);		// A transform is 3 float for vector and 4 for quaternion = 7 floats
//		_memTransformPhysX.PinInMemory();

//		_oHotSpot = CHotSpot.CreateHotspot(this, transform, "Penis Tip", true, new Vector3(0, -0.009f, 0.089f));		//###HACK!! ###DESIGN!!!: How to store different positions of uretra on different penises... Value provided by Blender??
//	}


//	//--------------------------------------------------------------------------	UPDATE
		
//	public void OnSimulatePre() {
//		_oObj.OnSimulatePre();

//		//=== Update the position of our Unity penis tip from the just-provided PhysX position (fetched by owning CPenis) ===
//		transform.position = new Vector3   (_memTransformPhysX.L[4], _memTransformPhysX.L[5], _memTransformPhysX.L[6]);
//		transform.rotation = new Quaternion(_memTransformPhysX.L[0], _memTransformPhysX.L[1], _memTransformPhysX.L[2], _memTransformPhysX.L[3]);
//		//###DESIGN!!!! ###IMPROVE!!: Get whole penis chain for stroking... so this half-baked design for tip should be replaced

//		//=== Adjust position of hotpsot (at uretra) from the last segment position.  We need to add the current segment lenght and the radius as uretra is at the far end of the penis tip capsule
//		float nPenisRadiusPlusSegLenght = _oPenis._nRadiusNow + _oPenis._nSegLenNow;
//		_oHotSpot.transform.localPosition = new Vector3(0, nPenisRadiusPlusSegLenght * -.08f, nPenisRadiusPlusSegLenght * .9f);	//###HACK: Bit of a hack as per the ratio

//		//CProp oPropPenisSizePercent = CGame.INSTANCE._oObj.PropFind(0, EGamePlay.PenisSize);
//	}



//	//--------------------------------------------------------------------------	IHotspot interface

//	public void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) {
//		// Penis tip itself does not move.  Reroute the Y movement of our hotspot to bend up/down the penis base with left mouse button and penis shaft if middle mouse button
//		//if (Input.GetMouseButton(2) == false)		###DESIGN: Keep this method of changing or keyboard only.
//		//	_oPenis._oObjDriver.PropFind(EPenis.BaseUpDown).ChangePropByHotSpotMove(oGizmo, eEditMode, eHotSpotOp, -150);	//###IMPROVE: Autoscale from prop min/max??	###TUNE
//		//else
//		//	_oPenis._oObjDriver.PropFind(EPenis.ShaftUpDown).ChangePropByHotSpotMove(oGizmo, eEditMode, eHotSpotOp, -250);
//	}

//	public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {
//		switch (eHotSpotEvent) {
//			case EHotSpotEvent.ContextMenu:
//				_oHotSpot.WndPopup_Create(null, new CObject[] { _oPenis._oObjDriver, _oPenis._oObj, _oObj });		//###DESIGN: Keep penis and penis tip objects accessible from penis tip? (or give penis its own hotspot??)
//				break;
//			case EHotSpotEvent.TriggerEnter:
//				Collider oCol1 = o as Collider;
//				if (oCol1.gameObject.name == "VaginaEntryTrigger")
//					CGame.INSTANCE.SetPenisInVagina(true);
//				break;
//			case EHotSpotEvent.TriggerExit:
//				Collider oCol2 = o as Collider;
//				if (oCol2.gameObject.name == "VaginaEntryTrigger")
//					CGame.INSTANCE.SetPenisInVagina(false);
//				break;
//		}
//	}
//}
