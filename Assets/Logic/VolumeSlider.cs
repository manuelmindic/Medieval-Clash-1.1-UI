using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TextMeshProUGUI sliderText;
    [SerializeField] private AudioSource audioSource;
    /*  public static VolumeSlider instance;

      void Awake()
      {
          if (instance != null)
              Destroy(gameObject);
          else
          {
              instance = this;
              DontDestroyOnLoad(this.gameObject);
          }
      }*/
    private void Start()

    {
        if (!PlayerPrefs.HasKey("musicVolume"))
        {
            PlayerPrefs.SetFloat("musicVolume", 1f);
        }
        Load();

        sliderText.text = (volumeSlider.value * 100).ToString("0");
        audioSource.volume = volumeSlider.value;

        volumeSlider.onValueChanged.AddListener((v) => {
            sliderText.text = (v * 100).ToString("0");
            ChangeVolume();
        });

        //  ChangeVolume();
    }

    public void ChangeVolume()
    {
        audioSource.volume = volumeSlider.value;
        Save();
    }

    private void Load()
    {
        volumeSlider.value = PlayerPrefs.GetFloat("musicVolume");
    }

    private void Save()
    {
        PlayerPrefs.SetFloat("musicVolume", volumeSlider.value);
        PlayerPrefs.Save();
    }
}
