using System.Collections.Generic;

namespace FptuGradingSystem.Domain.Entities
{
    public class Rubric
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal TotalWeight { get; set; } // Should equal 1.0 (or 100%)

        // Navigation properties
        public virtual Subject? Subject { get; set; }
        public virtual ICollection<RubricCriteria> Criteria { get; set; } = new List<RubricCriteria>();
    }
}
