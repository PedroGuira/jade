// Models/Estabelecimento.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jade.Models // Seu namespace
{
    public class Estabelecimento
    {
        public int Id { get; set; }

        [StringLength(300)]
        public string? LogoUrl { get; set; }

        [StringLength(300)]
        public string? CapaUrl { get; set; } // Imagem de capa do 

        [StringLength(20)]
        public string? TelefoneWhatsapp { get; set; }

        [StringLength(20)]
        public string? TelefoneFixo { get; set; } // Opcional

        public int? MarcaFranquiaId { get; set; }
        [ForeignKey("MarcaFranquiaId")]
        public virtual MarcaFranquia? MarcaFranquia { get; set; }

        public virtual ICollection<UsuarioAdmin> UsuariosAdmin { get; set; } = new List<UsuarioAdmin>();
        public virtual ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();
        public virtual ICollection<Produto> Produtos { get; set; } = new List<Produto>();
        public virtual ICollection<GrupoOpcao> GruposOpcao { get; set; } = new List<GrupoOpcao>();

        // --- GARANTIR QUE ESTA COLEÇÃO EXISTA ---
        public virtual ICollection<ItemOpcao> ItensOpcao { get; set; } = new List<ItemOpcao>();

        public virtual ICollection<LojaItemConfig> LojaItemConfiguracoes { get; set; } = new List<LojaItemConfig>();

        [StringLength(150)]
        public string? EnderecoRua { get; set; }
        [StringLength(20)]
        public string? EnderecoNumero { get; set; }
        [StringLength(100)]
        public string? EnderecoBairro { get; set; }
        [StringLength(100)]
        public string? EnderecoCidade { get; set; }
        [StringLength(2)]
        public string? EnderecoEstado { get; set; } // UF
        [StringLength(10)]
        public string? EnderecoCep { get; set; }
        [StringLength(200)]
        public string? EnderecoComplemento { get; set; }
        [StringLength(200)]
        public string? LinkLocalizacaoGoogleMaps { get; set; } // Opcional, para o botão de mapa

        [Column(TypeName = "decimal(18,2)")]
        public decimal PedidoMinimo { get; set; } = 0;

        [StringLength(50)]
        public string? TempoEntregaEstimado { get; set; } // Ex: "30-50 min", "Aprox. 45 min"

        // Horários de Funcionamento (formato flexível, o frontend pode ajudar a formatar)
        // Ex: "Seg-Sex: 08h-12h;14h-18h|Sáb: 09h-13h|Dom: Fechado"
        // Onde '|' separa dias e ';' separa múltiplos períodos no mesmo dia.
        [StringLength(500)]
        public string? HorariosFuncionamentoTexto { get; set; }

        // Informações de Redes Sociais
        [StringLength(150)]
        public string? LinkFacebook { get; set; }
        [StringLength(150)]
        public string? LinkInstagram { get; set; }

        // Promoção / Destaque
        public bool MostrarBannerPromocional { get; set; } = false;
        [StringLength(300)]
        public string? ImagemUrlBannerPromocional { get; set; }
        [StringLength(500)]
        public string? TextoBannerPromocional { get; set; }
        [StringLength(300)]
        public string? LinkUrlBannerPromocional { get; set; } // Para onde o banner leva ao clicar

        // Configurações Adicionais
        public bool PermiteObservacoesNoPedido { get; set; } = true;
        // public string CorPrimariaTema { get; set; } // Futuro: permitir customizar cores
        // public string CorSecundariaTema { get; set; }
    }
}