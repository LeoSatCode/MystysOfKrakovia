using UnityEngine;

[CreateAssetMenu(fileName = "New Resource Potion", menuName = "Mists of Krackovia/Items/Resource Potion")]
public class ResourcePotionItem : ItemData
{
    public int resourceAmount;
    public ResourceType resourceType; // Para saber se é de Mana, Fúria ou Energia

    public override void Use(PlayerController owner)
    {
        Debug.Log($"Usando {itemName}, restaurando {resourceAmount} de {resourceType}.");
        // A lógica para restaurar o recurso específico viria aqui
    }
}