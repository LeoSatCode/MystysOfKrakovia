using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class PortalController : MonoBehaviour
{
    [Tooltip("O objeto filho que contém o collider e o script do portal.")]
    [SerializeField] private GameObject activationZone;

    private PhotonView photonView;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    public void ActivatePortalForAll()
    {
        if (photonView == null)
        {
            Debug.LogError("PhotonView não encontrado no objeto do portal!");
            return;
        }
        photonView.RPC("RPC_ActivatePortal", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_ActivatePortal()
    {
        Debug.Log("Portal ativado para este cliente!");
        if (activationZone != null)
        {
            // Ativa apenas a zona de trigger para todos
            activationZone.SetActive(true);
        }
        else
        {
            Debug.LogError("A 'Activation Zone' não foi configurada no Inspector do PortalController!");
        }
    }
}