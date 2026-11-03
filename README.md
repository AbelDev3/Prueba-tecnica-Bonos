# Gestión de bonos por desempeño — Prueba técnica Full Stack

Solución para registrar, consultar, pagar, cancelar y eliminar lógicamente bonos por desempeño de empleados. Incluye API REST en ASP.NET Core, interfaz Angular y scripts SQL Server.

## Tecnologías

- **Backend:** C# / ASP.NET Core **.NET 10**, Entity Framework Core; separación en proyectos `Api`, `Application`, `Domain` e `Infrastructure`.
- **Frontend:** **Angular 22**.
- **Base de datos:** **SQL Server 2019 o superior**.
- **Pruebas automatizadas:** **xUnit** y **Moq**.

## Estructura del repositorio

```text
backend/
  src/
    BonusTest.Api/
    BonusTest.Application/
    BonusTest.Domain/
    BonusTest.Infrastructure/
  tests/
    BonusManagement.Tests/
frontend/
database/
  01_schema_seed.sql
  02_create_bonus.sql
  03_create_bonus_project_distribution.sql
README.md
```

## Funcionalidades implementadas

- Consulta paginada de bonos (10 registros por página) y filtros por área/departamento, empleado, periodo y estado.
- Consulta de empleado y validación de su estado activo.
- Registro de bonos de **importe fijo** o **porcentaje del sueldo mensual**.
- Cálculo en el servidor de importe bruto, retención y neto, con redondeo a dos decimales (`MidpointRounding.AwayFromZero`).
- Prevención de bonos activos duplicados para un mismo empleado y periodo.
- Pago de bonos registrados con distribución del importe bruto entre los proyectos del empleado según las horas trabajadas.
- Registro transaccional de la distribución en `BonusProjectDistribution` y cambio de estado a Pagado (`0202`).
- Cancelación de bonos registrados y eliminación lógica mediante `IsDeleted`.
- Registro de usuario y fecha para auditoría de creación/modificación.
- Catálogos para filtros y formularios; validaciones y mensajes de error de negocio.

## Requisitos previos

1. SDK de **.NET 10**.
2. **Node.js** y **npm** compatibles con la versión de Angular declarada en `frontend/package.json`.
3. Instancia de **SQL Server 2019+** accesible desde el equipo.
4. Git (opcional para ejecutar, necesario para consultar el historial de cambios).

## Preparar la base de datos

En SQL Server Management Studio u otra herramienta compatible:

1. Ejecutar `database/01_schema_seed.sql` para crear/cargar el esquema y los datos iniciales **según lo definido en el propio script**.
2. Ejecutar `database/02_create_bonus.sql` en la misma base de datos para crear los objetos del módulo de bonos.
3. Ejecutar `database/03_create_bonus_project_distribution.sql` en la misma base de datos para crear la tabla de distribución de pagos.
4. Configurar la cadena de conexión del backend para apuntar a esa base de datos.

> Revisar el nombre de base de datos, los permisos y la cadena de conexión antes de ejecutar los scripts. No se incluyen credenciales de un entorno personal en este documento.

## Ejecutar el backend

Desde la raíz del repositorio:

```powershell
cd backend
dotnet restore
dotnet run --project src/BonusTest.Api/BonusTest.Api.csproj
```

La API se ha probado localmente en `http://localhost:5106`. Si el perfil de lanzamiento usa otro puerto, ajustar la URL utilizada por el frontend.

**Configuración:** revisar `backend/src/BonusTest.Api/appsettings.json`, `appsettings.Development.json` y/o las variables de entorno para configurar la conexión SQL Server. No publicar credenciales reales.

## Ejecutar el frontend

En otra terminal, desde la raíz del repositorio:

```powershell
cd frontend
npm install
npm start
```

Acceder a `http://localhost:4200/bonuses` (si el servidor de desarrollo usa el puerto habitual). Verificar que `frontend/src/environments/environment.ts` apunte a la API local; durante el desarrollo se utilizó `http://localhost:5106/api/v1`.

Para compilar la versión de producción:

```powershell
cd frontend
npm run build
```

## API REST

Rutas principales (prefijo `/api/v1`):

| Método | Ruta                                                  | Descripción               |
| ------- | ----------------------------------------------------- | -------------------------- |
| GET     | `/Bonus`                                            | Lista paginada con filtros |
| GET     | `/Bonus/employee/{departmentCode}/{employeeNumber}` | Datos del empleado         |
| POST    | `/Bonus`                                            | Registrar bono             |
| POST    | `/Bonus/{bonusId}/pay`                              | Pagar y distribuir bono (`204 No Content`) |
| PUT     | `/Bonus/{bonusId}/cancel`                           | Cancelar bono              |
| DELETE  | `/Bonus/{bonusId}`                                  | Eliminar lógicamente bono |
| GET     | `/Catalog/departments`                              | Departamentos              |
| GET     | `/Catalog/states/{masterStateCode}`                 | Estados                    |
| GET     | `/Catalog/bonus-types`                              | Tipos de bono              |
| GET     | `/Catalog/bonus-reasons`                            | Motivos                    |

Las operaciones de escritura utilizan la cabecera `UserCode` para auditoría. En el frontend se incorpora mediante un interceptor HTTP. **Este identificador no constituye un mecanismo de autenticación seguro**.

## Reglas de negocio y decisiones técnicas

- Solo se permite registrar bonos para empleados activos (estado `0101`).
- No se admiten periodos futuros; el periodo se expresa como `YYYY-MM`.
- El bono fijo debe ser positivo y no superar el sueldo mensual.
- El bono porcentual debe ser positivo y no superar el 100 %.
- El motivo debe existir y encontrarse activo.
- No se permite más de un bono activo por empleado y periodo.
- El cálculo de bruto, retención y neto se realiza en el **backend** para centralizar la regla de negocio; el frontend muestra un cálculo orientativo durante el registro.
- El pago solo se permite para bonos en estado `0201` (Registrado); al completarse, pasan a `0202` (Pagado).
- La distribución usa las horas de `EmployeeProjectHours`: cada proyecto recibe una parte proporcional del **importe bruto**. Se redondea a dos decimales y el último proyecto recibe el remanente para cuadrar el total; con un solo proyecto, recibe el importe completo.
- No se permite pagar si el empleado no tiene proyectos o existen horas no positivas.
- La actualización condicional de estado y la inserción del detalle se realizan en una única transacción SQL; si falla la inserción, se revierte el pago. La condición de estado protege frente a pagos duplicados concurrentes.
- La cancelación cambia el estado de `0201` (registrado) a `0203` (cancelado).
- La eliminación es **lógica**, conservando el registro para trazabilidad; las búsquedas por identificador excluyen bonos con `IsDeleted = true`.
- Se utiliza separación por capas para aislar dominio, casos de uso, persistencia y exposición HTTP.
- El listado utiliza paginación del lado del servidor para evitar cargar todos los registros en el navegador.

## Pruebas automatizadas

Desde la raíz del repositorio:

```powershell
cd backend
dotnet test tests/BonusManagement.Tests/BonusManagement.Tests.csproj
```

**Último resultado validado durante el desarrollo:** **31 pruebas correctas, 0 fallidas, 0 omitidas** (28 unitarias y 3 de integración). Las pruebas unitarias con Moq verifican reglas de negocio sin SQL Server; las pruebas de integración comprueban conectividad, rollback y concurrencia de pagos en SQL Server.

### Preparar y ejecutar las pruebas de integración

1. Crear una base de datos independiente llamada `BonusTest_Integration` en SQL Server.
2. Ejecutar en esa base los scripts `database/01_schema_seed.sql`, `database/02_create_bonus.sql` y `database/03_create_bonus_project_distribution.sql`, comprobando el contexto de base de datos seleccionado en cada script.
3. Revisar `backend/tests/BonusManagement.Tests/appsettings.Integration.json`. La configuración incluida usa `localhost`, autenticación integrada de Windows y `TrustServerCertificate=True`; adaptarla al entorno local si es necesario, sin publicar credenciales.
4. Ejecutar la suite completa desde `backend`:

```powershell
dotnet test tests/BonusManagement.Tests/BonusManagement.Tests.csproj
```

Para ejecutar solo las pruebas de integración:

```powershell
dotnet test tests/BonusManagement.Tests/BonusManagement.Tests.csproj --filter "Category=Integration"
```

> Las pruebas de integración crean y limpian datos de prueba. Utilizar exclusivamente una base de datos de pruebas, nunca producción. La suite completa requiere que `BonusTest_Integration` esté disponible; no es una suite que funcione totalmente sin SQL Server.

## Alcance y trabajo pendiente

La entrega implementa el flujo solicitado de administración de bonos. Quedan como posibles mejoras posteriores:

- Autenticación y autorización reales, sustituyendo el identificador de auditoría configurable `UserCode` por la identidad del usuario autenticado.
- Pruebas automatizadas de interfaz/end-to-end y ampliación de escenarios de integración con SQL Server.
- Mayor cobertura de casos límite y refactorización adicional de datos de prueba.
- Parametrización de configuración para despliegue en entornos distintos del local y automatización CI/CD.

Estas mejoras **no forman parte de la funcionalidad implementada** en esta entrega.

## Historial de desarrollo

El repositorio conserva commits incrementales para backend, frontend, validaciones, pruebas y refactorizaciones; no se entrega como un único commit final.

## Puntos extra (opcionales)

### Implementado: pruebas unitarias y de integración

Se implementaron pruebas con **xUnit y Moq** en `backend/tests/BonusManagement.Tests`. El resultado validado es de **31 pruebas correctas**: 28 unitarias y 3 de integración. Cubren cálculos, redondeo, validaciones de negocio, auditoría, cancelación, eliminación lógica y pago, incluidos escenarios de distribución y errores.

Las pruebas de integración comprueban:

- Conectividad y disponibilidad de las tablas necesarias en `BonusTest_Integration`.
- Rollback de la transacción cuando falla la inserción de una distribución.
- Dos intentos concurrentes de pagar el mismo bono: solo uno finaliza correctamente y no se duplican los importes distribuidos.

### Implementado: pago y distribución de bonos por proyecto

El endpoint `POST /api/v1/Bonus/{bonusId}/pay` y el botón **Pagar** de Angular están implementados. La operación:

- Acepta únicamente bonos registrados (`0201`).
- Distribuye el **importe bruto** proporcionalmente a las horas de `EmployeeProjectHours`.
- Redondea los importes a dos decimales y asigna el remanente al último proyecto para asegurar que la suma coincida con el bruto.
- Registra cada asignación en `dbo.BonusProjectDistribution`.
- Cambia el bono a Pagado (`0202`) dentro de la misma transacción SQL.
- Impide volver a pagar un bono ya pagado y deshabilita las acciones en la interfaz para estados distintos de Registrado.

**Validación funcional realizada:** un bono de S/ 100.00 para un empleado con proyectos de 120 y 40 horas se distribuyó en S/ 75.00 y S/ 25.00, respectivamente; el bono quedó en estado `0202`.
