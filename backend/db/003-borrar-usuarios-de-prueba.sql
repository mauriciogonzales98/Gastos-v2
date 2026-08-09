-- Borra las cuentas de prueba que quedan en la base `gestiongastos` despues de verificar
-- algo a mano contra MySQL real, con todos sus datos. La API no expone baja de cuenta, asi
-- que la unica forma de sacarlas es por SQL.
--
-- El criterio es el dominio `@ejemplo.test`: `.test` es un TLD reservado por la RFC 2606
-- justamente para pruebas, asi que ninguna cuenta real puede caer ahi. Si hiciera falta
-- otro criterio, se cambia @patron y nada mas.
--
-- OJO: esto BORRA cuentas y todos sus movimientos y categorias propias. Mira primero el
-- SELECT de abajo, que lista exactamente lo que se va a borrar antes de borrarlo.
-- No toca las categorias del sistema: esas tienen UsuarioId nulo y no matchean.
--
-- Ejecutar desde PowerShell. Dos detalles que hacen fallar el comando "obvio":
--   * `mysql` no esta en el PATH: hay que invocar el .exe con la ruta completa.
--   * No usar pipe con -p: el prompt de contrasena se come la primera linea del script.
--
--   & "C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe" -u gestiongastos_app -p -e "source C:/Users/PC/Desktop/CursoIA/GestionGastos-v2/backend/db/003-borrar-usuarios-de-prueba.sql"
--
-- Es repetible: si no queda ninguna cuenta de prueba, el SELECT sale vacio y los DELETE no
-- borran nada.

USE gestiongastos;

SET @patron = '%@ejemplo.test';

-- Que se va a borrar. Primero muestra, despues borra.
SELECT
    u.Id,
    u.Email,
    (SELECT COUNT(*) FROM Movimientos m WHERE m.UsuarioId = u.Id) AS Movimientos,
    (SELECT COUNT(*) FROM Categorias c WHERE c.UsuarioId = u.Id) AS CategoriasPropias
FROM Usuarios u
WHERE u.Email LIKE @patron;

-- El orden es explicito a proposito. Usuario -> Movimiento y Usuario -> Categoria son
-- Cascade, pero Movimiento -> Categoria es Restrict: un usuario con movimientos cargados
-- en sus propias categorias puede hacer que la cascada choque contra ese Restrict.
-- Borrando en este orden el problema no existe.

DELETE FROM Movimientos
WHERE UsuarioId IN (SELECT Id FROM Usuarios WHERE Email LIKE @patron);

DELETE FROM Categorias
WHERE UsuarioId IN (SELECT Id FROM Usuarios WHERE Email LIKE @patron);

DELETE FROM Usuarios
WHERE Email LIKE @patron;

-- Confirmacion: tiene que salir vacio.
SELECT Id, Email FROM Usuarios WHERE Email LIKE @patron;
