
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Threading.Tasks;

namespace Car_Rent.Models
{
    public class EmailService
    {
        private const string _smtpServer = "smtp.gmail.com";
        private const int _smtpPort = 587;
        private const string _senderEmail = "nguyenphuongnamforwork@gmail.com";
        private const string _senderPassword = "qpwj nqec biml ibeo";   // 16 ký tự, KHÔNG khoảng trắng

        public async Task SendEmailAsync(string recipientEmail, string subject, string htmlBody)
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress("RentCar", _senderEmail));
            msg.To.Add(MailboxAddress.Parse(recipientEmail));
            msg.Subject = subject;
            msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();

            // 1) Kết nối TLS ngay
            await client.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);

            // 2) Xoá XOAUTH2 (đề phòng MailKit ưu tiên sai cơ chế)
            client.AuthenticationMechanisms.Remove("XOAUTH2");

            // 3) Đăng nhập bằng App Password
            await client.AuthenticateAsync(_senderEmail, _senderPassword);

            await client.SendAsync(msg);
            await client.DisconnectAsync(true);
        }
    }
}
