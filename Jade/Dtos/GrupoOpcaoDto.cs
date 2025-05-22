// Dtos/GrupoOpcaoDto.cs
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Jade.Dtos
{
    public class GrupoOpcaoCriarDto // Usado para criar e atualizar templates de marca ou grupos de loja
    {
        [Required(ErrorMessage = "O nome do grupo é obrigatório.")]
        [StringLength(100)]
        public string Nome { get; set; }

        [StringLength(250)]
        public string? Descricao { get; set; }

        [Required(ErrorMessage = "Mínimo de seleções é obrigatório.")]
        [Range(0, 100, ErrorMessage = "Mínimo de seleções deve ser entre 0 e 100.")]
        public int MinSelecoes { get; set; } = 0;

        [Required(ErrorMessage = "Máximo de seleções é obrigatório.")]
        [Range(1, 100, ErrorMessage = "Máximo de seleções deve ser entre 1 e 100.")]
        public int MaxSelecoes { get; set; } = 1;

        public int OrdemExibicao { get; set; } = 0;

        // Para AdminMarcaFranquia, ao criar template, o controller preencherá MarcaFranquiaId
        // Para AdminLoja, ao criar grupo local, o controller preencherá EstabelecimentoId
        // O DTO em si não precisa carregá-los do frontend para ESTE DTO de criação.
    }

    public class GrupoOpcaoVerDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string? Descricao { get; set; }
        public int MinSelecoes { get; set; }
        public int MaxSelecoes { get; set; }
        public int OrdemExibicao { get; set; }
        public int? EstabelecimentoId { get; set; } // ID da loja se for um grupo específico da loja
        public int? MarcaFranquiaId { get; set; }   // ID da marca se for um template da franquia
        public bool IsTemplateDaFranquia { get; set; } // True se MarcaFranquiaId != null E EstabelecimentoId == null
        public int QuantidadeItens { get; set; }
        // public List<ItemOpcaoVerDto> Itens { get; set; } = new List<ItemOpcaoVerDto>(); // Descomente se quiser carregar itens junto
    }
}