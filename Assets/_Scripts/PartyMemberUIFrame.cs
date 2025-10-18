using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Realtime;

public class PartyMemberUIFrame : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Slider healthSlider;
    // Adicione aqui slider de mana, etc., se quiser

    private Health _targetHealth;
    private Player _photonPlayer;

    // Função para popular o frame com os dados do jogador
    public void Initialize(Player player)
    {
        _photonPlayer = player;
        playerNameText.text = player.NickName;

        // Encontra o GameObject do jogador na cena para pegar o script de Health
        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (pc.GetComponent<Photon.Pun.PhotonView>().Owner == player)
            {
                _targetHealth = pc.GetComponent<Health>();
                break;
            }
        }
    }

    // Atualiza a barra de vida
    void Update()
    {
        if (_targetHealth != null && healthSlider != null)
        {
            healthSlider.maxValue = _targetHealth.GetMaxHealth();
            healthSlider.value = _targetHealth.GetCurrentHealth();
        }
    }
}