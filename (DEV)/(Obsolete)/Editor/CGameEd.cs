using UnityEngine;
using UnityEditor;
using System;


//[CustomEditor(typeof(CGame))]
public class CGameEd : Editor {

//	public bool					_GameIsRunning;
//	
//	bool Initialize() {
//		if (_GameIsRunning)
//			return _GameIsRunning;
//		
//		Debug.Log("=== CGameEd() INIT ===");
//
//		_GameIsRunning = true;
//		return _GameIsRunning;
//	}
//	
//	void OnSceneGUI() {			//###LEARN: This runs a *lot* (e.g. every mouse move!)
//		if (_GameIsRunning == false)
//			if (Initialize() == false)
//				return;
//		
//		//Event e = Event.current;
//		//CGame oGame = (CGame)target;
//		
//		Handles.BeginGUI();
//		GUILayout.BeginArea(new Rect(0, 0, 200, 100));
//		GUILayout.Label("== CGame Helper ==");
//
//		GUILayout.BeginHorizontal();
//		//if (GUILayout.Button("Clone Colliders", GUILayout.Width(100))) {		//###DISABLED: Not safe enough!
//			//CopyComponentTree(GameObject.Find("Woman8/Root/Sex/hip/abdomen/chest/lCollar"), GameObject.Find("Woman8/Root/Sex/hip/abdomen/chest/rCollar"), typeof(CapsuleCollider));  
//		//}
////		if (GUILayout.Button("Test Socket", GUILayout.Width(100))) {		//###DISABLED: Not safe enough!
////			CGame.TestSocket();
////		}
//		GUILayout.EndHorizontal();
//		GUILayout.EndArea();
//		Handles.EndGUI();
//	}
//
	
//	public override void OnInspectorGUI() {
//		//GUI.Label(new Rect (25, 25, 100, 30), "MouseIns: " + Event.current.mousePosition);
//		oGame._SphereRadius = EditorGUILayout.Slider("Sphere Radius", oGame._SphereRadius, 0.01f, 0.3f);
//		if (GUILayout.Button("")
//	}
}

//                // Encode the data string into a byte array.
//                byte[] msg = Encoding.ASCII.GetBytes("This is a test<EOF>");
//
//                // Send the data through the socket.
//                int bytesSent = sender.Send(msg);
//
//                // Receive the response from the remote device.
//                int bytesRec = sender.Receive(bytes);
//                Debug.Log("Echoed test = " + Encoding.ASCII.GetString(bytes,0,bytesRec));

