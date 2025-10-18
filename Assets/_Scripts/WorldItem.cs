using UnityEngine;
using Photon.Pun;

public class WorldItem : MonoBehaviour, IPunInstantiateMagicCallback
{
    public ItemData itemData;
    private PhotonView _photonView;

    void Awake()
    {
        _photonView = GetComponent<PhotonView>();
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] instantiationData = info.photonView.InstantiationData;
        if (instantiationData != null && instantiationData.Length > 0)
        {
            string itemName = (string)instantiationData[0];
            itemData = Resources.Load<ItemData>("_Items/" + itemName);

            if (itemData != null)
            {
                SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.sprite = itemData.icon;
                }
            }
        }
    }

    // Esta é a função que o PlayerController chama
    public bool TryCollect()
    {
        if (InventoryManager.Instance == null) return false;

        bool wasAdded = InventoryManager.Instance.AddItem(itemData);

        if (wasAdded)
        {
            // Se foi coletado com sucesso, envia um RPC para que o objeto seja destruído em todas as máquinas.
            _photonView.RPC("RPC_DestroyItem", RpcTarget.All);
        }

        return wasAdded;
    }

    [PunRPC]
    private void RPC_DestroyItem()
    {
        Destroy(gameObject);
    }
}