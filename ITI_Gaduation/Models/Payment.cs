using System.ComponentModel.DataAnnotations.Schema;

namespace ITI_Gaduation.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public double Amount { get; set; }
        public double Commission { get; set; }
        public DateTime DateTime { get; set; }
        public string Status { get; set; }
        public bool IsConfirmed { get; set; }
        [ForeignKey("Property")]
        public int PropertyId { get; set; }
        [ForeignKey("Renter")]
        public int RenterId { get; set; }
        [ForeignKey("Owner")]
        public int OwnerId { get; set; }
        public Property Property { get; set; }
        public User Owner { get; set; }
        public User Renter {  get; set; }

    }
}
