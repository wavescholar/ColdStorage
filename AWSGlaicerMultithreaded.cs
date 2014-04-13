
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

using System.Collections.Concurrent;

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using System.IO;
using DPCompress;
using Amazon.DynamoDBv2.Model;

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

        int sleepSec;

        public int SleepSec
        {
            get { return sleepSec; }
            set { sleepSec = value; }
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

        string fileName;

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

    }

    class GalicierHelper
    {
        private static AmazonGlacier client;

        public static bool checkVault(string vaultName)
        {
            bool result = false;
            try
            {
                client = new AmazonGlacierClient(Amazon.RegionEndpoint.USEast1);

                DescribeVaultRequest describeVaultRequest = new DescribeVaultRequest()
                {
                    VaultName = vaultName
                };
                DescribeVaultResponse describeVaultResponse = client.DescribeVault(describeVaultRequest);
                DescribeVaultResult describeVaultResult = describeVaultResponse.DescribeVaultResult;
                Console.WriteLine("\nVault description...");
                Console.WriteLine(
                   "\nVaultName: " + describeVaultResult.VaultName +
                   "\nVaultARN: " + describeVaultResult.VaultARN +
                   "\nVaultCreationDate: " + describeVaultResult.CreationDate +
                   "\nNumberOfArchives: " + describeVaultResult.NumberOfArchives +
                   "\nSizeInBytes: " + describeVaultResult.SizeInBytes +
                   "\nLastInventoryDate: " + describeVaultResult.LastInventoryDate
                   );
                result = true;
            }
            catch (AmazonGlacierException e)
            { Console.WriteLine(e.Message); result = false; }
            catch (AmazonServiceException e)
            { Console.WriteLine(e.Message); result = false; }
            catch (Exception e)
            { Console.WriteLine(e.Message); result = false; }

            return result;
        }

        public static void createVault(string vaultName)
        {
            try
            {
                var manager = new ArchiveTransferManager(Amazon.RegionEndpoint.USEast1);
                manager.CreateVault(vaultName);

                //manager.DeleteVault(vaultName);
                //Console.WriteLine("\nVault deleted. To continue, press Enter");
            }
            catch (AmazonGlacierException e)
            { Console.WriteLine(e.Message); }
            catch (AmazonServiceException e)
            { Console.WriteLine(e.Message); }
            catch (Exception e)
            { Console.WriteLine(e.Message); }

        }
    }

    class DynamoDBHelper
    {
        private static AmazonDynamoDBClient client;

        //Makes the table if not present
        public static bool DynamoTableCheck(string tableName)
        {
            bool isTableOK = false;

            var config = new AmazonDynamoDBConfig();
            config.ServiceURL = "http://dynamodb.us-east-1.amazonaws.com";
            client = new AmazonDynamoDBClient(config);
            Table dynamoTable;
            try
            {
                //This method will throw an exception if the table does not exist.
                dynamoTable = Table.LoadTable(client, tableName);
                return true;
            }
            catch (Exception ex)
            {
                Form1.log.Error(ex.ToString() + "Tabel may be missing or there are credential issues.");

                makeTable(tableName);

                //Try again
                dynamoTable = Table.LoadTable(client, tableName);

                isTableOK = true;
            }
            return isTableOK;
        }

        private static void makeTable(string tableName)
        {
            AmazonDynamoDBClient client;
            var config = new AmazonDynamoDBConfig();
            config.ServiceURL = "http://dynamodb.us-east-1.amazonaws.com";

            client = new AmazonDynamoDBClient(config);

            Console.WriteLine("\n*** Creating table ***");
            var request = new CreateTableRequest
            {
                AttributeDefinitions = new List<AttributeDefinition>()
        {
          new AttributeDefinition
          {
            AttributeName = "HashKeyId",
            AttributeType = "S"
          }

        },
               
        KeySchema = new List<KeySchemaElement>
        {
          new KeySchemaElement
          {
            AttributeName = "HashKeyId",
            KeyType = "HASH"
          }
        },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 5,
                    WriteCapacityUnits = 6
                },
                TableName = tableName
            };

            var response = client.CreateTable(request);

            var result = response.CreateTableResult;
            var tableDescription = result.TableDescription;
            Console.WriteLine("{1}: {0} \t ReadsPerSec: {2} \t WritesPerSec: {3}",
                            tableDescription.TableStatus,
                            tableDescription.TableName,
                            tableDescription.ProvisionedThroughput.ReadCapacityUnits,
                            tableDescription.ProvisionedThroughput.WriteCapacityUnits);

            string status = tableDescription.TableStatus;
            Console.WriteLine(tableName + " - " + status);

            WaitUntilTableReady(tableName);
        }

        private static void WaitUntilTableReady(string tableName)
        {
            var config = new AmazonDynamoDBConfig();
            config.ServiceURL = "http://dynamodb.us-east-1.amazonaws.com";
            client = new AmazonDynamoDBClient(config);
            string status = null;
            // Let us wait until table is created. Call DescribeTable.
            do
            {
                System.Threading.Thread.Sleep(5000); // Wait 5 seconds.
                try
                {
                    var res = client.DescribeTable(new DescribeTableRequest
                    {
                        TableName = tableName
                    });

                    Console.WriteLine("Table name: {0}, status: {1}",
                                   res.DescribeTableResult.Table.TableName,
                                   res.DescribeTableResult.Table.TableStatus);
                    status = res.DescribeTableResult.Table.TableStatus;
                }
                catch (Amazon.DynamoDBv2.Model.ResourceNotFoundException resourceNotFound)
                {
                    // DescribeTable is eventually consistent. So you might
                    // get resource not found. So we handle the potential exception.
                }
            } while (status != "ACTIVE");
        }

        public static void MakeDynamoEntry(string tableName, string[] fields, string[] vals)
        {
            AmazonDynamoDBClient client;
            var config = new AmazonDynamoDBConfig();
            config.ServiceURL = "http://dynamodb.us-east-1.amazonaws.com";

            client = new AmazonDynamoDBClient(config);
            Table dynamoTable;
            try
            {
                dynamoTable = Table.LoadTable(client, tableName);
                var entry = new Document();
                if (fields.Length != vals.Length)
                {
                    throw new Exception("fields.Length != vals.Length in ArchiveUploadMultipartParallel::MakeDynamoEntry(string vault, string[] fields, string[] vals )");
                }
                entry["date"] = System.DateTime.Now;
                
                entry["HashKeyId"] = Guid.NewGuid();

                for (int i = 0; i < fields.Length; i++)
                {
                    entry[fields[i]] = vals[i];
                }

                dynamoTable.PutItem(entry);
            }
            catch (Exception ex)
            {
                Form1.log.Error(ex.ToString());
            }
        }
    }

    class ArchiveUploadMultipartParallel
    {
        // Construct a ConcurrentQueue.
        ConcurrentQueue<string> SHA256ConcurrentQueue;
        public string vaultName = "";
        string archiveToUpload = "";
        long partSize = 4194304 * 2; // 8 MB.
        int ActiveWorkerCount = 0;
        String archiveDescription;

        public ArchiveUploadMultipartParallel(string vault)
        {
            vaultName = vault;
        }

        ManualResetEvent AllWorkerCompletedEvent = new ManualResetEvent(false);

        public AWSArchiveResult UploadFile(String filePath, String nativeChecksum, String arDescription)
        {
            archiveToUpload = filePath;

            archiveDescription = arDescription;

            FileInfo fio = new FileInfo(filePath);
            archiveDescription = fio.Name;

            AWSArchiveResult ar = new AWSArchiveResult();
            AmazonGlacier client;

            List<string> partChecksumList = new List<string>();

            SHA256ConcurrentQueue = new ConcurrentQueue<string>();

            try
            {
                using (client = new AmazonGlacierClient(Amazon.RegionEndpoint.USEast1))
                {
                    Console.WriteLine("Uploading an archive.");

                    string uploadId = InitiateMultipartUpload(client);

                    partChecksumList = UploadParts(uploadId, client);

                    ar = CompleteMPU(uploadId, client, partChecksumList, fio);
                }

            }
            catch (AmazonGlacierException e)
            {
                Form1.log.Error(e.ToString());
            }
            catch (AmazonServiceException e)
            {
                Form1.log.Error(e.ToString());
            }
            catch (Exception e)
            {
                Form1.log.Error(e.ToString());
            }
            return ar;
        }

        string InitiateMultipartUpload(AmazonGlacier client)
        {
            InitiateMultipartUploadRequest initiateMPUrequest = new InitiateMultipartUploadRequest()
            {
                VaultName = vaultName,
                PartSize = partSize,
                ArchiveDescription = archiveDescription
            };

            InitiateMultipartUploadResponse initiateMPUresponse = client.InitiateMultipartUpload(initiateMPUrequest);

            return initiateMPUresponse.InitiateMultipartUploadResult.UploadId;
        }

        List<string> UploadParts(string uploadID, AmazonGlacier client)
        {
            List<string> partChecksumList = new List<string>();
            long currentPosition = 0;

            var buffer = new byte[Convert.ToInt32(partSize)];

            long fileLength = new FileInfo(archiveToUpload).Length;

            ThreadPool.SetMaxThreads(25, 25);

            List<ThreadData> arThreadObj = new List<ThreadData>();

            FileStream fileToUpload = new FileStream(archiveToUpload, FileMode.Open, FileAccess.Read);

            //Beware - we create memory buffers for the entire file at once. 
            //BBCREVISIT - use a queue for the threads and pick off that as the ThreadPool frees up resources 
            while (currentPosition < fileLength)
            {
                Stream uploadPartStream = GlacierUtils.CreatePartStream(fileToUpload, partSize);

                ThreadData objData = new ThreadData();
                objData.uploadID = uploadID;
                objData.client = client;
                objData.currentPosition = currentPosition;
                objData.uploadPartStream = uploadPartStream;
                objData.buffer = new byte[Convert.ToInt32(partSize)];
                int read = 0;
                try
                {
                    read = fileToUpload.Read(objData.buffer, (int)0, (int)partSize);
                }
                catch (Exception e)
                {
                    Form1.log.Error(e.ToString());
                }

                if (read == -1)
                {
                    Form1.log.Info("Nothing to read :  fileLength % partSize ==0");
                    break;
                }

                arThreadObj.Add(objData);

                Form1.log.Info("Created Part : " + Convert.ToString(currentPosition) + " of Length " + uploadPartStream.Length);

                if (read != uploadPartStream.Length)
                {
                    Console.WriteLine("We have a problem Houston");
                }
                currentPosition = currentPosition + uploadPartStream.Length;//We are not using the stream right now. 

            }

            for (int ic = 0; ic < arThreadObj.Count; ic++)
            {
                Interlocked.Increment(ref ActiveWorkerCount);
                ThreadData objData = arThreadObj[ic];
                ThreadPool.QueueUserWorkItem(ThreadUpload, objData);
            }

            AllWorkerCompletedEvent.WaitOne();

            partChecksumList = SHA256ConcurrentQueue.ToList();

            fileToUpload.Close();

            return partChecksumList;
        }

        private void ThreadUpload(object f_object)
        {
            ThreadData objData = null;
            AmazonGlacier client = null;
            Stream uploadPartStream = null;
            try
            {
                objData = (ThreadData)f_object;

                string uploadID = objData.uploadID;
                client = objData.client;
                long currentPosition = objData.currentPosition;
                Form1.log.Info("Trying to upload Part :" + Convert.ToString(objData.currentPosition));

                //For the last one we need to make sure the buffer is the right size?
                //The uploadMPUrequest.SetRange probably takes care of this.

                int memoryBufferIndex = 0;//The index into buffer at which the stream begin
                int memoryBuffercount = (int)(objData.uploadPartStream.Length); //The length of the stream in bytes.

                uploadPartStream = new MemoryStream(objData.buffer, memoryBufferIndex, memoryBuffercount);

                //To ensure that part data is not corrupted in transmission, you compute a SHA256 tree
                // hash of the part and include it in your request. Upon receiving the part data, Amazon Glacier also computes a SHA256 tree hash. 
                //If these hash values don't match, the operation fails. For information about computing a SHA256 tree hash, see Computing Checksums
                string checksum = TreeHashGenerator.CalculateTreeHash(uploadPartStream);

                SHA256ConcurrentQueue.Enqueue(checksum);

                UploadMultipartPartRequest uploadMPUrequest = new UploadMultipartPartRequest()
                {
                    VaultName = vaultName,
                    Body = uploadPartStream,
                    Checksum = checksum,
                    UploadId = uploadID
                };

                uploadMPUrequest.SetRange(currentPosition, currentPosition + objData.uploadPartStream.Length - 1);

                UploadMultipartPartResponse mpr = client.UploadMultipartPart(uploadMPUrequest);

                Form1.log.Info("Sent " + Convert.ToString(mpr.ContentLength) + "bytes" + " for Part  :" + Convert.ToString(objData.currentPosition));

            }
            catch (Exception e)
            {
                Form1.log.Error(e.ToString());
                Form1.log.Error(e.StackTrace);
                Form1.log.Info("Retrying Part " + Convert.ToString(objData.currentPosition));

                //Retrying up to 10 times - waiting longer each try
                {
                    int fv = 0;
                    bool successfulPartUpload = false;
                    while (fv < 10 && successfulPartUpload == false)
                    {
                        successfulPartUpload = retryUpload(f_object);
                        Thread.Sleep(4000 * fv);
                    }

                }
            }
            finally
            {
                if (Interlocked.Decrement(ref ActiveWorkerCount) <= 0)
                    AllWorkerCompletedEvent.Set();

                uploadPartStream = null;
                f_object = null;
                objData = null;
                client = null;
            }
        }

        private bool retryUpload(object f_object)
        {
            try
            {
                ThreadData objData = null;
                AmazonGlacier client = null;
                Stream uploadPartStream = null;

                objData = (ThreadData)f_object;

                string uploadID = objData.uploadID;
                client = objData.client;
                long currentPosition = objData.currentPosition;
                Form1.log.Info("Trying to upload Part :" + Convert.ToString(objData.currentPosition));

                //For the last one we need to make sure the buffer is the right size?
                //The uploadMPUrequest.SetRange probably takes care of this.

                int memoryBufferIndex = 0;//The index into buffer at which the stream begin
                int memoryBuffercount = (int)(objData.uploadPartStream.Length); //The length of the stream in bytes.

                uploadPartStream = new MemoryStream(objData.buffer, memoryBufferIndex, memoryBuffercount);

                //To ensure that part data is not corrupted in transmission, you compute a SHA256 tree
                // hash of the part and include it in your request. Upon receiving the part data, Amazon Glacier also computes a SHA256 tree hash. 
                //If these hash values don't match, the operation fails. For information about computing a SHA256 tree hash, see Computing Checksums
                string checksum = TreeHashGenerator.CalculateTreeHash(uploadPartStream);

                SHA256ConcurrentQueue.Enqueue(checksum);

                UploadMultipartPartRequest uploadMPUrequest = new UploadMultipartPartRequest()
                {
                    VaultName = vaultName,
                    Body = uploadPartStream,
                    Checksum = checksum,
                    UploadId = uploadID
                };

                uploadMPUrequest.SetRange(currentPosition, currentPosition + objData.uploadPartStream.Length - 1);

                UploadMultipartPartResponse mpr = client.UploadMultipartPart(uploadMPUrequest);
                Form1.log.Info("Retry Success " + Convert.ToString(mpr.ContentLength) + "bytes" + " for Part  :" + Convert.ToString(objData.currentPosition));
                return true;

            }
            catch (Exception ex)
            {
                Form1.log.Error(ex.ToString());
                return false;

            }
        }


        AWSArchiveResult CompleteMPU(string uploadID, AmazonGlacier client, List<string> partChecksumList, FileInfo fio)
        {
            try
            {
                long fileLength = fio.Length;

                fileLength = new FileInfo(archiveToUpload).Length;

                FileStream inputFile = File.Open(archiveToUpload, FileMode.Open, FileAccess.Read);
                byte[] treeHash = Form1.ComputeSHA256TreeHash(inputFile);
                String localChecksum = BitConverter.ToString(treeHash).Replace("-", "").ToLower();

                CompleteMultipartUploadRequest completeMPUrequest = new CompleteMultipartUploadRequest()
                {
                    UploadId = uploadID,
                    ArchiveSize = fileLength.ToString(),
                    Checksum = localChecksum,
                    VaultName = vaultName
                };

                CompleteMultipartUploadResponse completeMPUresponse = client.CompleteMultipartUpload(completeMPUrequest);

                AWSArchiveResult ar = new AWSArchiveResult();
                ar.ArchiveID = completeMPUresponse.CompleteMultipartUploadResult.ArchiveId;
                ar.Checksum = localChecksum;

                return ar;
            }
            catch (Exception e)
            {
                Form1.log.Error(e.ToString());
                return new AWSArchiveResult();
            }
        }
    }

    class ThreadData
    {
        public string uploadID;
        public AmazonGlacier client;
        public long currentPosition;
        public string checksum;
        public Stream uploadPartStream;
        public byte[] buffer;
    }

    public class AWSMoveFilesXDynamoMT
    {
        ArchiveTransferManager manager;

        string archiveId;

        //Set the vault name you want to use here.
        string vaultName = "";

        AmazonDynamoDBClient client;

        AWSArchiveRquest archiveRequest;

        public AWSMoveFilesXDynamoMT(string vault)
        {
            vaultName = vault;
        }

        public AWSArchiveResult UploadFile(String filePath, String nativeChecksum, String archiveDescription)
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
                        Form1.log.Error(e.ToString());
                    }
                    catch (AmazonServiceException e)
                    {
                        Form1.log.Error(e.ToString());
                    }
                }
                return new AWSArchiveResult();
            }

        }

        public AWSArchiveResult DownloadFile(AWSArchiveRquest request)
        {
            archiveRequest = request;

            using (manager = new ArchiveTransferManager(RegionEndpoint.USEast1))
            {
                if (CheckRequiredFields())
                {
                    try
                    {
                        Form1.log.Info("Download  Archive" + request.ArchiveID + " " + request.Description + " " + request.FileName);

                        try
                        {
                            var options = new DownloadOptions();
                            options.StreamTransferProgress += OnProgress;

                            // Download an archive.
                            manager.Download(vaultName, request.ArchiveID, request.Description, options);

                            if (request.Description.Contains(".tif"))
                            {
                                String outfileName = String.Empty;

                                String fileName = request.Description;

                                FileInfo fi = new FileInfo(fileName);

                                fi.MoveTo(fileName + ".z");

                                fileName = fileName + ".z";

                                outfileName = fileName.Replace(".z", "");

                                FileStream inputFile = File.Open(fileName, FileMode.Open, FileAccess.Read);
                                byte[] treeHash = Form1.ComputeSHA256TreeHash(inputFile);
                                String zipChecksum = BitConverter.ToString(treeHash).Replace("-", "").ToLower();

                                Form1.log.Info(fileName + "  Tree SHA 256 Checksum : " + zipChecksum);

                                Form1.log.Info(fileName + "  Original Injection Tree SHA 256 Checksum : " + request.ChecksumTreeSHA256Compressed);

                                ParallelCompress.doNotUseTPL = false;

                                ParallelCompress.compressStrictSeqential = false;

                                ParallelCompress.UncompressFast(outfileName, fileName, true);

                                inputFile = File.Open(outfileName, FileMode.Open, FileAccess.Read);
                                treeHash = Form1.ComputeSHA256TreeHash(inputFile);
                                String decompressedChecksum = BitConverter.ToString(treeHash).Replace("-", "").ToLower();

                                Form1.log.Info(outfileName + "  Decmpressed Tree SHA 256 Checksum : " + zipChecksum);

                                Form1.log.Info(outfileName + "  Decmplressed Original Tree SHA 256 Checksum : " + request.ChecksumTreeSHA256Compressed);
                            }
                        }
                        catch (AmazonGlacierException e) { Form1.log.Error(e.ToString()); }
                        catch (AmazonServiceException e) { Form1.log.Error(e.ToString()); }
                        catch (Exception e) { Form1.log.Error(e.ToString()); }

                    }
                    catch (AmazonGlacierException e)
                    {
                        Form1.log.Error(e.ToString());
                    }
                    catch (AmazonServiceException e)
                    {
                        Form1.log.Error(e.ToString());
                    }
                }
                return new AWSArchiveResult();
            }
        }

        int currentPercentage = -1;

        void OnProgress(object sender, StreamTransferProgressArgs args)
        {
            String fileName = archiveRequest.Description;

            if (args.PercentDone != currentPercentage)
            {
                currentPercentage = args.PercentDone;
                Form1.log.Info("Downloaded " + fileName + " " + args.PercentDone + " % ");
            }
        }
        bool CheckRequiredFields()
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
