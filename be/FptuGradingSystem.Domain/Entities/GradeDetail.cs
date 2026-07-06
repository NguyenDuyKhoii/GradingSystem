namespace FptuGradingSystem.Domain.Entities
{
    public class GradeDetail
    {
        public int Id { get; set; }
        public int GradeId { get; set; }
        public int RubricCriteriaId { get; set; }
        public decimal Score { get; set; }
        public string Feedback { get; set; } = string.Empty;

        // Navigation properties
        public virtual Grade? Grade { get; set; }
        public virtual RubricCriteria? RubricCriteria { get; set; }
    }
}
