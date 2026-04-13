using System;

namespace GoonWarfareX.Models
{
    public class Stage
    {
        // Identity (1-3)
        public Guid ID { get; set; } = Guid.NewGuid();
        public string? Name { get; set; }
        public string? Description { get; set; }

        // Assets (4-5)
        public string? BackgroundImagePath { get; set; }
        public string? MusicPath { get; set; }

        // Stage Mechanics (6-10) - This is what makes it "Exceptional"
        public Element DominantElement { get; set; } // e.g., Lightning buff in the Lab
        public float DamageMultiplier { get; set; } = 1.0f;
        public bool HasGravity { get; set; } = true;
        public int MaxHazards { get; set; }
        public string? HazardType { get; set; }
    }
}