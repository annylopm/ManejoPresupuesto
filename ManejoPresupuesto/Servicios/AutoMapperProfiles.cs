using AutoMapper;
using ManejoPresupuesto.Models;

namespace ManejoPresupuesto.Servicios
{
    public class AutoMapperProfiles: Profile
    {
        public AutoMapperProfiles()
        {
            //Indicar de donde a donde se va a mappear
            //En este caso se mappea desde Cuenta a CuentaCreacionWM
            CreateMap<Cuenta, CuentaCreacionViewModel>();
            CreateMap<TransaccionActualizacionViewModel, Transaccion>().ReverseMap(); //ReverseMap permite mappear de ambos lados
        }
    }
}
