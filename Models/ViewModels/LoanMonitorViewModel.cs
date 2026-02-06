using Sistema_Almacen.Models;

namespace Sistema_Almacen.Models.ViewModels
{
    public class LoanMonitorViewModel
    {
        public List<Prestamo> ActiveLoans { get; set; } = new List<Prestamo>(); // Green / Yellow
        public List<Prestamo> OverdueLoans { get; set; } = new List<Prestamo>(); // Red
        public List<Prestamo> HistoryLoans { get; set; } = new List<Prestamo>(); // Closed

        public int TotalActive => ActiveLoans.Count;
        public int TotalOverdue => OverdueLoans.Count;
    }
}
