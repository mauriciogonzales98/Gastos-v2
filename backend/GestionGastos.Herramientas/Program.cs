using GestionGastos.Api.Data;
using GestionGastos.Herramientas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// Herramientas del paso final (AC-32 a AC-34). No forma parte de la aplicacion: es de uso
// manual y no se despliega.
//
//   dotnet run --project backend/GestionGastos.Herramientas -- sembrar 1000
//   dotnet run --project backend/GestionGastos.Herramientas -- medir
//   dotnet run --project backend/GestionGastos.Herramientas -- limpiar
//
// `medir` necesita la API levantada en Release (`dotnet run -c Release`): en Debug los
// numeros son de otro programa.

if (args.Length == 0)
{
    Console.Error.WriteLine("Uso: sembrar <cantidad> | medir [urlBase] | limpiar");
    return 1;
}

switch (args[0])
{
    case "sembrar":
        if (args.Length < 2 || !int.TryParse(args[1], out var cantidad) || cantidad < 0)
        {
            Console.Error.WriteLine("Uso: sembrar <cantidad>");
            return 1;
        }

        await using (var db = AbrirBase())
        {
            await Sembrador.Sembrar(db, cantidad);
        }

        return 0;

    case "limpiar":
        await using (var db = AbrirBase())
        {
            await Sembrador.Limpiar(db);
        }

        return 0;

    case "medir":
        var urlBase = args.Length > 1 ? args[1] : "http://localhost:5157";
        return await Medidor.Medir(urlBase);

    default:
        Console.Error.WriteLine($"Comando desconocido: {args[0]}");
        return 1;
}

static GestionGastosDbContext AbrirBase()
{
    // La misma cadena de conexion que la API: el UserSecretsId del csproj es el suyo, asi
    // que no hay que configurar ni copiar nada.
    // GetExecutingAssembly y no typeof(Program): la API tambien expone un `Program` y
    // nombrarlo aca choca con el que generan los top-level statements (CS0436).
    var configuracion = new ConfigurationBuilder()
        .AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly())
        .AddEnvironmentVariables()
        .Build();

    var connectionString = configuracion.GetConnectionString("MySql")
        ?? throw new InvalidOperationException(
            "Falta la cadena de conexion 'ConnectionStrings:MySql' en user-secrets. " +
            "Es la misma que usa la API: ver AGENTS.md.");

    var opciones = new DbContextOptionsBuilder<GestionGastosDbContext>()
        .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 5)))
        .Options;

    return new GestionGastosDbContext(opciones);
}
