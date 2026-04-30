import { useMemo, useState } from "react";
import "./App.css";
import BrokerModal from "./components/BrokerModal";
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

  const t = translations[language];

  return (
    <div className="app">
      <header className="app__header">
        <h1>{t.app.title}</h1>
        <p>{t.app.subtitle}</p>
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
        <section className="section">
          <h2>{t.app.brokersTitle}</h2>
          <div className="broker-list">
            <button
              className="broker-item"
              type="button"
              onClick={() => setActiveBrokerId("coinmotion")}
            >
              <span>Coinmotion — {t.app.coinmotionDescription}</span>
              <span className="broker-tag broker-tag--active">
                {t.app.available}
              </span>
            </button>
            <div className="broker-item broker-item--disabled">
              <span>{t.app.moreBrokers}</span>
              <span className="broker-tag">{t.app.planned}</span>
            </div>
          </div>
        </section>

        <section className="section">
          <h2>{t.app.howItWorks}</h2>
          <ol className="steps">
            {t.app.steps.map((step) => (
              <li key={step}>{step}</li>
            ))}
          </ol>
        </section>
      </main>

      <BrokerModal
        isOpen={activeBrokerId !== null}
        apiBaseUrl={apiBaseUrl}
        onClose={() => setActiveBrokerId(null)}
        language={language}
        brokerConfig={activeBrokerId ? BROKERS[activeBrokerId] : null}
      />
    </div>
  );
}

export default App;
