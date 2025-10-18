using UnityEngine;

[CreateAssetMenu(fileName = "New Melee Skill", menuName = "Mists of Krackovia/Skills/Melee Damage Skill")]
public class MeleeDamageSkill : Skill
{
    [Header("Configuração de Dano Melee")]
    public int baseDamage = 20;

    [Header("Feedback Sonoro")]
    public AudioClip castSound;

    public override bool Activate(PlayerController owner)
    {
        Transform target = owner.GetCurrentTarget();

        if (target == null)
        {
            Debug.Log($"{skillName} requer um alvo inimigo.");
            owner.RefundResource(this);
            return false;
        }

        if (Vector3.Distance(owner.transform.position, target.position) > owner.attackRange)
        {
            Debug.Log("Inimigo fora do alcance para o ataque.");
            owner.RefundResource(this);
            return false;
        }

        AudioSource ownerAudioSource = owner.GetComponent<AudioSource>();
        if (ownerAudioSource != null && castSound != null)
        {
            ownerAudioSource.PlayOneShot(castSound);
        }
        
        // --- CORREÇÃO AQUI ---
        // Chama a função PÚBLICA que criamos no PlayerController
        owner.LookAtTargetHorizontally(target);

        owner.PlaySkillAnimation(animationTrigger);
        return true;
    }

    // A função LookAtTargetHorizontally que você adicionou aqui deve ser REMOVIDA.

    public override void PerformAction(PlayerController owner)
    {
        Transform target = owner.GetCurrentTarget();
        if (target != null)
        {
            Health targetHealth = target.GetComponent<Health>();
            if (targetHealth != null)
            {
                int totalDamage = baseDamage + owner.strength;
                Debug.Log($"{skillName} atingiu {target.name} causando {totalDamage} de dano.");
                targetHealth.TakeDamage(totalDamage);
            }
        }
    }
}