import { useEffect, useState } from "react";
import type { ChangeEvent, FormEvent } from "react";
import type { BrokerConfig } from "../config/brokerConfigs";
import { translations, type Language } from "../i18n";

type UploadStatus = "idle" | "uploading" | "success" | "error";
type ModalStep = "disclaimer" | "instructions" | "upload" | "support";

type PricingMetrics = {
  total_sales_transactions: number;
  total_sales_volume_eur: number;
  total_profit_loss_eur: number;
};

type BrokerModalProps = {
  isOpen: boolean;
  apiBaseUrl: string;
  onClose: () => void;
  language: Language;
  brokerConfig: BrokerConfig | null;
};

function BrokerModal({
  isOpen,
  apiBaseUrl,
  onClose,
  language,
  brokerConfig,
}: BrokerModalProps) {
  const [file, setFile] = useState<File | null>(null);
  const [status, setStatus] = useState<UploadStatus>("idle");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [downloadUrl, setDownloadUrl] = useState<string | null>(null);
  const [hasAcknowledged, setHasAcknowledged] = useState(false);
  const [step, setStep] = useState<ModalStep>("disclaimer");
  const [selectedYear, setSelectedYear] = useState("");
  const [isPreviewOpen, setIsPreviewOpen] = useState(false);
  const [pricingMetrics, setPricingMetrics] = useState<PricingMetrics | null>(
    null,
  );

  const t = translations[language].brokerModal;
  const brokerTranslation = brokerConfig ? translations[language].brokers[brokerConfig.id as keyof typeof translations[typeof language]["brokers"]] : null;

  useEffect(() => {
    if (!isOpen) {
      setFile(null);
      setStatus("idle");
      setErrorMessage(null);
      setHasAcknowledged(false);
      setStep("disclaimer");
      setSelectedYear("");
      setIsPreviewOpen(false);
      setPricingMetrics(null);
      if (downloadUrl) {
        URL.revokeObjectURL(downloadUrl);
        setDownloadUrl(null);
      }
    }
  }, [isOpen, downloadUrl]);

  useEffect(() => {
    return () => {
      if (downloadUrl) {
        URL.revokeObjectURL(downloadUrl);
      }
    };
  }, [downloadUrl]);

  const handleFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    const selectedFile = event.target.files?.[0] ?? null;
    setFile(selectedFile);
    setStatus("idle");
    setErrorMessage(null);
    if (downloadUrl) {
      URL.revokeObjectURL(downloadUrl);
      setDownloadUrl(null);
    }
  };

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();

    if (!file) {
      setErrorMessage(t.errors.fileRequired);
      setStatus("error");
      return;
    }

    if (selectedYear && !/^[0-9]{4}$/.test(selectedYear)) {
      setErrorMessage(t.errors.yearFormat);
      setStatus("error");
      return;
    }

    setStatus("uploading");
    setErrorMessage(null);

    const formData = new FormData();
    formData.append("file", file);

    try {
      if (!brokerConfig) throw new Error("Missing broker config");
      
      const url = new URL(`${apiBaseUrl}${brokerConfig.apiEndpoint}`);
      if (selectedYear) {
        url.searchParams.set("year", selectedYear);
      }

      const response = await fetch(url.toString(), {
        method: "POST",
        body: formData,
      });

      if (!response.ok) {
        const contentType = response.headers.get("content-type");
        if (contentType?.includes("application/json")) {
          const errData = await response.json();
          throw new Error(errData?.detail || t.errors.uploadFailed);
        }

        const text = await response.text();
        throw new Error(text || t.errors.uploadFailed);
      }

      const data = await response.json();
      setPricingMetrics(data.pricing_metrics);

      setDownloadUrl(`${apiBaseUrl}/report/download/${data.report_id}`);

      setStatus("success");
      setStep("support");
    } catch (error) {
      const message =
        error instanceof Error ? error.message : t.errors.uploadFailed;
      setErrorMessage(message);
      setStatus("error");
    }
  };

  if (!isOpen || !brokerConfig) {
    return null;
  }

  return (
    <div className="modal" role="dialog" aria-modal="true">
      <div
        className="modal__backdrop"
        onClick={onClose}
        onKeyDown={(e) =>
          (e.key === "Enter" || e.key === "Escape") && onClose()
        }
        role="button"
        tabIndex={0}
        aria-label={t.close}
      />
      <div className="modal__content">
        <header className="modal__header">
          <div>
            <p className="modal__eyebrow">{brokerConfig.name}</p>
            <h2>{t.title}</h2>
          </div>
          <button
            type="button"
            className="modal__close"
            onClick={onClose}
            aria-label={t.close}
          >
            ✕
          </button>
        </header>

        {step === "disclaimer" && brokerTranslation && (
          <section className="modal__section">
            <h3>{brokerTranslation.disclaimerTitle}</h3>
            <div className="modal__copy">
              {brokerTranslation.disclaimerParagraphs.map((paragraph) => (
                <p className="muted" key={paragraph}>
                  {paragraph}
                </p>
              ))}
            </div>
            <label className="checkbox-label">
              <input
                type="checkbox"
                checked={hasAcknowledged}
                onChange={(e) => setHasAcknowledged(e.target.checked)}
              />
              <span>{brokerTranslation.disclaimerAcknowledge}</span>
            </label>
            <div className="modal__actions">
              <button type="button" className="secondary" onClick={onClose}>
                {t.previous}
              </button>
              <button
                type="button"
                className="primary"
                disabled={!hasAcknowledged}
                onClick={() => setStep("instructions")}
              >
                {t.next}
              </button>
            </div>
          </section>
        )}

        {step === "instructions" && brokerTranslation && (
          <section className="modal__section">
            <h3>{brokerTranslation.instructionsTitle}</h3>
            <div className="modal__page">
              <div className="modal__copy">
                <ol className="steps">
                  {brokerTranslation.instructionsSteps.map((item) => (
                    <li key={item}>{item}</li>
                  ))}
                </ol>
              </div>
              {brokerConfig.hasTutorialVideo && brokerConfig.videoAssetPath && (
                <button
                  type="button"
                  className="video-placeholder"
                  onClick={() => setIsPreviewOpen(true)}
                  aria-label={brokerTranslation.videoTitle}
                >
                  <img
                    src={brokerConfig.videoAssetPath}
                    alt={brokerTranslation.videoTitle}
                    className="video-placeholder__media"
                  />
                  <span className="video-placeholder__hint">
                    {t.enlargeVideo}
                  </span>
                </button>
              )}
            </div>
            <div className="modal__actions">
              <button
                type="button"
                className="secondary"
                onClick={() => setStep("disclaimer")}
              >
                {t.previous}
              </button>
              <button
                type="button"
                className="primary"
                onClick={() => setStep("upload")}
              >
                {t.next}
              </button>
            </div>
          </section>
        )}

        {step === "upload" && (
          <section className="modal__section">
            <h3>{t.uploadTitle}</h3>
            <p className="muted">{t.uploadDescription}</p>
            <form className="upload" onSubmit={handleSubmit}>
              {brokerConfig.hasYearSelection && (
                <label className="field-row">
                  <span>{t.yearLabel}</span>
                  <input
                    type="number"
                    min="2009"
                    max="2100"
                    placeholder="2025"
                    value={selectedYear}
                    onChange={(event) => setSelectedYear(event.target.value)}
                  />
                  <span className="muted">{t.yearHint}</span>
                </label>
              )}
              <label className="file-input">
                <input type="file" accept={brokerConfig.fileType} onChange={handleFileChange} />
                <span className="file-input__button">{t.chooseFile}</span>
                <span className="file-input__name">
                  {file ? file.name : t.noFile}
                </span>
              </label>

              {status === "error" && errorMessage && (
                <div className="status status--error" role="alert">
                  {errorMessage}
                </div>
              )}
              {status === "success" && (
                <div className="status status--success">{t.success}</div>
              )}

              <div className="modal__actions">
                <button
                  type="button"
                  className="secondary"
                  onClick={() => setStep("instructions")}
                >
                  {t.previous}
                </button>
                <button
                  type="submit"
                  className="primary"
                  disabled={status === "uploading"}
                >
                  {status === "uploading" ? t.generating : t.generate}
                </button>
              </div>
            </form>
          </section>
        )}

        {step === "support" && (
          <section className="modal__section">
            <h3>{t.supportTitle}</h3>

            {brokerConfig.metricsEnabled && pricingMetrics && (
              <div
                className="status"
                style={{ textAlign: "left", marginBottom: "1rem" }}
              >
                <strong>{t.reportOverview}</strong>
                <ul style={{ marginTop: "0.5rem", paddingLeft: "1.2rem" }}>
                  <li>
                    {t.transactionsProcessed}{" "}
                    {pricingMetrics.total_sales_transactions}
                  </li>
                  <li>
                    {t.totalSalesVolume}{" "}
                    {pricingMetrics.total_sales_volume_eur.toFixed(2)} &euro;
                  </li>
                  <li>
                    {t.totalProfitLoss}{" "}
                    {pricingMetrics.total_profit_loss_eur.toFixed(2)} &euro;
                  </li>
                </ul>
              </div>
            )}

            <div className="modal__copy">
              {status === "error" && errorMessage ? (
                <div className="status status--error" role="alert">
                  {errorMessage}
                </div>
              ) : (
                <div className="status status--success">{t.success}</div>
              )}
              <p className="muted">{t.supportDescription}</p>
              <a
                href="https://www.buymeacoffee.com/prmika"
                target="_blank"
                rel="noopener noreferrer"
              >
                <img
                  src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png"
                  alt="Buy Me A Coffee"
                  style={{ height: "60px", width: "217px" }}
                />
              </a>
            </div>
            <div className="modal__actions">
              {downloadUrl && (
                <button
                  className="primary"
                  onClick={async () => {
                    try {
                      setErrorMessage(null);
                      const response = await fetch(downloadUrl);
                      if (!response.ok) {
                        const errData = await response.json().catch(() => ({}));
                        throw new Error(
                          errData.detail || t.errors.downloadExpired,
                        );
                      }

                      const blob = await response.blob();
                      const url = window.URL.createObjectURL(blob);
                      const link = document.createElement("a");
                      link.href = url;
                      link.download = "pdf_reports.zip";
                      document.body.appendChild(link);
                      link.click();
                      document.body.removeChild(link);

                      setTimeout(() => window.URL.revokeObjectURL(url), 1000);
                    } catch (err) {
                      setErrorMessage(
                        err instanceof Error
                          ? err.message
                          : t.errors.downloadFailed,
                      );
                      setStatus("error");
                    }
                  }}
                >
                  {t.download}
                </button>
              )}
            </div>
          </section>
        )}
      </div>

      {isPreviewOpen && brokerTranslation && brokerConfig.videoAssetPath && (
        <div className="lightbox" role="dialog" aria-modal="true">
          <div
            className="lightbox__backdrop"
            onClick={() => setIsPreviewOpen(false)}
            onKeyDown={(e) =>
              (e.key === "Enter" || e.key === "Escape") &&
              setIsPreviewOpen(false)
            }
            role="button"
            tabIndex={0}
            aria-label={t.close}
          />
          <div className="lightbox__content">
            <button
              type="button"
              className="lightbox__close"
              onClick={() => setIsPreviewOpen(false)}
              aria-label={t.close}
            >
              ✕
            </button>
            <img
              src={brokerConfig.videoAssetPath}
              alt={brokerTranslation.videoTitle}
              className="lightbox__media"
            />
          </div>
        </div>
      )}
    </div>
  );
}

export default BrokerModal;
