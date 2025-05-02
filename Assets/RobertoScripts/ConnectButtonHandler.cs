// Save this as ConnectButtonHandler.cs and attach it to your UI Canvas or Button
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ConnectButtonHandler : MonoBehaviour
{
    public TMP_InputField masterField;
    public TMP_InputField leftField;
    public TMP_InputField rightField;

    public void OnConnectPressed()
    {
        if (NetworkConfig.Instance != null)
        {
            NetworkConfig.Instance.masterIP = masterField.text;
            NetworkConfig.Instance.leftIP = leftField.text;
            NetworkConfig.Instance.rightIP = rightField.text;
        }

        // Reload the active scene to apply new IP settings
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
