# PRD-001: Gestion de Gastos — Aplicacion para el registro y gestion de gastos personales

## Contexto y Problema
Llevar el control de los gastos personales suele requerir anotar todo en una hoja de cálculo o app genérica, categorizando manualmente cada movimiento. Eso genera fricción: la gente deja de registrar gastos porque cargarlos "bien" (con categoría, fecha, etc.) toma tiempo. El resultado es que no se tiene una visión clara de en qué se gasta la plata.

## Objetivos
Registrar gastos e ingresos de dinero de una manera simple mediante un formulario y luego poder visualizarlos y consultarlos de manera sencilla en un dashboard.

## Requerimientos Funcionales

- RF-01: El sistema debe permitir registrar un gasto indicando monto, categoría y fecha (por defecto, la fecha actual) mediante un formulario.
- RF-02: El sistema debe permitir registrar un ingreso indicando monto, categoría y fecha (por defecto, la fecha actual) mediante un formulario.
- RF-03: El sistema debe mostrar un dashboard (en una seccion dedicada al mismo) con el total de gastos agrupado por categoría, representado gráficamente (el tipo de gráfico específico queda a criterio de diseño de UI).
- RF-04: El sistema debe mostrar en el dashboard un balance (ingresos - egresos). ACA METI UN CAMBIO DE PRUEBA
- RF-05: El sistema debe permitir filtrar los datos del dashboard por fecha tambien.
- RF-06: El sistema debe listar los movimientos individuales (gastos e ingresos).
- RF-07: El sistema debe listar los movimientos individuales filtrados por categoría(por defecto todas las categorias).
- RF-08: El sistema debe listar los movimientos individuales filtrados por fecha(por defecto el mes actual).
- RF-09: El sistema debe mostrar en la pantalla principal un resumen rápido con el total ingresado y el total gastado en el mes actual.
- RF-10: El sistema debe requerir autenticación para acceder a cualquier pantalla o función del sistema.
- RF-11: El sistema debe autenticar usuarios mediante email y contraseña.

## Requerimientos No Funcionales
- RNF-01: El dashboard debe cargar en < 2 s p95 con hasta 1000 movimientos registrados, y en < 4 s p95 con hasta 10000 movimientos registrados.
- RNF-02: El registro de un gasto o ingreso debe confirmarse (guardado) en < 1 s p95.
- RNF-03: Las contraseñas deben almacenarse con hash seguro (bcrypt/argon2), nunca en texto plano; la sesión expira tras 24 h de inactividad.

## Criterios de Aceptación
- AC-01 (RF-01): Dado que el usuario completa monto y categoría de un gasto sin especificar fecha, cuando lo guarda, entonces el gasto queda registrado con la fecha del día actual.
- AC-02 (RF-02): Dado que el usuario completa monto y categoría de un ingreso, cuando lo guarda, entonces el monto se refleja en el resumen del mes actual (RF-09) y en el balance del dashboard (RF-04).
- AC-03 (RF-03): Dado que existen gastos cargados en distintas categorías, cuando el usuario abre el dashboard, entonces ve el total correcto por cada categoría, representado gráficamente (independientemente del tipo de gráfico elegido en el diseño).
- AC-04 (RF-04): Dado gastos e ingresos cargados, cuando el usuario abre el dashboard, entonces ve el balance (ingresos - egresos) correcto.
- AC-05 (RF-05): Dado que el usuario selecciona un rango de fechas en el filtro, cuando aplica el filtro, entonces el dashboard actualiza los datos de acuerdo a los movimientos realizados unicamente en el rango de fechas del filtro.
- AC-06 (RF-06): Dado que existen gastos e ingresos cargados, cuando el usuario abre el listado, entonces ve todos los movimientos individuales.
- AC-07 (RF-07): Dado que el usuario selecciona una categoría, cuando aplica el filtro, entonces el listado muestra solo movimientos de esa categoría.
- AC-08 (RF-08): Dado que el usuario no aplica ningún filtro de fecha, cuando abre el listado, entonces ve por defecto los movimientos del mes actual.
- AC-09 (RF-09): Dado que hay gastos e ingresos cargados en el mes actual, cuando el usuario entra a la pantalla principal, entonces ve el total ingresado y el total gastado del mes, coincidentes con los que muestra el dashboard filtrado por "este mes".
- AC-10 (RF-10): Dado un usuario no autenticado, cuando intenta acceder a cualquier pantalla o acción de la aplicación, entonces el sistema lo redirige a login/registro sin permitir la acción.
- AC-11 (RF-10): Dado dos usuarios con movimientos propios, cuando el usuario A inicia sesión, entonces solo ve sus propios movimientos, nunca los del usuario B.
- AC-12 (RF-11): Dado un usuario registrado con email y contraseña, cuando ingresa credenciales incorrectas, entonces el sistema rechaza el acceso sin iniciar sesión.

## Fuera de Alcance
- Conexión con bancos o tarjetas (APIs bancarias, Plaid, etc.)
- Multi-usuario / gastos compartidos
- Notificaciones push o recordatorios
- Exportación a otros formatos (PDF, Excel)
- Presupuestos y alertas de tope
- Entrada de datos por voz o imagen (ej: foto de un ticket/recibo)
- Registro de movimientos por texto libre en lenguaje natural con extracción por IA

## Riesgos y Dependencias
- Riesgo: la categorización manual repetitiva puede generar la misma fricción que se buscaba evitar → mitigación: lista de categorías predefinida y acotada (no campo libre) para que cargar sea rápido.
- Dependencia: base de datos (MySQL) disponible para persistir gastos e ingresos.
