namespace TypeD.Models.Data
{
    public class CommandHistoryEntry
    {
        public string CommandName { get; }
        public string Context { get; }
        public string Timestamp { get; }

        public CommandHistoryEntry(string commandName, string context)
        {
            CommandName = commandName;
            Context = context;
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
