using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using System.Xml;

using System.IO;
using System.Security.Cryptography;
using DPCompress;
using Amazon;
using Amazon.Glacier;
using Amazon.Glacier.Model;
using Amazon.Glacier.Transfer;
using System.Collections.Specialized;
using System.Configuration;
using Amazon.Runtime;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.SecurityToken;
using System.Threading.Tasks;

using log4net;
using System;
using System.Windows.Forms;
using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;

using System.Threading;

using ICSharpCode.SharpZipLib.Tar;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace AWSGlaicer
{
    public partial class Form1 : Form
    {
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public StreamWriter tarLog;

        public String tarLogName;

        public StreamWriter glacierTransactionLog;

        public StreamWriter TarFileIdLog;

        int maxTARGBThreshold = 25;

        TarArchive archive;

        String archiveName;

        String awsVaultName;

        public TextBoxAppender textBoxAppender;


        public Form1()
        {
            InitializeComponent();

            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = "Live Log Viewer.exe";
            proc.StartInfo.UseShellExecute = false;

            proc.Start();

            awsVaultName = textBoxAWSVaultName.Text;

            fileLogFileDialog.FileName = "";

            if (log.IsInfoEnabled) log.Info("Application [AWS GLacier Tool] Start");

            log.Info(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

            log.Info("GlaierTool Start " + System.DateTime.Now);

            //bbcrevisit remove later
            textBoxAWSDynamoTableName.Text = "temp";
            textBoxAWSVaultName.Text = "temp";
            fileLogFileDialog.FileName = "files.csv";

        }

        private void createTARAndTxLog(int partNumber)
        {
            FileInfo fioi = new FileInfo(fileLogFileDialog.FileName);
            String slogId = fioi.Name.Replace(".csv", "");

            DateTime dt = new DateTime();
            dt = DateTime.Now;
            String dtString = dt.ToString();
            dtString = dtString.Replace("/", "_").Replace(":", "").Replace(" ", "");

            tarLogName = "GlacierTARBall_" + slogId + "_Part" + Convert.ToString(partNumber) + "_" + dtString + "_LOG.csv";
            tarLog = new StreamWriter(tarLogName);
            tarLog.WriteLine("Zip FileLoc" + "," + "SHA256 Compressed" + "," + "FileName" + "," + "SHA 256 Decompressed");

            archiveName = "GlacierTARBall_" + slogId + "_Part" + Convert.ToString(partNumber) + "_" + dtString + ".tar";

            log.Info("Making Tar Log : " + "GlacierTarLog_" + slogId + "_Part" + Convert.ToString(partNumber) + "_" + dtString + ".csv");

            log.Info("Tar FileName : " + "GlacierTARBall_" + slogId + "_Part" + Convert.ToString(partNumber) + "_" + dtString + ".tar");

            FileStream fs = File.Create(archiveName);

            TarOutputStream stream = new TarOutputStream((Stream)fs);

            archive = TarArchive.CreateOutputTarArchive(stream);
        }

        private void SetSlogFile_Click(object sender, EventArgs e)
        {
            fileLogFileDialog.ShowDialog();
        }

        private void SetOutDir_Click(object sender, EventArgs e)
        {
            outDir.ShowDialog();
        }

        private void Copy_Click(object sender, EventArgs e)
        {
            if (textBoxAWSVaultName.Text == String.Empty || textBoxAWSDynamoTableName.Text == String.Empty)
                throw new Exception("textBoxAWSVaultName.Text ==String.Empty || textBoxAWSDynamoTableName.Text==String.Empty");

            //AWS Requiers more than three charaters for a table name
            if (textBoxAWSDynamoTableName.Text.Length < 3)
                throw new Exception("AWS Dynamo Requiers more than three charaters for a table name");

            bool vaultExists = GalicierHelper.checkVault(textBoxAWSVaultName.Text);
            if (!vaultExists)
            {
                GalicierHelper.createVault(textBoxAWSVaultName.Text);
            }

            DynamoDBHelper.DynamoTableCheck(textBoxAWSDynamoTableName.Text);

            BulkTransfer(fileLogFileDialog.FileName);
        }

        private void BulkTransfer(String fileList)
        {
            FileInfo fioi = new FileInfo(fileList);

            String fileLogName = fioi.Name.Replace(".csv", "");

            DateTime dt = new DateTime();
            dt = DateTime.Now;
            String dtString = dt.ToString();
            dtString = dtString.Replace("/", "_").Replace(":", "").Replace(" ", "");

            String transactionLogName = "GlacierTransationLog_" + fileLogName + "_" + dtString + ".csv";
            glacierTransactionLog = new StreamWriter(transactionLogName);
            log.Info("Making ArchiveLog :" + transactionLogName);

            glacierTransactionLog.WriteLine("AWSVaultName" + "," + "ArchiveName" + "," + "ArchiveID" + "," + "SHA256 Tree Checksum Received" + "," + "SHA256 Tree Checksum Sent");

            String tarFileIdLogName = "Tar_FileID_" + fileLogName + "_" + dtString + ".csv";
            TarFileIdLog = new StreamWriter(tarFileIdLogName);
            log.Info("Making ArchiveLog :" + tarFileIdLogName);

            StreamWriter ResubmitJobFileIds = new StreamWriter(fileLogName + "AWSGlacierCopyResubmit" + dtString + ".csv");

            ArrayList serverName = new ArrayList();
            ArrayList directoryName = new ArrayList();
            ArrayList archiveFileList = new ArrayList();

            ArrayList fileLogLines = new ArrayList();
            String headers = String.Empty;

            try
            {
                StreamReader sr = new StreamReader(fileList);

                headers = sr.ReadLine();

                ResubmitJobFileIds.WriteLine(headers);

                String line = String.Empty;

                while ((line = sr.ReadLine()) != null)
                {
                    fileLogLines.Add(line);

                    string[] fields = line.Split(new char[] { ',' });

                    serverName.Add(fields[0]);
                    directoryName.Add(fields[1]);
                    archiveFileList.Add(fields[2]);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }

            try
            {
                int partNumber = 0;

                createTARAndTxLog(partNumber);
                
                //We compress, tar and transfer in chunkc of size maxTARGBThreshold
                for (int i = 0; i < archiveFileList.Count; i++)
                {
                    bool lastFile = i==archiveFileList.Count-1?true:false;

                    String sourceDirectory = String.Empty;
                    if (serverName[i] != String.Empty)
                        sourceDirectory = "\\\\" + serverName[i] + "\\" + directoryName[i] + "\\";
                    else
                        sourceDirectory = directoryName[i] + "\\";

                    System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
                    string sinkDirectory = Directory.GetCurrentDirectory();

                    long bytesTarred = 0;

                    FileInfo fiobt = new System.IO.FileInfo(archiveName);
                    bytesTarred = fiobt.Length;
                    
                    try
                    {
                        ArrayList awsDynamoFields = new ArrayList();
                        ArrayList awsDynamoVals = new ArrayList();

                        archiveFile(sinkDirectory, sourceDirectory, (string)archiveFileList[i], awsDynamoFields, awsDynamoVals);

                        awsDynamoFields.Add("ArchiveName");
                        awsDynamoVals.Add(archiveName);

                        string[] fieldsA = (string[])awsDynamoFields.ToArray(typeof(string));
                        string[] valsA = (string[])awsDynamoVals.ToArray(typeof(string));
                        String dynamoTableName = textBoxAWSDynamoTableName.Text;
                        DynamoDBHelper.MakeDynamoEntry(dynamoTableName, fieldsA, valsA);

                        TarFileIdLog.WriteLine(archiveName + "," + fileLogLines[i]);
                        TarFileIdLog.Flush();
                    }
                    catch (Exception ex)
                    {
                        ResubmitJobFileIds.WriteLine(fileLogLines[i]);
                        ResubmitJobFileIds.Flush();
                        log.Error(ex.ToString());
                    }


                    if (bytesTarred > (Math.Pow(10, 9) * maxTARGBThreshold) || lastFile)
                    {
                        glacierTransactionLog.Flush();

                        tarLog.Flush();

                        tarLog.Close();

                        TarEntry entry = TarEntry.CreateEntryFromFile(tarLogName);
                        archive.WriteEntry(entry, true);

                        archive.Close();

                        //Copy TarFiles and Logs To Scratch Place
                        DirectoryInfo di = new DirectoryInfo(sinkDirectory);
                        
                        FileInfo[] zFiles = di.GetFiles("*.z", SearchOption.TopDirectoryOnly);

                        for (int j = 0; j < zFiles.Length; j++)
                        {
                            FileInfo fio = zFiles[j];
                            fio.Delete();
                        }

                        //Write Out Remaining Log in case of problem
                        StreamWriter remainingFileLog = new StreamWriter(fileLogName + "_Recover_" + "_" + dtString + ".csv");
                        remainingFileLog.WriteLine(headers);

                        for (int k = i; k < serverName.Count; k++)
                        {
                            remainingFileLog.WriteLine(fileLogLines[k]);
                        }
                        remainingFileLog.Flush();
                        remainingFileLog.Close();

                        //Transfer Tar Here
                        log.Info("Calculating SHA256TreeHash on Tar");
                        FileStream inputFile = File.Open(archiveName, FileMode.Open, FileAccess.Read);
                        byte[] treeHash = ComputeSHA256TreeHash(inputFile);
                        String Checksum = BitConverter.ToString(treeHash).Replace("-", "").ToLower();
                        log.Info("Sending to Glacier:  " + archiveName + "With Checksum " + Checksum);

                        ArchiveUploadMultipartParallel aruo = new ArchiveUploadMultipartParallel(awsVaultName);

                        AWSArchiveResult ar = aruo.UploadFile(archiveName, "", archiveName);

                        glacierTransactionLog.WriteLine(awsVaultName + "," + archiveName + "," + ar.ArchiveID + "," + ar.Checksum + "," + Checksum);

                        glacierTransactionLog.Flush();

                        //Make Dynamo Entry for Archive
                        ArrayList awsDynamoFields = new ArrayList();
                        ArrayList awsDynamoVals = new ArrayList();
                                                
                        awsDynamoFields.Add("ArchiveName");
                        awsDynamoVals.Add(archiveName);

                        awsDynamoFields.Add("ArchiveId");
                        awsDynamoVals.Add(ar.ArchiveID);

                        awsDynamoFields.Add("ArchiveCheckSum");
                        awsDynamoVals.Add(ar.Checksum);

                        string[] fieldsA = (string[])awsDynamoFields.ToArray(typeof(string));
                        string[] valsA = (string[])awsDynamoVals.ToArray(typeof(string));
                        String dynamoTableName = textBoxAWSDynamoTableName.Text;
                        DynamoDBHelper.MakeDynamoEntry(dynamoTableName, fieldsA, valsA);
                        
                        if (!lastFile)
                        {
                            partNumber += 1;

                            createTARAndTxLog(partNumber);
                        }
                    }
                }
                ////Close and send the last TarBall
                //{
                //    glacierTransactionLog.Flush();

                //    tarLog.Flush();

                //    tarLog.Close();

                //    TarEntry entry = TarEntry.CreateEntryFromFile(tarLogName);
                //    archive.WriteEntry(entry, true);
                //    archive.Close();

                //    //Transfer Tar Here
                //    log.Info("Calculating SHA256TreeHash on Tar");
                //    FileStream inputFile = File.Open(archiveName, FileMode.Open, FileAccess.Read);
                //    byte[] treeHash = ComputeSHA256TreeHash(inputFile);
                //    String Checksum = BitConverter.ToString(treeHash).Replace("-", "").ToLower();
                //    log.Info("Sending to Glacier:  " + archiveName + "With Checksum " + Checksum);

                //    ArchiveUploadMultipartParallel aruo = new ArchiveUploadMultipartParallel(awsVaultName);

                //    AWSArchiveResult ar = aruo.UploadFile(archiveName, "", archiveName);

                //    glacierTransactionLog.WriteLine(awsVaultName + "," + archiveName + "," + ar.ArchiveID + "," + ar.Checksum + "," + Checksum);
                //    glacierTransactionLog.Flush();

                //    MessageBox.Show("AWS Glacier Transation Complete ");
                //    //Application.Exit();
                //}
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
        }

        private static string GetChecksum(string file)
        {
            using (var stream = new BufferedStream(File.OpenRead(file), 1200000))
            {
                SHA256Managed sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }

        public static byte[] ComputeSHA256TreeHash(FileStream inputFile)
        {
            byte[][] chunkSHA256Hashes = GetChunkSHA256Hashes(inputFile);
            return ComputeSHA256TreeHash(chunkSHA256Hashes);
        }

        public static byte[][] GetChunkSHA256Hashes(FileStream file)
        {
            int ONE_MB = 1024 * 1024;
            long numChunks = file.Length / ONE_MB;
            if (file.Length % ONE_MB > 0)
            {
                numChunks++;
            }

            if (numChunks == 0)
            {
                return new byte[][] { CalculateSHA256Hash(null, 0) };
            }
            byte[][] chunkSHA256Hashes = new byte[(int)numChunks][];

            try
            {
                byte[] buff = new byte[ONE_MB];

                int bytesRead;
                int idx = 0;

                while ((bytesRead = file.Read(buff, 0, ONE_MB)) > 0)
                {
                    chunkSHA256Hashes[idx++] = CalculateSHA256Hash(buff, bytesRead);
                }
                return chunkSHA256Hashes;
            }
            finally
            {
                if (file != null)
                {
                    try
                    {
                        file.Close();
                    }
                    catch (IOException ioe)
                    {
                        throw ioe;
                    }
                }
            }

        }

        //This method uses a pair of arrays to iteratively compute the tree hash
        //level by level. Each iteration takes two adjacent elements from the
        //previous level source array, computes the SHA-256 hash on their
        //concatenated value and places the result in the next level's destination
        //array. At the end of an iteration, the destination array becomes the
        //source array for the next level.
        public static byte[] ComputeSHA256TreeHash(byte[][] chunkSHA256Hashes)
        {
            byte[][] prevLvlHashes = chunkSHA256Hashes;
            while (prevLvlHashes.GetLength(0) > 1)
            {

                int len = prevLvlHashes.GetLength(0) / 2;
                if (prevLvlHashes.GetLength(0) % 2 != 0)
                {
                    len++;
                }

                byte[][] currLvlHashes = new byte[len][];

                int j = 0;
                for (int i = 0; i < prevLvlHashes.GetLength(0); i = i + 2, j++)
                {

                    // If there are at least two elements remaining
                    if (prevLvlHashes.GetLength(0) - i > 1)
                    {

                        // Calculate a digest of the concatenated nodes
                        byte[] firstPart = prevLvlHashes[i];
                        byte[] secondPart = prevLvlHashes[i + 1];
                        byte[] concatenation = new byte[firstPart.Length + secondPart.Length];
                        System.Buffer.BlockCopy(firstPart, 0, concatenation, 0, firstPart.Length);
                        System.Buffer.BlockCopy(secondPart, 0, concatenation, firstPart.Length, secondPart.Length);

                        currLvlHashes[j] = CalculateSHA256Hash(concatenation, concatenation.Length);

                    }
                    else
                    { // Take care of remaining odd chunk
                        currLvlHashes[j] = prevLvlHashes[i];
                    }
                }

                prevLvlHashes = currLvlHashes;
            }

            return prevLvlHashes[0];
        }

        public static byte[] CalculateSHA256Hash(byte[] inputBytes, int count)
        {
            SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] hash = sha256.ComputeHash(inputBytes, 0, count);
            return hash;
        }

        private void archiveFile(String sinkDirectory, string sourceDirectory, string fileName, ArrayList awsDynamoFields, ArrayList awsDynamoVals)
        {
            try
            {
                log.Info("Moving " + sourceDirectory + fileName + " to " + sinkDirectory);

                awsDynamoFields.Add("sourceDirectory"); awsDynamoVals.Add(sourceDirectory);

                awsDynamoFields.Add("fileName"); awsDynamoVals.Add(fileName);

                awsDynamoFields.Add("awsVaultName"); awsDynamoVals.Add(awsVaultName);

                awsDynamoFields.Add("tarLogName"); awsDynamoVals.Add(tarLogName);

                String sha256TreeHash = String.Empty;

                File.Copy(sourceDirectory + "\\" + fileName, sinkDirectory + "\\" + fileName, true);

                sha256TreeHash = String.Empty;

                if (doCompress.Checked == true)
                {
                    klDOTNETTimer kldnt = new klDOTNETTimer();

                    ParallelCompress.doNotUseTPL = false;
                    ParallelCompress.compressStrictSeqential = false;
                    String outfileName = String.Empty;

                    String fileNameLoc = sinkDirectory + "\\" + fileName;

                    FileStream inputFile = File.Open(fileNameLoc, FileMode.Open, FileAccess.Read);
                    kldnt.Start();
                    byte[] treeHash = ComputeSHA256TreeHash(inputFile);
                    String fileChecksum = BitConverter.ToString(treeHash).Replace("-", "").ToLower();

                    kldnt.Stop();
                    double itime = kldnt.Duration;
                    log.Info("dt Compute SHA256 Tree Hash : " + fileName + "  = " + Convert.ToString(itime));
                    sha256TreeHash = BitConverter.ToString(treeHash).Replace("-", "").ToLower();

                    log.Info("dt Compute SHA256 Hash : " + fileName + "  = " + Convert.ToString(itime));
                    log.Info("SHA 256 Tree : " + fileName + " = " + sha256TreeHash);

                    FileInfo fio = new System.IO.FileInfo(fileName);
                    String shortName = fileName.Replace(fio.Extension, "");
                    outfileName = sinkDirectory + "\\" + shortName + ".z";
                    log.Info("Compressing " + fileName + "\\n");
                    ParallelCompress.CompressFast(outfileName, fileNameLoc, true);

                    FileStream outputFileStream = File.Open(outfileName, FileMode.Open, FileAccess.Read);
                    byte[] treeHashZipFile = ComputeSHA256TreeHash(outputFileStream);
                    String sha256TreeHashZipFile = BitConverter.ToString(treeHashZipFile).Replace("-", "").ToLower();

                    TarEntry entry = TarEntry.CreateEntryFromFile(fileNameLoc);
                    archive.WriteEntry(entry, true);
                    log.Info("SHA256 Checksum  : " + fileNameLoc + " = " + fileChecksum);
                    tarLog.WriteLine(fileName + "," + fileChecksum + "," + shortName + ".z" + "," + sha256TreeHashZipFile);
                    awsDynamoFields.Add("sha256TreeHashUncompressed"); awsDynamoVals.Add(sha256TreeHash);
                    awsDynamoFields.Add("sha256TreeHashCompressed"); awsDynamoVals.Add(sha256TreeHashZipFile);
                    File.Delete(fileNameLoc);
                }
                else
                {
                    String fileNameLoc = sinkDirectory + "\\" + fileName;

                    //For files less than 1MB this is the same as the simple checksum.
                    FileStream inputFile = File.Open(fileNameLoc, FileMode.Open, FileAccess.Read);
                    byte[] treeHash = ComputeSHA256TreeHash(inputFile);

                    sha256TreeHash = BitConverter.ToString(treeHash).Replace("-", "").ToLower();

                    TarEntry entry = TarEntry.CreateEntryFromFile(fileNameLoc);
                    archive.WriteEntry(entry, true);

                    awsDynamoFields.Add("sha256TreeHashUncompressed"); awsDynamoVals.Add(sha256TreeHash);

                    tarLog.WriteLine(fileNameLoc + "," + sha256TreeHash + "," + fileName + "," + "NoCompression");

                    log.Info("SHA256 Checksum  : " + fileNameLoc + " = " + sha256TreeHash);

                    File.Delete(fileNameLoc);
                }

            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                throw ex;
            }
        }

        private void Restore_Click(object sender, EventArgs e)
        {
            String fileFilter = "GlaicerLogs (*GlacierTransationLog*.csv)|*GlacierTransationLog*.csv";//"Text files (*.txt)|*.txt
            fileLogFileDialog.Filter = fileFilter;
            fileLogFileDialog.ShowDialog();
            StreamReader sr = new StreamReader(fileLogFileDialog.FileName);

            String line = String.Empty;

            ArrayList taskArray = new ArrayList();

            int sleepSec = 0;

            String headers = sr.ReadLine();
            //AWSVaultName,ArchiveName,ArchiveID,SHA256 Tree Checksum Received,SHA256 Tree Checksum Sent
            string[] headerFields = headers.Split(new char[] { ',' });
            if (String.Compare(headerFields[0], "AWSVaultName") == -1)
                throw new Exception("Possible problem with restore file. Expecting AWSVaultName,ArchiveName,ArchiveID,SHA256 Tree Checksum Received,SHA256 Tree Checksum Sent");

            while ((line = sr.ReadLine()) != null)
            {
                AWSArchiveRquest are = new AWSArchiveRquest();

                string[] fields = line.Split(new char[] { ',' });

                are.VaultName = fields[0];

                are.Description = fields[1];

                are.FileName = fields[1];

                are.ArchiveID = fields[2];

                are.ChecksumTreeSHA256Compressed = fields[3];

                are.SleepSec = sleepSec;
                sleepSec += (int)(60 * 60 * 1);
                taskArray.Add(Task.Factory.StartNew(() => DownloadT(are)));
            }

            Task[] TArr = (Task[])taskArray.ToArray(typeof(Task));

            Task.WaitAll(TArr);

        }

        private void DownloadT(AWSArchiveRquest are)
        {
            int sleepms = (int)(are.SleepSec * 1000);

            Form1.log.Info("Sleep To Stagger Are Request " + Convert.ToString(sleepms) + "ms  " + are.ArchiveID + " " + are.Description + " " + are.FileName);

            Thread.Sleep(sleepms);
            AWSMoveFilesXDynamoMT mtdl = new AWSMoveFilesXDynamoMT(are.VaultName);
            mtdl.DownloadFile(are);
        }

        private void GlaiceirUpload_Click(object sender, EventArgs e)
        {
            try
            {
                FileStream inputFile = File.Open(archiveName, FileMode.Open, FileAccess.Read);
                byte[] treeHash = ComputeSHA256TreeHash(inputFile);
                String Checksum = BitConverter.ToString(treeHash).Replace("-", "").ToLower();

                AWSMoveFilesXDynamoMT mtdl = new AWSMoveFilesXDynamoMT(awsVaultName);
                AWSArchiveResult ar = mtdl.UploadFile(archiveName, Checksum, archiveName);

                log.Info("Archived " + archiveName);
                log.Info("Archive Checsum In " + Checksum);
                log.Info("Archive Checksum Out " + ar.Checksum);

            }
            catch (Exception exc)
            {
                log.Error(exc.Message);
            }
        }

        private void RetryGalcier_Click(object sender, EventArgs e)
        {
            try
            {
                DateTime dt = new DateTime();
                dt = DateTime.Now;
                String dtString = dt.ToString();
                dtString = dtString.Replace("/", "_").Replace(":", "").Replace(" ", "");

                openFileDialog1.ShowDialog();
                String archiveName = openFileDialog1.FileName;
                FileStream inputFile = File.Open(archiveName, FileMode.Open, FileAccess.Read);

                glacierTransactionLog = new StreamWriter("GlacierTransationLog_" + dtString + ".csv");
                glacierTransactionLog.WriteLine("AWSVaultName" + "," + "ArchiveName" + "," + "ArchiveID" + "," + "SHA256 Tree Checksum Received" + "," + "SHA256 Tree Checksum Sent");

                log.Info("Calculating SHA256TreeHash on Tar");

                byte[] treeHash = ComputeSHA256TreeHash(inputFile);
                String Checksum = BitConverter.ToString(treeHash).Replace("-", "").ToLower();
                log.Info("Sending to Glacier:  " + archiveName + "With Checksum " + Checksum);

                ArchiveUploadMultipartParallel aruo = new ArchiveUploadMultipartParallel(awsVaultName);

                AWSArchiveResult ar = aruo.UploadFile(archiveName, "", archiveName);

                glacierTransactionLog.WriteLine(awsVaultName + "," + archiveName + "," + ar.ArchiveID + "," + ar.Checksum + "," + Checksum);

                glacierTransactionLog.Flush();

                glacierTransactionLog.Close();

            }
            catch (Exception exc)
            {
                log.Error(exc.Message);
            }
        }

        private void textBoxAWSVaultName_TextChanged(object sender, System.EventArgs e)
        {
            awsVaultName = textBoxAWSVaultName.Text;
        }

        private void tarSizeUpDouwnControl_ValueChanged(object sender, System.EventArgs e)
        {
            maxTARGBThreshold = (int)tarSizeUpDouwnControl.Value;
        }

        private void ArchiveDirectory_Click(object sender, System.EventArgs e)
        {
            if (textBoxAWSVaultName.Text == String.Empty || textBoxAWSDynamoTableName.Text == String.Empty)
                throw new Exception("textBoxAWSVaultName.Text ==String.Empty || textBoxAWSDynamoTableName.Text==String.Empty");

            //AWS Requiers more than three charaters for a table name
            if (textBoxAWSDynamoTableName.Text.Length < 3)
                throw new Exception("AWS Dynamo Requiers more than three charaters for a table name");

            bool vaultExists = GalicierHelper.checkVault(textBoxAWSVaultName.Text);
            if (!vaultExists)
            {
                GalicierHelper.createVault(textBoxAWSVaultName.Text);
            }

            bool tableOK = DynamoDBHelper.DynamoTableCheck(textBoxAWSDynamoTableName.Text);

            if (!tableOK)
                throw new Exception("AWS  Dynamo Table is not OK.  Check AWS credentials.");

            folderBrowserDialog1.ShowDialog();
            String directory = folderBrowserDialog1.SelectedPath;

            DirectoryInfo di = new System.IO.DirectoryInfo(directory);

            string[] files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);

            DateTime dt = new DateTime();
            dt = DateTime.Now;
            String dtString = dt.ToString();
            dtString = dtString.Replace("/", "_").Replace(":", "").Replace(" ", "");

            String fileList = "fileList" + dtString + ".csv";

            StreamWriter sr = new System.IO.StreamWriter(fileList);

            for (int i = 0; i < files.Length; i++)
            {
                FileInfo fio = new System.IO.FileInfo(files[i]);
                sr.WriteLine("," + fio.Directory + "," + fio.Name);
            }
            sr.Flush();
            sr.Close();

            BulkTransfer(fileList);
        }
    }

    public class TextBoxAppender : AppenderSkeleton
    {
        private TextBox _textBox;
        public TextBox AppenderTextBox
        {
            get
            {
                return _textBox;
            }
            set
            {
                _textBox = value;
            }
        }
        public string FormName { get; set; }
        public string TextBoxName { get; set; }

        private Control FindControlRecursive(Control root, string textBoxName)
        {
            if (root.Name == textBoxName) return root;
            foreach (Control c in root.Controls)
            {
                Control t = FindControlRecursive(c, textBoxName);
                if (t != null) return t;
            }
            return null;
        }

        protected override void Append(log4net.Core.LoggingEvent loggingEvent)
        {
            if (_textBox == null)
            {
                if (String.IsNullOrEmpty(FormName) ||
                    String.IsNullOrEmpty(TextBoxName))
                    return;

                Form form = Application.OpenForms[FormName];
                if (form == null)
                    return;

                _textBox = (TextBox)FindControlRecursive(form, TextBoxName);
                if (_textBox == null)
                    return;

                form.FormClosing += (s, e) => _textBox = null;
            }
            _textBox.Invoke((MethodInvoker)delegate
            {
                _textBox.AppendText(loggingEvent.RenderedMessage + Environment.NewLine);
            });
        }
    }

}
