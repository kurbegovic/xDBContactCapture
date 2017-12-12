$(function () {
    // sample service uri to post the contact data to
    var xPostUrl = "//xdbcc.services.contactcapture/api/data";

    // get session cookie with stringified contact data saved earlier
    var ccsContactCookie = Cookies.get('ccs-contact');

    // if can't find the session cookie do nothing
    if (ccsContactCookie != undefined) {
        // everything is looking good so far, go ahed and post to service
        $.ajax({
            type: "POST",
            url: xPostUrl,
            data: { "": ccsContactCookie },
            crossDomain: true,
            beforeSend: function (data, settings) {
                console.log('POST -> beforeSend: ' + ccsContactCookie);
            },
            success: function (data, textStatus, jqXHR) {
                console.log('POST -> success: ' + data);
            },
            error: function (data, textStatus, errorThrown) {
                console.log('POST -> error: ' + data + " | errorThrown: " + errorThrown)
            }
        });
    }
});