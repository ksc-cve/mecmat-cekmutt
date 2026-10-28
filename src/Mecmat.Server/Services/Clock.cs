namespace Mecmat.Services;

public static class Clock
{
    /// <summary>The group works in Bangkok time; "today" for ids, due dates and reminders is
    /// the Bangkok calendar day, not the server's UTC day.</summary>
    public static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");

    public static DateTime NowBangkok() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Bangkok);
    public static DateOnly TodayBangkok() => DateOnly.FromDateTime(NowBangkok());
}
