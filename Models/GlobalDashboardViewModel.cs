namespace Projeto5_Valcan.Models
{
    public class GlobalDashboardViewModel
    {
        public List<ProjectSummary> ProjectSummaries { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public bool UsandoDadosMock { get; set; }

        public int TotalIssues => ProjectSummaries.Sum(p => p.TotalIssues);
        public int TotalDone => ProjectSummaries.Sum(p => p.Done);
        public int TotalInProgress => ProjectSummaries.Sum(p => p.InProgress);
        public int TotalBlockers => ProjectSummaries.Sum(p => p.Blockers);
    }

    public class ProjectSummary
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<JiraIssue> Issues { get; set; } = new();
        public List<JiraIssue> Urgentes { get; set; } = new();

        public int TotalIssues => Issues.Count;
        public int Done => Issues.Count(i => i.Status.ToLower() is "done" or "concluído" or "closed");
        public int InProgress => Issues.Count(i => i.Status.ToLower() is "in progress" or "em progresso");
        public int ToDo => Issues.Count(i => i.Status.ToLower() is "to do" or "a fazer");
        public int Blockers => Urgentes.Count(u => u.DiasRestantes <= 1);

        public int ProgressPercent => TotalIssues > 0 ? (int)((double)Done / TotalIssues * 100) : 0;
    }
}
