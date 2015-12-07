using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Net.NetworkInformation;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System.IO;
using System.Text;

namespace HttpLibrary
{ 
    /// <summary>
    /// on error interface
    /// </summary>
    /// <param name="sender">sender object</param>
    /// <param name="e">arguments</param>
    public delegate void OnErrorDelegate(object sender, OnErrorEventArgs e);

    /// <summary>
    /// on request received interface
    /// </summary>
    /// <param name="sender">sender object</param>
    /// <param name="e">arguments</param>
    public delegate void OnRequestReceivedDelegate(object sender, OnRequestReceivedArgs e);

    /// <summary>
    /// credentials class
    /// </summary>
    public class ServerCredentials
    {
        /// <summary>
        /// server owner used in the username/password display form 
        /// </summary>
        public string ServerOwner;
        /// <summary>
        /// username
        /// </summary>
        public string UserName;
        /// <summary>
        /// password
        /// </summary>
        public string Password;
        /// <summary>
        /// Base64 encrypted username and password 
        /// </summary>
        public string Key;
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="ServerOwner">owner name</param>
        /// <param name="UserName">username</param>
        /// <param name="Password">password</param>
        public ServerCredentials(string ServerOwner, string UserName, string Password)
        {
            this.ServerOwner = ServerOwner;
            this.UserName = UserName;
            this.Password = Password;
            this.Key = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(UserName + ":" + Password));
        }
        /// <summary>
        /// function for reading a saved file 
        /// </summary>
        /// <param name="FileName">filename only</param>
        /// <returns>constructed ServerCredentials object read from a saved file</returns>
        public static ServerCredentials ReadFromFile(string FileName)
        {
            FileStream fs = new FileStream(@"\SD\" + FileName + ".crdn", FileMode.Open, FileAccess.Read);
            StreamReader Reader = new StreamReader(fs);
            string owner = Reader.ReadLine();
            string keeey = Reader.ReadLine();
            Reader.Close();
            fs.Close();
            string[] unpass = new string(UTF8Encoding.UTF8.GetChars(Convert.FromBase64String(keeey))).Split(':');
            return new ServerCredentials(owner, unpass[0], unpass[1]);
        }
        /// <summary>
        /// function for writing credentials into file
        /// </summary>
        /// <param name="FileName">filename only</param>
        /// <param name="Credentials">ServerCredentials object to save</param>
        public static void WriteToFile(string FileName, ServerCredentials Credentials)
        {
            FileStream fs = new FileStream(@"\SD\" + FileName + ".crdn", FileMode.Create, FileAccess.Write);
            StreamWriter Writer = new StreamWriter(fs);
            Writer.WriteLine(Credentials.ServerOwner);
            Writer.WriteLine(Credentials.Key);
            Writer.Close();
            fs.Close();
        }
    }
    /// <summary>
    /// server configuration class
    /// </summary>
    public class ServerConfiguration
    {
        /// <summary>
        /// listening ip address
        /// </summary>
        public string IpAddress;
        /// <summary>
        /// network subnet mask
        /// </summary>
        public string SubnetMask;
        /// <summary>
        /// usually routers ip address
        /// </summary>
        public string DefaultGateWay;
        /// <summary>
        /// listening port
        /// </summary>
        public int ListenPort;
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="IpAddress">listening ip address</param>
        /// <param name="SubnetMask">network subnet mask</param>
        /// <param name="DefaultGateWay">default gateway ip address</param>
        /// <param name="ListenPort">listening port</param>
        public ServerConfiguration(string IpAddress, string SubnetMask, string DefaultGateWay, int ListenPort)
        {
            this.IpAddress = IpAddress;
            this.SubnetMask = SubnetMask;
            this.DefaultGateWay = DefaultGateWay;
            this.ListenPort = ListenPort;
        }
    }

    /// <summary>
    /// error arguments passed when event is fired
    /// </summary>
    public class OnErrorEventArgs : EventArgs
    {
        private string EVENT_MESSAGE;
        /// <summary>
        /// event message
        /// </summary>
        public string EventMessage
        {
            get { return EVENT_MESSAGE; }
        }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="EVENT_MESSAGE">message</param>
        public OnErrorEventArgs(string EVENT_MESSAGE)
        {
            this.EVENT_MESSAGE = EVENT_MESSAGE;
        }
    }
    /// <summary>
    /// request received arguments passed when a request received event fires
    /// </summary>
    public class OnRequestReceivedArgs : EventArgs
    {
        private string FILE_NAME;
        private bool IS_IN_MMC;
        private byte[] REQUEST;
        /// <summary>
        /// name of the file in the request
        /// </summary>
        public string FileName
        {
            get
            {
                return FILE_NAME;
            }
        }
        /// <summary>
        /// is file in memory card
        /// </summary>
        public bool IsInMemoryCard
        {
            get
            {
                return IS_IN_MMC;
            }
        }
        /// <summary>
        /// request itself
        /// </summary>
        public byte[] Request
        {
            get
            {
                return REQUEST;
            }
        }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="FILE_NAME">filename</param>
        /// <param name="IS_IN_MMC">is in memory card</param>
        /// <param name="REQUEST">http request</param>
        public OnRequestReceivedArgs(string FILE_NAME, bool IS_IN_MMC, byte[] REQUEST)
        {
            this.FILE_NAME = FILE_NAME;
            this.IS_IN_MMC = IS_IN_MMC;
            this.REQUEST = REQUEST;
        }
    }
    /// <summary>
    /// HttpServer class
    /// </summary>
    public class HttpServer
    {
        private Thread SERVER_THREAD;
        private Socket LISTEN_SOCKET;
        private Socket ACCEPTED_SOCKET;
        private bool IS_SERVER_RUNNING;
        private string STORAGE_PATH;
        private FileStream FILE_STREAM;
        private StreamWriter FILE_WRITER;
        private byte[] RECEIVE_BUFFER;
        private byte[] SEND_BUFFER;
        private ServerConfiguration CONFIG;
        private ServerCredentials CREDENTIALS;
        private bool USE_AUTHENTICATION;
        private enum FileType { JPEG = 1, GIF = 2, Html = 3, ICO = 4, CSS = 5, JS = 6 };
        private string HtmlPageHeader = "HTTP/1.0 200 OK\r\nContent-Type: ";
        private string authheader = "HTTP/1.1 401 Authorization Required \nWWW-Authenticate: Basic realm=";
        private string Unauthorized = "<html><body><h1 align=center>" + "401 UNAUTHORIZED ACCESS</h1></body></html>";

        private void FragmentateAndSend(string FileName, FileType Type)
        {
            byte[] HEADER;
            long FILE_LENGTH;
            FILE_STREAM = new FileStream(STORAGE_PATH + "\\" + FileName, FileMode.Open, FileAccess.Read);
            FILE_LENGTH = FILE_STREAM.Length;

            switch (Type)
            {
                case FileType.Html:
                    HEADER = UTF8Encoding.UTF8.GetBytes(HtmlPageHeader + "text/html" + "; charset=utf-8\r\nContent-Length: " + FILE_LENGTH.ToString() + "\r\n\r\n");
                    break;
                case FileType.GIF:
                    HEADER = UTF8Encoding.UTF8.GetBytes(HtmlPageHeader + "image/gif" + "; charset=utf-8\r\nContent-Length: " + FILE_LENGTH.ToString() + "\r\n\r\n");
                    break;
                case FileType.JPEG:
                    HEADER = UTF8Encoding.UTF8.GetBytes(HtmlPageHeader + "image/jpeg" + "; charset=utf-8\r\nContent-Length: " + FILE_LENGTH.ToString() + "\r\n\r\n");
                    break;
                case FileType.ICO:
                    HEADER = UTF8Encoding.UTF8.GetBytes(HtmlPageHeader + "image/ico" + "; charset=utf-8\r\nContent-Length: " + FILE_LENGTH.ToString() + "\r\n\r\n");
                    break;

                case FileType.CSS:
                    HEADER = UTF8Encoding.UTF8.GetBytes(HtmlPageHeader + "text/css" + "; charset=utf-8\r\nContent-Length: " + FILE_LENGTH.ToString() + "\r\n\r\n");
                    break;

                case FileType.JS:
                    HEADER = UTF8Encoding.UTF8.GetBytes(HtmlPageHeader + "application/x-javascript" + "; charset=utf-8\r\nContent-Length: " + FILE_LENGTH.ToString() + "\r\n\r\n");
                    break;

                default:
                    HEADER = UTF8Encoding.UTF8.GetBytes(HtmlPageHeader + "text/html" + "; charset=utf-8\r\nContent-Length: " + FILE_LENGTH.ToString() + "\r\n\r\n");
                    break;
            }

            ACCEPTED_SOCKET.Send(HEADER, 0, HEADER.Length, SocketFlags.None);
            while (FILE_LENGTH > SEND_BUFFER.Length)
            {
                FILE_STREAM.Read(SEND_BUFFER, 0, SEND_BUFFER.Length);
                ACCEPTED_SOCKET.Send(SEND_BUFFER, 0, SEND_BUFFER.Length, SocketFlags.None);
                FILE_LENGTH -= SEND_BUFFER.Length;
            }
            FILE_STREAM.Read(SEND_BUFFER, 0, (int)FILE_LENGTH);
            ACCEPTED_SOCKET.Send(SEND_BUFFER, 0, (int)FILE_LENGTH, SocketFlags.None);

            FILE_STREAM.Close();
        }
        private string GetFileName(string RequestStr)
        {
            RequestStr = RequestStr.Substring(RequestStr.IndexOf("GET /") + 5);
            RequestStr = RequestStr.Substring(0, RequestStr.IndexOf("HTTP"));
            return RequestStr.Trim();
        }
        private bool RequestContains(string Request, string Str)
        {
            return (Request.IndexOf(Str) >= 0);
        }

        private string GetFileExtention(string FILE_NAME)
        {
            string x = FILE_NAME;
            x = x.Substring(x.LastIndexOf('.') + 1);
            return x;
        }

        private bool isFileExists(string FileName)
        {
            return File.Exists(STORAGE_PATH + "\\" + FileName);
        }

        private void ProcessRequest()
        {
            string REQUEST = "";
            string FILE_NAME = "";
            bool found = false;
            ACCEPTED_SOCKET.Receive(RECEIVE_BUFFER);
            if (USE_AUTHENTICATION)
            {
                if (Authenticate(RECEIVE_BUFFER))
                {
                    REQUEST = new string(UTF8Encoding.UTF8.GetChars(RECEIVE_BUFFER));
                    FILE_NAME = GetFileName(REQUEST);
                    found = isFileExists(FILE_NAME);
                    OnRequestReceivedFunction(new OnRequestReceivedArgs(FILE_NAME, found, RECEIVE_BUFFER));
                }
                else
                {
                    byte[] header = UTF8Encoding.UTF8.GetBytes(authheader + CREDENTIALS.ServerOwner + "\"\n\n");
                    ACCEPTED_SOCKET.Send(header, 0, header.Length, SocketFlags.None);
                    ACCEPTED_SOCKET.Send(UTF8Encoding.UTF8.GetBytes(Unauthorized), 0, Unauthorized.Length, SocketFlags.None);
                }
            }
            else
            {
                REQUEST = new string(UTF8Encoding.UTF8.GetChars(RECEIVE_BUFFER));
                FILE_NAME = GetFileName(REQUEST);
                found = isFileExists(FILE_NAME);
                OnRequestReceivedFunction(new OnRequestReceivedArgs(FILE_NAME, found, RECEIVE_BUFFER));
            }
            for (int i = 0; i < RECEIVE_BUFFER.Length; i++) RECEIVE_BUFFER[i] = 0;
        }
        private void RunServer()
        {
            try
            {
                LISTEN_SOCKET = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint BindingAddress = new IPEndPoint(IPAddress.Any, CONFIG.ListenPort);
                LISTEN_SOCKET.Bind(BindingAddress);
                LISTEN_SOCKET.Listen(1);
                IS_SERVER_RUNNING = true;
                while (true)
                {
                    ACCEPTED_SOCKET = LISTEN_SOCKET.Accept();
                    ProcessRequest();
                    ACCEPTED_SOCKET.Close();
                }
            }
            catch (Exception)
            {
                IS_SERVER_RUNNING = false;
                OnServerErrorFunction(new OnErrorEventArgs("Server Error\r\nCheck Connection Parameters"));
            }
        }
        private bool Authenticate(byte[] request)
        {
            return RequestContains(new string(UTF8Encoding.UTF8.GetChars(request)), CREDENTIALS.Key);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnServerErrorFunction(OnErrorEventArgs e)
        {
            OnServerError(this, e);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnRequestReceivedFunction(OnRequestReceivedArgs e)
        {
            OnRequestReceived(this, e);
        }

        /// <summary>
        /// returns if server uses credentials
        /// </summary>
        public bool SecurityEnabled
        {
            get
            {
                return USE_AUTHENTICATION;
            }
        }
        /// <summary>
        /// the servers configuration parameters
        /// </summary>
        public ServerConfiguration Configuration
        {
            get
            {
                return CONFIG;
            }
        }
        /// <summary>
        /// returns server configuration
        /// </summary>
        public bool IsServerRunning
        {
            get { return IS_SERVER_RUNNING; }
        }
        /// <summary>
        /// the server running thread handle
        /// </summary>
        public Thread RunningThread
        {
            get { return SERVER_THREAD; }
        }
        /// <summary>
        /// event fired when an error occures
        /// </summary>
        public event OnErrorDelegate OnServerError;
        /// <summary>
        /// an event fired when server receives a request from a client
        /// </summary>
        public event OnRequestReceivedDelegate OnRequestReceived;
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="Config">server configuration object</param>
        /// <param name="ReceiveBufferSize">receiving buffer size in bytes</param>
        /// <param name="SendBufferSize">sending buffer size in bytes</param>
        /// <param name="pages_folder">usually @"\SD" which is the sd card directory path</param>
        public HttpServer(ServerConfiguration Config, int ReceiveBufferSize, int SendBufferSize, string pages_folder)
        {
            SERVER_THREAD = null;
            LISTEN_SOCKET = null;
            ACCEPTED_SOCKET = null;
            IS_SERVER_RUNNING = false;
            STORAGE_PATH = pages_folder;
            RECEIVE_BUFFER = new byte[ReceiveBufferSize];
            SEND_BUFFER = new byte[SendBufferSize];
            CONFIG = Config;
            USE_AUTHENTICATION = false;
            
            if (!File.Exists(STORAGE_PATH + "\\index.txt"))
            {
                FILE_STREAM = new FileStream(STORAGE_PATH + "\\index.txt", FileMode.Create, FileAccess.Write);
                FILE_WRITER = new StreamWriter(FILE_STREAM);
                FILE_WRITER.WriteLine("<html>");
                FILE_WRITER.WriteLine("<head>");
                FILE_WRITER.WriteLine("<title>");
                FILE_WRITER.WriteLine("Index Page");
                FILE_WRITER.WriteLine("</title>");
                FILE_WRITER.WriteLine("<body>");
                FILE_WRITER.WriteLine("<h1 align=center>");
                FILE_WRITER.WriteLine("FILE LIST");
                FILE_WRITER.WriteLine("</h1>");
                FILE_WRITER.WriteLine("</body>");
                FILE_WRITER.WriteLine("</html>");
                FILE_WRITER.Close();
                FILE_STREAM.Close();
            }
            if (!File.Exists(STORAGE_PATH + "\\NotFound.txt"))
            {
                FILE_STREAM = new FileStream(STORAGE_PATH + "\\NotFound.txt", FileMode.Create, FileAccess.Write);
                FILE_WRITER = new StreamWriter(FILE_STREAM);
                FILE_WRITER.WriteLine("<html>");
                FILE_WRITER.WriteLine("<head>");
                FILE_WRITER.WriteLine("<title>");
                FILE_WRITER.WriteLine("Page Not Found");
                FILE_WRITER.WriteLine("</title>");
                FILE_WRITER.WriteLine("<body>");
                FILE_WRITER.WriteLine("<h1 align=center>");
                FILE_WRITER.WriteLine("Page Not Found");
                FILE_WRITER.WriteLine("</h1>");
                FILE_WRITER.WriteLine("</body>");
                FILE_WRITER.WriteLine("</html>");
                FILE_WRITER.Close();
                FILE_STREAM.Close();
            }
        }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="Config">server configuration object</param>
        /// <param name="Credentials">server credentials object</param>
        /// <param name="ReceiveBufferSize">receiving buffer size in bytes</param>
        /// <param name="SendBufferSize">sending buffer size in bytes</param>
        /// <param name="pages_folder">usually @"\SD" which is the sd card directory path</param>
        public HttpServer(ServerConfiguration Config,ServerCredentials Credentials, int ReceiveBufferSize, int SendBufferSize, string pages_folder)
        {
            SERVER_THREAD = null;
            LISTEN_SOCKET = null;
            ACCEPTED_SOCKET = null;
            IS_SERVER_RUNNING = false;
            STORAGE_PATH = pages_folder;
            RECEIVE_BUFFER = new byte[ReceiveBufferSize];
            SEND_BUFFER = new byte[SendBufferSize];
            CONFIG = Config;
            this.CREDENTIALS = Credentials;
            USE_AUTHENTICATION = true;

            if (!File.Exists(STORAGE_PATH + "\\index.txt"))
            {
                FILE_STREAM = new FileStream(STORAGE_PATH + "\\index.txt", FileMode.Create, FileAccess.Write);
                FILE_WRITER = new StreamWriter(FILE_STREAM);
                FILE_WRITER.WriteLine("<html>");
                FILE_WRITER.WriteLine("<head>");
                FILE_WRITER.WriteLine("<title>");
                FILE_WRITER.WriteLine("Index Page");
                FILE_WRITER.WriteLine("</title>");
                FILE_WRITER.WriteLine("<body>");
                FILE_WRITER.WriteLine("<h1 align=center>");
                FILE_WRITER.WriteLine("FILE LIST");
                FILE_WRITER.WriteLine("</h1>");
                FILE_WRITER.WriteLine("</body>");
                FILE_WRITER.WriteLine("</html>");
                FILE_WRITER.Close();
                FILE_STREAM.Close();
            }
            if (!File.Exists(STORAGE_PATH + "\\NotFound.txt"))
            {
                FILE_STREAM = new FileStream(STORAGE_PATH + "\\NotFound.txt", FileMode.Create, FileAccess.Write);
                FILE_WRITER = new StreamWriter(FILE_STREAM);
                FILE_WRITER.WriteLine("<html>");
                FILE_WRITER.WriteLine("<head>");
                FILE_WRITER.WriteLine("<title>");
                FILE_WRITER.WriteLine("Page Not Found");
                FILE_WRITER.WriteLine("</title>");
                FILE_WRITER.WriteLine("<body>");
                FILE_WRITER.WriteLine("<h1 align=center>");
                FILE_WRITER.WriteLine("Page Not Found");
                FILE_WRITER.WriteLine("</h1>");
                FILE_WRITER.WriteLine("</body>");
                FILE_WRITER.WriteLine("</html>");
                FILE_WRITER.Close();
                FILE_STREAM.Close();
            }
        }
        /// <summary>
        /// starts the server 
        /// </summary>
        public void Start()
        {
            SERVER_THREAD = new Thread(new ThreadStart(RunServer));
            SERVER_THREAD.Start();
        }
        /// <summary>
        /// stops the server
        /// </summary>
        public void Stop()
        {
            LISTEN_SOCKET.Close();
        }
        /// <summary>
        /// sends a file from mmc 
        /// </summary>
        /// <param name="FileName">complete file name ex: \SD\test.html</param>
        public void Send(string FileName)
        {
            string FILE_EXTENTION = GetFileExtention(FileName.ToLower());
            switch (FILE_EXTENTION)
            {
                case "gif":
                    FragmentateAndSend(FileName, FileType.GIF);
                    break;
                case "txt":
                    FragmentateAndSend(FileName, FileType.Html);
                    break;
                case "jpg":
                    FragmentateAndSend(FileName, FileType.JPEG);
                    break;
                case "jpeg":
                    FragmentateAndSend(FileName, FileType.JPEG);
                    break;
                case "ico":
                    FragmentateAndSend(FileName, FileType.ICO);
                    break;
                case "htm":
                    FragmentateAndSend(FileName, FileType.Html);
                    break;
                case "html":
                    FragmentateAndSend(FileName, FileType.Html);
                    break;
                case "css":
                    FragmentateAndSend(FileName, FileType.CSS);
                    break;
                case "js":
                    FragmentateAndSend(FileName, FileType.JS);
                    break;
                default:
                    FragmentateAndSend(FileName, FileType.Html);
                    break;
            }
        }

        /// <summary>
        /// sends an array of bytes in chunks of 256 if greater than 256
        /// </summary>
        /// <param name="data">byte array data to send</param>
        public void Send(byte[] data)
        {
            int datalength = data.Length;
            int i = 0;
            while (datalength > 256)
            {
                ACCEPTED_SOCKET.Send(data, i, 256, SocketFlags.None);
                i += 256;
                datalength -= 256;
            }
            ACCEPTED_SOCKET.Send(data, i, datalength, SocketFlags.None);
        }
        /// <summary>
        /// sends a 404 not found page
        /// </summary>
        public void SendNotFound()
        {
            FragmentateAndSend(STORAGE_PATH + "\\NotFound.txt", FileType.Html);
        }

        public void SendInternalServerError(string reason)
        {
            string Error500Body = "<!DOCTYPE html><meta charset=\"utf-8\"><body><H1>HTTP Error 500 – Internal server error</H1><p>" + reason + "</p></body>";
            byte[] bytes = Encoding.UTF8.GetBytes(Error500Body);
            string header = "HTTP/1.0 200 OK\r\nContent-Type: text/html" + "; charset=utf-8\r\nContent-Length: " + bytes.Length + "\r\n\r\n";

            Send(Encoding.UTF8.GetBytes(header));
            Send(bytes);
        }

        public void SendOK()
        {
            string emptyBody = "<!DOCTYPE html><meta charset=\"utf-8\"><body></body>";
            byte[] bytes = Encoding.UTF8.GetBytes(emptyBody);
            string header = "HTTP/1.0 200 OK\r\nContent-Type: text/html" + "; charset=utf-8\r\nContent-Length: " + bytes.Length + "\r\n\r\n";

            Send(Encoding.UTF8.GetBytes(header));
            Send(bytes);
        }

    }
}
