namespace AskDB.App.Helpers
{
    public static class Cache
    {
        public static string ApiKey { get; set; }

        public static bool HasUserEverConnectedToDatabase { get; set; } = false;
    }
}
