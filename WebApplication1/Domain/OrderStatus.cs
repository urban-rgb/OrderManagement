namespace WebApplication1.Domain;

// [x] TODO слегка переделать код в местах использованиях enum
public enum OrderStatus
{
    Pending = 100,
    Paid = 200,
    InTransit = 300,
    Delivered = 400,
    Cancelled = 500
}