using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Reflection;

[CustomEditor(typeof(CBody))]
public class CBodyEd : Editor {			// CBodyEd: Editor-time class to update a CBody's bone structure from Blender by pressing a button that appears in Unity Editor GUI.

	void OnSceneGUI() {
		CBody oBody = (CBody)target;

		Handles.BeginGUI();
		GUILayout.BeginArea(new Rect(0, 0, 200, 300));
		GUILayout.Label("== CBody Menu ==");
		if (GUILayout.Button("Start Blender"))
			StartBlender(oBody);
		if (GUILayout.Button("Update Bone Positions"))
			oBody.UpdateBonesFromBlender();
		if (GUILayout.Button("Reset Pins To Bones"))
			oBody.ResetPinsToBones();
		GUILayout.EndArea();
		Handles.EndGUI();
	}

	void StartBlender(CBody oBody) {
		//=== Initialize access to gBlender in edit mode ===
		if (ErosEngine.gBL_Init(CGame.GetFolderPathRuntime()) == false)
			throw new CException("ERROR: Could not start gBlender library!  Game unusable.");
		System.Diagnostics.Process oProcessBlender = CGame.LaunchProcessBlender();
		if (oProcessBlender == null)
			throw new CException("ERROR: Could not start Blender!  Game unusable.");
		if (ErosEngine.gBL_HandshakeBlender() == false)
			throw new CException("ERROR: Could not handshake with Blender!  Game unusable.");
	}
}
