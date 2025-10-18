using UnityEngine;

public class EnemyAnimationEventRelay : MonoBehaviour
{
    // Agora temos referências para AMBOS os tipos de IA
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private Boss_GuardianAI bossAI; // NOVA REFERÊNCIA

    // Referência para a hitbox, que pode estar em um filho
    [SerializeField] private EnemyMeleeHitbox meleeHitbox; // NOVA REFERÊNCIA

    void Awake()
    {
        // Tenta encontrar as IAs no mesmo objeto se não foram arrastadas
        if (enemyAI == null) enemyAI = GetComponent<EnemyAI>();
        if (bossAI == null) bossAI = GetComponent<Boss_GuardianAI>();

        // Se a hitbox não foi arrastada, tenta encontrá-la nos filhos
        if (meleeHitbox == null) meleeHitbox = GetComponentInChildren<EnemyMeleeHitbox>();
    }

    // A animação chama esta função
    public void EnableHitbox() // O nome deve ser EXATAMENTE este
    {
        // Repassa a chamada para a hitbox diretamente
        if (meleeHitbox != null)
        {
            meleeHitbox.EnableHitbox();
        }
        else
        {
             Debug.LogError($"[EnemyAnimationRelay] Hitbox não encontrada ou configurada no objeto '{gameObject.name}' ou seus filhos!", this);
        }
    }

    // A animação chama esta função
    public void DisableHitbox() // O nome deve ser EXATAMENTE este
    {
         if (meleeHitbox != null)
        {
            meleeHitbox.DisableHitbox();
        }
    }
}