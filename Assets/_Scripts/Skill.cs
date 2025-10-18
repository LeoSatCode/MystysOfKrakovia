using UnityEngine;

public enum ResourceType { None, Mana, Rage, Energy }

public abstract class Skill : ScriptableObject
{
    [Header("Informações Básicas")]
    public int id;
    public string skillName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;

    [Header("Ação e Custo")]
    public float cooldown;
    public ResourceType resourceType;
    public int resourceCost;
    public string animationTrigger;
    public float range = 15f;

    // A Ativação apenas inicia o processo
    public abstract bool Activate(PlayerController owner);

    // NOVO: O Efeito real da habilidade, a ser chamado pela animação
    public virtual void PerformAction(PlayerController owner)
    {
        // A implementação padrão não faz nada. As classes filhas (como ProjectileSkill)
        // vão sobrescrever este método com a lógica real.
    }
}