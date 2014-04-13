using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace DPCompress
{
    /// <summary>
    /// A Compression utility library to Fast Compress files using Multi Core technology.  This library uses
    /// Microsoft .net Task Parallel library.  This library depends on .net framework version 4
    /// </summary>
    public class ParallelCompress
    {
        # region Variable declarations
        private static int sliceBytes = 1048576;
        private static byte strictByte1 = 255;
        private static byte strictByte2 = 49;
        private static byte strictByte3 = 234;
   
        #endregion

        #region Initializations
        /// <summary>
        /// Initializes the compression library
        /// </summary>
        static ParallelCompress()
        {
            doNotUseTPL = false;
            compressStrictSeqential = false;

        }

        #endregion

        #region Private member functions
        private static void CompressStream(byte[] bytesToCompress, int length, ref MemoryStream retStream)
        {
            //retStream = new MemoryStream();
            GZipStream gzStream = new GZipStream(retStream, CompressionMode.Compress, true);
            gzStream.Write(bytesToCompress, 0, length);
            gzStream.Close();
        }

        private static void CompressStreamP(byte[] bytesToCompress, int length, int index, ref MemoryStream[] listOfMemStream, AutoResetEvent eventToSignal)
        {
            eventToSignal.Set();
            MemoryStream stream = new MemoryStream();
            GZipStream gzStream = new GZipStream(stream, CompressionMode.Compress, true);
            gzStream.Write(bytesToCompress, 0, length);
            gzStream.Close();

            listOfMemStream[index] = stream;
        }

        private static void OpenFiles(string targetFileName, string sourceFileName, bool canOverWrite,
                                        out FileStream targetFileStream, out FileStream sourceFileStream)
        {
            sourceFileStream = new FileStream(sourceFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            if (canOverWrite)
            {
                try
                {
                    targetFileStream = new FileStream(targetFileName, FileMode.Truncate);
                }
                catch (System.IO.FileNotFoundException)
                {
                    targetFileStream = new FileStream(targetFileName, FileMode.CreateNew);
                }
            }
            else
            {
                targetFileStream = new FileStream(targetFileName, FileMode.CreateNew);
            }
        }

        private static byte[] GetBytesToStore(int length)
        {
            int lengthToStore = System.Net.IPAddress.HostToNetworkOrder(length);
            byte[] lengthInBytes = BitConverter.GetBytes(lengthToStore);
            string base64Enc = Convert.ToBase64String(lengthInBytes);
            byte[] finalStore = System.Text.Encoding.ASCII.GetBytes(base64Enc);

            return finalStore;
        }

        private static int GetLengthFromBytes(byte[] intToParse)
        {
            string base64Enc = System.Text.Encoding.ASCII.GetString(intToParse);
            byte[] normStr = Convert.FromBase64String(base64Enc);
            int length = BitConverter.ToInt32(normStr, 0);

            return System.Net.IPAddress.NetworkToHostOrder(length);
        }

        private static void CompressStrictSeq(FileStream targetStream, FileStream sourceStream)
        {
            byte[] bufferToCompress = new byte[sourceStream.Length];
            MemoryStream compressedStream = new MemoryStream();
            sourceStream.Read(bufferToCompress, 0, (int) sourceStream.Length);
            CompressStream(bufferToCompress, (int) sourceStream.Length, ref compressedStream);

            targetStream.WriteByte(strictByte1);
            targetStream.WriteByte(strictByte2);
            targetStream.WriteByte(strictByte3);

            byte[] compressedBytes = compressedStream.ToArray();

            //byte[] lengthToStore = GetBytesToStore(compressedBytes.Length);
            //targetStream.Write(lengthToStore, 0, lengthToStore.Length);

            targetStream.Write(compressedBytes, 0, compressedBytes.Length);
            compressedStream.Close();
        }

        private static void CompressSequential(FileStream targetStream, FileStream sourceStream)
        {
            byte[] bufferRead = new byte[sliceBytes];
            MemoryStream compressedStream = null;
            int read = 0;
            while(0 != (read = sourceStream.Read(bufferRead, 0, sliceBytes)))
            {
                compressedStream = new MemoryStream();
                CompressStream(bufferRead, read, ref compressedStream);
                byte[] lengthToStore = GetBytesToStore((int)compressedStream.Length);

                targetStream.Write(lengthToStore, 0, lengthToStore.Length);
                byte[] compressedBytes = compressedStream.ToArray();
                compressedStream.Close();
                compressedStream = null;
                targetStream.Write(compressedBytes, 0, compressedBytes.Length);
            }
        }


        private static void CompressSeq(FileStream targetStream, FileStream sourceStream)
        {
            if (compressStrictSeqential)
            {
                CompressStrictSeq(targetStream, sourceStream);

            }
            else
            {
                CompressSequential(targetStream, sourceStream);
            }

            targetStream.Close();
            sourceStream.Close();
        }

        private static void CompressParallel(FileStream targetStream, FileStream sourceStream)
        {
            MemoryStream[] listOfMemStream = new MemoryStream[(int)(sourceStream.Length / sliceBytes + 1)];

            byte[] bufferRead = new byte[sliceBytes];

            float noOfTasksF = ((float)sourceStream.Length / (float)sliceBytes);
            int noOfTasksI = (int) sourceStream.Length / sliceBytes;
            float toComp = noOfTasksI;
            Task[] tasks;
            if (toComp < noOfTasksF)
            {
                tasks = new Task[sourceStream.Length / sliceBytes + 1];
            }
            else
            {
                tasks = new Task[sourceStream.Length / sliceBytes];
            }
            
            int taskCounter = 0;
            int read = 0;
            AutoResetEvent eventSignal = new AutoResetEvent(false);

            while(0 != (read = sourceStream.Read(bufferRead, 0, sliceBytes)))
            {
                tasks[taskCounter] = Task.Factory.StartNew(() => CompressStreamP(bufferRead, read, taskCounter, ref listOfMemStream, eventSignal));
                eventSignal.WaitOne(-1);
                taskCounter++;
                bufferRead = new byte[sliceBytes];
            }

            Task.WaitAll(tasks);

            for (taskCounter = 0; taskCounter < tasks.Length; taskCounter++)
            {
                byte[] lengthToStore = GetBytesToStore((int)listOfMemStream[taskCounter].Length);

                targetStream.Write(lengthToStore, 0, lengthToStore.Length);
                byte[] compressedBytes = listOfMemStream[taskCounter].ToArray();
                listOfMemStream[taskCounter].Close();
                listOfMemStream[taskCounter] = null;
                targetStream.Write(compressedBytes, 0, compressedBytes.Length);
            }

            sourceStream.Close();
            targetStream.Close();
        }

        private static int Compress(string targetFileName, string sourceFileName, bool canOverWrite)
        {
            int begin = System.Environment.TickCount;
            FileStream sourceStream = null, targetStream = null;
            try
            {
                bool canUseTPL = !doNotUseTPL;
                OpenFiles(targetFileName, sourceFileName, canOverWrite, out targetStream, out sourceStream);

                if (canUseTPL)
                {
                    CompressParallel(targetStream, sourceStream);
                }
                else
                {
                    CompressSeq(targetStream, sourceStream);
                }
            }
            catch (Exception ex)
            {
                if (sourceStream != null)
                {
                    sourceStream.Close();
                }

                if (targetStream != null)
                {
                    targetStream.Close();
                }
                throw new ParallelCompressException(ex.Message, ex.InnerException);
            }
            return System.Environment.TickCount - begin;
        }

        private static void UnCompress(byte[] buffToUnCompress, ref byte[] unCompressedBuffer)
        {
            MemoryStream cmpStream = new MemoryStream(buffToUnCompress);

            GZipStream unCompZip = new GZipStream(cmpStream, CompressionMode.Decompress, true);

            MemoryStream unCmpStream = new MemoryStream(); 
            unCompressedBuffer = new byte[buffToUnCompress.Length];

            int read = 0;
            while (0 != (read = unCompZip.Read(unCompressedBuffer, 0, buffToUnCompress.Length)))
            {
                unCmpStream.Write(unCompressedBuffer, 0, read);
            }

            unCompressedBuffer = unCmpStream.ToArray();

            unCmpStream.Close();
            unCompZip.Close();
            cmpStream.Close();
        }

        private static void UnCompressP(byte[] buffToUnCompress, int index, AutoResetEvent eventToTrigger, ref MemoryStream[] memStream)
        {
            eventToTrigger.Set();
            MemoryStream cmpStream = new MemoryStream(buffToUnCompress);

            GZipStream unCompZip = new GZipStream(cmpStream, CompressionMode.Decompress, true);

            byte[] unCompressedBuffer = new byte[buffToUnCompress.Length];

            MemoryStream msToAssign = new MemoryStream();
            int read = 0;
            while (0 != (read = unCompZip.Read(unCompressedBuffer, 0, buffToUnCompress.Length)))
            {
                msToAssign.Write(unCompressedBuffer, 0, read);
            }
            memStream[index] = msToAssign;

            unCompZip.Close();
            cmpStream.Close();
        }

        private static void UnCompressStrictSeq(FileStream targetStream, FileStream sourceStream)
        {
            byte[] buffToRead = new byte[sourceStream.Length];

            int length = sourceStream.Read(buffToRead, 0, (int)sourceStream.Length);

            if (sourceStream.Length != length + 3)
            {
                throw new ParallelCompressException("Error Reading the Source file.  Disk or File IO Error");
            }
            else
            {
                byte[] retVal = null;
                UnCompress(buffToRead, ref retVal);
                targetStream.Write(retVal, 0, retVal.Length);
            }
        }

        private static void UnCompressSeq(FileStream targetStream, FileStream sourceStream)
        {
            byte firstByte = (byte) sourceStream.ReadByte();
            byte secByte = (byte) sourceStream.ReadByte();
            byte thirdByte = (byte) sourceStream.ReadByte();

            if (firstByte == strictByte1 &&
                secByte == strictByte2 &&
                thirdByte == strictByte3)
            {
                UnCompressStrictSeq(targetStream, sourceStream);
            }
            else
            {
                sourceStream.Seek(0,0);
                int readLength = 0;
                byte[] buffToReadLength = new byte[8];

                while (0 != (readLength = sourceStream.Read(buffToReadLength, 0, 8)))
                {
                    if (readLength != 8)
                    {
                        throw new ParallelCompressException("Possible file corruption. Error uncomressing the file.  Contact BK");
                    }

                    int lengthToRead = GetLengthFromBytes(buffToReadLength);
                    byte[] buffRead = new byte[lengthToRead];

                    if (lengthToRead != sourceStream.Read(buffRead, 0, lengthToRead))
                    {
                        throw new ParallelCompressException("Possible file corruption. Error uncomressing the file.  Contact BK");
                    }
                    else
                    {
                        byte[] retVal = null;
                        UnCompress(buffRead, ref retVal);
                        targetStream.Write(retVal, 0, retVal.Length);
                    }
                }

            }
        }

        private static void UnCompressParallel(FileStream targetStream, FileStream sourceStream)
        {
            byte firstByte = (byte) sourceStream.ReadByte();
            byte secByte = (byte) sourceStream.ReadByte();
            byte thirdByte = (byte) sourceStream.ReadByte();

            List<byte[]> listOfReadBytes = new List<byte[]>();

            if (firstByte == strictByte1 &&
                secByte == strictByte2 &&
                thirdByte == strictByte3)
            {
                throw new ParallelCompressException("The File is compressed in Strict Sequential Mode.  Cannot Uncompress in Parallel Mode");
            }
            else
            {
                sourceStream.Seek(0,0);
                int readLength = 0;
                byte[] buffToReadLength = new byte[8];

                while (0 != (readLength = sourceStream.Read(buffToReadLength, 0, 8)))
                {
                    if (readLength != 8)
                    {
                        throw new ParallelCompressException("Possible file corruption. Error uncomressing the file.  Contact BK");
                    }

                    int lengthToRead = GetLengthFromBytes(buffToReadLength);
                    byte[] buffRead = new byte[lengthToRead];

                    if (lengthToRead != sourceStream.Read(buffRead, 0, lengthToRead))
                    {
                        throw new ParallelCompressException("Possible file corruption. Error uncomressing the file.  Contact BK");
                    }
                    listOfReadBytes.Add(buffRead);
                }

            }

            Task[] tasks = new Task[listOfReadBytes.Count];
            MemoryStream[] memStreamArr = new MemoryStream[listOfReadBytes.Count];
            AutoResetEvent signalToTrigger = new AutoResetEvent(false);

            for (int counter = 0;counter < listOfReadBytes.Count; counter++)
            {
                tasks[counter] = Task.Factory.StartNew(() => UnCompressP(listOfReadBytes[counter], counter, signalToTrigger, ref memStreamArr));
                signalToTrigger.WaitOne(-1);
            }

            Task.WaitAll(tasks);

            for (int counter = 0;counter < listOfReadBytes.Count; counter++)
            {
                int length = (int) memStreamArr[counter].Length;
                byte[] buffToWrite = new byte[length];

                memStreamArr[counter].Seek(0, 0);
                memStreamArr[counter].Read(buffToWrite, 0, length);
                targetStream.Write(buffToWrite, 0, length);

            }

                   
        }


        private static int UnCompress(string targetFileName, string sourceFileName, bool canOverWrite)
        {
            int begin = System.Environment.TickCount;

            FileStream sourceStream = null, targetStream = null;

            try
            {
                bool canUseTPL = !doNotUseTPL;
                OpenFiles(targetFileName, sourceFileName, canOverWrite, out targetStream, out sourceStream);

                if (canUseTPL)
                {
                    UnCompressParallel(targetStream, sourceStream);
                }
                else
                {
                    UnCompressSeq(targetStream, sourceStream);
                }

                targetStream.Close();
                sourceStream.Close();
            }
            catch (Exception ex)
            {
                if (sourceStream != null)
                {
                    sourceStream.Close();
                }

                if (targetStream != null)
                {
                    targetStream.Close();
                }
                throw new ParallelCompressException(ex.Message, ex.InnerException);
            }
            return System.Environment.TickCount - begin;
        }
        #endregion

        #region Public properties
        public static bool doNotUseTPL
        {
            set;
            get;
        }

        public static bool compressStrictSeqential
        {
            get;
            set;
        }
        #endregion

        #region Public member functions
        /// <summary>
        /// Fast Compress a given file.  Writes the compressed stream into a target File of choice
        /// </summary>
        /// <param name="Target File Name param"></param>
        /// <param name="Source File Name Param"></param>
        public static int CompressFast(string targetFileparam, string sourceFileParam)
        {
            return Compress(targetFileparam, sourceFileParam, false);
        }

        /// <summary>
        /// Fast Compress a given file.  Writes the compressed stream into a target File of choice
        /// </summary>
        /// <param name="Target File Name param"></param>
        /// <param name="Source File Name Param"></param>
        /// /// <param name="Overwrite if target file exists"></param>
        public static int CompressFast(string targetFileparam, string sourceFileParam, bool overWrite)
        {
            return Compress(targetFileparam, sourceFileParam, overWrite);
        }

        /// <summary>
        /// Fast Compress a given file.  Writes the compressed stream into a target file as SourceFileName.cprss
        /// </summary>
        /// <param name="File Name to compress"></param>
        public static int CompressFast(string sourceFileParam)
        {
            return Compress(sourceFileParam + ".cmpress", sourceFileParam, false);
        }

        /// <summary>
        /// Fast Compress a given file.  Writes the compressed stream into a target file as SourceFileName.cprss
        /// </summary>
        /// <param name="File Name to compress"></param>
        /// <param name="Overwrite if target file exists"></param>
        public static int CompressFast(string sourceFileParam, bool overWrite)
        {

            return Compress(sourceFileParam + ".cmpress", sourceFileParam, overWrite);

        }

        /// <summary>
        /// Fast Uncompresses a given file.  Writes the stream into a target file of choice
        /// </summary>
        /// <param name="Target File to store the uncompressed stream"></param>
        /// <param name="Source file name to perform uncompression"></param>
        public static int UncompressFast(string targetFileParam, string sourceFileParam)
        {
            return UnCompress(targetFileParam, sourceFileParam, false);
            
        }

        /// <summary>
        /// Fast Uncompresses a given file.  Writes the stream into a target file of choice
        /// </summary>
        /// <param name="Target File to store the uncompressed stream"></param>
        /// <param name="Source file name to perform uncompression"></param>
        /// <param name="Overwrite file if already exists"></param>
        public static int UncompressFast(string targetFileParam, string sourceFileParam, bool overWrite)
        {
            return UnCompress(targetFileParam, sourceFileParam, overWrite);

        }

     
        #endregion
    }
    
    public class ParallelCompressException : Exception
    {
        public ParallelCompressException()
            : base()
        {
        }

        public ParallelCompressException(string message)
            : base(message)
        {
        }

        public ParallelCompressException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
