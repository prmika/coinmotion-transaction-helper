import { useMemo, useState } from "react";
import "./App.css";
import BrokerModal from "./components/BrokerModal";
import CommentsAndQuestions from "./components/CommentsAndQuestions";
import { translations, type Language } from "./i18n";
import { BROKERS, type BrokerId } from "./config/brokerConfigs";

function App() {
  const [activeBrokerId, setActiveBrokerId] = useState<BrokerId | null>(null);
  const [language, setLanguage] = useState<Language>("fi");

  const apiBaseUrl = useMemo(() => {
    return (
      (import.meta.env.VITE_API_URL as string | undefined) ??
      "http://localhost:8000"
    );
  }, []);

  const handleOpenModal = (brokerId: BrokerId) => {
    setActiveBrokerId(brokerId);
  };

  const handleCloseModal = () => {
    setActiveBrokerId(null);
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
              onClick={() => handleOpenModal("coinmotion")}
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
            <button
              className="broker-card"
              type="button"
              onClick={() => handleOpenModal("binance")}
            >
              <div className="broker-card__logo">
                <img src="/binance_logo.png" alt="Binance logo" />
              </div>
              <div className="broker-card__body">
                <div>
                  <h3>Binance</h3>
                  <p className="muted">{t.app.binanceDescription}</p>
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

      <BrokerModal
        isOpen={activeBrokerId !== null}
        apiBaseUrl={apiBaseUrl}
        onClose={handleCloseModal}
        language={language}
        brokerConfig={activeBrokerId ? BROKERS[activeBrokerId] : null}
      />
    </div>
  );
}

export default App;
