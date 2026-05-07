using Mapster;
using backend.Domain;
using backend.Services.DTOs;

namespace backend;

public class OrderMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Order, OrderResponse>()
            .Map(dest => dest.Status, src => src.Status.ToString());
    }
}