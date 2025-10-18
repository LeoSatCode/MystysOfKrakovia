using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ActionBarSlot : MonoBehaviour, IDropHandler
{
    [Header("Configuração do Slot")]
    public int slotIndex;

    [Header("Referências da UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownOverlay; // NOVO
    [SerializeField] private TMP_Text cooldownText;   // NOVO

    private PlayerController localPlayerController;
    private Skill mySkill; // NOVO: Guarda a skill deste slot

    void Start()
    {
        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (pc.GetComponent<Photon.Pun.PhotonView>().IsMine)
            {
                localPlayerController = pc;
                break;
            }
        }
    }

    void Update()
    {
        // Se não houver skill neste slot, não faz nada
        if (mySkill == null) return;

        // Pede ao PlayerController a informação de cooldown desta skill
        if (localPlayerController.GetSkillCooldownData(mySkill.id, out float lastUsedTime))
        {
            float cooldownDuration = mySkill.cooldown;
            float timePassed = Time.time - lastUsedTime;

            // A skill está em cooldown?
            if (timePassed < cooldownDuration)
            {
                cooldownOverlay.gameObject.SetActive(true);
                cooldownText.gameObject.SetActive(true);

                float remainingTime = cooldownDuration - timePassed;

                // Atualiza a sombra radial
                cooldownOverlay.fillAmount = remainingTime / cooldownDuration;

                // Atualiza o texto do contador
                cooldownText.text = Mathf.CeilToInt(remainingTime).ToString();
            }
            else // Cooldown terminou
            {
                cooldownOverlay.gameObject.SetActive(false);
                cooldownText.gameObject.SetActive(false);
            }
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableSkillIcon draggableIcon = eventData.pointerDrag.GetComponent<DraggableSkillIcon>();
        if (draggableIcon != null)
        {
            localPlayerController.SetSkillInActionBar(slotIndex, draggableIcon.skill);
            // Não precisa chamar UpdateSlotUI daqui, pois o evento OnActionBarChanged fará isso
        }
    }

    public void UpdateSlotUI(Skill skill)
    {
        mySkill = skill; // Guarda a referência da skill
        if (mySkill != null)
        {
            iconImage.sprite = mySkill.icon;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
            // Garante que o feedback de cooldown suma se o slot for limpo
            cooldownOverlay.gameObject.SetActive(false);
            cooldownText.gameObject.SetActive(false);
        }
    }
}