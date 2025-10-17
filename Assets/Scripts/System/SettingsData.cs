using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class KeybindPair
{
    public string action;
    public KeyCode key;
}

[Serializable]
public class SettingsData
{
    public List<KeybindPair> KeybindsList = new List<KeybindPair>();
    public float MusicVolume = 1f;
    public float SFXVolume = 1f;
    public float MasterVolume = 1f;

    public SettingsData()
    {
        AddKey("up", KeyCode.UpArrow);
        AddKey("down", KeyCode.DownArrow);
        AddKey("left", KeyCode.LeftArrow);
        AddKey("right", KeyCode.RightArrow);
        AddKey("jump", KeyCode.Z);
        AddKey("dash", KeyCode.C);
        AddKey("interact", KeyCode.UpArrow);
        AddKey("attack", KeyCode.X);
    }

    public void AddKey(string action, KeyCode key)
    {
        KeybindsList.Add(new KeybindPair { action = action, key = key });
    }

    public Dictionary<string, KeyCode> GetKeybindsDictionary()
    {
        Dictionary<string, KeyCode> dict = new Dictionary<string, KeyCode>();
        foreach (var pair in KeybindsList)
            dict[pair.action] = pair.key;
        return dict;
    }
}
