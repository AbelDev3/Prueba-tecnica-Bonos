
using BonusTest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BonusTest.Infrastructure.Persistence.Configurations;

public class BonusProjectDistributionConfiguration
    : IEntityTypeConfiguration<BonusProjectDistribution>
{
    public void Configure(
        EntityTypeBuilder<BonusProjectDistribution> builder)
    {
        builder.ToTable("BonusProjectDistribution", "dbo");

        builder.HasKey(x => x.BonusProjectDistributionId);

        builder.Property(x => x.BonusProjectDistributionId)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.ProjectName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Hours)
            .HasPrecision(16, 2);

        builder.Property(x => x.DistributedAmount)
            .HasPrecision(16, 2);

        builder.HasOne(x => x.Bonus)
            .WithMany()
            .HasForeignKey(x => x.BonusId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new
        {
            x.BonusId,
            x.EmployeeProjectHoursId
        }).IsUnique();
    }
}
