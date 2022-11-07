namespace ManejoPresupuesto.Servicios
{
    public interface IServicioUsuario
    {
        int ObtenerUsuarioId();
    }

    public class ServicioUsuarios: IServicioUsuario
    {
        public int ObtenerUsuarioId()
        {
            return 1;
        }
    }
}
