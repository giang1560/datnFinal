public static class SupabaseConfig
{
    public const string Url =
        "https://eplfikhgdtowucwspzwh.supabase.co";

    public const string PublicKey =
        "sb_publishable_WrERbPOFEc2hMaJPYzOMUA_4XhVJNb4";
}

[System.Serializable]
public class RealtimeJoinMessage
{
    public string topic;
    public string @event;
    public RealtimePayload payload;
    public int @ref;
}

[System.Serializable]
public class RealtimePayload
{
    public RealtimeConfig config;
}

[System.Serializable]
public class RealtimeConfig
{
    public BroadcastConfig broadcast;
    public PresenceConfig presence;
    public PostgresChange[] postgres_changes;
}

[System.Serializable]
public class BroadcastConfig
{
    public bool self;
}

[System.Serializable]
public class PresenceConfig
{
}

[System.Serializable]
public class PostgresChange
{
    public string @event;
    public string schema;
    public string table;
}

[System.Serializable]
public class RealtimeMessage
{
    public string @event;
    public RealtimePayloadMS payload;
}


[System.Serializable]
public class RealtimePayloadMS
{
    public string eventType;
    public UserMap @new;
    public UserMap old;
}

[System.Serializable]
public class UserMap
{
    public string id;
    public string map_name;
    public string map_data;
    public int version;
    public string device_id;
    public bool is_delete;
}

[System.Serializable]
class UserMapArray
{
    public UserMap[] items;
}