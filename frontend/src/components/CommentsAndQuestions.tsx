type CommentsCopy = {
  title: string;
  description: string;
  askTitle: string;
  askDescription: string;
  nameLabel: string;
  namePlaceholder: string;
  emailLabel: string;
  emailPlaceholder: string;
  questionLabel: string;
  questionPlaceholder: string;
  sendButton: string;
  latestTitle: string;
  empty: string;
};

type CommentsAndQuestionsProps = {
  copy: CommentsCopy;
};

function CommentsAndQuestions({ copy }: CommentsAndQuestionsProps) {
  return (
    <section className="card comments">
      <div className="section-header">
        <h2>{copy.title}</h2>
        <p className="muted">{copy.description}</p>
      </div>

      <div className="comments__grid">
        <div className="comments__panel">
          <h3>{copy.askTitle}</h3>
          <p className="muted">{copy.askDescription}</p>
          <form className="comments__form">
            <label>
              <span>{copy.nameLabel}</span>
              <input type="text" placeholder={copy.namePlaceholder} disabled />
            </label>
            <label>
              <span>{copy.emailLabel}</span>
              <input
                type="email"
                placeholder={copy.emailPlaceholder}
                disabled
              />
            </label>
            <label>
              <span>{copy.questionLabel}</span>
              <textarea
                placeholder={copy.questionPlaceholder}
                rows={4}
                disabled
              />
            </label>
            <button type="button" className="primary" disabled>
              {copy.sendButton}
            </button>
          </form>
        </div>

        <div className="comments__panel">
          <h3>{copy.latestTitle}</h3>
          <div className="comments__empty">
            <p className="muted">{copy.empty}</p>
          </div>
        </div>
      </div>
    </section>
  );
}

export default CommentsAndQuestions;
