using UnityEngine;
using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Recursos de Classe")]
    public int maxRage = 100;
    public int maxMana = 150;
    public int maxEnergy = 100;
    private float _currentRage;
    private float _currentMana;
    private float _currentEnergy;

    [Header("Regeneração de Recursos")]
    [SerializeField] private float rageDecayRate = 5f;
    [SerializeField] private float manaRegenRate = 2f;
    [SerializeField] private float energyRegenRate = 2f;

    [Header("Componentes Específicos de Classe")]
    [SerializeField] private MeleeHitbox meleeHitbox;
    [SerializeField] private Transform projectileSpawnPoint;

    [Header("Componentes")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Animator animator;

    [Header("Stats de Movimento")]
    [SerializeField] private float speed = 5.0f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Stats da Câmera")]
    [SerializeField] private float mouseSensitivity = 350.0f;
    [SerializeField] private float cameraXClamp = 80.0f;

    [Header("Sistema de Alvo e Combate")]
    [Range(0.001f, 0.1f)] // Adiciona um slider no Inspector para facilitar
    [SerializeField] private float attackSpeedPerDexPoint = 0.01f;
    [SerializeField] private float tabTargetingRange = 20f;
    [SerializeField] public float attackRange = 3f;
    [SerializeField] private float swingTime = 1.5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask playerLayer;

    [Header("Sistema de Interação")]
    [SerializeField] private float interactRange = 5f;
    [SerializeField] private LayerMask npcLayer;
    [SerializeField] private LayerMask objectInteractableLayer;

    [Header("Nível e Experiência")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;

    [Header("Atributos")]
    public int constitution = 5;
    public int strength = 5;
    public int intelligence = 5;
    public int dexterity = 5;
    public int attributePoints = 0;

    public static event Action OnStatsChanged;
    public static event Action OnActionBarChanged;

    [Header("Habilidades e Action Bar")]
    public List<Skill> unlockedSkills = new List<Skill>();
    public Skill[] actionBarSlots = new Skill[5];
    public Skill defaultPotionSkill;

    [Header("Feedback Sonoro")]
    [SerializeField] private AudioClip footstepSound;

    private AudioSource audioSource;

    private Dictionary<int, float> _skillLastUsedTime = new Dictionary<int, float>();

    private GameObject _questLogPanel;
    private Transform _currentTarget;
    private Health _currentTargetHealth;
    private Health _myHealth;
    private Transform _currentFriendlyTarget;

    private bool _isAutoAttacking = false;
    private float _nextAttackTime = 0f;
    private int _comboCounter = 0;

    private Skill _currentlyCastingSkill;
    private bool _isCasting = false;

    private PhotonView _photonView;
    private Vector3 _playerVelocity;
    private float _xRotation = 0f;
    private Vector2 _moveInput;
    private bool _jumpInput;

    void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _myHealth = GetComponent<Health>();
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (!_photonView.IsMine)
        {
            playerCamera.gameObject.SetActive(false);
            enabled = false;
            return;
        }

        GameManager.LocalPlayerCamera = playerCamera;

        HUDManager hudManager = FindFirstObjectByType<HUDManager>();
        if (hudManager != null)
            _questLogPanel = hudManager.questLogPanel;

        string myClass = GameManager.SelectedCharacter.characterClass;
        if (myClass == "Guerreiro") _currentRage = 0;
        if (myClass == "Mago" || myClass == "Clérigo") _currentMana = maxMana;
        if (myClass == "Arqueiro") _currentEnergy = maxEnergy;

        if (HUDManager.Instance != null)
            HUDManager.Instance.AssignPlayer(this, GetComponent<Health>());

        //RecalculateSecondaryStats();
    }

    void Update()
    {
        if (!_photonView.IsMine) return;
        if (_myHealth != null && _myHealth.IsDead()) return;

        if (UIManager.Instance != null && UIManager.Instance.IsMenuOpen())
        {
            return; // Para a execução do Update aqui
        }

        if (!_isCasting)
        {
            HandleCameraRotation();
        }

        HandleInputs();
        UpdateAnimation();
        HandleResourceGeneration();

        if (_isAutoAttacking)
            AutoAttackLoop();
    }

    void FixedUpdate()
    {
        if (!_photonView.IsMine) return;
        if (_myHealth != null && _myHealth.IsDead()) return;

        if (_isCasting) return;

        HandleMovement();
    }

    public void PlayFootstepSound()
    {
        // Garante que o som não seja nulo e que o AudioSource exista
        if (audioSource != null && footstepSound != null)
        {
            // Toca o som de passo uma vez
            audioSource.PlayOneShot(footstepSound);
        }
    }

    void HandleInputs()
    {
        // --- MOVIMENTO ---
        if (!_isCasting)
        {
            _moveInput.x = Input.GetAxis("Horizontal");
            _moveInput.y = Input.GetAxis("Vertical");
            if (Input.GetButtonDown("Jump")) _jumpInput = true;
        }
        else
        {
            _moveInput = Vector2.zero;
        }

        // --- INTERAÇÕES DE TECLADO ---
        if (Input.GetKeyDown(KeyCode.Tab)) FindNearestEnemy();
        if (Input.GetButtonDown("Interact")) HandleInteraction(); // Interagir com NPCs (tecla 'E')
        if (Input.GetKeyDown(KeyCode.C)) { if (CharacterPanelUI.Instance != null) CharacterPanelUI.Instance.TogglePanel(); }
        if (Input.GetKeyDown(KeyCode.I)) { if (InventoryUI.Instance != null) InventoryUI.Instance.TogglePanel(); }
        if (Input.GetKeyDown(KeyCode.L) && _questLogPanel != null) _questLogPanel.SetActive(!_questLogPanel.activeSelf);
        if (Input.GetKeyDown(KeyCode.K)) { if (SkillBookUI.Instance != null) SkillBookUI.Instance.TogglePanel(); }

        // --- AÇÕES DA BARRA DE HABILIDADES ---
        if (Input.GetKeyDown(KeyCode.Alpha1)) UseActionBarSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) UseActionBarSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) UseActionBarSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) UseActionBarSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) UseActionBarSlot(4);

        // Cancela auto-attack se o jogador se mover
        if (_moveInput.magnitude > 0.01f && _isAutoAttacking)
            CancelAutoAttack();

        // --- INTERAÇÕES DE MOUSE ---

        // CLIQUE ESQUERDO: Apenas para atacar e selecionar alvos
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return; // Ignora cliques na UI
            HandleTargetingAndAttack();
        }
    }

    private bool HasEnoughResource(Skill skill)
    {
        switch (skill.resourceType)
        {
            case ResourceType.Mana: return _currentMana >= skill.resourceCost;
            case ResourceType.Rage: return _currentRage >= skill.resourceCost;
            case ResourceType.Energy: return _currentEnergy >= skill.resourceCost;
            default: return true;
        }
    }

    private void SpendResource(Skill skill)
    {
        switch (skill.resourceType)
        {
            case ResourceType.Mana: _currentMana -= skill.resourceCost; break;
            case ResourceType.Rage: _currentRage -= skill.resourceCost; break;
            case ResourceType.Energy: _currentEnergy -= skill.resourceCost; break;
        }
    }

    void UseActionBarSlot(int slotIndex)
    {
        if (_isCasting) return;

        Skill skillToUse = actionBarSlots[slotIndex];
        if (skillToUse == null) return;

        if (_skillLastUsedTime.ContainsKey(skillToUse.id) && Time.time < _skillLastUsedTime[skillToUse.id] + skillToUse.cooldown)
        {
            Debug.Log($"Habilidade '{skillToUse.skillName}' em cooldown.");
            return;
        }

        if (!HasEnoughResource(skillToUse))
        {
            Debug.Log($"{skillToUse.resourceType} insuficiente!");
            return;
        }

        _currentlyCastingSkill = skillToUse;

        SpendResource(skillToUse);

        // --- MUDANÇA PRINCIPAL AQUI ---
        // Agora nós verificamos se a ativação foi bem-sucedida
        bool success = skillToUse.Activate(this);

        // O cooldown e a trava de animação SÓ acontecem se a skill teve sucesso
        if (success)
        {
            _skillLastUsedTime[skillToUse.id] = Time.time;
            StartCoroutine(CastingCoroutine(1f));
        }
        else
        {
            // Se falhou, limpamos a skill que "estava sendo usada"
            _currentlyCastingSkill = null;
        }
    }

    private IEnumerator CastingCoroutine(float duration)
    {
        _isCasting = true;
        if (_isAutoAttacking)
            CancelAutoAttack();

        yield return new WaitForSeconds(duration);

        _isCasting = false;
    }
    public void PlaySkillAnimation(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
        {
            animator.SetTrigger(triggerName);
        }
    }
    public void AnimationEvent_SkillAction()
    {
        // Se o jogador estava de fato usando uma skill...
        if (_currentlyCastingSkill != null)
        {
            // ...pede para a skill executar seu efeito real (ex: disparar o projétil).
            _currentlyCastingSkill.PerformAction(this);

            // "Esquece" a skill para a próxima. Limpa depois de usar.
            _currentlyCastingSkill = null;
        }
    }

    public void LookAtTargetHorizontally(Transform target)
    {
        if (target == null) return;

        // Esta é a lógica correta que já estava no seu AutoAttackLoop
        Vector3 lookPosition = new Vector3(target.position.x, transform.position.y, target.position.z);
        transform.LookAt(lookPosition);
    }

    public bool GetSkillCooldownData(int skillId, out float lastUsedTime)
    {
        return _skillLastUsedTime.TryGetValue(skillId, out lastUsedTime);
    }

    public void UnlockSkill(Skill skillToUnlock)
    {
        if (skillToUnlock != null && !unlockedSkills.Contains(skillToUnlock))
        {
            unlockedSkills.Add(skillToUnlock);
            Debug.Log($"HABILIDADE APRENDIDA: {skillToUnlock.skillName}!");
        }
    }

    void HandleMovement()
    {
        bool isGrounded = controller.isGrounded;
        if (isGrounded && _playerVelocity.y < 0) _playerVelocity.y = -2f;

        Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        controller.Move(move.normalized * speed * Time.fixedDeltaTime);

        if (_jumpInput && isGrounded)
            _playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        _jumpInput = false;
        _playerVelocity.y += gravity * Time.fixedDeltaTime;
        controller.Move(_playerVelocity * Time.fixedDeltaTime);
    }

    void HandleCameraRotation()
    {
        if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -cameraXClamp, cameraXClamp);
            playerCamera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void UpdateAnimation()
    {
        animator.SetBool("IsAttacking", _isAutoAttacking);
        animator.SetFloat("VelocityX", _moveInput.x);
        animator.SetFloat("VelocityZ", _moveInput.y);
    }

    void HandleTargetingAndAttack()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit enemyHit, 100f, enemyLayer))
        {
            // --- LÓGICA ANTI-SPAM DE CLIQUE ---
            // Verifica se já estamos atacando este mesmo alvo. Se sim, não faz nada.
            bool isAlreadyAttackingThisTarget = _isAutoAttacking && _currentTarget == enemyHit.transform;
            if (isAlreadyAttackingThisTarget)
            {
                return; // Ignora o clique e deixa o combo continuar
            }
            _currentFriendlyTarget = null;
            SetTarget(enemyHit.transform);

            if (_currentTarget != null && _currentTargetHealth != null && !_currentTargetHealth.IsDead() &&
                Vector3.Distance(transform.position, _currentTarget.position) <= attackRange)
            {
                _isAutoAttacking = true;
                _nextAttackTime = Time.time;
                _comboCounter = 0;
                animator.SetBool("IsAttacking", true);
            }
        }
        else if (Physics.Raycast(ray, out RaycastHit playerHit, 100f, playerLayer))
        {
            CancelAutoAttack();
            SetTarget(null);

            _currentFriendlyTarget = playerHit.transform;
            Health friendlyHealth = _currentFriendlyTarget.GetComponent<Health>();
            PhotonView friendlyView = _currentFriendlyTarget.GetComponent<PhotonView>();

            if (friendlyView != null && !friendlyView.IsMine && friendlyHealth != null)
                HUDManager.Instance?.ShowPlayerTargetFrame(friendlyHealth, friendlyView.Owner);
        }
        else
        {
            SetTarget(null);
            CancelAutoAttack();
            _currentFriendlyTarget = null;
            HUDManager.Instance?.HideAllTargetFrames();
        }
    }

    void AutoAttackLoop()
    {
        if (_currentTarget == null || (_currentTargetHealth != null && _currentTargetHealth.IsDead()))
        {
            CancelAutoAttack();
            SetTarget(null);
            return;
        }

        if (Vector3.Distance(transform.position, _currentTarget.position) > attackRange)
        {
            CancelAutoAttack();
            return;
        }

        if (Time.time >= _nextAttackTime)
        {
            LookAtTargetHorizontally(_currentTarget);

            _comboCounter++;
            if (_comboCounter > 3) _comboCounter = 1;

            if (GameManager.SelectedCharacter.characterClass == "Guerreiro")
                _currentRage = Mathf.Min(maxRage, _currentRage + 10);

            _photonView.RPC("PerformAutoAttackFX", RpcTarget.All, _comboCounter);
            _nextAttackTime = Time.time + swingTime;
        }
    }

    void CancelAutoAttack()
    {
        _isAutoAttacking = false;
        _comboCounter = 0;
        animator.SetBool("IsAttacking", false);
        animator.ResetTrigger("Attack1");
        animator.ResetTrigger("Attack2");
        animator.ResetTrigger("Attack3");
    }

    void FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;
            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null && enemyHealth.IsDead()) continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance && distance <= tabTargetingRange)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }

        SetTarget(closestEnemy);
    }

    void SetTarget(Transform newTarget)
    {
        if (_currentTargetHealth != null) _currentTargetHealth.Deselect();
        _currentTarget = newTarget;

        if (_currentTarget != null)
        {
            _currentTargetHealth = _currentTarget.GetComponent<Health>();
            if (_currentTargetHealth != null && !_currentTargetHealth.IsDead())
            {
                _currentTargetHealth.Select();
                HUDManager.Instance?.ShowEnemyTargetFrame(_currentTargetHealth);
            }
        }
        else
        {
            _currentTargetHealth = null;
            if (!_currentFriendlyTarget)
                HUDManager.Instance?.HideAllTargetFrames();
        }
    }

    [PunRPC]
    void PerformAutoAttackFX(int comboStage)
    {
        if (animator != null) animator.SetTrigger("Attack" + comboStage);
    }

    void HandleInteraction()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        LayerMask combinedMask = npcLayer | objectInteractableLayer;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, combinedMask))
        {
            if (Vector3.Distance(transform.position, hit.transform.position) <= interactRange)
            {
                WorldItem worldItem = hit.collider.GetComponent<WorldItem>();
                if (worldItem != null)
                {
                    worldItem.TryCollect();
                    return; // Interagiu com o loot, a função para por aqui.
                }

                NPCController npc = hit.collider.GetComponent<NPCController>();
                if (npc != null)
                {
                    npc.Interact();
                    return;
                }

                QuestObject questObj = hit.collider.GetComponent<QuestObject>();
                if (questObj != null)
                {
                    questObj.OnInteract();
                    return;
                }
            }
        }
        else
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.EndDialogue();
            }
        }
    }

    public void AddXP(int amount)
    {
        // Apenas o dono do personagem deve processar o ganho de XP.
        if (_photonView.IsMine)
        {
            currentXP += amount;
            Debug.Log($"Ganhou {amount} de XP! Progresso: {currentXP} / {xpToNextLevel}");

            // A verificação de level up acontece aqui, no cliente que ganhou a XP.
            // O 'while' é ótimo caso ganhe XP para múltiplos níveis de uma vez.
            while (currentXP >= xpToNextLevel)
            {
                LevelUp();
            }

            // Dispara o evento para atualizar a UI de XP localmente.
            OnStatsChanged?.Invoke();
        }
    }

    private void LevelUp()
    {
        currentLevel++;
        currentXP -= xpToNextLevel;
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.5f);
        attributePoints += 5;
        RecalculateSecondaryStats();

        Debug.Log($"LEVEL UP! Nível {currentLevel}! Pontos: {attributePoints}");

        _photonView.RPC("RPC_LevelUpFX", RpcTarget.All);

        OnStatsChanged?.Invoke();
    }

    [PunRPC]
    private void RPC_LevelUpFX()
    {
        // Esta função roda em TODAS as máquinas.
        // Ela é responsável apenas por criar os efeitos visuais para que todos vejam.

        // Instancia o VFX na posição atual do jogador
        PhotonNetwork.Instantiate("LevelUp_VFX", transform.position, transform.rotation);

        // Instancia o texto flutuante um pouco acima da cabeça do jogador
        Vector3 textPosition = transform.position + Vector3.up * 2.2f;
        PhotonNetwork.Instantiate("FloatingTextCanvas", textPosition, Quaternion.identity);
    }

    public void SpendAttributePoint(string attribute)
    {
        if (attributePoints <= 0) return;
        attributePoints--;

        switch (attribute.ToLower())
        {
            case "constitution": constitution++; break;
            case "strength": strength++; break;
            case "intelligence": intelligence++; break;
            case "dexterity": dexterity++; break;
            default: attributePoints++; break;
        }

        RecalculateSecondaryStats();
        OnStatsChanged?.Invoke();
    }

    void RecalculateSecondaryStats()
    {
        int newMaxHealth = 50 + (constitution * 10) + (currentLevel * 5);
        if (_myHealth != null)
            _myHealth.UpdateMaxHealth(newMaxHealth, true);

        maxMana = 80 + (intelligence * 10);
        maxRage = 100;
        maxEnergy = 100;

        animator.speed = 1.0f + (dexterity * attackSpeedPerDexPoint);
    }

    void HandleResourceGeneration()
    {
        string myClass = GameManager.SelectedCharacter.characterClass;

        if (myClass == "Guerreiro")
        {
            if (!_isAutoAttacking)
                _currentRage = Mathf.MoveTowards(_currentRage, 0, rageDecayRate * Time.deltaTime);
        }
        else if (myClass == "Mago" || myClass == "Clérigo")
        {
            _currentMana = Mathf.MoveTowards(_currentMana, maxMana, manaRegenRate * Time.deltaTime);
        }
        else if (myClass == "Arqueiro")
        {
            _currentEnergy = Mathf.MoveTowards(_currentEnergy, maxEnergy, energyRegenRate * Time.deltaTime);
        }
    }

    public void AnimationEvent_FireProjectile()
    {
        if (!_photonView.IsMine) return;
        if (projectileSpawnPoint != null)
        {
            GameObject projGO = PhotonNetwork.Instantiate("FireballPrefab", projectileSpawnPoint.position, projectileSpawnPoint.rotation);
            projGO.GetComponent<Projectile>()?.Initialize(this);
        }
    }

    public void AnimationEvent_Arrow()
    {
        if (!_photonView.IsMine) return;
        if (projectileSpawnPoint != null)
        {
            GameObject projGO = PhotonNetwork.Instantiate("ArrowPrefab", projectileSpawnPoint.position, projectileSpawnPoint.rotation);
            projGO.GetComponent<Projectile>()?.Initialize(this);
        }
    }

    public void SetSkillInActionBar(int slotIndex, Skill skill)
    {
        if (slotIndex < 0 || slotIndex >= actionBarSlots.Length) return;
        actionBarSlots[slotIndex] = skill;
        Debug.Log($"Skill '{skill.skillName}' adicionada ao slot {slotIndex + 1}");
        OnActionBarChanged?.Invoke();
    }

    public Transform GetCurrentTarget() => _currentTarget;
    public Transform GetCurrentFriendlyTarget() => _currentFriendlyTarget;
    public Transform GetProjectileSpawnPoint() => projectileSpawnPoint;

    public void RefundResource(Skill skill)
    {
        Debug.Log("Habilidade falhou, recurso devolvido.");
        switch (skill.resourceType)
        {
            case ResourceType.Mana: _currentMana += skill.resourceCost; break;
            case ResourceType.Rage: _currentRage += skill.resourceCost; break;
            case ResourceType.Energy: _currentEnergy += skill.resourceCost; break;
        }
    }

    public void AnimationEvent_EnableMeleeHitbox() => meleeHitbox?.EnableHitbox();
    public void AnimationEvent_DisableMeleeHitbox() => meleeHitbox?.DisableHitbox();

    // As funções abaixo agora estão vazias, pois a lógica foi movida para as classes de Skill

    public void AnimationEvent_IceLance() { }
    public void AnimationEvent_TripleShot() { }

    [PunRPC]
    void RPC_PerformSkill(int skillID)
    {
        if (animator != null)
        {
            if (skillID == 1) animator.SetTrigger("Skill1");
        }
    }

    [PunRPC]
    private void RPC_SpawnHolyEffect(int targetViewID)
    {
        PhotonView targetView = PhotonView.Find(targetViewID);
        if (targetView != null)
        {
            // O prefab "HolyLight_VFX" precisa estar em uma pasta "Resources"
            PhotonNetwork.Instantiate("HolyLight_VFX", targetView.transform.position, Quaternion.identity);
        }
    }

    public void Respawn()
    {
        RoomManager roomManager = FindFirstObjectByType<RoomManager>();
        if (roomManager == null || roomManager.spawnPoint == null)
        {
            Debug.LogError("RoomManager ou SpawnPoint não encontrado!");
            return;
        }
        Transform spawnPoint = roomManager.spawnPoint;
        _photonView.RPC("RPC_Respawn", RpcTarget.All, spawnPoint.position, spawnPoint.rotation);
    }

    [PunRPC]
    void RPC_Respawn(Vector3 position, Quaternion rotation)
    {
        controller.enabled = false;
        transform.position = position;
        transform.rotation = rotation;
        controller.enabled = true;
        Physics.SyncTransforms();
        if (_photonView.IsMine) enabled = true;
        _myHealth?.Revive();
        animator?.Rebind();
        animator?.Update(0f);
        Debug.Log(gameObject.name + " renasceu!");
    }

    // Adicione estas duas funções ao seu PlayerController.cs
    public void UpdateSaveData(CharacterData saveData)
    {
        saveData.level = currentLevel;
        saveData.currentXP = currentXP;
        saveData.attributePoints = attributePoints;
        saveData.constitution = constitution;
        saveData.strength = strength;
        saveData.intelligence = intelligence;
        saveData.dexterity = dexterity;

        saveData.unlockedSkillNames.Clear();
        foreach (var skill in unlockedSkills)
        {
            saveData.unlockedSkillNames.Add(skill.name);
        }
        for (int i = 0; i < actionBarSlots.Length; i++)
        {
            if (actionBarSlots[i] != null)
                saveData.actionBarSkillNames[i] = actionBarSlots[i].name;
            else
                saveData.actionBarSkillNames[i] = null;
        }
    }

    public void LoadFromSaveData(CharacterData saveData)
    {
        currentLevel = saveData.level;
        currentXP = saveData.currentXP;
        attributePoints = saveData.attributePoints;
        constitution = saveData.constitution;
        strength = saveData.strength;
        intelligence = saveData.intelligence;
        dexterity = saveData.dexterity;

        foreach (var skillName in saveData.unlockedSkillNames)
        {
            Skill skill = Resources.Load<Skill>("_Skills/" + skillName);

            // Condição dupla: só adiciona se a skill foi encontrada E se ela já não está na lista
            if (skill != null && !unlockedSkills.Contains(skill))
            {
                unlockedSkills.Add(skill);
            }
        }
        for (int i = 0; i < saveData.actionBarSkillNames.Length; i++)
        {
            if (!string.IsNullOrEmpty(saveData.actionBarSkillNames[i]))
            {
                Skill skill = Resources.Load<Skill>("_Skills/" + saveData.actionBarSkillNames[i]);
                actionBarSlots[i] = skill;
            }
            else
            {
                actionBarSlots[i] = null;
            }
        }

        OnActionBarChanged?.Invoke();
        OnStatsChanged?.Invoke();
        RecalculateSecondaryStats();
    }

    public float GetCurrentRage() => _currentRage;
    public float GetCurrentMana() => _currentMana;
    public float GetCurrentEnergy() => _currentEnergy;
}