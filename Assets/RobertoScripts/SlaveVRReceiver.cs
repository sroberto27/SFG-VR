using PimDeWitte.UnityMainThreadDispatcher;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System;
using UnityEngine;

public class SlaveVRReceiver : MonoBehaviour
{
    public Camera slaveCamera;
    public RenderTexture outputTexture;
    public string masterIP = "10.131.80.244";
    public int masterPort = 55555;
    public bool isLeft = true; //  Set in Inspector: true for left slave, false for right

    private UdpClient udpClient;
    private Thread receiveThread;

    private TcpClient tcpClient;
    private NetworkStream stream;
    private int width = 1024;  // Full render width (split in half)
    private int height = 512;

    void Start()
    {
        // Use value from UI-stored config
        if (NetworkConfig.Instance != null)
        {
            masterIP = NetworkConfig.Instance.masterIP;
        }

        udpClient = new UdpClient(9050);
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

        slaveCamera.targetTexture = outputTexture;

        tcpClient = new TcpClient(masterIP, masterPort);
        stream = tcpClient.GetStream();
    }


    void ReceiveData()
    {
        while (true)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 9050);
            byte[] data = udpClient.Receive(ref ep);
            string message = Encoding.UTF8.GetString(data);
            string[] parts = message.Split(';');

            if (parts.Length == 3)
            {
                string[] pos = parts[0].Split(',');
                string[] rot = parts[1].Split(',');
                string[] scissor = parts[2].Split(',');

                Vector3 position = new Vector3(
                    float.Parse(pos[0]),
                    float.Parse(pos[1]),
                    float.Parse(pos[2]));

                Quaternion rotation = new Quaternion(
                    float.Parse(rot[0]),
                    float.Parse(rot[1]),
                    float.Parse(rot[2]),
                    float.Parse(rot[3]));

                float x = float.Parse(scissor[0]);
                float w = float.Parse(scissor[1]);
                Rect rect = new Rect(x, 0, w, 1);

                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    slaveCamera.transform.SetPositionAndRotation(position, rotation);
                    Scissor.SetScissorRectInplace(slaveCamera, rect);
                });
            }
        }
    }

    void LateUpdate()
    {
        SendFrameToMaster();
    }

    void SendFrameToMaster()
    {
        RenderTexture.active = outputTexture;

        //  Only send the appropriate half
        int halfWidth = width / 2;
        int xOffset = isLeft ? 0 : halfWidth;

        Texture2D tex = new Texture2D(halfWidth, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(xOffset, 0, halfWidth, height), 0, 0);
        tex.Apply();

        byte[] imageBytes = tex.EncodeToJPG();
        byte[] lengthPrefix = BitConverter.GetBytes(imageBytes.Length);

        try
        {
            if (stream != null && stream.CanWrite)
            {
                stream.Write(lengthPrefix, 0, 4);
                stream.Write(imageBytes, 0, imageBytes.Length);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to send frame: " + e.Message);
        }

        Destroy(tex);
    }

    void OnApplicationQuit()
    {
        receiveThread.Abort();
        udpClient.Close();

        if (stream != null) stream.Close();
        if (tcpClient != null) tcpClient.Close();
    }
}
