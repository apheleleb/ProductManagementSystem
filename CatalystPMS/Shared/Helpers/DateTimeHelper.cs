namespace CatalystPMS.Shared.Helpers;

public static class DateTimeHelper
{
    public static string ToRelativeTime(DateTime utcDate)
    {
        var diff = DateTime.UtcNow - utcDate;

        return diff.TotalSeconds switch
        {
            < 60 => "just now",
            < 3600 => $"{(int)diff.TotalMinutes} minute{((int)diff.TotalMinutes == 1 ? "" : "s")} ago",
            < 86400 => $"{(int)diff.TotalHours} hour{((int)diff.TotalHours == 1 ? "" : "s")} ago",
            < 2592000 => $"{(int)diff.TotalDays} day{((int)diff.TotalDays == 1 ? "" : "s")} ago",
            _ => utcDate.ToString("yyyy-MM-dd")
        };
    }

    public static string ToSastString(DateTime utcDate)
    {
        var sast = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(utcDate, sast).ToString("yyyy-MM-dd HH:mm");
    }
}