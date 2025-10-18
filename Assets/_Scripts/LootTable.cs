using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

// Uma classe auxiliar para organizar os drops no Inspector
[System.Serializable]
public class LootDrop
{
    public ItemData item;
    [Range(0, 100)]
    public float dropChance;
}

[CreateAssetMenu(fileName = "New Loot Table", menuName = "Mists of Krackovia/Loot Table")]
public class LootTable : ScriptableObject
{
    public List<LootDrop> possibleDrops;

    // Esta função "rola os dados" e decide qual item dropar
    public void CalculateAndDropLoot(Vector3 dropPosition)
    {
        // Rola um número para a chance de dropar ALGO vs NADA
        float dropRoll = Random.Range(0f, 100f);

        // Exemplo: 70% de chance de dropar algo, 30% de não vir nada
        if (dropRoll > 30f)
        {
            // Se vai dropar algo, agora decidimos O QUÊ
            float itemRoll = Random.Range(0f, 100f);
            float currentChance = 0f;

            foreach (var drop in possibleDrops)
            {
                currentChance += drop.dropChance;
                if (itemRoll <= currentChance)
                {
                    // Encontrou o item para dropar!
                    Debug.Log($"Dropando o item: {drop.item.itemName}");
                    // Chama a função para instanciar o item no mundo
                    SpawnLootInWorld(dropPosition, drop.item);
                    return; // Para o loop para não dropar múltiplos itens
                }
            }
        }
        else
        {
            Debug.Log("Nenhum loot dropado desta vez.");
        }
    }

    private void SpawnLootInWorld(Vector3 position, ItemData item)
    {
        // Instancia o objeto de loot para todos na sala e passa o nome do item para ele
        // O nome do item é usado para que o objeto saiba qual ItemData carregar.
        object[] instantiationData = { item.name };
        PhotonNetwork.InstantiateRoomObject("WorldItem_Prefab", position, Quaternion.identity, 0, instantiationData);
    }
}