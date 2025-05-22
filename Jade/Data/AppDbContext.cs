// Data/AppDbContext.cs
using Jade.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Jade.Data
{
    public class AppDbContext : IdentityDbContext<UsuarioAdmin, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Estabelecimento> Estabelecimentos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<GrupoOpcao> GruposOpcao { get; set; }
        public DbSet<ItemOpcao> ItensOpcao { get; set; }
        public DbSet<CategoriaGrupoOpcao> CategoriasGruposOpcao { get; set; }
        public DbSet<MarcaFranquia> MarcasFranquia { get; set; }
        public DbSet<LojaItemConfig> LojaItemConfiguracoes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // MarcaFranquia
            modelBuilder.Entity<MarcaFranquia>(entity =>
            {
                entity.HasIndex(mf => mf.Nome).IsUnique();
            });

            // Estabelecimento
            modelBuilder.Entity<Estabelecimento>(entity =>
            {
                entity.HasOne(e => e.MarcaFranquia)
                    .WithMany(mf => mf.EstabelecimentosDaMarca)
                    .HasForeignKey(e => e.MarcaFranquiaId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // UsuarioAdmin
            modelBuilder.Entity<UsuarioAdmin>(entity =>
            {
                entity.HasOne(u => u.Estabelecimento)
                    .WithMany(e => e.UsuariosAdmin)
                    .HasForeignKey(u => u.EstabelecimentoId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(u => u.MarcaFranquiaVinculada)
                    .WithMany(mf => mf.AdministradoresDaMarca)
                    .HasForeignKey(u => u.MarcaFranquiaIdVinculada)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Categoria
            modelBuilder.Entity<Categoria>(entity => {
                entity.HasOne(c => c.Estabelecimento)
                    .WithMany(e => e.Categorias)
                    .HasForeignKey(c => c.EstabelecimentoId)
                    .IsRequired(false) // Categoria pode ser template (sem EstId direto) ou de loja
                    .OnDelete(DeleteBehavior.Cascade); // Se EstId não for nulo.

                entity.HasOne(c => c.MarcaFranquiaOriginadora)
                    .WithMany(mf => mf.CategoriasTemplateDaMarca)
                    .HasForeignKey(c => c.MarcaFranquiaId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Produto
            modelBuilder.Entity<Produto>(entity => {
                entity.HasOne(p => p.Estabelecimento)
                    .WithMany(e => e.Produtos)
                    .HasForeignKey(p => p.EstabelecimentoId)
                    .IsRequired(false) // Produto pode ser template ou de loja
                    .OnDelete(DeleteBehavior.NoAction); // Evitar cascade se EstId não for nulo

                entity.HasOne(p => p.Categoria)
                    .WithMany(c => c.Produtos)
                    .HasForeignKey(p => p.CategoriaId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(p => p.ImagemUrl).IsRequired(false);

                entity.HasOne(p => p.MarcaFranquiaOriginadora)
                      .WithMany(mf => mf.ProdutosTemplateDaMarca) // Certifique-se que esta coleção existe em MarcaFranquia.cs
                      .HasForeignKey(p => p.MarcaFranquiaId)
                      .IsRequired(false) // Anulável, pois um produto pode ser de loja e não template de franquia
                                         // .OnDelete(DeleteBehavior.Cascade); // <<< PROBLEMA AQUI
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // GrupoOpcao
            modelBuilder.Entity<GrupoOpcao>(entity => {
                entity.HasOne(go => go.Estabelecimento)
                    .WithMany(e => e.GruposOpcao)
                    .HasForeignKey(go => go.EstabelecimentoId)
                    .IsRequired(false) // GrupoOpcao pode ser template ou de loja
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(go => go.MarcaFranquiaOriginadora)
                    .WithMany(mf => mf.GruposOpcaoTemplateDaMarca)
                    .HasForeignKey(go => go.MarcaFranquiaId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // --- AJUSTE AQUI para ItemOpcao ---
            modelBuilder.Entity<ItemOpcao>(entity => {
                entity.HasOne(io => io.GrupoOpcao)
                   .WithMany(go => go.ItensOpcao) // GrupoOpcao.cs deve ter ICollection<ItemOpcao> ItensOpcao
                   .HasForeignKey(io => io.GrupoOpcaoId)
                   .OnDelete(DeleteBehavior.Cascade); // Se deletar um GrupoOpcao, seus itens são deletados.

                entity.HasOne(io => io.Estabelecimento)
                    .WithMany(e => e.ItensOpcao) // Estabelecimento.cs deve ter ICollection<ItemOpcao> ItensOpcao
                    .HasForeignKey(io => io.EstabelecimentoId)
                    .IsRequired(false) // ItemOpcao pode ser template (EstabelecimentoId null) se seu GrupoOpcao for template
                    .OnDelete(DeleteBehavior.NoAction); // Se Estabelecimento for deletado, os ItemOpcao templates não são afetados.
                                                        // ItensOpcao locais seriam afetados pela deleção do GrupoOpcao local (que seria Cascade do Estabelecimento).

                // REMOVER A CONFIGURAÇÃO DE FK PARA MarcaFranquiaOriginadora AQUI
                // entity.HasOne(io => io.MarcaFranquiaOriginadora)
                //    .WithMany() 
                //    .HasForeignKey(io => io.MarcaFranquiaId) // Esta FK foi removida do modelo ItemOpcao
                //    .IsRequired(false)
                //    .OnDelete(DeleteBehavior.Cascade);
            });
            // --- FIM DO AJUSTE ItemOpcao ---

            // CategoriaGrupoOpcao
            modelBuilder.Entity<CategoriaGrupoOpcao>(entity =>
            {
                entity.HasKey(cgo => new { cgo.CategoriaId, cgo.GrupoOpcaoId });
                entity.HasOne(cgo => cgo.Categoria)
                    .WithMany(c => c.GruposDeOpcaoAssociados)
                    .HasForeignKey(cgo => cgo.CategoriaId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(cgo => cgo.GrupoOpcao)
                    .WithMany(go => go.CategoriasAssociadas)
                    .HasForeignKey(cgo => cgo.GrupoOpcaoId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // LojaItemConfig
            modelBuilder.Entity<LojaItemConfig>(entity =>
            {
                entity.HasOne(lic => lic.Estabelecimento)
                    .WithMany(e => e.LojaItemConfiguracoes) // Adicione esta coleção em Estabelecimento.cs
                    .HasForeignKey(lic => lic.EstabelecimentoId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(lic => new { lic.EstabelecimentoId, lic.TipoItem, lic.ItemOriginalId }).IsUnique();
                entity.Property(e => e.TipoItem)
                      .HasConversion<string>()
                      .HasMaxLength(50);
            });
        }
    }
}