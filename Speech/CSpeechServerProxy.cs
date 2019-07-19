/*###TODO: Speech commands
=== TODO ===
- Ok, very early implementation of moving body parts.
    - Blows up!
    - Really crapped on old wand implementation... rethink for new speech based build (and backup old)
    - Bitch having to re-start speech server... automate graceful startup / shutdown with Unity.
    - Same with web server... start with windows?
    - Speech file a mess... threading issues, distributed talk to server,
    - Why does Unity freeze when we change grammar??


- Need 'grammar group' concept.
- Map wand 'speech button' to grammar group change
- Add server command to activate proper grammar only.
- Phrase, grammar, action???
- Merge CSpeech with SRE?


- "[Left | Right] wand controls [Left | Right | One | Two ]": wand assignment to which character
    - Default is 'Left wand controls Left character' and 'Right wand controls right character'
- Need to switch between two grammars:
    - Global commands (No Wand 'Speech button' pressed)
    - Wand commands: (Wand 'Speech button' pressed)
        - Sent to the wand to set what happens when trigger is pressed.
        - As a wand is always assigned a body these map to body as appropriate


*/

using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;
using NetMQ;
using NetMQ.Sockets;

public class CSpeechServerProxy : MonoBehaviour
{
	private static  System.Diagnostics.Process	_oProcess_SpeechServer;	// Our speech server.  Makes it possible to use Microsoft Speech with full grammer builder in Unity on any recent version of Windows!
    public static CSpeechServerProxy INSTANCE;

    public List<CPhrase> _aPhrases = new List<CPhrase>();
    public static Dictionary<string, CPhrase> _mapPhrases = new Dictionary<string, CPhrase>();
    CPhrase _oPhraseCurrent;
    public RequestSocket _oSocket_RequestsToSpeechServer;

    public static string C_Separator_FunctionName   = "<|>";
    public static string C_Separator_Arguments      = "<->";
    public static string[] _aSeparator_FunctionName;
    public static string[] _aSeparator_Arguments;

    private Thread _oThread_OnSpeechEvents;
    bool _bThreadSubscriber_Exited = false;
    bool _bThreadOnSpeechIncoming = false;
    string _sThreadIn_SpeechText, _sThreadIn_NameGrammar;
    float _nThreadIn_ConfidenceLevel;
    CVrWand _oVrWand_BeingConfigured;       // VR Wand being configured via speech recognition.  (Happens when user presses ' configure wand by speech command' button)

    public void DoStart() {
        Debug.Log("=== CSpeechServerProxy starting ===");

        INSTANCE = this;

        _oProcess_SpeechServer = CGame.LaunchProcess_ErosSpeechServer();
		if (_oProcess_SpeechServer == null)
			CUtility.ThrowException("ERROR: Could not start Eros Speech Server!  Game unusable.");

        _aSeparator_FunctionName = new string[1];
        _aSeparator_FunctionName[0] = C_Separator_FunctionName;
        _aSeparator_Arguments = new string[1];
        _aSeparator_Arguments[0] = C_Separator_Arguments;

        AsyncIO.ForceDotNet.Force();        //###SOURCE: https://github.com/zeromq/netmq/issues/526 (bottom)
        NetMQConfig.Cleanup();

        _oSocket_RequestsToSpeechServer = new RequestSocket(">tcp://localhost:5555");

        _oThread_OnSpeechEvents = new Thread(Thread_OnSpeechEvents);
        _oThread_OnSpeechEvents.Start();

        CPhrase P;

        //P = CreatePhrase("PlaceHand");
        //P.AddWord(null, 1, "Place");
        //P.AddWord(null, 0, "your");
        //P.AddWord("Side", 0, "left", "right");
        //P.AddWord(null, 1, "hand on");
        //P.AddWord("Who", 1, "your", "his", "her");
        //P.AddWord("Destination", 1, "tits", "tit", "breasts=tits", "breast=tit", "boobs=tits", "boob=tit", "cock", "penis=cock", "cunt", "hip", "inner thigh", "abdomen", "head");

        //==================================================================    ALWAYS ON GRAMMAR
        P = CreatePhrase('A', "TestingAlwaysGrammar");
        P.AddWord(null, 1, "Testing Always Grammar");


        //==================================================================    GLOBAL GRAMMAR
        P = CreatePhrase('G', "TestingGlobalGrammar");
        P.AddWord(null, 1, "Testing Global Grammar");


        //==================================================================    WAND CONTROL GRAMMAR
        P = CreatePhrase('W', "TestingWandGrammar");
        P.AddWord(null, 1, "Testing Wand Grammar");

        P = CreatePhrase('W', "WandMove");
        P.AddWord(null, 1, "Move");
        P.AddWord("MoveWhat", 1, "Torso", "Pelvis", "Chest", "Left Hand", "Right Hand", "Both Hands", "Left Foot", "Right Foot", "Both Feet");

        P = CreatePhrase('W', "TestingHandAction");
        P.AddWord(null, 1, "Hand");
        P.AddWord("HandOnWhat", 1, "C", "Normal");

        ActivateGrammarGroup('G');              // Activate global grammar at startup.  Wand grammar activated upon proper wand button press

        SendMsgToSpeechServer("INITIALIZE");

        Debug.Log("--- CSpeechServerProxy started ---");
    }

    public void DoDestroy() {
        if (_oProcess_SpeechServer != null) {
            Debug.Log("=== CSpeechServerProxy termination starting ===");

            //=== Speech server shutdown procedure ===
            //- We send the 'SHUTDOWN' command.
            //- Speech server returns "SHUTDOWN_IN_PROGRESS" to both its response socket and its publisher socket.  It then terminates.
            //- Our response socket gets "SHUTDOWN_IN_PROGRESS" and block-waits until subscriber thread ends.
            //- Our subscriber socket also gets "SHUTDOWN_IN_PROGRESS", cleans up its socket and ends its thread.
            //- Main thread in unblocked by helper thread exiting, cleans up its socket and its process variable.

            string sMsgFromServer = SendMsgToSpeechServer("SHUTDOWN");          // Send shutdown command, server will send "TERMINATED" to our subscriber socket so we can close that socket and our helper thread properly
            Debug.Assert(sMsgFromServer == "SHUTDOWN_IN_PROGRESS");

            //=== Wait for the helper thread to gets its termination message and flag for join ===
            while (_bThreadSubscriber_Exited == false) {}
            _oThread_OnSpeechEvents.Join();
            _oThread_OnSpeechEvents = null;
            Debug.Log("--- CSpeechServerProxy termination: helper thread ended ---");

            //_oSocket_RequestsToSpeechServer.Disconnect("");     //###HACK: Get SSP to close?  Or move here?
            _oSocket_RequestsToSpeechServer.Close();
            _oSocket_RequestsToSpeechServer = null;
            NetMQConfig.Cleanup();

            _oProcess_SpeechServer = null;
            INSTANCE = null;            
            //enabled = false;      //###CHECK:!!!  Disable instead of destroy?
            Debug.Log("--- CSpeechServerProxy termination finished ---");
            GameObject.Destroy(this);
        }
    }


    public string SendMsgToSpeechServer(string sServerFunction, string sFunctionArgs = "") {
        string sMsg = sServerFunction;
        if (sFunctionArgs.Length > 0)
            sMsg += C_Separator_FunctionName + sFunctionArgs;
        Debug.LogFormat("OUT: Sending '{0}'", sMsg);
        _oSocket_RequestsToSpeechServer.SendFrame(sMsg);
        string sMsgFromServer = _oSocket_RequestsToSpeechServer.ReceiveFrameString();
        if (sMsgFromServer != "OK")
            Debug.LogWarningFormat("IN : Received '{0}'", sMsgFromServer);
        //Debug.AssertFormat(sMsgFromServer == "OK", "Speech Server returned error '{0}'!", sMsgFromServer);
        return sMsgFromServer;
    }

    public CPhrase CreatePhrase(char chGrammarGroupID, string sNamePhrase) {
        if (_oPhraseCurrent != null)
            FinalizePhrase();
        _oPhraseCurrent = new CPhrase(chGrammarGroupID, sNamePhrase);
        _aPhrases.Add(_oPhraseCurrent);
        _mapPhrases.Add(sNamePhrase, _oPhraseCurrent);
        return _oPhraseCurrent;
    }

    public void FinalizePhrase() {
        _oPhraseCurrent = null;
        SendMsgToSpeechServer("FinalizePhrase");
    }

    public void ActivateGrammarGroup(char chGrammarGroupID) {
        SendMsgToSpeechServer("ActivateGrammarGroup", chGrammarGroupID.ToString());
    }

    public void OnSpeechRecognized(string sNameGrammar, string sTextRecognized, float nRecogConfidence) {
        CPhrase oPhrase = _mapPhrases[sNameGrammar];

        Console.WriteLine(String.Format("{0}: Grammar='{1}' = '{2}'", nRecogConfidence, sNameGrammar, sTextRecognized));

        if (sTextRecognized == "Exit")
            Console.WriteLine(String.Format("EXIT!"));
    }

    void Update() {
        if (_bThreadOnSpeechIncoming) {         //#DEV27:
            _bThreadOnSpeechIncoming = false;

            if (_sThreadIn_NameGrammar == "W_WandMove") {     //###WEAK: Use constants?
                if (_oVrWand_BeingConfigured) {     //#DEV27?
                    Transform oNode = null;
                    CBodyBase oBodyBase = CGame.FindLeftRightBody(_oVrWand_BeingConfigured._chNameWand);

                    if (_sThreadIn_SpeechText.Contains("Torso")) {      //###WEAK: Contains?    //###MOVE:???
                        oNode = oBodyBase._oActor_Genitals.transform;
                    } else if (_sThreadIn_SpeechText.Contains("Chest")) {
                        oNode = oBodyBase._oActor_Chest.transform;
                    } else if (_sThreadIn_SpeechText.Contains("Pelvis")) {
                        oNode = oBodyBase._oActor_Pelvis.transform;
                    } else if (_sThreadIn_SpeechText.Contains("Left Hand")) {
                        oNode = oBodyBase._oActor_ArmL.transform;
                    } else if (_sThreadIn_SpeechText.Contains("Right Hand")) {
                        oNode = oBodyBase._oActor_ArmR.transform;
                    } else if (_sThreadIn_SpeechText.Contains("Left Foot")) {
                        oNode = oBodyBase._oActor_LegL.transform;
                    } else if (_sThreadIn_SpeechText.Contains("Right Foot")) {
                        oNode = oBodyBase._oActor_LegR.transform;
                    }
                    _oVrWand_BeingConfigured.AssignToObject(oNode);         
                }
            } else if (_sThreadIn_NameGrammar == "W_TestingHandAction") {
                if (_sThreadIn_SpeechText.Contains("Hand C")) {
                    CGame.INSTANCE.GetBodyBase(0)._oActor_ArmL.HandMode_Change(CActorArm.EHandMode.MasturbatingPenis);
                } else if (_sThreadIn_SpeechText.Contains("Hand Normal")) {
                    CGame.INSTANCE.GetBodyBase(0)._oActor_ArmL.HandMode_Change(CActorArm.EHandMode.Normal);
                }
            }
        }
    }

    private void Thread_OnSpeechEvents(){
        const string SubscriberAddress = "tcp://localhost:12345";
        SubscriberSocket oSockSubscriber = new SubscriberSocket();
        oSockSubscriber.Options.ReceiveHighWatermark = 1000;
        oSockSubscriber.Connect(SubscriberAddress);
        oSockSubscriber.Subscribe("");
        Debug.Log("Thread_OnSpeechEvents initializing");
        while (true) {
            string sMsgPacked = oSockSubscriber.ReceiveFrameString();
            if (sMsgPacked == "SHUTDOWN_IN_PROGRESS") {
                break;      // Speech server is in the process of shutting down.  Break our infinite loop so we close our sockets and have this thread be joined with main thread and destroyed.
            } else {
                string[] aMsgArgs = sMsgPacked.Split(_aSeparator_Arguments, StringSplitOptions.RemoveEmptyEntries);
                _nThreadIn_ConfidenceLevel = float.Parse(aMsgArgs[0]);
                _sThreadIn_NameGrammar = aMsgArgs[1];
                _sThreadIn_SpeechText = aMsgArgs[2];
                string sMsg = $"Speech Conf:{_nThreadIn_ConfidenceLevel} Gram:'{_sThreadIn_NameGrammar}' = '{_sThreadIn_SpeechText}'";
                Debug.Log(sMsg);
                CGame._aDebugMsgs[(int)EMsg.Speech] = sMsg;
                _bThreadOnSpeechIncoming = true;
            }
        };
        Debug.Log("Thread_OnSpeechEvents shutting down");
        oSockSubscriber.Unsubscribe("");
        oSockSubscriber.Disconnect(SubscriberAddress);
        oSockSubscriber.Close();
        oSockSubscriber = null;
        _bThreadSubscriber_Exited = true;       // Tell main task we're done so it we can re-join and exit cleanly
    }


    public void WandConfig_ObjectAssignment_Begin(CVrWand oVrWand_BeingConfigured) {
        if (_oVrWand_BeingConfigured == null) {         // Only configure one wand at a time.
            _oVrWand_BeingConfigured = oVrWand_BeingConfigured;
            CGame._oSpeechServerProxy.ActivateGrammarGroup('W');        // Load only the wand config grammar
            CGame._aDebugMsgs[(int)EMsg.WandConfig] = $"WandConfig: '{oVrWand_BeingConfigured._sNameWand}'";
        }
    }

    public void WandConfig_ObjectAssignment_End(CVrWand oVrWand_BeingConfigured) {
        if (_oVrWand_BeingConfigured == oVrWand_BeingConfigured) {
            _oVrWand_BeingConfigured = null;
            CGame._oSpeechServerProxy.ActivateGrammarGroup('G');        // No longer configuring this wand. Load back the global grammar
            CGame._aDebugMsgs[(int)EMsg.WandConfig] = "WandConfig: (None)";
        }
    }
}





public class CPhrase
{
    public char         _chGrammarGroupID;
    public string       _sNamePhrase;
    public string       _sNamePacked;
    public List<CWord>  _aWords = new List<CWord>();
    byte                _nWords;

    public CPhrase(char chGrammarGroupID, string sNamePhrase) {
        _chGrammarGroupID = chGrammarGroupID;
        _sNamePhrase = sNamePhrase;
        _sNamePacked = $"{_chGrammarGroupID}_{_sNamePhrase}";
        CSpeechServerProxy.INSTANCE.SendMsgToSpeechServer("CreatePhrase", _sNamePacked);
    }

    public void AddWord(string sNameWord, int bRequired, params string[] aChoicesRaw)
    {
        CWord oWord = new CWord(this, _nWords++, sNameWord, aChoicesRaw);
        _aWords.Add(oWord);
        List<string> aWordChoices_List = new List<string>();
        foreach (CWordChoice oWordChoice in oWord._aWordChoices)
            aWordChoices_List.Add(oWordChoice._sWord);
        string sArgsPacked = string.Join(CSpeechServerProxy.C_Separator_Arguments, aWordChoices_List);
        if (bRequired == 0)
            CSpeechServerProxy.INSTANCE.SendMsgToSpeechServer("AddWordChoicesOptional", sArgsPacked);
        else
            CSpeechServerProxy.INSTANCE.SendMsgToSpeechServer("AddWordChoices", sArgsPacked);
    }
}

public class CWord
{
    public CPhrase _oPhrase;
    public byte _nOrdinal;
    public string _sNameWord;
    public CWordChoice[] _aWordChoices;

    public CWord(CPhrase oPhrase, byte nOrdinal, string sNameWord, params string[] aChoicesRaw)
    {
        _oPhrase = oPhrase;
        _nOrdinal = nOrdinal;
        _sNameWord = sNameWord;
        _aWordChoices = new CWordChoice[aChoicesRaw.Length];
        int nChoice = 0;
        foreach (string sChoice in aChoicesRaw)
            _aWordChoices[nChoice] = new CWordChoice(this, aChoicesRaw[nChoice++]);
    }
}

public class CWordChoice
{
    public CWord _oWord;
    public string _sWord;
    public string _sWordSynonim;

    public CWordChoice(CWord oWord, string sWordRaw)
    {
        _oWord = oWord;
        int nPosEqual = sWordRaw.IndexOf('=');
        if (nPosEqual == -1)
        {
            _sWord = sWordRaw;
        }
        else
        {
            _sWord = sWordRaw.Substring(0, nPosEqual);
            _sWordSynonim = sWordRaw.Substring(nPosEqual + 1);
        }
    }
}

#region JUNK
//public class NetMqPublisher
//{
//    private readonly Thread _listenerWorker;

//    private bool _listenerCancelled;

//    public delegate string MessageDelegate(string message);

//    private readonly MessageDelegate _messageDelegate;

//    private readonly Stopwatch _contactWatch;

//    private const long ContactThreshold = 1000;

//    public bool Connected;

//    private void ListenerWork()
//    {
//        AsyncIO.ForceDotNet.Force();
//        using (var server = new ResponseSocket())
//        {
//            server.Bind("tcp://*:12346");

//            while (!_listenerCancelled)
//            {
//                Connected = _contactWatch.ElapsedMilliseconds < ContactThreshold;
//                string message;
//                if (!server.TryReceiveFrameString(out message)) continue;
//                _contactWatch.Restart();
//                var response = _messageDelegate(message);
//                server.SendFrame(response);
//            }
//        }
//        NetMQConfig.Cleanup();
//    }

//    public NetMqPublisher(MessageDelegate messageDelegate)
//    {
//        _messageDelegate = messageDelegate;
//        _contactWatch = new Stopwatch();
//        _contactWatch.Start();
//        _listenerWorker = new Thread(ListenerWork);
//    }

//    public void Start()
//    {
//        _listenerCancelled = false;
//        _listenerWorker.Start();
//    }

//    public void Stop()
//    {
//        _listenerCancelled = true;
//        _listenerWorker.Join();
//    }
//}

//public class CallNetMQ : MonoBehaviour {
//    public bool Connected;
//    private NetMqPublisher _netMqPublisher;
//    private string _response;

//    private void Start()
//    {
//        _netMqPublisher = new NetMqPublisher(HandleMessage);
//        _netMqPublisher.Start();
//    }

//    private void Update()
//    {
//        var position = transform.position;
//        _response = $"{position.x} {position.y} {position.z}";
//        Connected = _netMqPublisher.Connected;
//    }

//    private string HandleMessage(string message)
//    {
//        // Not on main thread
//        return _response;
//    }

//    private void OnDestroy()
//    {
//        _netMqPublisher.Stop();
//    }
//}




//using (var responseSocket = new ResponseSocket("@tcp://*:5555"))
//using (var requestSocket = new RequestSocket(">tcp://localhost:5555"))
//{
//    Console.WriteLine("requestSocket : Sending 'Hello'");
//    requestSocket.SendFrame("Hello");

//    var message = responseSocket.ReceiveFrameString();

//    Console.WriteLine("responseSocket : Server Received '{0}'", message);

//    Console.WriteLine("responseSocket Sending 'World'");
//    responseSocket.SendFrame("World");

//    message = requestSocket.ReceiveFrameString();
//    Console.WriteLine("requestSocket : Received '{0}'", message);

//    Console.ReadLine();
//}


//public void RecognizeSpeech() {
//    string sTextRecognized;
//    string sNameGrammar;
//    float nConfidence;
//    bool bSuccess = _oSpeechServer.RecognizeSpeech(out sTextRecognized, out sNameGrammar, out nConfidence);
//    Debug.LogFormat("{0}: Grammar='{1}' = '{2}'", nConfidence, sNameGrammar, sTextRecognized);
//}
#endregion