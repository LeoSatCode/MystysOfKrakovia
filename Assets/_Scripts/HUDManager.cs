using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("Componentes do Diário de Missões")]
    public GameObject questLogPanel;

    [Header("Prefabs de HUD das Classes")]
    [SerializeField] private GameObject warriorHudPrefab;
    [SerializeField] private GameObject mageHudPrefab;
    [SerializeField] private GameObject clericHudPrefab;
    [SerializeField] private GameObject archerHudPrefab;

    [Header("Target Frame - Inimigo")]
    [SerializeField] private GameObject enemyTargetFramePanel;
    [SerializeField] private TMP_Text enemyTargetNameText;
    [SerializeField] private Slider enemyTargetHealthSlider;

    [Header("Target Frame - Boss")]
    [SerializeField] private GameObject bossHealthBarPanel;
    [SerializeField] private Slider bossHealthSlider;
    [SerializeField] private TMP_Text bossNameText;

    private Health _bossHealth;

    [Header("Target Frame - Jogador")]
    [SerializeField] private GameObject playerTargetFramePanel;
    [SerializeField] private TMP_Text playerTargetNameText;
    [SerializeField] private Slider playerTargetHealthSlider;
    [SerializeField] private Button inviteButton;

    [Header("Convite de Grupo")]
    [SerializeField] private GameObject invitationPanel;
    [SerializeField] private TMP_Text invitationText;
    [SerializeField] private Button acceptInviteButton;
    [SerializeField] private Button declineInviteButton;

    [Header("Componentes de Morte")]
    [SerializeField] private GameObject respawnButton;

    // --- VARIÁVEIS INTERNAS ---
    private GameObject _currentHudInstance;
    private PlayerController _localPlayerController;
    private Health _localPlayerHealth;
    private Health _targetHealth;
    private Player _targetPhotonPlayer;

    // --- COMPONENTES DO HUD ---
    private Slider _healthSlider;
    private TMP_Text _healthText;
    private Slider _resourceSlider;
    private TMP_Text _resourceText;
    private Slider _xpSlider;
    private TMP_Text _xpText;
    private TMP_Text _levelText;

    void Awake() => Instance = this;

    void Start()
    {
        if (bossHealthBarPanel != null) bossHealthBarPanel.SetActive(false);
        if (enemyTargetFramePanel != null) enemyTargetFramePanel.SetActive(false);
        if (playerTargetFramePanel != null) playerTargetFramePanel.SetActive(false);
        if (invitationPanel != null) invitationPanel.SetActive(false);
        if (respawnButton != null) respawnButton.SetActive(false);
        if (questLogPanel != null) questLogPanel.SetActive(false);
    }

    // --- PLAYER LOCAL ---
    public void AssignPlayer(PlayerController pc, Health health)
    {
        _localPlayerController = pc;
        _localPlayerHealth = health;

        string playerClass = GameManager.SelectedCharacter.characterClass;
        GameObject hudPrefabToInstantiate = null;

        if (playerClass == "Guerreiro") hudPrefabToInstantiate = warriorHudPrefab;
        else if (playerClass == "Mago") hudPrefabToInstantiate = mageHudPrefab;
        else if (playerClass == "Clérigo") hudPrefabToInstantiate = clericHudPrefab;
        else if (playerClass == "Arqueiro") hudPrefabToInstantiate = archerHudPrefab;

        if (hudPrefabToInstantiate != null)
        {
            _currentHudInstance = Instantiate(hudPrefabToInstantiate, transform);

            // --- VIDA ---
            _healthSlider = _currentHudInstance.transform.Find("HealthSlider").GetComponent<Slider>();
            _healthText = _healthSlider.GetComponentInChildren<TMP_Text>();

            // --- RECURSO ---
            _resourceSlider = _currentHudInstance.transform.Find("ResourceSlider").GetComponent<Slider>();
            _resourceText = _resourceSlider.GetComponentInChildren<TMP_Text>();

            // --- XP ---
            var xpObj = _currentHudInstance.transform.Find("XPSlider");
            if (xpObj != null)
            {
                _xpSlider = xpObj.GetComponent<Slider>();
                _xpText = _xpSlider.GetComponentInChildren<TMP_Text>();
            }

            // --- NÍVEL ---
            var levelObj = _currentHudInstance.transform.Find("LevelText");
            if (levelObj != null) _levelText = levelObj.GetComponent<TMP_Text>();
        }
    }

    // --- TARGET FRAMES ---
    public void ShowEnemyTargetFrame(Health target)
    {
        _targetHealth = target;
        _targetPhotonPlayer = null;

        enemyTargetFramePanel.SetActive(true);
        playerTargetFramePanel.SetActive(false);

        enemyTargetNameText.text = target.gameObject.name;
        enemyTargetHealthSlider.maxValue = target.GetMaxHealth();
    }

    public void ShowPlayerTargetFrame(Health target, Player targetPhotonPlayer)
    {
        _targetHealth = target;
        _targetPhotonPlayer = targetPhotonPlayer;

        Debug.Log($"[HUDManager] Alvo definido: '{_targetPhotonPlayer.NickName}'.");

        playerTargetFramePanel.SetActive(true);
        enemyTargetFramePanel.SetActive(false);

        playerTargetNameText.text = targetPhotonPlayer.NickName;
        playerTargetHealthSlider.maxValue = target.GetMaxHealth();

        inviteButton.onClick.RemoveAllListeners();
        inviteButton.onClick.AddListener(OnInviteButtonClicked);

        bool isAlreadyInParty = false;
        if (PartyManager.Instance != null)
            isAlreadyInParty = PartyManager.Instance.IsPlayerInMyParty(targetPhotonPlayer);

        inviteButton.gameObject.SetActive(!isAlreadyInParty);
    }

    public void OnInviteButtonClicked()
    {
        Debug.Log("[HUDManager] Botão de convite clicado.");
        if (_targetPhotonPlayer != null)
        {
            Debug.Log($"[HUDManager] Enviando convite para '{_targetPhotonPlayer.NickName}'.");
            PartyManager.Instance.SendInvite(_targetPhotonPlayer);
            inviteButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("[HUDManager] Nenhum alvo válido para enviar convite!");
        }
    }

    public void HideAllTargetFrames()
    {
        _targetHealth = null;
        _targetPhotonPlayer = null;

        if (enemyTargetFramePanel != null) enemyTargetFramePanel.SetActive(false);
        if (playerTargetFramePanel != null) playerTargetFramePanel.SetActive(false);
    }

    // --- CONVITES ---
    public void ShowInvitationPanel(Player invitingPlayer)
    {
        if (invitationPanel == null || invitationText == null) return;

        invitationPanel.SetActive(true);
        invitationText.text = $"{invitingPlayer.NickName} te convidou para um grupo.";

        acceptInviteButton.onClick.RemoveAllListeners();
        acceptInviteButton.onClick.AddListener(() =>
        {
            PartyManager.Instance.RespondToInvite(invitingPlayer, true);
            invitationPanel.SetActive(false);
        });

        declineInviteButton.onClick.RemoveAllListeners();
        declineInviteButton.onClick.AddListener(() =>
        {
            PartyManager.Instance.RespondToInvite(invitingPlayer, false);
            invitationPanel.SetActive(false);
        });
    }

    // --- RESPAWN ---
    public void ShowRespawnButton()
    {
        if (respawnButton == null) return;

        respawnButton.SetActive(true);
        Button btn = respawnButton.GetComponent<Button>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            if (_localPlayerController != null)
            {
                _localPlayerController.Respawn();
                respawnButton.SetActive(false);
            }
        });
    }

    public void ShowBossHealthBar(Health bossHealth, string bossName)
    {
        _bossHealth = bossHealth;
        if (bossHealthBarPanel != null)
        {
            bossHealthBarPanel.SetActive(true);
            bossNameText.text = bossName;
            bossHealthSlider.maxValue = _bossHealth.GetMaxHealth();
            bossHealthSlider.value = _bossHealth.GetCurrentHealth();
        }
    }

    public void HideBossHealthBar()
    {
        _bossHealth = null;
        if (bossHealthBarPanel != null)
        {
            bossHealthBarPanel.SetActive(false);
        }
    }

    public bool IsThisMyCurrentTarget(Health potentialTarget) => _targetHealth == potentialTarget;

    // --- UPDATE ---
    void Update()
    {
        // --- HUD do Player ---
        if (_localPlayerController != null && _localPlayerHealth != null && !_localPlayerHealth.IsDead())
        {
            string playerClass = GameManager.SelectedCharacter.characterClass;

            // Vida
            if (_healthSlider != null)
            {
                _healthSlider.maxValue = _localPlayerHealth.GetMaxHealth();
                _healthSlider.value = _localPlayerHealth.GetCurrentHealth();
                if (_healthText != null)
                    _healthText.text = $"{_localPlayerHealth.GetCurrentHealth()} / {_localPlayerHealth.GetMaxHealth()}";
            }

            // Recurso
            if (_resourceSlider != null)
            {
                if (playerClass == "Guerreiro")
                {
                    _resourceSlider.maxValue = _localPlayerController.maxRage;
                    _resourceSlider.value = _localPlayerController.GetCurrentRage();
                    if (_resourceText != null)
                        _resourceText.text = $"{(int)_localPlayerController.GetCurrentRage()} / {_localPlayerController.maxRage}";
                }
                else if (playerClass == "Mago" || playerClass == "Clérigo")
                {
                    _resourceSlider.maxValue = _localPlayerController.maxMana;
                    _resourceSlider.value = _localPlayerController.GetCurrentMana();
                    if (_resourceText != null)
                        _resourceText.text = $"{(int)_localPlayerController.GetCurrentMana()} / {_localPlayerController.maxMana}";
                }
                else if (playerClass == "Arqueiro")
                {
                    _resourceSlider.maxValue = _localPlayerController.maxEnergy;
                    _resourceSlider.value = _localPlayerController.GetCurrentEnergy();
                    if (_resourceText != null)
                        _resourceText.text = $"{(int)_localPlayerController.GetCurrentEnergy()} / {_localPlayerController.maxEnergy}";
                }

                if (bossHealthBarPanel != null && bossHealthBarPanel.activeSelf)
                {
                    if (_bossHealth != null && !_bossHealth.IsDead())
                    {
                        bossHealthSlider.value = _bossHealth.GetCurrentHealth();
                    }
                    else
                    {
                        // Se o boss morreu ou a referência foi perdida, esconde a barra.
                        HideBossHealthBar();
                    }
                }
            }

            // XP e Nível
            if (_xpSlider != null)
            {
                _xpSlider.maxValue = _localPlayerController.xpToNextLevel;
                _xpSlider.value = _localPlayerController.currentXP;
                if (_xpText != null)
                    _xpText.text = $"{_localPlayerController.currentXP} / {_localPlayerController.xpToNextLevel}";
            }

            if (_levelText != null)
                _levelText.text = $"Nível {_localPlayerController.currentLevel}";
        }

        // --- HUD do Alvo ---
        if (_targetHealth != null)
        {
            if (enemyTargetFramePanel.activeSelf)
                enemyTargetHealthSlider.value = _targetHealth.GetCurrentHealth();

            if (playerTargetFramePanel.activeSelf)
                playerTargetHealthSlider.value = _targetHealth.GetCurrentHealth();
        }
        else
        {
            if (enemyTargetFramePanel.activeSelf || playerTargetFramePanel.activeSelf)
                HideAllTargetFrames();
        }
    }
}
