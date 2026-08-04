using Dsw2026Tpi.Data.Options;
using Dsw2026Tpi.Domain.Interfaces;
using System.Text.Json;

namespace Dsw2026Tpi.Data;

public class HolidayService : IHolidayService
{
    private readonly HashSet<DateOnly> _holidays;

    public HolidayService(string jsonPath)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, jsonPath);
        var json = File.ReadAllText(fullPath);
        var items = JsonSerializer.Deserialize<List<HolidayDto>>(json, JsonOptions.JsonSerializerOptions) ?? [];
        _holidays = items.Select(x => DateOnly.Parse(x.Date)).ToHashSet();
    }

    public bool IsHoliday(DateOnly date) => _holidays.Contains(date);

    private record HolidayDto(string Date, string Name);
}