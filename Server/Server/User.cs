using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    public class User
    {
        private Thread userThread;
        public string Username;
        private bool authSuccess = false;
        private Socket userHandle;
        
        public User(Socket handle)
        {
            userHandle = handle;
            userThread = new Thread(listener);
            userThread.IsBackground = true;
            userThread.Start();
        }
        private void listener()
        {
            try
            {
                while (userHandle.Connected)
                {
                    byte[] buffer = new byte[2048];
                    int bytesReceive = userHandle.Receive(buffer);
                    handleCommand(Encoding.Unicode.GetString(buffer, 0, bytesReceive));
                }
            }
            catch
            {
                Server.EndUser(this);
            }
        }

        private bool setName(string Name)
        {
            Username = Name;
            Server.NewUser(this);
            authSuccess = true;
            return true;
        }

        private void handleCommand(string cmd)
        {
            try
            {
                string[] commands = cmd.Split('#');
                int countCommands = commands.Length;
                for (int i = 0; i < countCommands; i++)
                {
                    string currentCommand = commands[i];
                    if (string.IsNullOrEmpty(currentCommand))
                        continue;
                    if (!authSuccess)
                    {
                        if (currentCommand.Contains("setname"))
                        {
                            if (setName(currentCommand.Split('|')[1]))
                                Send("#setnamesuccess");
                            else
                                Send("#setnamefailed");
                        }
                        continue;
                    }
                    if (currentCommand.Contains("message"))
                    {
                        string[] Arguments = currentCommand.Split('|');
                        Server.SendGlobalMessage($"[{Username}]: {Arguments[1]}", "Black");

                        continue;
                    }
                    if (currentCommand.Contains("endsession"))
                    {
                        Server.EndUser(this);
                        return;
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка с handleCommand: " + ex.Message);
            }
        }

        public void SendMessage(string content,string color)
        {
            Send($"#msg|{content}|{color}");
        }

        public void Send(byte[] buffer)
        {
            userHandle.Send(buffer);
        }

        public void Send(string Buffer)
        {
            userHandle.Send(Encoding.Unicode.GetBytes(Buffer));
        }

        public void End()
        {
            userHandle.Close();
        }
    }
}
