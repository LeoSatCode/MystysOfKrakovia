using UnityEngine;
using Photon.Pun;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Configuração do Spawn")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float respawnTime = 30f;
    
    [Header("Configuração da IA")]
    [SerializeField] private Transform[] patrolPoints;

    private GameObject _spawnedEnemy;
    private bool _isRespawning = false;

    // A função Start é chamada uma única vez quando o jogo começa.
    void Start()
    {
        // Apenas o Master Client deve instanciar inimigos.
        if (PhotonNetwork.IsMasterClient)
        {
            // Mágica aqui: chamamos a função de spawn diretamente no início.
            SpawnEnemy();
        }
    }

    void Update()
    {
        // A lógica de respawn continua no Update
        if (!PhotonNetwork.IsMasterClient) { return; }

        if (_spawnedEnemy == null && !_isRespawning)
        {
            // Agora, só chamamos a corrotina de espera APÓS a primeira morte.
            StartCoroutine(RespawnEnemyAfterDelay());
        }
    }

    // Renomeamos para ser mais claro
    private IEnumerator RespawnEnemyAfterDelay()
    {
        _isRespawning = true;
        Debug.Log($"Inimigo do spawner {gameObject.name} derrotado. Renascerá em {respawnTime} segundos.");
        
        yield return new WaitForSeconds(respawnTime);

        SpawnEnemy();

        _isRespawning = false;
    }

    // Criamos uma função separada para a lógica de spawn, para ser reutilizada
    void SpawnEnemy()
    {
        Debug.Log($"A instanciar inimigo em {gameObject.name}...");
        _spawnedEnemy = PhotonNetwork.InstantiateRoomObject(enemyPrefab.name, transform.position, transform.rotation);
        
        EnemyAI ai = _spawnedEnemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.Initialize(patrolPoints);
        }
    }
}