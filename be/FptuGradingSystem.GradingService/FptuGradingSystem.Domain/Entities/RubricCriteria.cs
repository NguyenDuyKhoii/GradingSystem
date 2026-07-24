using System.Collections.Generic;

namespace FptuGradingSystem.Domain.Entities
{
    public class RubricCriteria
    {
        public int Id { get; set; }
        public int RubricId { get; set; }
        public string CriteriaName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal MaxPoints { get; set; }
        public decimal Weight { get; set; } // Weight percentage (e.g., 0.25 for 25%)

        // Navigation properties
        public virtual Rubric? Rubric { get; set; }
        public virtual ICollection<GradeDetail> GradeDetails { get; set; } = new List<GradeDetail>();
    }
}
