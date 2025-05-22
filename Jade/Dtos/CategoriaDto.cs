// Dtos/CategoriaDto.cs
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Jade.Dtos // Seu namespace
{
    public class CategoriaCriarDto // Pode ser usado para criar e atualizar
    {
        [Required(ErrorMessage = "O nome da categoria é obrigatório.")]
        [StringLength(100)]
        public string Nome { get; set; }

        public int OrdemExibicao { get; set; } = 0;

        // REMOVA ou COMENTE a linha abaixo se o controller sempre usa o EstabelecimentoId do token:
        // public int EstabelecimentoId { get; set; } 

        public List<int>? GrupoOpcaoIds { get; set; } = new List<int>();
        public int? TargetMarcaFranquiaId { get; set; } // Para SuperAdmin criar template
        public int? TargetEstabelecimentoId { get; set; }
    }

    // CategoriaVerDto permanece como antes, incluindo EstabelecimentoId e GruposOpcaoAssociados
    public class CategoriaVerDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public int OrdemExibicao { get; set; }
        public int? EstabelecimentoId { get; set; } // <<< MUDADO PARA ANULÁVEL (int?)
        public int? MarcaFranquiaId { get; set; } // Adicionando para clareza no DTO
        public List<GrupoOpcaoVerDto> GruposOpcaoAssociados { get; set; } = new List<GrupoOpcaoVerDto>();
    }
}