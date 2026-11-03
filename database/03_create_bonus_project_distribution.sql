USE BonusTest;
GO

-- Detalle de distribución del importe bruto de un bono pagado.
-- Ejecutar después de 02_create_bonus.sql.
CREATE TABLE dbo.BonusProjectDistribution (
    BonusProjectDistributionId INT IDENTITY(1,1) NOT NULL,
    BonusId INT NOT NULL,
    EmployeeProjectHoursId INT NOT NULL,
    ProjectName NVARCHAR(200) NOT NULL,
    Hours DECIMAL(16,2) NOT NULL,
    DistributedAmount DECIMAL(16,2) NOT NULL,
    CONSTRAINT PK_BonusProjectDistribution PRIMARY KEY (BonusProjectDistributionId),
    CONSTRAINT FK_BonusProjectDistribution_Bonus FOREIGN KEY (BonusId)
        REFERENCES dbo.Bonus (BonusId),
    CONSTRAINT CK_BonusProjectDistribution_Hours CHECK (Hours > 0),
    CONSTRAINT CK_BonusProjectDistribution_Amount CHECK (DistributedAmount >= 0),
    CONSTRAINT UQ_BonusProjectDistribution_Bonus_ProjectHours
        UNIQUE (BonusId, EmployeeProjectHoursId)
);
GO

CREATE INDEX IX_BonusProjectDistribution_BonusId
ON dbo.BonusProjectDistribution (BonusId);
GO
