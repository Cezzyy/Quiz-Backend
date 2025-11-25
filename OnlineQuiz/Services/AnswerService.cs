using Mapster;
using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.IServices;
using OnlineQuiz.Models;

namespace OnlineQuiz.Services
{
    public class AnswerService : IAnswerService
    {
        private readonly IAttemptAnswerRepository _answerRepository;
        private readonly IAttemptRepository _attemptRepository;

        public AnswerService(
            IAttemptAnswerRepository answerRepository,
            IAttemptRepository attemptRepository)
        {
            _answerRepository = answerRepository;
            _attemptRepository = attemptRepository;
        }

        public async Task<AnswerResponseDto> RecordAnswerAsync(CreateAnswerDto createAnswerDto, int studentId)
        {
            var attempt = await _attemptRepository.GetByIdAsync(createAnswerDto.AttemptId);
            if (attempt == null)
            {
                throw new ArgumentException($"Attempt with ID {createAnswerDto.AttemptId} not found");
            }

            if (attempt.UserId != studentId)
            {
                throw new UnauthorizedAccessException("You can only record answers for your own attempts");
            }

            if (attempt.SubmittedAt != null)
            {
                throw new InvalidOperationException("Cannot record answers for submitted attempts");
            }

            var answer = new AttemptAnswer
            {
                AttemptId = createAnswerDto.AttemptId,
                QuestionId = createAnswerDto.QuestionId,
                ChoiceId = createAnswerDto.ChoiceId,
                FreeText = createAnswerDto.TextAnswer,
                IsCorrect = null // Will be determined during grading
            };

            var createdAnswer = await _answerRepository.CreateAsync(answer);

            return new AnswerResponseDto
            {
                AnswerId = createdAnswer.AttemptAnswerId,
                AttemptId = createdAnswer.AttemptId,
                QuestionId = createdAnswer.QuestionId,
                ChoiceId = createdAnswer.ChoiceId,
                TextAnswer = createdAnswer.FreeText,
                AnsweredAt = DateTime.UtcNow,
                IsCorrect = attempt.SubmittedAt != null ? createdAnswer.IsCorrect : null
            };
        }

        public async Task<List<AnswerResponseDto>> GetAnswersForAttemptAsync(int attemptId, int userId)
        {
            var attempt = await _attemptRepository.GetByIdAsync(attemptId);
            if (attempt == null)
            {
                throw new ArgumentException($"Attempt with ID {attemptId} not found");
            }

            var answers = await _answerRepository.GetByAttemptIdAsync(attemptId);
            var response = new List<AnswerResponseDto>();

            foreach (var answer in answers)
            {
                response.Add(new AnswerResponseDto
                {
                    AnswerId = answer.AttemptAnswerId,
                    AttemptId = answer.AttemptId,
                    QuestionId = answer.QuestionId,
                    ChoiceId = answer.ChoiceId,
                    TextAnswer = answer.FreeText,
                    AnsweredAt = DateTime.UtcNow,
                    IsCorrect = attempt.SubmittedAt != null ? answer.IsCorrect : null
                });
            }

            return response;
        }
    }
}
