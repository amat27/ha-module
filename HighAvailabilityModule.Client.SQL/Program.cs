namespace HighAvailabilityModule.Client.SQL
{
    using System;
    using System.Collections;
    using System.Threading.Tasks;

    using HighAvailabilityModule.Algorithm;
    class Program
    {
        static async Task Main(string[] args)
        {
            string utype;
            string uname;

            ArrayList AllType = new ArrayList();

            if (args.Length != 0)
            {
                utype = args[1];
                if (utype == "query")
                {
                    uname = "-1";
                    for (int i = 2; i < args.Length; i++)
                    {
                        AllType.Add(args[i]);
                    }
                }
                else
                {
                    uname = args[2];
                }
            }
            else
            {
                Console.WriteLine("Please give the client's type and machine name!");
                return;
            }

            var interval = TimeSpan.FromSeconds(0.2);
            var timeout = TimeSpan.FromSeconds(5);

            SQLMembershipClient client = new SQLMembershipClient(utype, uname);
            MembershipWithWitness algo = new MembershipWithWitness(client, interval, timeout);

            Console.WriteLine("Uuid:{0}", client.Uuid);
            Console.WriteLine("Type:{0}", client.Utype);
            Console.WriteLine("Machine Name:{0}", client.Uname);

            if (client.Utype == "query")
            {
                while (true)
                {
                    foreach (string qtype in AllType)
                    {
                        var primary = await client.GetHeartBeatEntryAsync(qtype);
                        if (!primary.IsEmpty)
                        {
                            Console.WriteLine($"[Query Result] Type:{primary.Utype}. Machine Name:{primary.Uname}. Running as primary. [{primary.TimeStamp}]");
                            await Task.Delay(TimeSpan.FromSeconds(2));
                        }
                    }
                }
            }
            else
            {
                await algo.RunAsync(
                () => Task.Run(
                    async () =>
                    {
                        while (true)
                        {
                            Console.WriteLine($"Type:{client.Utype}. Machine Name:{client.Uname}. Running as primary. [{DateTime.UtcNow}]");
                            await Task.Delay(TimeSpan.FromSeconds(2));
                        }
                    }),
                null);
            }
        }
    }
}