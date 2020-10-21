using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace awss3webapi.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        //public IEnumerable<string> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        private const string bucketName = "lalits3bucket";
        private static IAmazonS3 s3Client;

        //https://wasabi-support.zendesk.com/hc/en-us/articles/360022974252-How-do-I-use-AWS-SDK-for-NET-with-Wasabi-
        //https://www.c-sharpcorner.com/blogs/working-with-files-and-folders-in-s3-using-aws-sdk-for-net
        //https://www.c-sharpcorner.com/article/fileupload-to-aws-s3-using-asp-net/
        public void Get()
        {
            var endpoint = "https://s3.wasabisys.com"; //US-East-1 endpoint
            //var config = new AmazonS3Config { ServiceURL = endpoint };

            AmazonS3Config config = new AmazonS3Config();
            //config.CommunicationProtocol = Protocol.HTTP;
            config.RegionEndpoint = Amazon.RegionEndpoint.USWest2;

            
            s3Client = new AmazonS3Client(RegionEndpoint.USWest2);

            if (!(s3Client.DoesS3BucketExist(bucketName)))
            {
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = bucketName,
                    UseClientRegion = true
                };

                PutBucketResponse putBucketResponse = s3Client.PutBucket(putBucketRequest);
                Console.WriteLine("Response returned with '{0}'", putBucketResponse.HttpStatusCode);
            }

            //CreateBucketAsync().Wait();
        }


        ////public void Get()
        ////{
        ////    IAmazonS3 client = new AmazonS3Client(RegionEndpoint.USWest2);
        ////    TransferUtility utility = new TransferUtility(client);

        ////    TransferUtilityUploadRequest request = new TransferUtilityUploadRequest();

        ////    string subDirectoryInBucket = "";
        ////    string fileNameInS3 = "test.pdf";

        ////    FileStream localFilePath = new FileStream(@"C:\Users\Lalit\Desktop\Presigned Testing Docs\sample.pdf", FileMode.Open, FileAccess.Read,FileShare.Read);

        ////    //System.IO.Stream localFilePath = new System.IO.Stream();


        ////    if (subDirectoryInBucket == "" || subDirectoryInBucket == null)
        ////    {
        ////        request.BucketName = bucketName; //no subdirectory just bucket name  
        ////    }
        ////    else
        ////    {   // subdirectory and bucket name  
        ////        request.BucketName = bucketName + @"/" + subDirectoryInBucket;
        ////    }
        ////    request.Key = fileNameInS3; //file name up in S3  
        ////    request.InputStream = localFilePath;
        ////    utility.Upload(request); //commensing the transfer  

        ////    //return true; //indicate that the file was sent  
        ////}

        static async Task CreateBucketAsync()
        {
            try
            {
                Console.WriteLine("Trying to create a bucket");
                Console.WriteLine("S3 Client configuration: '{0}'", s3Client.Config.ServiceURL);
                if (!(await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, bucketName)))
                {
                    var putBucketRequest = new PutBucketRequest
                    {
                        BucketName = bucketName,
                        UseClientRegion = true
                    };

                    PutBucketResponse putBucketResponse = await s3Client.PutBucketAsync(putBucketRequest);
                    Console.WriteLine("Response returned with '{0}'", putBucketResponse.HttpStatusCode);
                }

            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
