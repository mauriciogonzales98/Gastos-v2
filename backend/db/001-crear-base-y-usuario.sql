-- Crea la base y el usuario de aplicacion de GestionGastos.
--
-- Ejecutar como root:
--   mysql -u root -p < backend/db/001-crear-base-y-usuario.sql
--
-- La contrasena NO se versiona: reemplazar el placeholder al momento de ejecutar
-- y usar el mismo valor en el user-secret "ConnectionStrings:MySql".

CREATE DATABASE IF NOT EXISTS gestiongastos
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_0900_ai_ci;

CREATE USER IF NOT EXISTS 'gestiongastos_app'@'localhost'
  IDENTIFIED BY '<REEMPLAZAR_POR_UNA_PASSWORD_LOCAL>';

GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, DROP, INDEX, REFERENCES
  ON gestiongastos.* TO 'gestiongastos_app'@'localhost';

FLUSH PRIVILEGES;
