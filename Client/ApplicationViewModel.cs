using System;
using System.Threading.Tasks;
using System.Windows;
using System.Net.Sockets;
using MyStream;
using System.IO;

namespace Client
{
    public class ApplicationViewModel : PropChanged
    {
        private string _UserName;
        public string UserName
        {
            get { return _UserName; }
            set
            {
                _UserName = value;
                OnPropertyChanged();
            }
        }

        private string _Chat;
        public string Chat
        {
            get { return _Chat; }
            set
            {
                _Chat = value;
                OnPropertyChanged();
            }
        }

        private string _Message;
        public string Message
        {
            get { return _Message; }
            set
            {
                _Message = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand ConnectCommand { get; }
        public RelayCommand SendCommand { get; }

        private TcpClient client;
        private const int Port = 8301;
        private const string IP = "192.168.0.3";

        public ApplicationViewModel()
        {
            ConnectCommand = new RelayCommand(Connect);
            SendCommand = new RelayCommand(Send);            
        }

        private void StartListeningServer()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    bool serverDown = false;

                    try
                    {
                        if (client?.Connected == true)
                        {
                            var myStreamReaderAndWriter = new MyStreamReaderAndWriter(client);
                            var getMessageToChat = myStreamReaderAndWriter.Read();

                            if (getMessageToChat.Message != null)
                            {
                                Chat += $"{getMessageToChat.Name}: {getMessageToChat.Message}" + "\n";
                            }
                            else
                            {
                                client.Close();
                            }
                        }
                    }
                    catch (IOException ioEx) when (ioEx.InnerException is SocketException sEx && sEx.ErrorCode == 10054)
                    {
                        serverDown = true;                      
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    if(serverDown)
                    {
                        Connect();
                        serverDown = false;
                    }
                }
            });
        }

        private void Connect(object obj)
        {
            Connect();
            StartListeningServer();
        }

        private void Connect()
        {
            client = new TcpClient();

            while (true)
            {
                try
                {
                    client.Connect(IP, Port);
                    break;
                }
                catch (SocketException sEx) when(sEx.ErrorCode == 10061)
                {
                    var result = MessageBox.Show("Server is down. Would you like to reconnect?", "Connection error", MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.No)
                    {
                        App.Current.Shutdown();
                    }
                }
            }
        }

        private async void Send(object obj)
        {
            if (client?.Connected == true && !string.IsNullOrWhiteSpace(Message))
            {
                try
                {
                    var sw = new MyStreamReaderAndWriter(client);
                    await sw.WriteAsync(new ChatMessageDto { Name = UserName, Message = Message });
                    Message = "";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
}
