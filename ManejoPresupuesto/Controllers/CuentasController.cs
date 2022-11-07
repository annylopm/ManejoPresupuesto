using AutoMapper;
using ManejoPresupuesto.Models;
using ManejoPresupuesto.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Reflection;

namespace ManejoPresupuesto.Controllers
{
    public class CuentasController : Controller
    {
        private readonly IRepositorioTiposCuentas repositorioTiposCuentas;
        private readonly IServicioUsuario servicioUsuario;
        private readonly IRepositorioCuentas repositorioCuentas;
        private readonly IMapper mapper;
        private readonly IRepositorioTransacciones repositorioTransacciones;

        //Constructor
        public CuentasController(IRepositorioTiposCuentas repositorioTiposCuentas, IServicioUsuario servicioUsuario,
            IRepositorioCuentas repositorioCuentas, IMapper mapper, IRepositorioTransacciones repositorioTransacciones)
        {
            this.repositorioTiposCuentas = repositorioTiposCuentas;
            this.servicioUsuario = servicioUsuario;
            this.repositorioCuentas = repositorioCuentas;
            this.mapper = mapper;
            this.repositorioTransacciones = repositorioTransacciones;
        }

        //metodo para el index
        public async Task<IActionResult> Index()
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var cuentasConTipoCuenta = await repositorioCuentas.Buscar(usuarioId); //buscar los tipos de cuenta del usuario

            var modelo = cuentasConTipoCuenta
                .GroupBy(x => x.TipoCuenta) //agrupamos por TipoCuenta
                .Select(grupo => new IndiceCuentasViewModel //tomamos de referencia a la clase IndiceCuentas
                {
                    TipoCuenta = grupo.Key, //key = TipoCuenta, con el que se hizo el GroupBy
                    Cuentas = grupo.AsEnumerable() //obtener el IEnumerable de los tiposCuentas
                    //No es necesario hacer Balance pq ese se hizo automaticamente
                }).ToList();

            return View(modelo);
        }

        //Metodo para permitir al usuario observar los movimientos de las cuentas por mes y año
        public async Task<IActionResult> Detalle(int id, int mes, int year)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var cuenta = await repositorioCuentas.ObtenerPorId(id, usuarioId);
            if (cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            DateTime fechaInicio;
            DateTime fechaFin;

            if (mes <= 0 || mes > 12 || year <= 1999)
            {
                var hoy = DateTime.Today;
                fechaInicio = new DateTime(hoy.Year, hoy.Month, 1); //fechaInicio sera el primer día del mes actual
            }
            else
            {
                fechaInicio = new DateTime(year, mes, 1);
            }
            fechaFin = fechaInicio.AddMonths(1).AddDays(-1); //indica que la fechaFin sera el ultimo día del mes de fechaInicio

            var obtenerTransaccionesPorCuenta = new ObtenerTransaccionesPorCuenta()
            {
                CuentaId = id,
                UsuarioId = usuarioId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            };

            var transacciones = await repositorioTransacciones.ObtenerPorCuentaId(obtenerTransaccionesPorCuenta); //enviamos el modelo creado para obtener la cuenta, que tiene los parametros para el query

            var modelo = new ReporteTransaccionesDetalladas();
            ViewBag.Cuenta = cuenta.Nombre;

            var transaccionesPorFecha = transacciones.OrderBy(x => x.FechaTransaccion)
                .GroupBy(x => x.FechaTransaccion) //agruparlas por fechas
                .Select(grupo => new ReporteTransaccionesDetalladas.TransaccionesPorFecha()
                {
                    FechaTransaccion = grupo.Key,
                    Transacciones = grupo.AsEnumerable()
                });

            modelo.TransaccionesAgrupadas = transaccionesPorFecha;
            modelo.FechaInicio = fechaInicio;
            modelo.FechaFin = fechaFin;

            return View(modelo);

        }


        //Metodo para colocar en el select los TiposCuentas que pertenecen al usuario
        [HttpGet]
        public async Task <IActionResult> Crear()
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId(); //obtenemos usuario
            var modelo = new CuentaCreacionViewModel(); //instanciamos clase cuentaCreacionVM
            modelo.TiposCuentas = await ObtenerTiposCuentas(usuarioId); //asignamos los tiposCuentas del usuario al modelo

            return View(modelo);
        }

        //Metodo para crear una cuenta
        [HttpPost]
        public async Task<IActionResult> Crear(CuentaCreacionViewModel cuenta)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var tipoCuenta = await repositorioTiposCuentas.ObtenerPorId(cuenta.TipoCuentaId, usuarioId); //validar el tipoCuenta que introduce el usuario

            if(tipoCuenta == null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            
            if (!ModelState.IsValid) //si son validos los datos
            {
                //obtener los tipos cuentas del usuario
                cuenta.TiposCuentas = await ObtenerTiposCuentas(usuarioId);
                return View(cuenta);
            }

            //si toda la validacion es correcta
            await repositorioCuentas.Crear(cuenta);
            return RedirectToAction("Index");
        }

        //metodo para editar una cuenta
        public async Task<IActionResult> Editar(int id)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var cuenta = await repositorioCuentas.ObtenerPorId(id, usuarioId);

            if(cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            ////MAPPEAR MANUAL, de un objeto a otro
            ////modelo que va a esperar la vista crearViewModel
            //var modelo = new CuentaCreacionViewModel() //acceder a la clase para mostrar al usuario los tipos de cuenta que tiene
            //{ //se pasan los valores de la cuenta a cuentaCreacionVM
            //    Id = cuenta.Id,
            //    Nombre = cuenta.Nombre,
            //    TipoCuentaId = cuenta.TipoCuentaId,
            //    Descripcion = cuenta.Descripcion,
            //    Balance = cuenta.Balance
            //};

            //Mappear con AUTOMAPPER
            var modelo = mapper.Map<CuentaCreacionViewModel>(cuenta);

            modelo.TiposCuentas = await ObtenerTiposCuentas(usuarioId);
            return View(modelo);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(CuentaCreacionViewModel cuentaEditar)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            
            //validar que la cuenta corresponde al usuario
            var cuenta = await repositorioCuentas.ObtenerPorId(cuentaEditar.Id, usuarioId); 
            if(cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            //validar tipo cuenta
            var tipoCuenta = await repositorioTiposCuentas.ObtenerPorId(cuentaEditar.TipoCuentaId, usuarioId);
            if(tipoCuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            //actualizar los datos
            await repositorioCuentas.Actualizar(cuentaEditar);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Borrar(int id) //confirmacion de borrar cuenta
        {
            //necesitamos comprobar que el usuario que elimina la cuenta sea el mismo que el que la creo
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var cuenta = await repositorioCuentas.ObtenerPorId(id, usuarioId);

            if(cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            return View(cuenta);
        }

        [HttpPost]
        public async Task<IActionResult> BorrarCuenta(int id) //borrar cuenta definitiva
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var cuenta = await repositorioCuentas.ObtenerPorId(id, usuarioId);

            if(cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            await repositorioCuentas.Borrar(id);
            return RedirectToAction("Index");
        }

        //Obtener los tipos cuentas para poder clasificar la cuenta creada
        private async Task<IEnumerable<SelectListItem>> ObtenerTiposCuentas( int usuarioId)
        {
            var tiposCuentas = await repositorioTiposCuentas.Obtener(usuarioId);
            return tiposCuentas.Select(x => new SelectListItem(x.Nombre, x.Id.ToString()));

        }
    }
}
