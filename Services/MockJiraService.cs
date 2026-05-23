using Projeto5_Valcan.Models;

namespace Projeto5_Valcan.Services
{
    public class MockJiraService : IJiraService
    {
        public Task<List<JiraProject>> BuscarProjetosAsync()
        {
            return Task.FromResult(new List<JiraProject>
            {
                new() { Key = "CT1", Name = "CLIENT-TEST1 (SCR)" },
                new() { Key = "CT2", Name = "CLIENT-TEST2 (KBN)" },
                new() { Key = "CT3", Name = "CLIENT-TEST3 (SCR)" },
            });
        }

        public Task<List<JiraIssue>> BuscarEpicsAsync(string projectKey) =>
            Task.FromResult(new List<JiraIssue>
            {
                new() { Key = $"{projectKey}-101", Summary = "Implementar autenticação", Status = "In Progress", Assignee = "João Silva", Priority = "Alta", IssueType = "Epic", Updated = DateTime.Now.AddHours(-2) },
                new() { Key = $"{projectKey}-102", Summary = "Módulo de relatórios", Status = "To Do", Assignee = "Maria Santos", Priority = "Média", IssueType = "Epic", Updated = DateTime.Now.AddHours(-5) },
            });

        public Task<List<JiraIssue>> BuscarTarefasUrgentesAsync(string projectKey) =>
            Task.FromResult(new List<JiraIssue>
            {
                new() { Key = $"{projectKey}-201", Summary = "Corrigir bug crítico", Status = "In Progress", Assignee = "João Silva", Priority = "Crítica", IssueType = "Bug", DueDate = DateTime.Today },
            });

        public Task<JiraIssueDetail?> BuscarDetalhesIssueAsync(string issueKey) =>
            Task.FromResult<JiraIssueDetail?>(new JiraIssueDetail
            {
                Key = issueKey, Summary = "Task de Exemplo", Description = "Descrição da tarefa de exemplo.", Status = "To Do", StatusCategory = "To Do",
                Assignee = "Unassigned", Priority = "Medium", IssueType = "Task", ParentKey = "CT3-5", ParentSummary = "Project-1",
                Sprint = "Sprint 1", Reporter = "Charles Nascimento", Labels = "None", Team = "None",
                DueDate = DateTime.Today.AddDays(-9), Created = DateTime.Now.AddDays(-73), Updated = DateTime.Now.AddDays(-2),
                ProjectKey = issueKey.Split('-')[0],
                Subtasks = new() { new() { Key = $"{issueKey}-sub1", Summary = "Subtarefa 1", Status = "Done" } },
                Comments = new() { new() { Author = "Charles Nascimento", AuthorInitials = "CN", Body = "Comentário de exemplo", Created = DateTime.Now.AddHours(-5) } }
            });

        public Task AdicionarComentarioAsync(string issueKey, string body) => Task.CompletedTask;
        public Task AtualizarStatusAsync(string issueKey, string transitionId) => Task.CompletedTask;
        public Task<List<JiraTransition>> BuscarTransicoesAsync(string issueKey) =>
            Task.FromResult(new List<JiraTransition>
            {
                new() { Id = "11", Name = "To Do" }, new() { Id = "21", Name = "In Progress" }, new() { Id = "31", Name = "Done" }
            });
    }
}
