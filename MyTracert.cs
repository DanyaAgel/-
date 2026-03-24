using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MyTraceroute
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Использование: MyTraceroute.exe <IP-адрес>");
                return;
            }

            string targetAddress = args[0];
            IPAddress destinationIp = IPAddress.Parse(targetAddress);

            Console.WriteLine($"Трассировка маршрута к {destinationIp}");
            Console.WriteLine("с максимальным числом прыжков 30:\n");

            const int maxHopLimit = 30;
            const int attemptsPerHop = 3;
            const int responseTimeoutMs = 3000;

            ushort packetSeq = 1;

            for (int hopNumber = 1; hopNumber <= maxHopLimit; hopNumber++)
            {
                Console.Write($"{hopNumber,2}  ");

                IPAddress currentRouterIp = null;
                long[] hopResponseTimes = new long[attemptsPerHop];
                bool isDestinationReached = false;

                for (int attempt = 0; attempt < attemptsPerHop; attempt++)
                {
                    try
                    {
                        using (Socket rawSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp))
                        {
                            rawSocket.ReceiveTimeout = responseTimeoutMs;
                            rawSocket.Ttl = (short)hopNumber;

                            byte[] icmpPacket = BuildIcmpEchoRequest(packetSeq++);

                            IPEndPoint targetEndpoint = new IPEndPoint(destinationIp, 0);
                            EndPoint senderEndpoint = new IPEndPoint(IPAddress.Any, 0);

                            DateTime startTime = DateTime.Now;

                            rawSocket.SendTo(icmpPacket, targetEndpoint);

                            byte[] receiveBuffer = new byte[1024];
                            int bytesReceived = rawSocket.ReceiveFrom(receiveBuffer, ref senderEndpoint);

                            TimeSpan elapsedTime = DateTime.Now - startTime;
                            IPAddress respondingIp = ((IPEndPoint)senderEndpoint).Address;

                            if (currentRouterIp == null)
                                currentRouterIp = respondingIp;

                            if (elapsedTime.TotalMilliseconds < responseTimeoutMs)
                                hopResponseTimes[attempt] = (long)elapsedTime.TotalMilliseconds;
                            else
                                hopResponseTimes[attempt] = 0;

                            if (bytesReceived >= 28)
                            {
                                int icmpMessageType = receiveBuffer[20];

                                if (icmpMessageType == 0)
                                {
                                    isDestinationReached = true;
                                }
                                else if (icmpMessageType == 11)
                                {
                                    isDestinationReached = false;
                                }
                            }
                        }

                        Thread.Sleep(100);
                    }
                    catch (SocketException)
                    {
                        hopResponseTimes[attempt] = 0;
                    }
                }

                for (int i = 0; i < attemptsPerHop; i++)
                {
                    if (hopResponseTimes[i] > 0)
                        Console.Write($"{hopResponseTimes[i],4} ms  ");
                    else
                        Console.Write("   *   ");
                }

                if (currentRouterIp != null)
                {
                    Console.Write($"{currentRouterIp,-16}");
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("   *   ");
                }

                if (isDestinationReached || (currentRouterIp != null && currentRouterIp.Equals(destinationIp)))
                {
                    Console.WriteLine("Трассировка завершена.");
                    return;
                }
            }
        }

        static byte[] BuildIcmpEchoRequest(ushort sequenceNumber)
        {
            byte[] packetData = new byte[40];

            packetData[0] = 8;
            packetData[1] = 0;
            packetData[2] = 0;
            packetData[3] = 0;

            ushort processId = (ushort)(System.Diagnostics.Process.GetCurrentProcess().Id % 65535);

            packetData[4] = (byte)(processId >> 8);
            packetData[5] = (byte)(processId & 0xFF);

            packetData[6] = (byte)(sequenceNumber >> 8);
            packetData[7] = (byte)(sequenceNumber & 0xFF);

            for (int i = 8; i < packetData.Length; i++)
            {
                packetData[i] = (byte)i;
            }

            ushort checksumValue = ComputeChecksum(packetData);

            packetData[2] = (byte)(checksumValue >> 8);
            packetData[3] = (byte)(checksumValue & 0xFF);

            return packetData;
        }

        static ushort ComputeChecksum(byte[] dataBuffer)
        {
            long accumulator = 0;

            for (int i = 0; i < dataBuffer.Length; i += 2)
            {
                if (i + 1 < dataBuffer.Length)
                    accumulator += (dataBuffer[i] << 8) + dataBuffer[i + 1];
                else
                    accumulator += (dataBuffer[i] << 8);
            }

            while ((accumulator >> 16) != 0)
                accumulator = (accumulator & 0xFFFF) + (accumulator >> 16);

            return (ushort)~accumulator;
        }
    }
}