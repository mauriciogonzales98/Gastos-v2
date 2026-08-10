# MEMORIA.md — estado del proyecto

Bitácora para retomar el trabajo entre sesiones. Última actualización: 2026-08-09.

## Dónde estamos

**Paso 0 (andamiaje) terminado, commiteado y pusheado.** Commit `61d6176`
(`chore: armar andamiaje de backend .net 10, frontend react y healthchecks`),
ya en `origin/main`.

**Features 1 y 2 terminadas, commiteadas y pusheadas** (`7a837c1`, `483429a`).

**Soporte de varias monedas terminado, commiteado y pusheado** (`377c3db`, `328c0c5`).
La moneda es una **tabla catálogo** (`Monedas`), no un enum: sumar una es insertar una fila.

**Feature 3 (dashboard y resumen) terminada, commiteada y pusheada** (`d2fa214`, `63eb84e`).

**Nota descriptiva del movimiento (RF-33) terminada** — 2026-08-09. Pedido fuera del PRD
original; se actualizó el PRD a la versión 4 primero, igual que con monedas.

**Las 33 RF del PRD están cubiertas.** 90 tests de xUnit y 47 de Vitest en verde.

**Paso final (no funcionales, AC-32 a AC-34) terminado** — 2026-08-09. Los tres pasan con
dos órdenes de magnitud de margen; los números están más abajo.

**El PRD está cubierto de punta a punta: 33 RF y 53 AC, todos verificados.**

## Plan general

Las RF del PRD se agrupan en 3 features core (más el agregado de monedas). Cada una se cierra con un
checkpoint verificable antes de pasar a la siguiente.

### Paso 0 — Andamiaje ✅ COMPLETADO

- [x] 0.1 Solución `GestionGastos.slnx` con `backend/GestionGastos.Api` (minimal
      API, net10.0) y `backend/GestionGastos.Tests` (xUnit). Frontend en
      `frontend/` con React 19 + Vite 8 + TS, Vitest + Testing Library.
- [x] 0.2 `/health` y `/health/db`, EF Core 9.0.18 + Pomelo 9.0.0, connection
      string en user-secrets, `.gitignore`.
- [x] 0.3 `backend/db/001-crear-base-y-usuario.sql` (con placeholder, sin
      credenciales versionadas).
- [x] Checkpoint: `/health` y `/health/db` responden 200.

### Feature 1 — Cuentas y autenticación (RF-01 a RF-05, RNF-03, RNF-04) ✅ COMPLETADA

- [x] 1.1 Entidad `Usuario` (`Entidades/Usuario.cs`) + migración `CrearUsuarios` en
      `Data/Migraciones`, ya aplicada. Hash bcrypt con factor 12 (RNF-03).
- [x] 1.2 `POST /auth/register` (201, 409 si el email ya existe → AC-02) y
      `POST /auth/login` (RF-02). El registro deja la sesión abierta.
- [x] 1.3 Cookie httpOnly `gestiongastos.sesion`, SameSite=Lax, `ExpireTimeSpan` 24 h con
      `SlidingExpiration` (RNF-04) + `POST /auth/logout` y `GET /auth/me`.
- [x] 1.4 `FallbackPolicy` que exige sesión en todo endpoint salvo los marcados
      `AllowAnonymous` (`/health`, `/health/db`, `/auth/*`). El id sale siempre de los
      claims vía `UsuarioActual.ObtenerId`, nunca de un parámetro del cliente.
- [x] 1.5 Tests xUnit: 20/20. AC-01 a AC-05, AC-09, AC-35, AC-36 y validaciones.
      AC-06/07/08 quedan parciales a propósito: hablan de movimientos (feature 2); lo que
      se testea hoy es que dos sesiones no se mezclan y que un id del cliente no cambia
      la identidad.
- [x] 1.6 Frontend: `ProveedorAutenticacion` + `Guarda` + `PantallaAutenticacion` /
      `PantallaPrincipal`. 8/8 tests de Vitest.
- [x] Checkpoint verificado contra MySQL con curl: alta 201, `/auth/me` 200 con cookie
      y 401 sin ella, alta duplicada 409, logout 204, `/auth/me` 401 después del logout,
      login con clave equivocada 401, login correcto 200. La cookie sale `HttpOnly` y
      vence a las 24 h.

### Feature 2 — Categorías y movimientos (RF-06 a RF-18) ✅ COMPLETADA

- [x] 2.1 `Entidades/Categoria.cs` (tipo, `EsDelSistema`, `FechaBajaUtc`) y
      `Entidades/Movimiento.cs` (monto `decimal(18,2)`, `DateOnly Fecha`), migración
      `CrearCategoriasYMovimientos` aplicada, seed en `Data/CatalogoDeCategorias.cs`.
- [x] 2.2 `GET/POST /categorias`, `PUT/DELETE /categorias/{id}`. Las del sistema dan 403;
      las de otro usuario, 404. La baja es lógica.
- [x] 2.3 `POST/PUT/DELETE /movimientos`, con monto > 0 y ≤ 2 decimales y categoría
      obligatoria, activa y accesible.
- [x] 2.4 `GET /movimientos?desde&hasta&categoriaId`, default mes actual, rango inclusivo.
- [x] 2.5 Tests xUnit: 53/53. Incluye AC-06, AC-07 y AC-08, que habían quedado parciales
      en la feature 1 porque necesitaban movimientos para verificarse.
- [x] 2.6 Frontend: `FormularioMovimiento`, `ListadoMovimientos`, `PanelCategorias`.
      26/26 en Vitest.
- [x] Checkpoint verificado con curl: catálogo predefinido completo, alta de un gasto y un
      ingreso, listado del mes actual, filtro por categoría, período vacío sin error,
      monto con 3 decimales → 400, renombrar categoría del sistema → 403.

**Decisiones tomadas**: gastos Comida, Transporte, Vivienda, Servicios, Salud, Ocio,
Otros; ingresos Sueldo, Ingreso extra, Otros (confirmadas por el usuario el 2026-08-08).
El movimiento **no guarda su tipo**: lo hereda de la categoría, así no pueden quedar en
desacuerdo.

> La moneda única que se decidió acá quedó **superada** por el bloque "Monedas" de abajo.

### Monedas (RF-24 a RF-32) ✅ COMPLETADA — 2026-08-08

Pedido fuera del PRD original ("múltiples monedas" estaba en Fuera de Alcance). Se
actualizó el PRD **primero** (versión 3) y después se implementó.

- [x] Tabla `Monedas` (`Codigo` ISO 4217, `Nombre`, `Simbolo`, `Decimales`,
      `EsPredeterminada`, `Orden`) sembrada con ARS y USD, y `Movimiento.MonedaCodigo`
      con foreign key. `GET /monedas` expone el catálogo.
- [x] Migraciones `AgregarMonedaAMovimientos` y `ConvertirMonedaEnCatalogo`, aplicadas.
      Los movimientos previos quedaron en ARS.
- [x] Pesos por defecto al crear (AC-38), rechazo de moneda inválida (AC-39), moneda
      editable (AC-47) y filtro `?moneda=` con las dos por defecto (AC-45).
- [x] Frontend: selector en el formulario, símbolo en cada fila (`$` / `US$`) y filtro.
- [x] Tests: 14 de xUnit + 6 de Vitest. **AC-49** inserta una tercera moneda solo como
      dato y verifica que quede usable de punta a punta: es lo que sostiene el diseño.
- [x] Verificado contra MySQL con el round-trip completo de las migraciones (Up → Down →
      Up): un movimiento en USD sobrevive intacto, así que las dos ramas de traducción
      (`Pesos`→`ARS` y `Dolares`→`USD`) quedan probadas sobre datos reales.

**Decisiones**: catálogo en tabla, no enum, porque van a sumarse monedas; códigos ISO 4217
("ARS", "USD") porque "Pesos" es ambiguo; la predeterminada y los decimales son datos de la
fila, no constantes; el frontend arma selector, filtro y formato desde `GET /monedas`.
La moneda se puede corregir sin borrar el movimiento; filtro con "todas" por defecto; las
categorías son compartidas entre monedas.
**No hay conversión**: ningún total suma montos de monedas distintas (RF-29).

AC-41, AC-42, AC-43 y AC-46 (que los totales del dashboard no se mezclen) quedan para la
feature 3, que es donde existe el dashboard.

### Feature 3 — Dashboard y resumen (RF-19 a RF-22, RF-29, RF-30, RNF-01) ✅ COMPLETADA

- [x] 3.1 `GET /dashboard?desde&hasta&moneda`: totales por categoría, ingresos, gastos y
      balance **agregados en SQL** con dos `GROUP BY`, no trayendo los movimientos para
      sumarlos en memoria. Verificado leyendo el SQL que genera EF.
- [x] 3.2 `ResumenDelMes` usa **el mismo endpoint** pedido con el mes actual: AC-30 se
      cumple por construcción, no por dos cálculos que hay que mantener de acuerdo.
- [x] 3.3 Frontend: `PanelDashboard` + `GraficoGastos` (barras horizontales de una sola
      serie) + filtro de fechas y de moneda, con estado vacío en cero y sin error.
- [x] 3.4 Tests: 11 de xUnit + 8 de Vitest. Incluye **AC-41, AC-42, AC-43 y AC-46**, que
      habían quedado pendientes del agregado de monedas porque necesitaban el dashboard.
- [x] Checkpoint verificado contra MySQL con datos en dos monedas: para cada moneda, los
      totales por categoría suman exactamente el total de gastos y el balance es
      ingresos − gastos. Ningún número mezcla monedas.

**Decisiones**: el dashboard devuelve un bloque por moneda armado sobre el catálogo, así
una moneda sin movimientos aparece en cero en vez de faltar (AC-31). El gráfico es de una
sola serie (magnitud por categoría), así que va de un solo tono y sin leyenda: el color no
codifica nada, la longitud sí. Cada barra lleva nombre y monto como etiqueta directa, con
lo que el gráfico se lee sin hover y sirve de tabla para un lector de pantalla.

### Nota descriptiva del movimiento (RF-33) ✅ COMPLETADA — 2026-08-09

Pedido del usuario: "quiero saber en qué gasté, más allá de la categoría". Se evaluó contra
etiquetas reutilizables y **se eligió la nota libre**: lo que hacía falta era *detalle para
leer*, no analítica. Etiquetas habría sido construir un catálogo entero (tabla, N:M, ABM,
filtro, totales) para responder algo que nadie preguntó.

- [x] PRD versión 4: `RF-33` y `AC-50` a `AC-53`.
- [x] `Movimiento.Descripcion` (`varchar(120)`, **nullable**) + migración
      `AgregarDescripcionAMovimientos`, aplicada. El largo sale de la constante
      `Movimiento.LargoMaximoDescripcion`: el mapeo de EF y la validación leen la misma.
- [x] Aceptada en POST y PUT, devuelta en GET. **Sin índice, a propósito**: no se busca.
- [x] Frontend: campo al final del formulario ("Nota (opcional)"), columna en el listado,
      se carga al editar y se limpia tras el alta.
- [x] Tests: 7 de xUnit + 5 de Vitest.
- [x] Checkpoint verificado contra MySQL con curl: alta con nota (recortada), sin nota,
      con nota en blanco → `null`, 121 caracteres → 400 con el motivo, edición y borrado
      de la nota, y el dashboard con los mismos totales antes y después.

**Decisiones**: blanco es `null` y no cadena vacía, así "sin nota" es un solo estado y no
dos que haya que comparar por separado en cada lugar. El `Trim()` va **antes** de medir el
largo, para que 130 espacios no cuenten como una nota de 130 caracteres. El PUT reemplaza
la nota —y la borra si viene en blanco— igual que el resto de los campos: no existe
"dejala como estaba", porque el PUT ya reemplaza el movimiento entero.

**El riesgo que esto abre está anotado en el PRD**: la nota puede volverse una taxonomía
informal ("alquiler" / "Alquiler" / "alq") que el sistema no entiende y que da falsa
sensación de estar clasificando. La mitigación es que **no se busca, no se filtra y no se
agrupa** — quedó escrito en Fuera de Alcance. Si algún día hace falta totalizar por algo
más fino que la categoría, se resuelve con un catálogo de etiquetas, no estirando la nota.

### Correcciones de frontend — 2026-08-09

- [x] El **resumen del mes va primero** en la pantalla principal, arriba del formulario.
- [x] Tras un alta exitosa se **limpian el monto y la nota**, para cargar el siguiente sin
      borrar a mano. El `setMonto('')` va después del `await` y dentro del `try`: si el
      backend rechaza el alta, lo cargado se conserva para poder corregirlo. Categoría,
      tipo, moneda y fecha se mantienen, que es lo cómodo para cargar varios seguidos.
      Al **editar** no se limpia nada acá: salir del modo edición ya lo hace, vía el efecto
      sobre `enEdicion`.

### Correcciones de frontend (tanda 2) ✅ COMPLETADA — 2026-08-10

Pedido del usuario: siete correcciones de presentación. **Ninguna agrega features**: es la
misma funcionalidad, mejor puesta. El backend no se tocó.

- [x] **La edición del movimiento va en un modal** (`ui/Modal.tsx`, `<dialog>` nativo).
      Cierra con Escape, con "Cancelar", con "Cerrar" y al guardar.
- [x] **Tres pestañas** (`ui/Pestanas.tsx`): Movimientos (resumen + alta + listado),
      Dashboard y Categorías.
- [x] Los botones de las filas de categoría dicen **solo la acción**: "Renombrar",
      "Eliminar".
- [x] Botones de fila **más chicos** (clase `.compacto`).
- [x] **Separador de miles en el monto** mientras se escribe (`utiles/montos.ts`).
- [x] **Flecha propia en los `<select>`**, en SVG inline, con el color del tema.
- [x] Tests: 71 de Vitest (eran 47). Backend intacto: 90/90 de xUnit.
- [x] **Verificado a mano en el navegador** por el usuario (2026-08-10): las siete
      correcciones se ven bien. Era lo único que no se podía cerrar desde los tests
      (la flecha de los `select` y el modal son CSS, y jsdom no los renderiza).

**Decisiones**:

- **Un solo formulario en pantalla.** Al editar, el de alta se desmonta y el mismo
  componente se monta dentro del modal. Con los dos a la vez habría dos campos "Monto"
  compitiendo por el mismo rótulo, que rompe tanto los tests como un lector de pantalla.
- **`<dialog>` nativo y no un div propio**: trae el fondo inerte, la trampa de foco, el
  Escape y la capa superior sin pelear con `z-index`.
- **El estado vive en `PantallaPrincipal`, no en las pestañas.** Cambiar de pestaña no
  vuelve a pedir nada ni pierde los filtros; hay un test que lo fija contando pedidos.
- **El nombre de la categoría acompaña al botón como `aria-describedby`**, no como
  rótulo. Así el nombre accesible del botón es lo que hace ("Eliminar") y de qué fila es
  lo dice la descripción. Los tests ubican el botón dentro de su `<li>`, que es lo que
  hace una persona.
- **El resumen del mes se queda en la pestaña Movimientos**: es la pantalla de trabajo
  diario. El dashboard con su propio filtro de período es otra cosa.

### Paso final — No funcionales medibles ✅ COMPLETADO — 2026-08-09

**Los tres AC pasan, con dos órdenes de magnitud de margen.** Medido con
`backend/GestionGastos.Herramientas`, contra la API en Release y MySQL real, p95 sobre
100 ejecuciones y 10 de calentamiento descartadas.

| AC | Escenario | Umbral | Medido | Margen |
|---|---|---|---|---|
| AC-32 (RNF-01) | Dashboard, cuenta con 1000 movimientos | < 2 s | **11,3 ms** | ×177 |
| AC-33 (RNF-01) | Dashboard, cuenta con 10000 movimientos | < 4 s | **72,2 ms** | ×55 |
| AC-34 (RNF-02) | `POST /movimientos` válido | < 1 s | **7,8 ms** | ×128 |

**Los números de dashboard son del peor caso, no del uso normal.** Sin parámetros el
dashboard mira solo el mes actual, y los datos van repartidos en dos años: así medía 5-8 ms
porque agregaba ~1/24 de la cuenta. Lo que está en la tabla es con un rango que cubre
**todos** los movimientos sembrados. El uso normal es bastante más rápido:

| Escenario | mes actual | rango completo |
|---|---|---|
| 1000 movimientos | 5,3 ms | 11,3 ms |
| 10000 movimientos | 7,6 ms | 72,2 ms |

F.4 (`EXPLAIN` y luego índices) **no hizo falta**: nada estuvo ni cerca de fallar. La
agregación en SQL con dos `GROUP BY` y el índice `(UsuarioId, Fecha, MonedaCodigo)` ya
alcanzaban; ahora está confirmado con números y no solo por lectura del SQL.

**La herramienta** (`backend/GestionGastos.Herramientas`, consola, no se despliega):

```powershell
dotnet run -c Release --project backend/GestionGastos.Herramientas -- sembrar 10000
dotnet run -c Release --project backend/GestionGastos.Herramientas -- medir
dotnet run -c Release --project backend/GestionGastos.Herramientas -- limpiar
```

- Comparte el `UserSecretsId` con la API, así lee la misma cadena de conexión sin
  configurar ni copiar nada.
- `sembrar` inserta por EF y no por HTTP: 10000 POST tardan muchísimo y medirían el costo
  de la API, no el del dashboard. Es idempotente (borra lo anterior) y usa semilla fija,
  así que dos corridas iguales dan los mismos datos y se pueden comparar.
- La cuenta de carga es `carga@ejemplo.test`, que cae bajo el criterio de
  `003-borrar-usuarios-de-prueba.sql`. `limpiar` la borra desde la herramienta, que es más
  cómodo: no hay que pedir la contraseña de MySQL. **Ya se corrió**: la base quedó limpia.

**Cómo medir para que el número signifique algo** (por si hay que rehacerlo)

- Contra la **API real y MySQL**, nunca contra los tests: esos corren sobre SQLite en
  memoria y darían un número que no existe en producción.
- Levantar la API en **Release** (`dotnet run -c Release`), no en Debug.
- **Descartar las primeras corridas**: el primer pedido paga JIT, apertura del pool de
  conexiones y el primer plan de consulta. Medir en caliente.
- El p95 se calcula del lado del cliente sobre los 100 tiempos, no promediando.
- El cuerpo de la respuesta se lee **dentro** de la medición: el AC habla del tiempo hasta
  ver los datos, no hasta recibir los headers.
- Ojo con el período del dashboard: si los datos están repartidos en meses y el pedido va
  sin rango, se está midiendo una fracción de la cuenta.

## La base la comparte v1 (ya resuelto)

`GestionGastos-v1` apunta al **mismo** MySQL y a la **misma** base `gestiongastos`, y ya
tenía ahí sus tablas `usuarios`, `categorias`, `movimientos` y su `__EFMigrationsHistory`.
Por eso `dotnet ef database update` falla con `Table 'usuarios' already exists`.

Decisión tomada el 2026-08-07: v2 se queda con la base y las tablas de v1 se borraron con
`backend/db/002-limpiar-tablas-de-v1.sql`. **v1 ya no funciona contra esta base.**

Si hay que rehacerlo en otra máquina:

```powershell
cd C:\Users\PC\Desktop\CursoIA\GestionGastos-v2
& "C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe" -u gestiongastos_app -p -e "source C:/Users/PC/Desktop/CursoIA/GestionGastos-v2/backend/db/002-limpiar-tablas-de-v1.sql"
dotnet ef database update --project backend/GestionGastos.Api
```

## Pendientes de decisión

1. ~~**Mecanismo de sesión**~~ — resuelto el 2026-08-07: **cookie httpOnly con expiración
   deslizante de 24 h**. No es legible por JavaScript (no se filtra ante un XSS) y da
   "24 h de inactividad" (RNF-04) sin plomería de refresh tokens.
2. ~~**Catálogo de categorías predefinidas**~~ — confirmado por el usuario el 2026-08-08.
   Si hay que cambiarlo, se toca `Data/CatalogoDeCategorias.cs` y va una migración nueva.
3. ~~**Decimales y moneda**~~ — superado: son datos de cada fila de `Monedas`.

**No queda ninguna decisión bloqueando.**

## Entorno verificado

| | |
|---|---|
| SDK | .NET 10.0.301 (hay varios SDK instalados; el 10 es el que usa el proyecto) |
| Node | v24.1.0 en Windows, pnpm 10.12.4 |
| MySQL | 8.4.5 en el puerto **3306**, base `gestiongastos`, usuario `gestiongastos_app` |
| API | `http://localhost:5157` |
| Frontend | `http://localhost:5173`, con proxy de `/api` al backend |

Último estado verde (2026-08-10, al cerrar las correcciones de frontend): `dotnet test`
**90/90**, `pnpm test` **71/71**, `pnpm lint` limpio, `pnpm build` OK, y la app probada a
mano en el navegador.

> Las cuentas de prueba que quedan de verificar algo a mano contra MySQL se sacan con
> `backend/db/003-borrar-usuarios-de-prueba.sql` (la API no expone baja de cuenta). Borra
> por dominio `@ejemplo.test`, así que las pruebas conviene hacerlas con mails de ahí.
> Se corrió el 2026-08-09 y dejó la base limpia.

## Cosas aprendidas (no repetir)

- **Una columna nueva conviene que sea nullable si el dominio lo permite.** Con
  `nullable: true` EF genera un `AddColumn` limpio y las filas viejas quedan intactas; es
  el NOT NULL el que arrastra el problema del `defaultValue: ""` de abajo.
- **EF no sabe migrar datos.** Al cambiar la forma de una columna genera un DROP + ADD que
  pierde los valores: la traducción (`'Pesos'` → `'ARS'`) hay que escribirla a mano con
  `migrationBuilder.Sql` antes de borrar la columna vieja.
- **Al agregar una columna NOT NULL, EF genera `defaultValue: ""`.** Para un enum guardado
  como texto eso deja las filas viejas con un valor inválido que revienta al leerlas: hay
  que editar la migración a mano y poner el default real.
- **MySQL no deja tirar un índice que sostiene una foreign key.** Al reemplazar
  `(UsuarioId, Fecha)` por `(UsuarioId, Fecha, MonedaCodigo)` hubo que invertir el orden
  que generó EF: primero crear el nuevo, después borrar el viejo.
- **`Enum.TryParse` acepta el índice numérico**, así que `"0"` pasa como el primer valor
  del enum. Si la API recibe nombres, hay que descartar los dígitos aparte.
- **`TaskStop` no mata el proceso de Windows**, solo el wrapper de WSL. La API queda viva
  y el build siguiente falla con `MSB3027` (archivo bloqueado). Se baja con
  `cmd.exe /c "taskkill /F /IM GestionGastos.Api.exe"`.
- **Un campo con separador de miles no puede ser `<input type="number">`.** El navegador
  considera inválido cualquier texto con separadores y devuelve `value` vacío: no hay
  forma de formatear mientras se escribe. Va `type="text" inputMode="decimal"` y el
  formato/parseo a mano (`utiles/montos.ts`). Ojo con los asserts de los tests: pasan de
  `toHaveValue(1500)` a `toHaveValue('1.500,00')`.
- **`jsdom` 30 no implementa `<dialog>`**: no trae `showModal` ni `close`, no mueve el
  foco y el Escape no dispara `cancel`. Hay un relleno mínimo en `setupTests.ts`. El
  listener del Escape va sobre el **documento**, no sobre el `<dialog>`: en jsdom el foco
  no entra al diálogo y un listener sobre el elemento nunca se dispararía.
- **El atajo `background` pisa el `background-image`.** Poner una flecha propia en un
  `select` con `background-image` no funciona si otra regla más específica setea
  `background: var(--bg)`. Va `background-color`. Lo mismo con `padding` y
  `padding-right`.
- **Los formularios van con `noValidate`.** La validación nativa del navegador cancela el
  submit en silencio (un `min`/`step` incumplido y no pasa nada), y varios AC piden que el
  rechazo **muestre el motivo**. Los mensajes los da el formulario, no el navegador.
- **En los tests de React no alcanza con esperar al formulario**: las categorías y los
  movimientos llegan en efectos posteriores. Hay que esperar a un dato concreto (una
  opción del selector) o el test corre contra una pantalla vacía.
- **En EF, los helpers no se pueden llamar dentro de una expresión de consulta.** Cosas
  como `.Where(m => m.UsuarioId == principal.ObtenerIdRequerido())` o
  `.Select(c => Response.De(c))` no se traducen a SQL: hay que sacar el valor a una
  variable antes y proyectar con un constructor.
- **`mysql` no está en el PATH de PowerShell.** Hay que invocar el .exe con ruta
  completa y con `&` por los espacios:
  `& "C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe" ...`
- **Los comandos de esta bitácora son de PowerShell, pero el prefijo `!` de Claude Code
  los manda a bash.** El `&` de PowerShell ahí es un error de sintaxis. En bash va sin `&`,
  con la ruta del ejecutable en formato WSL (`/mnt/c/...`) y la del `source` en formato
  Windows (`C:/...`), porque esa la resuelve `mysql.exe` y no el shell.
- **Los comandos `dotnet ef` se corren desde `GestionGastos-v2`**, no desde `CursoIA`.
  Desde el directorio equivocado el error es engañoso: "Unable to retrieve project
  metadata. Ensure it's an SDK-style project."
- **Para levantar la API desde WSL: `dotnet.exe run --launch-profile http`.** Con
  `--no-launch-profile` el entorno no es Development y no se cargan los user-secrets
  (la API muere con "Falta la cadena de conexion"). Poner `ASPNETCORE_ENVIRONMENT` como
  variable de WSL no sirve: no cruza al proceso de Windows.
- **Desde WSL no se llega al `localhost` de Windows**: la API escucha en el loopback de
  Windows. Para probarla hay que usar `curl.exe` (el de Windows), no el `curl` de WSL.
- **v1 y v2 comparten la base `gestiongastos` del mismo MySQL.** Antes de tocar el
  esquema, mirar qué hay ahí: no era una base vacía dedicada a v2.
- **`dotnet ef` 10.0.5 funciona con los paquetes de EF Core 9.0.18**, no hace falta
  bajar la herramienta.
- **Un proyecto nuevo que referencia a la API tiene que declarar EF explícito.** Pomelo
  9.0.0 arrastra `EntityFrameworkCore.Relational` 9.0.0 por transitiva y choca con la
  9.0.18 de la API (`MSB3277`). Se arregla poniendo los `PackageReference` de EF en la
  misma versión.
- **La API expone un `Program` público** (para los tests), así que en otra consola con
  top-level statements nombrarlo da `CS0436`. Para user-secrets va
  `Assembly.GetExecutingAssembly()` en vez de `typeof(Program).Assembly`.
- `dotnet test GestionGastos.slnx` **no funciona** (`MSB1009`): hay que pasarle el
  `.csproj` de tests. `dotnet build` sí acepta el `.slnx`.
- `vite.config.ts` tiene que importar `defineConfig` de **`vitest/config`**, no de
  `vite`: con el de `vite`, `tsc -b` rechaza la clave `test` y `pnpm build` falla.
- `erasableSyntaxOnly` está activo en el tsconfig: **no se pueden usar parameter
  properties** (`constructor(readonly x: number)`), hay que declarar el campo aparte.
- Los handlers de minimal API sin parámetros de cuerpo hay que pasarlos como
  `(Delegate)Handler`, si no ASP.NET los toma como `RequestDelegate` y descarta el
  `IResult` (warning ASP0016).
- Se pineó `SQLitePCLRaw.bundle_e_sqlite3` a 3.0.5 en los tests: las 2.1.x arrastran
  una vulnerabilidad alta (GHSA-2m69-gcr7-jv3q).
- **Hay dos servidores MySQL instalados**: `MySQL84` en el 3306 (el que usamos) y
  `MySQL80` en el 3307. Al diagnosticar, confirmar siempre contra cuál se está
  hablando.
- **No pasar scripts SQL por pipe con `-p`.** `Get-Content x.sql | mysql -u root -p`
  hace que el prompt de contraseña se coma la primera línea del script: root no
  autentica, el script no corre y a veces no se ve ningún error. Usar en su lugar:
  `mysql -u root -p -e "source C:/ruta/x.sql"`.
- **PowerShell no soporta `<` como redirección** (`El operador '<' está reservado
  para uso futuro`).
- Se pineó `Microsoft.OpenApi` a 2.11.0 porque la 2.0.0 que arrastra
  `Microsoft.AspNetCore.OpenApi` tiene una vulnerabilidad alta (GHSA-v5pm-xwqc-g5wc).
- Se usa `MySqlServerVersion` fija en lugar de `ServerVersion.AutoDetect`: AutoDetect
  abre una conexión al construir el `DbContext`, con lo que una base caída rompía el
  arranque en vez de dejar que `/health/db` respondiera 503.
- Antes de commitear, grepear el diff staged por credenciales. La contraseña real
  vive **solo** en user-secrets: nunca en `backend/db/*.sql` ni en `appsettings*.json`.
- El toolchain de Windows se puede invocar desde WSL (`dotnet.exe`, `cmd.exe /c pnpm`),
  así se respeta la regla de AGENTS.md de no instalar dependencias desde WSL.

## Para levantar el entorno mañana

```powershell
cd C:\Users\PC\Desktop\CursoIA\GestionGastos-v2

# Backend
cd backend\GestionGastos.Api
dotnet run                  # http://localhost:5157

# Frontend (otra terminal)
cd frontend
pnpm dev                    # http://localhost:5173
```

La cadena de conexión ya está en user-secrets; no hay que reconfigurar nada.
Verificar con `http://localhost:5157/health/db`.

Para volver a correr todo lo que hay:

```powershell
dotnet build GestionGastos.slnx
dotnet test backend\GestionGastos.Tests\GestionGastos.Tests.csproj   # el .slnx no sirve acá
cd frontend; pnpm test; pnpm lint; pnpm build
```

Si la base quedó de otra rama o de otra máquina:
`dotnet ef database update --project backend/GestionGastos.Api` (desde `GestionGastos-v2`).
