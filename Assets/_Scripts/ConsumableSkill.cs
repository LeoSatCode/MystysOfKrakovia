using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable Skill", menuName = "Mists of Krackovia/Skills/Consumable Skill")]
public class ConsumableSkill : Skill
{
    [Header("Configuração de Consumível")]
    [Tooltip("O item que esta habilidade vai tentar consumir do inventário.")]
    public ItemData itemToConsume;

    // O Activate agora retorna 'true' se teve sucesso e 'false' se falhou.
    public override bool Activate(PlayerController owner)
    {
        if (itemToConsume == null)
        {
            Debug.LogError($"A habilidade '{skillName}' não tem um item para consumir configurado!");
            return false; // Falhou
        }

        // Tenta consumir o item do inventário
        if (InventoryManager.Instance.ConsumeItem(itemToConsume))
        {
            // Se o item foi consumido com sucesso, executa o efeito do item
            Debug.Log($"Item '{itemToConsume.itemName}' consumido. Ativando efeito.");
            itemToConsume.Use(owner);

            if (!string.IsNullOrEmpty(animationTrigger))
            {
                owner.PlaySkillAnimation(animationTrigger);
            }
            return true; // Sucesso!
        }
        else
        {
            Debug.Log($"Item '{itemToConsume.itemName}' não encontrado no inventário.");
            // Opcional: Adicionar um som de "falha" ou mensagem na tela.
            return false; // Falhou
        }
    }
}