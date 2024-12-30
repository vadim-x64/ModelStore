namespace ModelStore.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] Photo { get; set; }
        public float Price { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public ICollection<Comment> Comments { get; set; }
    }
}