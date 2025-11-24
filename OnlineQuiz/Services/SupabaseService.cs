using Supabase;

namespace OnlineQuiz.Services
{
    public class SupabaseService
    {
        private readonly Supabase.Client _client;

        public SupabaseService(string url, string key)
        {
            var options = new SupabaseOptions
            {
                AutoConnectRealtime = true
            };

            _client = new Supabase.Client(url, key, options);
        }

        public Supabase.Client GetClient()
        {
            return _client;
        }

        public async Task InitializeAsync()
        {
            await _client.InitializeAsync();
        }
    }
}
