using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace xDBCC.Services.ContactCapture.Models
{
    public class ReceivedContact
    {
        public string ContactId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
    }
}