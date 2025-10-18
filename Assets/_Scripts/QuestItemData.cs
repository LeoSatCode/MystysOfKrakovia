using UnityEngine;

[CreateAssetMenu(fileName = "New Quest Item", menuName = "Mists of Krackovia/Items/Quest Item")]
public class QuestItemData : ItemData
{
    // A função 'Use' para um item de missão geralmente não faz nada,
    // pois ele é entregue a um NPC em vez de ser "usado" pelo jogador.
    public override void Use(PlayerController owner)
    {
        Debug.Log($"Isto é um item de missão ({itemName}) e não pode ser usado diretamente.");
    }
}