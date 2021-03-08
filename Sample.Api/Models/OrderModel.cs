using System;
using System.ComponentModel.DataAnnotations;

namespace Sample.Api.Models
{
    public class OrderModel
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public string CustomerNumber { get; set; }
        [Required]
        public string PaymentCardNumber { get; set; }
        // [Required]
        public string Notes { get; set; }
    }
}