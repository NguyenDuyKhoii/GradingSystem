namespace FptuGradingSystem.Domain.Entities
{
    public class GradeAppealDetail
    {
        public int Id { get; set; }
        public int GradeAppealId { get; set; }
        public int RubricCriteriaId { get; set; }
        public decimal Score { get; set; }
        public string Feedback { get; set; } = string.Empty;

        // Navigation properties
        public virtual GradeAppeal? GradeAppeal { get; set; }
        public virtual RubricCriteria? RubricCriteria { get; set; }
    }
}
