
using BonusTest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BonusTest.Infrastructure.Persistence.Configurations;

public class BonusConfiguration : IEntityTypeConfiguration<Bonus>
{
    public void Configure(EntityTypeBuilder<Bonus> builder)
    {
        builder.ToTable("Bonus");

        builder.HasKey(x => x.BonusId);

        builder.Property(x => x.BonusId)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.DepartmentCode)
            .HasColumnType("char(5)")
            .IsRequired();

        builder.Property(x => x.Period)
            .HasColumnType("char(7)")
            .IsRequired();

        builder.Property(x => x.ReasonCode)
            .HasColumnType("char(6)")
            .IsRequired();

        builder.Property(x => x.StateCode)
            .HasColumnType("char(4)")
            .HasDefaultValue("0201")
            .IsRequired();

        builder.Property(x => x.BonusValue)
            .HasPrecision(16, 2);

        builder.Property(x => x.GrossAmount)
            .HasPrecision(16, 2);

        builder.Property(x => x.TaxAmount)
            .HasPrecision(16, 2);

        builder.Property(x => x.NetAmount)
            .HasPrecision(16, 2);

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(x => x.ModifiedBy)
            .HasMaxLength(100)
            .IsUnicode(false);

        builder.Property(x => x.ModifiedAt)
            .HasColumnType("datetime2(3)");

        // Relación con clave compuesta de Employee
        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => new
            {
                x.DepartmentCode,
                x.EmployeeNumber
            })
            .HasPrincipalKey(x => new
            {
                x.DepartmentCode,
                x.EmployeeNumber
            })
            .OnDelete(DeleteBehavior.NoAction);

        // Índice único filtrado definido en SQL Server
        builder.HasIndex(x => new
            {
                x.DepartmentCode,
                x.EmployeeNumber,
                x.Period
            })
            .IsUnique()
            .HasDatabaseName("UX_Bonus_ActiveEmployeePeriod")
            .HasFilter("[IsDeleted] = 0 AND [StateCode] <> '0203'");

        builder.HasIndex(x => x.BonusId)
            .HasDatabaseName("IX_Bonus_List")
            .HasFilter("[IsDeleted] = 0");
    }
}
