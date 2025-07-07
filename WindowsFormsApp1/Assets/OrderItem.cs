public class OrderItem
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public bool IsServed { get; set; } = false;

    public OrderItem(string name, decimal price)
    {
        Name = name;
        Price = price;
    }
}
