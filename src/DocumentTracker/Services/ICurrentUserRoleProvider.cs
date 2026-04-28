namespace DocumentTracker.Services;

public interface ICurrentUserRoleProvider
{
    string? GetCurrentRole();
}
