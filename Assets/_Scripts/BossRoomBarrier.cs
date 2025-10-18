using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class BossRoomBarrier : MonoBehaviour
{
    [SerializeField] private GameObject barrierVisual;
    [SerializeField] private Collider barrierCollider;

    private PhotonView photonView;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        Debug.Log($"[BossRoomBarrier] Awake no objeto '{gameObject.name}'.");
        // REMOVEMOS a lógica que desativava visual/collider aqui. O RPC cuidará disso.
        // Garantimos apenas que o ESTADO INICIAL (desligado) seja sincronizado se alguém entrar atrasado
        if (PhotonNetwork.IsMasterClient)
        {
            // Força um RPC inicial para garantir que todos vejam desligado
            // (Pode ser redundante se o prefab já estiver desligado, mas é seguro)
            RPC_SetBarrierState(false);
        }
    }

    public void ActivateBarrier()
    {
        Debug.Log("[BossRoomBarrier] ActivateBarrier() chamado no Master Client. Enviando RPC...");
        photonView.RPC("RPC_SetBarrierState", RpcTarget.All, true);
    }

    public void DeactivateBarrier()
    {
        Debug.Log("[BossRoomBarrier] DeactivateBarrier() chamado no Master Client. Enviando RPC...");
        photonView.RPC("RPC_SetBarrierState", RpcTarget.All, false);
    }

    // Dentro de BossRoomBarrier.cs -> RPC_SetBarrierState

    [PunRPC]
    private void RPC_SetBarrierState(bool isActive)
    {
        Debug.Log($"[BossRoomBarrier] RPC recebido. Definindo para: {isActive}");

        // Lógica do Visual (já sabemos que funciona)
        if (barrierVisual != null) barrierVisual.SetActive(isActive);
        else Debug.LogWarning("[BossRoomBarrier] barrierVisual nulo.");

        // Lógica do Collider com Log Pós-Ação
        if (barrierCollider != null)
        {
            try
            {
                Debug.Log($"[BossRoomBarrier] Collider ANTES: enabled={barrierCollider.enabled}");
                barrierCollider.enabled = isActive;
                // Log IMEDIATAMENTE DEPOIS
                Debug.Log($"[BossRoomBarrier] Collider DEPOIS: enabled={barrierCollider.enabled}. Tentativa foi para {isActive}.");
            }
            catch (System.Exception e) { Debug.LogError($"[BossRoomBarrier] ERRO ao setar collider.enabled: {e.Message}", barrierCollider); }
        }
        else { Debug.LogWarning("[BossRoomBarrier] barrierCollider nulo."); }
    }
}