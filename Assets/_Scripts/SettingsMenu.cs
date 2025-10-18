using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private AudioMixer masterMixer;
    [SerializeField] private Slider masterVolumeSlider;
    // Se quiser controles separados, adicione mais sliders aqui (ex: musicVolumeSlider)

    void Start()
    {
        // No início, carrega o volume salvo (ou usa 1f - máximo - como padrão)
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);

        // Atualiza a posição do slider e o volume do mixer para corresponderem ao valor salvo
        masterVolumeSlider.value = savedVolume;
        SetMasterVolume(savedVolume);
    }

    // Esta função pública será chamada pelo evento "On Value Changed" do Slider
    public void SetMasterVolume(float sliderValue)
    {
        // O mixer usa uma escala logarítmica (decibéis), então precisamos converter.
        // Um valor de slider 0.0001 é mapeado para -80dB (mudo) e 1 para 0dB (máximo).
        float volumeInDb = Mathf.Log10(sliderValue) * 20;

        // Se o slider estiver no mínimo, garantimos que o som fique completamente mudo.
        if (sliderValue <= 0.0001f)
        {
            volumeInDb = -80f;
        }

        // Define o valor no parâmetro "MasterVolume" que expusemos no mixer
        masterMixer.SetFloat("MasterVolume", volumeInDb);

        // Salva a preferência do jogador no disco
        PlayerPrefs.SetFloat("MasterVolume", sliderValue);
    }
}