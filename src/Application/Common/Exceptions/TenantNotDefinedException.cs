namespace MyApp.Application.Common.Exceptions;

public class TenantNotDefinedException : Exception
{
    public TenantNotDefinedException() : base("Tenant is not defined")
    {
    }

    public TenantNotDefinedException(string message) : base(message)
    {
    }
}
