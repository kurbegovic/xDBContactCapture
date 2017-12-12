$(function () {

    // Using sc_ext_contact instead of SC_ANALYTICS_GLOBAL_COOKIE for non-Sitecore site
    // because sc_ext_contact is using the correct domain. Script will not be able to access
    // SC_ANALYTICS_GLOBAL_COOKIE since it has domain from Sitecore instance.
    // In my example:
    // SC_ANALYTICS_GLOBAL_COOKIE   = .xdbcontactcapture.local  -> this is sitecore instance domain and script cannot access it
    // sc_ext_contact               = xdbcc.sites.nonsitecore   -> this is the domain of the non-Sitecore site and script can access it
    var scContactCookie = Cookies.get('sc_ext_contact');

    if (scContactCookie != undefined) {
        // sc_ext_cookie will look something like this "66bd94a5a8c84f5d9ac44e587dfd280d|False"
        // get the first part of the "|" delimited string
        var contactId = scContactCookie.split("|")[0];

        // output contactId in console for debugging purposes
        console.log('contactId -> ' + contactId);

        // attach click event for the submit button (or any other button you wish depending on the site)
        $("#submit-button").click(function () {

            // get all the data we need from the page into JSON object
            var contactInfo = {
                ContactId: contactId,
                FirstName: $('#FirstName').val(),
                LastName: $('#LastName').val(),
                Email: $('#Email').val(),
                Country: $('#Country').val(),
                PostalCode: $('#PostalCode').val()
            };

            // now, let's stringify JSON object
            var jsonData = JSON.stringify(contactInfo);

            // save serialized contact data to a session cookie to be accessed later
            Cookies.set('ccs-contact', jsonData);
        });
    }
});