using UnityEngine;

public class SceneMusicTrigger : MonoBehaviour
{
    [Header("Configuração da Cena")]
    [SerializeField] private AudioClip sceneMusic;
    [SerializeField] private bool loopMusic = true;

    void Start()
    {
        // Ao iniciar a cena, encontra o AudioManager e manda ele tocar a música desta cena
        if (AudioManager.Instance != null && sceneMusic != null)
        {
            AudioManager.Instance.PlayMusic(sceneMusic, loopMusic);
        }
    }
}