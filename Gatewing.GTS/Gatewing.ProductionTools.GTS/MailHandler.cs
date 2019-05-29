using Gatewing.ProductionTools.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;

namespace Gatewing.ProductionTools.GTS
{
    public class MailHandler
    {
        private static Logger _logger;

        public static void Send(string toAddress, string fromAddress, string subject, string body, Logger logger, bool isHtml = false, bool isForWeb = false)
        {
            Send(new List<string> { toAddress }, fromAddress, subject, body, logger, isHtml, isForWeb);
        }

        public static void Send(IEnumerable<string> toAddresses, string fromAddress, string subject, string body, Logger logger, bool isHtml = false, bool isForWeb = false)
        {
            _logger = logger;

            try
            {
                _logger.LogInfo("Attempting to send an email.");

                var msg = new MailMessage();

                foreach (var address in toAddresses)
                    msg.To.Add(address);

                msg.From = new MailAddress(fromAddress);
                msg.Subject = subject;
                msg.Body = body;
                msg.IsBodyHtml = isHtml;

                var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential("gatewingalerts@gmail.com", "SO8!4)8dsd1"),
                    EnableSsl = true
                };

                client.SendCompleted += Client_SendCompleted;

                if (isForWeb)
                    client.Send(msg);
                else
                    client.SendAsync(msg, null);

                //return Task.Run()
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private static void Client_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
                _logger.LogInfo("Sending mail for token \"" + e.UserState + "\" was cancelled.");

            if (e.Error != null)
            {
                _logger.LogError("Error received for token: " + e.UserState);
                _logger.LogError(e.Error.Message);
            }
            else
            {
                _logger.LogInfo("Sending mail for token \"" + e.UserState + "\" was completed succesfully.");
            }
        }
    }
}