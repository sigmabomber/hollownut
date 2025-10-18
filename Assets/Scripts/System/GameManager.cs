using UnityEngine;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    public SettingsData CurrentSettings;
    public PlayerData CurrentPlayer;
    private string playerSavePassword = "Hollow Nutsack";
    public static GameManager Instance;
    async void Awake()
    {
        // Load settings
        CurrentSettings = SettingsManager.Load();
        ApplySettings();

        // Load player data (WAITS until complete)
        CurrentPlayer = await PlayerSaveSystem.LoadAsync(playerSavePassword);

        if (CurrentPlayer == null)
        {
            Debug.Log("Creating new player data");
            CurrentPlayer = new PlayerData();
        }
        else
        {
            Debug.Log("Loaded existing player data");
        }
    }

    private void Start()
    {


        Instance = this;
    }

    void ApplySettings()
    {
        AudioListener.volume = CurrentSettings.MasterVolume;
    }

    public async Task SavePlayer()
    {
        await PlayerSaveSystem.SaveAsync(CurrentPlayer, playerSavePassword);
    }

    public void SaveSettings()
    {
        SettingsManager.Save(CurrentSettings);
    }

     void OnApplicationQuit()
    {
        SaveSettings();
    }
}