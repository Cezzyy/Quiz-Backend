using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Services;

namespace OnlineQuiz.Repository
{
    public class ChoiceRepository : IChoiceRepository
    {
        private readonly SupabaseService _supabaseService;

        public ChoiceRepository(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<Choice> CreateAsync(Choice choice)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Choice>().Insert(choice);
            return result.Models.First();
        }

        public async Task<Choice?> GetByIdAsync(int choiceId)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Choice>()
                .Where(c => c.ChoiceId == choiceId)
                .Get();
            return result.Models.FirstOrDefault();
        }

        public async Task<List<Choice>> GetByQuestionIdAsync(int questionId)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Choice>()
                .Where(c => c.QuestionId == questionId)
                .Get();
            return result.Models;
        }

        public async Task<Choice> UpdateAsync(Choice choice)
        {
            var client = _supabaseService.GetClient();
            var result = await client.From<Choice>().Update(choice);
            return result.Models.First();
        }

        public async Task<bool> DeleteAsync(int choiceId)
        {
            var client = _supabaseService.GetClient();
            await client.From<Choice>()
                .Where(c => c.ChoiceId == choiceId)
                .Delete();
            return true;
        }
    }
}
