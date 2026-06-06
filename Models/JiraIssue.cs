namespace Projeto5_Valcan.Models
{
    public class JiraIssue
    {
        public string Key { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Assignee { get; set; } = "Não atribuído";
        public string Priority { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public string? ParentKey { get; set; }
        public string? ParentSummary { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? Updated { get; set; }

        public int DiasRestantes => DueDate.HasValue 
            ? (DueDate.Value.Date - DateTime.Today).Days 
            : int.MaxValue;

        public bool IsOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.Today;

        public string BadgeUrgencia => DiasRestantes switch
        {
            0 => "🔴 Vence hoje",
            1 => "🟠 Amanhã",
            <= 3 => "🟡 Em breve",
            _ => "🟢 5 dias"
        };

        public string BadgeClass => DiasRestantes switch
        {
            0 => "bg-danger",
            1 => "bg-warning text-dark",
            <= 3 => "bg-warning text-dark",
            _ => "bg-success"
        };
    }
}
