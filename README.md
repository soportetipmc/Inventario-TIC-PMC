# Inventario TIC – Palmas Montecarmelo (Fase 1)

ASP.NET Core MVC + Entity Framework Core + SQL Server, con Windows Authentication y roles por grupo de Active Directory.

Incluye: sedes, áreas, categorías, proveedores, empleados (importados de AD), activos con código automático
(PC-0001), asignaciones con historial, filtros, carga masiva por CSV.
Etiquetas imprimibles de 70 × 35 mm con logo, código y QR (una sola o toda la lista filtrada).
Pendiente (fases 2 y 3): adjuntos, mantenimientos, licencias, actas en PDF, alertas, reportes y auditoría.

## Puesta en marcha

1. Instalar el SDK de .NET (misma versión LTS que `TargetFramework` en el .csproj; ajústala si hace falta).
2. Editar `appsettings.json`: cadena de conexión y nombres de los tres grupos de AD
   (`DOMINIO\INV_Admin`, `DOMINIO\INV_Tecnico`, `DOMINIO\INV_Consulta`). Crear esos grupos en AD y añadir usuarios.
3. Crear la migración inicial (solo la primera vez):

       dotnet tool install --global dotnet-ef
       dotnet ef migrations add Inicial

   Al arrancar, la app aplica las migraciones y carga las sedes y categorías iniciales.
4. Probar en Visual Studio con IIS Express (ya trae Windows Authentication en `launchSettings.json`).

## Publicar en IIS (Windows Server 2016)

1. Instalar el ASP.NET Core Hosting Bundle de la misma versión de .NET.
2. `dotnet publish -c Release -o C:\inetpub\inventario-tic`
3. Crear el sitio en IIS, con su propio grupo de aplicaciones. Activar Windows Authentication y desactivar Anonymous.
4. Dar al usuario del grupo de aplicaciones permisos sobre la base de datos (crear/modificar esquema para migrar,
   o aplicar el script de migración con `dotnet ef migrations script` desde una cuenta administradora).
5. Programar el backup diario de SQL Server y probar una restauración.

## Notas

- La carga masiva usa CSV con punto y coma, sin comillas ni campos con `;` dentro.
- Los estados son una lista fija (en uso, en bodega, en reparación, baja, perdido).
- Si cambias el nombre de las clases de entidad, genera una nueva migración.

## Etiquetas con QR

- En el detalle de un activo: «Imprimir etiqueta». En el listado: «Imprimir etiquetas de esta lista» (respeta los filtros).
- Por defecto el QR abre la ficha del activo. En `appsettings.json`, `Etiqueta:UrlBase` debe ser la dirección con la que
  los usuarios entran a la herramienta (por ejemplo `http://servidor-inventario`); si queda vacía se usa la dirección de la petición.
- `Etiqueta:ContenidoQr = "Texto"` hace que el QR lleve los datos del activo (código, equipo, serial, sede) sin abrir la web.
- El logo está en `wwwroot/logo.png`.

## Sobre el servidor

- Con SQL Server 2017 la app fija el nivel de compatibilidad 140 en `Program.cs`. La documentación actual de EF Core
  lista SQL Server 2019 en adelante como versiones soportadas, así que con 2017 funciona de forma no oficial: pruébalo bien
  y planea subir de versión.
- SQL Server Developer no tiene licencia para producción y Windows Server «Evaluation» es de prueba temporal.

## Repositorio

- La carpeta `Migrations` (generada con `dotnet ef migrations add`) hay que versionarla: sin ella no se crea la base de datos.
- No subir contraseñas, claves ni cadenas de conexión con credenciales. Hoy `appsettings.json` solo usa autenticación de Windows.
- El repositorio guarda el código, no los datos: las copias de seguridad de SQL Server son aparte.
