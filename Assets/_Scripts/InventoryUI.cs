using UnityEngine;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject inventorySlotPrefab;

    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        inventoryPanel.SetActive(false);
        InitializeUI(12); // O tamanho do seu inventário
        UpdateUI(); // Limpa a UI no início
    }

    // --- MUDANÇA PRINCIPAL AQUI ---
    // Trocamos Start/OnDestroy por OnEnable/OnDisable para gerenciar os eventos.

    private void OnEnable()
    {
        // Se inscreve no evento toda vez que o painel de inventário (ou a cena) é ativado.
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnInventoryChanged += UpdateUI;
            Debug.Log(">>> INVENTORY UI: Inscrição no evento OnInventoryChanged realizada.");
            UpdateUI(); // Garante que a UI está atualizada ao ser ativada.
        }
    }

    private void OnDisable()
    {
        // Se desinscreve do evento toda vez que o painel é desativado ou destruído.
        // Isso evita que o "fantasma" da UI tente se atualizar.
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnInventoryChanged -= UpdateUI;
            Debug.Log(">>> INVENTORY UI: Inscrição no evento removida.");
        }
    }
    
    // O OnDestroy não é mais necessário para gerenciar eventos.
    // private void OnDestroy() { ... }

    void InitializeUI(int size)
    {
        // Limpa a lista para o caso de a inicialização ser chamada mais de uma vez.
        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }
        slotUIs.Clear();
        
        for(int i = 0; i < size; i++)
        {
            GameObject slotGO = Instantiate(inventorySlotPrefab, slotsContainer);
            slotUIs.Add(slotGO.GetComponent<InventorySlotUI>());
        }
    }

    void UpdateUI()
    {
        if (InventoryManager.Instance == null) return;

        Debug.Log(">>> INVENTORY UI: Evento recebido! Redesenhando os slots...");
        for(int i = 0; i < slotUIs.Count; i++)
        {
            if(i < InventoryManager.Instance.inventory.Count)
            {
                slotUIs[i].UpdateSlot(InventoryManager.Instance.inventory[i]);
            }
            else
            {
                slotUIs[i].ClearSlot();
            }
        }
    }

    public void TogglePanel()
    {
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
    }
}