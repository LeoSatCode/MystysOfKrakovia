using UnityEngine;
using Photon.Pun;

public class QuestObject : MonoBehaviour
{
    [SerializeField] private int questID;
    private PhotonView _photonView;

    void Awake()
    {
        _photonView = GetComponent<PhotonView>();
    }

    public void OnInteract()
    {
        if (QuestManager.Instance != null)
        {
            if (QuestManager.Instance.activeQuests.ContainsKey(questID))
            {
                QuestManager.Instance.AddQuestProgress_Interact(questID);
            }
        }

        // Desativa o Totem para todos na rede
        _photonView.RPC("RPC_DeactivateObject", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_DeactivateObject()
    {
        Debug.Log($"Objeto de missão '{gameObject.name}' desativado para todos.");
        gameObject.SetActive(false);
    }

    // --- NOVA FUNÇÃO DE RESPAWN ---
    [PunRPC]
    public void RPC_RespawnObject()
    {
        Debug.Log($"Objeto de missão '{gameObject.name}' reativado para todos.");
        gameObject.SetActive(true);
    }
}