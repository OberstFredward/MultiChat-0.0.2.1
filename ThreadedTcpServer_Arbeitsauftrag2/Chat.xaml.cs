using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ThreadedTcpServer_Arbeitsauftrag2
{
    /// <summary>
    /// Interaktionslogik für Chat.xaml
    /// </summary>
    public partial class Chat : Window, IDisposable
    {
        public static int ChatCounter { get; private set; }
        private int chatNumber;
        private bool disposed = false;
        private int counter = 0;
        private int port;
        public bool IsOpened { get; private set; }
        private MainWindow mainWindow;
        public Chat(MainWindow mainWindow,int port)
        {
            InitializeComponent();
            this.port = port;
            lblPort.Content = port;
            txbEingabe.Text = "";
            txbChat.Text = "";
            this.mainWindow = mainWindow;
            ChatCounter++;
            chatNumber = ChatCounter;
            IsOpened = true;
            Title = "Chat No. " + chatNumber;
        }

        public void RefreshChatWindow(string text, bool partner)
        {
            if (partner)
            {
                if (counter == 0)
                {
                    txbChat.Text += "<"+MainWindow.connectionListIp[chatNumber - 1]+">" + text;
                    counter++;
                }
                else txbChat.Text += "\n<" + MainWindow.connectionListIp[chatNumber - 1] + ">" + text;
            }
            else
            {
                if (counter == 0)
                {
                    txbChat.Text += "<Ich>" + text;
                    counter++;
                }
                else txbChat.Text += "\n<Ich>" + text;
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.Send(txbEingabe.Text);
            RefreshChatWindow(txbEingabe.Text,false);
            txbEingabe.Text = "";
        }

        public void Dispose()
        {
            if (!disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
                disposed = true;
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing) //Absichtliches zerstören des Objektes oder Aufruf des Dekonstruktors?
            {
                ChatCounter--;
                MainWindow.port = MainWindow.port - 1;
            }
        }

        ~Chat()
        {
            Dispose(false);
        }

        private void Chat_OnClosed(object sender, EventArgs e)
        {
            IsOpened = false;
            Dispose();
            mainWindow.Dispatcher.Invoke(() => mainWindow.lblListenPort.Content = MainWindow.port);
            MainWindow.connectionListChat.Remove(this);
            MainWindow.connectionListIp.Remove(((IPEndPoint)MainWindow.connections.Peek().client.Client.RemoteEndPoint).Address.ToString());
            MainWindow.connections.Pop();
        }

        private void TxbEingabe_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) btnSend_Click(this, new RoutedEventArgs());
        }
    }
}
