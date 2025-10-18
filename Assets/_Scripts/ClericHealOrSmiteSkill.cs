using UnityEngine;
using Photon.Pun;

[CreateAssetMenu(fileName = "New Cleric Heal-Smite", menuName = "Mists of Krackovia/Skills/Cleric Heal or Smite")]
public class ClericHealOrSmiteSkill : Skill
{
    [Header("Configuração de Clérigo")]
    public int damageAmount = 40;
    public int healAmount = 50;
    public GameObject holyLightVFXPrefab;

    [Header("Feedback Sonoro")]
    public AudioClip castSound; // Som da conjuração da magia

    public override bool Activate(PlayerController owner)
    {
        // --- LÓGICA DO SOM ---
        AudioSource ownerAudioSource = owner.GetComponent<AudioSource>();
        if (ownerAudioSource != null && castSound != null)
        {
            ownerAudioSource.PlayOneShot(castSound);
        }
        // --- FIM DA LÓGICA DO SOM ---

        // Validação foi movida para o PerformAction para garantir que a animação sempre toque
        owner.PlaySkillAnimation(animationTrigger);
        return true;
    }

    public override void PerformAction(PlayerController owner)
    {
        Transform enemyTarget = owner.GetCurrentTarget();
        Transform friendlyTarget = owner.GetCurrentFriendlyTarget();
        PhotonView ownerView = owner.GetComponent<PhotonView>();
        
        // Prioridade 1: Curar um aliado selecionado
        if (friendlyTarget != null && Vector3.Distance(owner.transform.position, friendlyTarget.position) <= range)
        {
            Health targetHealth = friendlyTarget.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.GetComponent<PhotonView>().RPC("RPC_ApplyHeal", RpcTarget.All, healAmount);
                ownerView.RPC("RPC_SpawnHolyEffect", RpcTarget.All, friendlyTarget.GetComponent<PhotonView>().ViewID);
            }
            return;
        }

        // Prioridade 2: Atacar um inimigo selecionado
        if (enemyTarget != null && Vector3.Distance(owner.transform.position, enemyTarget.position) <= range)
        {
            Health targetHealth = enemyTarget.GetComponent<Health>();
            if (targetHealth != null)
            {
                int totalDamage = damageAmount + owner.intelligence;
                targetHealth.TakeDamage(totalDamage);
                ownerView.RPC("RPC_SpawnHolyEffect", RpcTarget.All, enemyTarget.GetComponent<PhotonView>().ViewID);
            }
            return;
        }

        // Prioridade 3: Se nenhuma das condições acima for atendida, se cura
        ownerView.RPC("RPC_ApplyHeal", RpcTarget.All, healAmount);
        ownerView.RPC("RPC_SpawnHolyEffect", RpcTarget.All, ownerView.ViewID);
    }
}