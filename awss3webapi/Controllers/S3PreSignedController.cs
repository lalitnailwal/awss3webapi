using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using awss3webapi.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace awss3webapi.Controllers
{
    public class S3PreSignedController : ApiController
    {
        private string bucketName = ConfigurationManager.AppSettings["AWSBucketName"];
        private static IAmazonS3 s3Client;

        [HttpGet]
        [Route("api/S3PreSigned/CreateS3Bucket")]
        public IHttpActionResult CreateS3Bucket(string BucketName)
        { 
            s3Client = new AmazonS3Client(RegionEndpoint.USWest2);

            if (!(s3Client.DoesS3BucketExist(bucketName)))
            {
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = BucketName,
                    UseClientRegion = true
                };

                PutBucketResponse putBucketResponse = s3Client.PutBucket(putBucketRequest);
                Console.WriteLine("Response returned with '{0}'", putBucketResponse.HttpStatusCode);

                if (putBucketResponse.HttpStatusCode == HttpStatusCode.OK)
                    return Content(HttpStatusCode.OK, "Bucket Created Successfully");
                else
                    return Content(HttpStatusCode.InternalServerError, String.Format("Error while createing Bucket with the name {0}", bucketName));
            }

            return Content(HttpStatusCode.Conflict, String.Format("Bucket with the name {0} already exists", bucketName));
        }

        [HttpPost]
        [Route("api/S3PreSigned/GeneratePreSignedURL")]
        public async Task<IHttpActionResult> GeneratePreSignedURL()
        {            
            string strClientName = HttpContext.Current.Request.Form["clientname"].ToString();

            if (!Request.Content.IsMimeMultipartContent())
            {
                return StatusCode(HttpStatusCode.UnsupportedMediaType);
            }

            var filesReadToProvider = await Request.Content.ReadAsMultipartAsync();

            s3Client = new AmazonS3Client(RegionEndpoint.USWest2);
            var response = new List<string>();

            foreach (var stream in filesReadToProvider.Contents)
            {
                if (stream.Headers.ContentType != null)
                {
                    var filestream = await stream.ReadAsStreamAsync();

                    var request = new GetPreSignedUrlRequest
                    {
                        BucketName = bucketName,
                        Key = strClientName + "/" + stream.Headers.ContentDisposition.FileName.Trim('\"'),
                        Verb = HttpVerb.PUT,
                        Expires = DateTime.UtcNow.AddMinutes(10)
                    };

                    var url = s3Client.GetPreSignedURL(request);

                    response.Add(url);
                }
            }

            if (response == null)
            {
                return Content(HttpStatusCode.InternalServerError, String.Format("Error while createing presigned url"));
            }

            return Ok(response);
        }

        [HttpPost]
        [Route("api/S3PreSigned/UploadDocUsingPreSignedURL")]
        public async Task<IHttpActionResult> UploadDocUsingPreSignedURL()
        {

            try
            {
                string psurl = HttpContext.Current.Request.Form["psurl"].ToString();

                if (psurl == string.Empty)
                {
                    return BadRequest("The request doesn't contain presignedurl against which to upload");
                }

                if (!Request.Content.IsMimeMultipartContent())
                {
                    return StatusCode(HttpStatusCode.UnsupportedMediaType);
                }

                var filesReadToProvider = await Request.Content.ReadAsMultipartAsync();
                var filestream1 = await filesReadToProvider.Contents[0].ReadAsStreamAsync();

                string strUploadedFileName = string.Empty;
                string psurlFileName = string.Empty;


                s3Client = new AmazonS3Client(RegionEndpoint.USWest2);

                HttpWebRequest httpRequest = WebRequest.Create(psurl) as HttpWebRequest;

                foreach (var stream in filesReadToProvider.Contents)
                {
                    if (stream.Headers.ContentType == null)
                    {
                        psurlFileName = httpRequest.Address.Segments[2];
                    }
                    if (stream.Headers.ContentType != null)
                    {
                        strUploadedFileName = stream.Headers.ContentDisposition.FileName.Trim('\"');

                    }
                }

                if (strUploadedFileName.ToUpper() != psurlFileName.ToUpper())
                {
                    return BadRequest("It seemed uploaded file is not same against which presigned url is provided");
                }


                httpRequest.Method = "PUT";
                using (Stream dataStream = httpRequest.GetRequestStream())
                {
                    var buffer = new byte[8000];
                    using (Stream fileStream = filestream1)
                    {
                        int bytesRead = 0;
                        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            dataStream.Write(buffer, 0, bytesRead);
                        }
                    }
                }
                HttpWebResponse response = httpRequest.GetResponse() as HttpWebResponse;

                if(response.StatusCode == HttpStatusCode.OK)
                {
                    await NotifyFileUpload();
                }                
            }
            catch(Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, "There is some error, while uploading document " + ex.Message);
            }     

            return Content(HttpStatusCode.OK, "Documents uploaded successfully");
        }

        [HttpPost]
        [Route("api/S3PreSigned/GetViewPreSignedURL")]
        public async Task<IHttpActionResult> GetViewPreSignedURL()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                return StatusCode(HttpStatusCode.UnsupportedMediaType);
            }

            string strClientName = HttpContext.Current.Request.Form["clientname"].ToString();

            var filesReadToProvider = await Request.Content.ReadAsMultipartAsync();

            s3Client = new AmazonS3Client(RegionEndpoint.USWest2);
            var response = new List<string>();

            foreach (var stream in filesReadToProvider.Contents)
            {
                if (stream.Headers.ContentType != null)
                {
                    var filestream = await stream.ReadAsStreamAsync();

                    var expiryUrlRequest = new GetPreSignedUrlRequest
                    {
                        BucketName = bucketName,
                        Key = strClientName + "/" + stream.Headers.ContentDisposition.FileName.Trim('\"'),
                        Expires = DateTime.Now.AddMinutes(10)
                    };

                    var url = s3Client.GetPreSignedURL(expiryUrlRequest);

                    response.Add(url);
                }
            }

            if (response == null)
            {
                return Content(HttpStatusCode.InternalServerError, String.Format("Error while createing presigned url"));
            }

            return Ok(response);
        }

        public async Task<Boolean> NotifyFileUpload()
        {
            using (var client = new HttpClient())
            {                
                client.BaseAddress = new Uri(ConfigurationManager.AppSettings["NotificationURL"]);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                
                var objEmail = new EmailModel()
                {
                    toname = ConfigurationManager.AppSettings["toname"],
                    toemail = ConfigurationManager.AppSettings["toemail"],
                    subject = ConfigurationManager.AppSettings["subject"],
                    message = ConfigurationManager.AppSettings["message"]                  
                };

                HttpResponseMessage response = await client.PostAsJsonAsync<EmailModel>("api/Email/SendEmail", objEmail);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

    }    
}