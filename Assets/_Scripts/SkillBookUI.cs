using UnityEngine;
using UnityEngine.UI;

public class SkillBookUI : MonoBehaviour
{
    public static SkillBookUI Instance { get; private set; }

    [SerializeField] private GameObject skillBookPanel;
    [SerializeField] private Transform skillGridContainer;
    [SerializeField] private GameObject draggableIconPrefab;

    private PlayerController localPlayerController;

    void Awake() => Instance = this;
    void Start() => skillBookPanel.SetActive(false);

    public void TogglePanel()
    {
        skillBookPanel.SetActive(!skillBookPanel.activeSelf);
        if (skillBookPanel.activeSelf)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (localPlayerController == null)
            FindPlayer();

        // Limpa os ícones antigos
        foreach (Transform child in skillGridContainer)
            Destroy(child.gameObject);

        // Cria um ícone para cada skill aprendida
        foreach (Skill skill in localPlayerController.unlockedSkills)
        {
            GameObject iconGO = Instantiate(draggableIconPrefab, skillGridContainer);
            iconGO.GetComponent<Image>().sprite = skill.icon;
            iconGO.GetComponent<DraggableSkillIcon>().skill = skill;
        }
    }

    private void FindPlayer()
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
}