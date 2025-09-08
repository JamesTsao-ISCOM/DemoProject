using System.Net;
using System.Net.Mail;

namespace Project01_movie_lease_system.Service
{
    public interface IEmailService
    {
        Task SendEmail(IEnumerable<string> receptors, string subject, string body, Dictionary<string, string>? attachmentPaths = null, IEnumerable<string>? cc = null);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task SendEmail(IEnumerable<string> receptors, string subject, string body, Dictionary<string, string>? attachmentPaths = null, IEnumerable<string>? cc = null)
        {
            try
            {
                var email = _configuration.GetValue<string>("EMAIL_CONFIG:EMAIL");
                var password = _configuration.GetValue<string>("EMAIL_CONFIG:PASSWORD");
                var host = _configuration.GetValue<string>("EMAIL_CONFIG:HOST");
                var port = _configuration.GetValue<int>("EMAIL_CONFIG:PORT");
                Console.WriteLine($"Email Config - Email: {email}, Host: {host}, Port: {port}");
                var smtpClient = new SmtpClient(host, port)
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(email, password),
                    EnableSsl = true
                };
                
                // Create message with first receptor as primary
                var primaryReceptor = receptors.FirstOrDefault();
                if (string.IsNullOrEmpty(primaryReceptor))
                    throw new ArgumentException("At least one receptor is required");
                    
                var message = new MailMessage(email, primaryReceptor)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                
                // Add additional receptors as TO
                foreach (var receptor in receptors.Skip(1))
                {
                    if (!string.IsNullOrWhiteSpace(receptor))
                        message.To.Add(receptor.Trim());
                }
                
                // Add CC recipients
                if (cc != null)
                {
                    foreach (var ccAddress in cc)
                    {
                        if (!string.IsNullOrWhiteSpace(ccAddress))
                            message.CC.Add(ccAddress.Trim());
                    }
                }
                
                if (attachmentPaths != null)
                {
                    List<KeyValuePair<string, string>> attachmentList = attachmentPaths.ToList();
                    for (int i = 0; attachmentList != null && i < attachmentList.Count; i++)
                    {
                        var attachmentPath = attachmentList[i].Value;
                        if (!string.IsNullOrEmpty(attachmentPath))
                        {
                            var attachment = new Attachment(attachmentPath);
                            attachment.Name = attachmentList[i].Key; // 設定附件名稱
                            message.Attachments.Add(attachment);
                        }
                    }
                }
                await smtpClient.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }
    }
}