using System;
using System.Text;
using Cysharp.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;
using UnityEngine.Networking;

public class SupabaseRealtime : Singleton<SupabaseRealtime>
{
    WebSocket ws;
    int refId = 1;

    public async UniTask Connect(Action onConnected = null)
    {
        string url =
            $"{SupabaseConfig.Url.Replace("https", "wss")}" +
            "/realtime/v1/websocket" +
            $"?apikey={SupabaseConfig.PublicKey}" +
            "&vsn=1.0.0";

        Debug.Log("[SupabaseRealtime] Connecting to " + url);

        ws = new WebSocket(url);

        ws.OnOpen += () =>
        {
            Debug.Log("[SupabaseRealtime] Connected");
            JoinUserMaps();
            onConnected?.Invoke();
        };

        ws.OnMessage += (bytes) =>
        {
            string json = Encoding.UTF8.GetString(bytes);
            Debug.Log("[SupabaseRealtime] " + json);
        };

        ws.OnError += (e) =>
        {
            Debug.LogError("[SupabaseRealtime] Error: " + e);
        };

        ws.OnClose += (e) =>
        {
            Debug.Log("[SupabaseRealtime] Closed");
        };

        await ws.Connect();
    }

    void JoinUserMaps()
    {
        var msg = new RealtimeJoinMessage
        {
            topic = "realtime:public:user_maps",
            @event = "phx_join",
            payload = new RealtimePayload
            {
                config = new RealtimeConfig
                {
                    broadcast = new BroadcastConfig { self = false },
                    presence = new PresenceConfig(),
                    postgres_changes = new[]
                    {
                    new PostgresChange
                    {
                        @event = "*",
                        schema = "public",
                        table = "user_maps"
                    }
                }
                }
            },
            @ref = refId++
        };

        string json = JsonUtility.ToJson(msg);
        ws.SendText(json);

        Debug.Log("[SupabaseRealtime] Join user_maps message: " + json);
        Debug.Log("[SupabaseRealtime] Join user_maps (C# safe)");
    }


    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        ws?.DispatchMessageQueue();
#endif
    }

    [ContextMenu("Test Fetch Maps")]
    public void TestFetchMaps()
    {
        Test().Forget();
    }
    private async UniTask Test()
    {
        var maps = await FetchMapsAsync();
        Debug.Log("[SupabaseRealtime] Maps: " + maps.Length);

        await CreateMapAsync("[SupabaseRealtime] Map UniTask", "{\"tiles\":[1,2]}");
    }

    [ContextMenu("Test Create Map")]
    public void TestCreateMap()
    {
        CreateMapAsync("[SupabaseRealtime] Map Test", "{\"tiles\":[1,2,3]}").Forget();
    }

    [ContextMenu("Test Update Map")]
    public void TestUpdateMap()
    {
        UpdateMapAsync(
            "4f48ee83-24f8-43bd-be43-ca4dff125b42",
            "{\"tiles\":[3,4,5]}",
            2
        ).Forget();
    }

    /// <summary>
    /// Fetch all maps that are not deleted
    /// </summary>
    /// <returns></returns>
    public async UniTask<UserMap[]> FetchMapsAsync()
    {
        var req = SupabaseRest.Create(
            "/rest/v1/user_maps?select=*&is_delete=eq.false"
        );

        await SupabaseRest.SendAsync(req);

        Debug.Log($"[SupabaseRealtime] ResponseCode = {req.responseCode}");

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(req.error);
            Debug.Log(req.responseCode);
            Debug.Log(req.downloadHandler.text);
            return Array.Empty<UserMap>();
        }

        string json = "{\"items\":" + req.downloadHandler.text + "}";
        var wrapper = JsonUtility.FromJson<UserMapArray>(json);

        return wrapper.items;
    }

    [ContextMenu("Test Fetch My Maps")]
    public void TestFetchMyMaps()
    {
        FetchMyMapsAsync().ContinueWith(maps =>
        {
            Debug.Log("[SupabaseRealtime] My Maps: " + maps.Length);
        }).Forget();
    }

    /// <summary>
    /// Fetch maps created by this device that are not deleted
    /// </summary>
    /// <returns></returns>
    public async UniTask<UserMap[]> FetchMyMapsAsync()
    {
        string deviceId = DeviceId.Value;

        var req = SupabaseRest.Create(
            $"/rest/v1/user_maps" +
            $"?select=*" +
            $"&device_id=eq.{deviceId}" +
            $"&is_delete=eq.false"
        );

        await SupabaseRest.SendAsync(req);

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[FetchMyMaps] " + req.error);
            Debug.Log(req.downloadHandler.text);
            return Array.Empty<UserMap>();
        }

        string json = "{\"items\":" + req.downloadHandler.text + "}";
        var wrapper = JsonUtility.FromJson<UserMapArray>(json);

        return wrapper.items;
    }

    /// <summary>
    /// Create new map
    /// </summary>
    /// <param name="name"></param>
    /// <param name="mapJson"></param>
    /// <returns></returns>
    public async UniTask CreateMapAsync(string name, string mapJson)
    {
        string body =
            "{"
            + $"\"map_name\":\"{name}\","
            + $"\"map_data\":{mapJson},"
            + $"\"device_id\":\"{DeviceId.Value}\""
            + "}";

        Debug.Log("[SupabaseRealtime] CreateMapAsync body: " + body);

        var req = SupabaseRest.Create(
            "/rest/v1/user_maps",
            UnityWebRequest.kHttpVerbPOST,
            body
        );

        await SupabaseRest.SendAsync(req);

        Debug.Log($"[SupabaseRealtime] ResponseCode = {req.responseCode}");

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(req.error);
            Debug.Log(req.responseCode);
            Debug.Log(req.downloadHandler.text);
            return;
        }
    }

    /// <summary>
    /// Update map data and version
    /// </summary>
    /// <param name="mapId"></param>
    /// <param name="mapJson"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    public async UniTask UpdateMapAsync(
    string mapId,
    string mapJson,
    int version
    )
    {
        string body =
            "{"
            + $"\"map_data\":{mapJson},"
            + $"\"version\":{version}"
            + "}";

        var req = SupabaseRest.Create(
            $"/rest/v1/user_maps?id=eq.{mapId}",
            "PATCH",
            body
        );

        req.SetRequestHeader("Prefer", "return=representation");

        Debug.Log("[SupabaseRealtime] UpdateMapAsync body: " + body);

        await SupabaseRest.SendAsync(req);


        Debug.Log($"[SupabaseRealtime] ResponseCode = {req.responseCode}");
        Debug.Log("[SupabaseRealtime] Body: " + req.downloadHandler.text);

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(req.error);
            Debug.Log(req.responseCode);
            Debug.Log(req.downloadHandler.text);
            return;
        }
    }

    /// <summary>
    /// Soft delete map by setting is_delete to true
    /// </summary>
    /// <param name="mapId"></param>
    /// <returns></returns>
    public async UniTask DeleteMapAsync(string mapId)
    {
        var req = SupabaseRest.Create(
            $"/rest/v1/user_maps?id=eq.{mapId}",
            "PATCH",
            "{ \"is_delete\": true }"
        );

        await SupabaseRest.SendAsync(req);

        Debug.Log($"[SupabaseRealtime] ResponseCode = {req.responseCode}");

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(req.error);
            Debug.Log(req.responseCode);
            Debug.Log(req.downloadHandler.text);
            return;
        }
    }

    async void OnDestroy()
    {
        if (ws != null)
            await ws.Close();
    }
}
