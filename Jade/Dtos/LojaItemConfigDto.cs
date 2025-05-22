// Dtos/LojaItemConfigDto.cs
using Jade.Models; // Para o enum TipoItemConfiguravel
using System.ComponentModel.DataAnnotations;

namespace Jade.Dtos // Seu namespace
{
    // DTO para criar ou atualizar uma configuração de item de franquia para uma loja
    public class LojaItemConfigCriarAtualizarDto
    {
        [Required]
        public TipoItemConfiguravel TipoItem { get; set; }

        [Required]
        public int ItemOriginalId { get; set; } // ID do item template da franquia

        // Campos que a loja pode configurar
        public decimal? PrecoLocal { get; set; }
        public bool? DisponivelLocalmente { get; set; }
        public bool AtivoNaLoja { get; set; } = true; // Por padrão, ao configurar, ativa na loja
        public string? NomeLocal { get; set; }
        public int? OrdemExibicaoLocal { get; set; }

        // EstabelecimentoId virá do token do AdminLoja
    }

    // DTO para visualizar as configurações de uma loja
    public class LojaItemConfigVerDto
    {
        public int Id { get; set; } // Id da própria configuração
        public int EstabelecimentoId { get; set; }
        public TipoItemConfiguravel TipoItem { get; set; }
        public int ItemOriginalId { get; set; }

        // Dados do item original (template da franquia) para referência
        public string NomeOriginal { get; set; } // Ex: "Pizza Mussarela (Template)"
        public decimal? PrecoOriginalSugerido { get; set; } // Ex: Preço do ProdutoTemplate

        // Configurações locais
        public decimal? PrecoLocal { get; set; }
        public bool? DisponivelLocalmente { get; set; }
        public bool AtivoNaLoja { get; set; }
        public string? NomeLocal { get; set; }
        public int? OrdemExibicaoLocal { get; set; }
    }
}