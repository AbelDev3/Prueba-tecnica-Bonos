# Gestión de bonos por desempeño — Prueba técnica Full Stack

Solución para registrar, consultar, cancelar y eliminar lógicamente bonos por desempeño de empleados. Incluye API REST en ASP.NET Core, interfaz Angular y scripts SQL Server.

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
README.md
```

## Funcionalidades implementadas

- Consulta paginada de bonos (10 registros por página) y filtros por área/departamento, empleado, periodo y estado.
- Consulta de empleado y validación de su estado activo.
- Registro de bonos de **importe fijo** o **porcentaje del sueldo mensual**.
- Cálculo en el servidor de importe bruto, retención y neto, con redondeo a dos decimales (`MidpointRounding.AwayFromZero`).
- Prevención de bonos activos duplicados para un mismo empleado y periodo.
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
3. Configurar la cadena de conexión del backend para apuntar a esa base de datos.

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

**Último resultado informado durante el desarrollo:** 20 pruebas correctas, 0 fallidas. Los casos cubren cálculo fijo y porcentual, redondeo, empleado inactivo, duplicados, límites de periodo/importe/porcentaje, auditoría, cancelación y eliminación lógica. Las pruebas con Moq comprueban la lógica de servicio **sin conexión real a SQL Server**.

## Alcance y trabajo pendiente

La entrega implementa el flujo solicitado de administración de bonos. Quedan como posibles mejoras posteriores:

- Autenticación y autorización reales, sustituyendo el identificador de auditoría configurable `UserCode` por la identidad del usuario autenticado.
- Pruebas de integración con SQL Server y pruebas automatizadas de interfaz/end-to-end.
- Mayor cobertura de casos límite y refactorización adicional de datos de prueba.
- Parametrización de configuración para despliegue en entornos distintos del local y automatización CI/CD.

Estas mejoras **no forman parte de la funcionalidad implementada** en esta entrega.

## Historial de desarrollo

El repositorio conserva commits incrementales para backend, frontend, validaciones, pruebas y refactorizaciones; no se entrega como un único commit final.

## Puntos extra (opcionales)

### Implementado: pruebas unitarias del cálculo de bonos

Se implementaron pruebas automatizadas utilizando **xUnit y Moq** en el proyecto `backend/tests/BonusManagement.Tests`.

La solución cuenta con **20 casos de prueba ejecutados satisfactoriamente**, que cubren:

- Cálculo de bonos fijos y porcentuales.
- Redondeo financiero a dos decimales.
- Validación de empleados activos.
- Prevención de bonos duplicados.
- Restricciones de importes y porcentajes.
- Validación de periodos futuros.
- Validación del usuario de auditoría.
- Cancelación y eliminación lógica de bonos.
- Restricciones sobre bonos cancelados o eliminados.

### Pendiente: pago y distribución de bonos por proyecto

No se implementó el endpoint `POST /api/v1/Bonus/{bonusId}/pay` ni el botón de pago en la interfaz Angular.

Esta funcionalidad opcional contempla:

- Distribuir el importe bruto del bono entre los proyectos del empleado, proporcionalmente a las horas registradas en `EmployeeProjectHours`.
- Redondear cada distribución a dos decimales.
- Asignar al último proyecto la diferencia necesaria para cuadrar exactamente el importe bruto.
- Asignar el importe completo cuando exista un único proyecto.
- Guardar el detalle de distribución.
- Cambiar el estado del bono a **Pagado (`0202`)**.
- Ejecutar el registro del pago y sus distribuciones en una única transacción.

**Decisión técnica:** se priorizó completar, validar y documentar las funcionalidades obligatorias y las pruebas unitarias antes de abordar esta ampliación opcional.
