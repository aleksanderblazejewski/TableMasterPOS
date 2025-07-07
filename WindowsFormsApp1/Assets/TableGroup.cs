using System.Collections.Generic;

public class TableGroup
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<int> AssignedWaiterIds { get; set; } = new();
    public List<int> AssignedTableIds { get; set; } = new(); // <-- DODANE
}