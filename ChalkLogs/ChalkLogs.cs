using Cove.Server.Plugins;
using Cove.Server;
using Cove.Server.Actor;


// Change the namespace and class name!
namespace ChalkLogs
{
    public class ChalkLogs : CovePlugin
    {
        public ChalkLogs(CoveServer server) : base(server) { }

        public override void onInit()
        {
            base.onInit();

            Log("We out here logging our chalk!");

            // check if the chalkLogs.txt file exists
            if (!File.Exists("chalkLogs.txt"))
            {
                File.Create("chalkLogs.txt");
            }

        }

        public override void onNetworkPacket(WFPlayer sender, Dictionary<string, object> packet)
        {
            base.onNetworkPacket(sender, packet);

            object type;
            if (packet.TryGetValue("type", out type))
            { 
            
                // there was a error of some sort and just ignore the packet
                if (typeof(string) != type.GetType())
                    return;

                if (type.ToString() == "chalk_packet")
                {
                    File.AppendAllText("chalkLogs.txt", $"[{DateTime.Now}] {sender.Username} sent a chalk packet!\n");
                }

            }

        }

    }
}