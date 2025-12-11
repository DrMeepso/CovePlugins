using System.Text.Json;
using System.Text.Json.Serialization;
using Cove.GodotFormat;
using Cove.Server;
using Cove.Server.Chalk;
using Cove.Server.Plugins;

// Change the namespace and class name!
namespace PersistentChalk
{
    public class Vector2Converter : JsonConverter<Vector2>
    {
        public override Vector2 Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            float x = root.GetProperty("X").GetSingle();
            float y = root.GetProperty("Y").GetSingle();

            return new Vector2(x, y);
        }

        public override void Write(
            Utf8JsonWriter writer,
            Vector2 value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteNumber("X", value.x);
            writer.WriteNumber("Y", value.y);
            writer.WriteEndObject();
        }
    }

    public class ChalkCanvasConverter : JsonConverter<ChalkCanvas>
    {
        private CoveServer parentServer;
        private JsonSerializerOptions jsonOptions;

        public ChalkCanvasConverter(CoveServer parentServer)
            : base()
        {
            this.parentServer = parentServer;
        }

        public override ChalkCanvas Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            long canvasID = root.GetProperty("CanvasID").GetInt64();
            ChalkCanvas canvas = new ChalkCanvas(canvasID, this.parentServer);

            if (root.TryGetProperty("ChalkImage", out JsonElement chalkImageElement))
            {
                var chalkImage = JsonSerializer.Deserialize<Dictionary<string, long>>(
                    chalkImageElement.GetRawText(),
                    options
                );

                var deserializedChalkImage = chalkImage.ToDictionary(
                    kvp => JsonSerializer.Deserialize<Vector2>(kvp.Key, options),
                    kvp => kvp.Value
                );

                canvas.chalkUpdate(deserializedChalkImage);
            }

            return canvas;
        }

        public override void Write(
            Utf8JsonWriter writer,
            ChalkCanvas value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();

            writer.WriteNumber("CanvasID", value.canvasID);

            writer.WritePropertyName("ChalkImage");
            writer.WriteStartObject();

            foreach (var kvp in value.chalkImage)
            {
                var key = JsonSerializer.Serialize(kvp.Key, options);
                writer.WritePropertyName(key);
                writer.WriteNumberValue(kvp.Value);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }
    }

    public class PersistentChalk : CovePlugin
    {
        private JsonSerializerOptions jsonOptions;

        public PersistentChalk(CoveServer server)
            : base(server)
        {
            this.jsonOptions = new JsonSerializerOptions
            {
                IncludeFields = true,
                Converters = { new Vector2Converter(), new ChalkCanvasConverter(server) },
            };
        }

        private string currentDir = Directory.GetCurrentDirectory();

        private const string ChalkFile = "chalk.json";

        public override void onInit()
        {
            base.onInit();

            // check if there is a chalk.json file in the current directory
            if (File.Exists(Path.Combine(currentDir, ChalkFile)))
            {
                byte[] chalkData = File.ReadAllBytes(Path.Combine(currentDir, ChalkFile));
                Log("Chalk data file found. Loading chalk data...");
                loadChalk(chalkData);
            }
            else
            {
                // log that the chalk data file does not exist
                Log("Cannot find chalk data file.");
            }

            RegisterCommand(
                "savechalk",
                (player, args) =>
                {
                    if (!IsPlayerAdmin(player))
                    {
                        SendPlayerChatMessage(
                            player,
                            "You do not have permission to use this command!"
                        );
                        return;
                    }

                    saveChalk();
                    SendPlayerChatMessage(player, "The chalk has been saved!");
                }
            );
            SetCommandDescription("savechalk", "Saves the chalk data");
        }

        public long lastUpdate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // now
        public bool hadOfflineUpdate = false;

        public override void onUpdate()
        {
            base.onUpdate();

            if (ParentServer.AllPlayers.Count > 0)
                // At least 1 player is online, reset hadOfflineUpdate
                hadOfflineUpdate = false;

            // Only auto save the chalk data every 5 minutes if a player is online
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastUpdate <= 300)
                return;

            lastUpdate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (ParentServer.AllPlayers.Count == 0 && !hadOfflineUpdate)
                // We're doing a final update, mark as such
                hadOfflineUpdate = true;
            else if (hadOfflineUpdate)
                return;

            saveChalk();
            Log("Saving Chalk Data!");
        }

        public override void onEnd()
        {
            base.onEnd();

            saveChalk();
            Log("Saving Chalk Data!");

            UnregisterCommand("savechalk");
        }

        public void loadChalk(byte[] chalkData)
        {
            // deserialize the chalk data
            var chalk = JsonSerializer.Deserialize<List<ChalkCanvas>>(chalkData, jsonOptions);
            if (chalk != null)
            {
                // set the chalk data to the server's chalk data
                ParentServer.chalkCanvas = chalk;
                Log("Restored Chalk Data");
            }
            else
            {
                Log("Failed to restore chalk data, chalk file is corrupt");
            }
        }

        public void saveChalk()
        {
            // get the canvas data, it must be a clone so we dont mess up the original data or error the thread
            List<ChalkCanvas> chalkData = new List<ChalkCanvas>(ParentServer.chalkCanvas);

            // use the json formatter to serialize the chalk data
            string json = JsonSerializer.Serialize(chalkData, this.jsonOptions);

            // write the json string to a file
            File.WriteAllText(Path.Combine(currentDir, ChalkFile), json);
        }
    }
}
