using System;
using System.Collections.Generic;
using System.Linq;

public class Order
{
    public int Id { get; set; }                           // <--- NOWE pole
    public int TableId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public bool IsPaid { get; set; } = false;
    public DateTime CreatedAt { get; set; }

    public Order(int tableId)
    {
        TableId = tableId;
    }

    public decimal TotalPrice => Items.Sum(item => item.Price);

    public bool AllServed => Items.All(item => item.IsServed);

    public void AddItem(string name, decimal price)
    {
        Items.Add(new OrderItem(name, price));
    }

    public void MarkItemServed(int index)
    {
        if (index >= 0 && index < Items.Count)
            Items[index].IsServed = true;
    }

    public void MarkAllServed()
    {
        foreach (var item in Items)
            item.IsServed = true;
    }

    public void Pay()
    {
        IsPaid = true;
    }
}
