using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;

namespace ARJ_WebAPIClient.Servicios
{
    internal class Sender
    {
                public static void enviarMensaje(List<string> destinatarios, string asunto, string mensaje)
        {
            var fromAddress = new MailAddress("administrador@acequiarj.es", "Administrador ARJ");
            var mail = new MailMessage();

            foreach (var toAddress in destinatarios)
            {
                mail.To.Add(toAddress);
            }

            mail.From = fromAddress;
            mail.Subject = asunto;
            mail.Body = mensaje;

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("administrador@acequiarj.es", "wwvf exjf vzmm wrje")
            };

            smtp.Send(mail);

            mail.Dispose();
        }
    }
}
