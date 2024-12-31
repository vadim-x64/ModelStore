namespace ModelStore.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public string ProductName { get; set; }
        public float ProductPrice { get; set; }
        public string ProductDescription { get; set; }
        public byte[] ProductPhoto { get; set; }
        public int Quantity { get; set; }
    }
}