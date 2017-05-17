using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    public class Server
    {
        public static int CountUsers = 0;
        public delegate void UserEvent(string name);
        public static List<User> UserList = new List<User>();
        public static Socket ServerSocket;
        public const string Host = "127.0.0.1";
        public const int Port = 2222;
        public static bool Work = true;

        public Server()
        {
            IPAddress address = IPAddress.Parse(Host);
            ServerSocket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ServerSocket.Bind(new IPEndPoint(address, Port));
            ServerSocket.Listen(100);
            Console.WriteLine($"Сервер запущен на {Host}:{Port}");
            Console.WriteLine("Ожидание подключений...");
            while (Work)
            {
                Socket handle = ServerSocket.Accept();
                Console.WriteLine($"Новое подключение: {handle.RemoteEndPoint.ToString()}");
                new User(handle);
            }
            Console.WriteLine("Закрытие сервера...");
        }

        public static event UserEvent UserConnected = (Username) =>
        {
            Console.WriteLine($"Пользователь {Username} подключился");
            CountUsers++;
            SendGlobalMessage($"Пользователь {Username} подключился к чату.", "Black");
            SendUserList();
        };

        public static event UserEvent UserDisconnected = (Username) =>
        {
            Console.WriteLine($"Пользователь {Username} отключился");
            CountUsers--;
            SendGlobalMessage($"Пользователь {Username} отключился от чата.", "Black");
            SendUserList();
        };

        public static void NewUser(User user)
        {
            if (!UserList.Contains(user))
            {
                UserList.Add(user);
                UserConnected(user.Username);
            }
        }

        public static void EndUser(User user)
        {
            if (!UserList.Contains(user))
                return;
            UserList.Remove(user);
            user.End();
            UserDisconnected(user.Username);

        }

        public static void SendUserList()
        {
            string userList = "#userlist|";

            for (int i = 0; i < CountUsers; i++)
            {
                userList += UserList[i].Username + ",";
            }

            SendAllUsers(userList);
        }
        public static void SendAllUsers(byte[] data)
        {
            for (int i = 0; i < CountUsers; i++)
            {
                UserList[i].Send(data);
            }
        }
        public static void SendAllUsers(string data)
        {
            for (int i = 0; i < CountUsers; i++)
            {
                UserList[i].Send(data);
            }
        }

        public static void SendGlobalMessage(string content, string color)
        {
            for (int i = 0; i < CountUsers; i++)
            {
                UserList[i].SendMessage(content, color);
            }
        }
    }
}
