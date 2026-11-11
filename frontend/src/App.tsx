import { useEffect, useMemo, useState } from "react";
import "./App.css";
import BrokerModal from "./components/BrokerModal";
import { translations, type Language } from "./i18n";
import { BROKERS, type BrokerId } from "./config/brokerConfigs";
import {
  beginLogin,
  completeLogin,
  getAccessToken,
  getTokenExpiresAt,
  isOidcConfigured,
  logout,
} from "./auth";

function App() {
  const [activeBrokerId, setActiveBrokerId] = useState<BrokerId | null>(null);
  const [language, setLanguage] = useState<Language>("fi");
  const [accessToken, setAccessToken] = useState<string | null>(
    getAccessToken(),
  );
  const [authError, setAuthError] = useState<string | null>(null);

  useEffect(() => {
    completeLogin()
      .then((completed) => completed && setAccessToken(getAccessToken()))
      .catch((error: unknown) =>
        setAuthError(error instanceof Error ? error.message : "Login failed"),
      );
  }, []);

  useEffect(() => {
    if (!accessToken) return;
    const expiresAt = getTokenExpiresAt();
    if (expiresAt === null) return;
    const timer = window.setTimeout(
      () => setAccessToken(getAccessToken()),
      Math.max(0, expiresAt - Date.now()),
    );
    return () => window.clearTimeout(timer);
  }, [accessToken]);

  const apiBaseUrl = useMemo(() => {
    return (
      (import.meta.env.VITE_API_URL as string | undefined) ??
      "http://localhost:8000"
    );
  }, []);

  const t = translations[language];
  const oidcConfigured = isOidcConfigured();

  const handleLogin = async () => {
    try {
      setAuthError(null);
      await beginLogin();
    } catch (error) {
      setAuthError(error instanceof Error ? error.message : "Login failed");
    }
  };

  return (
    <div className="app">
      <header className="app__header">
        <div className="app__brand">
          <span className="app__brand-mark" aria-hidden="true">
            CT
          </span>
          <div>
            <p className="app__eyebrow">{t.app.brandEyebrow}</p>
            <h1>{t.app.title}</h1>
          </div>
        </div>
        <div className="app__header-actions">
          <div
            className="language-toggle"
            role="group"
            aria-label={t.app.languageLabel}
          >
            <button
              type="button"
              className={language === "fi" ? "active" : ""}
              aria-pressed={language === "fi"}
              onClick={() => setLanguage("fi")}
            >
              FI
            </button>
            <button
              type="button"
              className={language === "en" ? "active" : ""}
              aria-pressed={language === "en"}
              onClick={() => setLanguage("en")}
            >
              EN
            </button>
          </div>
          {accessToken ? (
            <details className="account-menu">
              <summary className="account-menu__trigger">
                <span className="account-menu__avatar" aria-hidden="true">
                  U
                </span>
                <span>{t.app.accountLabel}</span>
                <span className="account-menu__caret" aria-hidden="true" />
              </summary>
              <div className="account-menu__panel">
                <p className="account-menu__status">
                  <span
                    className="account-menu__status-dot"
                    aria-hidden="true"
                  />
                  {t.app.signedIn}
                </p>
                <button
                  type="button"
                  className="account-menu__logout"
                  onClick={() => void logout().then(() => setAccessToken(null))}
                >
                  {t.app.logOut}
                </button>
              </div>
            </details>
          ) : oidcConfigured ? (
            <button
              type="button"
              className="header-login"
              onClick={() => void handleLogin()}
            >
              {t.app.logIn}
            </button>
          ) : null}
        </div>
      </header>

      {authError && (
        <p className="app__auth-error" role="alert">
          {authError}
        </p>
      )}

      {accessToken ? (
        <main className="app__main app__main--dashboard">
          <div className="dashboard-intro">
            <p className="app__eyebrow">{t.app.dashboardEyebrow}</p>
            <h2>{t.app.dashboardTitle}</h2>
            <p>{t.app.dashboardDescription}</p>
          </div>

          <section className="section">
            <div className="section__heading">
              <h2>{t.app.brokersTitle}</h2>
            </div>
            <div className="broker-list">
              <button
                className="broker-item"
                type="button"
                onClick={() => setActiveBrokerId("coinmotion")}
              >
                <span className="broker-item__copy">
                  <strong>Coinmotion</strong>
                  <span>{t.app.coinmotionDescription}</span>
                </span>
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
      ) : (
        <main className="app__main app__main--welcome">
          <section className="welcome">
            <div className="welcome__intro">
              <p className="app__eyebrow">{t.app.welcomeEyebrow}</p>
              <h2>{t.app.welcomeTitle}</h2>
              <p className="welcome__description">{t.app.welcomeDescription}</p>
              <div className="welcome__actions">
                <button
                  type="button"
                  className="primary welcome__login"
                  disabled={!oidcConfigured}
                  onClick={() => void handleLogin()}
                >
                  {t.app.logIn}
                  <span aria-hidden="true">-&gt;</span>
                </button>
                {!oidcConfigured && (
                  <p className="welcome__config-note">
                    {t.app.oidcNotConfigured}
                  </p>
                )}
              </div>
            </div>
            <div className="welcome__details">
              <div className="welcome__guide">
                <h3>{t.app.howItWorks}</h3>
                <ol className="steps">
                  {t.app.steps.map((step) => (
                    <li key={step}>{step}</li>
                  ))}
                </ol>
              </div>
              <div className="welcome__brokers">
                <h3>{t.app.brokersTitle}</h3>
                <ul className="welcome__broker-list">
                  {Object.values(BROKERS)
                    .filter((broker) => broker.apiEndpoint)
                    .map((broker) => (
                      <li key={broker.id}>
                        <span>{broker.name}</span>
                        <span className="broker-tag broker-tag--active">
                          {t.app.available}
                        </span>
                      </li>
                    ))}
                </ul>
                <p className="welcome__more-brokers">{t.app.moreBrokers}</p>
              </div>
            </div>
          </section>
          <p className="welcome__footnote">{t.app.welcomeFootnote}</p>
        </main>
      )}

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
