using Microsoft.EntityFrameworkCore;

namespace ChatAISystem.Helper
{
    public class Pagination<T> : List<T>, IPagination
    {
        public int InitialPage { get; private set; }
        public int TotalPages { get; private set; }

        public Pagination(List<T> items, int counter, int initialPage, int regQuantity)
        {
            InitialPage = initialPage;
            TotalPages = (int)Math.Ceiling(counter / (double)regQuantity);
            this.AddRange(items);
        }

        public bool PreviousPages => InitialPage > 1;
        public bool LaterPages => InitialPage < TotalPages;

        public static async Task<Pagination<T>> CreatePagination(IQueryable<T> source, int initialPage, int regQuantity)
        {
            // Manejo seguro de valores nulos en la consulta
            var safeSource = source.Select(item => item == null ? (T)(object)new { } : item); // Crear una proyección segura si es necesario

            var counter = await safeSource.CountAsync(); // Contar los elementos en la fuente

            // Aplicar paginación y manejar valores nulos al materializar la consulta
            var items = await safeSource
                .Skip((initialPage - 1) * regQuantity)
                .Take(regQuantity)
                .ToListAsync();

            // Retornar la instancia de la clase Pagination
            return new Pagination<T>(items, counter, initialPage, regQuantity);
        }
    }
}
