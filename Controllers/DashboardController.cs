using Microsoft.AspNetCore.Mvc;
using Projeto5_Valcan.Models;
using Projeto5_Valcan.Services;

namespace Projeto5_Valcan.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IJiraService _jiraService;
        private readonly ILogger<DashboardController> _logger;
        private readonly IConfiguration _configuration;

        public DashboardController(IJiraService jiraService, ILogger<DashboardController> logger, IConfiguration configuration)
        {
            _jiraService = jiraService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new GlobalDashboardViewModel();
            vm.UsandoDadosMock = string.IsNullOrEmpty(_configuration["Jira:ApiToken"]);

            try
            {
                var projetos = await _jiraService.BuscarProjetosAsync();

                var tasks = projetos.Select(async p =>
                {
                    var summary = new ProjectSummary { Key = p.Key, Name = p.Name };
                    try
                    {
                        var epicsTask = _jiraService.BuscarEpicsAsync(p.Key);
                        var urgTask = _jiraService.BuscarTarefasUrgentesAsync(p.Key);
                        await Task.WhenAll(epicsTask, urgTask);
                        summary.Issues = await epicsTask;
                        summary.Urgentes = await urgTask;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao buscar dados do projeto {Key}", p.Key);
                    }
                    return summary;
                });

                vm.ProjectSummaries = (await Task.WhenAll(tasks)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar dashboard global");
                vm.ErrorMessage = $"Erro ao buscar dados: {ex.Message}";
            }

            return View(vm);
        }

        public async Task<IActionResult> Projects()
        {
            var viewModel = new DashboardViewModel();
            viewModel.UsandoDadosMock = string.IsNullOrEmpty(_configuration["Jira:ApiToken"]);

            try
            {
                viewModel.Projetos = await _jiraService.BuscarProjetosAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar projetos");
                viewModel.ErrorMessage = $"Erro ao buscar dados: {ex.Message}";
            }

            return View("SelectProject", viewModel);
        }

        public async Task<IActionResult> Project(string project)
        {
            var viewModel = new DashboardViewModel();
            viewModel.UsandoDadosMock = string.IsNullOrEmpty(_configuration["Jira:ApiToken"]);

            try
            {
                viewModel.Projetos = await _jiraService.BuscarProjetosAsync();
                viewModel.ProjetoSelecionado = project;
                viewModel.ProjetoNome = viewModel.Projetos.FirstOrDefault(p => p.Key == project)?.Name ?? project;

                var epicsTask = _jiraService.BuscarEpicsAsync(project);
                var urgentesTask = _jiraService.BuscarTarefasUrgentesAsync(project);
                await Task.WhenAll(epicsTask, urgentesTask);

                viewModel.Epics = await epicsTask;
                viewModel.TarefasUrgentes = await urgentesTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar dados do dashboard");
                viewModel.ErrorMessage = $"Erro ao buscar dados: {ex.Message}";
            }

            return View("ProjectDashboard", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Preview(string key)
        {
            try
            {
                var detail = await _jiraService.BuscarDetalhesIssueAsync(key);
                if (detail == null)
                    return Content("<div class='alert-dark-info'>Issue não encontrada.</div>");
                return PartialView("_IssuePreview", detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar preview da issue {Key}", key);
                return Content($"<div style='padding:10px;color:var(--text-muted);font-size:0.8rem;'>Erro ao carregar preview</div>");
            }
        }

        [HttpGet]
        public async Task<IActionResult> RefreshEpics(string project)
        {
            try
            {
                var epics = await _jiraService.BuscarEpicsAsync(project);
                return PartialView("_EpicsTable", epics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar epics");
                return Content($"<div class='alert-dark-danger'>Erro: {ex.Message}</div>");
            }
        }

        [HttpGet]
        public async Task<IActionResult> RefreshUrgentes(string project)
        {
            try
            {
                var urgentes = await _jiraService.BuscarTarefasUrgentesAsync(project);
                return PartialView("_TarefasUrgentes", urgentes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar tarefas urgentes");
                return Content($"<div class='alert-dark-danger'>Erro: {ex.Message}</div>");
            }
        }

        public async Task<IActionResult> Issue(string key, string? project)
        {
            try
            {
                var detail = await _jiraService.BuscarDetalhesIssueAsync(key);
                if (detail == null)
                    return RedirectToAction("Index", new { project });

                ViewBag.Transitions = await _jiraService.BuscarTransicoesAsync(key);
                ViewBag.Project = project ?? detail.ProjectKey;
                return View("IssueDetail", detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar detalhes da issue {Key}", key);
                TempData["Error"] = $"Erro ao buscar detalhes: {ex.Message}";
                return RedirectToAction("Index", new { project });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(string issueKey, string project, string commentBody)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(commentBody))
                    await _jiraService.AdicionarComentarioAsync(issueKey, commentBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar comentário");
                TempData["Error"] = $"Erro ao adicionar comentário: {ex.Message}";
            }
            return RedirectToAction("Issue", new { key = issueKey, project });
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(string issueKey, string project, string transitionId)
        {
            try
            {
                await _jiraService.AtualizarStatusAsync(issueKey, transitionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao alterar status");
                TempData["Error"] = $"Erro ao alterar status: {ex.Message}";
            }
            return RedirectToAction("Issue", new { key = issueKey, project });
        }
    }
}
