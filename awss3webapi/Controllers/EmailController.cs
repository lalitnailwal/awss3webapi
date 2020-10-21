using awss3webapi.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace awss3webapi.Controllers
{
    public class EmailController : ApiController
    {
        [HttpPost]
        [Route("api/Email/SendEmail")]
        public async Task<IHttpActionResult> SendEmail([FromBody] EmailModel objData)
        {
            try
            {
                var message = new MailMessage();
                message.To.Add(new MailAddress(objData.toname.ToString() + " <" + objData.toemail.ToString() + ">"));
                message.From = new MailAddress("Support <support@email.com>");               
                message.Subject = objData.subject.ToString();
                message.Body = createEmailBody(objData.toname.ToString(), objData.message.ToString());
                message.IsBodyHtml = true;
                using (var smtp = new SmtpClient("mail2.ebix.com"))
                {
                    await smtp.SendMailAsync(message);
                    await Task.FromResult(0);
                }
            }
            catch(Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, "Error while sending email: " + ex.Message);
            }

            return Content(HttpStatusCode.OK, "email sent successfully");            
        }

        private string createEmailBody(string userName, string message)
        {
            string body = string.Empty;
            using (StreamReader reader = new StreamReader(HttpContext.Current.Server.MapPath("/htmlTemplate.html")))
            {
                body = reader.ReadToEnd();
            }
            body = body.Replace("{UserName}", userName);
            body = body.Replace("{message}", message);
            return body;
        }
    }    
}
