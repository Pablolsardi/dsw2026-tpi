using System.Text.Json;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Data;

public class HolidayService : IHolidayService
{
    private readonly HashSet<DateOnly> _holidays;

    public HolidayService(string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        var items = JsonSerializer.Deserialize<List<HolidayDto>>(json) ?? [];
        _holidays = items.Select(x => DateOnly.Parse(x.Date)).ToHashSet();
    }

    public bool IsHoliday(DateOnly date) => _holidays.Contains(date);

    private record HolidayDto(string Date, string Name);
}