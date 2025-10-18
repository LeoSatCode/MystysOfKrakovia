using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    public static event Action OnInventoryChanged;

    public List<InventorySlot> inventory = new List<InventorySlot>();
    [SerializeField] private int inventorySize = 12;

    void Awake()
    {
        Instance = this;
        Debug.Log(">>> INVENTORY MANAGER: Awake! A instância foi criada.");
    }

    // --- NOVA FUNÇÃO AUXILIAR ---
    public bool HasItem(ItemData item)
    {
        foreach (var slot in inventory)
        {
            if (slot.item == item)
                return true;
        }
        return false;
    }

    public void UpdateSaveData(CharacterData saveData)
    {
        saveData.inventory.Clear();
        foreach (var slot in inventory)
        {
            saveData.inventory.Add(new InventorySaveData { itemName = slot.item.name, quantity = slot.quantity });
        }
    }

    public void LoadFromSaveData(CharacterData saveData)
    {
        inventory.Clear();
        foreach (var savedSlot in saveData.inventory)
        {
            ItemData item = Resources.Load<ItemData>("_Items/" + savedSlot.itemName);
            if (item != null)
            {
                inventory.Add(new InventorySlot(item, savedSlot.quantity));
            }
        }
        OnInventoryChanged?.Invoke();
    }

    public bool AddItem(ItemData item)
    {
        // --- NOVA VERIFICAÇÃO DE ITEM ÚNICO ---
        if (item.isUnique && HasItem(item))
        {
            Debug.Log($"Você já possui o item único '{item.itemName}'.");
            return false; // Falha ao adicionar pois já tem
        }
        // --- FIM DA VERIFICAÇÃO ---

        // Se o item é empilhável, procura por um slot que já tenha esse item
        if (item.isStackable)
        {
            foreach (InventorySlot slot in inventory)
            {
                if (slot.item == item && slot.quantity < item.maxStackSize)
                {
                    slot.AddQuantity(1);
                    Debug.Log($">>> INVENTORY MANAGER: Item '{item.itemName}' empilhado. Disparando evento OnInventoryChanged...");
                    OnInventoryChanged?.Invoke(); // Notifica a UI
                    return true;
                }
            }
        }

        // Se não encontrou um stack ou o item não é empilhável, procura um slot vazio
        if (inventory.Count < inventorySize)
        {
            inventory.Add(new InventorySlot(item, 1));
            Debug.Log($">>> INVENTORY MANAGER: Item '{item.itemName}' adicionado a um novo slot. Disparando evento OnInventoryChanged...");
            OnInventoryChanged?.Invoke(); // Notifica a UI
            return true;
        }

        // Se chegou aqui, o inventário está cheio
        Debug.Log("Inventário cheio!");
        return false;
    }

    public void RemoveItem(ItemData itemToRemove)
    {
        InventorySlot slotToRemove = null;
        foreach (var slot in inventory)
        {
            if (slot.item == itemToRemove)
            {
                slot.quantity--;
                if (slot.quantity <= 0)
                {
                    slotToRemove = slot;
                }
                break;
            }
        }

        if (slotToRemove != null)
        {
            inventory.Remove(slotToRemove);
        }

        OnInventoryChanged?.Invoke();
    }

    public bool ConsumeItem(ItemData item)
    {
        foreach (InventorySlot slot in inventory)
        {
            if (slot.item == item)
            {
                slot.quantity--;

                if (slot.quantity <= 0)
                {
                    inventory.Remove(slot);
                }

                Debug.Log($">>> INVENTORY MANAGER: Item '{item.itemName}' consumido. Disparando evento OnInventoryChanged...");
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        return false;
    }
}
