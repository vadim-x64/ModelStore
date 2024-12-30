namespace ModelStore.Models
{
    public class Login
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public int Role { get; set; } = 1;

        public bool IsBlocked { get; set; }
    }
}