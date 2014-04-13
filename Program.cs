
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using Amazon;
using Amazon.Glacier;
using Amazon.Glacier.Model;
using Amazon.Glacier.Transfer;
using System.Collections.Specialized;
using System.Configuration;
using Amazon.Runtime;


using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime;
using Amazon.SecurityToken;


using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using System.IO;

namespace AWSGlaicer
{
    public class AWSArchiveResult
    {
        string checksum;

        public string Checksum
        {
            get { return checksum; }
            set { checksum = value; }
        }
        string archiveID;

        public string ArchiveID
        {
            get { return archiveID; }
            set { archiveID = value; }
        }
    }

    public class AWSArchiveRquest
    {
        string vaultName;

        public string VaultName
        {
            get { return vaultName; }
            set { vaultName = value; }
        }


        string checksum;

        public string Checksum
        {
            get { return checksum; }
            set { checksum = value; }
        }
        string archiveID;

        public string ArchiveID
        {
            get { return archiveID; }
            set { archiveID = value; }
        }
       
        string description;

        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        string checksumSHA256Compressed;

        public string ChecksumSHA256Compressed
        {
            get { return checksumSHA256Compressed; }
            set { checksumSHA256Compressed = value; }
        }


        string checksumTreeSHA256Compressed;

        public string ChecksumTreeSHA256Compressed
        {
            get { return checksumTreeSHA256Compressed; }
            set { checksumTreeSHA256Compressed = value; }
        }
        
        string sha256TreeHashDecompressed;

        public string Sha256TreeHashDecompressed
        {
            get { return sha256TreeHashDecompressed; }
            set { sha256TreeHashDecompressed = value; }
        }
  
        string originalFullPath;

        public string OriginalFullPath
        {
            get { return originalFullPath; }
            set { originalFullPath = value; }
        }

    }
    
    public class AWSMoveFilesXDynamo
    {
        static ArchiveTransferManager manager;

        static string archiveId;

        //Set the vault name you want to use here.
        static string vaultName = "BarrettsIsotype_compressionslog_81513";

        private static AmazonDynamoDBClient client;

        public static AWSArchiveResult UploadFile(String filePath, String nativeChecksum, String archiveDescription)
        {
            using (manager = new ArchiveTransferManager(RegionEndpoint.USEast1))
            {
                if (CheckRequiredFields())
                {
                    try
                    {
                        Form1.log.Info("Upload a Archive");

                        var uploadResult = manager.Upload(vaultName, archiveDescription, filePath);

                        archiveId = uploadResult.ArchiveId;

                        Form1.log.Info("Upload successful. Archive Id : " + uploadResult.ArchiveId + "Checksum : " + uploadResult.Checksum);

                        var config = new AmazonDynamoDBConfig();
                        config.ServiceURL = "http://dynamodb.us-east-1.amazonaws.com";

                        client = new AmazonDynamoDBClient(config);
                        Table dynamoTable = Table.LoadTable(client, "FL6119");

                        var entry = new Document();
                        entry["FileID"] = uploadResult.ArchiveId;
                        entry["date"] = System.DateTime.Now;
                        entry["checksum"] = uploadResult.Checksum;
                        entry["archiveID"] = filePath;
                        entry["NAtiveChecksum"] = nativeChecksum;

                        dynamoTable.PutItem(entry);

                        AWSArchiveResult ar = new AWSArchiveResult();
                        ar.ArchiveID = uploadResult.ArchiveId;
                        ar.Checksum = uploadResult.Checksum;

                        return ar;

                    }
                    catch (AmazonGlacierException e)
                    {
                        Form1.log.Error(e.Message);
                    }
                    catch (AmazonServiceException e)
                    {
                        Form1.log.Error(e.Message);
                    }
                }
                return new AWSArchiveResult();
            }

        }


        public static AWSArchiveResult DownloadFile(AWSArchiveRquest request)
        {
            using (manager = new ArchiveTransferManager(RegionEndpoint.USEast1))
            {
                if (CheckRequiredFields())
                {
                    try
                    {
                        Form1.log.Info("Download a Archive");

                        try
                        {                            
                            var options = new DownloadOptions();
                            options.StreamTransferProgress += AWSMoveFilesXDynamo.OnProgress;
                                                        
                            // Download an archive.
                            manager.Download(vaultName, request.ArchiveID,request.Description, options);

                        }
                        catch (AmazonGlacierException e) { Form1.log.Error(e.Message); }
                        catch (AmazonServiceException e) { Form1.log.Error(e.Message); }
                        catch (Exception e) { Form1.log.Error(e.Message); }
                       
                    }
                    catch (AmazonGlacierException e)
                    {
                        Form1.log.Error(e.Message);
                    }
                    catch (AmazonServiceException e)
                    {
                        Form1.log.Error(e.Message);
                    }
                }
                return new AWSArchiveResult();
            }

        }
        
        static int currentPercentage = -1;

        static void OnProgress(object sender, StreamTransferProgressArgs args)
        {
            if (args.PercentDone != currentPercentage)
            {
                currentPercentage = args.PercentDone;
                Form1.log.Info("Downloaded "+  args.PercentDone + " % " );
            }
        }

        static bool CheckRequiredFields()
        {
            NameValueCollection appConfig = ConfigurationManager.AppSettings;

            if (string.IsNullOrEmpty(appConfig["AWSAccessKey"]))
            {
                Console.WriteLine("AWSAccessKey was not set in the App.config file.");
                return false;
            }
            if (string.IsNullOrEmpty(appConfig["AWSSecretKey"]))
            {
                Console.WriteLine("AWSSecretKey was not set in the App.config file.");
                return false;
            }
            if (string.IsNullOrEmpty(vaultName))
            {
                Console.WriteLine("The variable vaultName is not set.");
                return false;
            }

            return true;
        }
    }
    
    public class AWSLowLevelSNSSQSDownload
    {
        static string topicArn;
        static string queueUrl;
        static string queueArn;
        
        ////Set the vault name you want to use here.
        //static string vaultName = "boo2";

        //// Set the file path for the file you want to upload here.
        //static string filePath = "c:\\temp\\test.txt";

        //// Set the file path for the archive to be saved after download.
        //static string downloadFilePath = @"d:\";

        static string vaultName = "BarrettsIsotype_compressionslog_81513";
     //   static string archiveID = "_GND5741D8BP4hNli7We7iQvx22tttt3gSe5GhENuln5pGOOW2rQV6Lyotrg1xtmephYEwUsBNG9NFJUPh0pYXLJk3mPQRiqeDMX3jNEZAkUYPh5TkWn4lqH3fTeogJUJtSonJFgnw";
     //   static string fileName = "Gei3481891_partC1_slide4.afiz";
        static AmazonSimpleNotificationService snsClient;
        static AmazonSQS sqsClient;
        const string SQS_POLICY =
            "{" +
            "    \"Version\" : \"2008-10-17\"," +
            "    \"Statement\" : [" +
            "        {" +
            "            \"Sid\" : \"sns-rule\"," +
            "            \"Effect\" : \"Allow\"," +
            "            \"Principal\" : {" +
            "                \"AWS\" : \"*\"" +
            "            }," +
            "            \"Action\"    : \"sqs:SendMessage\"," +
            "            \"Resource\"  : \"{QuernArn}\"," +
            "            \"Condition\" : {" +
            "                \"ArnLike\" : {" +
            "                    \"aws:SourceArn\" : \"{TopicArn}\"" +
            "                }" +
            "            }" +
            "        }" +
            "    ]" +
            "}";

        public static void DownloadFile(AWSArchiveRquest request)
        {
            AmazonGlacier client;
            try
            {
                using (client = new AmazonGlacierClient(Amazon.RegionEndpoint.USEast1))
                {
                    // Setup SNS topic and SQS queue.
                    SetupTopicAndQueue();
                    RetrieveArchive(client,request);
                }
                Console.WriteLine("Operations successful. To continue, press Enter");
            }
            catch (AmazonGlacierException e) { Console.WriteLine(e.Message); }
            catch (AmazonServiceException e) { Console.WriteLine(e.Message); }
            catch (Exception e) { Console.WriteLine(e.Message); }
            finally
            {
                // Delete SNS topic and SQS queue.
                snsClient.DeleteTopic(new DeleteTopicRequest() { TopicArn = topicArn });
                sqsClient.DeleteQueue(new DeleteQueueRequest() { QueueUrl = queueUrl });
            }
        }

        static void SetupTopicAndQueue()
        {
            snsClient = new AmazonSimpleNotificationServiceClient(Amazon.RegionEndpoint.USEast1);
            sqsClient = new AmazonSQSClient(Amazon.RegionEndpoint.USEast1);

            long ticks = DateTime.Now.Ticks;
            topicArn = snsClient.CreateTopic(new CreateTopicRequest { Name = "GlacierDownload-" + ticks }).CreateTopicResult.TopicArn;
            queueUrl = sqsClient.CreateQueue(new CreateQueueRequest() { QueueName = "GlacierDownload-" + ticks }).CreateQueueResult.QueueUrl;
            queueArn = sqsClient.GetQueueAttributes(new GetQueueAttributesRequest() { QueueUrl = queueUrl, AttributeName = new List<string> { "QueueArn" } }).GetQueueAttributesResult.QueueARN;

            snsClient.Subscribe(new SubscribeRequest()
            {
                Protocol = "sqs",
                Endpoint = queueArn,
                TopicArn = topicArn
            });

            // Add policy to the queue so SNS can send messages to the queue.
            var policy = SQS_POLICY.Replace("{QuernArn}", queueArn).Replace("{TopicArn}", topicArn);
            sqsClient.SetQueueAttributes(new SetQueueAttributesRequest()
            {
                QueueUrl = queueUrl,
                Attribute = new List<Amazon.SQS.Model.Attribute>()
        {
           new Amazon.SQS.Model.Attribute()
           {
             Name = "Policy",
             Value = policy
           }
        }
            });
        }

        static void RetrieveArchive(AmazonGlacier client, AWSArchiveRquest request )
        {
            // Initiate job.
            InitiateJobRequest initJobRequest = new InitiateJobRequest()
            {
                VaultName = vaultName,
                JobParameters = new JobParameters()
                {
                    Type = "archive-retrieval",
                    ArchiveId = request.ArchiveID,
                    Description = "This job is to download archive updated as part of getting started",
                    SNSTopic = topicArn,
                }
            };
            InitiateJobResponse initJobResponse = client.InitiateJob(initJobRequest);
            string jobId = initJobResponse.InitiateJobResult.JobId;

            // Check queue for a message and if job completed successfully, download archive.
            ProcessQueue(jobId, client, request);
        }

        private static void ProcessQueue(string jobId, AmazonGlacier client, AWSArchiveRquest request)
        {
            var receiveMessageRequest = new ReceiveMessageRequest() { QueueUrl = queueUrl, MaxNumberOfMessages = 1 };
            bool jobDone = false;
            while (!jobDone)
            {
                var receiveMessageResponse = sqsClient.ReceiveMessage(receiveMessageRequest);
                if (receiveMessageResponse.ReceiveMessageResult.Message.Count == 0)
                {
                    Thread.Sleep(1000 * 60);
                    continue;
                }
                Amazon.SQS.Model.Message message = receiveMessageResponse.ReceiveMessageResult.Message[0];
                Dictionary<string, string> outerLayer = JsonConvert.DeserializeObject<Dictionary<string, string>>(message.Body);
                Dictionary<string, string> fields = JsonConvert.DeserializeObject<Dictionary<string, string>>(outerLayer["Message"]);
                string statusCode = fields["StatusCode"] as string;
                if (string.Equals(statusCode, GlacierUtils.JOB_STATUS_SUCCEEDED, StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("Downloading job output");
                    DownloadOutput(jobId, client, request); // This where we save job output to the specified file location.
                }
                else if (string.Equals(statusCode, GlacierUtils.JOB_STATUS_FAILED, StringComparison.InvariantCultureIgnoreCase))
                    Console.WriteLine("Job failed... cannot download the archive.");
                jobDone = true;
                sqsClient.DeleteMessage(new DeleteMessageRequest() { QueueUrl = queueUrl, ReceiptHandle = message.ReceiptHandle });
            }
        }

        private static void DownloadOutput(string jobId, AmazonGlacier client, AWSArchiveRquest request)
        {
            GetJobOutputRequest getJobOutputRequest = new GetJobOutputRequest()
            {

                JobId = jobId,
                VaultName = vaultName
            };
            GetJobOutputResponse getJobOutputResponse = client.GetJobOutput(getJobOutputRequest);
            GetJobOutputResult result = getJobOutputResponse.GetJobOutputResult;

            using (Stream webStream = result.Body)
            {
                using (Stream fileToSave = File.OpenWrite(request.Description))
                {
                    CopyStream(webStream, fileToSave);
                }
            }
        }

        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[65536];
            int length;
            while ((length = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, length);
            }
        }
    }

    public class klDOTNETTimer
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);
        private long startTime;
        private long stopTime;
        private long freq;

        public klDOTNETTimer()
        {
            startTime = 0;
            stopTime = 0;
            freq = 0;
            if (QueryPerformanceFrequency(out freq) == false)
            {
                throw new Win32Exception();
            }
        }

        public long Start()
        {
            QueryPerformanceCounter(out startTime);
            return startTime;
        }

        public long Stop()
        {
            QueryPerformanceCounter(out stopTime);
            return stopTime;
        }

        public double Duration
        {
            get
            {
                return (double)(stopTime - startTime) / (double)freq;
            }
        }

        public double DurationTics
        {
            get
            {
                return (double)(stopTime - startTime);
            }
        }

        public long Frequency
        {
            get
            {
                QueryPerformanceFrequency(out freq);
                return freq;
            }
        }
    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
