//#pragma strict    // We can't use usual pragma strict because we have eval (see http://answers.unity3d.com/questions/397906/whats-wrong-with-eval.html?sort=oldest and http://answers.unity3d.com/questions/314944/interface-error-with-eval.html)
import System;

public class CEvalProxy extends MonoBehaviour {

	static var INSTANCE : CEvalProxy;
    var EXPORTS = {};                   // The critical collection of 'exported objects'.  Unity codebase game objects MUST be in this collection for scripts to be able to call codebase objects.  Unity objects register themselves with SetExport()
    var _sNameNextExportCall = "";      //###WEAK: Temporary storage of export name in 'SetExportName' 'SetExportObject' calls.  (Used as a weak workaround to SendMessage only allowing one argument)
    
	var LastCodeLine;					//###OPT: Keep?
	var LastResults;

	var _nTimeResume : float;			// Time.time value when next script statement will execute.  Used to pace scripting playback to the pace the script-designer set
	
	public function OnStart(oBodyBase) {         //###DEV26: What to export???
		Debug.Log("=== CEvalProxy.OnStart() ===");
        INSTANCE = this;			//###CHECK: Needed??
    }
    public function ExportAdd_Name(sNameExport) {
        _sNameNextExportCall = sNameExport;         // Set name of export for upcoming call to SetExportObject()
    }
    public function ExportAdd_Object(oExportObject) {
		Debug.Log("CEvalProxy.ExportAdd('" + _sNameNextExportCall + "') = '" + oExportObject.ToString() + "'");
        EXPORTS[_sNameNextExportCall] = oExportObject;
    }
    public function ExportRemove(sNameExport) {
		Debug.Log("CEvalProxy.ExportRemove('" + sNameExport + "') = (NULL)");
        EXPORTS[sNameExport] = null;       //delete EXPORTS[sNameExport];     //###IMPROVE: This call does not compile!  (Unity asks for a ';'!!)
    }
	public static function PropSet(nTimeWait : float, oObj, sPropName : String, nValue : float) {
		oObj.PropSet(0, sPropName, nValue);				//###HACK24:!!!
		if (nTimeWait != 0)
			WaitOnTimer(nTimeWait);
	}

	public static function PropSetAll(nTimeWait : float, oObj, aValues) {	//###DESIGN!!! Static???
		var oPropGrp_HACK = oObj._aPropGrps[0];
		if (aValues.length != oPropGrp_HACK._aProps.length) {							//###HACK24:!!! PropGroups!!  What to do with multiple groups??
			Debug.LogError("Script Error in PropSetAll().  Mismatch argument count");		//###INFO: How to accept multiple arguments in JScript
			return;
		}
	
		var nProps = oPropGrp_HACK._aProps.length;
		for (var nProp = 0; nProp < nProps; nProp++) {
			var oProp = oPropGrp_HACK._aProps[nProp];
			oProp.PropSet(aValues[nProp]);
		}
		
		if (nTimeWait != 0)
			WaitOnTimer(nTimeWait);
	}

	public static function Log(nTimeWait : float, sText : String) {
		Debug.Log("EvalLog> " + sText);
		if (nTimeWait != 0)
			WaitOnTimer(nTimeWait);
	}

	public static function WaitOnTimer(nTimeWait : float) {
	    INSTANCE._nTimeResume = Time.time + nTimeWait;
	}

    public function EvaluateScript(sEvalPacked: String) {
        var nScriptLine = 0;        //###IMPROVE: Put back script line input?
	    var sMsgError = "";
	    if (sEvalPacked == "")
	        return;

        //=== Separate the first symbol before the first . period and expand to EXPORT['<VarName>'].<Command> ===
        var nPosFirstPeriod = sEvalPacked.IndexOf(".");
        var sNameExport     = sEvalPacked.Substring(0, nPosFirstPeriod);
        var sCommand        = sEvalPacked.Substring(nPosFirstPeriod+1);
        sEval = "EXPORTS['" + sNameExport + "']." + sCommand;

	    var sMsgDump = "";
        var oOutput = null;     //###IMPROVE: Starting to become a central catch-all for most of the game... improve logging?

        //###LEARN: Visual Studio does NOT show the proper variable values!  However in the 'auto' watch, there is a sub-tree named something like '$3' that has the proper values!!  (Maybe if we setup VS project properly for JScript??)
	    try {
	        //###INFO: If UnityVS can't compile this file because of IEvaluationDomainProvider (see http://forum.unity3d.com/threads/199342-IEvaluationDomainProvider-missing-in-Unity-4-2-1f4-when-compiling-js-code-from-cs) it is because Visual Studio is missing the reference to 'UnityScript' reference.
	        //###INFO: GenProjectUnityVS patches the way UnityVS generates its project for Visual Studio... we need to add a reference to UnityScript so that JS scripts containing eval can compile in UnityVS
	        sMsgDump = String.Format("Script#{0}  '{1}'", nScriptLine, sEval);
	        oOutput = eval(sEval, "Safe");		//###CHECK!!!!!! ###SECURITY!!!!! Make sure "Safe" is ALWAYS set to avoid becoming a gateway for viruses!!!
            if (oOutput != null) {
                sMsgDump += " => " + oOutput.ToString();
                CEvalProxy.INSTANCE.LastResults = oOutput.ToString();
            } else {
                CEvalProxy.INSTANCE.LastResults = "(NULL)";
            }
	    } catch (e) {
	        sMsgError = String.Format("###ERROR {0} Error {1} in '{2}' = {3}", nScriptLine, e.ToString(), sEval, (oOutput == null) ? "(Null)" : oOutput.ToString());
	        Debug.LogError(sMsgError);
	        oOutput = sMsgError;
	    }
        Debug.Log(sMsgDump);
	    CEvalProxy.INSTANCE.LastCodeLine = sEval;
	    return oOutput;
	}
}

//var e = EXPORTS;      //###LEARN: VS debugger is NOT able to properly debug in this jscript code I don't know why.  With this statement the debugger will show the result of 'EXPORTS' but 'e' is null even though it really is not!  Eval statement belows properly executes however... so check everything in jscript with 'printf' debugging!!
//var oGame2 = EXPORTS['GameInstance'];   //###LEARN: Dictionary must be used in this way.  'EXPORTS.GameInstance' form does NOT work!!
//Browser         = GameInstance._oBrowser_HACK;  //Have to it here cuz null at init time (Browser created too late)

    //var BodyBase;						//###IMPORTANT: Our C# script that is the final destination to all our commands.  (Most script lines have 'BodyBase' in their string)
    //var GameInstance;
    //var Browser;

