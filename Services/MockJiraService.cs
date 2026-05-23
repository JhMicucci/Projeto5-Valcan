using Projeto5_Valcan.Models;

namespace Projeto5_Valcan.Services
{
    /// <summary>
    /// Serviço que retorna dados mock para demonstração quando não há conexão com Jira
    /// </summary>
    public class MockJiraService : IJiraService
    {
        public Task<List<JiraIssue>> BuscarEpicsAsync()
        {
            var epics = new List<JiraIssue>
            {
                new() { Key = "PROJ-101", Summary = "Implementar sistema de autenticação", Status = "Em Progresso", Assignee = "João Silva", Priority = "Alta", IssueType = "Epic", Updated = DateTime.Now.AddHours(-2) },
                new() { Key = "PROJ-102", Summary = "Desenvolver módulo de relatórios", Status = "To Do", Assignee = "Maria Santos", Priority = "Média", IssueType = "Epic", Updated = DateTime.Now.AddHours(-5) },
                new() { Key = "PROJ-103", Summary = "Integração com API externa", Status = "Em Progresso", Assignee = "Pedro Costa", Priority = "Alta", IssueType = "Epic", Updated = DateTime.Now.AddHours(-12) },
                new() { Key = "PROJ-104", Summary = "Melhorias de performance", Status = "Em Revisão", Assignee = "Ana Oliveira", Priority = "Baixa", IssueType = "Epic", Updated = DateTime.Now.AddDays(-1) },
                new() { Key = "PROJ-105", Summary = "Refatoração do banco de dados", Status = "Em Progresso", Assignee = "Carlos Lima", Priority = "Média", IssueType = "Epic", Updated = DateTime.Now.AddDays(-1).AddHours(-6) },
            };

            return Task.FromResult(epics);
        }

        public Task<List<JiraIssue>> BuscarTarefasUrgentesAsync()
        {
            var hoje = DateTime.Today;
            var tarefas = new List<JiraIssue>
            {
                new() { Key = "PROJ-201", Summary = "Corrigir bug crítico no login", Status = "Em Progresso", Assignee = "João Silva", Priority = "Crítica", IssueType = "Bug", DueDate = hoje },
                new() { Key = "PROJ-202", Summary = "Entregar documentação técnica", Status = "To Do", Assignee = "Maria Santos", Priority = "Alta", IssueType = "Task", DueDate = hoje.AddDays(1) },
                new() { Key = "PROJ-203", Summary = "Revisar código do módulo X", Status = "Em Revisão", Assignee = "Pedro Costa", Priority = "Média", IssueType = "Task", DueDate = hoje.AddDays(2) },
                new() { Key = "PROJ-204", Summary = "Finalizar testes unitários", Status = "Em Progresso", Assignee = "Ana Oliveira", Priority = "Alta", IssueType = "Task", DueDate = hoje.AddDays(3) },
                new() { Key = "PROJ-205", Summary = "Preparar ambiente de staging", Status = "To Do", Assignee = "Carlos Lima", Priority = "Média", IssueType = "Task", DueDate = hoje.AddDays(5) },
            };

            return Task.FromResult(tarefas);
        }
    }
}
