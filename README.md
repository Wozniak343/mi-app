# Mi App - Gestor de Tareas (API + Frontend) - Prueba de Fullstack

Este repositorio contiene una pequeña aplicación full-stack para gestionar tareas. Está compuesta por:

- Una API REST construida con .NET (Minimal APIs) ubicada en `MiApp.Api/`.
- Un frontend en Angular (Aplicación SPA) ubicado en la carpeta `src/` del proyecto Angular.

El objetivo de este README es explicar qué hace la aplicación, cómo instalarla y ejecutarla localmente, y qué endpoints/funcionalidades están disponibles.

## Resumen funcional

La aplicación permite:

- Listar tareas (con filtros por título y por estado).
- Crear una nueva tarea.
- Editar una tarea existente (por id).
- Eliminar una tarea (por id).
- Verificar la conexión a la base de datos (endpoint de prueba).

Las rutas principales de la API son:

- `GET  /api/test-connection` — Verifica la conectividad a la base de datos.
- `GET  /api/tareas-usuarios` — Lista tareas; soporta filtros opcionales: `titulo` y `estado` (true/false).
- `POST /api/tareas-usuarios` — Crea una tarea. Cuerpo JSON: `{ "titulo": "...", "descripcion": "...", "fechaVencimiento": "YYYY-MM-DD" }` (titulo es obligatorio).
- `PUT  /api/tareas-usuarios/{id}` — Actualiza la tarea con id dado (mismo esquema que POST, `titulo` obligatorio).
- `DELETE /api/tareas-usuarios/{id}` — Elimina la tarea con id dado.

## Requisitos previos

Instale las dependencias necesarias en su máquina de desarrollo:

- .NET SDK 8.0 o superior (se usa `net8.0` en el proyecto API).
- Node.js 17.x o 20.x (LTS recomendado).
- npm (v8+), incluido con Node.js.
- Angular CLI (opcional, para usar comandos `ng` directamente):

```powershell
npm install -g @angular/cli
```

Además necesita una instancia de base de datos SQL Server (local o en red). Por defecto la aplicación apunta a la cadena de conexión definida en `MiApp.Api/appsettings.json`.

> Nota de seguridad: el `appsettings.json` incluido puede contener una contraseña de ejemplo. No utilice credenciales sensibles en entornos públicos ni en repositorios compartidos.

## Configuración de la base de datos

La cadena de conexión por defecto se encuentra en `MiApp.Api/appsettings.json` bajo `ConnectionStrings:Default`. Ejemplo:

```json
"ConnectionStrings": {
  "Default": "Server=localhost,1433;Database=MiAppDB;User Id=sa;Password=ProyectoTecnico2025**;Encrypt=True;TrustServerCertificate=True"
}
```

Si no tiene SQL Server instalado localmente, puede ejecutar una instancia rápida con Docker (Windows/mac/Linux):

```powershell
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=ProyectoTecnico2025**" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

Después de arrancar SQL Server, cree la base de datos `MiAppDB` o ajuste la cadena de conexión al nombre de base que prefiera.

## Instalación y ejecución (desarrollo)

1. Clonar el repositorio y posicionarse en la carpeta del proyecto Angular:

```powershell
cd C:\ruta\a\repositorio\mi-app
```

2. Instalar dependencias del frontend (desde la raíz `mi-app`):

```powershell
npm install
```

3. Ejecutar la API (.NET) en una terminal nueva:

```powershell
cd MiApp.Api
dotnet restore
dotnet build
dotnet run
```

La API arranca por defecto en `https://localhost:5001` o `http://localhost:5000` (según su configuración). La política CORS de desarrollo permite peticiones desde `http://localhost:4200`.

4. Ejecutar el frontend (otra terminal):

```powershell
cd ..\
npm start
# o alternativamente:
ng serve --open
```

Esto lanza el servidor de desarrollo de Angular en `http://localhost:4200`.

## Cómo probar rápidamente

1. Abrir `http://localhost:4200` en el navegador para usar la interfaz.
2. Probar la API directamente (por ejemplo con curl o Postman):

```powershell
# Probar conexión a la DB
curl https://localhost:5001/api/test-connection

# Listar tareas
curl "https://localhost:5001/api/tareas-usuarios"
```

Recuerde que si usa HTTPS en la API puede necesitar aceptar el certificado de desarrollo en el navegador o usar `-k` con curl.

## Dependencias principales

- Backend: .NET 8, Entity Framework Core (según implementación en `MiApp.Api`).
- Frontend: Angular 17 (ver `package.json`), RxJS, Zone.js.

## Funcionalidades implementadas (resumen técnico)

- API REST con endpoints CRUD para la entidad `Tarea`.
- Filtrado de listado por título y por estado (activo/inactivo).
- Validaciones básicas en servidor y mensajes estandarizados en JSON (`error`, `title` o `Problem` cuando aplica).
- Frontend Angular con formulario reactivo para crear/editar/eliminar tareas, búsqueda, paginación simple (si aplica) y manejo de estados de carga y errores.
- CORS configurado para desarrollo entre `localhost:4200` y la API.

## Buenas prácticas y notas de despliegue

- Cambie la contraseña de `sa` y use un usuario con permisos mínimos para producción.
- Configure TLS y secretos mediante el gestor de secretos del entorno (Azure Key Vault, variables de entorno o secretos de Docker/Kubernetes) en vez de `appsettings.json` para entornos de producción.
- Si despliega en contenedores, asegúrese de exponer y mapear correctamente los puertos y usar variables de entorno para la cadena de conexión.

## Solución de problemas

- Error de conexión a la base de datos: verifique que SQL Server está en ejecución, que el puerto 1433 está accesible y la cadena de conexión es correcta.
- `ng serve` falla: asegúrese de tener Node y Angular CLI compatibles con la versión del proyecto (Angular 20.x). Borre `node_modules` y ejecute `npm install` si hay problemas.
- Errores HTTPS/Certificado: para desarrollo puede usar las URLs HTTP o aceptar el certificado de desarrollo.

## Contacto y licencia

Este proyecto es una prueba técnica. Adapte el README y la configuración según sus políticas internas antes de publicarlo.

---

## Cómo crear la base de datos en SQL Server (paso a paso)

Esto es lo que hice para tener la tabla lista. Va sencillo y al grano.

1) Conectarme y crear/usar la base

- Abrí SQL Server Management Studio (SSMS) o Azure Data Studio.
- Me conecté a mi instancia (local o la del Docker).
- Si no tenía la base, la creé y luego la usé:

```powershell
CREATE DATABASE MiAppDB;
GO

USE MiAppDB;
GO
```

2) Crear la tabla y probar con datos

Copia y pega este script tal cual. Esto me deja la tabla, mete dos tareas de ejemplo, borra una por id (puedes cambiar el número) y al final muestra todo.
```powershell
CREATE TABLE dbo.Tareas (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    Titulo           NVARCHAR(150)    NOT NULL,
    Descripcion      NVARCHAR(MAX)    NULL,
    Estado           BIT              NOT NULL
                        CONSTRAINT DF_Tareas_Estado DEFAULT (0),
    FechaCreacion    DATETIME2(0)     NOT NULL
                        CONSTRAINT DF_Tareas_FechaCreacion DEFAULT (SYSDATETIME()),
    FechaVencimiento DATE             NULL,
    CONSTRAINT CK_Tareas_FecVenc
        CHECK (FechaVencimiento IS NULL OR FechaVencimiento >= CAST(FechaCreacion AS DATE))
);

INSERT INTO dbo.Tareas (Titulo, Descripcion, FechaVencimiento)
VALUES (N'Configurar entorno', N'Instalar .NET 8 y Angular', DATEADD(DAY, 7, CAST(SYSDATETIME() AS DATE)));

INSERT INTO dbo.Tareas (Titulo, Descripcion, Estado, FechaVencimiento)
VALUES (N'Diseñar esquema de base de datos', N'Definir tablas, claves primarias/foráneas y restricciones iniciales.',1,DATEADD(DAY, 7, CAST(SYSDATETIME() AS DATE)));

-- Eliminar una tarea por Id (ejemplo con Id = 5)
DELETE FROM dbo.Tareas
WHERE Id = 3;

SELECT *
FROM dbo.Tareas;
```
3) Qué hace cada parte (explicado fácil)

`Id INT IDENTITY(1,1) PRIMARY KEY:` el id se va aumentando solito (1, 2, 3…), y es la clave principal.
`Titulo NVARCHAR(150) NOT NULL:` texto corto obligatorio (hasta 150 caracteres).
`Descripcion NVARCHAR(MAX) NULL:` texto largo opcional.
`Estado BIT NOT NULL DEFAULT (0):` 0 o 1. Por defecto queda en 0 (yo lo uso como “inactiva”).
`FechaCreacion DATETIME2(0) DEFAULT (SYSDATETIME()):` guarda la fecha/hora al momento de insertar.
`FechaVencimiento DATE NULL:` fecha opcional.
`CK_Tareas_FecVenc:` esta regla evita poner una fecha de vencimiento menor a la fecha de creación (se permite NULL).
`INSERT de ejemplo:` agrega dos tareas; la segunda entra como activa (Estado = 1). Las dos tienen vencimiento a 7 días.
`DELETE:` borra por Id. Cambia el 3 por el id que quieras eliminar.
`SELECT *:` muestra lo que hay en la tabla para confirmar.

4) Notas rápidas por si algo no arranca

- Si marca permiso denegado, revisa el usuario con el que te conectas (que tenga permisos en MiAppDB).
- Si el puerto 1433 está ocupado, revisa el contenedor de Docker o el SQL local.
- Si usas Docker, asegúrate de haber corrido algo como:

```powershell
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=ProyectoTecnico2025**" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```
- Si ves advertencias de certificado en desarrollo, es normal; en producción ya se configura bien.
