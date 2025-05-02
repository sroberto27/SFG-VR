using System.Collections;
using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System;
using UnityEngine.XR;
public class MasterVRSender : MonoBehaviour
{
    public Camera vrCamera;
    public float portion = 0.5f; // Portion of screen for left view
    UdpClient udpLeft;
    UdpClient udpRight;
    IPEndPoint epLeft = new IPEndPoint(IPAddress.Parse("10.131.80.198"), 9050); // IP of Slave Left
    IPEndPoint epRight = new IPEndPoint(IPAddress.Parse("10.131.80.176"), 9050); // IP of Slave Right

    void Start()
    {
        XRSettings.gameViewRenderMode = GameViewRenderMode.OcclusionMesh;
        udpLeft = new UdpClient();
        udpRight = new UdpClient();
    }

    void Update()
    {
        if (udpLeft == null || udpRight == null)
        {
            udpLeft = new UdpClient();
            udpRight = new UdpClient();

            epLeft = new IPEndPoint(IPAddress.Parse(NetworkConfig.Instance.leftIP), 9050);
            epRight = new IPEndPoint(IPAddress.Parse(NetworkConfig.Instance.rightIP), 9050);
        }

        SendPOV(udpLeft, epLeft, 0f, portion);
        SendPOV(udpRight, epRight, portion, 1f - portion);
    }


    void SendPOV(UdpClient client, IPEndPoint endpoint, float xStart, float width)
    {
        Vector3 pos = vrCamera.transform.position;
        Quaternion rot = vrCamera.transform.rotation;

        string message = $"{pos.x},{pos.y},{pos.z};{rot.x},{rot.y},{rot.z},{rot.w};{xStart},{width}";
        byte[] data = Encoding.UTF8.GetBytes(message);
        client.Send(data, data.Length, endpoint);
    }
}
