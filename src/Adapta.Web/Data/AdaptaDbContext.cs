using Adapta.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Adapta.Web.Data;

public sealed class AdaptaDbContext(DbContextOptions<AdaptaDbContext> options)
    : IdentityDbContext<Professor>(options)
{
    public DbSet<Avaliacao> Avaliacoes => Set<Avaliacao>();
    public DbSet<AdaptacaoAvaliacao> Adaptacoes => Set<AdaptacaoAvaliacao>();
    public DbSet<Aluno> Alunos => Set<Aluno>();

    public DbSet<PerfilAdaptacao> PerfisAdaptacao => Set<PerfilAdaptacao>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<HistoricoAdaptacao> HistoricoAdaptacoes => Set<HistoricoAdaptacao>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        var professor = builder.Entity<Professor>();
        professor.Property(p => p.UserName).HasMaxLength(64).IsRequired();
        professor.Property(p => p.NormalizedUserName).HasMaxLength(64).IsRequired();

        var aluno = builder.Entity<Aluno>();
        aluno.HasOne<Professor>().WithMany().HasForeignKey(a => a.ProfessorId).OnDelete(DeleteBehavior.Restrict);
        aluno.HasIndex(a => new { a.ProfessorId, a.Apelido }).IsUnique();
        aluno.HasOne<PerfilAdaptacao>().WithMany().HasForeignKey(a => a.PerfilAdaptacaoId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PerfilAdaptacao>().HasIndex(p => p.Nome).IsUnique();

        var avaliacao = builder.Entity<Avaliacao>();
        avaliacao.HasOne<Professor>().WithMany().HasForeignKey(a => a.ProfessorId).OnDelete(DeleteBehavior.Restrict);
        avaliacao.HasIndex(a => new { a.ProfessorId, a.CriadaEm });

        var documento = builder.Entity<Documento>();
        documento.HasOne(d => d.Avaliacao).WithOne().HasForeignKey<Documento>(d => d.AvaliacaoId);
        documento.HasIndex(d => d.ChaveArquivo).IsUnique();
        documento.ToTable(t => t.HasCheckConstraint("CK_Documentos_Formato", "[Formato] IN (0,1)"));

        var adaptacao = builder.Entity<AdaptacaoAvaliacao>();
        adaptacao.HasOne<Avaliacao>().WithMany().HasForeignKey(a => a.AvaliacaoId);
        adaptacao.HasOne<PerfilAdaptacao>().WithMany().HasForeignKey(a => a.PerfilAdaptacaoId).OnDelete(DeleteBehavior.Restrict);
        adaptacao.ToTable(t => t.HasCheckConstraint("CK_Adaptacoes_Estado", "[Estado] IN (0,1,2,3)"));

        var historico = builder.Entity<HistoricoAdaptacao>();
        historico.HasOne<AdaptacaoAvaliacao>().WithMany().HasForeignKey(h => h.AdaptacaoId);
        historico.HasIndex(h => new { h.AdaptacaoId, h.RegistradoEm });
        historico.ToTable(t => t.HasCheckConstraint("CK_Historico_Estado", "[Estado] IN (0,1,2,3)"));
    }
}
