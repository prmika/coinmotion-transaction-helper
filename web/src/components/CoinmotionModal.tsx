import { useEffect, useState } from "react";
import type { ChangeEvent, FormEvent } from "react";

type UploadStatus = "idle" | "uploading" | "success" | "error";
type ModalStep = "disclaimer" | "instructions" | "upload" | "support";

type CoinmotionModalProps = {
  isOpen: boolean;
  apiBaseUrl: string;
  onClose: () => void;
};

function CoinmotionModal({
  isOpen,
  apiBaseUrl,
  onClose,
}: CoinmotionModalProps) {
  const [file, setFile] = useState<File | null>(null);
  const [status, setStatus] = useState<UploadStatus>("idle");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [downloadUrl, setDownloadUrl] = useState<string | null>(null);
  const [hasAcknowledged, setHasAcknowledged] = useState(false);
  const [step, setStep] = useState<ModalStep>("disclaimer");

  useEffect(() => {
    if (!isOpen) {
      setFile(null);
      setStatus("idle");
      setErrorMessage(null);
      setHasAcknowledged(false);
      setStep("disclaimer");
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
      setErrorMessage("Select a CSV file first.");
      setStatus("error");
      return;
    }

    setStatus("uploading");
    setErrorMessage(null);

    const formData = new FormData();
    formData.append("file", file);

    try {
      const response = await fetch(`${apiBaseUrl}/report/pdf-zip`, {
        method: "POST",
        body: formData,
      });

      if (!response.ok) {
        const contentType = response.headers.get("content-type");
        if (contentType?.includes("application/json")) {
          const data = await response.json();
          throw new Error(data?.detail || "Upload failed.");
        }

        const text = await response.text();
        throw new Error(text || "Upload failed.");
      }

      const blob = await response.blob();
      const objectUrl = URL.createObjectURL(blob);
      setDownloadUrl(objectUrl);
      setStatus("success");
      setStep("support");
    } catch (error) {
      const message = error instanceof Error ? error.message : "Upload failed.";
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
            <h2>Generate your tax report</h2>
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
            <h3>Disclaimer</h3>
            <div className="modal__copy">
              {/* Tämä palvelu tuottaa automaattisesti laskennallisen raportin
              Coinmotionin transaktiotietojen perusteella. Raportti on
              suuntaa-antava eikä ole veroneuvontaa. Käyttäjä vastaa itse
              tietojen oikeellisuudesta ja veroilmoitukselle ilmoitettavista
              tiedoista. Palvelu ei vastaa mahdollisista veroseuraamuksista tai
              virheistä, jotka aiheutuvat raportin käytöstä. ☑ Ymmärrän, että
              raportti on suuntaa-antava eikä korvaa virallista veroneuvontaa. */}
              <p className="muted">
                This service generates an automated, calculated report based on
                transaction data from Coinmotion.
              </p>
              <p className="muted">
                The report is for informational purposes only and does not
                constitute tax advice.
              </p>
              <p className="muted">
                The user is solely responsible for verifying the accuracy of the
                data and the information submitted to tax authorities.
              </p>
              <p className="muted">
                The service provider is not responsible for any tax consequences
                or errors resulting from the use of this report.
              </p>
            </div>
            <label className="checkbox">
              <input
                type="checkbox"
                checked={hasAcknowledged}
                onChange={(event) => setHasAcknowledged(event.target.checked)}
              />
              <span>
                I understand that this report is informational only and does not
                replace official tax advice.
              </span>
            </label>
            <div className="modal__actions">
              <button type="button" className="secondary" onClick={onClose}>
                Previous
              </button>
              <button
                type="button"
                className="primary"
                disabled={!hasAcknowledged}
                onClick={() => setStep("instructions")}
              >
                Next
              </button>
            </div>
          </section>
        )}

        {step === "instructions" && (
          <section className="modal__section">
            <h3>How to export your CSV</h3>
            <div className="modal__page">
              <div className="modal__copy">
                <ol className="steps">
                  <li>Open Coinmotion and navigate to transactions.</li>
                  <li>Choose the CSV export for the full date range.</li>
                  <li>Save the CSV file to your computer.</li>
                </ol>
              </div>
              <div className="video-placeholder">
                <div>
                  <p className="video-placeholder__title">Video guide</p>
                  <p className="muted">Add a short walkthrough clip here.</p>
                </div>
              </div>
            </div>
            <div className="modal__actions">
              <button
                type="button"
                className="secondary"
                onClick={() => setStep("disclaimer")}
              >
                Previous
              </button>
              <button
                type="button"
                className="primary"
                onClick={() => setStep("upload")}
              >
                Next
              </button>
            </div>
          </section>
        )}

        {step === "upload" && (
          <section className="modal__section">
            <h3>Upload CSV</h3>
            <p className="muted">
              We will return a zip file containing one PDF report per currency.
            </p>
            <form className="upload" onSubmit={handleSubmit}>
              <label className="file-input">
                <input type="file" accept=".csv" onChange={handleFileChange} />
                <span className="file-input__button">Choose file</span>
                <span className="file-input__name">
                  {file ? file.name : "No file selected"}
                </span>
              </label>

              <div className="actions">
                <button
                  type="button"
                  className="secondary"
                  onClick={() => setStep("instructions")}
                >
                  Previous
                </button>
                <button
                  type="submit"
                  className="primary"
                  disabled={status === "uploading"}
                >
                  {status === "uploading" ? "Generating…" : "Generate PDF zip"}
                </button>
              </div>
            </form>

            {status === "error" && errorMessage && (
              <div className="status status--error" role="alert">
                {errorMessage}
              </div>
            )}
            {status === "success" && (
              <div className="status status--success">
                Your report is ready. Download the zip file to view the PDFs.
              </div>
            )}

            <div className="api-hint">
              API endpoint: <span>{apiBaseUrl}/report/pdf-zip</span>
            </div>
          </section>
        )}

        {step === "support" && (
          <section className="modal__section">
            <h3>Your report is ready</h3>
            <div className="modal__copy">
              <p className="muted">
                Your PDF zip has been generated. If this tool saved you time,
                you can support future improvements below.
              </p>
              <a
                className="support-link"
                href="https://www.buymeacoffee.com/"
                target="_blank"
                rel="noreferrer"
              >
                Buy me a coffee
              </a>
            </div>
            <div className="modal__actions">
              <button
                type="button"
                className="secondary"
                onClick={() => setStep("upload")}
              >
                Previous
              </button>
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
                  Download zip
                </button>
              )}
            </div>
          </section>
        )}
      </div>
    </div>
  );
}

export default CoinmotionModal;
