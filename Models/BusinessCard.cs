namespace BusinessCardAPI.Models
{
    public class BusinessCard
    {
        public string ?Name { get; set; }
        public string ?Title { get; set; }
        public string ?Organization { get; set; }
        public string ?BaroNo { get; set; }
        public string ?Phone { get; set; }
        public List<string> ?Email { get; set; }
        public string ?Address { get; set; }
        public string ?Fax { get; set; }
        public string ?AdditionalInfo { get; set; }
    }
}
