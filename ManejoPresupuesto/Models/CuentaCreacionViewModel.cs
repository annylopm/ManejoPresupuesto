using Microsoft.AspNetCore.Mvc.Rendering;

namespace ManejoPresupuesto.Models
{
    public class CuentaCreacionViewModel : Cuenta
    {
        //selectListItem permite crear una lista despegable
        public IEnumerable<SelectListItem> TiposCuentas { get; set; } 
    }
}
