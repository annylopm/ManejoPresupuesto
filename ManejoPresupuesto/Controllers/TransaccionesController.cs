using AutoMapper;
using ManejoPresupuesto.Models;
using ManejoPresupuesto.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ManejoPresupuesto.Controllers
{
    public class TransaccionesController : Controller
    {
        private readonly IServicioUsuario servicioUsuario;
        private readonly IRepositorioCuentas repositorioCuentas;
        private readonly IRepositorioCategorias repositorioCategorias;
        private readonly IRepositorioTransacciones repositorioTransacciones;
        private readonly IMapper mapper;

        public TransaccionesController(IServicioUsuario servicioUsuario, IRepositorioCuentas repositorioCuentas, IRepositorioCategorias repositorioCategorias, IRepositorioTransacciones repositorioTransacciones, IMapper mapper)
        {
            this.servicioUsuario = servicioUsuario;
            this.repositorioCuentas = repositorioCuentas;
            this.repositorioCategorias = repositorioCategorias;
            this.repositorioTransacciones = repositorioTransacciones;
            this.mapper = mapper;
        }

        public IActionResult Index()
        {
            return View();
        }

        //metodo para crear categoria
        public async Task<IActionResult> Crear()
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var modelo = new TransaccionCreacionViewModel();
            modelo.Cuentas = await ObtenerCuentas(usuarioId);
            modelo.Categorias = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId); //obtener las categorias y ponerlas en el select
            return View(modelo);
        }

        [HttpPost]
        public async Task<IActionResult> Crear(TransaccionCreacionViewModel modelo)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();

            if (!ModelState.IsValid) //si no son validos actualizamos los datos del usuario
            {
                modelo.Cuentas = await ObtenerCuentas(usuarioId);
                modelo.Categorias = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId); //recordar que las categorias dependen del tipoOperacion
                return View(modelo);
            }

            var cuenta = await repositorioCuentas.ObtenerPorId(modelo.CuentaId, usuarioId); //validar la cuenta que introdujo el usuario llamando al repositorio de cuentas
            if(cuenta is null) //si no se encuntra la cuenta
            {
                return RedirectToAction("NoEncontrado", "Home"); //regresamos l vista de noEncontrado
            }

            var categoria = await repositorioCategorias.ObtenerPorId(modelo.CategoriaId, usuarioId);
            if(categoria is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            modelo.UsuarioId = usuarioId;

            //si es un gasto, se multiplica por -1 para clasificarlo como negativo
            if(modelo.TipoOperacionId == TipoOperacion.Gasto)
            {
                modelo.Monto *= -1;
            }

            await repositorioTransacciones.Crear(modelo);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var transaccion = await repositorioTransacciones.ObtenerPorId(id, usuarioId);
            //var transaccion = await repositorioTransacciones.ObtenerPorId(id, usuarioId);

            if(transaccion is null)
            {
                return RedirectToAction("NoEnontrado", "Home");
            }

            //se utiliza autoMapper para pasar info de transaccion a TransaccionActualizacionVW
            var modelo = mapper.Map<TransaccionActualizacionViewModel>(transaccion);

            //en caso de que sea un ingreso
            modelo.MontoAnterior = modelo.Monto;

            if(modelo.TipoOperacionId == TipoOperacion.Gasto)
            {
                modelo.MontoAnterior = modelo.Monto * -1;
            }

            modelo.CuentaAnteriorId = transaccion.CuentaId;
            //traer las categorias y las cuentas al modelo
            modelo.Categorias = await ObtenerCategorias(usuarioId,transaccion.TipoOperacionId); //utilizamos transaccion porque es la que viene de los datos del usuario
            modelo.Cuentas = await ObtenerCuentas(usuarioId);

            return View(modelo);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(TransaccionActualizacionViewModel modelo)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            if (!ModelState.IsValid)
            {
                //si el modelo no es valido, obtenemos nuevamente las categorias y cuentas del usuario y las asignamos en la vista
                modelo.Categorias = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId);
                modelo.Cuentas = await ObtenerCuentas(usuarioId);
                return View(modelo);
            }

            //verificar que la cuenta exista
            var cuenta = await repositorioCuentas.ObtenerPorId(modelo.CuentaId, usuarioId);
            if(cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            //verificar que la categoria exista
            var categoria = await repositorioCategorias.ObtenerPorId(modelo.CategoriaId, usuarioId);
            if(categoria is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var transaccion = mapper.Map<Transaccion>(modelo);
            if(modelo.TipoOperacionId == TipoOperacion.Gasto)
            {
                modelo.Monto *= -1;
            }

            await repositorioTransacciones.Actualizar(transaccion, modelo.MontoAnterior, modelo.CuentaAnteriorId);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Borrar(int id)
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var transaccion = await repositorioTransacciones.ObtenerPorId(id, usuarioId);

            if(transaccion is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            await repositorioTransacciones.Borrar(id);
            return RedirectToAction("Index");
        }

        public async Task<IEnumerable<SelectListItem>> ObtenerCuentas(int usuarioId)
        {
            var cuentas = await repositorioCuentas.Buscar(usuarioId);
            return cuentas.Select(x => new SelectListItem(x.Nombre, x.Id.ToString()));
        }

        //metodo para obtener las categorias en el selectList
        private async Task<IEnumerable<SelectListItem>> ObtenerCategorias(int usuarioId, TipoOperacion tipoOperacion)
        {
            var categorias = await repositorioCategorias.Obtener(usuarioId, tipoOperacion);
            return categorias.Select(x => new SelectListItem(x.Nombre, x.Id.ToString()));
        }


        //metodo para traer las categorias correspondientes al tipoOperacion
        [HttpPost]
        public async Task<IActionResult> ObtenerCategorias([FromBody] TipoOperacion tipoOperacion) //frombody, recibe el cuerpo de la peticion
        {
            var usuarioId = servicioUsuario.ObtenerUsuarioId();
            var categorias = await ObtenerCategorias(usuarioId, tipoOperacion); //llamando al metodo para obtener las categorias que corresponden al usuario y tipoOperacion
            return Ok(categorias); //estamos enviando una respuesta exitosa, y en el cuerpo de esa respuesta se envían las categorias
        }
    }
}
