
namespace Dsw2026Tpi.Application.Helpers;

public static class AvailabilityHelper
{
    public static List<(TimeOnly Start, TimeOnly End)> GetSlots(TimeOnly start, TimeOnly end)
    {
        var slots = new List<(TimeOnly, TimeOnly)>();
        var current = start;

        while (current < end)
        {
            var next = current.AddMinutes(30);
            slots.Add((current, next));
            current = next;
        }

        return slots;
    }

    public static bool TryParseDay(string day, out DayOfWeek result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(day))
            return false;

        return DayMap.TryGetValue(Normalize(day), out result);
    }

    public static string FormatDay(DayOfWeek day)
        => DayMap.First(x => x.Value == day).Key;

    public static bool IsValidRange(TimeOnly start, TimeOnly end)
    {
        if(start < end && ((end - start).TotalMinutes % 30 ==  0))
        {
            return true;
        }
        return false;
    }

    public static List<DateOnly> GetDatesInMonth(DayOfWeek day, DateOnly reference)
    {
        var today = reference;
        var dates = new List<DateOnly>();

        while (today.Month == reference.Month)
        {
            if(today.DayOfWeek == day)
               dates.Add(today);

            today = today.AddDays(1);
        }

        return dates;
    }

    public static bool HasOverlap(IEnumerable<(DayOfWeek Day, TimeOnly Start, TimeOnly End)> ranges) 
    {
        var list = ranges.ToList();
        for(int i = 0; i < list.Count; i++)
        {
            for(int j = i+1; j < list.Count; j++)
            {
                var a = list[i];
                var b = list[j];
                if (a.Day == b.Day && a.Start < b.End && b.Start < a.End)
                    return true;
            }
        }
        return false;
    }

    private static string Normalize(string text)
    {
        return text.Trim().ToUpperInvariant()
            .Replace('Á', 'A').Replace('É', 'E').Replace('Í', 'I')
            .Replace('Ó', 'O').Replace('Ú', 'U');
    }

    private static readonly Dictionary<string, DayOfWeek> DayMap = new()
    {
        ["LUNES"] = DayOfWeek.Monday,
        ["MARTES"] = DayOfWeek.Tuesday,
        ["MIERCOLES"] = DayOfWeek.Wednesday,
        ["JUEVES"] = DayOfWeek.Thursday,
        ["VIERNES"] = DayOfWeek.Friday,
        ["SABADO"] = DayOfWeek.Saturday,
        ["DOMINGO"] = DayOfWeek.Sunday
    };
}
