using UnityEngine;
using Photon.Pun;
using System.Collections;

public class RoomManager : MonoBehaviour
{
    [Header("Configuração de Spawn")]
    [SerializeField] public Transform spawnPoint;

    [Header("Referências da Cena")]
    [SerializeField] private CameraController gameCamera;
   
    [Tooltip("Arraste o Prefab do Portal que leva para a Dungeon do Boss")]
    [SerializeField] private PortalController portalCemiterio;

    void Start()
    {
        Debug.Log("Conectado à sala. Lendo dados do personagem para instanciar...");

        CharacterData data = GameManager.SelectedCharacter;

        if (data == null)
        {
            Debug.LogError("Dados do personagem selecionado são nulos! Não foi possível instanciar.");
            return;
        }

        // A lógica de if/else para determinar o nome do prefab (exatamente como era antes)
        string prefabNameToInstantiate;
        if (data.characterClass == "Guerreiro") { prefabNameToInstantiate = "PlayerPrefab_Warrior"; }
        else if (data.characterClass == "Mago") { prefabNameToInstantiate = "PlayerPrefab_Mage"; }
        else if (data.characterClass == "Arqueiro") { prefabNameToInstantiate = "PlayerPrefab_Archer"; }
        else if (data.characterClass == "Clérigo") { prefabNameToInstantiate = "PlayerPrefab_Cleric"; }
        else
        {
            Debug.LogWarning("Classe não reconhecida: " + data.characterClass + ". Usando Guerreiro como padrão.");
            prefabNameToInstantiate = "PlayerPrefab_Warrior";
        }

        Debug.Log($"Instanciando prefab '{prefabNameToInstantiate}' para o personagem '{data.characterName}'");

        // A chamada de Instantiate agora usa o nome direto, sem a subpasta
        GameObject playerInstance = PhotonNetwork.Instantiate(prefabNameToInstantiate, spawnPoint.position, spawnPoint.rotation);

        if (playerInstance == null)
        {
            Debug.LogError($"FALHA AO INSTANCIAR! Verifique se o nome do prefab '{prefabNameToInstantiate}' está EXATAMENTE correto e se o prefab está em uma pasta 'Resources'.");
            return;
        }

        if (playerInstance.GetComponent<PhotonView>().IsMine)
        {
            if(gameCamera != null)
                gameCamera.target = playerInstance.transform;

            StartCoroutine(ApplyLoadedData(playerInstance));
        }
    }

    private IEnumerator ApplyLoadedData(GameObject playerInstance)
    {
        yield return null; 

        Debug.Log("Aplicando dados salvos ao personagem instanciado...");

        // Carrega os dados (como já estava)
        playerInstance.GetComponent<PlayerController>()?.LoadFromSaveData(GameManager.SelectedCharacter);
        
        // 1. Pega a referência do QuestManager
        QuestManager questManager = FindFirstObjectByType<QuestManager>();
        
        // 2. Carrega os dados dele
        questManager?.LoadFromSaveData(GameManager.SelectedCharacter); 
        
        FindFirstObjectByType<InventoryManager>()?.LoadFromSaveData(GameManager.SelectedCharacter);

        // --- NOSSA NOVA LÓGICA DE VERIFICAÇÃO ---
        if (questManager != null && portalCemiterio != null)
        {
            // 3. Verifica se a missão 5 (ID do portal) está na lista de completas
            if (questManager.completedQuests.Contains(5))
            {
                Debug.Log("Jogador já completou a missão do portal. Ativando-o para todos.");
                
                // 4. Ativa o portal para todos na sala
                portalCemiterio.ActivatePortalForAll();
            }
        }
        // --- FIM DA NOVA LÓGICA ---

        Debug.Log("Dados aplicados com sucesso!");
    }
}