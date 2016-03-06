//	//---------------------------------------------------------------------------	GUI PROCESSING
//	public void oBtnTest1_Click(iGUIButton caller) {}
//	public void oBtnTest2_Click(iGUIButton caller) {}
//	public void oBtn_DevCode1_Click(iGUIButton caller) {}
//	public void oBtn_DevCode2_Click(iGUIButton caller) {}

//	//public void _oPaneLeft_Enable(iGUIListBox oListBox) {
//	//	PaneEnable(oListBox, 0);							//###IMPROVE: PaneID from name??
//	//}

//	//public void _oPaneRight_Enable(iGUIListBox oListBox) {
//	//	PaneEnable(oListBox, 1);
//	//}

//	////###OBS!!
//	//public void PaneEnable(iGUIListBox oListBox, int nPaneID) {			//###DESIGN ###MOVE???		###TODO: Selection, make generic, support emitter, etc.
//	//	oListBox.label.text = "=== " + CGame.INSTANCE._oFluid._sNameObject + " Properties ===";		//###TODO ###SOON!!

//	//	List<CObject> _aObj = new List<CObject>();					//###HACK!!! Find proper merge mechanism for multiple objects per property panel...
//	//	_aObj.Add(CGame.INSTANCE._oFluid);
//	//	_aObj.Add(CGame.INSTANCE._oFluidWorld);

//	//	foreach (CObject oObj in _aObj) {
//	//		foreach (CPropGroup oPropGrp in oObj._aPropGroups) {					//###PROBLEM: Why gap at top??
//	//			if (oPropGrp._nPaneID == nPaneID) {
//	//				oPropGrp.CreateWidget(oListBox);
//	//				foreach (int nPropID in oPropGrp._aPropIDs) {
//	//					CProp oProp = oObj.PropFind(nPropID);
//	//					oProp.CreateWidget(oListBox, oPropGrp);
//	//				}
//	//			}
//	//		}
//	//	}
//	//	//iGUILabel oLabelDummy = oListBox.addElement<iGUILabel>();		//###BUG!! ###TEMP: panel spaces last item at middle even if we set coordinates!  Adding dummy label to compensate
//	//	//oLabelDummy.label.text = "";
//	//}
//}




//	//[HideInInspector]	public int				_nCaptionCurrent;
//	//[HideInInspector]	public string[] _aCaptions = {		//###OBS???
//	//	"#Man wants to stick #Man_hisher huge cock deep inside #Woman's pussy.",
//	//	"#Woman wants to be fucked.",
//	//	"#Man is approaching #Woman's wet pussy.",
//	//	"#Man huge cock is getting bigger.",
//	//	"#Man is rubbing #Man_hisher cock on #Woman's clit.",
//	//	"#Woman's cunt is getting wet.",
//	//	"#Man's huge cock is overflowing with cum.",
//	//	"#Man cock is now truly massive.",
//	//	"#Woman wants a big cock deep inside her.",
//	//	"#Woman is rubbing her clit on #Man's massive shaft.",
//	//	"#Man puts #Man_hisher massive shaft in #Woman's wet cunt.",
//	//	"#Woman's cunt is throbing in extasy.",
//	//	"#Man won't be able to hold #Man_hisher cum much longer.",
//	//	"#Man is cumming all over.",
//	//};
	
//	//[HideInInspector]	public string[] _aCaptions2 = {
//	//	"#Man wants to ejaculate.",
//	//	"#Man is on the edge of cumming.",
//	//	"#Man wants to cum all over #Woman's body.",
//	//	"#Man wants to blow #Man_hisher load.",
//	//	"#Man's cock is near orgasm.",
//	//	"#Man can't hold much longer.",
//	//	"#Man wants to cum all over.",
	
//	//	"#Man wants to fuck a tight pussy.",
//	//	"#Man knows #Man_hisher huge cock is turning you on.",

//	//	"#Woman wants to feel a huge cock in her pussy.",
//	//	"#Woman knows her big tits are turning you on.",
//	//	"#Woman's clit is getting hard.",
//	//};

//		//if (Input.GetKeyDown(KeyCode.T)) {			//###HACK!! ###MOVE?? Very simple taunt system
//		//	string sCaption = _aCaptions[_nCaptionCurrent];
//		//	_nCaptionCurrent++;

//		//	CBody oBodyMan = CGame.INSTANCE._aBodies[0];		//###NOTE: Assume man/shemale is body 0
//		//	CBody oBodyWoman = CGame.INSTANCE._aBodies[1];	//###WEAK: Assumes body 1 is always woman (user can change)

//		//	sCaption = sCaption.Replace("#Man_hisher", "her");			//###TEMP!
//		//	sCaption = sCaption.Replace("#Man", oBodyMan._sHumanName);
//		//	sCaption = sCaption.Replace("#Woman", oBodyWoman._sHumanName);

//		//	oLabCaption.label.text = sCaption;				//###IMPROVE: Fade in and out
//		//	if (_nCaptionCurrent >= _aCaptions.Length)
//		//		_nCaptionCurrent = 0;
//		//	public void oPaneMsgIntro_MouseOver(iGUIPanel caller){

//	////---------------------------------------------------------------------------	CLOTH CUTTING WIDGETS		###WEAK: No _ naming convention!
//	//[HideInInspector] public iGUIPanel oTabClothCut;			
//	//[HideInInspector] public iGUIDropDownList oDropTemplate;
//	//[HideInInspector] public iGUIDropDownList oDropCurveSel;
//	////---------------------------------------------------------------------------	CLOTH FITTING WIDGETS		###CLEANUP!!!!! Get rid of this old junk!!
//	//[HideInInspector] public iGUIPanel oTabClothFit;
//	//[HideInInspector] public iGUICheckboxGroup oBtnGrpMode;
//	//[HideInInspector] public iGUICheckboxGroup oBtnGrpPivot;
//	//[HideInInspector] public iGUICheckboxGroup oBtnGrpEffect;
//	//[HideInInspector] public iGUITextarea oTxtDump;
//	////---------------------------------------------------------------------------	GAME WIDGETS
//	//[HideInInspector] public iGUIPanel oTabPlay;



//	//---------------------------------------------------------------------------	CLOTH CUTTING GUI PROCESSING
//	public void oDropCurveSel_ValueChange(iGUIDropDownList oGuiDropBox){
//		string sCurveSel = oGuiDropBox.options[oGuiDropBox.selectedIndex].text;
//		ECurveTypes eCurveType = (ECurveTypes)Enum.Parse(typeof(ECurveTypes), sCurveSel);		//###LEARN: How to convert a string into a enum! ###TODO: Scan code to apply this!
//		SelectCurveType(eCurveType);
//	}

//	void SelectCurveType(ECurveTypes eCurveType) {
//		//###BROKEN	CGame.INSTANCE._oGameMorph.ActivateCurve(eCurveType);
//		//=== Rebuild the list of templates in the template dropbox from the files present for the current curve type ===
//		this.oDropTemplate.removeAll();
//		this.oDropTemplate.addOption(C_SaveNewTemplate);
//		string[] aFiles = Directory.GetFiles(CCutterCurve.GetFolderPath(eCurveType), "*.CrvDef", SearchOption.TopDirectoryOnly);
//		foreach (string sFilePath in aFiles)
//			this.oDropTemplate.addOption(Path.GetFileNameWithoutExtension(sFilePath));
//		this.oDropTemplate.selectedIndex = -1;
//	}
//	public void oBtnCutSingle_Click(iGUIButton caller) {
//		//###BROKEN	CGame.INSTANCE._oGameMorph.DoCutSingle();
//	}

//	public void oBtnCutAll_Click(iGUIButton caller) {
//		CGame.INSTANCE._oGameMorph.DoCutAll();
//	}
//	public void oDropTemplate_ValueChange(iGUIDropDownList oGuiDropBox){
////		string sTemplate = oGuiDropBox.options[oGuiDropBox.selectedIndex].text;		//###TODO?
////		if (sTemplate == C_SaveNewTemplate) {
////			string sNewTemplate = "NewTemplate";						//###TODO: Prompt for new template name!
////			CGame.INSTANCE._oGameMorph._oCutterCurveActive.Save(sNewTemplate);
////			this.oDropTemplate.addOption(sNewTemplate);
////		} else {
////			CGame.INSTANCE._oGameMorph._oCutterCurveActive.Load(sTemplate);
////		}
//	} 






//	//---------------------------------------------------------------------------	TOP-LEVE GUI PROCESSING
//	public void oPanelTabs_TabChange(iGUITabPanel oGuiPanel){

//		return;			//###BROKEN!!

//		iGUIPanel oTabActive = (iGUIPanel)oGuiPanel.allItems[oGuiPanel.activePanel];
//		string sTabCaption = oTabActive.label.text;
		
//		switch (sTabCaption) {
//			case "Cloth Cutting":	CGame.INSTANCE.ChangeGameMode(EGameModes.Morph);	break;
//			case "Cloth Fitting":	CGame.INSTANCE.ChangeGameMode(EGameModes.ClothFit);	break;		//###DESIGN: Consider rename to 'Morph' throughout??
//			case "Game":			CGame.INSTANCE.ChangeGameMode(EGameModes.Play);		break;		//###DESIGN: Game or Play??
//			default:				throw new CException("Exception in iGUI OnTabChange: Could not recognize tab caption '" + sTabCaption + "'");
//		}
//	}


//	[HideInInspector] public iGUIPanel		_oPaneContextMenu;
	////---------------------------------------------------------------------------	PANES
	//[HideInInspector] public iGUIListBox	_oPaneLeft;
	//[HideInInspector] public iGUIListBox	_oPaneRight;
//	[HideInInspector]	public	iGUILabel oLabCaption;

	////---------------------------------------------------------------------------	MISC
	//public static iGUICode_Root INSTANCE;

	//const string C_SaveNewTemplate = "(Save New)";







	//=== From CCutterCurve... no longer loading files from Unity
	////---------------------------------------------------------------------------	LOAD / SAVE
	//public bool LoadCutterCurve() {
	//	string sFilePath = GetFilePath(_eCurveType, _sCurveName);
	//	if (File.Exists(sFilePath) == false) 
	//		return false;
	//	FileStream oFile = new FileStream(sFilePath, FileMode.Open);
	//    BinaryFormatter oBF = new BinaryFormatter();
	//	int nFileVersion 	= (int)oBF.Deserialize(oFile);
	//	int nCurvePts 		= (int)oBF.Deserialize(oFile);
	//	if (nFileVersion != G.C_FileVersion_CurveDefinition)
	//		throw new CException("Exception in SaveCurrentFile().  Unrecognized file version " + nFileVersion + ".  Can only read version " + G.C_FileVersion_CurveDefinition);
		
	//	ResetCurve();				// Reset / destroy the curve before the load

	//	bool bSymmetryX = _eCurveType == ECurveTypes.Side;			//###WEAK: Hardcoded concept of symmetry as applying only to side curve.  May need more symmetry curves later on
	//	CGame.gBL_SendCmd("Curve", "gBL_Curve_Create('" + _eCurveType + "'," + (nCurvePts-1).ToString() + "," + bSymmetryX + ")");		// Rebuild entire curve now that all points set ###NOTE: -1 on num points as first one is center! (not real curve point)
		
	//	//=== Deserialize the vectors that have been stored in the curve definition file and create hotspots for each one of these positions ===
	//	for (int nCurvePt = 0; nCurvePt < nCurvePts; nCurvePt++) {
	//		Vector3 v = CUtility.DeserializeVec(oFile);
	//		CHotSpot oHotSpot = CHotSpot.CreateHotspot(this, null, nCurvePt.ToString(), true, v);	//###BROKEN???  Transform... what to pass in??
	//		oHotSpot.transform.parent = GameObject.Find("(CGame)/(CurveHotSpots)").transform;		//###WEAK: Constants!
	//		_aHotSpots.Add(oHotSpot);
	//	}
	//	for (int nCurvePt = 1; nCurvePt < nCurvePts; nCurvePt++)			// Update all the hotspot from the adjusted position to cloth as calculated by Blender. (Done in separated loop because UpdateCurvePoint needs to have properly set # of points)
	//		UpdateCurvePoint(nCurvePt, _aHotSpots[nCurvePt].transform.position, true);
		
	//	oFile.Close();
	//	UpdateUnityCutterMesh();
	//	Debug.Log("LoadCutterCurve() loaded " + sFilePath);
	//	return true;
	//}
	
	//public void SaveCutterCurve() {
	//	string sFilePath = GetFilePath(_eCurveType, _sCurveName);
	//	FileStream oFile = new FileStream(sFilePath, FileMode.Create);		//###TEMP
	//    BinaryFormatter oBF = new BinaryFormatter();
	//	oBF.Serialize(oFile, G.C_FileVersion_CurveDefinition);			// Prepend each curve definition file with the version number
	//	oBF.Serialize(oFile, _aHotSpots.Count);
	//	foreach (CHotSpot oHotSpot in _aHotSpots)			// ReleaseGlobalHandles the hotspot game objects owned by this game mode to cleanup the scene before the next game mode
	//		CUtility.Serialize(oFile, oHotSpot.transform.position);
	//	oFile.Close();
	//	Debug.Log("SaveCutterCurve() saved " + sFilePath);
	//}
	
	//public static string GetFolderPath(ECurveTypes eCurveType) { return Application.dataPath + "/Resources/CurveDef/Body2/" + eCurveType + "/"; }	// Folder path derived from body type, curve type, curve name  //###HACK! Body type!
	//public static string GetFilePath(ECurveTypes eCurveType, string sCurveName) { return GetFolderPath(eCurveType) + sCurveName + ".CrvDef"; }
