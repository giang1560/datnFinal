using UnityEngine;
using Michsky.UI.Heat;
using Cysharp.Threading.Tasks;
using System;

public class LoadingEntryPoint : MonoBehaviour
{
    [Header("Loading scene")]
    [SerializeField] private ProgressBar myProgressBar;

    [Header("Settings")]
    [SerializeField] private int loadingDelayMs = 500;
    [SerializeField] private bool forceConnectSupabase = false;
    [SerializeField] private float supabaseTimeoutSeconds = 5f;
    async void Start()
    {
        myProgressBar.SetValue(0f);

        // Load level configurations
        await LevelRemoteConfig.Instance.LoadLevelAsync();

        //delay to show progress bar
        myProgressBar.SetValue(50f);
        await UniTask.Delay(loadingDelayMs);

        bool isConnectedToSupabase = false;
        float timer = 0f;

        // Connect to Supabase Realtime
        SupabaseRealtime.Instance.Connect(() =>
        {
            isConnectedToSupabase = true;
        }).Forget();

        // Wait until connected or timeout
        while (!isConnectedToSupabase)
        {
            timer += Time.deltaTime;
            if (timer >= supabaseTimeoutSeconds)
            {
                Debug.LogWarning("Connection to Supabase Realtime timed out.");
                if (forceConnectSupabase)
                {
                    Debug.LogError("Forced to connect to Supabase Realtime. Retrying...");
                    timer = 0f;
                }
                else
                {
                    Debug.LogWarning("Continuing without Supabase Realtime connection.");
                    break;
                }
                await UniTask.Yield();
            }

            //delay to show progress bar
            myProgressBar.SetValue(100f);
            await UniTask.Delay(loadingDelayMs);

            // Load Main Menu scene
            SceneController.Instance.LoadSceneAsync(SceneType.MainMenu);
        }
    }
}
