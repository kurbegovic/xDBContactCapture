using AutoMapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using xDBCC.Services.ContactCapture.Models;

namespace xDBCC.Services.ContactCapture.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class DataController : ApiController
    {
        // GET api/data
        public bool Get()
        {
            using (var context = new xDBContactCaptureEntities())
            {
                return context.Database.Exists();
            }
        }

        // POST api/data
        public int Post([FromBody]string value)
        {
            var receivedContact = JsonConvert.DeserializeObject<ReceivedContact>(value);

            return WriteContactToSql(receivedContact);
        }

        private int WriteContactToSql(ReceivedContact receivedContact)
        {
            using (var context = new xDBContactCaptureEntities())
            {
                var dbContact = context.Contacts.Find(receivedContact.ContactId);
                if (dbContact == null)
                {
                    var newContact = Mapper.Map<Contact>(receivedContact);

                    context.Contacts.Add(newContact);

                    return context.SaveChanges();
                }
            }

            return 0;
        }
    }
}