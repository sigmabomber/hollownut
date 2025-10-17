using System.IO;
using UnityEngine;

public static class SettingsManager
{
    private static string path = Path.Combine(Application.persistentDataPath, "settings.json");

    public static void Save(SettingsData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }

    public static SettingsData Load()
    {
        if (!File.Exists(path))
            return new SettingsData(); 

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<SettingsData>(json);
    }
}
