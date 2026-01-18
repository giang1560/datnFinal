using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class SupabaseRest
{
    public static UnityWebRequest Create(
        string path,
        string method = UnityWebRequest.kHttpVerbGET,
        string bodyJson = null
    )
    {
        var req = new UnityWebRequest(
            SupabaseConfig.Url + path,
            method
        );

        req.downloadHandler = new DownloadHandlerBuffer();

        if (!string.IsNullOrEmpty(bodyJson))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.SetRequestHeader("Content-Type", "application/json");
        }

        // 🔑 REQUIRED HEADERS
        req.SetRequestHeader("apikey", SupabaseConfig.PublicKey);
        req.SetRequestHeader("Authorization", "Bearer " + SupabaseConfig.PublicKey);
        req.SetRequestHeader("device-id", DeviceId.Value);

        return req;
    }

    public static async UniTask<UnityWebRequest> SendAsync(
        UnityWebRequest req
    )
    {
        await req.SendWebRequest().ToUniTask();
        return req;
    }
}

public static class DeviceId
{
    public static readonly string Value =
        SystemInfo.deviceUniqueIdentifier;
}
