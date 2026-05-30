using Haven.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("user_permissions");
        builder.HasKey(x => new { x.UserId, x.Name });
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("permission_name").HasMaxLength(128).IsRequired();
    }
}
