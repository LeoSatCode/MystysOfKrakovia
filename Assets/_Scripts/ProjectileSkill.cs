using UnityEngine;
using Photon.Pun;

[CreateAssetMenu(fileName = "New Projectile Skill", menuName = "Mists of Krackovia/Skills/Projectile Skill")]
public class ProjectileSkill : Skill
{
    [Header("Configuração de Projétil")]
    public GameObject projectilePrefab;

    [Header("Efeito Sonoro")]
    public AudioClip castSound;

    public override bool Activate(PlayerController owner)
    {
        Transform target = owner.GetCurrentTarget();

        if (target == null)
        {
            owner.RefundResource(this);
            return false;
        }

        if (Vector3.Distance(owner.transform.position, target.position) > range)
        {
            owner.RefundResource(this);
            return false;
        }

        AudioSource ownerAudioSource = owner.GetComponent<AudioSource>();
        if (ownerAudioSource != null && castSound != null)
        {
            ownerAudioSource.PlayOneShot(castSound);
        }

        // --- CORREÇÃO AQUI ---
        // A sua chamada já estava correta, apenas garantimos que a função existe no PlayerController.
        owner.LookAtTargetHorizontally(target);
        
        owner.PlaySkillAnimation(animationTrigger);
        return true;
    }

    // A função LookAtTargetHorizontally que você adicionou aqui deve ser REMOVIDA.

    public override void PerformAction(PlayerController owner)
    {
        if (projectilePrefab != null && owner.GetProjectileSpawnPoint() != null)
        {
            Transform spawnPoint = owner.GetProjectileSpawnPoint();
            GameObject projGO = PhotonNetwork.Instantiate(projectilePrefab.name, spawnPoint.position, owner.transform.rotation);
            projGO.GetComponent<Projectile>()?.Initialize(owner);
        }
    }
}