using System;
using System.Collections.Generic;
using System.Text;

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
