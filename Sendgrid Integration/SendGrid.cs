
 using SendGrid;
 using SendGrid.Helpers.Mail;

public async Task SendEmailAsync(string email, string subject, string htmlMessage)
{
    try
    {
        var client = new SendGridClient(_apiKey);
        var msg = MailHelper.CreateSingleEmail(new EmailAddress(_fromEmail), new EmailAddress(email), subject, "", htmlMessage);
        var response = await client.SendEmailAsync(msg);
    }
    catch (Exception ex)
    {
        if (ex.Message == "Error while copying content to a stream.")
        
        }
        else
        {
            throw ex;
        }
    }
}



