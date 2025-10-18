using UnityEngine;

[CreateAssetMenu(fileName = "New Health Potion", menuName = "Mists of Krackovia/Items/Health Potion")]
public class HealthPotionItem : ItemData
{
    public int healAmount;

    public override void Use(PlayerController owner)
    {
        Debug.Log($"Usando {itemName}, curando {healAmount} de vida.");
        owner.GetComponent<Health>().RPC_ApplyHeal(healAmount);
    }
}