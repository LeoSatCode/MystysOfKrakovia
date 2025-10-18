using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Garante que herda de MonoBehaviourPunCallbacks para OnPlayerLeftRoom
public class Boss_GuardianAI : MonoBehaviourPunCallbacks
{
    private enum BossState { Idle, Chasing, Casting, AttackingMelee }
    private BossState _currentState;

    [Header("Referências")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    // Remova a referência [SerializeField] private BossMeleeHitbox meleeHitbox; se estiver usando o Relay
    [SerializeField] private BossRoomBarrier barrierController; // Referência à barreira
    [SerializeField] private BossRoomTrigger entryTrigger;      // Referência ao trigger (opcional, para reset)
    private Health healthScript;
    private Vector3 _startPosition;
    private Quaternion _startRotation;

    [Header("IA de Combate")]
    [SerializeField] private string playerTag = "Player";
    // A variável 'decisionCooldown' foi removida

    [Header("Ataque Corpo-a-Corpo")]
    [SerializeField] private float meleeAttackRange = 3f;
    [SerializeField] private float meleeAttackCooldown = 3f;
    [SerializeField] private float meleeAnimationDuration = 1.2f; // AJUSTE ESTE VALOR!
    [SerializeField] private float chaseSpeed = 4f;

    [Header("Ataque Básico (Sombra Perfurante)")]
    [SerializeField] private GameObject shadowBoltPrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float shadowBoltCooldown = 10f;

    [Header("Ataque em Área (Poça)")]
    [SerializeField] private GameObject decayPoolPrefab;
    [SerializeField] private float decayPoolCooldown = 20f;

    [Header("Invocação (Ajudantes)")]
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private List<Transform> minionSpawnPoints;
    [SerializeField] private float summonCooldown = 35f;


    private Transform _targetPlayer;
    // A variável '_timeSinceLastDecision' foi removida
    private float _lastMeleeAttackTime, _lastShadowBoltTime, _lastDecayPoolTime, _lastSummonTime;
    private bool _isEngaged = false; // Controla se a luta começou

    // --- RASTREAMENTO DE JOGADORES PARA WIPE ---
    private List<Health> _playersInRoom = new List<Health>();
    private Coroutine _wipeCheckCoroutine;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        healthScript = GetComponent<Health>();
        _startPosition = transform.position;
        _startRotation = transform.rotation;
    }

    void Start()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            this.enabled = false;
        }
        _currentState = BossState.Idle; // Começa parado
        agent.isStopped = true;
    }

    // --- FUNÇÃO CHAMADA PELO MASTER QUANDO A LUTA DEVE COMEÇAR (NO PRIMEIRO ATAQUE) ---
    public void StartTrackingPlayersInRoom()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log("[BossAI] >>> StartTrackingPlayersInRoom() CHAMADO <<<");
        _playersInRoom.Clear();

        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        Debug.Log($"[BossAI] Encontrados {allPlayers.Length} PlayerControllers na cena.");

        foreach (PlayerController pc in allPlayers)
        {
            Health playerHealth = pc.GetComponent<Health>();
            // Idealmente verificar a distância/sala aqui
            if (playerHealth != null) // Adiciona mesmo se morto, para detectar wipe corretamente
            {
                _playersInRoom.Add(playerHealth);
                Debug.Log($"[BossAI] Jogador '{pc.GetComponent<PhotonView>().Owner.NickName}' adicionado à lista (Vivo: {!playerHealth.IsDead()}).");
            }
            else { Debug.LogWarning($"[BossAI] PlayerController '{pc.gameObject.name}' não tem componente Health."); }
        }

        if (_playersInRoom.Count > 0)
        {
            if (_wipeCheckCoroutine != null) StopCoroutine(_wipeCheckCoroutine);
            Debug.Log("[BossAI] Iniciando WipeCheckRoutine...");
            _wipeCheckCoroutine = StartCoroutine(WipeCheckRoutine());
        }
        else
        {
            Debug.LogError("[BossAI] StartTracking foi chamado, mas NENHUM jogador foi encontrado para rastrear! Wipe check não iniciado.");
        }
        Debug.Log("[BossAI] Fim da execução de StartTrackingPlayersInRoom().");
    }

    // --- ROTINA QUE RODA SÓ NO MASTER CLIENT PARA VERIFICAR WIPE ---
    private IEnumerator WipeCheckRoutine()
    {
        Debug.Log($"[BossAI] >>> WipeCheckRoutine INICIADA. Rastreando {_playersInRoom.Count} jogadores.");
        yield return new WaitForSeconds(3f); // Espera inicial curta

        while (_isEngaged && _playersInRoom.Count > 0)
        {
            _playersInRoom.RemoveAll(h => h == null); // Limpa referências nulas (jogadores desconectados)
            if (_playersInRoom.Count == 0)
            {
                Debug.Log("[BossAI] Nenhum jogador restante na lista. Parando Wipe Check.");
                break;
            }

            // Verifica se TODOS os jogadores restantes estão mortos
            bool allDead = _playersInRoom.All(playerHealth => playerHealth.IsDead());
            Debug.Log($"[BossAI] Verificando wipe... Jogadores na lista: {_playersInRoom.Count}. Todos mortos? {allDead}");

            if (allDead)
            {
                Debug.Log("[BossAI] WIPE DETECTADO! Resetando o boss e a sala.");
                // Chama o reset para todos via RPC
                photonView.RPC("RPC_ResetEncounter", RpcTarget.All);
                yield break; // Termina a corrotina AQUI
            }

            yield return new WaitForSeconds(2f); // Verifica a cada 2 segundos
        }

        Debug.Log($"[BossAI] WipeCheckRoutine terminada. Motivo: _isEngaged={_isEngaged}, _playersInRoom.Count={_playersInRoom.Count}");
        _wipeCheckCoroutine = null; // Limpa a referência da corrotina
    }


    void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (healthScript != null && healthScript.IsDead())
        {
            if (agent.enabled && !agent.isStopped) agent.isStopped = true;
            return;
        }

        if (!_isEngaged) return; // Se a luta não começou, não faz nada

        // Lógica da IA baseada no estado
        switch (_currentState)
        {
            case BossState.Idle:
                 Debug.Log("[BossAI] Update: Estado Idle, mas Engajado. Mudando para Chasing.");
                 _currentState = BossState.Chasing;
                 agent.isStopped = false; // Permite movimento
                 break;
            case BossState.Chasing:
                UpdateChasingState();
                break;
            case BossState.Casting:
                if (agent.enabled && !agent.isStopped) agent.isStopped = true; // Garante que fica parado
                break;
            case BossState.AttackingMelee:
                if (agent.enabled && !agent.isStopped) agent.isStopped = true; // Garante que fica parado
                break;
        }

        // Atualiza animação de velocidade
         if (agent.enabled)
         {
            if ((_currentState == BossState.Chasing || _currentState == BossState.Idle) && !agent.isStopped) // Idle é backup
                animator.SetFloat("Speed", agent.velocity.magnitude);
            else
                animator.SetFloat("Speed", 0f);
         }
    }

    // --- [MODIFICAÇÃO 2] LÓGICA DE UPDATECHASINGSTATE SUBSTITUÍDA ---
    private void UpdateChasingState()
    {
        FindTarget();
        if (_targetPlayer == null)
        {
            // Se perdeu o alvo (todos morreram ou saíram?) para e espera wipe check
            Debug.Log("[BossAI] UpdateChasing: Sem alvo. Parando e esperando Wipe Check.");
            agent.isStopped = true;
            // Não muda para Idle aqui, deixa o WipeCheck decidir o reset
            return;
        }

        // --- MUDANÇA PRINCIPAL ---
        // 1. Prioridade: Tentar usar uma habilidade especial.
        // Chamamos a nossa nova função de prioridade.
        if (TryPrioritizeSpecialAction())
        {
            // Se usou uma skill, o estado mudou para Casting.
            // A IA vai parar de perseguir e só voltará ao Chasing quando o cast terminar.
            return; // Sai da lógica de Chase/Melee deste frame.
        }

        // 2. Se nenhuma especial foi usada (estão em cooldown), avalia o ataque corpo-a-corpo
        float distance = Vector3.Distance(transform.position, _targetPlayer.position);
        bool canAttackMelee = Time.time >= _lastMeleeAttackTime + meleeAttackCooldown;

        if (canAttackMelee && distance <= meleeAttackRange)
        {
            EnterMeleeAttackState();
        }
        else if (distance > meleeAttackRange)
        {
            agent.isStopped = false; // Garante que pode mover
            agent.speed = chaseSpeed;
            agent.SetDestination(_targetPlayer.position);
        }
        else // No alcance, esperando cooldown
        {
            agent.isStopped = true;
            transform.LookAt(new Vector3(_targetPlayer.position.x, transform.position.y, _targetPlayer.position.z));
        }
    }
    // --- FIM DA MODIFICAÇÃO 2 ---

    private void EnterMeleeAttackState()
    {
        if (_currentState == BossState.AttackingMelee || _currentState == BossState.Casting) return; // Previne chamar enquanto já ataca/casta

        Debug.Log("[BossAI] Entrando no estado AttackingMelee.");
        _currentState = BossState.AttackingMelee;
        agent.isStopped = true;
        transform.LookAt(new Vector3(_targetPlayer.position.x, transform.position.y, _targetPlayer.position.z));
        animator.SetTrigger("MeleeAttack");
        _lastMeleeAttackTime = Time.time;
        StartCoroutine(MeleeAttackCooldown());
    }

    private IEnumerator MeleeAttackCooldown()
    {
        Debug.Log("[BossAI] Iniciando cooldown da animação Melee.");
        yield return new WaitForSeconds(meleeAnimationDuration);
        Debug.Log("[BossAI] Cooldown da animação Melee terminou.");
        if (_currentState == BossState.AttackingMelee) // Só volta se ainda estava atacando
        {
            Debug.Log("[BossAI] Voltando para o estado Chasing após ataque Melee.");
            _currentState = BossState.Chasing;
             if (agent.enabled) agent.isStopped = false; // Permite mover novamente
        } else {
            Debug.Log($"[BossAI] Cooldown Melee terminou, mas estado atual é {_currentState}. Não voltando para Chasing.");
        }
    }

    // --- [MODIFICAÇÃO 3] LÓGICA DE DECISÃO SUBSTITUÍDA POR PRIORIDADE ---
    bool TryPrioritizeSpecialAction()
    {
        // Verifica as skills em ordem de prioridade
        // (Você pode reordenar este 'if/else if' para mudar a prioridade)

        // Prioridade 1: Invocação (Summon)
        if (Time.time >= _lastSummonTime + summonCooldown)
        {
            SummonMinions();
            return true;
        }
        
        // Prioridade 2: Poça de Decay (Area)
        if (Time.time >= _lastDecayPoolTime + decayPoolCooldown)
        {
            CastDecayPool();
            return true;
        }

        // Prioridade 3: Sombra Perfurante (Bolt)
        if (Time.time >= _lastShadowBoltTime + shadowBoltCooldown)
        {
            CastShadowBolt();
            return true;
        }

        // Nenhuma skill especial estava pronta, retorna falso
        return false;
    }
    // --- FIM DA MODIFICAÇÃO 3 ---


    // --- FUNÇÃO CHAMADA PELO HEALTH AO SER ATACADO ---
    public void OnAttackedBy(Photon.Realtime.Player attacker)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Se o boss ainda não estava em combate, ativa-o!
        if (!_isEngaged)
        {
            _isEngaged = true; // MARCA A LUTA COMO ATIVA
            _currentState = BossState.Chasing; // Começa a perseguir
            agent.isStopped = false; // Garante que pode mover
            Debug.Log("[BossAI] O Boss foi atacado pela primeira vez! ENTRANDO EM COMBATE!");

            // "Inicia" todos os cooldowns
            float startTime = Time.time;
            _lastMeleeAttackTime = startTime;
            _lastShadowBoltTime = startTime;
            _lastDecayPoolTime = startTime;
            _lastSummonTime = startTime;

            // Mostra a barra de vida
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.ShowBossHealthBar(healthScript, gameObject.name);
            } else { Debug.LogError("[BossAI] HUDManager.Instance não encontrado!"); }

            // ATIVA A BARREIRA E COMEÇA O WIPE CHECK
            if (barrierController != null)
            {
                Debug.Log("[BossAI] Ativando a barreira...");
                barrierController.ActivateBarrier();
            } else { Debug.LogError("[BossAI] Referência barrierController é nula! A barreira não será ativada."); }

            Debug.Log("[BossAI] Chamando StartTrackingPlayersInRoom pela primeira vez...");
            StartTrackingPlayersInRoom(); // Inicia o rastreamento AGORA
        }

        // Atualiza o alvo para quem atacou por último (sempre)
        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (pc.GetComponent<PhotonView>().Owner == attacker)
            {
                _targetPlayer = pc.transform;
                Debug.Log($"[BossAI] Boss agora está mirando em: {attacker.NickName}");
                break;
            }
        }
    }

    // --- FUNÇÃO DE RESET (CHAMADA VIA RPC) ---
    [PunRPC]
    public void RPC_ResetEncounter()
    {
         Debug.Log("[BossAI] >>> RPC_ResetEncounter RECEBIDO <<<");
        _isEngaged = false; // MARCA A LUTA COMO TERMINADA
        _currentState = BossState.Idle;
        _targetPlayer = null;
         _playersInRoom.Clear();

        // Para a corrotina de wipe check
        if (_wipeCheckCoroutine != null)
        {
             Debug.Log("[BossAI] Parando WipeCheckCoroutine durante o reset.");
            StopCoroutine(_wipeCheckCoroutine);
            _wipeCheckCoroutine = null;
        }

        // Reseta vida
        healthScript.Revive();
        if (HUDManager.Instance != null) HUDManager.Instance.HideBossHealthBar();

        // Reseta posição e rotação
         Debug.Log("[BossAI] Resetando posição e rotação.");
         if (agent.enabled)
         {
            agent.isStopped = true; // Garante que para antes de teleportar
            agent.ResetPath();
            agent.enabled = false; // Desativa para teleportar
            transform.position = _startPosition;
            transform.rotation = _startRotation;
            agent.enabled = true; // Reativa
            agent.isStopped = true; // Garante que continua parado após reativar
         } else { // Se o agent já estava desativado (ex: morreu antes do reset?)
             transform.position = _startPosition;
             transform.rotation = _startRotation;
         }


        // Reseta Animator
         Debug.Log("[BossAI] Resetando Animator.");
        animator.Rebind();
        animator.Update(0f);
        animator.SetFloat("Speed", 0f);

        // Reseta cooldowns
         Debug.Log("[BossAI] Resetando cooldowns.");
        float resetTime = -10f;
        _lastMeleeAttackTime = resetTime;
        _lastShadowBoltTime = resetTime;
        _lastDecayPoolTime = resetTime;
        _lastSummonTime = resetTime;
        // _timeSinceLastDecision = 0f; // <-- [MODIFICAÇÃO 4] LINHA REMOVIDA

        // Desativa a barreira e reseta o trigger (Só o Master)
        if(PhotonNetwork.IsMasterClient){
             Debug.Log("[BossAI] Master Client desativando barreira e resetando trigger.");
            if (barrierController != null) barrierController.DeactivateBarrier(); else Debug.LogError("[BossAI] barrierController nulo no reset!");
            if (entryTrigger != null) entryTrigger.ResetTrigger(); else Debug.LogWarning("[BossAI] entryTrigger nulo no reset (opcional).");
        }

        // Destroi minions restantes (Só o Master)
        if (PhotonNetwork.IsMasterClient)
        {
             Debug.Log("[BossAI] Master Client destruindo minions restantes.");
            EnemyAI[] minions = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            int minionsDestroyed = 0;
            foreach(EnemyAI minion in minions)
            {
                // Verifica se o minion pertence a esta instância do boss (se houver múltiplos bosses)
                // Ou simplesmente pelo nome do prefab, como antes:
                if(minion != null && minion.gameObject.name.Contains(minionPrefab.name)) // Adicionado cheque de nulo
                {
                    PhotonNetwork.Destroy(minion.gameObject);
                    minionsDestroyed++;
                }
            }
             Debug.Log($"[BossAI] {minionsDestroyed} minions destruídos.");
        }

         Debug.Log("[BossAI] >>> Reset completo <<<");
    }


    // --- HABILIDADES E CASTING (sem mudanças significativas) ---
    void CastShadowBolt() { _lastShadowBoltTime = Time.time; EnterCastingState("CastBolt", 2.0f); }
    void CastDecayPool() { _lastDecayPoolTime = Time.time; EnterCastingState("CastArea", 2.5f); }
    void SummonMinions() { _lastSummonTime = Time.time; EnterCastingState("CastSummon", 3.0f); }

    private void EnterCastingState(string trigger, float duration)
    {
        if (_currentState == BossState.Casting || _currentState == BossState.AttackingMelee) {
             Debug.LogWarning($"[BossAI] Tentou entrar em Casting ({trigger}) enquanto já estava {_currentState}. Ignorando.");
             return; // Evita interromper outra ação
        }
         Debug.Log($"[BossAI] Entrando no estado Casting para: {trigger}");
        agent.isStopped = true;
        if (_targetPlayer != null)
            transform.LookAt(new Vector3(_targetPlayer.position.x, transform.position.y, _targetPlayer.position.z));
        _currentState = BossState.Casting;
        animator.SetTrigger(trigger);
        StartCoroutine(CastingCooldown(duration));
    }

    private IEnumerator CastingCooldown(float duration)
    {
         Debug.Log("[BossAI] Iniciando cooldown da animação de Casting.");
        yield return new WaitForSeconds(duration);
         Debug.Log("[BossAI] Cooldown da animação de Casting terminou.");
         if(_currentState == BossState.Casting) // Só volta se ainda estava conjurando
         {
             Debug.Log("[BossAI] Voltando para o estado Chasing após Casting.");
             _currentState = BossState.Chasing;
             if(agent.enabled) agent.isStopped = false; // Permite mover
         } else {
              Debug.Log($"[BossAI] Cooldown Casting terminou, mas estado atual é {_currentState}. Não voltando para Chasing.");
         }
    }


    // --- EVENTOS DE ANIMAÇÃO (sem mudanças) ---
    // Funções Enable/DisableMeleeHitbox foram removidas daqui, estão no Relay
    public void AnimEvent_FireProjectile() { if (shadowBoltPrefab != null && projectileSpawnPoint != null) { PhotonNetwork.InstantiateRoomObject(shadowBoltPrefab.name, projectileSpawnPoint.position, transform.rotation); } }
    public void AnimEvent_SpawnDecayPool() { if (decayPoolPrefab != null && _targetPlayer != null) { PhotonNetwork.InstantiateRoomObject(decayPoolPrefab.name, _targetPlayer.position, Quaternion.identity); } }
    public void AnimEvent_SpawnMinions() { for (int i = 0; i < 2; i++) { if (minionPrefab != null && minionSpawnPoints.Count > 0) { Transform spawnPoint = minionSpawnPoints[Random.Range(0, minionSpawnPoints.Count)]; GameObject minionGO = PhotonNetwork.InstantiateRoomObject(minionPrefab.name, spawnPoint.position, spawnPoint.rotation); EnemyAI minionAI = minionGO.GetComponent<EnemyAI>(); if (minionAI != null) { minionAI.Initialize(new Transform[0]); } } } }


    // --- FUNÇÕES AUXILIARES ---
    void FindTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        Transform closestPlayer = null;
        float minDistance = Mathf.Infinity;
        bool foundTarget = false;

        foreach (GameObject player in players)
        {
             if(player == null) continue; // Segurança extra
            Health playerHealth = player.GetComponent<Health>();
            // Verifica se está vivo E se está na lista de rastreamento (garante que está na sala)
            if (playerHealth != null && !playerHealth.IsDead() && _playersInRoom.Contains(playerHealth))
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPlayer = player.transform;
                    foundTarget = true;
                }
            }
        }
         if(foundTarget) {
             _targetPlayer = closestPlayer;
         } else if (_isEngaged) {
            _targetPlayer = null;
            // Debug.Log("[BossAI] FindTarget não encontrou jogadores VIVOS E NA SALA."); // Log pode poluir muito
         }
    }

    // --- MORTE ---
    public void OnDeath()
    {
         Debug.Log("[BossAI] >>> Boss DERROTADO! <<<");
        _isEngaged = false; // Para a IA e o wipe check

        if (_wipeCheckCoroutine != null)
        {
            Debug.Log("[BossAI] Parando WipeCheckCoroutine na morte do boss.");
            StopCoroutine(_wipeCheckCoroutine);
            _wipeCheckCoroutine = null;
        }

        // Desativa a barreira (Só o Master)
        if(PhotonNetwork.IsMasterClient)
        {
             Debug.Log("[BossAI] Master Client desativando barreira na morte do boss.");
            if (barrierController != null) barrierController.DeactivateBarrier();
        }

        if (HUDManager.Instance != null) HUDManager.Instance.HideBossHealthBar();

        this.enabled = false;
        if (agent != null) agent.enabled = false;
        if (GetComponent<Collider>() != null) GetComponent<Collider>().enabled = false;

         Debug.Log("[BossAI] Rotina de morte concluída.");
        // Loot/Fim da dungeon aqui
    }

    // --- CALLBACK DO PHOTON PARA JOGADORES SAINDO ---
     public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
     {
         if (PhotonNetwork.IsMasterClient && _isEngaged)
         {
             Debug.Log($"[BossAI] Jogador {otherPlayer.NickName} saiu. Verificando lista de rastreamento...");
             // Encontra o componente Health do jogador que saiu para remover da lista
             Health healthToRemove = null;
             foreach(Health h in _playersInRoom){
                 if(h != null && h.GetComponent<PhotonView>().Owner == otherPlayer){
                     healthToRemove = h;
                     break;
                 }
             }
             if(healthToRemove != null){
                 _playersInRoom.Remove(healthToRemove);
                 Debug.Log($"[BossAI] Jogador removido da lista. Jogadores restantes rastreados: {_playersInRoom.Count}");
             } else {
                 Debug.LogWarning($"[BossAI] Jogador {otherPlayer.NickName} saiu, mas não foi encontrado na lista de rastreamento.");
             }


             // Se não sobrou ninguém NA LISTA e a luta estava ativa, reseta
             if (_playersInRoom.Count == 0 && _isEngaged)
             {
                 Debug.Log("[BossAI] Último jogador rastreado saiu da sala durante a luta. Resetando.");
                 photonView.RPC("RPC_ResetEncounter", RpcTarget.All);
             }
             // Se ainda há jogadores, mas o alvo era quem saiu, encontra um novo alvo
             else if (_targetPlayer != null && _targetPlayer.GetComponent<PhotonView>().Owner == otherPlayer)
             {
                 Debug.Log("[BossAI] O alvo atual saiu, procurando novo alvo...");
                 FindTarget();
             }
         }
     }
}