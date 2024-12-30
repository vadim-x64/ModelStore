namespace ModelStore.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public Product Product { get; set; }
        public DateTime DateAdded { get; set; }
    }
}