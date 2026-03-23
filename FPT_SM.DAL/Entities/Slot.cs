namespace FPT_SM.DAL.Entities;

public class Slot
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public ICollection<Class> Classes { get; set; } = new List<Class>();
}
