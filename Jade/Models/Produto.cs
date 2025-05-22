// Models/Produto.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jade.Models // Use o seu namespace correto
{
    public class Produto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Nome { get; set; }

        [StringLength(500)]
        public string? Descricao { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Preco { get; set; }

        [StringLength(300)]
        public string? ImagemUrl { get; set; } // Já estava como nullable

        public bool Disponivel { get; set; } = true;

        public int? CategoriaId { get; set; }
        [ForeignKey("CategoriaId")]
        public virtual Categoria? Categoria { get; set; } // Categoria pode ser nula para um produto

        public int? EstabelecimentoId { get; set; } // <<< DEVE SER ANULÁVEL (int?)
        [ForeignKey("EstabelecimentoId")]
        public virtual Estabelecimento? Estabelecimento { get; set; } // <<< DEVE SER ANULÁVEL

        public int? MarcaFranquiaId { get; set; } // Já deve estar anulável
        [ForeignKey("MarcaFranquiaId")]
        public virtual MarcaFranquia? MarcaFranquiaOriginadora { get; set; }

        // PROPRIEDADES REMOVIDAS:
        // public bool IsPersonalizavel { get; set; } = false;
        // public int MaxSabores { get; set; } = 0;
        // public int MaxAcompanhamentos { get; set; } = 0;

        // Futuramente, se precisar de personalizações específicas de produto:
        // public virtual ICollection<ProdutoGrupoOpcao> GruposDeOpcaoEspecificosDoProduto { get; set; } = new List<ProdutoGrupoOpcao>();
    }
}