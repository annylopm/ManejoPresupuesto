using Dapper;
using ManejoPresupuesto.Models;
using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic;
using System.Collections.Generic;

namespace ManejoPresupuesto.Servicios
{
    public interface IRepositorioTiposCuentas
    {
        Task Actualizar(TipoCuenta tipoCuenta);
        Task Borrar(int id);
        Task Crear(TipoCuenta tipoCuenta);
        Task<bool> Existe(string nombre, int usuarioId);
        Task<IEnumerable<TipoCuenta>> Obtener(int usuarioId);
        Task<TipoCuenta> ObtenerPorId(int id, int usuarioId);
        Task Ordenar(IEnumerable<TipoCuenta> tipoCuentasOrdenados);
    }

    //Configuracion para conectar a la DB
    public class RepopsitorioTiposCuentas: IRepositorioTiposCuentas
    {
        private readonly string connectionString;
        public RepopsitorioTiposCuentas(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        //Pasar los valores a la DB para crear una cuenta 
        public async Task Crear(TipoCuenta tipoCuenta)
        {
            using var connection = new SqlConnection(connectionString); //establecer conexion con DB
            //Primero se manda llamar el Procedimiento almacenado que se creo en la DB, luego se crea un objeto anonimo en donde
            //Solo pasamos los valores necesarios para el procedimiento almacenado y al final indicamos que se esta usando un 
            //procedimiento almacenado
            var id = await connection.QuerySingleAsync<int>("TiposCuentas_Insertar", new { usuarioId = tipoCuenta.UsuarioId, nombre = tipoCuenta.Nombre}, commandType: System.Data.CommandType.StoredProcedure); //pasando los valores que se van a registrar en la DB 
            

            tipoCuenta.Id = id;

        }

        //Verificar si ya hay un registro con el mismo tipoCuenta
        public async Task<bool> Existe(string nombre, int usuarioId)
        {
            using var connection = new SqlConnection(connectionString); //establecer la conexion con DB
            var existe = await connection.QueryFirstOrDefaultAsync<int>(@"SELECT 1 FROM TiposCuentas WHERE Nombre = @Nombre AND UsuarioId = @UsuarioId;", new { nombre, usuarioId}); //verificar que existe un registro

            return existe == 1; //si encuentra un registro, retorna 1. Si no existe, se retorna 0
        }

        //Obtener los tipos de cuenta registrados por un usuario
        public async Task<IEnumerable<TipoCuenta>> Obtener(int usuarioId)
        {
            using var connection = new SqlConnection(connectionString);
            return await connection.QueryAsync<TipoCuenta>(@"SELECT Id, Nombre, Orden FROM TiposCuentas WHERE UsuarioId = @UsuarioId ORDER BY Orden;", new {usuarioId});
        }

        //actualizar tipos cuentas
        public async Task Actualizar(TipoCuenta tipoCuenta)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync(@"UPDATE TiposCuentas SET Nombre = @Nombre WHERE Id = @Id;", tipoCuenta);
        }

        //obtener cuenta por id
        public async Task<TipoCuenta> ObtenerPorId(int id, int usuarioId)
        {
            using var connection = new SqlConnection(connectionString);
            return await connection.QueryFirstOrDefaultAsync<TipoCuenta>($"SELECT Id, Nombre, Orden FROM TiposCuentas WHERE Id = @Id AND UsuarioId = @UsuarioId;", new { id, usuarioId});
        }

        //metodo para borrar registros
        public async Task Borrar(int id)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync($"DELETE TiposCuentas WHERE Id = @Id", new {id});

        }

        //metodo para los registros ordenados para la DB
        public async Task Ordenar(IEnumerable<TipoCuenta> tipoCuentasOrdenados)
        {
            var query = "UPDATE TiposCuentas SET Orden = @Orden WHERE Id = @Id;";
            using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync(query, tipoCuentasOrdenados);
        }
    }
}
