import { sha256 } from "js-sha256";

type OidcConfiguration = {
  authorization_endpoint: string;
  token_endpoint: string;
  end_session_endpoint?: string;
};

type TokenResponse = {
  access_token: string;
  expires_in?: number;
  id_token?: string;
};

const authority = import.meta.env.VITE_OIDC_AUTHORITY as string | undefined;
const clientId = import.meta.env.VITE_OIDC_CLIENT_ID as string | undefined;
const audience = import.meta.env.VITE_OIDC_AUDIENCE as string | undefined;
const scope = (import.meta.env.VITE_OIDC_SCOPE as string | undefined) ?? "openid profile tax-helper.reports";
const redirectUri = `${window.location.origin}${window.location.pathname}`;

let configuration: OidcConfiguration | null = null;
let token: { value: string; expiresAt: number } | null = null;

function base64Url(bytes: Uint8Array): string {
  return btoa(String.fromCharCode(...bytes)).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

async function getConfiguration(): Promise<OidcConfiguration> {
  if (!authority || !clientId) throw new Error("OIDC is not configured");
  if (!configuration) {
    const response = await fetch(`${authority.replace(/\/$/, "")}/.well-known/openid-configuration`);
    if (!response.ok) throw new Error("Unable to load identity provider configuration");
    configuration = (await response.json()) as OidcConfiguration;
  }
  return configuration;
}

export async function beginLogin(): Promise<void> {
  const oidc = await getConfiguration();
  const verifier = base64Url(crypto.getRandomValues(new Uint8Array(32)));
  const verifierBytes = new TextEncoder().encode(verifier);
  const digest = globalThis.crypto?.subtle
    ? await globalThis.crypto.subtle.digest("SHA-256", verifierBytes)
    : sha256.arrayBuffer(verifier);
  const challenge = base64Url(new Uint8Array(digest));
  const state = base64Url(crypto.getRandomValues(new Uint8Array(24)));
  sessionStorage.setItem("oidc_pkce_verifier", verifier);
  sessionStorage.setItem("oidc_state", state);
  const authorizationParams: Record<string, string> = {
    response_type: "code",
    client_id: clientId!,
    redirect_uri: redirectUri,
    scope,
    state,
    code_challenge: challenge,
    code_challenge_method: "S256",
  };
  if (audience) authorizationParams.audience = audience;
  const params = new URLSearchParams(authorizationParams);
  window.location.assign(`${oidc.authorization_endpoint}?${params}`);
}

export async function completeLogin(): Promise<boolean> {
  const params = new URLSearchParams(window.location.search);
  const providerError = params.get("error");
  if (providerError) {
    const description = params.get("error_description");
    window.history.replaceState({}, document.title, window.location.pathname);
    throw new Error(description ? `${providerError}: ${description}` : providerError);
  }
  const code = params.get("code");
  if (!code) return false;
  const state = params.get("state");
  const expectedState = sessionStorage.getItem("oidc_state");
  const verifier = sessionStorage.getItem("oidc_pkce_verifier");
  sessionStorage.removeItem("oidc_state");
  sessionStorage.removeItem("oidc_pkce_verifier");
  if (!state || state !== expectedState || !verifier) throw new Error("Invalid OIDC callback state");
  const oidc = await getConfiguration();
  const response = await fetch(oidc.token_endpoint, { method: "POST", headers: { "Content-Type": "application/x-www-form-urlencoded" }, body: new URLSearchParams({ grant_type: "authorization_code", client_id: clientId!, code, redirect_uri: redirectUri, code_verifier: verifier }) });
  if (!response.ok) throw new Error("Unable to exchange OIDC authorization code");
  const data = (await response.json()) as TokenResponse;
  token = { value: data.access_token, expiresAt: Date.now() + (data.expires_in ?? 300) * 1000 };
  window.history.replaceState({}, document.title, window.location.pathname);
  return true;
}

export function getAccessToken(): string | null {
  return token && token.expiresAt > Date.now() ? token.value : null;
}

export async function logout(): Promise<void> {
  const oidc = await getConfiguration().catch(() => null);
  token = null;
  if (oidc?.end_session_endpoint) {
    const params = new URLSearchParams({ post_logout_redirect_uri: redirectUri });
    window.location.assign(`${oidc.end_session_endpoint}?${params}`);
  }
}

export function isOidcConfigured(): boolean {
  return Boolean(authority && clientId);
}
