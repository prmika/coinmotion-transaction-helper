import { useMemo, useState } from "react";
import "./App.css";
import CoinmotionModal from "./components/CoinmotionModal";

function App() {
  const [isModalOpen, setIsModalOpen] = useState(false);

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

  return (
    <div className="app">
      <header className="app__header">
        <div className="brand">
          <span className="brand__dot" aria-hidden="true" />
          <div>
            <p className="brand__eyebrow">coinmotion-transaction-helper</p>
            <h1 className="brand__title">Crypto Tax Report Builder</h1>
          </div>
        </div>
        <p className="brand__subtitle">
          Create country-ready PDF tax reports from your broker transaction
          statements in minutes.
        </p>
      </header>

      <main className="app__main">
        <section className="card hero">
          <div>
            <h2>Make tax reporting simple</h2>
            <p className="muted">
              This tool converts broker statements into PDF tax reports with
              FIFO-based cost basis calculations. Start by choosing your broker
              below.
            </p>
          </div>
          <div className="hero__stats">
            <div>
              <p className="hero__label">Output</p>
              <p className="hero__value">PDF zip</p>
            </div>
            <div>
              <p className="hero__label">Method</p>
              <p className="hero__value">FIFO + assumptions</p>
            </div>
            <div>
              <p className="hero__label">Privacy</p>
              <p className="hero__value">Local dev</p>
            </div>
          </div>
        </section>

        <section className="card">
          <div className="section-header">
            <h2>Supported brokers</h2>
            <p className="muted">
              More integrations are planned. Select Coinmotion to get started.
            </p>
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
                  <p className="muted">CSV statement import</p>
                </div>
                <span className="badge badge--active">Available</span>
              </div>
            </button>
            <div className="broker-card broker-card--disabled">
              <div className="broker-card__logo placeholder">+</div>
              <div className="broker-card__body">
                <div>
                  <h3>More brokers</h3>
                  <p className="muted">Coming soon</p>
                </div>
                <span className="badge">Planned</span>
              </div>
            </div>
          </div>
        </section>

        <section className="card card--muted">
          <h2>How it works</h2>
          <ol className="steps">
            <li>Export your broker transaction statement as CSV.</li>
            <li>Review the disclaimer and upload the file.</li>
            <li>Download a zip file containing PDF reports per currency.</li>
          </ol>
        </section>
      </main>

      <CoinmotionModal
        isOpen={isModalOpen}
        apiBaseUrl={apiBaseUrl}
        onClose={handleCloseModal}
      />
    </div>
  );
}

export default App;
