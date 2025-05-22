// Models/LojaItemConfig.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jade.Models // Seu namespace
{
    public enum TipoItemConfiguravel
    {
        Produto = 1,
        Categoria = 2,
        ItemOpcao = 3,
        GrupoOpcao = 4 // Adicionando GrupoOpcao se ele também pode ter configurações locais
    }

    public class LojaItemConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EstabelecimentoId { get; set; }
        [ForeignKey("EstabelecimentoId")]
        public virtual Estabelecimento Estabelecimento { get; set; }

        [Required]
        public TipoItemConfiguravel TipoItem { get; set; }

        [Required]
        public int ItemOriginalId { get; set; } // ID do CategoriaTemplate, ProdutoTemplate, GrupoOpcaoTemplate ou ItemOpcaoTemplate

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrecoLocal { get; set; } // Para ProdutoTemplate ou ItemOpcaoTemplate

        public bool? DisponivelLocalmente { get; set; } // Para ProdutoTemplate ou ItemOpcaoTemplate

        public bool AtivoNaLoja { get; set; } = false; // Default pode ser false, loja ativa explicitamente

        [StringLength(150)]
        public string? NomeLocal { get; set; } // Para CategoriaTemplate ou ProdutoTemplate

        public int? OrdemExibicaoLocal { get; set; } // Para CategoriaTemplate ou GrupoOpcaoTemplate

        // Adicionar mais campos de sobrescrita conforme necessário
        // Ex: DescricaoLocal, ImagemUrlLocal (para ProdutoTemplate)
    }
}