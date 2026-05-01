using Mapster;
using OrderManagement.Domain;
using OrderManagement.Services.DTOs;
using System.Diagnostics.CodeAnalysis;

namespace OrderManagement;

[ExcludeFromCodeCoverage]
public class OrderMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Order, OrderResponse>()
            .Map(dest => dest.Status, src => src.Status.ToString());
    }
}