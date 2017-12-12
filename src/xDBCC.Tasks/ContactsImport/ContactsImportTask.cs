using Sitecore.Analytics.Data;
using Sitecore.Analytics.DataAccess;
using Sitecore.Analytics.Model;
using Sitecore.Analytics.Model.Entities;
using Sitecore.Analytics.Tracking;
using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.EntityClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xDBCC.Tasks.ContactsImport
{
    public class ContactsImportTask
    {
        private readonly string _logInfoPrefix = "xDB CONTACTS IMPORT:";
        private ContactRepository _contactRepository;

        public ContactRepository XdbContactRepository
        {
            get { return _contactRepository; }
        }

        public ContactsImportTask()
        {
            _contactRepository = Sitecore.Configuration.Factory.CreateObject("tracking/contactRepository", true) as ContactRepository;

            Assert.IsNotNull(_contactRepository, "contactRepository");
        }

        public void Execute(Sitecore.Data.Items.Item[] items, Sitecore.Tasks.CommandItem command, Sitecore.Tasks.ScheduleItem schedule)
        {
            Assert.ArgumentNotNull(items, "items");
            Assert.ArgumentNotNull(command, "command");
            Assert.ArgumentNotNull(schedule, "schedule");

            // Settings item is the 1st item id
            var settingsItem = items[0];

            var entityBuilder = new EntityConnectionStringBuilder();

            // Build Entity connection string
            entityBuilder.Provider = settingsItem.Fields["Provider"].Value;
            entityBuilder.ProviderConnectionString = settingsItem.Fields["ProviderConnectionString"].Value;
            entityBuilder.Metadata = settingsItem.Fields["Metadata"].Value;

            Log.Info(string.Format("{0} Import Started", _logInfoPrefix), this);

            ProcessContacts(entityBuilder);

            Log.Info(string.Format("{0} Import Completed", _logInfoPrefix), this);
        }

        private void ProcessContacts(EntityConnectionStringBuilder entityBuilder)
        {
            var processedContactIds = new List<string>();

            // List of Contacts to process
            List<Models.Contact> contactsToProcess = new List<Models.Contact>();

            using (var dbContext = new Models.xDBContactCaptureEntities(entityBuilder.ConnectionString))
            {
                contactsToProcess = dbContext.Contacts
                    .Where(x => !x.ContactImportedOnDate.HasValue)
                    .Take(100) // Process max 100 at the time
                    .ToList();
            }

            if (contactsToProcess.Count > 0)
            {
                Log.Info(string.Format("{0} Processing {1} contact(s)", _logInfoPrefix, contactsToProcess.Count), this);
            }
            else
            {
                Log.Info(string.Format("{0} No contacts to process found", _logInfoPrefix), this);
            }

            foreach (var contact in contactsToProcess)
            {
                if (ImportXdbContact(contact))
                {
                    processedContactIds.Add(contact.ContactId);

                    Log.Error(string.Format("{0} Contact {1} processing completed", _logInfoPrefix, contact.ContactId), this);
                }
            }

            // Update ContactImportedOnDate for imported contacts
            using (var dbContext = new Models.xDBContactCaptureEntities(entityBuilder.ConnectionString))
            {
                var importedContacts = dbContext.Contacts
                    .Where(x => processedContactIds.Contains(x.ContactId))
                    .ToList();

                foreach (var contact in importedContacts)
                {
                    contact.ContactImportedOnDate = DateTime.UtcNow;
                }

                dbContext.SaveChanges();
            }
        }

        private bool ImportXdbContact(Models.Contact contact)
        {
            var leaseOwner = new LeaseOwner(GetType() + Guid.NewGuid().ToString(), LeaseOwnerType.OutOfRequestWorker);

            Contact xdbContact = null;
            Guid contactGuid = Guid.Empty;

            // Attempt to obtain an exclusive lock on an existing contact in xDB.
            LockAttemptResult<Contact> lockResult = _contactRepository.TryLoadContact(contact.Email, leaseOwner, TimeSpan.FromSeconds(30));

            // If contact is not found by email try by contactId
            if (lockResult.Status == LockAttemptStatus.NotFound)
            {
                if (Guid.TryParse(contact.ContactId, out contactGuid))
                {
                    lockResult = _contactRepository.TryLoadContact(contactGuid, leaseOwner, TimeSpan.FromSeconds(30));
                }
            }

            // If contact is still not found create a new contact
            if (lockResult.Status == LockAttemptStatus.NotFound)
            {
                // if contactGuid is NOT Guid.Empty use it or just generate new Guid
                xdbContact = XdbContactRepository.CreateContact(contactGuid != Guid.Empty ? contactGuid : Guid.NewGuid());

                //Setting these values allows processing and aggregation to process this contact without erroring.
                xdbContact.System.Value = 0;
                xdbContact.System.VisitCount = 0;

                Log.Info(string.Format("{0} Creating new contact {1}", _logInfoPrefix, contact.ContactId), this);
            }
            else if(lockResult.Status == LockAttemptStatus.AlreadyLocked)
            {
                Log.Info(string.Format("{0} Contact {1} is locked and cannot be processed.", _logInfoPrefix, contact.ContactId), this);

                return false;
            }
            else if(lockResult.Status == LockAttemptStatus.DatabaseUnavailable)
            {
                Log.Info(string.Format("{0} Database is unavailable. Contact {1} cannot be processed.", _logInfoPrefix, contact.ContactId), this);

                return false;
            }
            else
            {
                xdbContact = lockResult.Object;

                Log.Info(string.Format("{0} Updating contact {1}", _logInfoPrefix, contact.ContactId), this);
            }

            // Update contact Identifier if needed
            if (xdbContact.Identifiers.IdentificationLevel != ContactIdentificationLevel.Known)
            {
                xdbContact.Identifiers.IdentificationLevel = Sitecore.Analytics.Model.ContactIdentificationLevel.Known;
            }
            if (xdbContact.Identifiers.Identifier != contact.Email)
            {
                xdbContact.Identifiers.Identifier = contact.Email;
            }

            // Personal facet
            var personalFacet = xdbContact.GetFacet<IContactPersonalInfo>("Personal");
            personalFacet.FirstName = personalFacet.FirstName != contact.FirstName ? contact.FirstName : personalFacet.FirstName;
            personalFacet.Surname = personalFacet.Surname != contact.LastName ? contact.LastName : personalFacet.Surname;

            // Emails facet
            var emailsFacet = xdbContact.GetFacet<IContactEmailAddresses>("Emails");
            emailsFacet.Preferred = "Home";
            var email = emailsFacet.Entries.Contains("Home") ? emailsFacet.Entries["Home"] : emailsFacet.Entries.Create("Home");
            email.SmtpAddress = email.SmtpAddress != contact.Email ? contact.Email : email.SmtpAddress;

            // Addresses facet
            var addressesFacet = xdbContact.GetFacet<IContactAddresses>("Addresses");
            addressesFacet.Preferred = "Home";
            var address = addressesFacet.Entries.Contains("Billing") ? addressesFacet.Entries["Billing"] : addressesFacet.Entries.Create("Billing");
            address.Country = address.Country != contact.Country ? contact.Country : address.Country;
            address.PostalCode = address.PostalCode != contact.PostalCode ? contact.PostalCode : address.PostalCode;

            return XdbContactRepository.SaveContact(xdbContact, new ContactSaveOptions(true, leaseOwner));
        }
    }
}