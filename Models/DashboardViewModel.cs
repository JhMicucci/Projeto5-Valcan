namespace Projeto5_Valcan.Models
{
    public class DashboardViewModel
    {
        public List<JiraProject> Projetos { get; set; } = new();
        public string? ProjetoSelecionado { get; set; }
        public string? ProjetoNome { get; set; }
        public List<JiraIssue> Epics { get; set; } = new();
        public List<JiraIssue> TarefasUrgentes { get; set; } = new();
        public int TotalEpics => Epics.Count;
        public int TotalUrgentes => TarefasUrgentes.Count;
        public int VencemHojeOuAmanha => TarefasUrgentes.Count(t => t.DiasRestantes <= 1);
        public int VencemEm2a5Dias => TarefasUrgentes.Count(t => t.DiasRestantes >= 2 && t.DiasRestantes <= 5);
        public string? ErrorMessage { get; set; }
        public bool UsandoDadosMock { get; set; }
    }
}
