using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;


namespace awss3webapi.Controllers
{
    public class S3PreSignedController : ApiController
    {
        private const string bucketName = "lalits3bucket";
        private static IAmazonS3 s3Client;

        [HttpGet]
        // GET: S3PreSigned
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
            if (!Request.Content.IsMimeMultipartContent())
            {
                return StatusCode(HttpStatusCode.UnsupportedMediaType);
            }

            var filesReadToProvider = await Request.Content.ReadAsMultipartAsync();

            s3Client = new AmazonS3Client(RegionEndpoint.USWest2);
            var response = new List<string>();

            foreach (var stream in filesReadToProvider.Contents)
            {
                var filestream = await stream.ReadAsStreamAsync();

                var request = new GetPreSignedUrlRequest
                {
                    BucketName = bucketName,
                    Key = stream.Headers.ContentDisposition.FileName.Trim('\"'),
                    Verb = HttpVerb.PUT,
                    Expires = DateTime.UtcNow.AddMinutes(10)
                };

                var url = s3Client.GetPreSignedURL(request);

                response.Add(url);
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
            s3Client = new AmazonS3Client(RegionEndpoint.USWest2);
            var filestream1 = await filesReadToProvider.Contents[0].ReadAsStreamAsync();


            HttpWebRequest httpRequest = WebRequest.Create(psurl) as HttpWebRequest;
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

            return Content(HttpStatusCode.OK, "Documents uploaded successfully");








            ////////var response = new List<string>();

            ////////try
            ////////{
            ////////    foreach (var stream in filesReadToProvider.Contents)
            ////////    {
            ////////        var filestream = await stream.ReadAsStreamAsync();

            ////////        var uploadRequest = new TransferUtilityUploadRequest
            ////////        {
            ////////            InputStream = filestream,
            ////////            Key = stream.Headers.ContentDisposition.FileName.Trim('\"'),
            ////////            BucketName = bucketName,
            ////////            CannedACL = S3CannedACL.NoACL
            ////////        };

            ////////        using (var fileTransferUtility = new TransferUtility(s3Client))
            ////////        {
            ////////            fileTransferUtility.Upload(uploadRequest);
            ////////        }
            ////////    }
            ////////}
            ////////catch(Exception ex)
            ////////{
            ////////    return Content(HttpStatusCode.InternalServerError, "Error while Uploading docs using presigned url " + ex.Message);
            ////////}          

            ////////return Content(HttpStatusCode.OK, "Documents uploaded successfully");
        }

        [HttpPost]
        [Route("api/S3PreSigned/GetViewPreSignedURL")]
        public async Task<IHttpActionResult> GetViewPreSignedURL()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                return StatusCode(HttpStatusCode.UnsupportedMediaType);
            }

            var filesReadToProvider = await Request.Content.ReadAsMultipartAsync();

            s3Client = new AmazonS3Client(RegionEndpoint.USWest2);
            var response = new List<string>();

            foreach (var stream in filesReadToProvider.Contents)
            {
                var filestream = await stream.ReadAsStreamAsync(); 

                var expiryUrlRequest = new GetPreSignedUrlRequest
                {
                    BucketName = bucketName,
                    Key = stream.Headers.ContentDisposition.FileName.Trim('\"'),
                    Expires = DateTime.Now.AddMinutes(10)
                };

                var url = s3Client.GetPreSignedURL(expiryUrlRequest);

                response.Add(url);
            }

            if (response == null)
            {
                return Content(HttpStatusCode.InternalServerError, String.Format("Error while createing presigned url"));
            }

            return Ok(response);
        }



        //public IHttpActionResult CreatePreSignedURL1()
        //{

        //    if (!Request.Content.IsMimeMultipartContent())
        //    {
        //        throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
        //    }

        //    string root = HttpContext.Current.Server.MapPath("~/App_Data");
        //    var provider = new MultipartFormDataStreamProvider(root);

        //    try
        //    {
        //        // Read the form data.
        //        Request.Content.ReadAsMultipartAsync(provider);


        //        // This illustrates how to get the file names.
        //        foreach (MultipartFileData file in provider.FileData)
        //        {
        //            Trace.WriteLine(file.Headers.ContentDisposition.FileName);//get FileName
        //            Trace.WriteLine("Server file path: " + file.LocalFileName);//get File Path
        //        }
        //        //return Request.CreateResponse(HttpStatusCode.OK, "pass upload file successed!");
        //    }
        //    catch (System.Exception e)
        //    {
        //        //return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
        //    }

        //    return Ok();
        //}


        //[HttpPost]
        //public async Task<IHttpActionResult> UploadFile()
        //{
        //    if (!Request.Content.IsMimeMultipartContent())
        //    {
        //        return StatusCode(HttpStatusCode.UnsupportedMediaType);
        //    }

        //    var filesReadToProvider = await Request.Content.ReadAsMultipartAsync();


        //    foreach (var stream in filesReadToProvider.Contents)
        //    {
        //        var fileBytes = await stream.ReadAsStreamAsync();
        //        var uploadRequest = new TransferUtilityUploadRequest
        //        {
        //            InputStream = fileBytes,
        //            Key = "test.pdf",
        //            BucketName = bucketName,
        //            CannedACL = S3CannedACL.NoACL
        //        };
        //        s3Client = new AmazonS3Client(RegionEndpoint.USWest2);
        //        using (var fileTransferUtility = new TransferUtility(s3Client))
        //        {
        //            fileTransferUtility.Upload(uploadRequest);
        //        }

        //        var expiryUrlRequest = new GetPreSignedUrlRequest
        //        {
        //            BucketName = bucketName,
        //            Key = "test.pdf",
        //            Expires = DateTime.Now.AddDays(1)
        //        };

        //        var url = s3Client.GetPreSignedURL(expiryUrlRequest);
        //    }

        //    return StatusCode(HttpStatusCode.OK);
        //}

        //[HttpPost]
        //public async Task<IHttpActionResult> CreatePreSignedURL()
        //{

        //    if (!Request.Content.IsMimeMultipartContent())
        //    {
        //        return StatusCode(HttpStatusCode.UnsupportedMediaType);
        //    }

        //    var filesReadToProvider = await Request.Content.ReadAsMultipartAsync();

        //    s3Client = new AmazonS3Client(RegionEndpoint.USWest2);
        //    var response = new List<string>();

        //    foreach (var stream in filesReadToProvider.Contents)
        //    {
        //        var filestream = await stream.ReadAsStreamAsync();

        //        var uploadRequest = new TransferUtilityUploadRequest
        //        {
        //            InputStream = filestream,
        //            Key = stream.Headers.ContentDisposition.FileName.Trim('\"'),
        //            BucketName = bucketName,
        //            CannedACL = S3CannedACL.NoACL
        //        };

        //        using (var fileTransferUtility = new TransferUtility(s3Client))
        //        {
        //            fileTransferUtility.Upload(uploadRequest);
        //        }

        //        var expiryUrlRequest = new GetPreSignedUrlRequest
        //        {
        //            BucketName = bucketName,
        //            Key = stream.Headers.ContentDisposition.FileName.Trim('\"'),
        //            Expires = DateTime.Now.AddDays(1)
        //        };

        //        var url = s3Client.GetPreSignedURL(expiryUrlRequest);

        //        response.Add(url);
        //    }

        //    if (response == null)
        //    {
        //        return Content(HttpStatusCode.InternalServerError, String.Format("Error while createing presigned url"));
        //    }

        //    return Ok(response);
        //}
    }
}