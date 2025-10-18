using UnityEngine;
using Photon.Pun;

public class BossRoomTrigger : MonoBehaviour
{
    [SerializeField] private BossRoomBarrier barrierController;
    [SerializeField] private Boss_GuardianAI bossAI;

    private bool _barrierActivated = false;

    // Dentro de BossRoomTrigger.cs
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[BossRoomTrigger] OnTriggerEnter com: {other.gameObject.name} (Tag: {other.tag})");

        if (_barrierActivated) return; // Se a luta já começou por ataque, não faz nada
        if (!other.CompareTag("Player")) return;
        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv == null || !pv.IsMine) return;

        Debug.Log("[BossRoomTrigger] Jogador local detectado entrando na área.");

        // A lógica de ativação foi MOVIDA para o Boss_GuardianAI.OnAttackedBy
        // Apenas logamos a entrada aqui, se necessário.
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[BossRoomTrigger] Master Client registrou entrada do jogador local.");
            // Não faz mais nada aqui.
        }
        _barrierActivated = false; // Garantir que está falso até a luta começar

        Debug.Log("[BossRoomTrigger] Fim da execução de OnTriggerEnter.");
    }

    // A função ResetTrigger pode continuar existindo se você quiser usá-la externamente
    public void ResetTrigger()
    {
        Debug.Log("[BossRoomTrigger] Resetando o trigger (se necessário).");
        _barrierActivated = false;
        // GetComponent<Collider>().enabled = true; // Se desativar o collider em algum momento
    }
}