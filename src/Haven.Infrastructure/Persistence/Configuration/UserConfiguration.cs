using Haven.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Haven.Infrastructure.Persistence.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();
        
        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(User.MaxNameLength)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .IsRequired();
        
        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired();
        
        builder.Property(x => x.RequirePasswordChange)
            .HasColumnName("require_password_change")
            .IsRequired();

        builder.HasMany(x => x.Permissions)
            .WithOne()
            .HasForeignKey(p => p.UserId);
    }
}