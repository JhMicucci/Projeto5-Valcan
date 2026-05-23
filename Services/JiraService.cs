using Microsoft.Extensions.Options;
using Projeto5_Valcan.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Projeto5_Valcan.Services
{
    public interface IJiraService
    {
        Task<List<JiraProject>> BuscarProjetosAsync();
        Task<List<JiraIssue>> BuscarEpicsAsync(string projectKey);
        Task<List<JiraIssue>> BuscarTarefasUrgentesAsync(string projectKey);
    }

    public class JiraService : IJiraService
    {
        private readonly HttpClient _httpClient;
        private readonly JiraSettings _settings;
        private readonly ILogger<JiraService> _logger;

        public JiraService(HttpClient httpClient, IOptions<JiraSettings> settings, ILogger<JiraService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;

            if (!string.IsNullOrEmpty(_settings.Email) && !string.IsNullOrEmpty(_settings.ApiToken))
            {
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.Email}:{_settings.ApiToken}"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            }
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<List<JiraProject>> BuscarProjetosAsync()
        {
            try
            {
                var url = $"{_settings.BaseUrl}/rest/api/3/project";
                _logger.LogInformation("Buscando projetos: {Url}", url);
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Jira retornou {StatusCode}: {Body}", response.StatusCode, errorBody);
                    response.EnsureSuccessStatusCode();
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var projects = new List<JiraProject>();

                foreach (var project in doc.RootElement.EnumerateArray())
                {
                    projects.Add(new JiraProject
                    {
                        Key = project.GetProperty("key").GetString() ?? "",
                        Name = project.GetProperty("name").GetString() ?? "",
                        ProjectTypeKey = project.TryGetProperty("projectTypeKey", out var ptk) ? ptk.GetString() ?? "" : "",
                        AvatarUrl = project.TryGetProperty("avatarUrls", out var avatars) && avatars.TryGetProperty("48x48", out var av)
                            ? av.GetString() ?? "" : ""
                    });
                }

                return projects;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar projetos do Jira");
                throw;
            }
        }

        public async Task<List<JiraIssue>> BuscarEpicsAsync(string projectKey)
        {
            var jql = $"project = {projectKey} ORDER BY updated DESC";
            return await ExecutarBuscaAsync(jql);
        }

        public async Task<List<JiraIssue>> BuscarTarefasUrgentesAsync(string projectKey)
        {
            var jql = $"project = {projectKey} AND statusCategory != Done AND duedate is not EMPTY ORDER BY duedate ASC";
            return await ExecutarBuscaAsync(jql);
        }

        private async Task<List<JiraIssue>> ExecutarBuscaAsync(string jql)
        {
            try
            {
                var url = $"{_settings.BaseUrl}/rest/api/3/search/jql?jql={Uri.EscapeDataString(jql)}&maxResults=50&fields=summary,status,assignee,priority,updated,duedate,issuetype";

                _logger.LogInformation("Buscando Jira: {Url}", url);
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Jira retornou {StatusCode}: {Body}", response.StatusCode, errorBody);
                    response.EnsureSuccessStatusCode();
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var issues = new List<JiraIssue>();
                var issuesArray = root.TryGetProperty("results", out var results) ? results : root.GetProperty("issues");

                foreach (var issue in issuesArray.EnumerateArray())
                {
                    var issueData = issue.TryGetProperty("issue", out var innerIssue) ? innerIssue : issue;
                    var fields = issueData.GetProperty("fields");

                    issues.Add(new JiraIssue
                    {
                        Key = issueData.GetProperty("key").GetString() ?? "",
                        Summary = fields.TryGetProperty("summary", out var summary) ? summary.GetString() ?? "Sem nome" : "Sem nome",
                        Status = fields.TryGetProperty("status", out var status) && status.TryGetProperty("name", out var statusName) 
                            ? statusName.GetString() ?? "—" : "—",
                        Assignee = fields.TryGetProperty("assignee", out var assignee) && assignee.ValueKind != JsonValueKind.Null 
                            && assignee.TryGetProperty("displayName", out var displayName) 
                            ? displayName.GetString() ?? "Não atribuído" : "Não atribuído",
                        Priority = fields.TryGetProperty("priority", out var priority) && priority.ValueKind != JsonValueKind.Null 
                            && priority.TryGetProperty("name", out var priorityName) 
                            ? priorityName.GetString() ?? "—" : "—",
                        IssueType = fields.TryGetProperty("issuetype", out var issueType) && issueType.ValueKind != JsonValueKind.Null 
                            && issueType.TryGetProperty("name", out var typeName) 
                            ? typeName.GetString() ?? "—" : "—",
                        DueDate = fields.TryGetProperty("duedate", out var duedate) && duedate.ValueKind != JsonValueKind.Null 
                            ? DateTime.TryParse(duedate.GetString(), out var dd) ? dd : null : null,
                        Updated = fields.TryGetProperty("updated", out var updated) && updated.ValueKind != JsonValueKind.Null 
                            ? DateTime.TryParse(updated.GetString(), out var ud) ? ud : null : null
                    });
                }

                return issues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar dados do Jira");
                throw;
            }
        }
    }
}
