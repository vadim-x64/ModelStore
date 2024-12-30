namespace ModelStore.Models
{
    public class Registration
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string? MiddleName { get; set; }

        public DateOnly BirthDate { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }

        public string Address { get; set; }

        public byte[]? ProfilePicture { get; set; }
    }
}