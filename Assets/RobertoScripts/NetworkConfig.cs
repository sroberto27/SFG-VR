// Save this as NetworkConfig.cs (make it a prefab or use DontDestroyOnLoad)
using UnityEngine;

public class NetworkConfig : MonoBehaviour
{
    public static NetworkConfig Instance;

    public string masterIP = "10.131.80.244";
    public string leftIP = "10.131.80.198";
    public string rightIP = "10.131.80.176";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
