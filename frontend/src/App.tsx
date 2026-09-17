import { useEffect, useMemo, useState } from "react";
import "./App.css";
import BrokerModal from "./components/BrokerModal";
import { translations, type Language } from "./i18n";
import { BROKERS, type BrokerId } from "./config/brokerConfigs";
import { beginLogin, completeLogin, getAccessToken, isOidcConfigured, logout } from "./auth";

function App() {
  const [activeBrokerId, setActiveBrokerId] = useState<BrokerId | null>(null);
  const [language, setLanguage] = useState<Language>("fi");
  const [accessToken, setAccessToken] = useState<string | null>(getAccessToken());
  const [authError, setAuthError] = useState<string | null>(null);

  useEffect(() => {
    completeLogin().then((completed) => completed && setAccessToken(getAccessToken())).catch((error: unknown) => setAuthError(error instanceof Error ? error.message : "Login failed"));
  }, []);

  useEffect(() => {
    if (!accessToken) return;
    const timer = window.setInterval(() => setAccessToken(getAccessToken()), 1000);
    return () => window.clearInterval(timer);
  }, [accessToken]);

  const apiBaseUrl = useMemo(() => {
    return (
      (import.meta.env.VITE_API_URL as string | undefined) ??
      "http://localhost:8000"
    );
  }, []);

  const t = translations[language];

  const handleLogin = async () => {
    try { setAuthError(null); await beginLogin(); } catch (error) { setAuthError(error instanceof Error ? error.message : "Login failed"); }
  };

  return (
    <div className="app">
      <header className="app__header">
        <h1>{t.app.title}</h1>
        <p>{t.app.subtitle}</p>
        {isOidcConfigured() && (accessToken ? <button type="button" onClick={() => void logout().then(() => setAccessToken(null))}>Log out</button> : <button type="button" onClick={() => void handleLogin()}>Log in</button>)}
        {authError && <p role="alert">{authError}</p>}
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
              onClick={() => accessToken ? setActiveBrokerId("coinmotion") : void handleLogin()}
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
        accessToken={accessToken}
      />
    </div>
  );
}

export default App;
