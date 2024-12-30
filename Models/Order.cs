namespace ModelStore.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public Registration User { get; set; }
        public DateTime OrderDate { get; set; }
        public string PaymentMethod { get; set; }
        public string DeliveryMethod { get; set; }
        public string Address { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public ICollection<OrderItem> OrderItems { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}