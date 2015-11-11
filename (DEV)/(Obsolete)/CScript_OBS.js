////#pragma strict		// We can't use usual pragma strict because we have eval (see http://answers.unity3d.com/questions/397906/whats-wrong-with-eval.html?sort=oldest and http://answers.unity3d.com/questions/314944/interface-error-with-eval.html)
//import System;
//import System.IO;


//public class CScript extends MonoBehaviour {

//	static var INSTANCE : CScript;
//	var _nScriptLineCurrent : int;
//	var _aScriptLines : ArrayList;

//	var ScriptFile : String;

//	var Run		: Boolean;
//	var Reload	: Boolean;

//	var Body1	: CBodyProxy;
//	var Body2	: CBodyProxy;

//	var _nTimeResume : float;			// Time.time value when next script statement will execute.  Used to pace scripting playback to the pace the script-designer set
	

//	function Start() {
//		INSTANCE = this;
//		CGame.s_oScriptInterpreter = this;
//		Body1	= CGame.s_aBodyProxies[0];		// Obtain references to the bodies scripts can control through the s_aBodyProxies array prepared for us
//		Body2	= CGame.s_aBodyProxies[1];		//###DESIGN: Keep 1 based?
//		LoadScript();
//	}

//	function LoadScript() {
//		var oFileReader = new StreamReader(ScriptFile);
//		_aScriptLines = new ArrayList();

//		while (oFileReader.Peek() >= 0) {		//###TODO!!
//			var sLine = oFileReader.ReadLine();
//			_aScriptLines.Add(sLine);
//		}
//		oFileReader.Close();
//		_nScriptLineCurrent = 0;
//	}
	
//	function FixedUpdate() {					//###DESIGN: Run at much lower frequency if we're Runing to poll???
//		if (Reload) {
//			LoadScript();
//			Reload = false;
//		}
//		if (Run == false)						//###TEMP
//			return;
//		if (_nScriptLineCurrent >= _aScriptLines.Count)
//			return;

////		if (Input.GetKey(KeyCode.Space) == false)
////			return;

//		if (_nTimeResume > Time.time)
//			return;

//		EvaluateScriptLine(_nScriptLineCurrent++);
//	}



//	public static function Set(nTimeWait : float, oObj : CObject, sPropName : String, nValue : float) {
//		oObj.PropSet(sPropName, nValue);		//###NOTROBUST
//		if (nTimeWait != 0)
//			WaitOnTimer(nTimeWait);
//	}

//	public static function SetAll(nTimeWait : float, oObj : CObject, aValues) {
//		if (aValues.length != oObj._aProps.length) {
//			Debug.LogError("Script Error in SetAll().  Mismatch argument count");
//			return;
//		}
	
//		var nProps = oObj._aProps.length;
//		for (var nProp = 0; nProp < nProps; nProp++) {
//			var oProp = oObj._aProps[nProp];
//			oProp.PropSet(aValues[nProp]);
//		}
		
//		if (nTimeWait != 0)
//			WaitOnTimer(nTimeWait);
//	}

//	public static function Log(nTimeWait : float, sText : String) {
//		Debug.Log("EvalLog> " + sText);
//		if (nTimeWait != 0)
//			WaitOnTimer(nTimeWait);
//	}

//	public static function WaitOnTimer(nTimeWait : float) {
//		INSTANCE._nTimeResume = Time.time + nTimeWait;
//	}

//	public static function TestInJS(sMsg) {
//		oTypeScriptInterpreter = CGame.s_oTest.GetType();
//		aMethods = oTypeScriptInterpreter.GetMethods();
//		Debug.Log("Methods : " + aMethods.length);
//		aMethod = oTypeScriptInterpreter.GetMethod("TestInCS");
//		if (aMethod == null)
//			Debug.Log("TestInJS NULL METHOD!   " + sMsg);
//		aMethod.Invoke(CGame.s_oTest, [ sMsg + "FromJS!!!"]);
//	}
	

	
//	function EvaluateScriptLine(nScriptFileCurrentLine : int) {
//		var sEval = _aScriptLines[nScriptFileCurrentLine];
//		if (sEval == "")
//			return;
//		var oOutput = null;
//		try {
//			oOutput = eval(sEval, "Safe");		//###CHECK!!!!!! ###SECURITY!!!!! ###DESIGN: Can we make do with safe???
//			Debug.Log(String.Format("#{0} '{1}' = {2}", nScriptFileCurrentLine, sEval, (oOutput == null) ? "(Null)" : oOutput.ToString()));
//		} catch (e) {
//			Debug.LogError(String.Format("#{0} Error {1} in '{2}' = {3}", nScriptFileCurrentLine, e.ToString(), sEval, (oOutput == null) ? "(Null)" : oOutput.ToString()));
//		}
//	}
//}