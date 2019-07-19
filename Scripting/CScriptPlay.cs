/*###DISCUSSION: EroticVR Script
=== NEXT ===
 * Develop first test script to spread legs, get dick bigger, prompt, etc

=== TODO ===
 * WaitOnUser() to wait for spacebar
 * Get() and storage of variables... kept somewhere???
 * Screen captions.

=== LATER ===

=== IMPROVE ===

===== DESIGN =====
=== NEEDS ===
 * The script handler has to run on its own thread and block on various events: timeout, target on some global var (lust), event (penetration), etc

=== UNCERTAINTIES ===
 * What can we do to improve recording of vector3 and quaternion changes???
   * Animate vector by a vector (and a quaternion by an axis and angle) instead of seperating x,y,z?

=== SIMPLIFICATIONS ===
 * For now we assume we script only for one man/shemale (slot 1) and a woman (slot 2)... scripts not expected to fully work otherwise.

=== IDEAS ===

=== LEARNED ===

=== PROBLEMS ===
 *+++++ Damn problem compiling UnityVS because of eval problem!!
 * Having animated properties will make script playback fail to set these properties

=== PROBLEMS??? ===

=== WISHLIST ===
 * Clipboard entry of transactions??
 * GUI for script files with listbox-style controls to go up/down?

*/

using UnityEngine;
using System.Collections;
using System.IO;


public class CScriptPlay : MonoBehaviour {		// CScriptPlay: Responsible for loading / parsing / executing EroticVR script files (that themselves were recorded with CScriptRecord and optionally edited by pose designer / user)

	public	string	_ScriptFile;
	public	int		_CurrentLine;

	public	bool	_Run;
	public	bool	_Reload;

	ArrayList		_aScriptLines;          // The script file code lines, one string per code line

    GameObject      _oEvalProxy;			// Our JavaScript class that enables C# to evaluate script code by using Javascript's eval. (neat/useful trick)  We communicate with it via 'SendMessage()' to avoid complex project build issues.


	public void OnStart(CGame oGame) {      //###DESIGN: Keep game argument?
        //_oEvalProxy = gameObject.AddComponent<CEvalProxy>();
        Transform oEvalProxyT = CGame.INSTANCE.transform.Find("CEvalProxy");
        _oEvalProxy = oEvalProxyT.gameObject;
        _oEvalProxy.SendMessage("OnStart", oGame);
        ExportAdd("GameInstance", oGame);
    }

    public void ExportAdd(string sNameExport, object oExportObject) {
        _oEvalProxy.SendMessage("ExportAdd_Name",    sNameExport);       // We must first push in the name then the object (weak workaround to 'SendMessage' only accepting one argument)
        _oEvalProxy.SendMessage("ExportAdd_Object",  oExportObject);
    }

    public void ExportRemove(string sNameExport) {
        _oEvalProxy.SendMessage("ExportRemove",    sNameExport);
    }

  //  public void OnUpdate() {							//###DESIGN: _Run at much lower frequency if we're Runing to poll???
		//if (_Reload) {              //#DEV27: ###OBSOLETE?
		//	LoadScript(_ScriptFile);
		//	_Reload = false;
		//}
		//if (_Run == false)						//###TEMP
		//	return;
		//if (_CurrentLine >= _aScriptLines.Count)
		//	return;

		//string sScriptLine = (string)_aScriptLines[_CurrentLine];
        //_oEvalProxy.SendMessage("EvaluateScript", sScriptLine);
		//_CurrentLine++;
    //}

    public void LoadScript(string sScriptFile) {

		if (File.Exists(sScriptFile)) {
			_ScriptFile = sScriptFile;

			StreamReader oFileReader = new StreamReader(_ScriptFile);
			_aScriptLines = new ArrayList();

			while (oFileReader.Peek() >= 0) {		//###TODO!!
				string sLine = oFileReader.ReadLine();
				_aScriptLines.Add(sLine);
			}
			oFileReader.Close();
			_CurrentLine = 0;
		} else {
			Debug.LogError("CScriptPlay.LoadScript() cannot find file " + sScriptFile);
		}
	}

	public void ExecuteAll() {				
		//int nLine = 0;
		if   (_aScriptLines == null)
			return;
        foreach (string sScriptLine in _aScriptLines)
			_oEvalProxy.SendMessage("EvaluateScript", sScriptLine);       //###IMPROVE:!! Keep going even when errors?  No logging / returning of results??? WTF??
    }

	public void ExecuteLine(string sCodeLine) {
		_oEvalProxy.SendMessage("EvaluateScript", sCodeLine);
	}
}
