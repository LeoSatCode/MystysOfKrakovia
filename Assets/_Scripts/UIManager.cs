using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Referências de Paineis")]
    [SerializeField] private GameObject settingsPanel;

    private bool isMenuOpen = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettingsPanel();
        }
    }

    public void ToggleSettingsPanel()
    {
        isMenuOpen = !isMenuOpen;
        settingsPanel.SetActive(isMenuOpen);

        if (isMenuOpen)
        {
            // Apenas libera o cursor do mouse
            // Time.timeScale = 0f; // <-- REMOVIDO
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Apenas retoma o estado normal do cursor (que será controlado pela câmera)
            // Time.timeScale = 1f; // <-- REMOVIDO
        }
    }

    public bool IsMenuOpen()
    {
        return isMenuOpen;
    }
}