import type { CardResponse, QuestionRequest } from "../api/types";

export const MAX_GENERATED_QUESTIONS = 50;

function shuffleArray<T>(array: T[]): T[] {
  const shuffled = [...array];

  for (let i = shuffled.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));

    [shuffled[i], shuffled[j]] = [shuffled[j], shuffled[i]];
  }

  return shuffled;
}

export function generateQuestions(
  cards: CardResponse[]
): QuestionRequest[] {
  const validCards = cards.filter(
    (card) => card.term.trim() && card.definition.trim()
  );

  // Sample the questions; distractors still come from all valid cards.
  const selected = shuffleArray(validCards).slice(0, MAX_GENERATED_QUESTIONS);

  return selected.map((card, index) => {
    const correctAnswer = card.definition.trim();

    const incorrectAnswers = [
      ...new Set(
        validCards
          .filter((other) => other.id !== card.id)
          .map((other) => other.definition.trim())
          .filter((answer) => answer !== correctAnswer)
      ),
    ];

    const shuffledIncorrect = shuffleArray(incorrectAnswers);

    const options = [
      { text: correctAnswer, isCorrect: true },
      ...shuffledIncorrect.slice(0, 3).map((text) => ({
        text,
        isCorrect: false,
      })),
    ];

    return {
      text: `What is the translation of "${card.term}"?`,
      timeLimitSeconds: 30,
      points: 100,
      orderIndex: index,
      answerOptions: shuffleArray(options),
    };
  });
}