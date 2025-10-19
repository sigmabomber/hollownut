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
        CurrentSettings = SettingsManager.Load();
        ApplySettings();

        CurrentPlayer = await PlayerSaveSystem.LoadAsync(playerSavePassword);

        if (CurrentPlayer == null)
        {
            CurrentPlayer = new PlayerData();
        }
        else
        {
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