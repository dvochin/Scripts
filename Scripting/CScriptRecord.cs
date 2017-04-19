using UnityEngine;
using System;
using System.IO;
using System.Collections;

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

	public void WriteObject(CObject oObject) {	
		// Serialize_OBS all CProp properties of this object onto currently opened script recorder onto a 'SetAll()' function

		string sScriptLine = string.Format("SetAll(0, Body.{0}, [", oObject._sNameObject);		//###DESIGN!!! Sometimes write which body??
		//###BROKEN19:
		//foreach (CProp oProp in oObject._aProps)
		//	sScriptLine += oProp.PropGet().ToString("F3") + ",";
		sScriptLine = sScriptLine.Substring(0, sScriptLine.Length - 1) + "]);";
		Debug.Log(sScriptLine);
		_oStreamWriterScriptFile.WriteLine(sScriptLine);
		_oStreamWriterScriptFile.Flush();			// Keep?
	}

	public void WriteProperty(CProp oProp) {
		// Serialize_OBS a single CProp property onto existing script record file.  Basically records the 'transactions' that user-action pushes onto the scene

		float nTime = Time.time;
		float nTimeDelta = nTime - _nTimeLastScriptWrite;
		if (nTimeDelta > 3) nTimeDelta = 3;		//###DESIGN: Keep max??
		_nTimeLastScriptWrite = nTime;
		string sScriptLine = string.Format("Set({0:F2}, Body.{1}, '{2}', {3:F3});", nTimeDelta, oProp._oPropGrp._oObj._sNameObject, oProp._sNameProp, oProp._nValueLocal);
		//Debug.Log(sScriptLine);
		_oStreamWriterScriptFile.WriteLine(sScriptLine);
	}
}
