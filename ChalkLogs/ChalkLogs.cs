using Cove.Server;
using Cove.Server.Actor;
using Cove.Server.Plugins;

// Change the namespace and class name!
namespace ChalkLogs
{
    public class ChalkLogs : CovePlugin
    {
        public ChalkLogs(CoveServer server)
            : base(server) { }

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

        public override void onChalkUpdate(
            WFPlayer? sender,
            long canvasID,
            Dictionary<int, object> packet
        )
        {
            base.onChalkUpdate(sender, canvasID, packet);
            File.AppendAllText(
                "chalkLogs.txt",
                $"[{DateTime.Now}] {sender.Username} [{sender.SteamId}] Used chalk on canvas {canvasID}\n"
            );
        }
    }
}
