using Dapper;
using ManejoPresupuesto.Models;
using Microsoft.Data.SqlClient;

namespace ManejoPresupuesto.Servicios
{
    public interface IRepositorioCuentas
    {
        Task Actualizar(CuentaCreacionViewModel cuenta);
        Task Borrar(int id);
        Task<IEnumerable<Cuenta>> Buscar(int usuarioId);
        Task Crear(Cuenta cuenta);
        Task<Cuenta> ObtenerPorId(int id, int usuarioId);
    }
    public class RepositorioCuentas : IRepositorioCuentas
    {
        private readonly string connectionString;
        public RepositorioCuentas(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        //metodo para crear una cuenta de un tipo de cuenta respectivamente
        public async Task Crear(Cuenta cuenta)
        {
            using var connection = new SqlConnection(connectionString);
            var id = await connection.QuerySingleAsync<int>(@"INSERT INTO Cuentas (Nombre, TipoCuentaId, Descripcion, Balance)
                                                            VALUES (@Nombre, @TipoCuentaId, @Descripcion, @Balance);
                                                            SELECT SCOPE_IDENTITY();", cuenta); //pasamos los valores de cuenta para que se llene la consulta
            cuenta.Id = id; //obtener el id

        }

        //metodo para buscar los tipos de cuenta que tiene un usuario por su Id 
        public async Task<IEnumerable<Cuenta>> Buscar(int usuarioId)
        {
            using var connection = new SqlConnection(connectionString);
            return await connection.QueryAsync<Cuenta>(@"SELECT Cuentas.Id, Cuentas.Nombre, Balance, tc.Nombre AS TipoCuenta
                                                        FROM Cuentas
                                                        INNER JOIN TiposCuentas tc
                                                        ON tc.Id = Cuentas.TipoCuentaId
                                                        WHERE tc.UsuarioId = @UsuarioId
                                                        ORDER BY tc.Orden", new { usuarioId });
        }

        public async Task<Cuenta> ObtenerPorId(int id, int usuarioId)
        {
            using var connection = new SqlConnection(connectionString);
            return await connection.QueryFirstOrDefaultAsync<Cuenta>(@"SELECT Cuentas.Id, Cuentas.Nombre, Balance, Descripcion, tc.Id
                                                        FROM Cuentas
                                                        INNER JOIN TiposCuentas tc
                                                        ON tc.Id = Cuentas.TipoCuentaId
                                                        WHERE tc.UsuarioId = @UsuarioId AND Cuentas.Id = @Id", new{ id, usuarioId} );
        }

        //Tipo CuentaCreacionVM porque se va a actualizar
        public async Task Actualizar(CuentaCreacionViewModel cuenta)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync(@"UPDATE Cuentas
                                            SET Nombre = @Nombre, Balance = @Balance, Descripcion = @Descripcion, TipoCuentaId = @TipoCuentaId
                                            WHERE Id = @Id;", cuenta);
        }

        public async Task Borrar(int id)
        {
            var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync(@"DELETE Cuentas WHERE Id = @Id;", new {id});
        }
    }
}
