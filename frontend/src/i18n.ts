export type Language = "fi" | "en";

export const translations = {
  fi: {
    app: {
      title: "Kryptoveroraportit",
      subtitle:
        "Luo luovutusvoittolaskelmat valitsemasi välittäjän tapahtumatiedoista.",
      brokersTitle: "Välittäjät",
      coinmotionDescription: "CSV-tuonti",
      available: "Saatavilla",
      moreBrokers: "Lisää välittäjiä tulossa",
      planned: "",
      howItWorks: "Käyttöohje",
      steps: [
        "Vie välittäjän tapahtumat CSV-tiedostoon.",
        "Hyväksy vastuuvapauslauseke ja lataa tiedosto.",
        "Lataa zip-paketti, jossa on PDF-raportit valuutoittain.",
      ],
    },
    brokerModal: {
      title: "Luo luovutusvoittoraportti",
      uploadTitle: "Lataa CSV",
      uploadDescription:
        "Palvelu palauttaa zip-paketin, jossa on PDF-raportit valuutoittain.",
      yearLabel: "Raportin vuosi (valinnainen)",
      yearHint: "Jätä tyhjiksi, jos haluat raportin kaikilta vuosilta.",
      chooseFile: "Valitse tiedosto",
      noFile: "Ei tiedostoa valittuna",
      generate: "Luo raportti",
      generating: "Luodaan...",
      supportTitle: "Raportti valmis",
      supportDescription: "Luovutusvoittolaskelma on luotu.",
      transactionsProcessed: "Käsitellyt tapahtumat:",
      totalSalesVolume: "Myyntivolyymi:",
      totalProfitLoss: "Voitto/tappio:",
      download: "Lataa zip",
      donateTitle: "Tue",
      donateDescription:
        "Jos työkalu säästi aikaasi, voit halutessasi tukea jatkokehitystä.",
      previous: "Edellinen",
      next: "Seuraava",
      success: "Raportti on valmis. Lataa zip nähdäksesi PDF:t.",
      errors: {
        fileRequired: "Valitse CSV-tiedosto ensin.",
        yearFormat: "Vuoden tulee olla muodossa VVVV.",
        uploadFailed: "Lataus epäonnistui.",
        downloadFailed: "Lataus epäonnistui.",
        downloadExpired:
          "Lataus epäonnistui. Raportti on saattanut vanhentua. Luo uusi raportti.",
      },
      enlargeVideo: "Klikkaa suurentaaksesi",
      close: "Sulje",
    },
    brokers: {
      coinmotion: {
        name: "Coinmotion",
        videoTitle: "Video-ohje",
        videoDescription: "Lisää tähän lyhyt opastusvideo.",
        instructionsTitle: "Miten saan tapahtumaraportin CSV-muodossa",
        instructionsSteps: [
          "Avaa Coinmotion ja siirry raportointiin.",
          "Tapahtumaraportit kohdassa klikkaa 'Lataa CSV tiedosto'.",
          "Tiedosto tallentuu tietokoneellesi ja voit ladata sen tänne.",
        ],
        disclaimerTitle: "Vastuuvapauslauseke",
        disclaimerParagraphs: [
          "Tämä palvelu tuottaa automaattisesti laskennallisen raportin Coinmotionin transaktiotietojen perusteella.",
          "Raportti on suuntaa-antava eikä ole veroneuvontaa.",
          "Käyttäjä vastaa itse tietojen oikeellisuudesta ja veroilmoitukselle ilmoitettavista tiedoista.",
          "Palvelu ei vastaa mahdollisista veroseuraamuksista tai virheistä, jotka aiheutuvat raportin käytöstä.",
        ],
        disclaimerAcknowledge:
          "Ymmärrän, että raportti on suuntaa-antava eikä korvaa virallista veroneuvontaa.",
      },
    },
  },
  en: {
    app: {
      title: "Crypto Tax Reports",
      subtitle:
        "Generate capital gains reports from your broker transaction statements.",
      brokersTitle: "Brokers",
      coinmotionDescription: "CSV import",
      available: "Available",
      moreBrokers: "More brokers coming",
      planned: "Planned",
      howItWorks: "How it works",
      steps: [
        "Export your broker transaction statement as CSV.",
        "Review the disclaimer and upload the file.",
        "Download a zip containing PDF reports per currency.",
      ],
    },
    brokerModal: {
      title: "Generate tax report",
      uploadTitle: "Upload CSV",
      uploadDescription: "Returns a zip file with one PDF report per currency.",
      yearLabel: "Report year (optional)",
      yearHint: "Leave empty to include all years.",
      chooseFile: "Choose file",
      noFile: "No file selected",
      generate: "Generate report",
      generating: "Generating...",
      supportTitle: "Report ready",
      supportDescription: "Your report has been generated.",
      transactionsProcessed: "Transactions processed:",
      totalSalesVolume: "Total sales volume:",
      totalProfitLoss: "Total profit/loss:",
      download: "Download zip",
      donateTitle: "Support the developer",
      donateDescription:
        "If this tool saved you time, consider supporting future development.",
      previous: "Previous",
      next: "Next",
      success: "Report is ready. Download the zip to view PDFs.",
      errors: {
        fileRequired: "Select a CSV file first.",
        yearFormat: "Year must be in YYYY format.",
        uploadFailed: "Upload failed.",
        downloadFailed: "Download failed.",
        downloadExpired:
          "Download failed. Report may have expired. Generate again.",
      },
      enlargeVideo: "Click to enlarge",
      close: "Close",
    },
    brokers: {
      coinmotion: {
        name: "Coinmotion",
        videoTitle: "Video guide",
        videoDescription: "Add a short walkthrough clip here.",
        instructionsTitle: "How to export your CSV",
        instructionsSteps: [
          "Open Coinmotion and navigate to transactions.",
          "Choose the CSV export for the full date range.",
          "Save the CSV file to your computer.",
        ],
        disclaimerTitle: "Disclaimer",
        disclaimerParagraphs: [
          "This service generates an automated report based on transaction data from Coinmotion.",
          "The report is for informational purposes only and does not constitute tax advice.",
          "You are responsible for verifying the accuracy of the data submitted to tax authorities.",
          "The service provider is not responsible for any tax consequences resulting from use of this report.",
        ],
        disclaimerAcknowledge:
          "I understand that this report is informational only and does not replace official tax advice.",
      },
    },
  },
} as const;
