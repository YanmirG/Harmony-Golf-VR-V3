using System;
using Convai.Scripts.Runtime.Addons;
using UnityEngine;

public class ConvaiAPIKeySetup : ScriptableObject
{
    private const string RESOURCE_PATH = "ConvaiAPIKey";
    private static ConvaiAPIKeySetup _instance;
    public string APIKey;

    public static event Action OnAPIKeyNotFound;

    public static bool GetAPIKey(out string apiKey)
    {
        if (_instance == null) _instance = Resources.Load<ConvaiAPIKeySetup>(RESOURCE_PATH);

        if (_instance == null || string.IsNullOrEmpty(_instance.APIKey))
        {
            NotifyAPIKeyNotFound();
            apiKey = string.Empty;
            return false;
        }

        apiKey = _instance.APIKey;
        return true;
    }

    private static void NotifyAPIKeyNotFound()
    {
        if (NotificationSystemHandler.Instance != null) NotificationSystemHandler.Instance.NotificationRequest(NotificationType.APIKeyNotFound);
        OnAPIKeyNotFound?.Invoke();
    }
}