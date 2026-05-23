namespace Projeto5_Valcan.Models
{
    public class JiraIssueDetail
    {
        public string Key { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusCategory { get; set; } = string.Empty;
        public string Assignee { get; set; } = "Unassigned";
        public string Priority { get; set; } = string.Empty;
        public string IssueType { get; set; } = string.Empty;
        public string? ParentKey { get; set; }
        public string? ParentSummary { get; set; }
        public string? Sprint { get; set; }
        public string? Reporter { get; set; }
        public string? Labels { get; set; }
        public string? Team { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }
        public int? StoryPoints { get; set; }
        public List<JiraSubtask> Subtasks { get; set; } = new();
        public List<JiraComment> Comments { get; set; } = new();
        public string ProjectKey { get; set; } = string.Empty;

        public bool IsDueDateOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.Today;
        public int? DiasRestantes => DueDate.HasValue ? (DueDate.Value.Date - DateTime.Today).Days : null;
    }

    public class JiraSubtask
    {
        public string Key { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsDone => Status.ToLower() is "done" or "concluído" or "closed";
    }

    public class JiraComment
    {
        public string Author { get; set; } = string.Empty;
        public string AuthorInitials { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime Created { get; set; }
    }
}
