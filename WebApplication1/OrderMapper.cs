using Mapster;
using WebApplication1.Domain;
using WebApplication1.Services.DTOs;
using System.Diagnostics.CodeAnalysis;

namespace WebApplication1;

[ExcludeFromCodeCoverage]
public class OrderMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Order, OrderResponse>()
            .Map(dest => dest.Status, src => src.Status.ToString());
    }
}