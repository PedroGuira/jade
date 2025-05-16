using System.ComponentModel.DataAnnotations;

namespace Jade.Dtos
{
    public class ProdutoCriarDto
    {
        [Required]
        [StringLength(150)]
        public string Nome { get; set; }

        [StringLength(500)]
        public string? Descricao { get; set; } // Nullable

        [Required]
        [Range(0.01, 1000000.00, ErrorMessage = "O preço deve ser maior que zero.")]
        public decimal Preco { get; set; }

        [StringLength(300)]
        public string? ImagemUrl { get; set; } // Nullable

        public bool Disponivel { get; set; } = true;
        public int? CategoriaId { get; set; } // Nullable

        [Required(ErrorMessage = "O ID do estabelecimento é obrigatório.")]
        public int EstabelecimentoId { get; set; } // Você precisará saber qual estabelecimento está criando

        public bool IsPersonalizavel { get; set; } = false;
        public int MaxSabores { get; set; } = 0;
        public int MaxAcompanhamentos { get; set; } = 0;
    }

    public class ProdutoVerDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string? Descricao { get; set; }
        public decimal Preco { get; set; }
        public string? ImagemUrl { get; set; }
        public bool Disponivel { get; set; }
        public int? CategoriaId { get; set; }
        public string? CategoriaNome { get; set; }
        public int? EstabelecimentoId { get; set; } // Se for de loja, este é o ID da loja. Se for template, pode ser null.
        public int? MarcaFranquiaId { get; set; }   // Se for template, este é o ID da marca. Se for de loja, pode ser null ou o ID da marca da loja.
        public bool IsTemplateDaFranquia { get; set; } = false; // Adicionado para clareza
                                                                // As opções de personalização virão da categoria associada
    }
}
