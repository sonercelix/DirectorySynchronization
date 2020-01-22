using System;
using System.Configuration;
using System.IO;
using WinSCP;

namespace DirectorySynchronization
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            WinSCP();
        }

        private static void WinSCP()
        {
            try
            {
                Log("*********************");
                Log("Program started");
                var appSettings = ConfigurationManager.AppSettings;
                string host = appSettings["SftpIp"];
                string userName = appSettings["SftpUserName"];
                string password = appSettings["SftpPassword"];
                // Setup session options
                SessionOptions sessionOptions = new SessionOptions
                {
                    GiveUpSecurityAndAcceptAnySshHostKey = true,
                    Protocol = Protocol.Sftp,
                    HostName = host,
                    UserName = userName,
                    Password = password
                };

                using (Session session = new Session())
                {
                    session.FileTransferred += FileTransferred;
                    string sourceDir = appSettings["SourcePath"];
                    string destDir = appSettings["DestinationPath"];
                    session.Open(sessionOptions);
                    if (session.Opened)
                    {
                        Log("SFTP Connected");
                        SynchronizationResult synchronizationResult;
                        synchronizationResult = session.SynchronizeDirectories(SynchronizationMode.Remote, sourceDir, destDir, true);
                        synchronizationResult.Check();

                        if (!synchronizationResult.IsSuccess)
                        {
                            Log("Error Unsuccessful");
                            foreach (SessionRemoteException item in synchronizationResult.Failures)
                            {
                                Log(item.Message);
                            }
                        }
                        else
                        {
                            Log("Successful");
                        }

                        Log("Uploaded: " + synchronizationResult.Uploads.Count);
                        Log("Removed: " + synchronizationResult.Removals.Count);

                        if (synchronizationResult.Uploads.Count > 0)
                        {
                            Log("Uploaded Files");
                            foreach (TransferEventArgs item in synchronizationResult.Uploads)
                            {
                                Log(item.FileName);
                            }
                        }

                        if (synchronizationResult.Removals.Count > 0)
                        {
                            Log("Removals Files");
                            foreach (RemovalEventArgs item in synchronizationResult.Removals)
                            {
                                Log(item.FileName);
                            }
                        }
                    }
                    else
                    {
                        Log("SFTP Not Connected");
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
            finally
            {
                Log("Program finished");
            }
        }

        private static void FileTransferred(object sender, TransferEventArgs e)
        {
            if (e.Error == null)
            {
                Console.WriteLine("Upload of {0} succeeded", e.FileName);
            }
            else
            {
                Console.WriteLine("Upload of {0} failed: {1}", e.FileName, e.Error);
            }

            if (e.Chmod != null)
            {
                if (e.Chmod.Error == null)
                {
                    Console.WriteLine(
                        "Permissions of {0} set to {1}", e.Chmod.FileName, e.Chmod.FilePermissions);
                }
                else
                {
                    Console.WriteLine(
                        "Setting permissions of {0} failed: {1}", e.Chmod.FileName, e.Chmod.Error);
                }
            }
            else
            {
                Console.WriteLine("Permissions of {0} kept with their defaults", e.Destination);
            }

            if (e.Touch != null)
            {
                if (e.Touch.Error == null)
                {
                    Console.WriteLine(
                        "Timestamp of {0} set to {1}", e.Touch.FileName, e.Touch.LastWriteTime);
                }
                else
                {
                    Console.WriteLine(
                        "Setting timestamp of {0} failed: {1}", e.Touch.FileName, e.Touch.Error);
                }
            }
            else
            {
                // This should never happen during "local to remote" synchronization
                Console.WriteLine(
                    "Timestamp of {0} kept with its default (current time)", e.Destination);
            }
        }

        private static void Log(string message)
        {
            try
            {
                string fileName = DateTime.Now.ToString("yyyyddMM") + ".txt";
                using (StreamWriter w = File.AppendText(fileName))
                {
                    w.WriteLine(string.Format("{0}|{1}", DateTime.Now.ToString("yyyyMMddHHmmss"), message));
                    Console.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}