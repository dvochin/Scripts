using UnityEngine;
using System.IO;
using System;

public class CScriptRecord {		// CScriptRecord: Appends scriptable-actions to an open script file represening user actions in a scene over time.  Files can be played back with CScriptPlay

	public string	_ScriptFile;

	StreamWriter	_oStreamWriterScriptFile;
	float			_nTimeLastScriptWrite;

	public CScriptRecord(string sScriptFile, string sHeader) {
		_ScriptFile = sScriptFile;
		_oStreamWriterScriptFile = new StreamWriter(_ScriptFile);
		_oStreamWriterScriptFile.AutoFlush = true;
		_oStreamWriterScriptFile.WriteLine("Log(0,'===== EroticVR Script Recorder: " + sHeader + " =====')");
	}

	public void WriteObject(CObj oObject) {	
		// Serialize all CObj properties of this object onto currently opened script recorder onto a 'SetAll()' function

		string sScriptLine = string.Format("SetAll(0, BodyBase.{0}, [", oObject._sName);		//###DESIGN!!! Sometimes write which body??  ###WEAK:!!! Huge amount of assumptions between script record, script play and script executor!!!
		foreach (CObj oObj in oObject._aChildren)		//###HACK:!!!! Iterating through all props!
			sScriptLine += oObj.Get().ToString("F3") + ",";
		sScriptLine = sScriptLine.Substring(0, sScriptLine.Length - 1) + "]);";
		Debug.Log(sScriptLine);
		_oStreamWriterScriptFile.WriteLine(sScriptLine);
		_oStreamWriterScriptFile.Flush();			// Keep?
	}

	public void WriteProperty(CObj oObj) {
		// Serialize a single CObj property onto existing script record file.  Basically records the 'transactions' that user-action pushes onto the scene

		float nTime = Time.time;
		float nTimeDelta = nTime - _nTimeLastScriptWrite;
		if (nTimeDelta > 3) nTimeDelta = 3;		//###DESIGN: Keep max??
		_nTimeLastScriptWrite = nTime;
		//###BROKEN #DEV26
		//string sScriptLine = string.Format("Set({0:F2}, BodyBase.{1}, '{2}', {3:F3});", nTimeDelta, oObj._oObjParent._oObj._sName, oObj._sName, oObj._nValue);
		//Debug.Log(sScriptLine);
		//_oStreamWriterScriptFile.WriteLine(sScriptLine);
	}

	public void CloseFile() {							//###CHECK: Needed??  dtor can do soon enough?
		_oStreamWriterScriptFile.Close();
		_oStreamWriterScriptFile = null;
	}
}
