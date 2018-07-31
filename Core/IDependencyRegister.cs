namespace Core
{
    using Microsoft.Extensions.DependencyInjection;

    public interface IDependencyRegister
    {
        void Register(IServiceCollection services);
    }
}
