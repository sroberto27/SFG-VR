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
    public int width = 1024;
    public int height = 1024;

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
        compositeTexture = new Texture2D(width * 2, height, TextureFormat.RGB24, false);
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
                        if (client.Client.RemoteEndPoint.ToString().Contains("10.131.80.176"))
                            leftTexture.SetPixels32(tempTexture.GetPixels32());
                        else
                            rightTexture.SetPixels32(tempTexture.GetPixels32());

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
        //  Avoid creating a new texture
        //  Just reuse compositeTexture
        Color[] left = leftTexture.GetPixels();
        Color[] right = rightTexture.GetPixels();

        //  We reuse a shared buffer to avoid allocations
        Color[] combined = compositeTexture.GetPixels(); // reuse internal buffer

        Array.Copy(left, 0, combined, 0, left.Length);
        Array.Copy(right, 0, combined, left.Length, right.Length);

        compositeTexture.SetPixels(combined);
        compositeTexture.Apply();

        displaySurface.material.mainTexture = compositeTexture; //  one shared GPU upload
    }

    void OnApplicationQuit()
    {
        serverThread?.Abort();
        listener?.Stop();
    }
}


/*// MASTER: Unity C# script running on Meta Quest, receives images from slaves and displays stitched view
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
    public int width = 1024;
    public int height = 1024;

    private TcpListener listener;
    private Thread serverThread;

    private Texture2D leftTexture;
    private Texture2D rightTexture;

    private byte[] leftBuffer;
    private byte[] rightBuffer;

    void Start()
    {
        listener = new TcpListener(IPAddress.Any, 55555);
        listener.Start();
        serverThread = new Thread(new ThreadStart(AcceptClients));
        serverThread.IsBackground = true;
        serverThread.Start();

        leftTexture = new Texture2D(width, height);
        rightTexture = new Texture2D(width, height);
    }

    void AcceptClients()
    {
        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Thread clientThread = new Thread(() => HandleClient(client));
            clientThread.IsBackground = true;
            clientThread.Start();
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

                Texture2D tex = new Texture2D(width, height);
                tex.LoadImage(imageData);

                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (client.Client.RemoteEndPoint.ToString().Contains("10.131.80.176"))
                        leftTexture.SetPixels32(tex.GetPixels32());
                    else
                        rightTexture.SetPixels32(tex.GetPixels32());

                    UpdateBlendedTexture();
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
        Texture2D composite = new Texture2D(width * 2, height);
        Color[] left = leftTexture.GetPixels();
        Color[] right = rightTexture.GetPixels();
        Color[] combined = new Color[left.Length + right.Length];

        Array.Copy(left, 0, combined, 0, left.Length);
        Array.Copy(right, 0, combined, left.Length, right.Length);

        composite.SetPixels(combined);
        composite.Apply();

        displaySurface.material.mainTexture = composite;
    }

    void OnApplicationQuit()
    {
        serverThread.Abort();
        listener.Stop();
    }
}
*/