namespace CspFoundation.Models
{
    public class ReturnModel
    {
        public bool IsSucceed { get; set; } = true;
        public string Description { get; set; } = "-";
        public string Exception { get; set; } = "-";
    }
}
