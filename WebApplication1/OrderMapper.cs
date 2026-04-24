using Mapster;
using WebApplication1.Domain;
using WebApplication1.Services.DTOs;

namespace WebApplication1;

public class OrderMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Order, OrderResponse>()
            .Map(dest => dest.Status, src => src.Status.ToString());
    }
}