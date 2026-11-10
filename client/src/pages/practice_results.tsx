interface PracticeResultsProps {
    correctAnswers: number;
    totalQuestions: number;
}

export default function PracticeResults({
    correctAnswers,
    totalQuestions,
}: PracticeResultsProps) {
    return (
        <div className="container play">
            <h1 className="page-title">Practice completed!</h1>

            <p className="play__verdict">
                {correctAnswers} / {totalQuestions} correct
            </p>
        </div>
    );
}