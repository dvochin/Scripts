//using System;
//using UnityEngine;
//using System.IO;
//using System.Reflection;
//using System.Collections;
//using System.Collections.Generic;
//using System.Runtime.InteropServices;


////---------------------------------------------------------------------------	
//public class CGameNet : Photon.MonoBehaviour {
	
//	//---------------------------------------------------------------------------	IMPORTANT MEMBERS
//						public static PhotonView _oBodyPV;

//	[HideInInspector]	public static CGameNet	INSTANCE = null;
	
//	//---------------------------------------------------------------------------	FLAGS
	
//	void Awake() {
//		INSTANCE = this;
//	}
		
//	void OnJoinedLobby() {
//		PhotonNetwork.JoinRandomRoom();
//	}

//	void OnPhotonRandomJoinFailed() {
//		PhotonNetwork.CreateRoom(null, true, true, 4);  // no name (gets a guid), visible and open with 4 players max
//	}

//	void OnJoinedRoom() {
//		//StartGame();							//###HACK!!!!!  Photon starting game this way??? CHECK!!
//		//int nNumBodies = CGame.INSTANCE._aSkinHosts.Count;
//		//GameObject oGO = PhotonNetwork.Instantiate("__Shem", Vector3.zero, Quaternion.identity, 0);		//###LEARN: Pose/rotation doesn't 'take' here... keep in CBody ctor?
//		//oGO.SetActive(true);			// Prefab is stored with top object deactivated to ease development... activate it here...
//		//_oBodyPV = Game.INSTANCE._Body.GetComponent<PhotonView>();
//		//Game.OnBodyChanged();			//###HACK??? CSoftBody now available, let GUI populate itself...
//	}

//	//void OnGUI() {
//		//GUILayout.Label("Player #" + PhotonNetwork.player.ID + " VID:" + (_oBodyPV ? _oBodyPV.viewID.ToString() : "(None)") + " (" + PhotonNetwork.connectionStateDetailed + ")");
//		//GUILayout.Label("Player #" + PhotonNetwork.player.ID + " VID:" + (_oBodyPV ? _oBodyPV.viewID.ToString() : "(None)") + " (" + PhotonNetwork.connectionStateDetailed + ")");
//		//GUILayout.Label(_sGuiMessage1_DEBUG);
//		//GUILayout.Label(_sGuiMessage2_DEBUG);
//		//GUILayout.Label(_sGuiMessage3_DEBUG);
//		//if (PhotonNetwork.connectionStateDetailed == PeerState.Joined)
//		//bool shoutMarco = GameLogic.playerWhoIsIt == PhotonNetwork.player.ID;
//		//if (shoutMarco && GUILayout.Button("Marco!"))
//		//this._oBodyPV.RPC("Marco", PhotonTargets.All);
//	//}
//}

