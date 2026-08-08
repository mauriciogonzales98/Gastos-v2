-- Limpia las tablas que GestionGastos-v1 dejo en la base `gestiongastos`, para que v2
-- pueda tomar el schema. v1 y v2 comparten base y sus migraciones chocan: v1 ya habia
-- creado `usuarios`, `categorias` y `movimientos` con la migracion `InicialMovimientos`.
--
-- OJO: esto BORRA los datos de v1 y su historial de migraciones. Ese proyecto deja de
-- funcionar contra esta base hasta que se lo apunte a otra o se lo migre de nuevo.
--
-- Ejecutar desde PowerShell. Dos detalles que hacen fallar el comando "obvio":
--   * `mysql` no esta en el PATH: hay que invocar el .exe con la ruta completa.
--   * No usar pipe con -p: el prompt de contrasena se come la primera linea del script.
--
--   & "C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe" -u gestiongastos_app -p -e "source C:/Users/PC/Desktop/CursoIA/GestionGastos-v2/backend/db/002-limpiar-tablas-de-v1.sql"
--
-- Despues, la migracion de v2 (desde la carpeta GestionGastos-v2, no desde CursoIA):
--   dotnet ef database update --project backend/GestionGastos.Api

USE gestiongastos;

SET FOREIGN_KEY_CHECKS = 0;

-- `movimientos` va primero por sus claves foraneas; el IF EXISTS deja el script
-- repetible aunque alguna tabla ya no este.
DROP TABLE IF EXISTS movimientos;
DROP TABLE IF EXISTS categorias;
DROP TABLE IF EXISTS usuarios;

-- El historial de migraciones de v1: si queda, EF creeria que ya aplico migraciones
-- que no son suyas.
DROP TABLE IF EXISTS `__EFMigrationsHistory`;

SET FOREIGN_KEY_CHECKS = 1;
