using System.Net;

namespace HttpProxyServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== HTTP Proxy Server ===");

            string host = ReadInput("Введите адрес для прослушивания (по умолчанию 127.0.0.1): ", "127.0.0.1");
            string portStr = ReadInput("Введите порт (по умолчанию 8080): ", "8080");

            if (!IPAddress.TryParse(host, out IPAddress listenAddress))
            {
                Console.WriteLine("Некорректный адрес, используется 127.0.0.1");
                listenAddress = IPAddress.Parse("127.0.0.1");
            }

            if (!int.TryParse(portStr, out int listenPort) || listenPort < 1 || listenPort > 65535)
            {
                Console.WriteLine("Некорректный порт, используется 8080");
                listenPort = 8080;
            }

            Console.WriteLine($"Запуск прокси на {listenAddress}:{listenPort}");

            var server = new ProxyServer(listenAddress, listenPort);
            server.Run();
        }

        private static string ReadInput(string prompt, string defaultValue)
        {
            Console.Write(prompt);
            string input = Console.ReadLine()?.Trim() ?? "";
            return string.IsNullOrEmpty(input) ? defaultValue : input;
        }
    }
}
