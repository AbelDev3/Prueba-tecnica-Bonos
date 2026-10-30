
using BonusTest.Application.Bonuses;
using BonusTest.Domain.Entities;
using Moq;

namespace BonusManagement.Tests;

public class BonusServiceTests
{
    [Fact]
    public async Task CreateAsync_BonoFijo_CalculaImportesCorrectamente()
    {
        // Arrange: empleado activo con sueldo de S/ 5,000
        var employee = new Employee
        {
            DepartmentCode = "TECH1",
            EmployeeNumber = 101,
            FullName = "Andrea Torres Vega",
            HireDate = new DateTime(2020, 1, 15),
            MonthlySalary = 5000m,
            TaxRate = 8m,
            EmployeeStateCode = "0101"
        };

        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetEmployeeAsync(
                "TECH1",
                101,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        repositoryMock
            .Setup(r => r.IsActiveReasonAsync(
                "000001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        repositoryMock
            .Setup(r => r.ExistsActiveBonusAsync(
                "TECH1",
                101,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        repositoryMock
            .Setup(r => r.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new BonusService(repositoryMock.Object);

        var request = new CreateBonusRequest(
            DepartmentCode: "TECH1",
            EmployeeNumber: 101,
            Period: "2026-07",
            BonusTypeId: 1,
            BonusValue: 1000m,
            ReasonCode: "000001"
        );

        // Act
        var result = await service.CreateAsync(
            request,
            "candidato",
            CancellationToken.None
        );

        // Assert: cálculos financieros
        Assert.Equal(1000m, result.GrossAmount);
        Assert.Equal(80m, result.TaxAmount);
        Assert.Equal(920m, result.NetAmount);

        // Assert: datos del bono enviado al repositorio
        repositoryMock.Verify(r => r.AddAsync(
            It.Is<Bonus>(b =>
                b.DepartmentCode == "TECH1" &&
                b.EmployeeNumber == 101 &&
                b.GrossAmount == 1000m &&
                b.TaxAmount == 80m &&
                b.NetAmount == 920m &&
                b.StateCode == "0201" &&
                b.CreatedBy == "candidato" &&
                !b.IsDeleted
            ),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        repositoryMock.Verify(r => r.SaveChangesAsync(
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    
    [Fact]
    public async Task CreateAsync_BonoPorcentual_CalculaImportesCorrectamente()
    {
        // Arrange
        var employee = new Employee
        {
            DepartmentCode = "TECH1",
            EmployeeNumber = 101,
            FullName = "Andrea Torres Vega",
            HireDate = new DateTime(2020, 1, 15),
            MonthlySalary = 5000m,
            TaxRate = 8m,
            EmployeeStateCode = "0101"
        };

        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetEmployeeAsync(
                "TECH1",
                101,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        repositoryMock
            .Setup(r => r.IsActiveReasonAsync(
                "000001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        repositoryMock
            .Setup(r => r.ExistsActiveBonusAsync(
                "TECH1",
                101,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        repositoryMock
            .Setup(r => r.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new BonusService(repositoryMock.Object);

        var request = new CreateBonusRequest(
            DepartmentCode: "TECH1",
            EmployeeNumber: 101,
            Period: "2026-07",
            BonusTypeId: 2,
            BonusValue: 12.35m,
            ReasonCode: "000001"
        );

        // Act
        var result = await service.CreateAsync(
            request,
            "candidato",
            CancellationToken.None
        );

        // Assert
        Assert.Equal(617.50m, result.GrossAmount);
        Assert.Equal(49.40m, result.TaxAmount);
        Assert.Equal(568.10m, result.NetAmount);

        repositoryMock.Verify(r => r.AddAsync(
            It.Is<Bonus>(b =>
                b.BonusTypeId == 2 &&
                b.BonusValue == 12.35m &&
                b.GrossAmount == 617.50m &&
                b.TaxAmount == 49.40m &&
                b.NetAmount == 568.10m
            ),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        repositoryMock.Verify(r => r.SaveChangesAsync(
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    
    [Theory]
    [InlineData("100.005", "100.01")]
    [InlineData("100.004", "100.00")]
    [InlineData("100.015", "100.02")]
    public async Task CreateAsync_BonoFijo_RedondeaCorrectamente(
        string bonusValue,
        string expectedGross)
    {
        // Arrange
        var employee = new Employee
        {
            DepartmentCode = "TECH1",
            EmployeeNumber = 101,
            FullName = "Andrea Torres Vega",
            HireDate = new DateTime(2020, 1, 15),
            MonthlySalary = 5000m,
            TaxRate = 0m,
            EmployeeStateCode = "0101"
        };

        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetEmployeeAsync(
                "TECH1",
                101,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        repositoryMock
            .Setup(r => r.IsActiveReasonAsync(
                "000001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        repositoryMock
            .Setup(r => r.ExistsActiveBonusAsync(
                "TECH1",
                101,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new BonusService(repositoryMock.Object);

        var request = new CreateBonusRequest(
            "TECH1",
            101,
            "2026-07",
            1,
            decimal.Parse(
                bonusValue,
                System.Globalization.CultureInfo.InvariantCulture),
            "000001"
        );

        // Act
        var result = await service.CreateAsync(
            request,
            "candidato",
            CancellationToken.None
        );

        // Assert
        var expected = decimal.Parse(
            expectedGross,
            System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(expected, result.GrossAmount);
        Assert.Equal(expected, result.NetAmount);
        Assert.Equal(0m, result.TaxAmount);
    }

    
    [Fact]
    public async Task CreateAsync_EmpleadoInactivo_LanzaBusinessException()
    {
        // Arrange
        var employee = new Employee
        {
            DepartmentCode = "VENTA",
            EmployeeNumber = 202,
            FullName = "Rosa Medina Quispe",
            HireDate = new DateTime(2020, 1, 15),
            MonthlySalary = 4000m,
            TaxRate = 8m,
            EmployeeStateCode = "0102"
        };

        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetEmployeeAsync(
                "VENTA",
                202,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var service = new BonusService(repositoryMock.Object);

        var request = new CreateBonusRequest(
            "VENTA",
            202,
            "2026-07",
            1,
            500m,
            "000001"
        );

        // Act
        var exception = await Assert.ThrowsAsync<
            BonusTest.Domain.Exceptions.BusinessException>(
            () => service.CreateAsync(
                request,
                "candidato",
                CancellationToken.None
            )
        );

        // Assert
        Assert.Equal("EMPLOYEE_INACTIVE", exception.Code);

        repositoryMock.Verify(
            r => r.AddAsync(
                It.IsAny<Bonus>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );

        repositoryMock.Verify(
            r => r.SaveChangesAsync(
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
    }

    
    [Fact]
    public async Task CreateAsync_BonoDuplicado_LanzaBusinessException()
    {
        // Arrange
        var employee = new Employee
        {
            DepartmentCode = "TECH1",
            EmployeeNumber = 101,
            FullName = "Andrea Torres Vega",
            HireDate = new DateTime(2020, 1, 15),
            MonthlySalary = 5000m,
            TaxRate = 8m,
            EmployeeStateCode = "0101"
        };

        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetEmployeeAsync(
                "TECH1",
                101,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        repositoryMock
            .Setup(r => r.IsActiveReasonAsync(
                "000001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Simulamos que ya existe un bono activo
        // para este empleado y periodo.
        repositoryMock
            .Setup(r => r.ExistsActiveBonusAsync(
                "TECH1",
                101,
                "2026-07",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new BonusService(repositoryMock.Object);

        var request = new CreateBonusRequest(
            "TECH1",
            101,
            "2026-07",
            1,
            500m,
            "000001"
        );

        // Act
        var exception = await Assert.ThrowsAsync<
            BonusTest.Domain.Exceptions.BusinessException>(
            () => service.CreateAsync(
                request,
                "candidato",
                CancellationToken.None
            )
        );

        // Assert: código de error esperado
        Assert.Equal("DUPLICATE_BONUS", exception.Code);

        // Verificamos que se consultó la existencia del bono.
        repositoryMock.Verify(
            r => r.ExistsActiveBonusAsync(
                "TECH1",
                101,
                "2026-07",
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );

        // No debe intentar registrar ni persistir cambios.
        repositoryMock.Verify(
            r => r.AddAsync(
                It.IsAny<Bonus>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );

        repositoryMock.Verify(
            r => r.SaveChangesAsync(
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
    }


}
