using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    private void Start()
    {
        ApplySavedVolume();
    }

    private void ApplySavedVolume()
    {
        if (PlayerPrefs.HasKey("musicVolume"))
        {
            float savedVolume = PlayerPrefs.GetFloat("musicVolume");
            audioSource.volume = savedVolume;
        }
        else
        {
            audioSource.volume = 1f;
        }
    }
}