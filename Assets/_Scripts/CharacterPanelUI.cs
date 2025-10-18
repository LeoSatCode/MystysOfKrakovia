using UnityEngine;
using TMPro;

public class CharacterPanelUI : MonoBehaviour
{
    public static CharacterPanelUI Instance { get; private set; }

    [Header("Referências")]
    [SerializeField] private TMP_Text constitutionText;
    [SerializeField] private TMP_Text strengthText;
    [SerializeField] private TMP_Text intelligenceText;
    [SerializeField] private TMP_Text dexterityText;
    [SerializeField] private TMP_Text attributePointsText;
    [SerializeField] private GameObject characterPanel; // O próprio painel

    private PlayerController localPlayerController;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        characterPanel.SetActive(false); // Começa fechado
    }

    // --- Gerenciamento de Eventos ---
    private void OnEnable()
    {
        PlayerController.OnStatsChanged += UpdateUI;
    }
    private void OnDisable()
    {
        PlayerController.OnStatsChanged -= UpdateUI;
    }
    // --------------------------------

    void UpdateUI()
    {
        if (localPlayerController == null)
        {
            // Encontra o jogador local na primeira atualização
            foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (pc.GetComponent<Photon.Pun.PhotonView>().IsMine)
                {
                    localPlayerController = pc;
                    break;
                }
            }
        }

        if (localPlayerController == null) return; // Se ainda não encontrou, sai

        // Atualiza todos os textos com os valores do PlayerController
        constitutionText.text = $"Constituição: {localPlayerController.constitution}";
        strengthText.text = $"Força: {localPlayerController.strength}";
        intelligenceText.text = $"Inteligência: {localPlayerController.intelligence}";
        dexterityText.text = $"Destreza: {localPlayerController.dexterity}";
        attributePointsText.text = $"Pontos Disponíveis: {localPlayerController.attributePoints}";
    }

    // --- Funções para os Botões ---
    public void OnAddConstitution() { localPlayerController?.SpendAttributePoint("constitution"); }
    public void OnAddStrength() { localPlayerController?.SpendAttributePoint("strength"); }
    public void OnAddIntelligence() { localPlayerController?.SpendAttributePoint("intelligence"); }
    public void OnAddDexterity() { localPlayerController?.SpendAttributePoint("dexterity"); }
    // ---------------------------------

    public void TogglePanel()
    {
        characterPanel.SetActive(!characterPanel.activeSelf);
        if (characterPanel.activeSelf)
        {
            UpdateUI(); // Garante que a UI está atualizada ao abrir
        }
    }
}