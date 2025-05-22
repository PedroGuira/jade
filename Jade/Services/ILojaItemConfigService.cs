// Services/ILojaItemConfigService.cs
using Jade.Dtos;
using Jade.Models; // Para TipoItemConfiguravel
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jade.Services
{
    public interface ILojaItemConfigService
    {
        Task<LojaItemConfigVerDto?> CriarOuAtualizarConfigAsync(LojaItemConfigCriarAtualizarDto dto, int estabelecimentoId);
        Task<LojaItemConfigVerDto?> ObterConfigAsync(int estabelecimentoId, int itemOriginalId, TipoItemConfiguravel tipoItem);
        Task<IEnumerable<LojaItemConfigVerDto>> ObterTodasConfiguracoesDaLojaAsync(int estabelecimentoId, TipoItemConfiguravel? tipoItemFiltro = null);
        Task<bool> DeletarConfigAsync(int configId, int estabelecimentoId); // Ou apenas desativar (setar AtivoNaLoja = false)
    }
}