using StockControlPrototype.Models;

namespace StockControlPrototype.ViewModels;

public class ProductDetailsVm
{
    public Product Product { get; set; } = null!;
    public List<StockMovement> Movements { get; set; } = new();
}
