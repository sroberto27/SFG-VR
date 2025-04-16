using Unity.WebRTC;
using UnityEngine.UI;
using UnityEngine;

public class SlaveVideoReceiver : MonoBehaviour
{
    public RawImage rawImage; // Assign UI RawImage component

    void Start()
    {
        Debug.Log("SlaveVideoReceiver started. RawImage assigned: " + (rawImage != null));
    }
    // Add to SlaveVideoReceiver.cs for testing
    public void TestRawImage()
    {
        Texture2D testTexture = new Texture2D(512, 512);
        Color[] colors = new Color[512 * 512];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.red;
        }
        testTexture.SetPixels(colors);
        testTexture.Apply();

        rawImage.texture = testTexture;
        rawImage.color = Color.white;
        Debug.Log("Test texture applied to RawImage");
    }

    // Call this from Start() to test if the RawImage can display textures at all
    public void OnTrackReceived(RTCTrackEvent e)
    {
        Debug.Log("OnTrackReceived called! Track type: " + e.Track.Kind);

        if (e.Track is VideoStreamTrack videoTrack)
        {
            Debug.Log("Video track received successfully!");

            // Connect the track directly to the texture
            videoTrack.OnVideoReceived += texture =>
            {
                Debug.Log("check [SlaveVideoReceiver] Video frame received!");
                Debug.Log("Video frame received! Texture size: " + texture.width + "x" + texture.height);
                if (rawImage != null)
                {
                    rawImage.texture = texture;
                    rawImage.color = Color.white; // Ensure the RawImage is visible
                    Debug.Log("Texture assigned to RawImage");
                }
                else
                {
                    Debug.LogError("RawImage is null when trying to assign texture!");
                }
            };
        }
        else
        {
            Debug.LogWarning("Received track is not a VideoStreamTrack!");
        }
    }
}