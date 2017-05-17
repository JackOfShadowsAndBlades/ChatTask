using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.IO;


namespace Client
{
    public partial class ChatForm : Form
    {
        private delegate void ChatEvent(string content, string clr);
        private ChatEvent addMessage;
        private Socket serverSocket;
        private Thread listenThread;
        private string host = "127.0.0.1";
        private int port = 2222;
        public ChatForm()
        {
            InitializeComponent();
            nameData.Hide();
            addMessage = new ChatEvent(AddMessage);
        }

        private void AddMessage(string Content, string Color = "Black")
        {
            if (InvokeRequired)
            {
                Invoke(addMessage, Content, Color);
                return;
            }
            chatBox.SelectionStart = chatBox.TextLength;
            chatBox.SelectionLength = Content.Length;
            chatBox.AppendText(Content + Environment.NewLine);
        }


        private void ChatForm_Load(object sender, EventArgs e)
        {
            IPAddress temp = IPAddress.Parse(host);
            serverSocket = new Socket(temp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Connect(new IPEndPoint(temp, port));
            if (serverSocket.Connected)
            {
                enterChat.Enabled = true;
                nicknameData.Enabled = true;
                AddMessage("Связь с сервером установлена");
                listenThread = new Thread(listener);
                listenThread.IsBackground = true;
                listenThread.Start();

            }
            else
                AddMessage("Связь с сервером не установлена");

        }

        public void Send(byte[] buffer)
        {
            serverSocket.Send(buffer);
        }
        public void Send(string Buffer)
        {
            serverSocket.Send(Encoding.Unicode.GetBytes(Buffer));
        }


        public void handleCommand(string cmd)
        {

            string[] commands = cmd.Split('#');
            int countCommands = commands.Length;
            for (int i = 0; i < countCommands; i++)
            {
                try
                {
                    string currentCommand = commands[i];
                    if (string.IsNullOrEmpty(currentCommand))
                        continue;
                    if (currentCommand.Contains("setnamesuccess"))
                    {
                        Invoke((MethodInvoker)delegate
                        {
                            AddMessage($"Добро пожаловать, {nicknameData.Text}");
                            nameData.Text = nicknameData.Text;
                            nameData.Show();
                            chatBox.Enabled = true;
                            messageData.Enabled = true;
                            userList.Enabled = true;
                            nicknameData.Enabled = false;
                            enterChat.Enabled = false;
                        });
                        continue;
                    }
                    if (currentCommand.Contains("setnamefailed"))
                    {
                        AddMessage("Неверное имя пользователя.");
                        continue;
                    }
                    if (currentCommand.Contains("msg"))
                    {
                        string[] Arguments = currentCommand.Split('|');
                        AddMessage(Arguments[1], Arguments[2]);
                        continue;
                    }

                    if (currentCommand.Contains("userlist"))
                    {
                        string[] Users = currentCommand.Split('|')[1].Split(',');
                        int countUsers = Users.Length;
                        userList.Invoke((MethodInvoker)delegate { userList.Items.Clear(); });
                        for (int j = 0; j < countUsers; j++)
                        {
                            userList.Invoke((MethodInvoker)delegate { userList.Items.Add(Users[j]); });
                        }
                        continue;
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка с handleCommand: " + ex.Message);
                }

            }
        }
        public void listener()
        {
            try
            {
                while (serverSocket.Connected)
                {
                    byte[] buffer = new byte[2048];
                    int bytesReceive = serverSocket.Receive(buffer);
                    handleCommand(Encoding.Unicode.GetString(buffer, 0, bytesReceive));
                }
            }
            catch
            {
                MessageBox.Show("Связь с сервером прервана");
                Application.Exit();
            }
        }

        private void enterChat_Click(object sender, EventArgs e)
        {
            string nickName = nicknameData.Text;
            if (string.IsNullOrEmpty(nickName))
                return;
            Send($"#setname|{nickName}");
        }

        private void ChatForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (serverSocket.Connected)
                Send("#endsession");
        }

        private void messageData_KeyUp(object sender, KeyEventArgs key)
        {
            if (key.KeyData == Keys.Enter)
            {
                string msgData = messageData.Text;
                if (string.IsNullOrEmpty(msgData))
                    return;
                Send($"#message|{msgData}");
                messageData.Text = string.Empty;
            }
        }

    }
}
