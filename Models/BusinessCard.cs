namespace BusinessCardAPI.Models
{
    public class BusinessCard
    {
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? Titles { get; set; }
        public string? Organization { get; set; }
        public string? Phone { get; set; }
        public List<string>? Email { get; set; }
        public string? Address { get; set; }
        public string? WebAddress { get; set; }
    }
}
