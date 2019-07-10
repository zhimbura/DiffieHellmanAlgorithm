using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;
namespace DiffieHellman
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        //лист клиентов
        public List<DiffieClient> ClientsList = new List<DiffieClient>();
        // полученный ключ шифрования
        public int TrueKey = 0;
        //Поток прослушивание порта
        Thread ListenAnswer;
        //делегат остановки сервера прослушивания
        Action StopListen;
        // то-же остановка сервера (более корректная)
        bool ListenAnswerBreak = false;
        //секретный ключ клиента
        public int SecretKey = 0;
        // нужна для вычисления ключа
        public int p = 17;
        // нужна для вычисления ключа
        public int g = 3;
        // полученный публичный ключ
        public int PublicKey = 0;
        public MainWindow()
        {
            InitializeComponent();
            // событие проверки связи
            btn_CreateConnect.Click += Btn_CreateConnect_Click;
            // событие запуска сервера
            btn_StartListen.Click += (sender, e) => {
                Debug("Прослушивание запущено");
                //иницилизация запуска сервера
                ListenAnswer = new Thread(new ThreadStart(() => {
                    string ThisIp = "";
                    //получение ip клиента с формы
                    this.Dispatcher.Invoke(() => {
                        ThisIp = text_ThisIP.Text;
                    });
                    //получние ip и порта из строки
                    DiffieClient diffieClient = new DiffieClient(ThisIp);
                    // инициальзация клиента 
                    UdpClient client = new UdpClient(diffieClient.PORT);
                    // инициальзауия параметров прослушки
                    IPEndPoint ip = new IPEndPoint(IPAddress.Any, diffieClient.PORT);
                    // бесконечный цикл прослушки
                    while (true)
                    {
                        // завершение цикла
                        if (ListenAnswerBreak) break;
                        try
                        {
                            //инициализация остановки прослушки и освобождение ресурсов
                            StopListen = () =>
                            {
                                client.Close();
                                client.Dispose();
                            };
                            //получение данных из сети
                            byte[] data = client.Receive(ref ip);
                            //конвертирование байтов в строку
                            string message = Encoding.UTF8.GetString(data);
                            //отправка команды в диспетчер команд
                            CommandDispetcher(message, ip);
                        }
                        // если ошибка
                        catch (Exception ex)
                        {
                            // пишем ошибку
                            this.Dispatcher.Invoke(() => {
                                Debug("Ошибка " + ex.Message);
                            });
                        }

                    }
                    //завершение работы клиента
                    client.Close();
                }));
                //запуск сервера прослушки
                ListenAnswer.Start();
            };
            btn_CreateKey.Click += (sender, e) => {
                //начало генерации ключа 
                //перебор всех участников и отправка всем команды генерации приватного ключа
                foreach (var item in ClientsList)
                { 
                    SendMessage("createKey:", item.IP, item.PORT);
                }
                //ожидаем чтобы все успели сгенерировать ключ
                Thread.Sleep(500);
                //начало генерации ключа команда:публичный ключ:какому клиенту адресуется:сколько итераций осталось
                SendMessage("getPublicKey:" + PublicKey + ":0" + ":" + (ClientsList.Count) , ClientsList[0].IP , ClientsList[0].PORT);
            };
        }
        //диспетчер команд
        public void CommandDispetcher(string command, IPEndPoint sender)
        {
            //получение порта клиента
            string[] port = command.Split('_');
            //получение данных и команды
            string[] data = port[0].Split(':');
            switch (data[0])
            {
                //отправка запроса на проверку связи
                case "testConnect":
                    //отправка всем что соеденение есть
                    this.Dispatcher.Invoke(() => {
                        SendMessage("testConnectResult:", sender.Address.ToString(), int.Parse(port[1]));
                        //Debug(string.Format("Получено " + command + " от ", sender.Address.ToString(), sender.Port));
                    });
                    break;
                //команда завершения проверки соединения
                case "testConnectResult":
                    this.Dispatcher.Invoke(() => {
                        Debug(string.Format("Связь с {0}:{1} установлена", sender.Address.ToString(), port[1]));
                    });
                    break;
                //команда генерации ключа
                case "createKey":
                    //получаем порт клиента
                    int thisPort = 0;
                    this.Dispatcher.Invoke(() => { thisPort = new DiffieClient(text_ThisIP.Text).PORT; });
                    //так как рандом зависит от времени компьютера, а все генерируют ключ в раз, то мы прибавляем к времени компьютера порт клиента 
                    Random random = new Random(DateTime.UtcNow.Millisecond + thisPort);
                    //геренрируем случайное число
                    SecretKey = random.Next(2, 16);
                    this.Dispatcher.Invoke(() => {
                        Debug(string.Format("Сгенерирован секретный ключ " + SecretKey, sender.Address.ToString(), port[1]));
                    });
                    break;
                 //получение публичного ключа и генерация настроящего ключа
                case "getPublicKey":
                    //получение публичного ключа предыдущего клиента
                    int publicKeyPrev = int.Parse(data[1]);
                    //получение ID предыдущего клиента
                    int senderId = int.Parse(data[2]);
                    //получение оставшегося количества итераций
                    int count = int.Parse(data[3]) -1;
                    //вывод полученных данных 
                    this.Dispatcher.Invoke(() => {
                        Debug("publicKeyPrev "+ publicKeyPrev + " senderId "+ senderId);
                    });
                    //генерация публичного ключа если это первая итерация иначе прололжаем генерацию
                    if (publicKeyPrev == 0) PublicKey = Convert.ToInt32(Math.Pow(g, SecretKey) % p);
                    else PublicKey = Convert.ToInt32(Math.Pow(publicKeyPrev, SecretKey) % p);
                    //если количество итераций закончилось
                    if (count == 0)
                    {
                        //опять задаем количество итераций
                        count = ClientsList.Count-1;
                        //если настоящий ключ уже был сгенерирован 
                        //то значит все сгенерировали его и начался второй круг
                        if (TrueKey != 0)
                        {
                            //отправляем чтобы все показали настоящий ключ
                            foreach (var item in ClientsList)
                            {
                                SendMessage("ShowKey:", item.IP, item.PORT);
                            }
                            //и завершаем генерацию ключа
                            break;
                        }
                        else
                        {
                            //иначе генерируем настоящий ключ
                            TrueKey = PublicKey;
                            //выводим его на экран
                            this.Dispatcher.Invoke(() => {
                                Debug("TrueKey " + TrueKey);
                            });
                            // и заново генерируем публичный ключ
                            PublicKey = Convert.ToInt32(Math.Pow(g, SecretKey) % p);
                        }
                    }
                    // если id отправляемого дошел до конца, то начинаем сначала
                    if (senderId == ClientsList.Count-1) senderId = -1;
                    //потправляем публичный ключ следующему клиенту
                    SendMessage("getPublicKey:" + PublicKey + ":"+(senderId+1)+":" + count, ClientsList[senderId + 1].IP, ClientsList[senderId + 1].PORT);
                    break;
                //показываем настоящий ключ
                case "ShowKey":
                    this.Dispatcher.Invoke(() => {
                        Debug("Общий ключ " + TrueKey);
                    });
                    break;
                default:
                    break;
            }

        }

        //проверка связи
        private void Btn_CreateConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //задаем массив клиентов
                ClientsList = new List<DiffieClient>();
                //записываем первого клиента самого себя
                ClientsList.Add(new DiffieClient(text_ThisIP.Text));
                //получаем всех остальных клиентов и разделяем их 
                var IPlist = text_IPlist.Text.Split('\n');
                //перебераем всех клиентов
                foreach (var item in IPlist)
                {
                    //создаем нового клиента
                    DiffieClient client = new DiffieClient(item);
                    //добавляем клиента в массив
                    ClientsList.Add(client);
                    //отправляем запрос на проверку связи
                    SendMessage("testConnect:", client.IP, client.PORT);
                }
                //сортируем клиентов по порту
                ClientsList.Sort();
            }
            catch (Exception ex)
            {
                //если происходит ошибка выводим ее
                Debug(ex.Message);
            }

        }
        //отправка сообщения
        public int SendMessage(string Message ,string IP, int PORT)
        {
            //количество отправленных байт
            int cendCount = 0;
            try
            {
                //создаем клиента 
                DiffieClient diffieClient = new DiffieClient();
                //получаем порт текщего клиента
                this.Dispatcher.Invoke(() => {
                    diffieClient = new DiffieClient(text_ThisIP.Text);
                });
                //создаем клиент отправки
                UdpClient client = new UdpClient(IP, PORT);
                //генерируем сообщение и с дописаным портом отправлителя 
                byte[] data = Encoding.UTF8.GetBytes(Message + "_" + diffieClient.PORT);
                //отправляем сообщение
                cendCount = client.Send(data, data.Length);
                //закрываем клиент отправки
                client.Close();
            }
            catch (Exception ex)
            {
                //воводим ошибку
                Debug(ex.Message);
            }
            //возвращаем количество отправленных байт
            return cendCount;
        }
        //вывод сообщения в форму 
        public void Debug(string msg)
        {
            // пишем время, сообщение и переход на новую строку
            text_Debug.Text += DateTime.UtcNow + " : " + msg + "\n";
            //скролим вниз
            text_Debug.ScrollToEnd();
        }
        //закрытие формы
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Exit();
        }
        //закрытие формы
        private void Window_Closed(object sender, EventArgs e)
        {
            Exit();
        }
        //Убиваем поток прослушки сообщения 
        public void Exit()
        {
            if (StopListen != null) StopListen();
            ListenAnswerBreak = true;
            if (ListenAnswer != null)
                if (ListenAnswer.IsAlive) ListenAnswer.Abort();
            
        }
    }
}
