using System.Collections.Generic;

namespace FptuGradingSystem.Domain.Entities
{
    public class Class
    {
        public int Id { get; set; }
        public string ClassCode { get; set; } = string.Empty; // e.g. SE1801

        // Navigation properties
        public virtual ICollection<ExamClass> ExamClasses { get; set; } = new List<ExamClass>();
    }
}
