// Assinatura: AnimationEventRelay.cs
using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    // --- REFERÊNCIAS ---
    // Precisamos de referências para os dois scripts que podemos precisar chamar.
    // Nem todos os personagens terão os dois, então verificaremos antes de usar.
    [SerializeField] private PlayerController playerController;
    [SerializeField] private MeleeHitbox meleeHitbox;


    // --- FUNÇÕES DE EVENTO PARA O MAGO ---

    // Esta função é chamada pela animação de magia do Mago

    public void TriggerFootstepEvent()
    {
        if (playerController != null)
        {
            playerController.PlayFootstepSound();
        }
    }
    public void TriggerFireProjectileEvent()
    {
        // Ele simplesmente repassa o chamado para a função correta no script principal
        if (playerController != null)
        {
            playerController.AnimationEvent_FireProjectile();
        }
        else
        {
            Debug.LogError("PlayerController não está atribuído no AnimationEventRelay para o evento de projétil!");
        }
    }

    public void TriggerArrowEvent()
    {
        // Ele simplesmente repassa o chamado para a função correta no script principal
        if (playerController != null)
        {
            playerController.AnimationEvent_Arrow();
        }
        else
        {
            Debug.LogError("PlayerController não está atribuído no AnimationEventRelay para o evento de projétil!");
        }
    }

    public void TriggerSkillActionEvent()
    {
        if (playerController != null)
        {
            // Repassa a chamada para a nova função genérica no PlayerController
            playerController.AnimationEvent_SkillAction();
        }
        else
        {
            Debug.LogError("PlayerController não está atribuído no AnimationEventRelay!");
        }
    }

    // Função chamada pela animação de INÍCIO do golpe do Guerreiro
    public void EnableHitboxEvent()
    {
        if (meleeHitbox != null)
        {
            meleeHitbox.EnableHitbox();
        }
        else
        {
            Debug.LogError("MeleeHitbox não está atribuído no AnimationEventRelay para o evento de EnableHitbox!");
        }
    }

    // Função chamada pela animação de FIM do golpe do Guerreiro
    public void DisableHitboxEvent()
    {
        if (meleeHitbox != null)
        {
            meleeHitbox.DisableHitbox();
        }
        else
        {
            Debug.LogError("MeleeHitbox não está atribuído no AnimationEventRelay para o evento de DisableHitbox!");
        }
    }
}