using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Class Skill Reward", menuName = "Mists of Krackovia/Class Skill Reward")]
public class ClassSkillReward : ScriptableObject
{
    // Usamos uma struct para criar um par (chave-valor) visível no Inspector
    [System.Serializable]
    public struct RewardMapping
    {
        public string characterClass;
        public Skill skillReward;
    }

    public List<RewardMapping> rewards;

    // Função que busca a skill correta na lista
    public Skill GetSkillForClass(string className)
    {
        foreach (var mapping in rewards)
        {
            if (mapping.characterClass == className)
            {
                return mapping.skillReward;
            }
        }
        return null; // Retorna nulo se não encontrar
    }
}