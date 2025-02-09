using System.Net.Mail;
using System.Net;
using MailKit.Net.Smtp;
using MimeKit;

namespace Car_Rent.Models
{
    public class EmailService
    {
        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _senderEmail = "nguyenphuongnamforwork@gmail.com"; // Email của anh
        private readonly string _senderPassword = "riwx ymof qwlt npyp"; // Mật khẩu ứng dụng (hoặc mật khẩu email)

        public async Task SendEmailAsync(string recipientEmail, string subject, string body)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("RentCar", _senderEmail));
            emailMessage.To.Add(new MailboxAddress("", recipientEmail));
            emailMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body // Nội dung email dạng HTML
            };

            emailMessage.Body = bodyBuilder.ToMessageBody();

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                await client.ConnectAsync(_smtpServer, _smtpPort, false);
                await client.AuthenticateAsync(_senderEmail, _senderPassword);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }
    }
}
