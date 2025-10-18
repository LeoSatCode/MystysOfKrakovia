using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableSkillIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Skill skill; // O ScriptableObject da habilidade que este ícone representa
    private Transform originalParent;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Guarda o pai original e move o ícone para o topo da hierarquia do Canvas
        originalParent = transform.parent;
        transform.SetParent(transform.root); // Move para o Canvas principal
        transform.SetAsLastSibling(); // Garante que fique na frente de tudo

        // Torna o ícone "transparente" a cliques para que o OnDrop do slot funcione
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Move o ícone junto com o mouse
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Devolve o ícone para o pai original se não for solto em um slot válido
        transform.SetParent(originalParent);
        transform.position = originalParent.position;

        // Permite que o ícone seja clicado novamente
        canvasGroup.blocksRaycasts = true;
    }
}