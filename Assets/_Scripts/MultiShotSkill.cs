using UnityEngine;
using Photon.Pun;

[CreateAssetMenu(fileName = "New Multi-Shot Skill", menuName = "Mists of Krackovia/Skills/Multi-Shot Skill")]
public class MultiShotSkill : ProjectileSkill // Herda de ProjectileSkill!
{
    [Header("Configuração de Multi-Shot")]
    [Tooltip("O ângulo em graus entre os projéteis.")]
    public float spreadAngle = 15f;

    // Nós sobrescrevemos a função PerformAction para mudar o comportamento
    public override void PerformAction(PlayerController owner)
    {
        if (projectilePrefab == null || owner.GetProjectileSpawnPoint() == null) return;

        Transform spawnPoint = owner.GetProjectileSpawnPoint();
        Quaternion centerRotation = owner.transform.rotation;

        // Projétil Central
        GameObject projCenter = PhotonNetwork.Instantiate(projectilePrefab.name, spawnPoint.position, centerRotation);
        projCenter.GetComponent<Projectile>()?.Initialize(owner);

        // Projétil da Esquerda
        Quaternion leftRotation = centerRotation * Quaternion.Euler(0, -spreadAngle, 0);
        GameObject projLeft = PhotonNetwork.Instantiate(projectilePrefab.name, spawnPoint.position, leftRotation);
        projLeft.GetComponent<Projectile>()?.Initialize(owner);

        // Projétil da Direita
        Quaternion rightRotation = centerRotation * Quaternion.Euler(0, spreadAngle, 0);
        GameObject projRight = PhotonNetwork.Instantiate(projectilePrefab.name, spawnPoint.position, rightRotation);
        projRight.GetComponent<Projectile>()?.Initialize(owner);
    }
}