export type BrokerId = "coinmotion" | "binance"; // Union type for future brokers: "coinmotion" | "binance" | "kraken"

export interface BrokerConfig {
  id: BrokerId;
  name: string;
  apiEndpoint: string;
  fileType: string;
  hasYearSelection: boolean;
  hasTutorialVideo: boolean;
  videoAssetPath?: string;
  metricsEnabled: boolean;
}

export const BROKERS: Record<BrokerId, BrokerConfig> = {
  coinmotion: {
    id: "coinmotion",
    name: "Coinmotion",
    apiEndpoint: "/report/generate",
    fileType: ".csv",
    hasYearSelection: true,
    hasTutorialVideo: true,
    videoAssetPath: "/src/assets/coinmotion_help.gif",
    metricsEnabled: true,
  },
  binance: {
    id: "binance",
    name: "Binance",
    apiEndpoint: "",
    fileType: ".csv",
    hasYearSelection: false,
    hasTutorialVideo: false,
    metricsEnabled: false,
},
};
