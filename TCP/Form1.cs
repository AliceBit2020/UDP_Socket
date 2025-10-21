using System;
using System.IO;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;


namespace TCP
{
    public partial class Form1 : Form
    {
        [Serializable]
        class Message
        {
            public string mes; // текст сообщения
            public string user; // имя пользователя
        }
        public SynchronizationContext uiContext;

        public Form1()
        {
            InitializeComponent();
            // Получим контекст синхронизации для текущего потока 
            uiContext = SynchronizationContext.Current;
            WaitClientQueryAsync();
        }

        // прием сообщения
        private async void WaitClientQueryAsync()////  server
        {
            await Task.Run(() =>
            {
            try
            {
                // установим для сокета адрес локальной конечной точки
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 49152);

                // создаем дейтаграммный сокет
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);


                socket.Bind(ipEndPoint); // Свяжем объект Socket с локальной конечной точкой.

                    while (true)
                    {


                        EndPoint remote = new IPEndPoint(0, 0);// информация об удаленном хосте, который отправил датаграмму
                        byte[] arr = new Byte[1024];
                        socket.ReceiveFrom(arr, ref remote); // получим UDP-датаграмму  блокуючий

                        Task.Run(() =>
                        {
                            string clientIP = ((IPEndPoint)remote).Address.ToString(); // получим IP-адрес удаленного 

                            // Создадим поток, резервным хранилищем которого является память.
                            MemoryStream stream = new MemoryStream(arr);
                            // BinaryFormatter сериализует и десериализует объект в двоичном формате 
                            BinaryFormatter formatter = new BinaryFormatter();
                            Message m = (Message)formatter.Deserialize(stream); // выполняем десериализацию
                                                                                // полученную от удаленного узла информацию добавляем в список
                            uiContext.Send(d => listBox1.Items.Add(clientIP), null);
                            uiContext.Send(d => listBox1.Items.Add(m.user), null);
                            uiContext.Send(d => listBox1.Items.Add(m.mes), null);

                           // socket.SendTo(arr, remote);

                            stream.Close();
                        });

                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Получатель: " + ex.Message);
                }
            });         
        }

        // отправление сообщения
        private async Task SendAsync()
        {
           int res= await Task<int>.Run(() =>
            {
                try
                {
                    IPEndPoint ipEndPoint = new IPEndPoint(
                        IPAddress.Parse(ip_address.Text), 49152);

                    // создаем дейтаграммный сокет
                    Socket socket = new Socket(AddressFamily.InterNetwork /*схема адресации*/, SocketType.Dgram /*тип сокета*/, ProtocolType.Udp /*протокол*/ );

                    // Создадим поток, резервным хранилищем которого является память.
                    MemoryStream stream = new MemoryStream();
                    // BinaryFormatter сериализует и десериализует объект в двоичном формате 
                    BinaryFormatter formatter = new BinaryFormatter();
                    Message m = new Message();
                    m.mes = textBox2.Text; // текст сообщения
                    m.user = Environment.UserDomainName + @"\" + Environment.UserName; // имя пользователя
                    formatter.Serialize(stream, m); // выполняем сериализацию
                    byte[] arr = stream.ToArray(); // записываем содержимое потока в байтовый массив
                    stream.Close();
                    socket.SendTo(arr, ipEndPoint); // передаем UDP-датаграмму на удаленный узел
                    socket.Shutdown(SocketShutdown.Send); // Отключаем объект Socket от передачи.
                    socket.Close(); // закрываем UDP-подключение и освобождаем все ресурсы
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Отправитель: " + ex.Message);
                }

                return 5;

            });


            uiContext.Send(d => listBox1.Items.Add(res), null);

        }

            private  void button1_Click(object sender, EventArgs e)
            {
                //await SendAsync();////  блокировка 
                //
                SendAsync();//// кнопка не блокируется
            }

    }
}
