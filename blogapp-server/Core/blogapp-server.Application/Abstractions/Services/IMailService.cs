using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Abstractions.Services
{
    public interface IMailService
    {
        Task SendMailAsync(string to, string subject, string content);
        //Birden fazla kişiye mail gönderilecekse
        Task SendMailAsync(string[] toes, string subject, string content);
        Task SendForgotPasswordMailAsync(string to, string userFullName, long userId, string resetToken);
        Task SendVerifyMailAsync(string to, string fullName, long userId, string emailConfirmToken);
    }
}
