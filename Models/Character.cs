using System;

namespace GoonWarfareX.Models;

public enum Element { Fire, Water, Earth, Wind, Lightning, None }
public enum Archetype { Striker, Tank, Tactician }

public class Character
{
    // Identity & Visuals (1-5)
    public Guid ID { get; set; } = Guid.NewGuid();
    public string? Name { get; set; }
    public string? Bio { get; set; }
    public string? SpritePath { get; set; }
    public Element ElementType { get; set; }

    // Vitals (6-9)
    public float HP { get; set; }
    public float MaxHP { get; set; }
    public float Stamina { get; set; }
    public int BaseSpeed { get; set; }

    // Combat Stats (10-14)
    public int AttackPower { get; set; }
    public float DefenseGrade { get; set; } 
    public float Agility { get; set; }
    public int Accuracy { get; set; } 
    public float CriticalHitRate { get; set; }

    // Special Moves (15-16)
    public string? SpecialMoveName { get; set; }
    public float SpecialMoveDamage { get; set; }

    // State Tracking (17-20)
    public bool IsBlocking { get; set; }
    public bool IsStunned { get; set; }
    public int WinCount { get; set; }
    public int LossCount { get; set; }
}