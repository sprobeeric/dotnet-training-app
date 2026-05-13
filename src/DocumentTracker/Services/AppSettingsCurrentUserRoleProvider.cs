namespace DocumentTracker.Services;

public class AppSettingsCurrentUserRoleProvider : ICurrentUserRoleProvider
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AppSettingsCurrentUserRoleProvider(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentRole()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.IsInRole(PaymentReceiptRoles.ReceiptAdmin) == true)
        {
            return PaymentReceiptRoles.ReceiptAdmin;
        }

        if (user?.IsInRole(DocumentRoles.DocumentAdmin) == true)
        {
            return DocumentRoles.DocumentAdmin;
        }

        return _configuration["Training:CurrentUserRole"];
    }
}
