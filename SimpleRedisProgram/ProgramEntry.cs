using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RedisCore;
using Trinity;
using Trinity.Extension;
namespace SimpleRedisProgram
{
    internal class ProgramEntry
    {
        static int serverCount = 1;
        static void Main(string[] args)
        {
            TrinityConfig.Servers.Clear();
            TrinityConfig.AddServer(new Trinity.Network.ServerInfo("127.0.0.1", 5304, Environment.CurrentDirectory, Trinity.Diagnostics.LogLevel.Info));

            switch (args[0])
            {
                case "-s":
                    RedisServer server = new RedisServer();
                    server.Start(blocking: true);
                    break;
                case "-c":
                    ClientShell();
                    break;
            }
        }

        private static void ClientShell()
        {
            TrinityConfig.CurrentRunningMode = RunningMode.Client;
            serverCount = Global.ServerCount;
            Console.WriteLine("You know you're playing with {0} slave machines", serverCount);
            Console.WriteLine("Begin Playing with the toy Redis:");
            while (true)
            {
                Console.Write(" >");
                string command = Console.ReadLine();
                List<string> commandAndParameter = GetCommandAndParameter(command.Trim());
                ExecuteCommand(commandAndParameter);
            }
        }

        private static void ExecuteCommand(List<string> args)
        {
            ClientCommands clientCommands = new ClientCommands();
            if (args[0] == "set")
            {
                clientCommands.Set(args[1], args[2]);
            }
            else if (args[0] == "get")
            {
                clientCommands.Get(args[1]);
            }
            else if (args[0] == "del")
            {
                args.RemoveAt(0);
                clientCommands.Del(args.ToArray());
            }
            else if (args[0] == "type")
            {
                clientCommands.Type(args[1]);
            }
            else if (args[0] == "exist")
            {
                clientCommands.Exists(args[1]);
            }
            else if (args[0] == "rename")
            {
                clientCommands.ReName(args[1], args[2]);
            }
            else if (args[0] == "renamenx")
            {
                clientCommands.ReNameNx(args[1], args[2]);
            }
            else if (args[0] == "append")
            {
                clientCommands.Append(args[1], args[2]);
            }
            else if (args[0] == "mset")
            {
                args.RemoveAt(0);
                clientCommands.MSet(args.ToArray());
            }
            else if (args[0] == "setrange")
            {
                int offset = 0;
                if (int.TryParse(args[2], out offset))
                    clientCommands.SetRange(args[1], offset, args[3]);
            }
            else if (args[0] == "getrange")
            {
                int start = 0;
                int end = 0;
                if (int.TryParse(args[2], out start) && int.TryParse(args[3], out end))
                    clientCommands.GetRange(args[1], start, end);
            }
            else if (args[0] == "strlen")
            {
                clientCommands.StrLen(args[1]);
            }
            else if (args[0] == "getset")
            {
                clientCommands.GetSet(args[1], args[2]);
            }
            else if (args[0] == "incr")
            {
                clientCommands.Incr(args[1]);
            }
            else if (args[0] == "decr")
            {
                clientCommands.Decr(args[1]);
            }
            else if (args[0] == "incrby")
            {
                double increment = 0;
                if (double.TryParse(args[2], out increment))
                    clientCommands.IncrBy(args[1], increment);
            }
            else if (args[0] == "decrby")
            {
                double decrement = 0;
                if (double.TryParse(args[2], out decrement))
                    clientCommands.DecrBy(args[1], decrement);
            }
            else if (args[0] == "setnx")
            {
                clientCommands.SetNX(args[1], args[2]);
            }
            else if (args[0] == "llen")
            {
                clientCommands.LLen(args[1]);
            }
            else if (args[0] == "lrem")
            {
                int count = 0;
                int value = 0;
                if (int.TryParse(args[2], out count) && int.TryParse(args[3], out value))
                    clientCommands.LRem(args[1], count, value);
            }
            else if (args[0] == "rpush")
            {
                List<int> valueList = new List<int>();
                int value;
                bool isInt = true;
                for (int i = 2; i < args.Count; i++)
                {
                    if (int.TryParse(args[i], out value))
                        valueList.Add(value);
                    else
                    {
                        isInt = false;
                        break;
                    }
                }
                if (isInt)
                    clientCommands.RPush(args[1], valueList.ToArray());
            }
            else if (args[0] == "rpop")
            {
                clientCommands.RPop(args[1]);
            }
            else if (args[0] == "lpop")
            {
                clientCommands.LPop(args[1]);
            }
            else if (args[0] == "lset")
            {
                int index;
                int value;
                if (int.TryParse(args[2], out index) && int.TryParse(args[3], out value))
                    clientCommands.LSet(args[1], index, value);
            }
            else if (args[0] == "rpushx")
            {
                int value;
                if (int.TryParse(args[2], out value))
                    clientCommands.RPushX(args[1], value);
            }
            else if (args[0] == "lpush")
            {
                List<int> valueList = new List<int>();
                int value;
                bool isInt = true;
                for (int i = 2; i < args.Count; i++)
                {
                    if (int.TryParse(args[i], out value))
                        valueList.Add(value);
                    else
                    {
                        isInt = false;
                        break;
                    }
                }
                if (isInt)
                    clientCommands.LPush(args[1], valueList);
            }
            else if (args[0] == "ltrim")
            {
                int start;
                int stop;
                if (int.TryParse(args[2], out start) && int.TryParse(args[3], out stop))
                    clientCommands.LTrim(args[1], start, stop);
            }
            else if (args[0] == "lindex")
            {
                int index;
                if (int.TryParse(args[2], out index))
                    clientCommands.LIndex(args[1], index);
            }
            else if (args[0] == "lpushx")
            {
                int value;
                if (int.TryParse(args[2], out value))
                    clientCommands.LPushX(args[1], value);
            }
            else if (args[0] == "linsert")
            {
                int pivot;
                int value;
                if (args[2] == "before" || args[2] == "after")
                    if (int.TryParse(args[3], out pivot) && int.TryParse(args[4], out value))
                        clientCommands.LInsert(args[1], args[2], pivot, value);
            }
            else if (args[0] == "lrange")
            {
                int start;
                int stop;
                if (int.TryParse(args[2], out start) && int.TryParse(args[3], out stop))
                    clientCommands.LRange(args[1], start, stop);
            }
            else
                Console.WriteLine("don't understand what you're talking about.");
        }

        private static List<string> GetCommandAndParameter(string readLine)
        {
            List<string> commandAndParameter = new List<string>();
            StringBuilder mergeChar = new StringBuilder();
            for (int i = 0; i < readLine.Length; i++)
            {
                if (i == readLine.Length - 1)
                {
                    mergeChar.Append(readLine[i]);
                    commandAndParameter.Add(mergeChar.ToString());
                    mergeChar.Clear();
                }
                else if (readLine[i] == '\"')
                {
                    if (mergeChar.ToString().Contains("\""))
                    {
                        mergeChar.Append(readLine[i]);
                        commandAndParameter.Add(mergeChar.ToString());
                        mergeChar.Clear();
                    }
                    else
                        mergeChar.Append(readLine[i]);
                }
                else if (readLine[i] == ']')
                {
                    mergeChar.Append(readLine[i]);
                    commandAndParameter.Add(mergeChar.ToString());
                    mergeChar.Clear();
                }
                else if (readLine[i] == ' ')
                {
                    if (mergeChar.ToString().Contains("\""))
                        mergeChar.Append(readLine[i]);
                    else
                    {
                        if (mergeChar.Length > 0)
                        {
                            commandAndParameter.Add(mergeChar.ToString());
                            mergeChar.Clear();
                        }
                    }
                }
                else
                    mergeChar.Append(readLine[i]);
            }
            return commandAndParameter;
        }
    }
}
