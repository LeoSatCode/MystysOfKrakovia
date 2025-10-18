using UnityEngine;
using Photon.Pun;

public class ScenePortal : MonoBehaviour
{
    [Tooltip("O nome exato da cena para onde o portal leva")]
    public string sceneToLoad;

    [Tooltip("O ID da missão necessária para usar este portal. Deixe 0 para desativar a checagem.")]
    public int requiredQuestID = 5;

    private void OnTriggerEnter(Collider other)
    {
        // Verifica se quem entrou no trigger é o nosso jogador local
        if (other.CompareTag("Player") && other.GetComponent<PhotonView>().IsMine)
        {
            // --- NOVA LÓGICA ---
            // O portal pode ser usado se o ID for 0 (sem checagem)
            // OU se o QuestManager existir E o jogador tiver completado a quest.
            bool canPass = (requiredQuestID == 0) || 
                           (QuestManager.Instance != null && QuestManager.Instance.completedQuests.Contains(requiredQuestID));
            // --- FIM DA NOVA LÓGICA ---

            if (canPass)
            {
                Debug.Log($"Requisitos do portal atendidos! Entrando no portal para a cena: {sceneToLoad}");
                
                // Força o salvamento do jogo antes de sair da dungeon
                GameManager.Instance.SaveGame();

                GetComponent<Collider>().enabled = false;
                PhotonNetwork.LoadLevel(sceneToLoad);
            }
            else
            {
                Debug.Log($"Jogador tentou usar o portal sem ter a missão {requiredQuestID} completa.");
            }
        }
    }
}