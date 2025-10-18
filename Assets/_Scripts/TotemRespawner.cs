using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class TotemRespawner : MonoBehaviour
{
    [Header("Configurações")]
    [Tooltip("A lista de todos os totens que este script deve gerenciar.")]
    [SerializeField] private List<GameObject> totemsToManage;

    [Tooltip("Tempo em segundos para verificar e renascer os totens.")]
    [SerializeField] private float respawnCheckTime = 30f; // A cada 30 segundos é mais eficiente

    void Start()
    {
        // Apenas o Master Client terá a responsabilidade de controlar o respawn.
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Este cliente é o Master. Iniciando a rotina de respawn de totens.");
            StartCoroutine(CheckAndRespawnTotems());
        }
    }

    private IEnumerator CheckAndRespawnTotems()
    {
        // Loop infinito que roda apenas no Master Client
        while (true)
        {
            // Espera o tempo definido antes de fazer a próxima verificação
            yield return new WaitForSeconds(respawnCheckTime);

            Debug.Log("Verificando o estado dos totens...");

            // Passa por cada totem na lista
            foreach (GameObject totem in totemsToManage)
            {
                // Se o totem não estiver nulo E estiver inativo...
                if (totem != null && !totem.activeInHierarchy)
                {
                    Debug.Log($"Totem '{totem.name}' está inativo. Enviando comando de respawn.");

                    // ...pega o PhotonView dele e chama o RPC para reativá-lo para TODOS.
                    PhotonView totemView = totem.GetComponent<PhotonView>();
                    if (totemView != null)
                    {
                        totemView.RPC("RPC_RespawnObject", RpcTarget.All);
                    }
                }
            }
        }
    }
}