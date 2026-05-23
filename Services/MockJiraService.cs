using Projeto5_Valcan.Models;

namespace Projeto5_Valcan.Services
{
    public class MockJiraService : IJiraService
    {
        public Task<List<JiraProject>> BuscarProjetosAsync()
        {
            var projects = new List<JiraProject>
            {
                new() { Key = "CT1", Name = "CLIENT-TEST1 (SCR)" },
                new() { Key = "CT2", Name = "CLIENT-TEST2 (KBN)" },
                new() { Key = "CT3", Name = "CLIENT-TEST3 (SCR)" },
            };
            return Task.FromResult(projects);
        }

        public Task<List<JiraIssue>> BuscarEpicsAsync(string projectKey)
        {
            var epics = new List<JiraIssue>
            {
                new() { Key = $"{projectKey}-101", Summary = "Implementar sistema de autenticação", Status = "In Progress", Assignee = "João Silva", Priority = "Alta", IssueType = "Epic", Updated = DateTime.Now.AddHours(-2) },
                new() { Key = $"{projectKey}-102", Summary = "Desenvolver módulo de relatórios", Status = "To Do", Assignee = "Maria Santos", Priority = "Média", IssueType = "Epic", Updated = DateTime.Now.AddHours(-5) },
                new() { Key = $"{projectKey}-103", Summary = "Integração com API externa", Status = "In Progress", Assignee = "Pedro Costa", Priority = "Alta", IssueType = "Epic", Updated = DateTime.Now.AddHours(-12) },
            };
            return Task.FromResult(epics);
        }

        public Task<List<JiraIssue>> BuscarTarefasUrgentesAsync(string projectKey)
        {
            var hoje = DateTime.Today;
            var tarefas = new List<JiraIssue>
            {
                new() { Key = $"{projectKey}-201", Summary = "Corrigir bug crítico no login", Status = "In Progress", Assignee = "João Silva", Priority = "Crítica", IssueType = "Bug", DueDate = hoje },
                new() { Key = $"{projectKey}-202", Summary = "Entregar documentação técnica", Status = "To Do", Assignee = "Maria Santos", Priority = "Alta", IssueType = "Task", DueDate = hoje.AddDays(1) },
            };
            return Task.FromResult(tarefas);
        }
    }
}
