// Models/LojaItemConfig.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jade.Models // Seu namespace
{
    public enum TipoItemConfiguravel
    {
        Produto = 1,
        Categoria = 2, // Se categorias da franquia puderem ser desativadas/renomeadas localmente
        ItemOpcao = 3 // Para preços adicionais locais de itens de opção da franquia
        // GrupoOpcao geralmente é herdado via Categoria, mas poderia ser configurável aqui também
    }

    public class LojaItemConfig
    {
        public int Id { get; set; } // PK

        [Required]
        public int EstabelecimentoId { get; set; } // A loja que está fazendo a configuração
        [ForeignKey("EstabelecimentoId")]
        public virtual Estabelecimento Estabelecimento { get; set; }

        [Required]
        public TipoItemConfiguravel TipoItem { get; set; }

        [Required]
        public int ItemOriginalId { get; set; } // ID do ProdutoTemplate, CategoriaTemplate ou ItemOpcaoTemplate

        // Campos de sobrescrita (anuláveis, se não sobrescritos, usa o valor do template)
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrecoLocal { get; set; }

        public bool? DisponivelLocalmente { get; set; } // true/false para override, null para usar o padrão do template

        public bool AtivoNaLoja { get; set; } = true; // Se a loja quer usar este item da franquia

        [StringLength(150)]
        public string? NomeLocal { get; set; } // Se a loja pode renomear localmente (para Produto, Categoria, ItemOpcao)

        // Adicionar mais campos de sobrescrita conforme necessário
    }
}