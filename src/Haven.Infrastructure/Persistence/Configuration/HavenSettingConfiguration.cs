using Haven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class HavenSettingConfiguration : IEntityTypeConfiguration<HavenSetting>
{
    public void Configure(EntityTypeBuilder<HavenSetting> builder)
    {
        builder.ToTable("haven_settings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Category)
            .HasColumnName("category")
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Value)
            .HasColumnName("value")
            .IsRequired();

        builder.HasIndex(x => x.Category)
            .IsUnique();
    }
}
