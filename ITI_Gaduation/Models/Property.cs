using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace ITI_Gaduation.Models
{
    public class Property
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; } //Include Amenities
        public double Price {  get; set; }
        public int RoomsCount { get; set; }
        public string GenderPreference {  get; set; }
        public string City {  get; set; }
        public string Region { get; set; }
        public string Street { get; set; }
        public string Status {  get; set; }
        public DateTime CreatedAt { get; set; }
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }
        public ICollection<PropertyImage> propertyImages { get; set; }
    }
}
