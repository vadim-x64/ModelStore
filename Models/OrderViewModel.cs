namespace ModelStore.Models
{
    public class OrderViewModel
    {
        public Registration User { get; set; }
        public List<CartItem> CartItems { get; set; }
    }
}