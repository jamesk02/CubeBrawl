using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [SerializeField]
    private AudioSource duelAmbient;

    [SerializeField]
    private AudioSource menuTheme;

    [SerializeField]
    private AudioSource authTheme;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.activeSceneChanged += OnSceneChanged;

        instance = this;
        DontDestroyOnLoad(this); // the sound manager should exist for all scenes so we can get and set data throughout
    }

    private void OnSceneChanged(Scene current, Scene next)
    {
        // Main menu theme
        if (next.buildIndex == 0)
        {
            authTheme.loop = true;
            authTheme.PlayDelayed(0.1f);
        }
        else if (next.buildIndex == 1)
        {
            authTheme.Stop();
            duelAmbient.Stop();

            menuTheme.loop = true;
            menuTheme.PlayDelayed(0.3f);
        }
        // In game duel ambient music
        else if (next.buildIndex == 2)
        {
            menuTheme.Stop();

            duelAmbient.loop = true;
            duelAmbient.PlayDelayed(3);
        }
    }

    // ensures the sky box continues to rotate
    private void Update()
    {
        RenderSettings.skybox.SetFloat("_Rotation", Time.time);
    }

}
