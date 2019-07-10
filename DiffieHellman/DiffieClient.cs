using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
namespace DiffieHellman
{
    //класс клиента с реализованным интерфейсом для сортировки по порту
    public class DiffieClient : IComparable
    {

        public string IP { get; set; }
        public int PORT { get; set; }
        
        public int PublicKey { get; set; }

        public DiffieClient()
        {
            PublicKey = 0;
        }
        //конструктор с инициализацией ip и порта
        public DiffieClient(string IpPort)
        {
            //разделяем строку
            string[] data = IpPort.Split(':');
            //получаем ip
            IP = data[0];
            //порт
            PORT = int.Parse(data[1]);
            PublicKey = 0;
        }
        //Реализация интерфейса
        public int CompareTo(object obj)
        {
            //пробуем получить клиента 
            DiffieClient client = obj as DiffieClient;
            if (client != null)
                //возвращаем результат сравления 
                return this.PORT.CompareTo(client.PORT);
            else
                //выбразываем ошибку
                throw new Exception("Невозможно сравнить два объекта");
        }
    }
}
