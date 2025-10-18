using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class Health : MonoBehaviour, IPunObservable
{
    [Header("Configuração de Vida")]
    public int maxHealth = 100; // MUDANÇA: Tornei público para o PlayerController recalcular

    [Header("Recompensa")]
    [SerializeField] private int xpValue = 15;
    [SerializeField] private LootTable lootTable;
    [SerializeField] private ItemData guaranteedQuestDrop;

    [Header("Feedback Visual")]
    [SerializeField] private GameObject selector;
    [SerializeField] private Slider overheadHealthBar;
    [SerializeField] private TMP_Text overheadHealthText;
    [SerializeField] private GameObject healVFXPrefab;

    [Header("Feedback Sonoro")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;

    private AudioSource audioSource;
    private int _currentHealth;
    private PhotonView _photonView;
    private bool _isDead = false;
    private Player _lastAttacker;

    void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        _currentHealth = maxHealth;
        if (overheadHealthBar != null)
        {
            overheadHealthBar.maxValue = maxHealth;
            overheadHealthBar.value = maxHealth;
            Canvas canvas = overheadHealthBar.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.worldCamera == null)
                canvas.worldCamera = Camera.main;
            overheadHealthBar.gameObject.SetActive(false);
            if (overheadHealthText != null)
                overheadHealthText.text = $"{_currentHealth} / {maxHealth}";
        }
    }

    public void UpdateMaxHealth(int newMaxHealth, bool healNewHealth)
    {
        // Apenas o dono do personagem (você) pode iniciar a mudança de vida máxima.
        if (_photonView.IsMine)
        {
            _photonView.RPC("RPC_UpdateMaxHealth", RpcTarget.All, newMaxHealth, healNewHealth);
        }
    }

    [PunRPC]
    private void RPC_UpdateMaxHealth(int newMaxHealth, bool healNewHealth)
    {
        // Esta é a nova função que roda em TODAS as máquinas, garantindo que todos saibam da nova vida máxima.
        int healthDifference = newMaxHealth - maxHealth;
        maxHealth = newMaxHealth;

        // Se a opção de curar estiver ativa e a vida aumentou, cura a diferença.
        if (healNewHealth && healthDifference > 0)
        {
            _currentHealth += healthDifference;
        }
        
        // Garante que a vida atual não ultrapasse a máxima.
        if (_currentHealth > maxHealth)
        {
            _currentHealth = maxHealth;
        }

        // Atualiza a UI (isto vai rodar em todas as máquinas).
        if (overheadHealthBar != null)
        {
            overheadHealthBar.maxValue = maxHealth;
            overheadHealthBar.value = _currentHealth; // Importante atualizar o valor atual também.
        }
        if (overheadHealthText != null)
        {
            overheadHealthText.text = $"{_currentHealth} / {maxHealth}";
        }
    }

    public void TakeDamage(int damage)
    {
        _photonView.RPC("RPC_TakeDamage", RpcTarget.All, damage);
    }

    [PunRPC]
    public void RPC_TakeDamage(int damage, PhotonMessageInfo info)
    {
        if (_isDead) return;
        
        _lastAttacker = info.Sender;
        
        bool canTakeDamage = (gameObject.CompareTag("Enemy") && PhotonNetwork.IsMasterClient) ||
                             (gameObject.CompareTag("Player") && _photonView.IsMine);

        if (!canTakeDamage) return;

        // --- NOVA LÓGICA DE AGGRO ---
        // Se este objeto for um inimigo, avisa a IA sobre quem o atacou.
        if (gameObject.CompareTag("Enemy"))
        {
            GetComponent<EnemyAI>()?.OnAttackedBy(info.Sender);

            GetComponent<Boss_GuardianAI>()?.OnAttackedBy(info.Sender);
        }
        // --- FIM DA NOVA LÓGICA ---

        _currentHealth -= damage;
        if (overheadHealthBar != null)
            overheadHealthBar.value = _currentHealth;
        if (overheadHealthText != null)
            overheadHealthText.text = $"{_currentHealth} / {maxHealth}";

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            _photonView.RPC("RPC_Die", RpcTarget.All);
        }
        else
        {
            _photonView.RPC("RPC_PlayHurtAnimation", RpcTarget.All);
        }
    }
    
    [PunRPC]
    public void RPC_PlayHurtAnimation()
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null && !_isDead)
            animator.SetTrigger("Hurt");

        if (audioSource != null && hurtSound != null)
            audioSource.PlayOneShot(hurtSound);
        
        if (gameObject.CompareTag("Enemy"))
        {
            GetComponent<EnemyAI>()?.StunForDuration(1f);
        }
    }

    [PunRPC]
    public void RPC_Die()
    {
        if (_isDead) return;
        _isDead = true;

        Debug.Log($"{gameObject.name} foi derrotado!");

        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.SetTrigger("Die");

        SetOverheadBarActive(false);
        Deselect();

        if (HUDManager.Instance != null && HUDManager.Instance.IsThisMyCurrentTarget(this))
            HUDManager.Instance.HideAllTargetFrames();

        // --- A GRANDE MUDANÇA ESTÁ AQUI ---
        // Em vez de Health tentar desligar os componentes da IA...
        // ...ele agora apenas avisa a todos os outros scripts no mesmo objeto que a morte aconteceu.
        // Cada script de IA será responsável por se desligar.
        BroadcastMessage("OnDeath", SendMessageOptions.DontRequireReceiver);
        
        // A lógica de dar XP e Loot continua aqui, pois ela é comum a todos os inimigos.
        if (gameObject.CompareTag("Enemy"))
        {
            if (audioSource != null && deathSound != null)
                audioSource.PlayOneShot(deathSound);

            Vector3 dropPosition = transform.position + Vector3.up * 0.5f;

            if (PhotonNetwork.IsMasterClient)
            {
                if (guaranteedQuestDrop != null)
                {
                    object[] data = { guaranteedQuestDrop.name };
                    PhotonNetwork.InstantiateRoomObject("WorldItem_Prefab", dropPosition, Quaternion.identity, 0, data);
                }
                if (lootTable != null)
                {
                    lootTable.CalculateAndDropLoot(dropPosition);
                }
            }
            
            if (_lastAttacker != null)
            {
                foreach (PlayerController pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                {
                    if (pc.GetComponent<PhotonView>().Owner == _lastAttacker)
                    {
                        pc.AddXP(xpValue);
                        QuestManager.Instance?.AddQuestProgress_Kill(gameObject.name, 1);
                        break;
                    }
                }
            }

            if (PhotonNetwork.IsMasterClient)
                StartCoroutine(DestroyAfterDelay(10f));
        }
        else if (gameObject.CompareTag("Player"))
        {
            // Lógica de morte do jogador continua a mesma
            if (audioSource != null && deathSound != null)
                audioSource.PlayOneShot(deathSound);
            
            PlayerController pc = GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;

            if (HUDManager.Instance != null && _photonView.IsMine)
                HUDManager.Instance.ShowRespawnButton();
        }
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (this != null && this.gameObject != null)
            PhotonNetwork.Destroy(gameObject);
    }

    [PunRPC]
    public void RPC_ApplyHeal(int amount)
    {
        if (_isDead) return;

        _currentHealth += amount;
        if (_currentHealth > maxHealth)
            _currentHealth = maxHealth;

        if (healVFXPrefab != null)
        {
            PhotonNetwork.Instantiate(healVFXPrefab.name, transform.position, Quaternion.identity);
        }

        if (overheadHealthBar != null)
            overheadHealthBar.value = _currentHealth;
        if (overheadHealthText != null)
            overheadHealthText.text = $"{_currentHealth} / {maxHealth}";
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            if (PhotonNetwork.IsMasterClient || _photonView.IsMine)
                stream.SendNext(_currentHealth);
        }
        else
        {
            _currentHealth = (int)stream.ReceiveNext();
            if (overheadHealthBar != null)
                overheadHealthBar.value = _currentHealth;
            if (overheadHealthText != null)
                overheadHealthText.text = $"{_currentHealth} / {maxHealth}";
        }
    }

    public void SetOverheadBarActive(bool isActive)
    {
        if (overheadHealthBar != null)
            overheadHealthBar.gameObject.SetActive(isActive);
    }

    public void Select()
    {
        if (selector != null)
            selector.SetActive(true);
    }

    public void Deselect()
    {
        if (selector != null)
            selector.SetActive(false);
    }

    public int GetCurrentHealth() => _currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsDead() => _isDead;

    public void Revive()
    {
        _isDead = false;
        _currentHealth = maxHealth;
        if (overheadHealthBar != null)
            overheadHealthBar.value = _currentHealth;
        if (overheadHealthText != null)
            overheadHealthText.text = $"{_currentHealth} / {maxHealth}";
    }
}