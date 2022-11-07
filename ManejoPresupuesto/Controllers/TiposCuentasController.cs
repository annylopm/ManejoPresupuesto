using Dapper;
using ManejoPresupuesto.Models;
using ManejoPresupuesto.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Data.SqlClient;

namespace ManejoPresupuesto.Controllers
{
    public class TiposCuentasController : Controller
    {
        private readonly IRepositorioTiposCuentas repositorioTiposCuentas;
        private readonly IServicioUsuario servicioUsuario;

        //private readonly string connectionString;
        public TiposCuentasController(IRepositorioTiposCuentas repositorioTiposCuentas, IServicioUsuario servicioUsuario)
        {
            this.repositorioTiposCuentas = repositorioTiposCuentas;
            this.servicioUsuario = servicioUsuario;
        }

        //Listar tipoCuentas del usuario
        public async Task<IActionResult> Index()
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var tiposCuentas = await repositorioTiposCuentas.Obtener(usuarioId);

            return View(tiposCuentas);
        }

        public IActionResult Crear()
        {

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Crear(TipoCuenta tipoCuenta)
        {
            //Validacion por atributo
            if (!ModelState.IsValid)
            {
                return View(tipoCuenta);
            }

            tipoCuenta.UsuarioId = servicioUsuario.ObtenerUsuarioId();

            //Accion para enviar datos para comprobar que existe un tipo cuenta en el registro de un usuario, recibe true o false
            var yaExisteTipoCuenta = await repositorioTiposCuentas.Existe(tipoCuenta.Nombre, tipoCuenta.UsuarioId);

            if (yaExisteTipoCuenta) //aqui se comprueba
            {
                //si ya existe el tipo de cuenta, se envia un mensjae de error, indicando el campo al que pertenecería el error
                ModelState.AddModelError(nameof(tipoCuenta.Nombre), $"El nombre {tipoCuenta.Nombre} ya existe.");

                return View(tipoCuenta); //se retorna la vista con el tipoCuenta
            }

            //Permite crear el tipo de cuenta si no esta registrado ese tipoCuenta
            await repositorioTiposCuentas.Crear(tipoCuenta);

            return RedirectToAction("Index");

        }

        [HttpGet]
        public async Task<ActionResult> Editar(int id) //permite cargar el registro por su id
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var tipoCuenta = await repositorioTiposCuentas.ObtenerPorId(id, usuarioId);
            if(tipoCuenta is null) //Si no existen los datos
            {
                return RedirectToAction("NoEncontrado", "Home");
            }
            return View(tipoCuenta);
        }

        [HttpPost]
        public async Task<ActionResult> Editar(TipoCuenta tipoCuenta)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var tipoCuentaExiste = await repositorioTiposCuentas.ObtenerPorId(tipoCuenta.Id, usuarioId);

            if(tipoCuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            await repositorioTiposCuentas.Actualizar(tipoCuenta);
            return RedirectToAction("Index");
        }

        //Enviar a vista de confirmacion para borrar el resgitro tipo cuenta
        public async Task<IActionResult> Borrar(int id)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var tipoCuenta = await repositorioTiposCuentas.ObtenerPorId(id, usuarioId);

            if(tipoCuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            return View(tipoCuenta);
        }

        //Para borrar el registro de Tipo cuenta
        [HttpPost]
        public async Task<IActionResult> BorrarTipoCuenta(int id)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var tipoCuenta = await repositorioTiposCuentas.ObtenerPorId(id, usuarioId);

            if(tipoCuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            await repositorioTiposCuentas.Borrar(id); //se borra el registro
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> VerificarExisteCuenta(String nombre)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var yaExisteTipoCuenta = await repositorioTiposCuentas.Existe(nombre, usuarioId);

            if (yaExisteTipoCuenta)
            {
                return Json($"El nombre {nombre} ya existe"); //json interpreta datos como una cadena de texto
            }
            return Json(true);
        }

        //Hacer que permanezca el orden en que el usuario ha dejado la lista
        [HttpPost]
        public async Task<IActionResult> Ordenar([FromBody] int[] ids) //frombody: recibe del cuerpo la peticion; recibe un arreglo de Id's
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var tiposCuentas = await repositorioTiposCuentas.Obtener(usuarioId);
            var idsTiposCuentas = tiposCuentas.Select(x => x.Id); //obtenemos los ids de las cuentas de cada usuario

            //Verificar que los ids realmente pertenezcan al usuario
            //Se hace una comparacion de los ids que vienen del frontend con los ids que ya existen en la DB
            var idsTiposCuentasNoPertenecenAlUsuario = ids.Except(idsTiposCuentas).ToList();
            if(idsTiposCuentasNoPertenecenAlUsuario.Count > 0)//si existe algun id que no pertenezca
            {
                //hay problemas
                return Forbid(); //Prohibir
            }

            //seleccionar y asociar el valor e indice de los ids con el id y orden respectivamente
            var tiposCuentasOrdenadas = ids.Select((valor, indice) => new TipoCuenta() { Id = valor, Orden = indice + 1 }).AsEnumerable();

            await repositorioTiposCuentas.Ordenar(tiposCuentasOrdenadas);

            return Ok();
        }


    }
}
