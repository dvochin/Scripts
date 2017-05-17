using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(CGame))]
public class CGameEd : Editor {         // CGameEd: Provides editor-time services to update Unity information from Blender files.

    public bool _GameIsRunning;
    public bool _BlenderStarted = false;

	//Transform _oBoneRootT;
	//Transform _oBoneLastT;

    bool Initialize() {             //###OBS?
		if (_GameIsRunning)
			return _GameIsRunning;
		Debug.Log("=== CGameEd() INIT ===");

		//_oBoneRootT = _oBoneLastT = GameObject.Find("G3F-Stock/Genesis3Female/").transform;
		
		_GameIsRunning = true;
		return _GameIsRunning;
	}
	
	void OnSceneGUI() {			//###LEARN: This runs a *lot* (e.g. every mouse move!)
		if (_GameIsRunning == false)
			if (Initialize() == false)
				return;
		
		Handles.BeginGUI();
		GUILayout.BeginArea(new Rect(0, 0, 300, 100));
		GUILayout.Label("== CGame GUI ==");

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Start Blender"))
			StartBlender();
        if (GUILayout.Button("Update Woman"))
            UpdateBonesFromBlender("Woman");
        if (GUILayout.Button("Update Man"))
            UpdateBonesFromBlender("Man");
        GUILayout.EndHorizontal();
		GUILayout.EndArea();
		Handles.EndGUI();
	}

    bool StartBlender() {
        if (_BlenderStarted)                        //###BUG: Doesn't remember if started or not... static variable?
            return _BlenderStarted;		
		if (ErosEngine.gBL_Init(CGame.GetFolderPathRuntime()) == false)
			CUtility.ThrowException("ERROR: Could not start gBlender library!  Game unusable.");
		System.Diagnostics.Process oProcessBlender = CGame.LaunchProcessBlender();
		if (oProcessBlender == null)
			CUtility.ThrowException("ERROR: Could not start Blender!  Game unusable.");
		if (ErosEngine.gBL_HandshakeBlender() == false)
			CUtility.ThrowException("ERROR: Could not handshake with Blender!  Game unusable.");
        _BlenderStarted = true;
        return _BlenderStarted;
    }

	//void FixBone(string sBoneName, string sBoneLabel, double OX, double OY, double OZ, double EX, double EY, double EZ, double eulX, double eulY, double eulZ, string sRotOrder, double RX, double RY, double RZ, double RW) {
	//	Transform oBoneT = null;

	//	//if (sBoneName.StartsWith("l"))
	//	//	sBoneName = "r" + sBoneName.Substring(1);
	//	//else if (sBoneName.StartsWith("r"))
	//	//	sBoneName = "l" + sBoneName.Substring(1);

	//	while (oBoneT == null || _oBoneLastT == null) {
	//		oBoneT = _oBoneLastT.FindChild(sBoneName);
	//		if (oBoneT == null)
	//			_oBoneLastT = _oBoneLastT.parent;
	//	}

	//	if (oBoneT == null) {
	//		Debug.LogErrorFormat("###ERROR: FixBone() could not find bone '{0}'", sBoneName);
	//		return;
	//	}

	//	Vector3 vecOrigin	= new Vector3((float)OX/100.0f, (float)OY/100.0f, (float)OZ/100.0f);
	//	Vector3 vecEndPoint	= new Vector3((float)EX/100.0f, (float)EY/100.0f, (float)EZ/100.0f);
	//	//Quaternion quatRot  = new Quaternion((float)RX, (float)RY, (float)RZ, (float)RW);
	//	Quaternion quatRot  = Quaternion.Euler((float)(eulX*180.0/Math.PI), (float)(eulY*180.0/Math.PI), (float)(eulZ*180.0/Math.PI));		//Returns a rotation that rotates z degrees around the z axis, x degrees around the x axis, and y degrees around the y axis (in that order)

	//	oBoneT.position = vecOrigin;
	//	oBoneT.rotation = quatRot;

	//	GameObject oBoneArrowGO = Resources.Load("Models/(Tests)/BoneArrow/BoneArrow", typeof(GameObject)) as GameObject;
	//	Transform oBoneArrowT = GameObject.Instantiate(oBoneArrowGO).transform;
	//	Vector3 vecBone = vecEndPoint - vecOrigin;
	//	float nBoneLength = vecBone.magnitude;
	//	oBoneArrowT.parent = oBoneT;
	//	oBoneArrowT.name = oBoneT.name + "->";
	//	oBoneArrowT.localScale = new Vector3(nBoneLength, nBoneLength, nBoneLength);
	//	oBoneArrowT.localPosition = new Vector3();
	//	oBoneArrowT.localRotation = Quaternion.Euler(0,0,0);

	//	Debug.LogFormat("FixBone() updated '{0}'", sBoneName);
	//	_oBoneLastT = oBoneT;					// Remember the last bone we set as we're fed in 'depth first search' order and this bone enables us to lookup parent chain (to avoid complete lookups at every bone)
	//}

	//public void UpdateBonesFromBlender(string sSex) {
	//	Debug.LogWarning("=== FixBone starting ===");
	//	CGameEd self = this;            // For convenience adapting to Blender scripts
	//}












	public void UpdateBonesFromBlender(string sSex) {                      // Update our body's bones from the current Blender structure... Launched at edit-time by our helper class CBodyEd
        //StartBlender();
        GameObject oResourcesGO = GameObject.Find("Resources");
        GameObject oPrefabGO = CUtility.FindObject(oResourcesGO, "Prefab" + sSex + "A");
        oPrefabGO.SetActive(true);
		Transform oBoneRootT = CUtility.FindNodeByName(oPrefabGO.transform, "Bones");

        string sNameBodySrc = sSex + "A";        // Remove 'Prefab' to obtain Blender body source name (a bit weak)
		CByteArray oBA = new CByteArray("'Client'", "gBL_GetBones('" + sNameBodySrc + "')");

		//=== Read the recursive bone tree.  The mesh is still based on our bone structure which remains authoritative but we need to map the bone IDs from Blender to Unity! ===
		ReadBone_RECURSIVE(ref oBA, oBoneRootT);		//###IMPROVE? Could define bones in Unity from what they are in Blender?  (Big design decision as we have lots of extra stuff on Unity bones!!!)

		oBA.CheckMagicNumber_End();				// Read the 'end magic number' that always follows a stream.

		Debug.Log("+++ UpdateBonesFromBlender() OK +++");
	}

	void ReadBone_RECURSIVE(ref CByteArray oBA, Transform oBoneParent) {                          // Precise opposite of gBlender.Stream_SendBone(): Reads a bone from Blender, finds (or create) equivalent Unity bone and updates position
		string sBoneName = oBA.ReadString();

		Transform oBoneT = oBoneParent.FindChild(sBoneName);
		if (oBoneT == null) {
			oBoneT = new GameObject(sBoneName).transform;
			oBoneT.parent = oBoneParent;
		}

		CBone oBone = CUtility.FindOrCreateComponent(oBoneT.gameObject, typeof(CBone)) as CBone;
		oBone.DeserializeFromBlenderStaticBoneImportProcedure(ref oBA);

		int nBoneChildren = oBA.ReadByte();
		for (int nBoneChild = 0; nBoneChild < nBoneChildren; nBoneChild++)
			ReadBone_RECURSIVE(ref oBA, oBoneT);
	}
}
