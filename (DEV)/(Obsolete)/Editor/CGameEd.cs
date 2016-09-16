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
			throw new CException("ERROR: Could not start gBlender library!  Game unusable.");
		System.Diagnostics.Process oProcessBlender = CGame.LaunchProcessBlender("Erotic9.blend");
		if (oProcessBlender == null)
			throw new CException("ERROR: Could not start Blender!  Game unusable.");
		if (ErosEngine.gBL_HandshakeBlender() == false)
			throw new CException("ERROR: Could not handshake with Blender!  Game unusable.");
        _BlenderStarted = true;
        return _BlenderStarted;
    }

	public void UpdateBonesFromBlender() {                      // Update our body's bones from the current Blender structure... Launched at edit-time by our helper class CBodyEd
        //StartBlender();

        GameObject oBoneRootGO = GameObject.Find("Resources/PrefabWoman/Bones");      //###WEAK: Hardcoded path.  ###IMPROVE: Support man & woman body
        oBoneRootGO.SetActive(true);
        Transform oBoneRootT = oBoneRootGO.transform;

        string sNameBodySrc = "WomanA";        //###TEMP  sNameBodySrc.Substring(6);			// Remove 'Prefab' to obtain Blender body source name (a bit weak)
		CMemAlloc<byte> memBA = new CMemAlloc<byte>();
		CGame.gBL_SendCmd_GetMemBuffer("'Client'", "gBL_GetBones('" + sNameBodySrc + "')", ref memBA);		//###TODO: get body type from enum in body plus type!	//oBody._sMeshSource + 
		byte[] oBA = (byte[])memBA.L;
		int nPosBA = 0;

		//=== Decrypt the bytes array that Blender Python prepared for us in 'gBL_GetBones()' ===
		ushort nMagicBegin = BitConverter.ToUInt16(oBA, nPosBA); nPosBA += 2;		// Read basic sanity check magic number at start
		if (nMagicBegin != G.C_MagicNo_TranBegin)
			throw new CException("ERROR in CBodyEd.UpdateBonesFromBlender().  Invalid transaction begin magic number!");

		//=== Read the recursive bone tree.  The mesh is still based on our bone structure which remains authoritative but we need to map the bone IDs from Blender to Unity! ===
		ReadBone(ref oBA, ref nPosBA, oBoneRootT);		//###IMPROVE? Could define bones in Unity from what they are in Blender?  (Big design decision as we have lots of extra stuff on Unity bones!!!)

		CUtility.BlenderSerialize_CheckMagicNumber(ref oBA, ref nPosBA, true);				// Read the 'end magic number' that always follows a stream.

		Debug.Log("+++ UpdateBonesFromBlender() OK +++");
	}

	void ReadBone(ref byte[] oBA, ref int nPosBA, Transform oBoneParent) {							// Precise opposite of gBlender.Stream_SendBone(): Reads a bone from Blender, finds (or create) equivalent Unity bone and updates position
		string sBoneName = CUtility.BlenderStream_ReadStringPascal(ref oBA, ref nPosBA);
		Vector3	vecBone = CUtility.ByteArray_ReadVector(ref oBA, ref nPosBA);
		Transform oBone = oBoneParent.FindChild(sBoneName);
		if (oBone == null) {
			oBone = new GameObject(sBoneName).transform;
			oBone.parent = oBoneParent;
			Debug.LogWarning(string.Format("ReadBone created new bone '{0}' under parent '{1}' and set to position {2:F3},{3:F3},{4:F3}", sBoneName, oBoneParent.name, vecBone.x, vecBone.y, vecBone.z));
		} else {
			Debug.Log(string.Format("ReadBone updated bone '{0}' under parent '{1}' and set position {2:F3},{3:F3},{4:F3}", sBoneName, oBoneParent.name, vecBone.x, vecBone.y, vecBone.z));
		}
		oBone.position = vecBone;
		int nBones = oBA[nPosBA++];
		for (int nBone = 0; nBone < nBones; nBone++)
			ReadBone(ref oBA, ref nPosBA, oBone);
	}
	
	//public override void OnInspectorGUI() {
	//	GUI.Label(new Rect (25, 25, 100, 30), "MouseIns: " + Event.current.mousePosition);
	//	oGame._SphereRadius = EditorGUILayout.Slider("Sphere Radius", oGame._SphereRadius, 0.01f, 0.3f);
	//	if (GUILayout.Button("")
	//}
}
