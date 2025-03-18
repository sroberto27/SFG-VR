using System.Collections;
using UnityEngine;
using Unity.WebRTC;
using WS = WebSocketSharp;


public class RenderNodeReceiver : MonoBehaviour
{
    public Camera scissorCamera;
    public string signalingServerUrl = "ws://MASTER_IP:8080";

    private WS.WebSocket ws;
    private RTCPeerConnection peer;
    private RTCDataChannel dataChannel;

    void Start()
    {
        ws = new WS.WebSocket(signalingServerUrl);

        ws.OnOpen += (sender, e) =>
        {
            Debug.Log($"? Slave connected to signaling server: {signalingServerUrl}");

            // ? Send register message to signaling server
            var registerMsg = new
            {
                type = "register",
                role = "slave"
            };
            ws.Send(JsonUtility.ToJson(registerMsg));
            Debug.Log("? Sent register message as slave.");
        };


        ws.OnMessage += OnMessage;
        ws.OnError += (sender, e) => {
            Debug.LogError("? Slave WebSocket Error: " + e.Message);
        };

        ws.OnClose += (sender, e) => {
            Debug.LogWarning("?? Slave WebSocket closed");
        };

        ws.Connect();
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
            Debug.Log("Received own ID.");
        }
        else if (msg.type == "offer")
        {
            HandleOffer(msg);
        }
        else if (msg.type == "ice")
        {
            HandleIce(msg);
        }
    }

    void HandleOffer(SignalingMessage msg)
    {
        RTCConfiguration config = new RTCConfiguration
        {
            iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } }
        };

        peer = new RTCPeerConnection(ref config);

        peer.OnIceCandidate = candidate =>
        {
            SendMessage("ice", msg.from, new IceCandidatePayload
            {
                candidate = candidate.Candidate,
                sdpMid = candidate.SdpMid,
                sdpMLineIndex = candidate.SdpMLineIndex ?? 0
            });
        };

        peer.OnDataChannel = channel =>
        {
            dataChannel = channel;
            dataChannel.OnMessage = bytes => OnCameraPoseReceived(bytes);
        };

        StartCoroutine(OnReceivedOffer(msg));
    }

    IEnumerator OnReceivedOffer(SignalingMessage msg)
    {
        RTCSessionDescription desc = new RTCSessionDescription
        {
            type = RTCSdpType.Offer,
            sdp = msg.payload.sdp
        };

        var op = peer.SetRemoteDescription(ref desc);
        yield return op;

        var answer = peer.CreateAnswer();
        yield return answer;

        var localDesc = answer.Desc;
        peer.SetLocalDescription(ref localDesc);

        SendMessage("answer", msg.from, new SdpPayload
        {
            sdp = localDesc.sdp,
            type = localDesc.type.ToString()
        });

    }

    void HandleIce(SignalingMessage msg)
    {
        IceCandidatePayload icePayload = JsonUtility.FromJson<IceCandidatePayload>(JsonUtility.ToJson(msg.payload));

        RTCIceCandidateInit iceInit = new RTCIceCandidateInit
        {
            candidate = icePayload.candidate,
            sdpMid = icePayload.sdpMid,
            sdpMLineIndex = icePayload.sdpMLineIndex
        };

        RTCIceCandidate iceCandidate = new RTCIceCandidate(iceInit);
        peer.AddIceCandidate(iceCandidate);
    }


    void OnCameraPoseReceived(byte[] data)
    {
        string json = System.Text.Encoding.UTF8.GetString(data);

        if (json.Contains("pos"))
        {
            CameraPose camPose = JsonUtility.FromJson<CameraPose>(json);
            scissorCamera.transform.SetPositionAndRotation(camPose.pos, camPose.rot);
        }
        else if (json.Contains("width"))
        {
            PartitionData partition = JsonUtility.FromJson<PartitionData>(json);
            Rect partitionRect = new Rect(partition.x, partition.y, partition.width, partition.height);
            Scissor.SetScissorRectInplace(scissorCamera, partitionRect);
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
