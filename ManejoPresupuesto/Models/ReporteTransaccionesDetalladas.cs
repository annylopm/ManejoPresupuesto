namespace ManejoPresupuesto.Models
{
    public class ReporteTransaccionesDetalladas
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        //Ienumerable para las transacciones por fecha
        public IEnumerable<TransaccionesPorFecha> TransaccionesAgrupadas { get; set; }

        //Calculamos el total de depositos/retiros de una fecha
        public decimal BalanceDepositos => TransaccionesAgrupadas.Sum(x => x.BalanceDepositos);
        public decimal BalanceRetiros => TransaccionesAgrupadas.Sum(x => x.BalanceRetiros);

        //Calcular el total de transacciones
        public decimal Total => BalanceDepositos - BalanceRetiros;
        
        //clase para las transacciones que se han hecho en una fecha (depositos o ingresos Y retiros o gastos)
        public class TransaccionesPorFecha
        {
            public DateTime FechaTransaccion { get; set; }  
            public IEnumerable<Transaccion> Transacciones { get; set; }

            //Balance para los ingresos o depositos, se seleccionan los que corresponden a ingreso y
            //luego se suma el monto de los seleccionados
            public decimal BalanceDepositos => 
                Transacciones.Where(x => x.TipoOperacionId == TipoOperacion.Ingreso).Sum(x => x.Monto);

            //Balance para los retiros o gastos, se seleccionan los que corresponden a gastos y
            //luego se suma el monto de los seleccionados
            public decimal BalanceRetiros => 
                Transacciones.Where(x => x.TipoOperacionId == TipoOperacion.Gasto).Sum(x => x.Monto);
        }
    }
}
