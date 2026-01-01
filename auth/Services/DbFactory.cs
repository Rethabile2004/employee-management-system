using Google.Cloud.Firestore;

namespace Auth.Services
{
    public static class DbFactory
    {
        private static FirestoreDb _db;
        private static readonly object _lock = new();

        public static FirestoreDb GetFirestoreDb()
        {
            if (_db != null) return _db;

            lock (_lock)
            {
                if (_db != null) return _db;

                string path = Path.Combine(Directory.GetCurrentDirectory(), "AppData", "serviceAccountKey.json");
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);

                _db = FirestoreDb.Create("streamline-4994a");
                return _db;
            }
        }
    }
}