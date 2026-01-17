import { useMemo, useState } from "react";
import "./App.css";
import CoinmotionModal from "./components/CoinmotionModal";
import CommentsAndQuestions from "./components/CommentsAndQuestions";
import { translations, type Language } from "./i18n";

function App() {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [language, setLanguage] = useState<Language>("fi");

  const apiBaseUrl = useMemo(() => {
    return (
      (import.meta.env.VITE_API_URL as string | undefined) ??
      "http://localhost:8000"
    );
  }, []);

  const handleOpenModal = () => {
    setIsModalOpen(true);
  };

  const handleCloseModal = () => {
    setIsModalOpen(false);
  };

  const t = translations[language];

  return (
    <div className="app">
      <header className="app__header">
        <div className="brand">
          <span className="brand__dot" aria-hidden="true" />
          <div>
            <h1 className="brand__title">{t.app.title}</h1>
          </div>
        </div>
        <p className="brand__subtitle">{t.app.subtitle}</p>
        <div className="language-toggle" role="group" aria-label="Language">
          <button
            type="button"
            className={language === "fi" ? "active" : ""}
            onClick={() => setLanguage("fi")}
          >
            FI
          </button>
          <button
            type="button"
            className={language === "en" ? "active" : ""}
            onClick={() => setLanguage("en")}
          >
            EN
          </button>
        </div>
      </header>

      <main className="app__main">
        <section className="card hero">
          <div>
            <h2>{t.app.heroTitle}</h2>
            <p className="muted">{t.app.heroDescription}</p>
          </div>
          <div className="hero__stats">
            <div>
              <p className="hero__label">{t.app.stats.outputLabel}</p>
              <p className="hero__value">{t.app.stats.outputValue}</p>
            </div>
            <div>
              <p className="hero__label">{t.app.stats.methodLabel}</p>
              <p className="hero__value">{t.app.stats.methodValue}</p>
            </div>
            <div>
              <p className="hero__label">{t.app.stats.privacyLabel}</p>
              <p className="hero__value">{t.app.stats.privacyValue}</p>
            </div>
          </div>
        </section>

        <section className="card">
          <div className="section-header">
            <h2>{t.app.brokersTitle}</h2>
            <p className="muted">{t.app.brokersDescription}</p>
          </div>
          <div className="broker-grid">
            <button
              className="broker-card"
              type="button"
              onClick={handleOpenModal}
            >
              <div className="broker-card__logo">
                <img src="/coinmotion_logo.png" alt="Coinmotion logo" />
              </div>
              <div className="broker-card__body">
                <div>
                  <h3>Coinmotion</h3>
                  <p className="muted">{t.app.coinmotionDescription}</p>
                </div>
                <span className="badge badge--active">{t.app.available}</span>
              </div>
            </button>
            <div className="broker-card broker-card--disabled">
              <div className="broker-card__logo placeholder">+</div>
              <div className="broker-card__body">
                <div>
                  <h3>{t.app.moreBrokers}</h3>
                  <p className="muted">{t.app.comingSoon}</p>
                </div>
                <span className="badge">{t.app.planned}</span>
              </div>
            </div>
          </div>
        </section>

        <section className="card card--muted">
          <h2>{t.app.howItWorks}</h2>
          <ol className="steps">
            {t.app.steps.map((step) => (
              <li key={step}>{step}</li>
            ))}
          </ol>
        </section>

        <CommentsAndQuestions copy={t.comments} />
      </main>

      <CoinmotionModal
        isOpen={isModalOpen}
        apiBaseUrl={apiBaseUrl}
        onClose={handleCloseModal}
        language={language}
      />
    </div>
  );
}

export default App;
