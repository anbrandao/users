using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using users_api.Domain.Entities;

namespace users_api.Infra.Ef
{
    public class UserConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuario");
            builder.Property(e => e.Nome).HasMaxLength(100).IsRequired();
            builder.Property(e => e.CreatedDate).HasColumnType("datetime2").IsRequired();
            builder.Property(e => e.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        }
    }
}
