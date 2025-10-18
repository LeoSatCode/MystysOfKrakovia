using UnityEngine;

public class ActionBarUI : MonoBehaviour
{
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject actionBarSlotPrefab;

    private ActionBarSlot[] slots;

    void Start()
    {
        PlayerController.OnActionBarChanged += UpdateAllSlots;
        CreateSlots();
    }

    void OnDestroy()
    {
        PlayerController.OnActionBarChanged -= UpdateAllSlots;
    }

    void CreateSlots()
    {
        slots = new ActionBarSlot[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject slotGO = Instantiate(actionBarSlotPrefab, slotsContainer);
            slots[i] = slotGO.GetComponent<ActionBarSlot>();
            slots[i].slotIndex = i;
            slots[i].UpdateSlotUI(null); // Começa vazio
        }
    }

    void UpdateAllSlots()
    {
         PlayerController localPlayerController = null;
         foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
         {
             if (pc.GetComponent<Photon.Pun.PhotonView>().IsMine)
             {
                 localPlayerController = pc;
                 break;
             }
         }

         if (localPlayerController == null) return;

         for (int i = 0; i < 5; i++)
         {
             slots[i].UpdateSlotUI(localPlayerController.actionBarSlots[i]);
         }
    }
}