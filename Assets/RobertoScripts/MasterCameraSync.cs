// =========================
// STEP 2: Unity Master PC Script (Handles VR Headset + Sends Camera Pose)
// =========================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;       // For WebRTC
using WS = WebSocketSharp;

public class MasterCameraSync : MonoBehaviour
{
    public SlaveVideoReceiver myreciver;

    public Camera vrCamera;
    public string signalingServerUrl = "ws://localhost:8080";

    private WS.WebSocket ws;

    private string myId;
    private List<string> remoteIds = new List<string>();

    private List<RTCPeerConnection> peerConnections = new List<RTCPeerConnection>();
    private List<RTCDataChannel> dataChannels = new List<RTCDataChannel>();

    private float updateInterval = 0.02f; // 50fps

    void Start()
    {
        ws = new WS.WebSocket(signalingServerUrl);

        ws.OnOpen += (sender, e) =>
        {
            Debug.Log($"? Master connected to signaling server: {signalingServerUrl}");

            // ? Send register message to signaling server
            var registerMsg = new
            {
                type = "register",
                role = "master"
            };
            ws.Send(JsonUtility.ToJson(registerMsg));
            Debug.Log("? Sent register message as master.");
        };


        ws.OnMessage += OnMessage;
        ws.OnError += (sender, e) => {
            Debug.LogError("? Master WebSocket Error: " + e.Message);
        };

        ws.OnClose += (sender, e) => {
            Debug.LogWarning("?? Master WebSocket closed");
        };

        ws.Connect();
        StartCoroutine(BroadcastCameraPose());
    }


    void OnDestroy()
    {
        
        ws.Close();
    }

    void OnMessage(object sender, WS.MessageEventArgs e)
    {
        var msg = JsonUtility.FromJson<SignalingMessage>(e.Data);
        if (msg.type == "id")
        {
            myId = msg.id;
        }
        else if (msg.type == "remote_connected")
        {
            if (!remoteIds.Contains(msg.id))
            {
                remoteIds.Add(msg.id);
                SetupPeerConnection(msg.id);
            }
        }
    }

    void SetupPeerConnection(string remoteId)
    {
        RTCConfiguration config = new RTCConfiguration
        {
            iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } }
        };

        var peer = new RTCPeerConnection(ref config);
        var dc = peer.CreateDataChannel("camSync");

        dc.OnOpen = () => Debug.Log("DataChannel open with " + remoteId);
        dc.OnClose = () => Debug.Log("DataChannel closed with " + remoteId);
        // Add this to your MasterCameraSync.cs where you set up peer connections
        peer.OnTrack = e =>
        {
            Debug.Log("MASTER: Track received of type: " + e.Track.Kind);
            myreciver.OnTrackReceived(e);
            // Find and notify all SlaveVideoReceiver components
            SlaveVideoReceiver[] receivers = FindObjectsOfType<SlaveVideoReceiver>();

            if (receivers.Length > 0)
            {
                Debug.Log("Found " + receivers.Length + " SlaveVideoReceiver components");
                foreach (SlaveVideoReceiver receiver in receivers)
                {
                    receiver.OnTrackReceived(e);
                }
            }
            else
            {
                Debug.LogError("No SlaveVideoReceiver components found in the scene!");
            }
        };
        peer.OnIceCandidate = candidate =>
        {
            SendMessage("ice", remoteId, new IceCandidatePayload
            {
                candidate = candidate.Candidate,
                sdpMid = candidate.SdpMid,
                sdpMLineIndex = candidate.SdpMLineIndex ?? 0

        });
        };

        peerConnections.Add(peer);
        dataChannels.Add(dc);

        StartCoroutine(CreateOffer(peer, remoteId));
    }

    IEnumerator CreateOffer(RTCPeerConnection peer, string remoteId)
    {
        var offer = peer.CreateOffer();
        yield return offer;
        var localDesc = offer.Desc;

        SendMessage("offer", remoteId, new SdpPayload
        {
            sdp = offer.Desc.sdp,
            type = offer.Desc.type.ToString()
        });
    }

    IEnumerator BroadcastCameraPose()
    {
        while (true)
        {
            SendCameraPose();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    void SendCameraPose()
    {
        var camPose = new CameraPose
        {
            pos = vrCamera.transform.position,
            rot = vrCamera.transform.rotation
        };


        string json = JsonUtility.ToJson(camPose);
        Debug.Log("[Master -> Slaves] Sending CameraPose: " + json);
        foreach (var dc in dataChannels)
        {
            if (dc.ReadyState == RTCDataChannelState.Open)
                dc.Send(System.Text.Encoding.UTF8.GetBytes(json));
        }
    }

    void SendMessage(string type, string targetId, object payload)
    {
        var msg = new OutgoingMessage
        {
            type = type,
            target = targetId,
            payload = payload
        };

        string json = JsonUtility.ToJson(msg);
        ws.Send(json);
    }
    [System.Serializable]
    public class SignalingMessage
    {
        public string type;
        public string id;
        public string from;
        public SdpPayload payload;   // SDP payload for offer/answer (default)
    }

    [System.Serializable]
    public class SdpPayload
    {
        public string sdp;           // Offer/answer SDP
        public string type;          // "offer" or "answer"
    }

    [System.Serializable]
    public class IceCandidatePayload
    {
        public string candidate;     // ICE candidate string
        public string sdpMid;        // sdpMid string
        public int sdpMLineIndex;    // index
    }

    [System.Serializable]
    public class CameraPose
    {
        public Vector3 pos;
        public Quaternion rot;
    }

    [System.Serializable]
    public class PartitionData
    {
        public float x;
        public float y;
        public float width;
        public float height;
    }
    [System.Serializable]
    public class OutgoingMessage
    {
        public string type;
        public string target;
        public object payload;
    }

}

