using UnityEngine;
using UnityEditor;
using System;


[CustomEditor(typeof(CGame))]
public class CGameEd : Editor {         // CGameEd: Provides editor-time services to update Unity information from Blender files.

    public bool _GameIsRunning;
    public bool _BlenderStarted = false;


    bool Initialize() {             //###OBS?
		if (_GameIsRunning)
			return _GameIsRunning;
		Debug.Log("=== CGameEd() INIT ===");
		_GameIsRunning = true;
		return _GameIsRunning;
	}
	
	void OnSceneGUI() {			//###LEARN: This runs a *lot* (e.g. every mouse move!)
		if (_GameIsRunning == false)
			if (Initialize() == false)
				return;
		
		Handles.BeginGUI();
		GUILayout.BeginArea(new Rect(0, 0, 200, 100));
		GUILayout.Label("== CGame GUI ==");

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Start Blender"))
			StartBlender();
        if (GUILayout.Button("Update Bone Positions"))
            UpdateBonesFromBlender();
        GUILayout.EndHorizontal();
		GUILayout.EndArea();
		Handles.EndGUI();
	}

    bool StartBlender() {
        if (_BlenderStarted)                        //###BUG: Doesn't remember if started or not... static variable?
            return _BlenderStarted;		
		if (ErosEngine.gBL_Init(CGame.GetFolderPathRuntime()) == false)
			CUtility.ThrowException("ERROR: Could not start gBlender library!  Game unusable.");
		System.Diagnostics.Process oProcessBlender = CGame.LaunchProcessBlender("Erotic9.blend");
		if (oProcessBlender == null)
			CUtility.ThrowException("ERROR: Could not start Blender!  Game unusable.");
		if (ErosEngine.gBL_HandshakeBlender() == false)
			CUtility.ThrowException("ERROR: Could not handshake with Blender!  Game unusable.");
        _BlenderStarted = true;
        return _BlenderStarted;
    }

	public void UpdateBonesFromBlender() {                      // Update our body's bones from the current Blender structure... Launched at edit-time by our helper class CBodyEd
        //StartBlender();

        GameObject oBoneRootGO = GameObject.Find("Resources/PrefabWomanA/Bones");      //###WEAK: Hardcoded path.  ###IMPROVE: Support man & woman body
        oBoneRootGO.SetActive(true);
        Transform oBoneRootT = oBoneRootGO.transform;

        string sNameBodySrc = "WomanA";        //###TEMP  sNameBodySrc.Substring(6);			// Remove 'Prefab' to obtain Blender body source name (a bit weak)
		CByteArray oBA = new CByteArray("'Client'", "gBL_GetBones('" + sNameBodySrc + "')");

		//=== Read the recursive bone tree.  The mesh is still based on our bone structure which remains authoritative but we need to map the bone IDs from Blender to Unity! ===
		ReadBone(ref oBA, oBoneRootT);		//###IMPROVE? Could define bones in Unity from what they are in Blender?  (Big design decision as we have lots of extra stuff on Unity bones!!!)

		oBA.CheckMagicNumber_End();				// Read the 'end magic number' that always follows a stream.

		Debug.Log("+++ UpdateBonesFromBlender() OK +++");
	}

	void ReadBone(ref CByteArray oBA, Transform oBoneParent) {                          // Precise opposite of gBlender.Stream_SendBone(): Reads a bone from Blender, finds (or create) equivalent Unity bone and updates position
		string sBoneName = oBA.ReadString();
        Vector3 vecBone  = oBA.ReadVector();              // Bone position itself.
        Quaternion quatBone = oBA.ReadQuaternion();       // And its quaternion rotation (in Blender's 90-degree rotated about x domain)

		Transform oBone = oBoneParent.FindChild(sBoneName);
		if (oBone == null) {
			oBone = new GameObject(sBoneName).transform;
			oBone.parent = oBoneParent;
		}
        Debug.LogFormat("ReadBone created bone '{0}' under '{1}' with rot:{2:F3},{3:F3},{4:F3},{5:F3} / pos:{6:F3},{7:F3},{8:F3}", sBoneName, oBoneParent.name, quatBone.x, quatBone.y, quatBone.z, quatBone.w, vecBone.x, vecBone.y, vecBone.z);
        oBone.position = vecBone;
        oBone.rotation = quatBone;
        oBone.Rotate(new Vector3(1, 0, 0),  90, Space.World);           //###LEARN!!!: How to rotate about a global x-axis!
        oBone.Rotate(new Vector3(0, 0, 1), 180, Space.Self);            // Rotate about Z axis so bone points +Y from parent bone to child ###CHECK!

		int nBones = oBA.ReadByte();
		for (int nBone = 0; nBone < nBones; nBone++)
			ReadBone(ref oBA, oBone);
	}
}
