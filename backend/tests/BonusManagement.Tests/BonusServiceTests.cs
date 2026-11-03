
using BonusTest.Application.Bonuses;
using BonusTest.Domain.Entities;
using Moq;
using BonusTest.Domain.Exceptions;

namespace BonusManagement.Tests;

public class BonusServiceTests
{

    private static Employee CrearEmpleadoActivo(
        decimal sueldo = 5000m,
        decimal tasaImpuesto = 8m)
    {
        return new Employee
        {
            DepartmentCode = "TECH1",
            EmployeeNumber = 101,
            FullName = "Andrea Torres Vega",
            HireDate = new DateTime(2020, 1, 15),
            MonthlySalary = sueldo,
            TaxRate = tasaImpuesto,
            EmployeeStateCode = "0101"
        };
    }


    private static Mock<IBonusRepository> CrearRepositorioParaRegistro(
        Employee employee)
    {
        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetEmployeeAsync(
                employee.DepartmentCode,
                employee.EmployeeNumber,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        repositoryMock
            .Setup(r => r.IsActiveReasonAsync(
                "000001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        repositoryMock
            .Setup(r => r.ExistsActiveBonusAsync(
                employee.DepartmentCode,
                employee.EmployeeNumber,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        repositoryMock
            .Setup(r => r.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return repositoryMock;
    }



    [Fact]
    public async Task CreateAsync_BonoFijo_CalculaImportesCorrectamente()
    {
        // Arrange: empleado activo con sueldo de S/ 5,000
        var employee = CrearEmpleadoActivo();

        var repositoryMock = CrearRepositorioParaRegistro(employee);

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
        var employee = CrearEmpleadoActivo();

        var repositoryMock = CrearRepositorioParaRegistro(employee);

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
        var employee = CrearEmpleadoActivo(tasaImpuesto: 0m);

        var repositoryMock = CrearRepositorioParaRegistro(employee);

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
        var employee = CrearEmpleadoActivo();
        employee.DepartmentCode = "VENTA";
        employee.EmployeeNumber = 202;
        employee.FullName = "Rosa Medina Quispe";
        employee.MonthlySalary = 4000m;
        employee.EmployeeStateCode = "0102";

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
        var employee = CrearEmpleadoActivo();


        var repositoryMock = CrearRepositorioParaRegistro(employee);

        // Sobrescribimos únicamente el comportamiento
        // que necesita esta prueba.
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


    [Fact]
    public async Task CancelAsync_BonoRegistrado_CancelaYRegistraAuditoria()
    {
        // Arrange
        var bonus = new Bonus
        {
            BonusId = 10,
            DepartmentCode = "TECH1",
            EmployeeNumber = 101,
            Period = "2026-07",
            BonusTypeId = 1,
            BonusValue = 500m,
            ReasonCode = "000001",
            GrossAmount = 500m,
            TaxAmount = 40m,
            NetAmount = 460m,
            StateCode = "0201",
            IsDeleted = false,
            CreatedBy = "candidato",
            CreatedAt = DateTime.UtcNow
        };

        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetByIdAsync(
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bonus);

        repositoryMock
            .Setup(r => r.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new BonusService(repositoryMock.Object);

        // Act
        await service.CancelAsync(
            10,
            "abel.alvarado",
            CancellationToken.None
        );

        // Assert
        Assert.Equal("0203", bonus.StateCode);
        Assert.Equal("abel.alvarado", bonus.ModifiedBy);
        Assert.NotNull(bonus.ModifiedAt);
        Assert.False(bonus.IsDeleted);

        repositoryMock.Verify(
            r => r.SaveChangesAsync(
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }


    [Fact]
    public async Task DeleteAsync_BonoRegistrado_RealizaEliminacionLogica()
    {
        // Arrange
        var bonus = new Bonus
        {
            BonusId = 11,
            DepartmentCode = "TECH1",
            EmployeeNumber = 101,
            Period = "2026-07",
            BonusTypeId = 1,
            BonusValue = 500m,
            ReasonCode = "000001",
            GrossAmount = 500m,
            TaxAmount = 40m,
            NetAmount = 460m,
            StateCode = "0201",
            IsDeleted = false,
            CreatedBy = "candidato",
            CreatedAt = DateTime.UtcNow
        };

        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetByIdAsync(
                11,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bonus);

        repositoryMock
            .Setup(r => r.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new BonusService(repositoryMock.Object);

        // Act
        await service.DeleteAsync(
            11,
            "abel.alvarado",
            CancellationToken.None
        );

        // Assert: eliminación lógica
        Assert.True(bonus.IsDeleted);

        // El estado original se conserva.
        Assert.Equal("0201", bonus.StateCode);

        // Auditoría
        Assert.Equal("abel.alvarado", bonus.ModifiedBy);
        Assert.NotNull(bonus.ModifiedAt);

        // No se crea un nuevo registro.
        repositoryMock.Verify(
            r => r.AddAsync(
                It.IsAny<Bonus>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );

        // Se persisten los cambios una sola vez.
        repositoryMock.Verify(
            r => r.SaveChangesAsync(
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }


    [Fact]
    public async Task CancelAsync_BonoYaCancelado_LanzaBusinessException()
    {
        // Arrange
        var bonus = new Bonus
        {
            BonusId = 12,
            DepartmentCode = "TECH1",
            EmployeeNumber = 101,
            Period = "2026-07",
            BonusTypeId = 1,
            BonusValue = 500m,
            ReasonCode = "000001",
            GrossAmount = 500m,
            TaxAmount = 40m,
            NetAmount = 460m,
            StateCode = "0203",
            IsDeleted = false,
            CreatedBy = "candidato",
            CreatedAt = DateTime.UtcNow
        };

        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetByIdAsync(
                12,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bonus);

        var service = new BonusService(repositoryMock.Object);

        // Act
        var exception = await Assert.ThrowsAsync<
            BonusTest.Domain.Exceptions.BusinessException>(
            () => service.CancelAsync(
                12,
                "abel.alvarado",
                CancellationToken.None
            )
        );

        // Assert
        Assert.Equal("INVALID_BONUS_STATE", exception.Code);

        // El estado no debe modificarse.
        Assert.Equal("0203", bonus.StateCode);

        // No debe registrar cambios.
        repositoryMock.Verify(
            r => r.SaveChangesAsync(
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
    }


    [Fact]
    public async Task DeleteAsync_BonoCancelado_LanzaBusinessException()
    {
        // Arrange
        var bonus = new Bonus
        {
            BonusId = 13,
            DepartmentCode = "TECH1",
            EmployeeNumber = 101,
            Period = "2026-07",
            BonusTypeId = 1,
            BonusValue = 500m,
            ReasonCode = "000001",
            GrossAmount = 500m,
            TaxAmount = 40m,
            NetAmount = 460m,
            StateCode = "0203",
            IsDeleted = false,
            CreatedBy = "candidato",
            CreatedAt = DateTime.UtcNow
        };

        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetByIdAsync(
                13,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bonus);

        var service = new BonusService(repositoryMock.Object);

        // Act
        var exception = await Assert.ThrowsAsync<
            BonusTest.Domain.Exceptions.BusinessException>(
            () => service.DeleteAsync(
                13,
                "abel.alvarado",
                CancellationToken.None
            )
        );

        // Assert: operación rechazada
        Assert.Equal("INVALID_BONUS_STATE", exception.Code);

        // El bono conserva su estado.
        Assert.Equal("0203", bonus.StateCode);

        // No debe marcarse como eliminado.
        Assert.False(bonus.IsDeleted);

        // No debe modificar los datos de auditoría.
        Assert.Null(bonus.ModifiedBy);
        Assert.Null(bonus.ModifiedAt);

        // No debe persistir cambios.
        repositoryMock.Verify(
            r => r.SaveChangesAsync(
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
    }


    [Fact]
    public async Task CreateAsync_PeriodoFuturo_LanzaBusinessException()
    {
        // Arrange
        var repositoryMock = new Mock<IBonusRepository>();
        var service = new BonusService(repositoryMock.Object);

        // Siempre seleccionamos el mes siguiente.
        var futurePeriod = DateTime.Today
            .AddMonths(1)
            .ToString("yyyy-MM");

        var request = new CreateBonusRequest(
            DepartmentCode: "TECH1",
            EmployeeNumber: 101,
            Period: futurePeriod,
            BonusTypeId: 1,
            BonusValue: 500m,
            ReasonCode: "000001"
        );

        // Act
        var exception = await Assert.ThrowsAsync<
            BonusTest.Domain.Exceptions.BusinessException>(
            () => service.CreateAsync(
                request,
                "abel.alvarado",
                CancellationToken.None
            )
        );

        // Assert
        Assert.Equal("FUTURE_PERIOD", exception.Code);

        // No debe consultar empleados.
        repositoryMock.Verify(
            r => r.GetEmployeeAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );

        // No debe registrar bonos.
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
    public async Task CreateAsync_BonoFijoSuperaSueldo_LanzaBusinessException()
    {
        // Arrange
        var employee = CrearEmpleadoActivo();

        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetEmployeeAsync(
                "TECH1",
                101,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var service = new BonusService(repositoryMock.Object);

        var request = new CreateBonusRequest(
            DepartmentCode: "TECH1",
            EmployeeNumber: 101,
            Period: DateTime.Today.ToString("yyyy-MM"),
            BonusTypeId: 1,
            BonusValue: 6000m,
            ReasonCode: "000001"
        );

        // Act
        var exception = await Assert.ThrowsAsync<
            BonusTest.Domain.Exceptions.BusinessException>(
            () => service.CreateAsync(
                request,
                "abel.alvarado",
                CancellationToken.None
            )
        );

        // Assert
        Assert.Equal("BONUS_EXCEEDS_SALARY", exception.Code);

        // No debe consultar motivos ni registrar bonos.
        repositoryMock.Verify(
            r => r.IsActiveReasonAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );

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
    public async Task CreateAsync_PorcentajeMayorA100_LanzaBusinessException()
    {
        // Arrange
        var employee = CrearEmpleadoActivo();

        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetEmployeeAsync(
                "TECH1",
                101,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var service = new BonusService(repositoryMock.Object);

        var request = new CreateBonusRequest(
            DepartmentCode: "TECH1",
            EmployeeNumber: 101,
            Period: DateTime.Today.ToString("yyyy-MM"),
            BonusTypeId: 2,
            BonusValue: 120m,
            ReasonCode: "000001"
        );

        // Act
        var exception = await Assert.ThrowsAsync<
            BonusTest.Domain.Exceptions.BusinessException>(
            () => service.CreateAsync(
                request,
                "abel.alvarado",
                CancellationToken.None
            )
        );

        // Assert
        Assert.Equal("INVALID_PERCENTAGE", exception.Code);

        // La operación debe detenerse antes de validar motivos.
        repositoryMock.Verify(
            r => r.IsActiveReasonAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );

        // No debe registrar el bono.
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


    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_UsuarioInvalido_LanzaBusinessException(
        string? userCode)
    {
        // Arrange
        var repositoryMock = new Mock<IBonusRepository>();
        var service = new BonusService(repositoryMock.Object);

        var request = new CreateBonusRequest(
            DepartmentCode: "TECH1",
            EmployeeNumber: 101,
            Period: DateTime.Today.ToString("yyyy-MM"),
            BonusTypeId: 1,
            BonusValue: 500m,
            ReasonCode: "000001"
        );

        // Act
        var exception = await Assert.ThrowsAsync<
            BonusTest.Domain.Exceptions.BusinessException>(
            () => service.CreateAsync(
                request,
                userCode!,
                CancellationToken.None
            )
        );

        // Assert
        Assert.Equal("USER_REQUIRED", exception.Code);

        // No debe consultar información del empleado.
        repositoryMock.Verify(
            r => r.GetEmployeeAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );

        // No debe intentar registrar el bono.
        repositoryMock.Verify(
            r => r.AddAsync(
                It.IsAny<Bonus>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );

        // No debe persistir cambios.
        repositoryMock.Verify(
            r => r.SaveChangesAsync(
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
    }


    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BonoEliminado_NoPermiteCancelarNiEliminar(
        bool cancelar)
    {
        // Arrange
        const int bonusId = 20;

        var repositoryMock = new Mock<IBonusRepository>();

        // Simula el filtro !IsDeleted del repositorio:
        // un bono eliminado no se devuelve.
        repositoryMock
            .Setup(r => r.GetByIdAsync(
                bonusId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bonus?)null);

        var service = new BonusService(repositoryMock.Object);

        // Act
        var exception = await Assert.ThrowsAsync<
            BonusTest.Domain.Exceptions.BusinessException>(
            async () =>
            {
                if (cancelar)
                {
                    await service.CancelAsync(
                        bonusId,
                        "abel.alvarado",
                        CancellationToken.None);
                }
                else
                {
                    await service.DeleteAsync(
                        bonusId,
                        "abel.alvarado",
                        CancellationToken.None);
                }
            });

        // Assert
        Assert.Equal("BONUS_NOT_FOUND", exception.Code);

        repositoryMock.Verify(
            r => r.GetByIdAsync(
                bonusId,
                It.IsAny<CancellationToken>()),
            Times.Once);

        repositoryMock.Verify(
            r => r.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PayAsync_DosProyectos_DistribuyeBrutoCorrectamente()
    {
        // Arrange
        var bonus = new Bonus
        {
            BonusId = 20,
            DepartmentCode = "TECH1",
            EmployeeNumber = 101,
            GrossAmount = 617.50m,
            StateCode = "0201",
            CreatedBy = "candidato",
            CreatedAt = DateTime.UtcNow
        };

        var projects = new List<EmployeeProjectHours>
        {
            new()
            {
                EmployeeProjectHoursId = 1,
                DepartmentCode = "TECH1",
                EmployeeNumber = 101,
                ProjectName = "Portal de clientes",
                Hours = 120m
            },
            new()
            {
                EmployeeProjectHoursId = 2,
                DepartmentCode = "TECH1",
                EmployeeNumber = 101,
                ProjectName = "App móvil",
                Hours = 40m
            }
        };

        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetByIdAsync(
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bonus);

        repositoryMock
            .Setup(r => r.GetEmployeeProjectHoursAsync(
                "TECH1",
                101,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var service = new BonusService(repositoryMock.Object);

        // Act
        await service.PayAsync(
            20,
            "abel.alvarado",
            CancellationToken.None);

        // Assert
        repositoryMock.Verify(
            r => r.PayBonusAsync(
                It.Is<Bonus>(b =>
                    b.BonusId == 20 &&
                    b.ModifiedBy == "abel.alvarado" &&
                    b.ModifiedAt != null),
                It.Is<IReadOnlyCollection<BonusProjectDistribution>>(d =>
                    d.Count == 2 &&
                    d.Sum(x => x.DistributedAmount) == 617.50m &&
                    d.Any(x =>
                        x.ProjectName == "Portal de clientes" &&
                        x.DistributedAmount == 463.13m) &&
                    d.Any(x =>
                        x.ProjectName == "App móvil" &&
                        x.DistributedAmount == 154.37m)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }


    [Fact]
    public async Task PayAsync_TresProyectos_AjustaRedondeoEnUltimoProyecto()
    {
        // Arrange
        var bonus = new Bonus
        {
            BonusId = 21,
            DepartmentCode = "TECH1",
            EmployeeNumber = 101,
            GrossAmount = 100m,
            StateCode = "0201",
            CreatedBy = "candidato",
            CreatedAt = DateTime.UtcNow
        };

        var projects = new List<EmployeeProjectHours>
        {
            new()
            {
                EmployeeProjectHoursId = 1,
                DepartmentCode = "TECH1",
                EmployeeNumber = 101,
                ProjectName = "Proyecto A",
                Hours = 1m
            },
            new()
            {
                EmployeeProjectHoursId = 2,
                DepartmentCode = "TECH1",
                EmployeeNumber = 101,
                ProjectName = "Proyecto B",
                Hours = 1m
            },
            new()
            {
                EmployeeProjectHoursId = 3,
                DepartmentCode = "TECH1",
                EmployeeNumber = 101,
                ProjectName = "Proyecto C",
                Hours = 1m
            }
        };

        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetByIdAsync(
                21,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bonus);

        repositoryMock
            .Setup(r => r.GetEmployeeProjectHoursAsync(
                "TECH1",
                101,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var service = new BonusService(repositoryMock.Object);

        // Act
        await service.PayAsync(
            21,
            "abel.alvarado",
            CancellationToken.None);

        // Assert
        repositoryMock.Verify(
            r => r.PayBonusAsync(
                It.IsAny<Bonus>(),
                It.Is<IReadOnlyCollection<BonusProjectDistribution>>(d =>
                    d.Count == 3 &&
                    d.Sum(x => x.DistributedAmount) == 100m &&
                    d.Any(x =>
                        x.ProjectName == "Proyecto A" &&
                        x.DistributedAmount == 33.33m) &&
                    d.Any(x =>
                        x.ProjectName == "Proyecto B" &&
                        x.DistributedAmount == 33.33m) &&
                    d.Any(x =>
                        x.ProjectName == "Proyecto C" &&
                        x.DistributedAmount == 33.34m)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }


    [Fact]
    public async Task PayAsync_UnSoloProyecto_AsignaTodoElBruto()
    {
        // Arrange
        var bonus = new Bonus
        {
            BonusId = 22,
            DepartmentCode = "TECH1",
            EmployeeNumber = 101,
            GrossAmount = 617.50m,
            StateCode = "0201",
            CreatedBy = "candidato",
            CreatedAt = DateTime.UtcNow
        };

        var projects = new List<EmployeeProjectHours>
        {
            new()
            {
                EmployeeProjectHoursId = 1,
                DepartmentCode = "TECH1",
                EmployeeNumber = 101,
                ProjectName = "Proyecto único",
                Hours = 80m
            }
        };

        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetByIdAsync(
                22,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bonus);

        repositoryMock
            .Setup(r => r.GetEmployeeProjectHoursAsync(
                "TECH1",
                101,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var service = new BonusService(repositoryMock.Object);

        // Act
        await service.PayAsync(
            22,
            "abel.alvarado",
            CancellationToken.None);

        // Assert
        repositoryMock.Verify(
            r => r.PayBonusAsync(
                It.IsAny<Bonus>(),
                It.Is<IReadOnlyCollection<BonusProjectDistribution>>(d =>
                    d.Count == 1 &&
                    d.Single().ProjectName == "Proyecto único" &&
                    d.Single().DistributedAmount == 617.50m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }


    [Fact]
    public async Task PayAsync_SinProyectos_LanzaErrorYNoPaga()
    {
        // Arrange
        var bonus = new Bonus
        {
            BonusId = 23,
            DepartmentCode = "TECH1",
            EmployeeNumber = 101,
            GrossAmount = 617.50m,
            StateCode = "0201",
            CreatedBy = "candidato",
            CreatedAt = DateTime.UtcNow
        };

        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetByIdAsync(
                23,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bonus);

        repositoryMock
            .Setup(r => r.GetEmployeeProjectHoursAsync(
                "TECH1",
                101,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmployeeProjectHours>());

        var service = new BonusService(repositoryMock.Object);

        // Act
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.PayAsync(
                23,
                "abel.alvarado",
                CancellationToken.None));

        // Assert
        Assert.Equal("PROJECTS_NOT_FOUND", exception.Code);

        repositoryMock.Verify(
            r => r.PayBonusAsync(
                It.IsAny<Bonus>(),
                It.IsAny<IReadOnlyCollection<BonusProjectDistribution>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }


    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task PayAsync_HorasInvalidas_LanzaErrorYNoPaga(
        int invalidHours)
    {
        // Arrange
        var bonus = new Bonus
        {
            BonusId = 24,
            DepartmentCode = "TECH1",
            EmployeeNumber = 101,
            GrossAmount = 617.50m,
            StateCode = "0201",
            CreatedBy = "candidato",
            CreatedAt = DateTime.UtcNow
        };

        var projects = new List<EmployeeProjectHours>
        {
            new()
            {
                EmployeeProjectHoursId = 1,
                DepartmentCode = "TECH1",
                EmployeeNumber = 101,
                ProjectName = "Proyecto A",
                Hours = 120m
            },
            new()
            {
                EmployeeProjectHoursId = 2,
                DepartmentCode = "TECH1",
                EmployeeNumber = 101,
                ProjectName = "Proyecto B",
                Hours = invalidHours
            }
        };

        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetByIdAsync(
                24,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bonus);

        repositoryMock
            .Setup(r => r.GetEmployeeProjectHoursAsync(
                "TECH1",
                101,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var service = new BonusService(repositoryMock.Object);

        // Act
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.PayAsync(
                24,
                "abel.alvarado",
                CancellationToken.None));

        // Assert
        Assert.Equal("INVALID_PROJECT_HOURS", exception.Code);

        repositoryMock.Verify(
            r => r.PayBonusAsync(
                It.IsAny<Bonus>(),
                It.IsAny<IReadOnlyCollection<BonusProjectDistribution>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }


    [Fact]
    public async Task PayAsync_ImportePequeno_NoGeneraDistribucionesNegativas()
    {
        // Arrange
        var bonus = new Bonus
        {
            BonusId = 25,
            DepartmentCode = "TECH1",
            EmployeeNumber = 101,
            GrossAmount = 0.03m,
            StateCode = "0201",
            CreatedBy = "candidato",
            CreatedAt = DateTime.UtcNow
        };

        var projects = Enumerable.Range(1, 5)
            .Select(i => new EmployeeProjectHours
            {
                EmployeeProjectHoursId = i,
                DepartmentCode = "TECH1",
                EmployeeNumber = 101,
                ProjectName = $"Proyecto {i}",
                Hours = 1m
            })
            .ToList();

        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetByIdAsync(
                25,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bonus);

        repositoryMock
            .Setup(r => r.GetEmployeeProjectHoursAsync(
                "TECH1",
                101,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        var service = new BonusService(repositoryMock.Object);

        // Act
        await service.PayAsync(
            25,
            "abel.alvarado",
            CancellationToken.None);

        // Assert
        repositoryMock.Verify(
            r => r.PayBonusAsync(
                It.IsAny<Bonus>(),
                It.Is<IReadOnlyCollection<BonusProjectDistribution>>(d =>
                    d.Count == 5 &&
                    d.All(x => x.DistributedAmount >= 0m) &&
                    d.Sum(x => x.DistributedAmount) == 0.03m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }


    [Fact]
    public async Task PayAsync_BonoPagado_LanzaErrorYNoPaga()
    {
        // Arrange
        var bonus = new Bonus
        {
            BonusId = 26,
            DepartmentCode = "TECH1",
            EmployeeNumber = 101,
            GrossAmount = 617.50m,
            StateCode = "0202",
            CreatedBy = "candidato",
            CreatedAt = DateTime.UtcNow
        };

        var repositoryMock = new Mock<IBonusRepository>();

        repositoryMock
            .Setup(r => r.GetByIdAsync(
                26,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(bonus);

        var service = new BonusService(repositoryMock.Object);

        // Act
        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.PayAsync(
                26,
                "abel.alvarado",
                CancellationToken.None));

        // Assert
        Assert.Equal("INVALID_BONUS_STATE", exception.Code);

        repositoryMock.Verify(
            r => r.GetEmployeeProjectHoursAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        repositoryMock.Verify(
            r => r.PayBonusAsync(
                It.IsAny<Bonus>(),
                It.IsAny<IReadOnlyCollection<BonusProjectDistribution>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }


}
