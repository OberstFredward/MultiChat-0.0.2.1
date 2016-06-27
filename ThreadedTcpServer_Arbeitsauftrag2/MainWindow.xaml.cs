using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ThreadedTcpServer_Arbeitsauftrag2
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static ArrayList connectionListIp = new ArrayList();
        public static ArrayList connectionListChat = new ArrayList();
        private Task listener;
        private Task sender;
        private ConnectionThread connection;
        public static int port;
        public static Stack<ConnectionThread> connections = new Stack<ConnectionThread>(); 
        public MainWindow()
        {
            InitializeComponent();
            port = 50000;
            lblListenPort.Content = Convert.ToString(port);
            connection = new ConnectionThread(this);
            connections.Push(connection);
            listener = new Task(connection.HandleConnection);
            listener.Start();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            byte value = ConnectionThread.CheckIpAdress(tbxIp.Text);
            if (value == 0 || value == 2)
            {
                Dispatcher.InvokeAsync(delegate
                {
                    try
                    {
                        Chat newChat = new Chat(this, Convert.ToInt32(tbxPort.Text));
                        newChat.Show();
                        connectionListChat.Add(newChat);
                        connectionListIp.Add(tbxIp.Text);
                        MainWindow.connections.Push(connection);
                        MainWindow.port = MainWindow.port + 1;
                        lblListenPort.Content = port;
                        this.sender = new Task(() => Send("Hallo! Ich möchte gerne mit dir chatten."));
                        this.sender.Start();
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(exception.Message, "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    }
                });
            }
            else
            {
                MessageBox.Show("Die eingegebene IPv4 ist ungültig", "Ungültige IP", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
        }

        public void Send(string text)
        {
            try
            {
                TcpClient tcpClient = new TcpClient();
                UTF8Encoding encoding = new UTF8Encoding();
                Dispatcher.InvokeAsync(delegate
                {
                    try
                    {
                        tcpClient.Connect(IPAddress.Parse(tbxIp.Text), Convert.ToInt32(tbxPort.Text));
                        NetworkStream ns = tcpClient.GetStream();
                        byte[] bytes = encoding.GetBytes(text);
                        ns.Write(bytes, 0, bytes.Length);
                        tcpClient.Close();
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show("Die eingegebene IPv4 ist ungültig", "Ungültige IP", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                        return;
                    }

                });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
            }
        }
    }
    public class ConnectionThread
    {
        public bool exit = false;
        public static int ConnectionCounter { get; private set; }
        public TcpListener listener { get; private set; }
        public TcpClient client { get; private set; }
        private Dispatcher MainThreadDispatcher;
        private MainWindow mainWindow;
        private bool newListener;

        public ConnectionThread(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), MainWindow.port);
            newListener = false;
        }
        public void HandleConnection()
        {
            string[] dataString = null;
            client = null;
            NetworkStream ns = null;
            StreamReader reader = null;
            string TcpClientIp = "";
            while (!exit)
            {
                listener.Start();
                client = listener.AcceptTcpClient();
                ns = client.GetStream();
                reader = new StreamReader(ns);
                ConnectionCounter++;
                TcpClientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();



                if (MainWindow.connectionListIp.Contains(TcpClientIp)) //Bereits vorhandene Verbindung
                {
                    mainWindow.Dispatcher.InvokeAsync(delegate
                    {
                        Chat chatWindow = (Chat)MainWindow.connectionListChat[MainWindow.connectionListIp.IndexOf(TcpClientIp)];
                        if (!chatWindow.IsOpened)
                        {
                            MainWindow.connections.Push(this);
                            chatWindow = new Chat(mainWindow,MainWindow.port);
                            MainWindow.port = MainWindow.port + 1;
                            chatWindow.Show();
                        }
                        chatWindow.RefreshChatWindow(reader.ReadLine(), true);
                    });
                }
                else //Neue Verbindung
                {
                    mainWindow.Dispatcher.Invoke(delegate
                    {
                        Chat newChatWindow = new Chat(mainWindow,MainWindow.port);
                        newChatWindow.Show();
                        
                        MainWindow.connectionListIp.Add(TcpClientIp);
                        MainWindow.connectionListChat.Add(newChatWindow);
                        newChatWindow.RefreshChatWindow(reader.ReadLine(), true);
                    });
                    MainWindow.port = MainWindow.port + 1;
                    mainWindow.Dispatcher.InvokeAsync(() => mainWindow.lblListenPort.Content = MainWindow.port);
                    ConnectionThread newConnection = new ConnectionThread(mainWindow);
                    MainWindow.connections.Push(newConnection);
                    Task newTask = new Task(newConnection.HandleConnection);
                    newTask.Start();
                }
                Thread.Sleep(1000); //Wartet eine Sekunde und wartet dann erneut auf Anfragen
            }
        }

        public static byte CheckIpAdress(string ipString)
        //Byte Rückgabe ==>  0=LocalIPv4 | 1=LocalIPv6 | 2=ExternIPv4 | 3=ExternIPv6 | 255=Fehler
        {
            IPAddress address;
            if (IPAddress.TryParse(ipString, out address))
            {
                switch (address.AddressFamily)
                {
                    case AddressFamily.InterNetwork: //IPv4
                        string[] ipv4Blocks = ipString.Split('.');
                        byte block0 = Convert.ToByte(ipv4Blocks[0]);
                        byte block1 = Convert.ToByte(ipv4Blocks[1]);

                        if (block0 == 10 || (block0 == 172 && (block1 >= 16 && block1 <= 31)) ||
                            (block0 == 192 && block1 == 168) || block0 == 127) //LocalPrüfung
                        {
                            return 0; //LocalIPv4
                        }
                        else return 2; //ExternIPv4
                        break;
                    case AddressFamily.InterNetworkV6: //IPv6
                        if (address.IsIPv6LinkLocal) return 1; //LocalIPv6
                        else return 3; //ExternIPv6
                        break;
                    default:
                        return 255; //Fehler
                        break;
                }
            }
            return 255; //Fehler
        }
    }
}
