// Dtos/ItemOpcaoDto.cs
using System.ComponentModel.DataAnnotations;

namespace Jade.Dtos
{
    public class ItemOpcaoCriarDto // Usado para criar e atualizar
    {
        [Required(ErrorMessage = "O nome do item de opção é obrigatório.")]
        [StringLength(100)]
        public string Nome { get; set; }

        [Range(0, 10000, ErrorMessage = "Preço adicional deve ser um valor válido.")]
        public decimal PrecoAdicional { get; set; } = 0.00m;

        public bool Disponivel { get; set; } = true;

        public int OrdemExibicao { get; set; } = 0;

        // GrupoOpcaoId virá da rota.
        // EstabelecimentoId/MarcaFranquiaId do item serão inferidos do GrupoOpcao pai.
    }

    public class ItemOpcaoVerDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public decimal PrecoAdicional { get; set; }
        public bool Disponivel { get; set; }
        public int OrdemExibicao { get; set; }
        public int GrupoOpcaoId { get; set; } // ID do grupo pai
        public int? EstabelecimentoId { get; set; } // ID do estabelecimento se o grupo pai for de loja
        public int? MarcaFranquiaId { get; set; }   // ID da marca se o grupo pai for template de marca
        public bool IsTemplateDaFranquia { get; set; } // True se o grupo pai for template
    }
}