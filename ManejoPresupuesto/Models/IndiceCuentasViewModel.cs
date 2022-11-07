namespace ManejoPresupuesto.Models
{
    public class IndiceCuentasViewModel
    {
        public string TipoCuenta { get; set; }
        public IEnumerable<Cuenta> Cuentas { get; set; } //Mandar traer la coleccion de cuentas que pertenecen al TipoCuenta
        public decimal Balance => Cuentas.Sum(x => x.Balance); //sumar los balances con LINQ
    }
}
