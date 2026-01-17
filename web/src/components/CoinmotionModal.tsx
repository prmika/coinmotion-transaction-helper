import { useEffect, useState } from "react";
import type { ChangeEvent, FormEvent } from "react";
import { translations, type Language } from "../i18n";

type UploadStatus = "idle" | "uploading" | "success" | "error";
type ModalStep = "disclaimer" | "instructions" | "upload" | "support";

type CoinmotionModalProps = {
  isOpen: boolean;
  apiBaseUrl: string;
  onClose: () => void;
  language: Language;
};

function CoinmotionModal({
  isOpen,
  apiBaseUrl,
  onClose,
  language,
}: CoinmotionModalProps) {
  const [file, setFile] = useState<File | null>(null);
  const [status, setStatus] = useState<UploadStatus>("idle");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [downloadUrl, setDownloadUrl] = useState<string | null>(null);
  const [hasAcknowledged, setHasAcknowledged] = useState(false);
  const [step, setStep] = useState<ModalStep>("disclaimer");
  const [selectedYear, setSelectedYear] = useState("");
  const [isPreviewOpen, setIsPreviewOpen] = useState(false);

  const t = translations[language].modal;

  useEffect(() => {
    if (!isOpen) {
      setFile(null);
      setStatus("idle");
      setErrorMessage(null);
      setHasAcknowledged(false);
      setStep("disclaimer");
      setSelectedYear("");
      setIsPreviewOpen(false);
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
      const url = new URL(`${apiBaseUrl}/report/pdf-zip`);
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
          const data = await response.json();
          throw new Error(data?.detail || t.errors.uploadFailed);
        }

        const text = await response.text();
        throw new Error(text || t.errors.uploadFailed);
      }

      const blob = await response.blob();
      const objectUrl = URL.createObjectURL(blob);
      setDownloadUrl(objectUrl);
      setStatus("success");
      setStep("support");
    } catch (error) {
      const message =
        error instanceof Error ? error.message : t.errors.uploadFailed;
      setErrorMessage(message);
      setStatus("error");
    }
  };

  if (!isOpen) {
    return null;
  }

  return (
    <div className="modal" role="dialog" aria-modal="true">
      <div className="modal__backdrop" onClick={onClose} />
      <div className="modal__content">
        <header className="modal__header">
          <div>
            <p className="modal__eyebrow">Coinmotion</p>
            <h2>{t.title}</h2>
          </div>
          <button
            type="button"
            className="modal__close"
            onClick={onClose}
            aria-label="Close"
          >
            ✕
          </button>
        </header>

        {step === "disclaimer" && (
          <section className="modal__section">
            <h3>{t.disclaimerTitle}</h3>
            <div className="modal__copy">
              {t.disclaimerParagraphs.map((paragraph) => (
                <p className="muted" key={paragraph}>
                  {paragraph}
                </p>
              ))}
            </div>
            <label className="checkbox">
              <input
                type="checkbox"
                checked={hasAcknowledged}
                onChange={(event) => setHasAcknowledged(event.target.checked)}
              />
              <span>{t.disclaimerAcknowledge}</span>
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

        {step === "instructions" && (
          <section className="modal__section">
            <h3>{t.instructionsTitle}</h3>
            <div className="modal__page">
              <div className="modal__copy">
                <ol className="steps">
                  {t.instructionsSteps.map((item) => (
                    <li key={item}>{item}</li>
                  ))}
                </ol>
              </div>
              <button
                type="button"
                className="video-placeholder"
                onClick={() => setIsPreviewOpen(true)}
                aria-label={t.videoTitle}
              >
                <img
                  src="/src/assets/coinmotion_help.gif"
                  alt={t.videoTitle}
                  className="video-placeholder__media"
                />
                <span className="video-placeholder__hint">
                  {t.enlargeVideo}
                </span>
              </button>
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
              <label className="file-input">
                <input type="file" accept=".csv" onChange={handleFileChange} />
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
            <div className="modal__copy">
              <div className="status status--success">{t.success}</div>
              <p className="muted">{t.supportDescription}</p>
              <a
                className="support-link"
                href="https://www.buymeacoffee.com/"
                target="_blank"
                rel="noreferrer"
              >
                {t.buyCoffee}
              </a>
            </div>
            <div className="modal__actions">
              {downloadUrl && (
                <button
                  className="primary"
                  onClick={() => {
                    if (downloadUrl) {
                      const link = document.createElement("a");
                      link.href = downloadUrl;
                      link.download = "pdf_reports.zip";
                      link.click();
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

      {isPreviewOpen && (
        <div className="lightbox" role="dialog" aria-modal="true">
          <div
            className="lightbox__backdrop"
            onClick={() => setIsPreviewOpen(false)}
          />
          <div className="lightbox__content">
            <button
              type="button"
              className="lightbox__close"
              onClick={() => setIsPreviewOpen(false)}
              aria-label={language === "fi" ? "Sulje" : "Close"}
            >
              ✕
            </button>
            <img
              src="/src/assets/coinmotion_help.gif"
              alt={t.videoTitle}
              className="lightbox__media"
            />
          </div>
        </div>
      )}
    </div>
  );
}

export default CoinmotionModal;
