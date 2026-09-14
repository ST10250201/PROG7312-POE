using System.ComponentModel.DataAnnotations;

namespace SmartX.Web.Models
{
    public class Sensor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "MAC address is required.")]
        [Display(Name = "MAC Address")]
        public string MacAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sensor location is required.")]
        [Display(Name = "Deployment Location")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sensor category is required.")]
        [Display(Name = "Sensor Category")]
        public string Category { get; set; } = string.Empty;
    }
}