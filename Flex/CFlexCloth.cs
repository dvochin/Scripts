//using UnityEngine;
//using System;

//public class CFlexCloth_OBS : CBMesh, IObject, IFlexProcessor, IHotSpotMgr {

//	//---------------------------------------------------------------------------	MEMBERS
//	[HideInInspector]	public 	CObject				_oObj;                          // The multi-purpose CObject that stores CProp properties  to publicly define our object.  Provides client/server, GUI and scripting access to each of our 'super public' properties.

//    //---------------------------------------------------------------------------	PhysX-related properties sent during BSoft_Init()
//    [HideInInspector]   public CHotSpot             _oHotSpot;



//    //---------------------------------------------------------------------------	INIT

//    public CFlexCloth_OBS() {                           // Setup the default arguments... usually overriden by our derived class   //###BUG??? Why are these settings not overriding those in instanced node???
//	}

//	public static CFlexCloth_OBS Create(CBody oBody, string sNameCloth) { 
//		CFlexCloth_OBS oFlexCloth = (CFlexCloth_OBS)CBMesh.Create(null, oBody, sNameCloth, typeof(CFlexCloth_OBS));
//		return oFlexCloth;
//	}

//	public override void OnDeserializeFromBlender() {
//		base.OnDeserializeFromBlender();

//		if (GetComponent<Collider>() != null)
//			Destroy(GetComponent<Collider>());                      //###LEARN: Hugely expensive mesh collider created by the above lines... turn it off!

//		//=== Set bounds to infinite so our dynamically-created mesh never has to recalculate bounds ===
//		_oMeshNow.bounds = CGame._oBoundsInfinite;          //####IMPROVE: This can hurt performance ####OPT!!
//		_oMeshNow.MarkDynamic();        // Docs say "Call this before assigning vertices to get better performance when continually updating mesh"

//        //=== Call our C++ side to construct the solid tetra mesh.  We need that to assign tetrapins ===		//###DESIGN!: Major design problem between cutter sent here... can cut cloth too??  (Will have to redesign cutter on C++ side for this problem!)
//        //###DEV ###DESIGN: Recreate public properties each time???
//        CFlex.CreateFlexObject(gameObject, _oMeshNow, uFlex.FlexBodyType.Cloth, uFlex.FlexInteractionType.SelfCollideFiltered, CGame.INSTANCE.nMassCloth, 0, Color.blue);
//        uFlex.FlexProcessor oFlexProc = CUtility.FindOrCreateComponent(gameObject, typeof(uFlex.FlexProcessor)) as uFlex.FlexProcessor;
//        oFlexProc._oFlexProcessor = this;
//        uFlex.FlexParticles oSoftFlexParticles = GetComponent<uFlex.FlexParticles>();
//        //GetComponent<MeshRenderer>().enabled = CGame.INSTANCE.showFlexParticles == false;       //###F

//        //###HACK
//        Material oMat = new Material(Shader.Find("PhysShaders/DoubleSided"));        // If material was not found (usual case) we just create a standard diffuse on
//        //UnityEngine.Object oTex = Resources.Load("/Materials/Textures/(Test)/a40_06_01_02_00_Darziel_Top_ON_PinkGingham");
//        //oTex = Resources.Load("/Materials/Textures/(Test)/a40_06_01_02_00_Darziel_Top_ON_PinkGingham.bmp");
//        //oTex = Resources.Load("Materials/Textures/(Test)/a40_06_01_02_00_Darziel_Top_ON_PinkGingham");
//        //oTex = Resources.Load("Materials/Textures/(Test)/a40_06_01_02_00_Darziel_Top_ON_PinkGingham.bmp");
//        //oTex = Resources.Load("Textures/(Test)/a40_06_01_02_00_Darziel_Top_ON_PinkGingham");
//        //oTex = Resources.Load("Textures/(Test)/a40_06_01_02_00_Darziel_Top_ON_PinkGingham.bmp");
//        //oMat.mainTexture = oTex;
//        GetComponent<MeshRenderer>().material = oMat;
        
//        _oObj = new CObject(this, 0, typeof(EFlexCloth), "Cloth " + gameObject.name);        //###IMPROVE: Name of soft body to GUI
//        _oObj.PropGroupBegin("", "", true);
//        _oObj.PropAdd(EFlexCloth.Tightness, "Tightness", 1.0f, 0.01f, 3.0f, "", CProp.Local);
//        _oObj.FinishInitialization();
//        _oHotSpot = CHotSpot.CreateHotspot(this, _oBody.FindBone("chest"), "Cloth", false, new Vector3(0, 0.05f, 0.25f));   //###TUNE

//        _oObj.PropSet(EFlexCloth.Tightness, 2.0f);          //###TUNE
//    }



//    public override void OnDestroy() {
//		Debug.Log("Destroy CFlexCloth " + gameObject.name);
//		///ErosEngine.SoftBody_Destroy(_oObj._hObject);		//###CHECK: Everything destroyed?  Actors, colliders, etc??
//		base.OnDestroy();
//	}


//    public virtual void OnChangeGameMode(EGameModes eGameModeNew, EGameModes eGameModeOld) {		//###DEV

//		switch (eGameModeNew) { 
//			case EGameModes.Play:
//                break;

//			case EGameModes.Configure:      //###CLEAN
//				break;
//		}
//	}
//    //--------------------------------------------------------------------------	IHotspot interface

//    public void OnHotspotChanged(CGizmo oGizmo, EEditMode eEditMode, EHotSpotOp eHotSpotOp) { }

//    public void OnHotspotEvent(EHotSpotEvent eHotSpotEvent, object o) {		//###DESIGN? Currently an interface call... but if only GUI interface occurs through CObject just have cursor directly invoke the GUI_Create() method??
//    	if (eHotSpotEvent == EHotSpotEvent.ContextMenu)             //###IMPROVE: Move this common action to base class??
//    		_oHotSpot.WndPopup_Create(new CObject[] { _oObj });
//    }
//    public void OnPropSet_Tightness(float nValueOld, float nValueNew) {
//        uFlex.FlexSprings oFlexSprings = gameObject.GetComponent<uFlex.FlexSprings>();
//        for (int nSpring = 0; nSpring < oFlexSprings.m_springsCount; nSpring++)
//            oFlexSprings.m_springCoefficients[nSpring] = nValueNew;
//        //oFlexSprings.m_newStiffness = nValueNew;
//        //oFlexSprings.m_overrideStiffness = true;          //###NOTE: Not doing it this way as it iterates every frame!
//        Debug.LogFormat("Cloth Tightness {0}", nValueNew);
//    }

//    public void OnPropSet_NeedReset(CProp oProp, float nValueOld, float nValueNew) { }

    
//    //---------------------------------------------------------------------------	Flex
//    public void PreContainerUpdate(uFlex.FlexSolver solver, uFlex.FlexContainer cntr, uFlex.FlexParameters parameters) { }      //###CLEANUP: Needed??
//}
