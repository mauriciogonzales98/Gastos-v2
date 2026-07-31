# AGENTS.md

## Propósito
App de registro y gestión de gastos e ingresos personales, cargados por formulario, con un dashboard para visualizarlos.

## Stack
- Backend: .NET 10 (SDK 10.0.301), Entity Framework Core 9.0.18 + Pomelo.MySQL 9.0.0
- Frontend: React 19 + Vite, Node 24 + pnpm
- Base de datos: MySQL 8.4.5 local, puerto 3306, schema `gestiongastos`
- Testing: xUnit en backend, Vitest en frontend

> EF Core queda en 9.x a propósito: Pomelo todavía no publicó provider para EF Core 10.
> El `TargetFramework` sí es `net10.0`.

## Cómo correr

**Correr todo desde Windows (PowerShell), no desde WSL.** El proyecto vive en el
filesystem de Windows; un `pnpm install` hecho desde WSL genera symlinks que
`node.exe` no puede resolver y el dev server no arranca. Si pasa: borrar
`node_modules` y reinstalar desde PowerShell.

Backend (`/backend/GestionGastos.Api`):
```
dotnet restore
dotnet run          # http://localhost:5157
dotnet test
```

Frontend (`/frontend`):
```
pnpm install
pnpm dev            # http://localhost:5173
pnpm test
```

Smoke checks: `/health` (no toca la base) y `/health/db` (verifica MySQL).

## Configuración local
La cadena de conexión va en user-secrets, nunca en `appsettings`:
```
dotnet user-secrets set "ConnectionStrings:MySql" "Server=127.0.0.1;Port=3306;Database=gestiongastos;User Id=gestiongastos_app;Password=..."
```
La plantilla está en `appsettings.Development.example.json`. Para crear la base y
el usuario desde cero: `backend/db/001-crear-base-y-usuario.sql`.

## Qué NO hacer
- No guardar contraseñas en texto plano: deben almacenarse con hash seguro (bcrypt/argon2) (RNF-03).
- No commitear credenciales: ni en `appsettings*.json` ni en los `.sql` de `backend/db/`.
- No correr `pnpm install` desde WSL (ver arriba).
