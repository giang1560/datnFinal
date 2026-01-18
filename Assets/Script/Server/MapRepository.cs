using System;
using System.Collections.Generic;

public class MapRepository
{
    public readonly Dictionary<string, UserMap> Maps =
        new Dictionary<string, UserMap>();

    public event Action<UserMap> OnMapAdded;
    public event Action<UserMap> OnMapUpdated;
    public event Action<string> OnMapRemoved;

    public void Apply(UserMap map)
    {
        if (map.is_delete)
        {
            if (Maps.Remove(map.id))
                OnMapRemoved?.Invoke(map.id);
            return;
        }

        if (Maps.ContainsKey(map.id))
        {
            Maps[map.id] = map;
            OnMapUpdated?.Invoke(map);
        }
        else
        {
            Maps.Add(map.id, map);
            OnMapAdded?.Invoke(map);
        }
    }
}
