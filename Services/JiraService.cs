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
        Task<JiraIssueDetail?> BuscarDetalhesIssueAsync(string issueKey);
        Task AdicionarComentarioAsync(string issueKey, string body);
        Task AtualizarStatusAsync(string issueKey, string transitionName);
        Task<List<JiraTransition>> BuscarTransicoesAsync(string issueKey);
    }

    public class JiraTransition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
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

        public async Task<JiraIssueDetail?> BuscarDetalhesIssueAsync(string issueKey)
        {
            try
            {
                var url = $"{_settings.BaseUrl}/rest/api/3/issue/{issueKey}?fields=summary,description,status,assignee,priority,issuetype,parent,reporter,labels,duedate,created,updated,subtasks,comment,sprint,story_points,customfield_10016,customfield_10020";
                _logger.LogInformation("Buscando detalhes: {Url}", url);

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Jira retornou {StatusCode}: {Body}", response.StatusCode, errorBody);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var fields = root.GetProperty("fields");

                var detail = new JiraIssueDetail
                {
                    Key = root.GetProperty("key").GetString() ?? "",
                    Summary = GetString(fields, "summary") ?? "Sem nome",
                    Status = GetNested(fields, "status", "name") ?? "—",
                    StatusCategory = GetNested(fields, "status", "statusCategory", "name") ?? "",
                    Assignee = GetNested(fields, "assignee", "displayName") ?? "Unassigned",
                    Priority = GetNested(fields, "priority", "name") ?? "—",
                    IssueType = GetNested(fields, "issuetype", "name") ?? "—",
                    Reporter = GetNested(fields, "reporter", "displayName"),
                    ParentKey = GetNested(fields, "parent", "key"),
                    ParentSummary = GetNested(fields, "parent", "fields", "summary"),
                    DueDate = GetDate(fields, "duedate"),
                    Created = GetDate(fields, "created"),
                    Updated = GetDate(fields, "updated"),
                    ProjectKey = issueKey.Split('-')[0]
                };

                // Description (ADF format - extract text)
                if (fields.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.Object)
                {
                    detail.Description = ExtractAdfText(desc);
                }

                // Labels
                if (fields.TryGetProperty("labels", out var labels) && labels.ValueKind == JsonValueKind.Array)
                {
                    var labelList = new List<string>();
                    foreach (var l in labels.EnumerateArray())
                        labelList.Add(l.GetString() ?? "");
                    detail.Labels = labelList.Any() ? string.Join(", ", labelList) : "None";
                }
                else
                {
                    detail.Labels = "None";
                }

                // Sprint (customfield_10020)
                if (fields.TryGetProperty("customfield_10020", out var sprints) && sprints.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in sprints.EnumerateArray())
                    {
                        if (s.TryGetProperty("name", out var sName))
                        {
                            detail.Sprint = sName.GetString();
                            break;
                        }
                    }
                }

                // Story Points (customfield_10016)
                if (fields.TryGetProperty("customfield_10016", out var sp) && sp.ValueKind == JsonValueKind.Number)
                {
                    detail.StoryPoints = sp.GetInt32();
                }

                // Subtasks
                if (fields.TryGetProperty("subtasks", out var subtasks) && subtasks.ValueKind == JsonValueKind.Array)
                {
                    foreach (var st in subtasks.EnumerateArray())
                    {
                        detail.Subtasks.Add(new JiraSubtask
                        {
                            Key = st.GetProperty("key").GetString() ?? "",
                            Summary = GetNested(st, "fields", "summary") ?? "",
                            Status = GetNested(st, "fields", "status", "name") ?? ""
                        });
                    }
                }

                // Comments
                if (fields.TryGetProperty("comment", out var comment) && comment.TryGetProperty("comments", out var comments))
                {
                    foreach (var c in comments.EnumerateArray())
                    {
                        var authorName = GetNested(c, "author", "displayName") ?? "Unknown";
                        var initials = string.Concat(authorName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => w[0]));
                        var bodyText = "";
                        if (c.TryGetProperty("body", out var cBody))
                            bodyText = ExtractAdfText(cBody);

                        detail.Comments.Add(new JiraComment
                        {
                            Author = authorName,
                            AuthorInitials = initials.ToUpper(),
                            Body = bodyText,
                            Created = DateTime.TryParse(c.GetProperty("created").GetString(), out var cd) ? cd : DateTime.MinValue
                        });
                    }
                }

                return detail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar detalhes da issue {Key}", issueKey);
                throw;
            }
        }

        public async Task AdicionarComentarioAsync(string issueKey, string body)
        {
            var url = $"{_settings.BaseUrl}/rest/api/3/issue/{issueKey}/comment";
            var payload = new
            {
                body = new
                {
                    type = "doc",
                    version = 1,
                    content = new[]
                    {
                        new
                        {
                            type = "paragraph",
                            content = new[]
                            {
                                new { type = "text", text = body }
                            }
                        }
                    }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Erro ao adicionar comentário: {Error}", error);
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task<List<JiraTransition>> BuscarTransicoesAsync(string issueKey)
        {
            var url = $"{_settings.BaseUrl}/rest/api/3/issue/{issueKey}/transitions";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var transitions = new List<JiraTransition>();

            foreach (var t in doc.RootElement.GetProperty("transitions").EnumerateArray())
            {
                transitions.Add(new JiraTransition
                {
                    Id = t.GetProperty("id").GetString() ?? "",
                    Name = t.GetProperty("name").GetString() ?? ""
                });
            }
            return transitions;
        }

        public async Task AtualizarStatusAsync(string issueKey, string transitionId)
        {
            var url = $"{_settings.BaseUrl}/rest/api/3/issue/{issueKey}/transitions";
            var payload = new { transition = new { id = transitionId } };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Erro ao atualizar status: {Error}", error);
                response.EnsureSuccessStatusCode();
            }
        }

        // Helpers
        private static string? GetString(JsonElement el, string prop)
        {
            return el.TryGetProperty(prop, out var v) && v.ValueKind != JsonValueKind.Null ? v.GetString() : null;
        }

        private static string? GetNested(JsonElement el, params string[] path)
        {
            var current = el;
            foreach (var p in path)
            {
                if (!current.TryGetProperty(p, out var next) || next.ValueKind == JsonValueKind.Null)
                    return null;
                current = next;
            }
            return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
        }

        private static DateTime? GetDate(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out var v) && v.ValueKind != JsonValueKind.Null)
                return DateTime.TryParse(v.GetString(), out var d) ? d : null;
            return null;
        }

        private static string ExtractAdfText(JsonElement adf)
        {
            var sb = new StringBuilder();
            ExtractText(adf, sb);
            return sb.ToString().Trim();
        }

        private static void ExtractText(JsonElement el, StringBuilder sb)
        {
            if (el.TryGetProperty("text", out var text))
                sb.Append(text.GetString());
            if (el.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in content.EnumerateArray())
                    ExtractText(child, sb);
            }
        }
    }
}
