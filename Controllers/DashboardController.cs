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

        public async Task<IActionResult> Index(string? project)
        {
            var viewModel = new DashboardViewModel();
            var usandoMock = string.IsNullOrEmpty(_configuration["Jira:ApiToken"]);
            viewModel.UsandoDadosMock = usandoMock;

            try
            {
                // Buscar lista de projetos
                viewModel.Projetos = await _jiraService.BuscarProjetosAsync();

                // Se nenhum projeto selecionado, mostrar tela de seleção
                if (string.IsNullOrEmpty(project))
                {
                    return View("SelectProject", viewModel);
                }

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

            return View(viewModel);
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
    }
}
