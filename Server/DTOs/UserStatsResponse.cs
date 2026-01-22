namespace Server.DTOs;

public class UserStatsResponse
{
    public int TotalUsers { get; set; }
    public int TotalAdmins { get; set; }
    public int VerifiedUsers { get; set; }
    public int UnverifiedUsers { get; set; }
    public int NewUsersThisMonth { get; set; }
}
