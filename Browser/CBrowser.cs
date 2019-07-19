/*###DOCS26: Nov 2017 - Web Browser integration
=== DEV ===
- Remove INewWindowHandler... web can no longer create new windows.  Unity always master of small focused windows
- Make CBrowser the base class and derived ones have the members
- Create CBrowerWand on both sides
    - Create fully self-contained web app for the wand buttons... e.g sub buttons, sliders, pane hide / show, Unity notifications, etc
- Create one jsp for everything?  (How it configures the page is through a function in the JSP and it loads the proper files?)
- How about sizing?  static for now?  (wand browser might need to be much more flexible!
- Should develop wand web page out of Unity... but how to test at small size?
    - Create web facet with small resolution?


--- Buttons ---
- Topmost row: Wide 'config' and 'play' radio buttons
- Second row: Main game modes:
- Button matrix: About 8 wide and 8 rows
    - Fuck type
    - Fuck depth
    - Fuck speed
    - Cum
    - Penis Hardness
    - Penis Angle, Penis Curve?
    - Hands
    - Left hand
    - Right hand
    - Legs Spread:
    - Left Leg
    - Right Leg

=== IDEAS ===
- 

=== IDEAS ===
+ A line only shows the most important buttons but has an 'expand button' to insert the 'detail rows'
    - e.g. Fucking control has about 8 buttons, when you expand you have the speed, depth controls.
    - Have + / - buttons and a tree icons?
- Or... have the buttons at the top decide what the bottom buttons are and their detail level. (color them)
- Have the 'hidden buttons' always be available as popup of the small 'right arrow' at right.
- Instead of having sliders, could have about 10 small buttons?
- Each button has a tooltip that explains what it does as well as the equivalent speech command.
- Have posing be the same thing??
    - Could have the main poses be topmost buttons and the related ones sub ones.

=== QUESTIONS ===


- Browser dpi, dimensions, aspect ratio... prefs stored where??  PaneRot?  CGame? Here??

=== NEXT ===

=== TODO ===
- Prop slider width calc is garbage... make autosize!

=== LATER ===

=== OPTIMIZATIONS ===

=== REMINDERS ===

=== IMPROVE ===

=== NEEDS ===

=== DESIGN ===

=== QUESTIONS ===

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===

=== WISHLIST ===

*/

using UnityEngine;
using ZenFulcrum.EmbeddedBrowser;
using System.Collections.Generic;
using System;

public class CBrowser : Browser, INewWindowHandler {
	static int s_nBrowsersCreated = 0;
    public CWebViewTree _oWebViewTree_SelectedObject;       // Tree property viewer for the 'selected object'  #DEV27: Name?  (Really selected or constant??
    public CWebViewTree _oWebViewTree_GameSettings;         // Tree property viewer for the 'game settings'
    public CWebViewTree _oWebViewTree_Cock_HACK;            //###DEV27: Temp property viewer for cock
    public CWebViewGridPose _oWebViewGrid_Poses;            // Grid database viewer / editor for the body poses
    public CWebViewGridBody _oWebViewGrid_Body;             // Grid database viewer / editor for the body definitions


	public static CBrowser Create(out CBrowser oBrowser, bool bInitialize = true) {
		Transform oPaneRotParentT;
		if ((s_nBrowsersCreated++ % 2) == 0)
			oPaneRotParentT = CGame._oPaneRot._oPaneRot_AnchorL;
		else
			oPaneRotParentT = CGame._oPaneRot._oPaneRot_AnchorR;

		oBrowser = CUtility.InstantiatePrefab<CBrowser>("Prefabs/ZenBrowser/CBrowser", "Browser-Popup-" + s_nBrowsersCreated.ToString(), oPaneRotParentT);
		oBrowser.transform.localRotation = Quaternion.Euler(0, 180, 0);      //###IMPROVE: Coordinate panel size with pane rot which needs that!       ###HACK! Why is inversion about Y needed?? ###IMPROVE: Save this quat?
		oBrowser.transform.localScale = CGame._oPaneRot._vecSize_PaneDocked;

		oBrowser.Resize((int)(CGame._oPaneRot._vecSize_PaneDocked.x * CGame._nBrowser_PixelsPerMeterResolution_HACK), (int)(CGame._oPaneRot._vecSize_PaneDocked.y * CGame._nBrowser_PixelsPerMeterResolution_HACK));           //#DEV26: What size?  Let web codebase tell us?

		if (bInitialize)
			oBrowser.Initialize();

		return oBrowser;
	}

	public Browser CreateBrowser(Browser parent) {      // Callback from ZenBrowser to create a new browser window somewhere in the scene. JScript code that creates new window will result in this being called
        CBrowser oBrowser;
		CBrowser.Create(out oBrowser, /*bInitialize=*/ false);
		return oBrowser;
	}

	//   public void Start() {
	//       _oBrowserZen = GetComponent<Browser>();
	//	//_oBrowser.ShowDevTools();
	//	Initialize();
	//}

	public void Initialize() {
		//_oBrowserZen = GetComponent<Browser>();
		//###DEV26Z: ###HOWTO: Change between VR versus non-VR mouse handler
        UIHandler = gameObject.GetComponent<CBrowserInputVR>();  //###TEMP: Mouse based versus VR headset based
        //UIHandler = gameObject.GetComponent<ClickMeshBrowserUI>();


		NewWindowHandler = this;
		//_oBrowser.LoadURL("http://localhost:8888/WebApp.html", false);
		//_oBrowser.Url = "http://www.snappymaria.com/misc/TouchEventTest.html";
		//LoadURL("localGame://SmartClient/smartclientSDK/templates/Test1.html", false);
		Url = "http://localhost:8080/DevCode/App.jsp?isc_remoteDebug=true";      //###SOURCE: On remote debugging see https://www.smartclient.com/smartclient-11.0/isomorphic/system/reference/SmartClient_Reference.html#group..remoteDebugging
        //Url = "http://localhost:8080/DevCode/CBrowserWand.jsp?isc_remoteDebug=true";      //###SOURCE: On remote debugging see https://www.smartclient.com/smartclient-11.0/isomorphic/system/reference/SmartClient_Reference.html#group..remoteDebugging

        //Reload();

        //#DEV27D: ###DESIGN: Too much done in CBrowser?  New class?  what to do??
        RegisterFunction("W2U_Unity", args => {           //###INFO: See http://www.gwtproject.org/doc/latest/DevGuideCodingBasicsJSNI.html
            string sEval = (string)args[0];
            Debug.LogFormat("W2U_Unity gets '{0}' from browser.", sEval);
            CGame._oScriptPlay.ExecuteLine(sEval);
        });
        SendEvalToBrowser("U2W_Initialize();");         // Tell web browser that it is up ('W2U_Unity' function up)     //#DEV26:!!! REDO!

        ShowDevTools();
        //GameMode_EditBody();
        enabled = true;
        Debug.Log("- WebBrowser initialized.");

        //#DEV27: ###HACK! We should create / destroy these according to game modes, etc
        if (CGame._aBodyBases.Length > 0)
            _oWebViewTree_SelectedObject = new CWebViewTree(this, CGame._aBodyBases[0]._oObj, "SelectedObject", "Selected");
        _oWebViewTree_GameSettings = new CWebViewTree(this, CGame._oObj, "GameSettings", "Settings");
        _oWebViewGrid_Poses = new CWebViewGridPose(this, "CEditorPose", "Poses", "Poses");
        _oWebViewGrid_Body = new CWebViewGridBody(this, "CEditorBody", "Body", "Body");
    }

	public void W2U_InitializeCompleted() {     // Message from the browser telling us that it has completed its initialization (we can now send game-time commands)
		Debug.Log("=== W2U_InitializeCompleted() ===");
		//SendEvalToBrowser("W2U_Start();");                // Tell browser to invoke its starting sequence       //#DEV26: Improve and clarify init flow!
	}

	protected new void Update() {
		base.Update();
		if (Input.GetKeyDown(KeyCode.BackQuote))        //###TEMP       //#DEV26: ###KEEP?
			Initialize();
        if (Input.GetKeyDown(KeyCode.Backspace))
            ShowDevTools();
        if (Input.GetKeyDown(KeyCode.Keypad0)) {        //###TEMP
            CGame.INSTANCE.ChangeGameMode(EGameModes.Play);
        }
        if (Input.GetKeyDown(KeyCode.Keypad1)) {        //###TEMP
            _oWebViewGrid_Body.U2W_ManuallyLoadRecord("Huge");
        }
        if (Input.GetKeyDown(KeyCode.Keypad2)) {        //###TEMP
            _oWebViewGrid_Poses.U2W_ManuallyLoadRecord("a");  //###PROBLEM: Doesn't work if user didn't go to pane!  (Even though we do load right?)
        }
        //if (Input.GetKeyDown(KeyCode.KeypadPlus)) {
        //    if (_oWebViewTree_SelectedObject == null)
        //        _oWebViewTree_SelectedObject = new CWebViewTree(this, CGame._aBodyBases[0]._oObj, "Browser._oWebViewTree_SelectedObject", "SelectedObject", "Selected");
        //    if (_oWebViewTree_GameSettings == null)
        //        _oWebViewTree_GameSettings = new CWebViewTree(this, CGame._oObj, "Browser._oWebViewTree_GameSettings", "GameSettings", "Settings");
        //}
        //if (Input.GetKeyDown(KeyCode.KeypadMinus) && _oWebViewTree_SelectedObject != null) {
        //    _oWebViewTree_SelectedObject.Dispose();
        //    _oWebViewTree_SelectedObject = null;
        //    _oWebViewTree_GameSettings.Dispose();
        //    _oWebViewTree_GameSettings = null;
        //}
        //if (Input.GetKeyDown(KeyCode.KeypadMultiply) && _oWebViewGrid_Poses == null) {
        //    _oWebViewGrid_Poses  = new CWebViewGridPose(this, "CEditorPose", "Browser._oWebViewGrid_Poses", "Poses", "Poses");
        //    _oWebViewGrid_Body   = new CWebViewGridBody(this, "CEditorBody", "Browser._oWebViewGrid_Body", "Body", "Body");
        //}
        //if (Input.GetKeyDown(KeyCode.KeypadDivide) && _oWebViewGrid_Poses != null) {
        //    _oWebViewGrid_Poses.Dispose();
        //    _oWebViewGrid_Poses = null;
        //    //_oWebViewGrid_Body.Dispose();
        //    _oWebViewGrid_Body= null;
        //}

		//if (Input.GetKeyDown(KeyCode.Alpha1))
		//	GameMode_EditBody();
		//if (Input.GetKeyDown(KeyCode.Alpha2))
		//	GameMode_EditBones();
		//if (Input.GetKeyDown(KeyCode.Alpha3))
		//	GameMode_Play();
	}



	//public void GameMode_EditBody() {       //#DEV26: ###OBS???
	//	SendEvalToBrowser("TreeProps_Clear()");
	//	CGame._aFlatLookupArray_Objects_HACK = new List<CObj>();
	//	Util_SendTree_Objects_RECURSIVE(CGame._aBodyBases[0]._oObj, ref CGame._aFlatLookupArray_Objects_HACK);
	//}

	//public void GameMode_EditBones() {      //###OBS: Jscript doesn't get bones, only actors?
	//	SendEvalToBrowser("TreeProps_Clear()");
	//	CGame._aFlatLookupArray_Objects_HACK = new List<CObj>();
	//	Util_SendTree_Bones_RECURSIVE(CGame._aBodyBases[0]._oBoneRootT, 0, ref CGame._aFlatLookupArray_Bones_HACK); //#DEV27: OBS?
	//}

	//public void GameMode_Play() {
	//	SendEvalToBrowser("TreeProps_Clear()");
	//}

	//public void GameMode_Dev() {
	//	SendEvalToBrowser("TreeProps_Clear()");
	//	CGame._aFlatLookupArray_Objects_HACK = new List<CObj>();
	//	//Util_SendTree_Objects_RECURSIVE(CGame._aBodyBases[0]._oActor_ArmL._oObj, ref CGame._aFlatLookupArray_Objects_HACK);
	//	Util_SendTree_Objects_RECURSIVE(CGame._oObj, ref CGame._aFlatLookupArray_Objects_HACK);
	//}

	public void SendEvalToBrowser(string sEval) {
		Debug.Log($"WebEval: '{sEval}'");
        EvalJS(sEval);              //#DEV27: ###IMPROVE: See this implementation on ways we can improve try / catch and logging in both Web and Unity!
        //try {_zfb_event(61, 
        //    JSON.stringify(eval("window.SelectedObject.U2W_DoDestroy();" )) || 'null');} 
        //catch(ex) {
        //    _zfb_event(61, 'fail-' + (JSON.stringify(ex.stack) || 'null'));
        //}
        //string sEval_Wrapped = $"window.CallEval(\"{sEval}\",\"Unity\");";        //###NOTE: Does not catch exceptions well enough for us to trap and display error message! 
		//EvalJS(sEval_Wrapped);      
	}
}
