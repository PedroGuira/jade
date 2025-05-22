// Services/IGrupoOpcaoService.cs
using Jade.Dtos;
using Jade.Models; // Para retornar GrupoOpcao em GetGrupoOpcaoInternoAsync
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jade.Services
{
    public interface IGrupoOpcaoService
    {
        // Para AdminLoja (operações em grupos específicos do seu estabelecimento)
        Task<IEnumerable<GrupoOpcaoVerDto>> ObterTodosDeLojaAsync(int estabelecimentoId);
        Task<GrupoOpcaoVerDto?> ObterDeLojaPorIdAsync(int id, int estabelecimentoId);
        Task<GrupoOpcaoVerDto?> CriarParaLojaAsync(GrupoOpcaoCriarDto dto, int estabelecimentoId);
        Task<bool> AtualizarDeLojaAsync(int id, GrupoOpcaoCriarDto dto, int estabelecimentoId);
        Task<bool> DeletarDeLojaAsync(int id, int estabelecimentoId);

        // Para AdminMarcaFranquia (operações em grupos "template" da sua marca)
        Task<IEnumerable<GrupoOpcaoVerDto>> ObterTodosTemplatesDaMarcaAsync(int marcaFranquiaId);
        Task<GrupoOpcaoVerDto?> ObterTemplateDaMarcaPorIdAsync(int grupoOpcaoTemplateId, int marcaFranquiaId);
        Task<GrupoOpcaoVerDto?> CriarTemplateParaMarcaAsync(GrupoOpcaoCriarDto dto, int marcaFranquiaId);
        Task<bool> AtualizarTemplateDaMarcaAsync(int grupoOpcaoTemplateId, GrupoOpcaoCriarDto dto, int marcaFranquiaId);
        Task<bool> DeletarTemplateDaMarcaAsync(int grupoOpcaoTemplateId, int marcaFranquiaId);

        // Para AdminLoja de uma Franquia: lista grupos locais + templates da marca que ele pode usar/ver
        Task<IEnumerable<GrupoOpcaoVerDto>> ObterTodosParaLojaDeFranquiaAsync(int estabelecimentoId);

        // Para SuperAdminSistema (ver tudo, ou com filtros - simplificado por agora)
        Task<IEnumerable<GrupoOpcaoVerDto>> ObterTodosParaSuperAdminAsync();
        Task<GrupoOpcaoVerDto?> ObterPorIdParaSuperAdminAsync(int grupoOpcaoId);

        Task<GrupoOpcaoVerDto?> ObterTemplateDaMarcaDaSuaFranquiaAsync(int grupoOpcaoTemplateId, int lojaEstabelecimentoId);

        // Helper para outros services/controllers validarem acesso e obterem o contexto do grupo
        Task<GrupoOpcao?> GetGrupoOpcaoInternoVerificadoAsync(int grupoOpcaoId, string userRole, int? userEstabelecimentoId, int? userMarcaFranquiaId);

        Task<GrupoOpcaoVerDto?> MapGrupoOpcaoParaClienteAsync(GrupoOpcao grupoOriginal, int lojaEstabelecimentoId, bool apenasItensAtivos);
    }
}