// MASTER: Unity C# script running on Meta Quest, receives images from slaves and displays stitched view
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System;
using PimDeWitte.UnityMainThreadDispatcher;

public class MasterVRReceiver : MonoBehaviour
{
    public Renderer displaySurface; // Material on a curved surface
    public int width = 512;
    public int height = 512;

    private TcpListener listener;
    private Thread serverThread;
    private Texture2D compositeTexture;
    private Texture2D leftTexture;
    private Texture2D rightTexture;
    private Texture2D tempTexture;
    private void Awake()
    {

        leftTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        rightTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        compositeTexture = new Texture2D(width*2, height, TextureFormat.RGB24, false);
    }

    void Start()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, 55555);
            listener.Start();
            serverThread = new Thread(AcceptClients);
            serverThread.IsBackground = true;
            serverThread.Start();
        }
        catch (SocketException ex)
        {
            Debug.LogError("Port already in use or failed to bind: " + ex.Message);
        }
    }

    void AcceptClients()
    {
        while (true)
        {
            try
            {
                TcpClient client = listener.AcceptTcpClient();
                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.IsBackground = true;
                clientThread.Start();
            }
            catch (Exception e)
            {
                Debug.LogError("Client accept failed: " + e.Message);
            }
        }
    }

    void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        BinaryReader reader = new BinaryReader(stream);

        while (true)
        {
            try
            {
                int length = reader.ReadInt32();
                byte[] imageData = reader.ReadBytes(length);

                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (tempTexture == null)
                        tempTexture = new Texture2D(width, height, TextureFormat.RGB24, false);

                    if (tempTexture.LoadImage(imageData))
                    {
                        string remoteIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                        string leftIP = NetworkConfig.Instance.leftIP;
                        string rightIP = NetworkConfig.Instance.rightIP;

                        if (remoteIP == leftIP)
                            leftTexture.SetPixels32(tempTexture.GetPixels32());
                        else if (remoteIP == rightIP)
                            rightTexture.SetPixels32(tempTexture.GetPixels32());
                        else
                            Debug.LogWarning("Unrecognized sender IP: " + remoteIP);

                        UpdateBlendedTexture();
                    }
                    else
                    {
                        Debug.LogWarning("Failed to decode received image");
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogError("Client dropped: " + e.Message);
                return;
            }
        }
    }



    void UpdateBlendedTexture()
    {
        if (leftTexture == null || rightTexture == null) return;

        // Make sure compositeTexture is correctly sized
        if (compositeTexture.width != width * 2 || compositeTexture.height != height)
        {
            compositeTexture = new Texture2D(width * 2, height, TextureFormat.RGB24, false);
        }

        // Read pixels from each texture
        Color[] leftPixels = leftTexture.GetPixels();
        Color[] rightPixels = rightTexture.GetPixels();

        // Create combined array for composite texture
        Color[] combined = new Color[width * 2 * height];

        for (int y = 0; y < height; y++)
        {
            int rowStartLeft = y * width;
            int rowStartCombined = y * width * 2;

            // Copy left row
            Array.Copy(leftPixels, rowStartLeft, combined, rowStartCombined, width);

            // Copy right row
            Array.Copy(rightPixels, rowStartLeft, combined, rowStartCombined + width, width);
        }

        compositeTexture.SetPixels(combined);
        compositeTexture.Apply();
        displaySurface.material.mainTexture = compositeTexture;
    }



    void OnApplicationQuit()
    {
        serverThread?.Abort();
        listener?.Stop();
    }
}