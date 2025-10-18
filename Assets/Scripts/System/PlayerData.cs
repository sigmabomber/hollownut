using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerData
{
    public float HP = 100f;
    public bool StickUnlocked = false;
    public bool DashUnlocked = false;
    public int Checkpoint = 1;
    public string SceneName = "SampleScene";
    public List<string> BossesDefeated = new List<string>();

    public PlayerData()
    {
    }

    public void Set<T>(string key, T value)
    {
        switch (key)
        {
            case "HP":
                HP = Convert.ToSingle(value);
                break;
            case "StickUnlocked":
                StickUnlocked = Convert.ToBoolean(value);
                break;
            case "DashUnlocked":
                DashUnlocked = Convert.ToBoolean(value);
                break;
            case "Checkpoint":
                Checkpoint = Convert.ToInt32(value);
                break;
            case "BossesDefeated":
                BossesDefeated = value as List<string>;
                break;
            case "SceneName":
                SceneName = value as string;
                break;
            default:
                Debug.LogWarning($"Unknown key: {key}");
                break;
        }
    }

    public T Get<T>(string key)
    {
        object value = key switch
        {
            "HP" => HP,
            "StickUnlocked" => StickUnlocked,
            "DashUnlocked" => DashUnlocked,
            "Checkpoint" => Checkpoint,
            "BossesDefeated" => BossesDefeated,
            _ => throw new KeyNotFoundException($"Key {key} not found in PlayerData")
        };

        if (typeof(T) == typeof(float) && value is int intVal)
            return (T)(object)(float)intVal;

        return (T)value;
    }
}