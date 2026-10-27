using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Mapping;

public sealed class PropostaMapping : IEntityTypeConfiguration<Proposta>
{
    public void Configure(EntityTypeBuilder<Proposta> builder)
    {
        builder.ToTable("propostas");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClienteId).IsRequired();
        builder.HasIndex(x => x.ClienteId).IsUnique();

        builder.Property(x => x.Cpf).HasMaxLength(11).IsRequired();
        builder.Property(x => x.Score).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.QuantidadeCartoes).IsRequired();
        builder.Property(x => x.LimitePorCartao).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.LimiteTotal).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.CriadoEmUtc).IsRequired();
    }
}
