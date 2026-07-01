using System;
using System.IO;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Datagram_sockets
{
    public partial class Form1 : Form
    {
        [Serializable]
        public class Message
        {
            public string mes; // текст повідомлення
            public string user; // ім'я користувача
            public Message()
            {

            }
        }
        public System.Threading.SynchronizationContext uiContext;

        public Form1()
        {
            InitializeComponent();
            // Отримаємо контекст синхронізації для поточного потоку
            uiContext = SynchronizationContext.Current;
            WaitClientQuery();
        }

        // прийом повідомлення
        private async void WaitClientQuery()
        {
            await Task.Run(() =>
            {
                try
                {
                    // встановимо для сокета адресу локальної кінцевої точки
                    IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any /* Надає IP-адресу, яка вказує, що сервер повинен контролювати дії клієнтів на всіх мережевих інтерфейсах.*/,
                        49152 /* порт */);

                    // створюємо дейтаграмний сокет
                    Socket socket = new Socket(AddressFamily.InterNetwork /*схема адресації*/, SocketType.Dgram /*тип сокета*/, ProtocolType.Udp /*протокол*/ );
                    /* Значення InterNetwork вказує на те, що при підключенні об'єкта Socket до кінцевої точки передбачається використання IPv4-адреси.
                       Підтримує дейтаграми — ненадійні повідомлення з фіксованою (зазвичай малою) максимальною довжиною, що передаються без встановлення підключення. 
                     * Можливі втрата та дублювання повідомлень, а також їх отримання не в тому порядку, в якому вони були відправлені. 
                     * Об'єкт Socket типу Dgram не вимагає встановлення підключення перед прийомом та передачею даних і може забезпечувати зв'язок із багатьма одноранговими вузлами.
                     * Dgram використовує протокол Datagram (Udp) та InterNetwork.
                     */

                    socket.Bind(ipEndPoint); // Зв'яжемо об'єкт Socket із локальною кінцевою точкою.
                    while (true)
                    {
                        EndPoint remote = new IPEndPoint(0x7F000000, 100); // інформація про віддалений хост, який відправив дейтаграму
                        byte[] arr = new byte[1024];
                        int len = socket.ReceiveFrom(arr, ref remote); // отримаємо UDP-дейтаграму
                        string clientIP = ((IPEndPoint)remote).Address.ToString(); // отримаємо IP-адресу віддаленого вузла
                        byte[] copy = new byte[len];
                        Array.Copy(arr, 0, copy, 0, len);
                        // Створимо потік, резервним сховищем якого є пам'ять.
                        MemoryStream stream = new MemoryStream(copy);
                        // XmlSerializer серіалізує та десеріалізує об'єкт у XML-форматі
                        XmlSerializer serializer = new XmlSerializer(typeof(Message));
                        Message m = serializer.Deserialize(stream) as Message; // виконуємо десеріалізацію
                        // отриману від віддаленого вузла інформацію додаємо до списку
                        uiContext.Send(d => listBox1.Items.Add(clientIP), null);
                        uiContext.Send(d => listBox1.Items.Add(m.user), null);
                        uiContext.Send(d => listBox1.Items.Add(m.mes), null);
                        stream.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Отримувач: " + ex.Message);
                }
            });
        }

        // відправлення повідомлення
        private async void button1_Click(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                try
                {
                    IPEndPoint ipEndPoint = new IPEndPoint(
                        IPAddress.Parse(ip_address.Text) /* IP-адреса віддаленого DNS-вузла, до якого планується підключення. */,
                        49152 /* порт */);

                    // створюємо дейтаграмний сокет
                    Socket socket = new Socket(AddressFamily.InterNetwork /*схема адресації*/, SocketType.Dgram /*тип сокета*/, ProtocolType.Udp /*протокол*/ );
                    /* Значення InterNetwork вказує на те, що при підключенні об'єкта Socket до кінцевої точки передбачається використання IPv4-адреси.
                       Підтримує дейтаграми — ненадійні повідомлення з фіксованою (зазвичай малою) максимальною довжиною, що передаються без встановлення підключення. 
                     * Можливі втрата та дублювання повідомлень, а також їх отримання не в тому порядку, в якому вони були відправлені. 
                     * Об'єкт Socket типу Dgram не вимагає встановлення підключення перед прийомом та передачею даних і може забезпечувати зв'язок із багатьма одноранговими вузлами.
                     * Dgram використовує протокол Datagram (Udp) та InterNetwork.
                     */
                    // Створимо потік, резервним сховищем якого є пам'ять.
                    MemoryStream stream = new MemoryStream();
                    // XmlSerializer серіалізує та десеріалізує об'єкт у XML-форматі
                    XmlSerializer serializer = new XmlSerializer(typeof(Message));
                    Message m = new Message();
                    m.mes = textBox2.Text; // текст повідомлення
                    m.user = Environment.UserDomainName + @"\" + Environment.UserName; // ім'я користувача
                    serializer.Serialize(stream, m); // виконуємо серіалізацію
                    byte[] arr = stream.ToArray(); // записуємо вміст потоку в байтовий масив
                    stream.Close();
                    socket.SendTo(arr, ipEndPoint); // передаємо UDP-дейтаграму на віддалений вузол
                    socket.Shutdown(SocketShutdown.Send); // Відключаємо об'єкт Socket від передачі.
                    socket.Close(); // закриваємо UDP-підключення та звільняємо всі ресурси
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Відправник: " + ex.Message);
                }
            });
        }
    }
}