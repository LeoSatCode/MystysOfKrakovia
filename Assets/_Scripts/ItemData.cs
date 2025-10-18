using UnityEngine;

// Enum para categorizar os itens
public enum ItemType { Potion, Currency, QuestItem }

// Classe base para todos os itens
public abstract class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public ItemType itemType;
    [TextArea(3, 5)]
    public string description;
    
    public bool isStackable = true;
    public int maxStackSize = 16;

    public bool isUnique = false;

    // Cada item terá uma forma diferente de ser "usado"
    public abstract void Use(PlayerController owner);
}